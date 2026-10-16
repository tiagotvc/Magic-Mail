using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalAves.Meta.WorldMap
{
    // Window masks and rays follow the original card, including mirroring and ground depth.
    public sealed class WorldMapWindowLight : IDisposable
    {
        [Serializable] sealed class Profiles{public Profile[] profiles;}
        [Serializable] sealed class Profile{public string name;public Vector4[] windows;}
        static Dictionary<string,Vector4[]> profiles;
        static Mesh quad;
        public static Mesh Quad
        {
            get
            {
                if(quad!=null)return quad;
                quad=new Mesh{name="MapLightQuad",hideFlags=HideFlags.HideAndDontSave};
                quad.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0),new Vector3(.5f,-.5f,0)};
                quad.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};quad.triangles=new[]{0,1,2,0,2,3};quad.RecalculateBounds();return quad;
            }
        }
        readonly GameObject root;
        readonly List<MeshRenderer> rays=new List<MeshRenderer>();
        readonly Material material;
        readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
        Sprite shown;float shownHeight;
        readonly WorldMapKit kit;
        readonly Func<Texture,Material> cardMaterial;
        MeshRenderer houseLamp;
        GameObject lampSupport;
        Mesh lampSupportMesh;
        public Transform Root=>root.transform;
        public WorldMapWindowLight(Transform parent,int layer,Shader shader,WorldMapKit kit,Func<Texture,Material> cardMaterial)
        {
            this.kit=kit;this.cardMaterial=cardMaterial;
            root=new GameObject("LuzDasJanelas"){layer=layer};root.transform.SetParent(parent,false);
            if(shader==null)shader=Shader.Find("RoyalAves/WorldMap/WindowGlow");
            if(shader!=null)material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
        }
        static Vector4[] Windows(Sprite sprite)
        {
            if(profiles==null)
            {
                profiles=new Dictionary<string,Vector4[]>();var text=Resources.Load<TextAsset>("WorldMapWindowProfiles");
                if(text!=null)foreach(var p in JsonUtility.FromJson<Profiles>(text.text).profiles)
                {profiles[p.name]=p.windows;if(p.name.EndsWith("_0"))profiles[p.name.Substring(0,p.name.Length-2)]=p.windows;}
            }
            return profiles.TryGetValue(sprite.name,out var values)?values:Array.Empty<Vector4>();
        }
        public void SetActive(bool active)=>root.SetActive(active);
        public static bool IsBuilding(Sprite sprite)
        {
            var n=sprite.name.ToLowerInvariant();
            foreach(var word in new[]{"house","cottage","post","workshop","factory","garage","inn","stables","station","studio","archive","depot","shelter","atelier","mill","aviary","observatory","pavilion","tent","booth","kiosk","lighthouse","headquarters","sorting-hall","dispatch-tower","parcel-mountain"})if(n.Contains(word))return true;
            return false;
        }
        public void Tick(Sprite sprite,float height,float night,MeshRenderer body,MaterialPropertyBlock bodyBlock)
        {
            var windows=Windows(sprite);
            bodyBlock.SetFloat("_WindowLight",night);
            bodyBlock.SetFloat("_WindowCount",windows.Length);
            bool headquarters=sprite.name.StartsWith("central-headquarters-"),dispatch=sprite.name.StartsWith("dispatch-tower-");
            bool parcelDepot=sprite.name=="parcel-mountain-v53",stampFactory=sprite.name=="stamp-factory-v53";
            bodyBlock.SetFloat("_WindowWarm",dispatch||parcelDepot?1:0);
            bodyBlock.SetFloat("_WindowPaintedGlass",headquarters||dispatch||parcelDepot||stampFactory||sprite.name.StartsWith("sorting-hall-")?1:0);
            var r=sprite.rect;var t=sprite.texture;
            bodyBlock.SetVector("_WindowSpriteRect",new Vector4(r.x/t.width,r.y/t.height,r.width/t.width,r.height/t.height));
            for(int i=0;i<4;i++)bodyBlock.SetVector("_WindowRect"+i,i<windows.Length?windows[i]:Vector4.zero);
            body.SetPropertyBlock(bodyBlock);
            root.SetActive((windows.Length>0||IsBuilding(sprite))&&material!=null);
            if(shown!=sprite||shownHeight!=height)
            {
                Clear();shown=sprite;shownHeight=height;
                foreach(var window in windows)
                {
                    var center=WorldMapCards.ImagePoint(sprite,height,new Vector2(window.x+window.z*.5f,window.y+window.w*.5f));
                    Ray("ReflexoDaJanela",center,height);
                }
                if(headquarters)Ray("LuzDaLampadaOriginal",WorldMapCards.ImagePoint(sprite,height,new Vector2(.283f,.278f)),height*.60f);
                if(IsBuilding(sprite)&&!headquarters&&!dispatch&&kit.christmasBulbs!=null&&kit.christmasBulbs.Length>0&&kit.christmasBulbs[0]!=null)
                {
                    var anchor=WorldMapCards.ImagePoint(sprite,height,sprite.name.StartsWith("sorting-hall-")?new Vector2(.18f,.47f):new Vector2(.30f,.55f));
                    lampSupport=new GameObject("SuporteDaLampada",typeof(MeshFilter),typeof(MeshRenderer)){layer=root.layer};lampSupport.transform.SetParent(root.transform,false);
                    lampSupport.transform.localPosition=anchor+Vector3.back*.030f;
                    lampSupportMesh=Support(height);lampSupport.GetComponent<MeshFilter>().sharedMesh=lampSupportMesh;
                    var mount=lampSupport.GetComponent<MeshRenderer>();mount.sharedMaterial=cardMaterial(Texture2D.whiteTexture);
                    block.Clear();block.SetColor("_Tint",new Color(.50f,.25f,.075f));mount.SetPropertyBlock(block);
                    var lamp=new GameObject("LampadaDaCasa",typeof(MeshFilter),typeof(MeshRenderer)){layer=root.layer};lamp.transform.SetParent(root.transform,false);
                    lamp.transform.localPosition=anchor+Vector3.back*.035f;lamp.transform.localRotation=Quaternion.Euler(0,0,180);
                    var art=kit.christmasBulbs[0];lamp.GetComponent<MeshFilter>().sharedMesh=WorldMapCards.Get(art,height*.14f,true);
                    houseLamp=lamp.GetComponent<MeshRenderer>();houseLamp.sharedMaterial=cardMaterial(art.texture);
                    houseLamp.shadowCastingMode=ShadowCastingMode.Off;houseLamp.receiveShadows=false;
                    Ray("LuzDaLampadaDaCasa",anchor-Vector3.up*height*.06f,height);
                }
            }
            block.Clear();block.SetColor("_Color",new Color(1,.57f,.12f,night*.85f));foreach(var ray in rays){ray.gameObject.SetActive(night>.005f);ray.SetPropertyBlock(block);}
            if(houseLamp!=null){block.Clear();block.SetFloat("_Emission",.15f+night*2.2f);houseLamp.SetPropertyBlock(block);}
        }
        void Ray(string name,Vector3 center,float height)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)){layer=root.layer};go.transform.SetParent(root.transform,false);
            go.transform.localPosition=new Vector3(center.x,center.y-height*.16f,-.045f);go.transform.localScale=new Vector3(height*.48f,height*.65f,1);
            go.GetComponent<MeshFilter>().sharedMesh=Quad;var drawing=go.GetComponent<MeshRenderer>();drawing.sharedMaterial=material;
            drawing.shadowCastingMode=ShadowCastingMode.Off;drawing.receiveShadows=false;rays.Add(drawing);
        }
        static Mesh Support(float h)
        {
            var v=new[]{new Vector3(-.075f,-.035f,0),new Vector3(-.075f,.035f,0),new Vector3(-.058f,.035f,0),new Vector3(-.058f,-.035f,0),
                new Vector3(-.063f,.006f,0),new Vector3(-.063f,.022f,0),new Vector3(.008f,.022f,0),new Vector3(.008f,.006f,0)};
            for(int i=0;i<v.Length;i++)v[i]*=h;
            var uv=new Vector2[v.Length];for(int i=0;i<uv.Length;i++)uv[i]=Vector2.one*.5f;
            var mesh=new Mesh{name="SuporteDaLampadaNaParede",vertices=v,uv=uv,triangles=new[]{0,1,2,0,2,3,4,5,6,4,6,7}};mesh.RecalculateBounds();return mesh;
        }
        void Clear(){foreach(var ray in rays)if(ray!=null)Kill(ray.gameObject);rays.Clear();if(houseLamp!=null)Kill(houseLamp.gameObject);houseLamp=null;if(lampSupport!=null)Kill(lampSupport);if(lampSupportMesh!=null)Kill(lampSupportMesh);lampSupport=null;lampSupportMesh=null;}
        public void Dispose(){Clear();if(material!=null)Kill(material);if(root!=null)Kill(root);}
        static void Kill(UnityEngine.Object obj){if(Application.isPlaying)UnityEngine.Object.Destroy(obj);else UnityEngine.Object.DestroyImmediate(obj);}
    }
}
