using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalAves.Meta.WorldMap
{
    // Constroi (em geometria deformada pela mesma MapCurve) o chao com margens elevadas, os canais com agua,
    // as faces de borda, as pontes com espessura e os trechos de ladrilhos das fases dificeis.
    public sealed class WorldMapTerrain
    {
        public const float PathHalf = 1.18f;
        public const float EdgeStrip = 0.025f;
        public const float SlopeW = 0.075f;
        // A low physical shoulder supports the painted lip without creating a steep trench.
        public const float BankH = .039f;
        public const float RimW = 0.025f;
        public const float OuterHalf = 18f;
        // Broad painted surfaces need far fewer rows than the small rounded road caps.
        // Keep the cap independently refined instead of tessellating the entire lawn at that density.
        public const float Step = 0.60f;
        public const float WaterY = -0.23f;
        public const float Floor = -0.29f;
        public const float BridgeHalf = 0.66f;
        const float BandLen = 4.8f;
        const float PathBandLen = 2.5f;

        struct Seg
        {
            public float x0, h0, x1, h1;
            public int kind; // 0 outer, 1 rim, 2 face, 3 path, 4 path edge
            public float u0,u1;
        }

        static readonly Seg[] Profile = BuildProfile();

        public readonly Mesh ground = new Mesh { name = "WorldMapGround", indexFormat = IndexFormat.UInt32 };
        public readonly Mesh water = new Mesh { name = "WorldMapWater", indexFormat = IndexFormat.UInt32 };

        readonly List<Vector3> gv = new List<Vector3>(8192), gn = new List<Vector3>(8192);
        readonly List<Color> gc = new List<Color>(8192);
        readonly List<int> gt = new List<int>(16384);
        readonly List<Vector2> gu = new List<Vector2>(16384);
        readonly List<Vector2> laneCoordinates = new List<Vector2>(16384);
        readonly List<Vector3> lawnPalette = new List<Vector3>(16384), pathPalette = new List<Vector3>(16384);
        readonly List<Vector3> wv = new List<Vector3>(512), wn = new List<Vector3>(512);
        readonly List<Color> wc = new List<Color>(512);
        readonly List<Vector2> wu = new List<Vector2>(512);
        readonly List<Vector2> waterMotionCoordinates=new List<Vector2>(512);
        readonly List<int> wt = new List<int>(1024);
        readonly List<float> rows = new List<float>(256);
        readonly List<float> refinedRows = new List<float>(256);
        readonly List<float> shoreColumns=new List<float>();
        readonly Color[] rowArtTints = new Color[5];
        readonly List<(float s,int level)> shores=new List<(float,int)>();
        float rowInset0,rowInset1,rowEnd0,rowEnd1,rowCap0,rowCap1;

        WorldMapRoute route;
        float radius, cursor, curvature;
        public bool IllustratedBridge, Lava;
        public IReadOnlyList<PropPlacement> Scenery;
        MapPalette PaletteAt(float s) => Lava?WorldMapRoute.LavaPalette:route.PaletteAt(s);
        readonly List<List<int>> islandTriangles=new List<List<int>>();
        float basisS0=float.NaN,basisS1=float.NaN;
        Vector2 basis0,basis1;
        float lateralRadius;

        static Seg[] BuildProfile()
        {
            var outer = PathHalf + SlopeW + RimW;
            var top = PathHalf + SlopeW;
            var list = new List<Seg>();
            for (var side = -1; side <= 1; side += 2)
            {
                // Montado do exterior para o centro de cada lado, em x crescente nas duas metades.
                if (side < 0)
                {
                    for (var x = -OuterHalf; x < -3f;)
                    {var end=Mathf.Min(x+(x>=-5f?.15f:.8f),-3f);list.Add(new Seg { x0=x,h0=BankH,x1=end,h1=BankH,kind=0 });x=end;}
                    for (var x = -3f; x < -outer; x += .15f)
                        list.Add(new Seg { x0 = x, h0 = BankH, x1 = Mathf.Min(x + .15f, -outer), h1 = BankH, kind = 0 });
                    list.Add(new Seg { x0 = -outer, h0 = BankH, x1 = -top, h1 = BankH, kind = 1 });
                    list.Add(new Seg { x0 = -top, h0 = BankH, x1 = -PathHalf, h1 = 0, kind = 2 });
                    list.Add(new Seg { x0 = -PathHalf, h0 = 0, x1 = -PathHalf + EdgeStrip, h1 = 0, kind = 4 });
                }
                else
                {
                    list.Add(new Seg { x0 = PathHalf - EdgeStrip, h0 = 0, x1 = PathHalf, h1 = 0, kind = 4 });
                    list.Add(new Seg { x0 = PathHalf, h0 = 0, x1 = top, h1 = BankH, kind = 2 });
                    list.Add(new Seg { x0 = top, h0 = BankH, x1 = outer, h1 = BankH, kind = 1 });
                    for (var x = outer; x < 3f; x += .15f)
                        list.Add(new Seg { x0 = x, h0 = BankH, x1 = Mathf.Min(x + .15f, 3f), h1 = BankH, kind = 0 });
                    for (var x = 3f; x < OuterHalf;)
                    {var end=Mathf.Min(x+(x<5f?.15f:.8f),OuterHalf);list.Add(new Seg { x0=x,h0=BankH,x1=end,h1=BankH,kind=0 });x=end;}
                }
                if (side < 0)
                    for(var x=-PathHalf+EdgeStrip;x<PathHalf-EdgeStrip;x+=.16f)
                        list.Add(new Seg {x0=x,h0=0,x1=Mathf.Min(x+.16f,PathHalf-EdgeStrip),h1=0,kind=3});
            }
            for(int i=0;i<list.Count;i++)
            {var segment=list[i];segment.u0=GroundU(segment.x0);segment.u1=GroundU(segment.x1);list[i]=segment;}
            return list.ToArray();
        }
        static float GroundU(float x)
        {
            var a=Mathf.Abs(x);
            var u=a<=PathHalf?.18f*a/PathHalf:a<=PathHalf+SlopeW?Mathf.Lerp(.18f,.205f,(a-PathHalf)/SlopeW):Mathf.Lerp(.205f,.498f,Mathf.InverseLerp(PathHalf+SlopeW,3f,a));
            return .5f+Mathf.Sign(x)*u;
        }

        public static float FootHeight(float lateralOffset)
        {
            var a = Mathf.Abs(lateralOffset);
            if (a <= PathHalf) return 0;
            if (a >= PathHalf + SlopeW) return BankH;
            return BankH * (a - PathHalf) / SlopeW;
        }

        public static float LaneDistance(float x,float endDistance)
        {
            if(endDistance>=.6f)return Mathf.Abs(x)-PathHalf;
            var p=new Vector2(Mathf.Abs(x),endDistance-.6f);
            var k0=new Vector2(p.x/PathHalf,p.y/.6f).magnitude;
            var k1=new Vector2(p.x/(PathHalf*PathHalf),p.y/.36f).magnitude;
            return k0<.0001f?-.6f:k0*(k0-1)/Mathf.Max(k1,.0001f);
        }
        static float LaneHeight(float x,float end)=>BankH*Mathf.SmoothStep(0,1,Mathf.Clamp01(LaneDistance(x,end)/SlopeW));
        public static float SurfaceHeight(WorldMapRoute route,float s,float x)=>LaneHeight(x,route.DistanceFromLaneEnd(s));

        // Art-directed widening gives the distant road the broad silhouette of an illustrated map.
        // Props and ground share this mapping, so objects remain on the margins as it widens.
        public static float Lateral(float offset, float rel)
        {
            var scale = Mathf.Lerp(1,.8f,Mathf.InverseLerp(0,14,rel));
            var a = Mathf.Abs(offset);
            return Mathf.Sign(offset) * (a <= PathHalf ? a * scale : PathHalf * scale + a - PathHalf);
        }

        public void Rebuild(WorldMapRoute route, float radius, float cursorMeters, float back, float front)
        {
            this.route = route;
            this.radius = radius;
            curvature = route.Curvature;
            lateralRadius=radius/Mathf.Max(.1f,curvature);
            basisS0=basisS1=float.NaN;
            cursor = cursorMeters;
            gv.Clear(); gn.Clear(); gc.Clear(); gt.Clear(); gu.Clear();
            laneCoordinates.Clear();
            lawnPalette.Clear();pathPalette.Clear();
            wv.Clear(); wn.Clear(); wc.Clear(); wu.Clear(); wt.Clear();
            waterMotionCoordinates.Clear();

            var lo = Mathf.Max(0, cursorMeters - back);
            var hi = Mathf.Min(route.Length, cursorMeters + front);
            shores.Clear();
            foreach(var gap in route.Gaps)
            {
                if(gap.start>=lo-.7f&&gap.start<=hi+.7f)shores.Add((gap.start,gap.afterLevel));
                if(gap.end>=lo-.7f&&gap.end<=hi+.7f)shores.Add((gap.end,gap.afterLevel));
            }
            // Intervalos de terra = janela menos canais.
            var from = lo;
            var capNext = lo <= 0.001f; // so a borda real da terra recebe parede, nao o corte da janela
            var gaps = route.Gaps;
            for (var i = 0; i < gaps.Count && from < hi; i++)
            {
                var g = gaps[i];
                if (g.end <= from) continue;
                if (g.start >= hi) break;
                if (g.start > from) Land(from, Mathf.Min(g.start, hi), capNext, g.start <= hi);
                BridgeAndWater(g);
                from = Mathf.Max(from, g.end);
                capNext = true;
            }
            if (from < hi) Land(from, hi, capNext, hi >= route.Length - 0.001f);

            PostalRibbon(lo, hi);

            while (gu.Count < gv.Count) gu.Add(Vector2.zero);
            Apply(ground, gv, gn, gc, gu, gt);
            while(laneCoordinates.Count<gv.Count)laneCoordinates.Add(Vector2.zero);
            ground.SetUVs(1,laneCoordinates);
            while(lawnPalette.Count<gv.Count)lawnPalette.Add(Vector3.one);
            while(pathPalette.Count<gv.Count)pathPalette.Add(Vector3.one);
            ground.SetUVs(2,lawnPalette);ground.SetUVs(3,pathPalette);
            // Each island owns its material, even while two different grounds share the viewport.
            if(route.Islands!=null)
            {
                var count=route.Islands.ActiveIslandCount;
                while(islandTriangles.Count<count)islandTriangles.Add(new List<int>());
                foreach(var indices in islandTriangles)indices.Clear();
                for(var i=0;i<gt.Count;i+=3)
                {
                    var p=(gv[gt[i]]+gv[gt[i+1]]+gv[gt[i+2]])/3;
                    var s=cursor+Mathf.Atan2(p.z,p.y+radius)*radius;
                    var indices=islandTriangles[route.IslandIndexAt(s)];
                    indices.Add(gt[i]);indices.Add(gt[i+1]);indices.Add(gt[i+2]);
                }
                ground.subMeshCount=count;
                for(var i=0;i<count;i++)ground.SetTriangles(islandTriangles[i],i,false);
            }
            Apply(water, wv, wn, wc, wu, wt);
            water.SetUVs(1,waterMotionCoordinates);
        }

        static void Apply(Mesh mesh, List<Vector3> v, List<Vector3> n, List<Color> c, List<Vector2> uv, List<int> t)
        {
            mesh.Clear();
            if (v.Count == 0) return;
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetColors(c);
            if (uv != null) mesh.SetUVs(0, uv);
            mesh.SetTriangles(t, 0, false);
            mesh.RecalculateBounds();
        }

        // ---- coordenadas ----------------------------------------------------------------------------

        Vector2 Basis(float s)
        {
            if(s==basisS0)return basis0;
            if(s==basisS1)return basis1;
            var angle=(s-cursor)/radius;
            basisS0=basisS1;basis0=basis1;basisS1=s;
            basis1=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));return basis1;
        }
        Vector3 P(float s, float cx, float x, float h)
        {
            var basis=Basis(ShoreMeters(s,x));
            var lateral=(cx+Lateral(x,s-cursor))/lateralRadius;
            var ring=radius-lateralRadius+(lateralRadius+h)*Mathf.Cos(lateral);
            return new Vector3((lateralRadius+h)*Mathf.Sin(lateral),ring*basis.x-radius,ring*basis.y);
        }
        Vector3 SurfaceNormal(float s,Vector3 point)
        {
            // sin(asin(x)) is x: avoid recomputing three angles for every corner of every quad.
            var sin=Mathf.Clamp(point.x/(lateralRadius+BankH),-1,1);
            var cos=Mathf.Sqrt(Mathf.Max(0,1-sin*sin));var basis=Basis(s);
            return new Vector3(sin,cos*basis.x,cos*basis.y);
        }
        float ShoreMeters(float s,float x)
        {
            foreach(var edge in shores)
            {
                var distance=Mathf.Abs(s-edge.s);
                if(distance<.65f)
                {
                    var shape=route.Islands?.AtLevel(edge.level)?.bankShape??WorldMapBankShape.Rounded;
                    float Triangle(float n)=>Mathf.Abs(Mathf.Repeat(n,1)*2-1)*2-1;
                    var shift=shape==WorldMapBankShape.Angular?.30f*Triangle(x*.65f+edge.level*.13f)+.10f*Triangle(x*1.4f):
                        shape==WorldMapBankShape.Scalloped?.28f*Mathf.Sin(x*1.8f+edge.level*.4f)+.08f*Mathf.Sin(x*3.9f):
                        .22f*Mathf.Sin(x*.9f+edge.level*.4f)+.07f*Mathf.Sin(x*2.1f);
                    return s+shift*(1-Mathf.SmoothStep(0,1,distance/.65f));
                }
            }
            return s;
        }
        Vector3 N(float s) => MapCurve.Normal(s - cursor, radius);
        Vector3 T(float s) => MapCurve.Tangent(s - cursor, radius);

        static Color Vc(Color c, float k = 1)
        {
            c = new Color(c.r * k, c.g * k, c.b * k, 1);
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;
        }

        // ---- terra ----------------------------------------------------------------------------------

        void Land(float a, float b, bool capStart, bool capEnd)
        {
            rows.Clear();
            rows.Add(a);
            for (var k = Mathf.FloorToInt(a / Step) + 1; k * Step < b - 0.01f; k++) rows.Add(k * Step);
            rows.Add(b);
            refinedRows.Clear();
            for(var row=0;row+1<rows.Count;row++)
            {
                var s0=rows[row];var s1=rows[row+1];refinedRows.Add(s0);
                var inset0=route.InsetAmount(s0);var inset1=route.InsetAmount(s1);
                // Refine the rounded transition, not the entire lawn before the road starts.
                if(inset0!=inset1||inset0>0&&inset0<.999f||inset1>0&&inset1<.999f)
                {
                    int divisions=Mathf.CeilToInt((s1-s0)/.04f);
                    for(var j=1;j<divisions;j++)refinedRows.Add(Mathf.Lerp(s0,s1,j/(float)divisions));
                }
            }
            refinedRows.Add(b);rows.Clear();rows.AddRange(refinedRows);
            var cxPrev = route.CenterX(rows[0]);
            for (var r = 0; r + 1 < rows.Count; r++)
            {
                var s0 = rows[r];
                var s1 = rows[r + 1];
                var mid = (s0 + s1) * 0.5f;
                var cx1 = route.CenterX(s1);
                var pal = PaletteAt(mid);
                var inset = route.InsetAmount(mid);
                rowInset0=route.InsetAmount(s0);rowInset1=route.InsetAmount(s1);
                rowCap0=Mathf.Sqrt(rowInset0);rowCap1=Mathf.Sqrt(rowInset1);
                rowEnd0=route.DistanceFromLaneEnd(s0);rowEnd1=route.DistanceFromLaneEnd(s1);
                for (var kind = 0; kind < rowArtTints.Length; kind++)
                {
                    var segment = new Seg { kind = kind };
                    var tint = SegColor(segment, pal, 0, 0, inset);
                    var reference = SegColor(segment, route.LavaAt(mid,Lava)?WorldMapRoute.LavaPalette:WorldMapRoute.PaletteOfChapter(0), 0, 0, inset);
                    rowArtTints[kind] = new Color(tint.r / Mathf.Max(.01f, reference.r), tint.g / Mathf.Max(.01f, reference.g), tint.b / Mathf.Max(.01f, reference.b), -inset);
                }
                for (var i = 0; i < Profile.Length; i++)
                {
                    var sg = Profile[i];
                    // The bitmap supplies the finished ground colors, bevels and bands. This ratio only
                    // recolors it for later areas; alpha 0 identifies illustrated ground, alpha 1 overlays.
                    AddSegment(sg, s0, s1, cxPrev, cx1, rowArtTints[sg.kind]);
                }
                cxPrev = cx1;
            }
            // Tiles das fases dificeis que caem nesta terra.
            // Difficulty is conveyed by level tokens; gray paving grids distracted from the lane art.
            if (capStart) Cap(a, -1);
            if (capEnd) Cap(b, 1);
            if(capStart)ShoreRim(a,1);
            if(capEnd)ShoreRim(b,-1);
        }

        void ShoreRim(float s,int landDirection)
        {
            var c=Vc(Color.Lerp(PaletteAt(s).rim,new Color(1,.96f,.72f),.25f));
            ShoreColumns(s);
            for(int i=0;i+1<shoreColumns.Count;i++)
            {
                var x=shoreColumns[i];var e=shoreColumns[i+1];var end=s+landDirection*.035f;
                Quad(gv,gn,gc,gt,P(s,route.CenterX(s),x,BankH+.003f),P(end,route.CenterX(end),x,BankH+.003f),P(end,route.CenterX(end),e,BankH+.003f),P(s,route.CenterX(s),e,BankH+.003f),N(s),N(end),N(end),N(s),c);
            }
        }

        static Color SegColor(Seg sg, MapPalette pal, int band, int pband, float inset = 1)
        {
            var outer = band == 0 ? pal.outerA : pal.outerB;
            var path = pband == 0 ? pal.pathA : pal.pathB;
            switch (sg.kind)
            {
                case 0: return Vc(outer);
                case 1: return Vc(Color.Lerp(outer, pal.rim, .2f * inset));
                case 2: return Vc(Color.Lerp(outer, path, .3f * inset));
                case 4: return Vc(Color.Lerp(path, outer, .12f));
                default: return Vc(path);
            }
        }

        void AddSegment(Seg sg, float s0, float s1, float cx0, float cx1, Color color)
        {
            float Edge(float x,float cap)
            {
                return Mathf.Abs(x) <= PathHalf ? x * cap : x - Mathf.Sign(x) * PathHalf * (1 - cap);
            }
            var x00=Edge(sg.x0,rowCap0);var x01=Edge(sg.x0,rowCap1);
            var x11=Edge(sg.x1,rowCap1);var x10=Edge(sg.x1,rowCap0);
            var p00 = P(s0, cx0, x00, LaneHeight(x00,rowEnd0));
            var p01 = P(s1, cx1, x01, LaneHeight(x01,rowEnd1));
            var p11 = P(s1, cx1, x11, LaneHeight(x11,rowEnd1));
            var p10 = P(s0, cx0, x10, LaneHeight(x10,rowEnd0));
            Quad(gv, gn, gc, gt, p00, p01, p11, p10,SurfaceNormal(s0,p00),SurfaceNormal(s1,p01),
                SurfaceNormal(s1,p11),SurfaceNormal(s0,p10),color,true);
            // The cap's grass has the grass palette even when it crosses a former lane column.
            Color GroundTint(float x,float end)
            {
                var tint=rowArtTints[LaneDistance(x,end)>=0?0:3];tint.a=color.a;return tint;
            }
            gc[gc.Count-4]=GroundTint(x00,rowEnd0);gc[gc.Count-3]=GroundTint(x01,rowEnd1);
            gc[gc.Count-2]=GroundTint(x11,rowEnd1);gc[gc.Count-1]=GroundTint(x10,rowEnd0);
            // Carry both palettes to the fragment; choosing one on a cap vertex creates diagonal seams.
            while(lawnPalette.Count<gv.Count-4)lawnPalette.Add(Vector3.one);
            while(pathPalette.Count<gv.Count-4)pathPalette.Add(Vector3.one);
            var lawn=rowArtTints[0];var path=rowArtTints[3];
            for(var k=0;k<4;k++){lawnPalette.Add(new Vector3(lawn.r,lawn.g,lawn.b));pathPalette.Add(new Vector3(path.r,path.g,path.b));}
            while (gu.Count < gv.Count - 4) gu.Add(Vector2.zero);
            gu.Add(new Vector2(sg.u0, s0 / 14f));
            gu.Add(new Vector2(sg.u0, s1 / 14f));
            gu.Add(new Vector2(sg.u1, s1 / 14f));
            gu.Add(new Vector2(sg.u1, s0 / 14f));
            // Physical lane coordinates keep the painted bevel the same width on curves and caps.
            while(laneCoordinates.Count<gv.Count-4)laneCoordinates.Add(Vector2.zero);
            laneCoordinates.Add(new Vector2(x00,rowEnd0));
            laneCoordinates.Add(new Vector2(x01,rowEnd1));
            laneCoordinates.Add(new Vector2(x11,rowEnd1));
            laneCoordinates.Add(new Vector2(x10,rowEnd0));
        }

        // Parede vertical na ponta da terra (face de borda mais escura que o topo).
        void ShoreColumns(float s)
        {
            shoreColumns.Clear();var cap=Mathf.Sqrt(route.InsetAmount(s));
            float Edge(float x)=>Mathf.Abs(x)<=PathHalf?x*cap:x-Mathf.Sign(x)*PathHalf*(1-cap);
            foreach(var segment in Profile){shoreColumns.Add(Edge(segment.x0));shoreColumns.Add(Edge(segment.x1));}
            shoreColumns.Sort();
            for(int i=shoreColumns.Count-1;i>0;i--)if(Mathf.Abs(shoreColumns[i]-shoreColumns[i-1])<.0001f)shoreColumns.RemoveAt(i);
        }
        void Cap(float s, int direction)
        {
            var cx = route.CenterX(s);
            var pal = PaletteAt(s);
            var n = T(s) * direction;
            var top=Vc(Color.Lerp(pal.outerA,pal.face,.28f));
            var bottom=Vc(Color.Lerp(pal.outerA,pal.face,.68f),.96f);
            // A continuous river bank. Profile categories must not become colored vertical panels.
            ShoreColumns(s);
            for(int i=0;i+1<shoreColumns.Count;i++)
            {
                var x=shoreColumns[i];var end=shoreColumns[i+1];
                var floor=route.WaterHeight(s)-.06f;
                Quad(gv,gn,gc,gt,P(s,cx,x,BankH),P(s,cx,end,BankH),P(s,cx,end,floor),P(s,cx,x,floor),n,n,n,n,top);
                gc[gc.Count-2]=bottom;gc[gc.Count-1]=bottom;
            }
        }

        // Low scenery is part of the curved ground, so ponds, leaves and stones cannot float.
        // Different regional recipes break up repeated large sprites without crowding the lane.
        void GroundDetails(float from,float to)
        {
            var first=Mathf.Max(1,Mathf.FloorToInt(route.LevelAt(from))-1);
            var last=Mathf.Min(route.LevelSlots,Mathf.CeilToInt(route.LevelAt(to))+1);
            for(var level=first;level<=last;level++)
            {
                if(route.Islands?.unifiedBase==true&&level%3!=0)continue;
                var s=route.Pos(level)+.46f;
                if(s<from||s>to||route.InGap(s,.7f))continue;
                var chapter=route.ChapterAtMeters(s);var pal=PaletteAt(s);
                var side=level%2==0?-1:1;
                var x=side*(1.83f+(level%3)*.10f);
                var recipe=route.Islands?.unifiedBase==true?2:(level+chapter*2)%5;
                if(DetailOccupied(s,x,recipe==0?.46f:.12f))continue;
                // Puddles and openings are independent, editable sprites, never painted into this mesh.
                if(recipe==0)continue;
                if(recipe==1||recipe==3)
                {
                    for(var k=0;k<3;k++)
                    {
                        var px=x+(k-1)*.18f;var ps=s+(k%2)*.17f;
                        var color=route.LavaAt(s,Lava)?new Color(.9f,.36f,.16f):chapter%3==0?new Color(.92f,.68f,.28f):chapter%3==1?new Color(.59f,.43f,.77f):new Color(.72f,.79f,.67f);
                        Patch(ps,px,.11f-k*.013f,.12f-k*.012f,Vc(Color.Lerp(color,pal.face,.3f)),0,5);
                        Patch(ps+.018f,px-.012f,.09f-k*.012f,.10f-k*.012f,Vc(color),.004f,5);
                    }
                }
                else
                {
                    // Sparse broad grass blades, or warm mineral chips in LavaLand.
                    for(var k=0;k<3;k++)
                    {
                        var dx=(k-1)*.085f;
                        var col=Vc(Color.Lerp(pal.face,pal.outerA,.38f));
                        var a=DetailPoint(s,x+dx-.065f,.006f);
                        var b=DetailPoint(s+.19f+(k==1?.08f:0),x+dx+.02f,.006f);
                        var c=DetailPoint(s-.025f,x+dx+.045f,.006f);
                        Quad(gv,gn,gc,gt,a,b,c,a,N(s),N(s),N(s),N(s),col);
                    }
                }
            }
        }
        Vector3 DetailPoint(float s,float x,float lift)=>
            P(s,route.CenterX(s),x,SurfaceHeight(route,s,x)+lift);
        void TrailDetails(float from,float to)
        {
            if(route.Islands?.unifiedBase==true)return;
            var first=Mathf.Max(1,Mathf.FloorToInt(route.LevelAt(from)));
            var last=Mathf.Min(route.LevelSlots-1,Mathf.CeilToInt(route.LevelAt(to)));
            for(var level=first;level<=last;level++)
            {
                if(level%3!=1)continue;
                var s=(route.Pos(level)+route.Pos(level+1))*.5f;
                if(route.InGap(s,1.25f)||route.InsetAmount(s)<.99f)continue;
                var x=level%2==0?-.72f:.72f;var p=PaletteAt(s);
                if(route.LavaAt(s,Lava))
                {
                    LavaCrack(s,x,level);
                    continue;
                }
                // Painted potholes have a warm inner wall and a tiny lower lip; the clickable route stays clear.
                var soil=Color.Lerp(p.face,new Color(.30f,.18f,.17f),.65f);
                Patch(s,x,.17f,.22f,Vc(Color.Lerp(p.pathA,soil,.45f)),.002f,14);
                Patch(s+.025f,x,.12f,.145f,Vc(soil),.003f,14);
                Patch(s-.15f,x,.12f,.025f,Vc(Color.Lerp(p.pathA,Color.white,.28f)),.004f,14);
            }
        }

        void LavaCrack(float s,float x,int seed)
        {
            var side=Mathf.Sign(x);
            void Vein(float startS,float startX,float width,float lift,Color color)
            {
                for(var i=0;i<6;i++)
                {
                    float X(int n)=>startX+side*n*.065f+(n%2==0?-.025f:.025f);
                    float S(int n)=>startS+(n-3)*.10f;
                    var a=DetailPoint(S(i),X(i)-width,lift);var b=DetailPoint(S(i+1),X(i+1)-width,lift);
                    var c=DetailPoint(S(i+1),X(i+1)+width,lift);var d=DetailPoint(S(i),X(i)+width,lift);
                    Quad(gv,gn,gc,gt,a,b,c,d,N(S(i)),N(S(i+1)),N(S(i+1)),N(S(i)),Vc(color));
                }
            }
            var origin=x-side*.12f;
            Vein(s,origin,.059f,.011f,new Color(.25f,.065f,.10f));
            Vein(s,origin,.031f,.014f,new Color(1.7f,.29f,.025f));
            Vein(s,origin,.011f,.017f,new Color(2.2f,1.1f,.18f));
            if(seed%2==1)
            {
                Patch(s-.08f,x,.13f,.19f,Vc(new Color(.32f,.075f,.10f)),.012f,8);
                Patch(s-.08f,x,.095f,.14f,Vc(new Color(1.7f,.40f,.035f)),.015f,9);
                Patch(s-.09f,x-.02f,.05f,.08f,Vc(new Color(2,1.05f,.2f)),.018f,9);
            }
        }

        bool DetailOccupied(float s,float x,float padding)
        {
            if(Scenery==null)return false;
            foreach(var p in Scenery)
                if(Mathf.Abs(p.s-s)<padding+.40f&&
                   Mathf.Abs(p.offset-x)<WorldMapCards.Width(p.sprite,p.height)*.5f+padding)return true;
            return false;
        }

        void Patch(float s,float x,float width,float length,Color color,float lift,int count=20)
        {
            var center=DetailPoint(s,x,.006f+lift);
            for(var i=0;i<count;i++)
            {
                Vector3 Edge(int k)
                {
                    var a=k*Mathf.PI*2/count;
                    var wobble=count>6?1+.1f*Mathf.Sin(a*3+s):1;
                    var ps=s+Mathf.Sin(a)*length*wobble;
                    return DetailPoint(ps,x+Mathf.Cos(a)*width*wobble,.006f+lift);
                }
                Quad(gv,gn,gc,gt,center,Edge(i+1),Edge(i),center,N(s),N(s),N(s),N(s),color);
            }
        }

        // ---- ladrilhos ------------------------------------------------------------------------------

        // Narrow diagonal postal ribbon; its width is independent of the illustrated road widening.
        void PostalRibbon(float a, float b)
        {
            // The route starts at its first medal: leave the entrance lawn clear.
            if(route.Islands?.unifiedBase==true)a=Mathf.Max(a,route.Pos(1));
            var cream = Vc(new Color32(255, 244, 204, 255));
            var purple = Vc(new Color32(207, 119, 225, 255));
            Vector3 RibbonP(float s, float x, float h) => MapCurve.Point(s - cursor, route.CenterX(s) + x, SurfaceHeight(route,s,x)+h, radius);
            for (var s = a; s < b; s += 0.12f)
            {
                if (route.InGap(s, .12f)) continue;
                var e = Mathf.Min(s + 0.12f, b);
                if (route.InGap(e, .025f)) continue;
                Quad(gv, gn, gc, gt, RibbonP(s,-.049f,.016f), RibbonP(e,-.049f,.016f),
                    RibbonP(e,.049f,.016f), RibbonP(s,.049f,.016f), N(s),N(e),N(e),N(s), cream);
            }
            for (var s = Mathf.Ceil(a / .48f) * .48f; s + .3f < b; s += .48f)
            {
                if (route.InGap(s, .15f) || route.InGap(s + .3f, .15f)) continue;
                Quad(gv, gn, gc, gt, RibbonP(s,-.044f,.019f), RibbonP(s+.24f,-.044f,.019f),
                    RibbonP(s+.30f,.044f,.019f), RibbonP(s+.06f,.044f,.019f), N(s),N(s+.24f),N(s+.30f),N(s+.06f), purple);
            }
        }

        void TilePatch(int level, float landA, float landB)
        {
            const float pitch = 0.28f;
            const float size = 0.245f;
            var center = route.Pos(level);
            var cx = route.CenterX(center);
            var pal = PaletteAt(center);
            for (var row = 0; row < 4; row++)
            {
                var s = center + (row - 1.5f) * pitch;
                if (s - size * 0.5f < landA || s + size * 0.5f > landB) continue;
                for (var col = 0; col < 5; col++)
                {
                    var x = (col - 2) * pitch;
                    var color = Color.Lerp(pal.pathA, ((row + col) & 1) == 0 ? pal.tileA : pal.tileB, 0.40f);
                    Paver(s, x, size, Vc(color));
                }
            }
        }

        void Paver(float s, float offset, float size, Color color)
        {
            var a = size * 0.5f; var r = 0.035f;
            var outline = new[] { new Vector2(-a+r,-a),new Vector2(a-r,-a),new Vector2(a,-a+r),
                new Vector2(a,a-r),new Vector2(a-r,a),new Vector2(-a+r,a),new Vector2(-a,a-r),new Vector2(-a,-a+r) };
            var center = P(s,route.CenterX(s),offset,0.022f);
            for(var i=0;i<outline.Length;i++)
            {
                var p=outline[i]; var q=outline[(i+1)%outline.Length];
                var ps=s+p.y; var qs=s+q.y;
                var pt=P(ps,route.CenterX(ps),offset+p.x,0.022f);
                var qt=P(qs,route.CenterX(qs),offset+q.x,0.022f);
                Quad(gv,gn,gc,gt,center,qt,pt,center,N(s),N(qs),N(ps),N(s),color);
                Quad(gv,gn,gc,gt,P(ps,route.CenterX(ps),offset+p.x,0.004f),pt,qt,
                    P(qs,route.CenterX(qs),offset+q.x,0.004f),N(ps),N(ps),N(qs),N(qs),color*0.86f);
            }
        }

        // ---- canal, agua e ponte --------------------------------------------------------------------

        void BridgeAndWater(WorldMapRoute.Gap g)
        {
            var settings=route.CrossingAt(g);
            var pal = PaletteAt(g.start-.01f);
            if(settings!=null&&!settings.customChannelColor)
            {
                var colors=new[]{new Color32(28,184,229,255),new Color32(143,68,213,255),new Color32(229,89,185,255),new Color32(255,104,28,255),new Color32(58,206,184,255)};
                pal.water=colors[(int)settings.liquid];
            }
            var waterHeight=route.WaterHeight((g.start+g.end)*.5f);
            // Agua: ausencia de terra com uma superficie propria abaixo das margens.
            rows.Clear();
            var a = g.start - 0.05f;
            var b = g.end + 0.05f;
            rows.Add(a);
            for (var k = Mathf.FloorToInt(a / Step) + 1; k * Step < b - 0.01f; k++) rows.Add(k * Step);
            rows.Add(b);
            for (var r = 0; r + 1 < rows.Count; r++)
            {
                var s0 = rows[r];
                var s1 = rows[r + 1];
                var cx0 = route.CenterX(s0);
                var cx1 = route.CenterX(s1);
                var k0 = WaterShade(s0, g);
                var k1 = WaterShade(s1, g);
                var base0 = Vc(pal.water, k0);
                var base1 = Vc(pal.water, k1);
                base0.a=Mathf.InverseLerp(g.start,g.end,s0);base1.a=Mathf.InverseLerp(g.start,g.end,s1);
                // Water must also tessellate across the planet; a single chord would sink below it.
                for (var x0 = -OuterHalf; x0 < OuterHalf; x0 += .4f)
                {
                    var x1 = Mathf.Min(x0 + .4f, OuterHalf);
                    var baseIndex = wv.Count;
                    wv.Add(P(s0,cx0,x0,waterHeight)); wv.Add(P(s1,cx1,x0,waterHeight));
                    wv.Add(P(s1,cx1,x1,waterHeight)); wv.Add(P(s0,cx0,x1,waterHeight));
                    var n0 = N(s0); var n1 = N(s1);
                    wn.Add(n0); wn.Add(n1); wn.Add(n1); wn.Add(n0);
                    wc.Add(base0); wc.Add(base1); wc.Add(base1); wc.Add(base0);
                    wu.Add(new Vector2(x0,s0)); wu.Add(new Vector2(x0,s1));
                    wu.Add(new Vector2(x1,s1)); wu.Add(new Vector2(x1,s0));
                    var winterRule=route.Islands?.Managed(settings)==true?(route.Islands.Recipe(settings).christmas?.2f:.1f):0;
                    for(int i=0;i<4;i++)waterMotionCoordinates.Add(new Vector2((int)(settings?.waterMotion??WorldMapWaterMotion.Ripples)+winterRule,(int)(settings?.liquid??WorldMapLiquid.Clear)+(settings?.waterGloss??.85f)*.1f));
                    wt.Add(baseIndex); wt.Add(baseIndex + 1); wt.Add(baseIndex + 2);
                    wt.Add(baseIndex); wt.Add(baseIndex + 2); wt.Add(baseIndex + 3);
                }
            }
            Bridge(g, pal);

        }

        static float WaterShade(float s, WorldMapRoute.Gap g)
        {
            var d = Mathf.Min(s - g.start, g.end - s);
            return Mathf.Lerp(0.96f, 1f, Mathf.SmoothStep(0, 1, d / .4f));
        }

        void Bridge(WorldMapRoute.Gap g, MapPalette pal)
        {
            var settings=route.CrossingAt(g);
            var crossing=settings?.crossing??WorldMapCrossing.Logs;
            if(crossing==WorldMapCrossing.OpenWater||crossing==WorldMapCrossing.Stones)return;
            var plankA = Vc(new Color32(139, 215, 155, 255));
            var plankB = Vc(new Color32(154, 223, 165, 255));
            var beam = Vc(new Color32(100, 170, 120, 255));
            // Cross straight between the banks. End sleepers overlap the full shoreline warp,
            // including transparent art margins, so neither end floats above open water.
            var overlap=Mathf.Max(.4f,settings?.bridgeBankOverlap??.6f);
            var from = g.start - overlap;
            var to = g.end + overlap;
            bool newDeck=route.Islands?.bridgeDeck!=null;
            var half=newDeck?(settings?.bridgeWidth??1.45f)/1.44f:BridgeHalf;
            Vector3 BridgeP(float s,float x)
            {
                var arch=.035f*Mathf.Sin(Mathf.PI*Mathf.InverseLerp(from,to,s));
                return MapCurve.Point(s-cursor,route.CenterX(s)+Lateral(x,s-cursor),BankH+.028f+arch,radius,route.Curvature);
            }
            if(IllustratedBridge)
            {
                for(var s=from;s<to-.001f;s+=Step)
                for(var x=-half;x<half-.001f;x+=.16f)
                {
                    var e=Mathf.Min(s+Step,to);var x1=Mathf.Min(x+.16f,half);
                    var style=newDeck?2:route.Islands?.unifiedBase==true?(crossing==WorldMapCrossing.Planks?3:2):route.LavaAt(g.end+.01f,Lava)?3:2+(g.afterLevel/9+2)%3;
                    Quad(gv,gn,gc,gt,BridgeP(s,x),BridgeP(e,x),BridgeP(e,x1),BridgeP(s,x1),N(s),N(e),N(e),N(s),new Color(1,1,1,style));
                    while(gu.Count<gv.Count-4)gu.Add(Vector2.zero);
                    gu.Add(new Vector2(Mathf.InverseLerp(-half,half,x),Mathf.InverseLerp(from,to,s)));
                    gu.Add(new Vector2(Mathf.InverseLerp(-half,half,x),Mathf.InverseLerp(from,to,e)));
                    gu.Add(new Vector2(Mathf.InverseLerp(-half,half,x1),Mathf.InverseLerp(from,to,e)));
                    gu.Add(new Vector2(Mathf.InverseLerp(-half,half,x1),Mathf.InverseLerp(from,to,s)));
                }
                return;
            }
            Box(gv, gn, gc, gt, from, to, -BridgeHalf, BridgeHalf, -0.035f, 0.005f, beam, beam, true);
            Box(gv, gn, gc, gt, from, to, -BridgeHalf-.016f, BridgeHalf+.016f, .005f, .018f,
                Vc(new Color32(246, 243, 191, 255)), beam, true);
            var index = 0;
            for (var s = from; s < to - 0.05f; s += 0.72f, index++)
            {
                var e = Mathf.Min(s + 0.69f, to);
                Box(gv, gn, gc, gt, s, e, -BridgeHalf, BridgeHalf, 0, 0.025f, (index & 1) == 0 ? plankA : plankB, beam, false);
            }
        }

        // ---- primitivas -----------------------------------------------------------------------------

        // Caixa no espaco (s, x lateral, altura) deformada pela curva. A face superior e laterais longas
        // sao subdivididas a cada Step para acompanhar a superficie.
        void Box(List<Vector3> v, List<Vector3> n, List<Color> c, List<int> t,
            float s0, float s1, float x0, float x1, float h0, float h1, Color top, Color side, bool subdivide)
        {
            var count = subdivide ? Mathf.Max(1, Mathf.CeilToInt((s1 - s0) / Step)) : 1;
            for (var i = 0; i < count; i++)
            {
                var a = Mathf.Lerp(s0, s1, (float)i / count);
                var b = Mathf.Lerp(s0, s1, (float)(i + 1) / count);
                var ca = route.CenterX(a);
                var cb = route.CenterX(b);
                var na = N(a);
                var nb = N(b);
                // topo
                Quad(v, n, c, t, P(a, ca, x0, h1), P(b, cb, x0, h1), P(b, cb, x1, h1), P(a, ca, x1, h1), na, nb, nb, na, top);
                // laterais
                var left = new Vector3(-1, 0, 0);
                var right = new Vector3(1, 0, 0);
                Quad(v, n, c, t, P(a, ca, x0, h0), P(b, cb, x0, h0), P(b, cb, x0, h1), P(a, ca, x0, h1), left, left, left, left, side);
                Quad(v, n, c, t, P(a, ca, x1, h1), P(b, cb, x1, h1), P(b, cb, x1, h0), P(a, ca, x1, h0), right, right, right, right, side);
            }
            var cs = route.CenterX(s0);
            var ce = route.CenterX(s1);
            var back = -T(s0);
            var fwd = T(s1);
            Quad(v, n, c, t, P(s0, cs, x0, h0), P(s0, cs, x1, h0), P(s0, cs, x1, h1), P(s0, cs, x0, h1), back, back, back, back, side);
            Quad(v, n, c, t, P(s1, ce, x0, h1), P(s1, ce, x1, h1), P(s1, ce, x1, h0), P(s1, ce, x0, h0), fwd, fwd, fwd, fwd, side);
        }

        // Quad de 4 vertices proprios (cor chapada por face). Enrola para que a face aponte para a normal.
        static void Quad(List<Vector3> v, List<Vector3> n, List<Color> c, List<int> t,
            Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 n0, Vector3 n1, Vector3 n2, Vector3 n3, Color color,bool unitNormals=false)
        {
            var i = v.Count;
            v.Add(p0); v.Add(p1); v.Add(p2); v.Add(p3);
            n.Add(unitNormals?n0:n0.normalized); n.Add(unitNormals?n1:n1.normalized); n.Add(unitNormals?n2:n2.normalized); n.Add(unitNormals?n3:n3.normalized);
            c.Add(color); c.Add(color); c.Add(color); c.Add(color);
            var facing = Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), n0 + n1 + n2 + n3);
            if (facing >= 0) { t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3); }
            else { t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2); }
        }
    }
}
