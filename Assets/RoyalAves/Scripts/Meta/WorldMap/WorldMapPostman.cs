using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalAves.Meta.WorldMap
{
    // Keep the approved illustration intact. A continuous mesh adds only restrained idle motion.
    public sealed class WorldMapPostman : IDisposable
    {
        const int Columns=32,Rows=48;
        readonly GameObject root;
        readonly Mesh mesh;
        readonly Vector3[] rest,animated;
        public bool Active=>root!=null&&root.activeInHierarchy;
        public Bounds LocalBounds=>mesh.bounds;
        public Transform Actor=>root.transform;

        public WorldMapPostman(Transform parent,int layer,WorldMapKit kit,Func<Texture,Material> material)
        {
            root=new GameObject("CarteiroAprovado",typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};
            root.transform.SetParent(parent,false);
            var sprite=kit.postmanSprite;var bounds=WorldMapCards.VisibleBounds(sprite);
            float height=Mathf.Max(.001f,bounds.height),width=bounds.width/height;
            rest=new Vector3[(Columns+1)*(Rows+1)];animated=new Vector3[rest.Length];
            var uv=new Vector2[rest.Length];var normals=new Vector3[rest.Length];var triangles=new int[Columns*Rows*6];
            for(int y=0;y<=Rows;y++)for(int x=0;x<=Columns;x++)
            {
                int i=y*(Columns+1)+x;float u=x/(float)Columns,v=y/(float)Rows;
                rest[i]=new Vector3((u-.5f)*width,v,-.025f);
                float sx=Mathf.Lerp(bounds.xMin,bounds.xMax,u),sy=Mathf.Lerp(bounds.yMin,bounds.yMax,v);
                uv[i]=new Vector2((sx*sprite.pixelsPerUnit+sprite.pivot.x+sprite.rect.x)/sprite.texture.width,
                    (sy*sprite.pixelsPerUnit+sprite.pivot.y+sprite.rect.y)/sprite.texture.height);
                normals[i]=Vector3.back;
            }
            for(int y=0;y<Rows;y++)for(int x=0;x<Columns;x++)
            {
                int i=y*(Columns+1)+x,k=(y*Columns+x)*6;
                triangles[k]=i;triangles[k+1]=i+Columns+1;triangles[k+2]=i+1;
                triangles[k+3]=i+1;triangles[k+4]=i+Columns+1;triangles[k+5]=i+Columns+2;
            }
            mesh=new Mesh{name="ApprovedPostmanContinuous",hideFlags=HideFlags.HideAndDontSave};
            mesh.MarkDynamic();mesh.vertices=rest;mesh.uv=uv;mesh.normals=normals;mesh.triangles=triangles;mesh.RecalculateBounds();
            root.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=root.GetComponent<MeshRenderer>();renderer.sharedMaterial=material(sprite.texture);
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            var block=new MaterialPropertyBlock();block.SetFloat("_GroundedFoot",0);block.SetFloat("_Emission",.03f);renderer.SetPropertyBlock(block);
        }
        public void SetActive(bool active)=>root.SetActive(active);
        public void Tick(float height,float time,int seed)
        {
            root.SetActive(true);
            float phase=time+seed*.31f,breath=Mathf.Sin(phase*1.7f),sway=Mathf.Sin(phase*.85f);
            for(int i=0;i<rest.Length;i++)
            {
                var p=rest[i];float u=(i%(Columns+1))/(float)Columns,v=p.y;
                float anchored=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.20f,.75f,v));
                p.x+=sway*.006f*anchored;p.y+=breath*.005f*anchored;
                // Soft cloth movement without cutting apart the original coat/scarf silhouette.
                float cloth=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.40f,.60f,u))*
                    Mathf.SmoothStep(0,1,Mathf.InverseLerp(.57f,.67f,v))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.76f,.84f,v)));
                p.x+=Mathf.Sin(phase*1.8f+u*5)*.005f*cloth;
                p.y+=Mathf.Sin(phase*1.3f)*.006f*cloth;
                animated[i]=p*height;
            }
            mesh.vertices=animated;mesh.RecalculateBounds();
        }
        public void Dispose(){Kill(mesh);Kill(root);}
        static void Kill(UnityEngine.Object value)
        {
            if(value==null)return;
            if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
