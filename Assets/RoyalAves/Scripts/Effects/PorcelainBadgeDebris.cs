using UnityEngine;

namespace RoyalAves.Effects
{
    // One large front-wall fragment, sampled at the ORIGINAL texel scale: the crown never shrinks.
    public sealed class PorcelainBadgeDebris : MonoBehaviour
    {
        Mesh frontMesh, coreMesh;
        Material frontMaterial, coreMaterial;
        MeshRenderer front, core;
        Vector3 origin, initialScale;
        Quaternion initialRotation;
        float age, worldUnit;
        public bool KeepsOriginalScale => transform.localScale == initialScale;
        public float Age => age;
        public static PorcelainBadgeDebris Play(SpriteRenderer source, Sprite intact)
        {
            var root=new GameObject("InkwellCrownWallFragment");root.layer=source.gameObject.layer;
            var field=source.GetComponentInParent<SweetSugar.Scripts.Level.FieldBoard>();
            root.transform.SetParent(field!=null?field.transform:source.transform.parent,false);
            Vector2 center=new Vector2(287.5f,320);
            Vector3 localCenter=new Vector3((center.x-intact.pivot.x)/intact.pixelsPerUnit,
                (512-center.y-intact.pivot.y)/intact.pixelsPerUnit,0);
            root.transform.SetPositionAndRotation(source.transform.TransformPoint(localCenter),source.transform.rotation);
            var parent=root.transform.parent!=null?root.transform.parent.lossyScale:Vector3.one;
            root.transform.localScale=new Vector3(source.transform.lossyScale.x/parent.x,source.transform.lossyScale.y/parent.y,1);
            var effect=root.AddComponent<PorcelainBadgeDebris>();
            effect.origin=root.transform.position;effect.initialScale=root.transform.localScale;effect.initialRotation=root.transform.rotation;
            effect.worldUnit=Mathf.Max(source.transform.lossyScale.x,source.transform.lossyScale.y);
            effect.Build(intact,center);return effect;
        }
        void Build(Sprite sprite,Vector2 center)
        {
            Vector2[] border={new Vector2(216,310),new Vector2(236,268),new Vector2(288,255),
                new Vector2(340,275),new Vector2(365,320),new Vector2(350,376),new Vector2(231,376)};
            int count=border.Length;
            var points=new Vector3[count+1];var uv=new Vector2[count+1];var colors=new Color[count+1];var triangles=new int[count*3];
            void Set(int i,Vector2 pixel)
            {
                points[i]=new Vector3(pixel.x-center.x,center.y-pixel.y)/sprite.pixelsPerUnit;
                uv[i]=new Vector2((sprite.rect.x+pixel.x)/sprite.texture.width,(sprite.rect.y+512-pixel.y)/sprite.texture.height);
                colors[i]=Color.white;
            }
            Set(0,center);
            for(int i=0;i<count;i++){Set(i+1,border[i]);triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%count+1;}
            frontMesh=new Mesh{name="CrownPorcelainFront",vertices=points,uv=uv,colors=colors,triangles=triangles};
            frontMaterial=new Material(Shader.Find("Sprites/Default")){mainTexture=sprite.texture};
            var face=new GameObject("OriginalSizeCrown");face.layer=gameObject.layer;face.transform.SetParent(transform,false);
            face.AddComponent<MeshFilter>().sharedMesh=frontMesh;
            front=face.AddComponent<MeshRenderer>();front.sharedMaterial=frontMaterial;
            front.enabled=false;
            front.sortingLayerName=PowerUpEffectOrder.SortingLayer;front.sortingOrder=PowerUpEffectOrder.Value+5;
            var corePoints=new Vector3[(count+1)*2];var coreColors=new Color[corePoints.Length];
            var coreIndices=new System.Collections.Generic.List<int>();
            for(int i=0;i<=count;i++)
            {
                corePoints[i]=points[i]+Vector3.forward*.008f;corePoints[i+count+1]=points[i]+Vector3.forward*.06f;
                coreColors[i]=new Color(.97f,.91f,.84f,1);coreColors[i+count+1]=new Color(.82f,.76f,.68f,1);
            }
            for(int i=0;i<count;i++)
            {
                int a=i+1,b=(i+1)%count+1,c=a+count+1,d=b+count+1;
                coreIndices.AddRange(new[]{0,a,b,count+1,d,c,a,c,d,a,d,b});
            }
            coreMesh=new Mesh{name="WhitePorcelainThickness",vertices=corePoints,colors=coreColors,triangles=coreIndices.ToArray()};
            coreMaterial=new Material(Shader.Find("Sprites/Default")){mainTexture=Texture2D.whiteTexture};
            gameObject.AddComponent<MeshFilter>().sharedMesh=coreMesh;
            core=gameObject.AddComponent<MeshRenderer>();core.sharedMaterial=coreMaterial;
            core.sortingLayerName=front.sortingLayerName;core.sortingOrder=front.sortingOrder-1;core.enabled=false;
        }
        void Update()
        {
            age+=Time.deltaTime;
            if(age<.025f)return;
            float t=age-.025f;core.enabled=true;
            transform.position=origin+new Vector3(-.85f*t,3.6f*t-8f*t*t,-.08f)*worldUnit;
            transform.rotation=initialRotation*Quaternion.Euler(145*t,220*t,-165*t);
            front.enabled=transform.TransformDirection(Vector3.back).z<0;
            if(age>2.3f)Destroy(gameObject);
        }
        void OnDestroy()
        {
            if(Application.isPlaying){Destroy(frontMesh);Destroy(coreMesh);Destroy(frontMaterial);Destroy(coreMaterial);}
            else{DestroyImmediate(frontMesh);DestroyImmediate(coreMesh);DestroyImmediate(frontMaterial);DestroyImmediate(coreMaterial);}
        }
    }
}
