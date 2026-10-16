using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalAves.Meta.WorldMap
{
    public sealed class WorldMapMailboxRig : ScriptableObject
    {
        public Sprite source,backing;
    }

    // Independent hinges: neither the painted barrel nor its support is animated.
    public sealed class WorldMapMailbox : IDisposable
    {
        static WorldMapMailboxRig profile;
        static WorldMapMailboxRig Profile=>profile!=null?profile:profile=Resources.Load<WorldMapMailboxRig>("WorldMapMailboxAnimation");
        public static bool Supports(Sprite sprite)=>sprite!=null&&Profile!=null&&sprite==Profile.source&&Profile.backing!=null;
        readonly GameObject root;
        readonly Transform flag,door;
        readonly Mesh flagMesh,doorMesh;
        static readonly Vector3 DoorAxis=new Vector3(322,-63,0).normalized;
        public bool Active=>root!=null&&root.activeInHierarchy;
        public WorldMapMailbox(Transform parent,int layer,Func<Texture,Material> material)
        {
            var p=Profile;
            root=new GameObject("CaixaCorreioAnimada"){layer=layer};root.transform.SetParent(parent,false);
            Add("CorpoEPosteFixos",root.transform,WorldMapCards.RegisteredOverlay(p.backing,p.source,1),material(p.backing.texture),Vector3.zero,layer);
            flag=Hinge("Bandeirinha",new Vector2(838,560),new[]{
                new Vector2(757,145),new Vector2(782,116),new Vector2(821,104),new Vector2(925,112),new Vector2(1028,150),new Vector2(1066,184),new Vector2(1080,225),new Vector2(1074,262),new Vector2(1056,284),new Vector2(1024,298),new Vector2(942,302),new Vector2(919,321),new Vector2(900,487),new Vector2(907,525),new Vector2(903,571),new Vector2(879,608),new Vector2(844,626),new Vector2(804,621),new Vector2(772,602),new Vector2(752,571),new Vector2(749,535),new Vector2(758,472),new Vector2(765,357),new Vector2(765,255),new Vector2(756,191)
            },-.035f,material(p.source.texture),layer,out flagMesh);
            door=Hinge("Portinha",new Vector2(377,840),new[]{
                new Vector2(340,380),new Vector2(390,387),new Vector2(437,416),new Vector2(480,464),new Vector2(511,521),new Vector2(533,589),new Vector2(546,672),new Vector2(552,782),new Vector2(548,846),new Vector2(535,870),new Vector2(499,874),new Vector2(282,828),new Vector2(223,811),new Vector2(211,786),new Vector2(205,709),new Vector2(199,626),new Vector2(199,552),new Vector2(210,491),new Vector2(234,440),new Vector2(265,402),new Vector2(305,383)
            },-.025f,material(p.source.texture),layer,out doorMesh);
        }
        static void Add(string name,Transform parent,Mesh mesh,Material material,Vector3 position,int layer)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};go.transform.SetParent(parent,false);go.transform.localPosition=position;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
        }
        Transform Hinge(string name,Vector2 pivot,Vector2[] outline,float depth,Material material,int layer,out Mesh mesh)
        {
            var source=Profile.source;Vector2 ToUV(Vector2 pixel)=>new Vector2(pixel.x/1230f,1-pixel.y/1278f);
            var center=WorldMapCards.ImagePoint(source,1,ToUV(pivot));
            var go=new GameObject(name){layer=layer};go.transform.SetParent(root.transform,false);go.transform.localPosition=center+Vector3.forward*depth;
            var vertices=new Vector3[outline.Length];var uv=new Vector2[outline.Length];
            for(int i=0;i<outline.Length;i++){var point=ToUV(outline[i]);vertices[i]=WorldMapCards.ImagePoint(source,1,point)-center;uv[i]=(source.rect.position+point*source.rect.size)/new Vector2(source.texture.width,source.texture.height);}
            mesh=new Mesh{name="Recorte"+name,vertices=vertices,uv=uv,triangles=Triangulate(vertices)};
            mesh.RecalculateNormals();mesh.RecalculateBounds();Add("Arte"+name,go.transform,mesh,material,Vector3.zero,layer);return go.transform;
        }
        static float Cross(Vector3 a,Vector3 b)=>a.x*b.y-a.y*b.x;
        static int[] Triangulate(Vector3[] v)
        {
            var indices=new List<int>();var result=new List<int>();float area=0;
            for(int i=0;i<v.Length;i++){indices.Add(i);area+=Cross(v[i],v[(i+1)%v.Length]);}
            float sign=area>0?1:-1;int guard=v.Length*v.Length;
            while(indices.Count>2&&guard-->0)
            {
                bool found=false;
                for(int k=0;k<indices.Count;k++)
                {
                    int a=indices[(k+indices.Count-1)%indices.Count],b=indices[k],c=indices[(k+1)%indices.Count];
                    if(Cross(v[b]-v[a],v[c]-v[b])*sign<=0)continue;
                    bool inside=false;foreach(int j in indices){if(j==a||j==b||j==c)continue;var p=v[j];if(Cross(v[b]-v[a],p-v[a])*sign>=0&&Cross(v[c]-v[b],p-v[b])*sign>=0&&Cross(v[a]-v[c],p-v[c])*sign>=0){inside=true;break;}}
                    if(inside)continue;result.Add(a);result.Add(b);result.Add(c);indices.RemoveAt(k);found=true;break;
                }
                if(!found)throw new InvalidOperationException("Mailbox outline could not be triangulated");
            }
            return result.ToArray();
        }
        static float Ease(float from,float to,float t)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(from,to,t));
        public void Tick(float height,float time,int seed)
        {
            root.SetActive(true);root.transform.localScale=Vector3.one*height;
            float t=Mathf.Repeat(time+seed*.31f,7);
            float signal=Ease(.15f,.65f,t)*(1-Ease(.95f,1.5f,t));
            float opening=Ease(2,2.9f,t)*(1-Ease(3.7f,4.6f,t));
            flag.localRotation=Quaternion.Euler(0,0,-55*signal);
            door.localRotation=Quaternion.AngleAxis(-100*opening,DoorAxis);
        }
        public void SetActive(bool active)=>root.SetActive(active);
        public void Dispose(){Kill(flagMesh);Kill(doorMesh);Kill(root);}
        static void Kill(UnityEngine.Object value){if(value==null)return;if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
    }
}
