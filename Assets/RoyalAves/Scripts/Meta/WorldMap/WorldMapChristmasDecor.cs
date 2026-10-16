using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalAves.Meta.WorldMap
{
    // Decorations inherit the real tree card's mirror, depth and sway. Existing bulb art is reused.
    public sealed class WorldMapChristmasDecor : IDisposable
    {
        readonly GameObject root;
        readonly Transform lightsRoot,iceRoot;
        readonly Func<Texture,Material> material;
        readonly List<GameObject> pieces=new List<GameObject>();
        readonly List<MeshRenderer> lights=new List<MeshRenderer>();
        readonly List<MeshRenderer> halos=new List<MeshRenderer>();
        Material glowMaterial;
        readonly List<MeshRenderer> iceDrawings=new List<MeshRenderer>();
        readonly List<Mesh> ownedMeshes=new List<Mesh>();
        readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
        Sprite source,ice,bulb;float height;
        public bool Active=>root!=null&&root.activeInHierarchy&&(lightsRoot.gameObject.activeSelf||iceRoot.gameObject.activeSelf);
        public WorldMapChristmasDecor(Transform parent,int layer,Func<Texture,Material> material)
        {
            this.material=material;root=new GameObject("NatalLuzesEGelo"){layer=layer};root.transform.SetParent(parent,false);
            root.transform.localPosition=new Vector3(0,0,-.025f);
            lightsRoot=new GameObject("LuzesDoObjeto"){layer=layer}.transform;lightsRoot.SetParent(root.transform,false);
            iceRoot=new GameObject("GeloDoObjeto"){layer=layer}.transform;iceRoot.SetParent(root.transform,false);
        }
        public void SetActive(bool active)=>root.SetActive(active);
        public void Tick(WorldMapKit kit,Sprite sprite,float size,float time,WorldMapEffectPose lightPose,WorldMapEffectPose icePose,bool tree,float night=0,bool shrub=false,WorldMapPiecePose[] piecePoses=null)
        {
            var first=kit.christmasBulbs.Length>0?kit.christmasBulbs[0]:null;
            root.SetActive(first!=null||kit.christmasIcicles!=null);
            if(sprite!=source||height!=size||ice!=kit.christmasIcicles||first!=bulb)
            {
                Clear();source=sprite;height=size;ice=kit.christmasIcicles;bulb=first;Build(kit);
            }
            lightPose.Apply(lightsRoot,size,new Vector2(0,.60f),(tree||shrub)&&first!=null);
            icePose.Apply(iceRoot,size,new Vector2(0,.28f),tree&&ice!=null);
            foreach(var drawing in iceDrawings)drawing.GetComponent<WorldMapEffectPiece>().Apply(piecePoses,size);
            for(var i=0;i<lights.Count;i++)
            {
                var pulse=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.45f,.65f,Mathf.Sin(time*3.2f+i*1.7f)));
                var brightness=Mathf.Lerp(.18f+night*.12f,1,pulse);
                block.Clear();block.SetColor("_Tint",new Color(brightness,brightness,brightness,1));block.SetFloat("_Emission",pulse*.55f+night*(.4f+pulse*2.2f));
                lights[i].SetPropertyBlock(block);
                if(i<halos.Count)
                {
                    var tint=i%3==0?new Color(1,.68f,.12f):i%3==1?new Color(1,.12f,.05f):new Color(.08f,1,.75f);
                    tint.a=night*(.10f+pulse*.65f);block.Clear();block.SetColor("_Color",tint);halos[i].SetPropertyBlock(block);
                }
            }
            foreach(var drawing in iceDrawings)
            {
                var r=ice.rect;var t=ice.texture;block.Clear();block.SetFloat("_IceGlint",1);block.SetFloat("_IceTime",time);
                block.SetVector("_IceRect",new Vector4(r.x/t.width,r.y/t.height,r.width/t.width,r.height/t.height));drawing.SetPropertyBlock(block);
            }
        }
        MeshFilter Piece(string name,Mesh mesh,Texture texture,out MeshRenderer renderer)
        {
            var go=new GameObject(name){layer=root.layer};go.transform.SetParent(lightsRoot,false);pieces.Add(go);
            var filter=go.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
            renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material(texture);
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return filter;
        }
        void Build(WorldMapKit kit)
        {
            if(glowMaterial==null&&kit.glow!=null)glowMaterial=new Material(kit.glow){hideFlags=HideFlags.HideAndDontSave};
            var shape=WorldMapCards.Get(source,height,true);var vertices=shape.vertices;var indices=shape.triangles;
            bool pine=source.name.IndexOf("pine",StringComparison.OrdinalIgnoreCase)>=0||source.name.IndexOf("pinheiro",StringComparison.OrdinalIgnoreCase)>=0;
            int rows=pine?2:1;
            if(bulb!=null)for(var row=0;row<rows;row++)
            {
                float y=height*(row==0?.64f:.42f);var span=Horizontal(vertices,indices,y);
                float left=span.x*.78f,right=span.y*.78f;
                if(right-left<height*.15f)continue;
                float sag=height*.065f;
                var points=new Vector3[25];
                for(var i=0;i<points.Length;i++){var t=i/(float)(points.Length-1);points[i]=new Vector3(Mathf.Lerp(left,right,t),y-sag*4*t*(1-t),0);}
                var cord=Ribbon(points,height*.005f);ownedMeshes.Add(cord);
                Piece("FioDeNatal_"+row,cord,Texture2D.whiteTexture,out var line);
                block.Clear();block.SetColor("_Tint",new Color(.08f,.19f,.14f));line.SetPropertyBlock(block);
                int count=pine?5:7;
                for(var i=0;i<count;i++)
                {
                    var t=(i+.5f)/count;var art=kit.christmasBulbs[(i+row)%kit.christmasBulbs.Length];if(art==null)continue;
                    var light=Piece("LuzDeNatal_"+row+"_"+i,WorldMapCards.Get(art,height*.073f,true),art.texture,out var drawing);
                    // Existing art points upward with its socket at the bottom; hang it from the socket.
                    light.transform.localRotation=Quaternion.Euler(0,0,180);
                    light.transform.localPosition=new Vector3(Mathf.Lerp(left,right,t),y-sag*4*t*(1-t),-.003f);
                    lights.Add(drawing);
                    if(glowMaterial!=null)
                    {
                        var halo=Piece("HaloDeNatal_"+row+"_"+i,WorldMapWindowLight.Quad,Texture2D.whiteTexture,out var haloDrawing);
                        haloDrawing.sharedMaterial=glowMaterial;halo.transform.localPosition=light.transform.localPosition+new Vector3(0,-height*.035f,-.018f);
                        halo.transform.localScale=Vector3.one*height*.23f;halos.Add(haloDrawing);
                    }
                }
            }
            if(ice!=null)for(var side=-1;side<=1;side+=2)
            {
                float x=shape.bounds.size.x*.27f*side;
                float bottom=Bottom(vertices,indices,x)+height*.04f;
                if(float.IsInfinity(bottom))continue;
                if(kit.treeDecorationProfiles!=null)foreach(var profile in kit.treeDecorationProfiles)
                {
                    if(profile==null||profile.source!=source)continue;
                    var point=WorldMapCards.ImagePoint(source,height,side<0?profile.leftIce:profile.rightIce);
                    x=point.x;bottom=point.y;break;
                }
                float size=height*.115f;
                var original=WorldMapCards.Get(ice,size,true);var region=ice.rect;var texture=ice.texture;
                for(int part=0;part<3;part++)
                {
                    var clip=new Rect((region.x+region.width*part/3f)/texture.width,region.y/texture.height,region.width/(3*texture.width),region.height/texture.height);
                    var iceMesh=WorldMapSeparatedSnow.Clip(original,clip);var center=iceMesh.bounds.center;var points=iceMesh.vertices;
                    for(int i=0;i<points.Length;i++)points[i]-=center;iceMesh.vertices=points;iceMesh.RecalculateBounds();ownedMeshes.Add(iceMesh);
                    var cluster=Piece("GeloGalhoBaixo_"+side+"_"+part,iceMesh,ice.texture,out var drawing);
                    cluster.transform.SetParent(iceRoot,false);iceDrawings.Add(drawing);
                    cluster.transform.localPosition=new Vector3(x,bottom-size*.94f,-.006f)+center;
                    cluster.gameObject.AddComponent<WorldMapEffectPiece>();
                }
            }
            foreach(var piece in pieces)piece.transform.localPosition-=Vector3.up*height*(piece.transform.parent==lightsRoot ? .60f : .28f);
            foreach(var drawing in iceDrawings)
            {
                bool left=drawing.name.Contains("_-1_");var part=drawing.name.Substring(drawing.name.LastIndexOf('_')+1);
                drawing.GetComponent<WorldMapEffectPiece>().Init((left?"ice-left-":"ice-right-")+part,(left?"Gelo esquerdo ":"Gelo direito ")+(int.Parse(part)+1));
            }
        }
        static Vector2 Horizontal(Vector3[] v,int[] indices,float y)
        {
            float left=float.PositiveInfinity,right=float.NegativeInfinity;
            for(var i=0;i<indices.Length;i+=3)for(var e=0;e<3;e++)
            {
                var a=v[indices[i+e]];var b=v[indices[i+(e+1)%3]];
                if(Mathf.Abs(a.y-b.y)<.00001f||y<Mathf.Min(a.y,b.y)||y>Mathf.Max(a.y,b.y))continue;
                var x=Mathf.Lerp(a.x,b.x,(y-a.y)/(b.y-a.y));left=Mathf.Min(left,x);right=Mathf.Max(right,x);
            }
            return new Vector2(left,right);
        }
        static float Bottom(Vector3[] v,int[] indices,float x)
        {
            float bottom=float.PositiveInfinity;
            for(var i=0;i<indices.Length;i+=3)for(var e=0;e<3;e++)
            {
                var a=v[indices[i+e]];var b=v[indices[i+(e+1)%3]];
                if(Mathf.Abs(a.x-b.x)<.00001f||x<Mathf.Min(a.x,b.x)||x>Mathf.Max(a.x,b.x))continue;
                bottom=Mathf.Min(bottom,Mathf.Lerp(a.y,b.y,(x-a.x)/(b.x-a.x)));
            }
            return bottom;
        }
        static Mesh Ribbon(Vector3[] p,float width)
        {
            var v=new Vector3[p.Length*2];var uv=new Vector2[v.Length];var triangles=new int[(p.Length-1)*6];
            for(var i=0;i<p.Length;i++)
            {
                v[i*2]=p[i]+Vector3.up*width*.5f;v[i*2+1]=p[i]-Vector3.up*width*.5f;uv[i*2]=uv[i*2+1]=Vector2.one*.5f;
                if(i==p.Length-1)continue;var t=i*6;triangles[t]=i*2;triangles[t+1]=i*2+2;triangles[t+2]=i*2+1;
                triangles[t+3]=i*2+1;triangles[t+4]=i*2+2;triangles[t+5]=i*2+3;
            }
            var mesh=new Mesh{name="FioNatalCurvo",vertices=v,uv=uv,triangles=triangles};mesh.RecalculateBounds();return mesh;
        }
        static void Destroy(UnityEngine.Object obj){if(Application.isPlaying)UnityEngine.Object.Destroy(obj);else UnityEngine.Object.DestroyImmediate(obj);}
        void Clear(){foreach(var piece in pieces)Destroy(piece);pieces.Clear();lights.Clear();halos.Clear();iceDrawings.Clear();foreach(var mesh in ownedMeshes)Destroy(mesh);ownedMeshes.Clear();}
        public void Dispose(){Clear();if(glowMaterial!=null)Destroy(glowMaterial);if(root!=null)Destroy(root);}
    }
}
