using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalAves.Meta.WorldMap
{
    // Small pooled decorations, local to each tree card. No frame swapping or moving trunk.
    public sealed class WorldMapTreeDecor
    {
        public struct LeafPose
        {
            public Vector3 position,scale;
            public float angle,alpha;
        }
        readonly GameObject root;
        readonly Transform[] hangers=new Transform[2];
        public const int LeafCount=5;
        public const float LeafPeriod=6.4f,LeafLifetime=3.4f,LeafRelativeSize=.13f;
        readonly MeshFilter[] letters=new MeshFilter[2],leaves=new MeshFilter[LeafCount];
        readonly MeshRenderer[] letterRenderers=new MeshRenderer[2],leafRenderers=new MeshRenderer[LeafCount];
        readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
        readonly Func<Texture,Material> cardMaterial;
        readonly Material leafMaterial;

        public WorldMapTreeDecor(Transform parent,int layer,Func<Texture,Material> cardMaterial,Material leafMaterial)
        {
            this.cardMaterial=cardMaterial;this.leafMaterial=leafMaterial;
            root=new GameObject("EnfeitesDaArvore"){layer=layer};root.transform.SetParent(parent,false);
            for(int i=0;i<2;i++)
            {
                hangers[i]=new GameObject("CartaPendurada_"+i){layer=layer}.transform;hangers[i].SetParent(root.transform,false);
                letters[i]=Card(hangers[i],"Envelope",layer,out letterRenderers[i]);
            }
            for(int i=0;i<leaves.Length;i++)leaves[i]=Card(root.transform,"FolhaSolta_"+i,layer,out leafRenderers[i]);
        }
        static MeshFilter Card(Transform parent,string name,int layer,out MeshRenderer renderer)
        {
            var go=new GameObject(name){layer=layer};go.transform.SetParent(parent,false);
            renderer=go.AddComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            return go.AddComponent<MeshFilter>();
        }
        public void SetActive(bool active)=>root.SetActive(active);

        // Different seeds and intermittent lifetimes prevent a synchronized curtain of leaves.
        public static LeafPose LeafAt(int seed,int index,float time,float height,float width,float lateral=-1)
        {
            float age=Mathf.Repeat(time+Mathf.Repeat(seed*.413f,LeafPeriod)+index*(LeafPeriod/LeafCount),LeafPeriod);
            float u=Mathf.Clamp01(age/LeafLifetime),side=lateral<0?1:-1;
            float opacity=Mathf.SmoothStep(0,1,u/.12f)*(1-Mathf.SmoothStep(0,1,(u-.70f)/.30f));
            bool upper=(index&1)==0;
            float arc=Mathf.Sin(u*Mathf.PI)*(upper?.05f:-.045f);
            float x=width*.06f+u*(width*.34f+height*.18f);
            float y=height*((upper?.73f:.61f)-u*.16f-u*u*.04f+arc);
            return new LeafPose
            {
                // Two short downward arcs start inside the middle of the canopy, toward the road.
                position=new Vector3(side*x,y,-.035f-index*.002f),
                scale=new Vector3(Mathf.Cos(u*7+index)*.42f+.58f,.9f,1),
                angle=side*(-25+u*135+Mathf.Sin(u*9)*18),
                alpha=age<LeafLifetime?opacity:0
            };
        }
        public void Tick(WorldMapKit kit,PropPlacement p,Sprite tree,float time,bool suppressLeaves)
        {
            root.SetActive(true);
            float height=p.height,width=WorldMapCards.Width(tree,height),letterHeight=height*.205f;
            int variant=Array.IndexOf(kit.postalTrees,tree);
            int entrance=Array.IndexOf(kit.entranceTrees,tree);
            int expressive=Array.IndexOf(kit.expressiveTrees,tree);
            if(entrance>=0){suppressLeaves=true;if(entrance==1)variant=2;}
            for(int i=0;i<2;i++)
            {
                var sprite=kit.postalTreeDetails[2+i];
                hangers[i].gameObject.SetActive(sprite!=null);
                if(sprite==null)continue;
                letters[i].sharedMesh=WorldMapCards.Get(sprite,letterHeight);
                letterRenderers[i].sharedMaterial=cardMaterial(sprite.texture);
                block.Clear();block.SetFloat("_GroundedFoot",0);block.SetFloat("_Emission",.04f);letterRenderers[i].SetPropertyBlock(block);
                float x=width*(i==0?-.22f:.23f);
                float y=height*(i==0?.53f:.61f);
                if(variant==1)y=height*(i==0?.47f:.46f);
                if(variant==2)y=height*(i==0?.56f:.43f);
                if(expressive==3)y=height*(i==0?.78f:.82f);
                hangers[i].localPosition=new Vector3(x,y,-.024f-i*.002f);
                hangers[i].localRotation=Quaternion.Euler(0,0,(i==0?-6:6)+Mathf.Sin(time*1.35f+p.seed*.71f+i)*5);
                // Rotate at the cord knot, not around the envelope's center.
                letters[i].transform.localPosition=new Vector3(0,-letterHeight,0);
            }
            for(int i=0;i<leaves.Length;i++)
            {
                var pose=LeafAt(p.seed,i,time,height,width,p.offset);
                bool visible=!suppressLeaves&&pose.alpha>.005f;
                leaves[i].gameObject.SetActive(visible);
                if(!visible)continue;
                leaves[i].sharedMesh=WorldMapCards.Get(kit.postalTreeDetails[i%2],height*LeafRelativeSize);
                leafRenderers[i].sharedMaterial=leafMaterial;
                leaves[i].transform.localPosition=pose.position;
                leaves[i].transform.localRotation=Quaternion.Euler(0,0,pose.angle);
                leaves[i].transform.localScale=pose.scale;
                block.Clear();block.SetColor("_Tint",new Color(1,1,1,pose.alpha));leafRenderers[i].SetPropertyBlock(block);
            }
        }
    }
}
