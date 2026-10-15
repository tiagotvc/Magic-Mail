using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Meta.WorldMap
{
    // Medalhao postal procedural (mesma especificacao do postal-level-token.obj: face roxa, aro dourado, lateral creme),
    // gerado em codigo para ter normais/cores por vertice sem depender do mapeamento de submalhas do OBJ.
    public static class WorldMapTokenMesh
    {
        public const float Radius = 0.46f;
        public const float SideHeight = .09f;
        public const float FaceHeight = .125f;
        public const float FaceRadius = .411f;
        public const float RimInner = .445f;
        const int Segments = 128;

        static Color Vc(Color c)
        {
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;
        }

        public static Mesh Build()
        {
            var cream = Vc(new Color32(255, 223, 178, 255));
            var creamLow = Vc(new Color32(221, 171, 112, 255));
            var gold = Vc(new Color32(255, 246, 212, 255));
            var goldDark = Vc(new Color32(214, 177, 119, 255));
            var purpleOuter = Vc(new Color32(166, 88, 204, 255));
            var purpleInner = Vc(new Color32(209, 139, 230, 255));
            cream.a = creamLow.a = gold.a = goldDark.a = 0;
            purpleOuter = purpleInner = Color.white;
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var c = new List<Color>();
            var t = new List<int>();

            void Ring(float r0, float y0, float r1, float y1, Vector3Func nrm, Color c0, Color c1)
            {
                var start = v.Count;
                for (var i = 0; i <= Segments; i++)
                {
                    var a = i * Mathf.PI * 2 / Segments;
                    var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    v.Add(dir * r0 + Vector3.up * y0);
                    v.Add(dir * r1 + Vector3.up * y1);
                    var nn = nrm(dir);
                    n.Add(nn); n.Add(nn);
                    c.Add(c0); c.Add(c1);
                }
                for (var i = 0; i < Segments; i++)
                {
                    var a = start + i * 2;
                    // anel visto de cima/fora: ordem escolhida para a normal apontar para fora
                    t.Add(a); t.Add(a + 1); t.Add(a + 2);
                    t.Add(a + 1); t.Add(a + 3); t.Add(a + 2);
                }
            }

            // Continuous rounded cream body: warm underside, light side and a broad visible top rim.
            Ring(Radius*.96f,0,Radius,.018f,d=>d,creamLow,creamLow);
            Ring(Radius,.018f,Radius,SideHeight*.76f,d=>d,creamLow,cream);
            Ring(Radius,SideHeight*.76f,Radius*.985f,SideHeight,d=>(d+Vector3.up).normalized,cream,gold);
            Ring(Radius*.985f,SideHeight,FaceRadius,SideHeight+.008f,d=>Vector3.up,gold,gold);
            Ring(FaceRadius,SideHeight+.008f,FaceRadius*.965f,FaceHeight-.004f,d=>(Vector3.up+d*.2f).normalized,purpleOuter,purpleInner);
            Ring(FaceRadius*.965f,FaceHeight-.004f,FaceRadius*.4f,FaceHeight,d=>Vector3.up,purpleOuter,purpleInner);
            Ring(FaceRadius*.4f,FaceHeight,0,FaceHeight,d=>Vector3.up,purpleOuter,purpleInner);

            // Corrige o enrolamento de cada triangulo para que a face aponte para a normal do vertice.
            for (var i = 0; i < t.Count; i += 3)
            {
                var a = t[i]; var b = t[i + 1]; var d = t[i + 2];
                var f = Vector3.Cross(v[b] - v[a], v[d] - v[a]);
                if (Vector3.Dot(f, n[a] + n[b] + n[d]) < 0) { t[i + 1] = d; t[i + 2] = b; }
            }
            var mesh = new Mesh { name = "WorldMapToken" };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetColors(c);
            mesh.SetUVs(0, v.ConvertAll(p => new Vector2(p.x / (Radius * 2) + .5f, p.z / (Radius * 2) + .5f)));
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        delegate Vector3 Vector3Func(Vector3 dir);
    }

    // Cartoes verticais com pivo no pe, uma malha por (sprite, altura). Alpha clip fica no shader.
    public static class WorldMapCards
    {
        static readonly Dictionary<(Sprite, int), Mesh> Cache = new Dictionary<(Sprite, int), Mesh>();
        static readonly Dictionary<(Sprite,Sprite,int),Mesh> OverlayCache=new Dictionary<(Sprite,Sprite,int),Mesh>();
        public static Vector3 ImagePoint(Sprite source,float height,Vector2 point)
        {
            var bounds=VisibleBounds(source);var scale=height/Mathf.Max(.001f,bounds.height);
            var reference=(point*source.rect.size-source.pivot)/source.pixelsPerUnit;
            return new Vector3((reference.x-bounds.center.x)*scale,(reference.y-bounds.yMin)*scale,0);
        }

        // Register a transparent decoration to the original image frame, rather than centering
        // its trimmed alpha bounds. The card parent supplies mirroring, sinking and animation.
        public static Mesh RegisteredOverlay(Sprite overlay,Sprite source,float height)
        {
            var key=(overlay,source,Mathf.RoundToInt(height*100));
            if(OverlayCache.TryGetValue(key,out var cached)&&cached!=null)return cached;
            var bounds=VisibleBounds(source);var scale=height/Mathf.Max(.001f,bounds.height);
            var points=overlay.vertices;var vertices=new Vector3[points.Length];
            for(var i=0;i<points.Length;i++)
            {
                var uv=(points[i]*overlay.pixelsPerUnit+overlay.pivot)/overlay.rect.size;
                var reference=(uv*source.rect.size-source.pivot)/source.pixelsPerUnit;
                vertices[i]=new Vector3((reference.x-bounds.center.x)*scale,(reference.y-bounds.yMin)*scale,0);
            }
            var mesh=new Mesh{name="RegisteredSnow_"+source.name,vertices=vertices,uv=overlay.uv,
                normals=System.Array.ConvertAll(points,_=>Vector3.back),triangles=System.Array.ConvertAll(overlay.triangles,t=>(int)t)};
            mesh.uv2=System.Array.ConvertAll(vertices,v=>new Vector2(0,v.y/Mathf.Max(.001f,height)));
            mesh.RecalculateBounds();OverlayCache[key]=mesh;return mesh;
        }

        // bottomPivot: ignora o pivo do sprite e apoia a base no chao (retratos com pivo no centro).
        public static Mesh Get(Sprite sprite, float height, bool bottomPivot = false)
        {
            if (sprite == null) return null;
            var key = (sprite, Mathf.RoundToInt(height * 100) * (bottomPivot ? -1 : 1) - (bottomPivot ? 1 : 0));
            if (Cache.TryGetValue(key, out var mesh) && mesh != null) return mesh;
            var bounds = VisibleBounds(sprite);
            var scale = height / Mathf.Max(0.001f, bounds.height);
            var points = sprite.vertices;
            var vertices = new Vector3[points.Length];
            for (var i = 0; i < points.Length; i++)
                vertices[i] = new Vector3((points[i].x - bounds.center.x) * scale, (points[i].y - bounds.yMin) * scale, 0);
            mesh = new Mesh { name = "Card_" + sprite.name };
            mesh.vertices = vertices;
            mesh.uv = sprite.uv; // UVs do sub-sprite; nunca 0..1 do atlas inteiro.
            mesh.uv2 = System.Array.ConvertAll(vertices,v=>new Vector2(0,v.y/Mathf.Max(.001f,height)));
            mesh.normals = System.Array.ConvertAll(points, _ => Vector3.back);
            mesh.triangles = System.Array.ConvertAll(sprite.triangles, v => (int)v);
            mesh.RecalculateBounds();
            Cache[key] = mesh;
            return mesh;
        }

        // Largura em metros de um cartao de uma dada altura.
        public static float Width(Sprite sprite, float height)
        {
            var b = VisibleBounds(sprite);
            return height * b.width / Mathf.Max(0.001f, b.height);
        }

        public static Rect VisibleBounds(Sprite sprite)
        {
            var p = sprite.vertices;
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var v in p) { min = Vector2.Min(min, v); max = Vector2.Max(max, v); }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        public static void Clear()
        {
            foreach (var mesh in Cache.Values)
                if (mesh != null) Object.DestroyImmediate(mesh);
            Cache.Clear();
            foreach(var mesh in OverlayCache.Values)if(mesh!=null)Object.DestroyImmediate(mesh);
            OverlayCache.Clear();
        }

        // Elipse horizontal (plano XZ, normal +Y) para a sombra de contato.
        public static Mesh ShadowQuad()
        {
            var mesh = new Mesh { name = "WorldMapShadow" };
            mesh.SetVertices(new[] { new Vector3(-1, 0, -1), new Vector3(-1, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 0, -1) });
            mesh.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) });
            mesh.SetNormals(new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
