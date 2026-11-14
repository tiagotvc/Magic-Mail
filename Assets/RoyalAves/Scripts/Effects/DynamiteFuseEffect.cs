using UnityEngine;

namespace RoyalAves.Effects
{
    // A moving flame burns away the ivory rope in the original sprite, without editing its PNG.
    public sealed class DynamiteFuseEffect : MonoBehaviour
    {
        SpriteRenderer source;
        Material rope, fire;
        SpriteRenderer[] layers;
        Material[] originals;
        Mesh quad;
        Transform flame;
        float age, duration;
        public static DynamiteFuseEffect Attach(SpriteRenderer source,float duration,SpriteRenderer[] layers=null)
        {
            if(source==null || source.sprite==null)return null;
            var asset=Resources.Load<Material>("Effects/DynamiteFuse");
            if(asset==null){Debug.LogError("Missing dynamite fuse material");return null;}
            var root=new GameObject("Dynamite burning fuse");
            root.transform.SetParent(source.transform,false);
            var effect=root.AddComponent<DynamiteFuseEffect>();
            effect.source=source;effect.duration=duration;
            effect.layers=layers??new[]{source};
            effect.originals=new Material[effect.layers.Length];
            effect.rope=new Material(asset);
            for(int i=0;i<effect.layers.Length;i++)
            {
                var layer=effect.layers[i];
                if(layer==null || layer.sprite!=source.sprite)continue;
                effect.originals[i]=layer.sharedMaterial;layer.sharedMaterial=effect.rope;
            }
            effect.fire=new Material(asset);effect.fire.SetFloat("_Flame",1);
            effect.quad=new Mesh{name="Animated fuse flame"};
            effect.quad.vertices=new[]{new Vector3(-.5f,0),new Vector3(.5f,0),new Vector3(.5f,1),new Vector3(-.5f,1)};
            effect.quad.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
            effect.quad.triangles=new[]{0,1,2,0,2,3};effect.quad.RecalculateBounds();
            var flame=new GameObject("Flame consuming rope");effect.flame=flame.transform;
            flame.transform.SetParent(root.transform,false);
            flame.AddComponent<MeshFilter>().sharedMesh=effect.quad;
            var renderer=flame.AddComponent<MeshRenderer>();renderer.sharedMaterial=effect.fire;
            renderer.sortingLayerID=source.sortingLayerID;renderer.sortingOrder=source.sortingOrder+3;
            effect.Animate();return effect;
        }
        void Update(){age+=Time.deltaTime;Animate();}
        void Animate()
        {
            if(source==null || source.sprite==null)return;
            float t=Mathf.Clamp01(age/Mathf.Max(.01f,duration));
            // Pixel coordinates from the 400x400 master: tip (180,80), bend (218,100), base (202,151).
            Vector2 a=new Vector2(.45f,.80f),b=new Vector2(.59f,.78f),c=new Vector2(.505f,.6225f);
            Vector2 point=(1-t)*(1-t)*a+2*(1-t)*t*b+t*t*c;
            var sprite=source.sprite;
            Vector2 local=(Vector2.Scale(point,sprite.rect.size)-sprite.pivot)/sprite.pixelsPerUnit;
            float height=sprite.rect.height/sprite.pixelsPerUnit;
            flame.localPosition=new Vector3(local.x,local.y-height*1.35f*.35f,-.02f);
            flame.localScale=new Vector3(height*1.35f,height*1.35f,1);
            rope.SetFloat("_BurnY",point.y);fire.SetFloat("_Age",age);
        }
        void OnDisable(){Restore();}
        void Restore()
        {
            if(layers==null)return;
            for(int i=0;i<layers.Length;i++)
                if(layers[i]!=null && layers[i].sharedMaterial==rope)layers[i].sharedMaterial=originals[i];
        }
        void OnDestroy()
        {
            Restore();
            if(rope!=null)Destroy(rope);if(fire!=null)Destroy(fire);if(quad!=null)Destroy(quad);
        }
    }
}
