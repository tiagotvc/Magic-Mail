using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalAves.Meta.WorldMap
{
    // Keep the approved illustration intact. A continuous mesh adds only restrained idle motion.
    public sealed class WorldMapPostman : IDisposable
    {
        const int Columns=64,Rows=96;
        readonly GameObject root;
        readonly Mesh mesh;
        readonly Vector3[] rest,animated;
        readonly WorldMapForearm[] arms;
        readonly Vector3[] elbows;
        readonly float[][] weights;
        readonly Quaternion[] rotations;
        readonly System.Collections.Generic.List<Mesh> limbMeshes=new System.Collections.Generic.List<Mesh>();
        Transform[] joints=Array.Empty<Transform>();
        float actorHeight=1,actorWidth;
        public bool Articulated=>joints.Length>0;
        public Sprite Source {get;private set;}
        public bool Active=>root!=null&&root.activeInHierarchy;
        public Bounds LocalBounds=>Articulated?new Bounds(new Vector3(0,.5f,0)*actorHeight,new Vector3(actorWidth+.1f,1.10f,.05f)*actorHeight):mesh.bounds;
        public Transform Actor=>root.transform;

        public WorldMapPostman(Transform parent,int layer,WorldMapKit kit,Func<Texture,Material> material,Sprite source=null)
        {
            root=new GameObject("CarteiroAprovado",typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};
            root.transform.SetParent(parent,false);
            var sprite=source??kit.postmanSprite;Source=sprite;var bounds=WorldMapCards.VisibleBounds(sprite);
            arms=WorldMapMission100.Character(sprite)?.forearms??new[]{new WorldMapForearm{region=new Rect(.55f,.60f,.44f,.13f),heldItem=new Rect(.83f,.64f,.16f,.30f),elbow=new Vector2(.55f,.67f),degrees=1.8f}};
            elbows=new Vector3[arms.Length];weights=new float[arms.Length][];rotations=new Quaternion[arms.Length];
            var profile=WorldMapMission100.Character(sprite);
            actorWidth=bounds.width/Mathf.Max(.001f,bounds.height);
            if(profile?.bodyBacking!=null&&profile.forearmSprite!=null)
            {
                rest=animated=Array.Empty<Vector3>();
                mesh=UnityEngine.Object.Instantiate(WorldMapCards.RegisteredOverlay(profile.bodyBacking,sprite,1));
                root.GetComponent<MeshFilter>().sharedMesh=mesh;root.GetComponent<MeshRenderer>().sharedMaterial=material(profile.bodyBacking.texture);
                root.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;root.GetComponent<MeshRenderer>().receiveShadows=false;
                joints=new Transform[arms.Length];
                var sourceMesh=WorldMapCards.RegisteredOverlay(profile.forearmSprite,sprite,1);
                for(int a=0;a<arms.Length;a++)
                {
                    weights[a]=Array.Empty<float>();elbows[a]=WorldMapCards.ImagePoint(sprite,1,arms[a].elbow);
                    var joint=new GameObject("ForearmJoint_"+a){layer=layer};joint.transform.SetParent(root.transform,false);joint.transform.localPosition=elbows[a];joints[a]=joint.transform;
                    var piece=new GameObject("ForearmAndTool",typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};piece.transform.SetParent(joint.transform,false);
                    var scale=arms[a].sourceScale.sqrMagnitude>0?arms[a].sourceScale:Vector2.one;
                    var sourceElbow=WorldMapCards.ImagePoint(sprite,1,arms[a].sourceElbow.sqrMagnitude>0?arms[a].sourceElbow:arms[a].elbow);
                    piece.transform.localScale=new Vector3(scale.x,scale.y,1);piece.transform.localPosition=-Vector3.Scale(sourceElbow,piece.transform.localScale)+Vector3.back*.004f;
                    var region=arms[a].region;var art=profile.forearmSprite;var r=art.rect;var t=art.texture;
                    var clip=new Rect((r.x+region.x*r.width)/t.width,(r.y+region.y*r.height)/t.height,region.width*r.width/t.width,region.height*r.height/t.height);
                    var limb=WorldMapSeparatedSnow.Clip(sourceMesh,clip);limbMeshes.Add(limb);piece.GetComponent<MeshFilter>().sharedMesh=limb;piece.GetComponent<MeshRenderer>().sharedMaterial=material(art.texture);
                    piece.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;piece.GetComponent<MeshRenderer>().receiveShadows=false;
                }
                return;
            }
            for(int a=0;a<arms.Length;a++){elbows[a]=WorldMapCards.ImagePoint(sprite,1,arms[a].elbow);weights[a]=new float[(Columns+1)*(Rows+1)];}
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
                var frame=new Vector2((sx*sprite.pixelsPerUnit+sprite.pivot.x)/sprite.rect.width,(sy*sprite.pixelsPerUnit+sprite.pivot.y)/sprite.rect.height);
                for(int a=0;a<arms.Length;a++)
                {
                    float Mask(Rect r){if(!r.Contains(frame))return 0;float edge=Mathf.Min(Mathf.Min(frame.x-r.xMin,r.xMax-frame.x),Mathf.Min(frame.y-r.yMin,r.yMax-frame.y));return Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/.012f));}
                    weights[a][i]=Mathf.Max(Mask(arms[a].region),Mask(arms[a].heldItem))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.01f,.12f,Vector2.Distance(frame,arms[a].elbow)));
                }
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
            float phase=time+seed*.31f;
            for(int a=0;a<arms.Length;a++)rotations[a]=Quaternion.Euler(0,0,Mathf.Sin(phase*(arms[a].speed>0?arms[a].speed:1.15f)+a*.85f+arms[a].phase)*arms[a].degrees);
            if(Articulated){actorHeight=height;root.transform.localScale=Vector3.one*height;for(int a=0;a<joints.Length;a++)joints[a].localRotation=rotations[a];return;}
            for(int i=0;i<rest.Length;i++)
            {
                var p=rest[i];
                for(int a=0;a<arms.Length;a++)if(weights[a][i]>0)p+=(rotations[a]*(rest[i]-elbows[a])+elbows[a]-rest[i])*weights[a][i];
                animated[i]=p*height;
            }
            mesh.vertices=animated;mesh.RecalculateBounds();
        }
        public void Dispose(){foreach(var limb in limbMeshes)Kill(limb);Kill(mesh);Kill(root);}
        static void Kill(UnityEngine.Object value)
        {
            if(value==null)return;
            if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
