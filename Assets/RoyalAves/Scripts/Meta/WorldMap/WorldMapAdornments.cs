using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalAves.Meta.WorldMap
{
    [Serializable] public struct WorldMapAdornment
    {
        public string id,label;
        public Sprite sprite;
        public Sprite alternate;
        public float size;
        public Vector2 anchor;
        public bool behind,emissive,blink;
        public Color lightColor;
        public WorldMapAdornmentMotion motion;
        public static WorldMapAdornment Create(Sprite sprite,string label,bool lamp=false)=>new WorldMapAdornment{
            id="adornment-"+Guid.NewGuid().ToString("N"),label=label,sprite=sprite,size=lamp?.14f:.12f,anchor=new Vector2(.5f,.6f),emissive=lamp,lightColor=new Color(1,.75f,.30f,1)};
        public static bool HasDefault(Sprite source)=>source!=null&&source.name.StartsWith("lamp-straight-glossy-v46");
        public static WorldMapAdornment[] Defaults(Sprite source,WorldMapKit kit)=>HasDefault(source)&&kit.envelopeCream!=null?new[]{new WorldMapAdornment{
            id="adornment-lamp-letter",label="Cartinha do poste",sprite=kit.envelopeCream,size=.095f,anchor=new Vector2(.5f,.49f),lightColor=Color.white}}:Array.Empty<WorldMapAdornment>();
        public static WorldMapAdornment[] Copy(WorldMapAdornment[] values)=>values==null?null:(WorldMapAdornment[])values.Clone();
    }
    public sealed class WorldMapAdornments : IDisposable
    {
        sealed class Piece {public WorldMapAdornment data;public WorldMapEffectPiece pose;public MeshRenderer art,halo;public MaterialPropertyBlock block=new MaterialPropertyBlock(),glow=new MaterialPropertyBlock();public float phase;}
        readonly GameObject root;readonly int layer;readonly Func<Texture,Material> material;readonly Material glowMaterial;
        readonly List<Piece> pieces=new List<Piece>();
        WorldMapAdornment[] shown;Sprite source;float height;bool custom;
        public WorldMapAdornments(Transform parent,int layer,Func<Texture,Material> material,Material glow)
        {this.layer=layer;this.material=material;glowMaterial=glow;root=new GameObject("AdereçosDoObjeto"){layer=layer};root.transform.SetParent(parent,false);}
        public void Tick(Sprite source,float height,bool custom,WorldMapAdornment[] values,WorldMapPiecePose[] poses,WorldMapKit kit,float time,float night)
        {
            root.SetActive(true);
            if(this.source!=source||this.height!=height||this.custom!=custom||!ReferenceEquals(shown,values))
            {
                Clear();this.source=source;this.height=height;this.custom=custom;shown=values;
                foreach(var data in custom?values??Array.Empty<WorldMapAdornment>():WorldMapAdornment.Defaults(source,kit))
                {
                    if(data.sprite==null||string.IsNullOrEmpty(data.id))continue;
                    var go=new GameObject(data.label??"Adereço"){layer=layer};go.transform.SetParent(root.transform,false);
                    go.transform.localPosition=WorldMapCards.ImagePoint(source,height,data.anchor)+Vector3.forward*(data.behind?.03f:-.045f);
                    var art=new GameObject("ArteDoAdereço",typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};art.transform.SetParent(go.transform,false);
                    float size=Mathf.Max(.015f,data.size)*height;art.transform.localPosition=Vector3.down*size*(data.motion==WorldMapAdornmentMotion.RibbonSway?1:.5f);
                    if(data.motion==WorldMapAdornmentMotion.Windsock)art.transform.localPosition+=Vector3.right*WorldMapCards.Width(data.sprite,size)*.5f;
                    art.GetComponent<MeshFilter>().sharedMesh=WorldMapCards.Get(data.sprite,size);
                    var renderer=art.GetComponent<MeshRenderer>();renderer.sharedMaterial=material(data.sprite.texture);renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                    var piece=new Piece{data=data,art=renderer,pose=go.AddComponent<WorldMapEffectPiece>(),phase=pieces.Count*1.71f};piece.pose.Init(data.id,data.label??"Adereço");
                    if(data.emissive&&glowMaterial!=null)
                    {
                        var halo=new GameObject("AuraDoAdereço",typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};halo.transform.SetParent(go.transform,false);halo.transform.localPosition=Vector3.back*.012f;halo.transform.localScale=Vector3.one*size*2.6f;
                        halo.GetComponent<MeshFilter>().sharedMesh=WorldMapWindowLight.Quad;piece.halo=halo.GetComponent<MeshRenderer>();piece.halo.sharedMaterial=glowMaterial;piece.halo.shadowCastingMode=ShadowCastingMode.Off;
                    }
                    pieces.Add(piece);
                }
            }
            foreach(var piece in pieces)
            {
                piece.pose.Apply(poses,height);
                float cycle=Mathf.Repeat(time*.16f+piece.phase*.17f,1);
                piece.art.enabled=true;
                if((piece.data.motion==WorldMapAdornmentMotion.OwlBlink||piece.data.motion==WorldMapAdornmentMotion.OwlLanding||piece.data.motion==WorldMapAdornmentMotion.StampBubble)&&piece.data.alternate!=null)
                {
                    bool alternate=piece.data.motion==WorldMapAdornmentMotion.StampBubble?cycle>.48f:Mathf.Repeat(time+piece.phase,4.7f)<.16f;
                    var sprite=alternate?piece.data.alternate:piece.data.sprite;
                    piece.art.GetComponent<MeshFilter>().sharedMesh=WorldMapCards.Get(sprite,Mathf.Max(.015f,piece.data.size)*height);piece.art.sharedMaterial=material(sprite.texture);
                }
                var movement=piece.pose.transform;float wave=Mathf.Sin(time*2.5f+piece.phase);
                switch(piece.data.motion)
                {
                    case WorldMapAdornmentMotion.LetterFlutter:movement.localRotation*=Quaternion.Euler(0,0,Mathf.Sin(time*6+piece.phase)*8);movement.localPosition+=Vector3.up*(height*.025f*wave);break;
                    case WorldMapAdornmentMotion.RopeTug:var stretch=movement.localScale;stretch.x*=1+wave*.065f;movement.localScale=stretch;movement.localPosition+=Vector3.right*(height*.015f*wave);break;
                    case WorldMapAdornmentMotion.Windsock:movement.localRotation*=Quaternion.Euler(0,0,wave*5);var billow=movement.localScale;billow.y*=1+Mathf.Sin(time*3.7f+piece.phase)*.045f;movement.localScale=billow;break;
                    case WorldMapAdornmentMotion.WheelSpin:movement.localRotation*=Quaternion.Euler(0,0,-time*95);break;
                    case WorldMapAdornmentMotion.WaterDrop:var fall=Mathf.Repeat(time*1.25f+piece.phase*.25f,1);movement.localPosition+=new Vector3(height*fall*.12f,-height*fall*.64f,0);movement.localScale*=.55f+Mathf.Sin(fall*Mathf.PI)*.45f;break;
                    case WorldMapAdornmentMotion.LetterFly:
                        float flight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.60f,1,cycle));movement.localPosition+=new Vector3(flight*height*.60f,flight*height*.55f,0);movement.localRotation*=Quaternion.Euler(0,0,flight*22+wave*2);movement.localScale*=1-flight*.85f;break;
                    case WorldMapAdornmentMotion.QuillSpin:movement.localRotation*=Quaternion.Euler(0,0,-time*17);break;
                    case WorldMapAdornmentMotion.InkRise:
                        float rise=Mathf.Repeat(time*.34f+piece.phase*.19f,1);movement.localPosition+=Vector3.up*height*rise*.45f;movement.localScale*=Mathf.Sin(rise*Mathf.PI)*.65f+.18f;break;
                    case WorldMapAdornmentMotion.PoolRipple:var rippleScale=movement.localScale;rippleScale.x*=1+wave*.025f;rippleScale.y*=1+Mathf.Sin(time*1.8f+piece.phase)*.03f;movement.localScale=rippleScale;break;
                    case WorldMapAdornmentMotion.StampBubble:
                        movement.localPosition+=new Vector3(Mathf.Sin(cycle*5+piece.phase)*height*.07f,cycle*height*.48f,0);movement.localScale*=Mathf.Min(1,cycle*8)*(1-Mathf.SmoothStep(.85f,1,cycle));movement.localRotation*=Quaternion.Euler(0,0,cycle>.48f?Mathf.Sin(time*2+piece.phase)*18:0);break;
                    case WorldMapAdornmentMotion.TubeIn:
                        movement.localPosition+=Vector3.left*height*.34f*(1-cycle);movement.localScale*=1-Mathf.SmoothStep(.55f,.92f,cycle);piece.art.enabled=cycle<.92f;break;
                    case WorldMapAdornmentMotion.TubeOut:
                        movement.localPosition+=new Vector3(cycle*height*.40f,Mathf.Sin(cycle*Mathf.PI)*height*.15f,0);movement.localScale*=Mathf.SmoothStep(0,.15f,cycle)*(1-Mathf.SmoothStep(.78f,1,cycle));break;
                    case WorldMapAdornmentMotion.WaxBubble:
                        movement.localScale*=Mathf.Sin(cycle*Mathf.PI)*.8f;piece.art.enabled=cycle<.8f;break;
                    case WorldMapAdornmentMotion.SealMark:
                        movement.localPosition+=Vector3.up*cycle*height*.35f;movement.localScale*=Mathf.Sin(cycle*Mathf.PI);piece.art.enabled=cycle>.35f;break;
                    case WorldMapAdornmentMotion.LetterRain:
                        float rain=Mathf.Repeat(time*.40f+piece.phase*.23f,1);movement.localPosition+=new Vector3(Mathf.Sin(rain*5+piece.phase)*height*.15f,-rain*height*.72f,0);movement.localRotation*=Quaternion.Euler(0,0,wave*12);movement.localScale*=Mathf.Min(1,rain*8)*(1-Mathf.SmoothStep(.83f,1,rain));break;
                    case WorldMapAdornmentMotion.RibbonSway:movement.localRotation*=Quaternion.Euler(0,0,wave*6);break;
                    case WorldMapAdornmentMotion.ParcelWiggle:movement.localRotation*=Quaternion.Euler(0,0,Mathf.Sin(time*7)*Mathf.Pow(Mathf.Max(0,Mathf.Sin(time*.55f)),10)*4);break;
                    case WorldMapAdornmentMotion.OwlLanding:
                        float flightHeight=1-Mathf.SmoothStep(0,.18f,cycle)+Mathf.SmoothStep(.72f,.94f,cycle);movement.localPosition+=Vector3.up*height*flightHeight*.38f;movement.localScale*=1-flightHeight*.8f;piece.art.enabled=cycle<.94f;break;
                }
                float pulse=piece.data.blink?Mathf.Lerp(.22f,1,Mathf.SmoothStep(0,1,.5f+.5f*Mathf.Sin(time*2.8f+piece.phase))):1;
                if(piece.data.motion==WorldMapAdornmentMotion.SealGlow)pulse=.5f+.5f*Mathf.Sin(time*1.7f+piece.phase);
                piece.block.SetColor("_Tint",piece.data.motion==WorldMapAdornmentMotion.WaxBubble?new Color(1,.38f,.51f):Color.white);piece.block.SetFloat("_MagicFlow",piece.data.motion==WorldMapAdornmentMotion.InkFall?1:piece.data.motion==WorldMapAdornmentMotion.PoolRipple?2:0);piece.block.SetFloat("_MagicClock",time);
                piece.block.SetFloat("_GroundedFoot",0);piece.block.SetFloat("_NightAmount",piece.data.emissive?night*.2f:night*.3f);piece.block.SetFloat("_Emission",piece.data.emissive?(.12f+night*.9f)*pulse:0);piece.art.SetPropertyBlock(piece.block);
                if(piece.halo!=null){var color=piece.data.lightColor;if(color.maxColorComponent<.01f)color=new Color(1,.75f,.3f);color.a=(.035f+night*.30f)*pulse;piece.glow.SetColor("_Color",color);piece.halo.SetPropertyBlock(piece.glow);}
            }
        }
        public void SetActive(bool active)=>root.SetActive(active);
        void Clear(){foreach(var piece in pieces){piece.pose.gameObject.SetActive(false);Kill(piece.pose.gameObject);}pieces.Clear();}
        public void Dispose(){Clear();Kill(root);}
        static void Kill(UnityEngine.Object value){if(value==null)return;if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
    }
}
