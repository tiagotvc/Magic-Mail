using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalAves.Meta.WorldMap
{
    public sealed class WorldMapSeparatedSnow : IDisposable
    {
        [Serializable] sealed class Profiles{public Profile[] profiles;}
        [Serializable] sealed class Profile{public string name;public Vector4[] frost,painted;}
        static Dictionary<string,Profile> profiles;
        readonly Transform parent;
        readonly List<WorldMapEffectPiece> pieces=new List<WorldMapEffectPiece>();
        readonly List<Mesh> meshes=new List<Mesh>();
        public WorldMapSeparatedSnow(Transform parent){this.parent=parent;}
        static Vector4[] Regions(Sprite source,bool painted)
        {
            if(profiles==null)
            {
                profiles=new Dictionary<string,Profile>();var text=Resources.Load<TextAsset>("WorldMapSnowPieceProfiles");
                if(text!=null)foreach(var p in JsonUtility.FromJson<Profiles>(text.text).profiles){profiles[p.name]=p;profiles[p.name+"_0"]=p;}
            }
            if(profiles.TryGetValue(source.name,out var profile))
            {var regions=painted?profile.painted:profile.frost;if(regions!=null&&regions.Length>0)return regions;}
            return new[]{new Vector4(0,0,1,1)};
        }
        public void Build(Sprite source,Sprite overlay,float height,Material material,bool lamp)
        {
            Clear();bool painted=overlay!=null;var art=painted?overlay:source;
            var mesh=painted?WorldMapCards.RegisteredOverlay(overlay,source,height):WorldMapCards.Get(source,height,true);
            var regions=Regions(source,painted);var rect=art.rect;var tex=art.texture;
            for(int i=0;i<regions.Length;i++)
            {
                var region=regions[i];var center=WorldMapCards.ImagePoint(source,height,new Vector2(region.x+region.z*.5f,region.y+region.w*.5f));
                var go=new GameObject("PecaDeNeve_"+(i+1)){layer=parent.gameObject.layer};go.transform.SetParent(parent,false);
                go.transform.localPosition=center+new Vector3(0,-height*.55f,-.014f);
                var body=new GameObject("ArteDaPeca",typeof(MeshFilter),typeof(MeshRenderer)){layer=go.layer};body.transform.SetParent(go.transform,false);body.transform.localPosition=-center;
                var clipRect=new Rect((rect.x+region.x*rect.width)/tex.width,(rect.y+region.y*rect.height)/tex.height,region.z*rect.width/tex.width,region.w*rect.height/tex.height);
                var clipped=Clip(mesh,clipRect);meshes.Add(clipped);
                body.GetComponent<MeshFilter>().sharedMesh=clipped;var drawing=body.GetComponent<MeshRenderer>();drawing.sharedMaterial=material;
                drawing.shadowCastingMode=ShadowCastingMode.Off;drawing.receiveShadows=false;
                var block=new MaterialPropertyBlock();block.SetFloat("_UseEffectClip",1);
                block.SetVector("_EffectClip",new Vector4((rect.x+region.x*rect.width)/tex.width,(rect.y+region.y*rect.height)/tex.height,region.z*rect.width/tex.width,region.w*rect.height/tex.height));
                if(!painted)
                {
                    block.SetVector("_SnowRect",new Vector4(rect.x/tex.width,rect.y/tex.height,rect.width/tex.width,rect.height/tex.height));
                    block.SetFloat("_SnowDust",1);block.SetFloat("_SnowOnly",1);block.SetFloat("_SnowFloor",lamp?.84f:.22f);
                }
                drawing.SetPropertyBlock(block);var piece=go.AddComponent<WorldMapEffectPiece>();piece.Init("snow-"+i,"Neve "+(i+1));pieces.Add(piece);
            }
        }
        public void Apply(WorldMapPiecePose[] values,float height){foreach(var piece in pieces)piece.Apply(values,height);}
        struct Vertex{public Vector3 p;public Vector2 uv,foot;}
        public static Mesh Clip(Mesh original,Rect rect)
        {
            var positions=original.vertices;var uv=original.uv;var foot=original.uv2;var triangles=original.triangles;
            var v=new List<Vector3>();var u=new List<Vector2>();var f=new List<Vector2>();var indices=new List<int>();
            for(int t=0;t<triangles.Length;t+=3)
            {
                var polygon=new List<Vertex>();for(int k=0;k<3;k++){int i=triangles[t+k];polygon.Add(new Vertex{p=positions[i],uv=uv[i],foot=foot.Length>i?foot[i]:Vector2.zero});}
                for(int edge=0;edge<4&&polygon.Count>0;edge++)
                {
                    var output=new List<Vertex>();float limit=edge==0?rect.xMin:edge==1?rect.xMax:edge==2?rect.yMin:rect.yMax;
                    float Distance(Vertex a){float value=edge<2?a.uv.x:a.uv.y;return edge==0||edge==2?value-limit:limit-value;}
                    for(int k=0;k<polygon.Count;k++)
                    {
                        var a=polygon[k];var b=polygon[(k+1)%polygon.Count];float da=Distance(a),db=Distance(b);
                        if(da>=0)output.Add(a);
                        if((da>=0)!=(db>=0)){float blend=da/(da-db);output.Add(new Vertex{p=Vector3.Lerp(a.p,b.p,blend),uv=Vector2.Lerp(a.uv,b.uv,blend),foot=Vector2.Lerp(a.foot,b.foot,blend)});}
                    }
                    polygon=output;
                }
                int start=v.Count;foreach(var a in polygon){v.Add(a.p);u.Add(a.uv);f.Add(a.foot);}
                for(int k=1;k+1<polygon.Count;k++){indices.Add(start);indices.Add(start+k);indices.Add(start+k+1);}
            }
            var mesh=new Mesh{name="PecaNeveRecortada",vertices=v.ToArray(),uv=u.ToArray(),uv2=f.ToArray(),triangles=indices.ToArray()};mesh.RecalculateBounds();return mesh;
        }
        static void Kill(UnityEngine.Object obj){if(Application.isPlaying)UnityEngine.Object.Destroy(obj);else UnityEngine.Object.DestroyImmediate(obj);}
        public void Clear(){foreach(var piece in pieces)if(piece!=null){piece.gameObject.SetActive(false);Kill(piece.gameObject);}pieces.Clear();foreach(var mesh in meshes)if(mesh!=null)Kill(mesh);meshes.Clear();}
        public void Dispose()=>Clear();
    }
}
