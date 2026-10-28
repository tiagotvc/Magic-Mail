using System;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.Meta;

namespace RoyalAves.UI.RoyalPostman2026
{
    [ExecuteAlways]
    public sealed class RoyalPostmanHatchReveal : MonoBehaviour
    {
        public bool IsOpen=>root!=null&&root.gameObject.activeSelf;
        public bool Ready {get;private set;}
        public string ReceiptId {get;private set;}
        public const float RevealSeconds=3.2f;
        RectTransform root,eggGroup;
        Image dimmer,egg,pet,flash;
        RoyalPostmanRevealLight glow,coreGlow,rays;
        RoyalPostmanRadialSparkles sparkles;
        RoyalPostmanEggShard[] shards;
        Vector2[] directions,centers,rests;
        Text title,nameLabel,rarityLabel,hint;
        RoyalPostmanBevelPanel rarityPlate;
        Button close;
        Vector2 start,eggSize;
        Action acknowledge;
        double opened;
        static readonly string[] PT={"Comum","Raro","Épico","Lendário"},EN={"Common","Rare","Epic","Legendary"};
        static readonly Color[] Colors={new Color(.5f,.84f,1),new Color(.68f,.37f,1),new Color(1,.42f,.2f),new Color(1,.78f,.13f)};
        static RectTransform Node(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size;return r;
        }
        static Image Picture(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var r=Node(parent,name,position,size);var image=r.gameObject.AddComponent<Image>();image.raycastTarget=false;return image;
        }
        static Text Label(Transform parent,Text template,string name,Vector2 position,Vector2 size,int fontSize)
        {
            var label=Instantiate(template,parent,false);label.name=name;label.gameObject.SetActive(true);var r=label.rectTransform;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size;r.localScale=Vector3.one;label.fontSize=fontSize;label.resizeTextForBestFit=true;label.resizeTextMaxSize=fontSize;label.resizeTextMinSize=22;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;return label;
        }
        void Build(RoyalPostmanPetsScreen owner)
        {
            if(root!=null)return;
            root=Node(owner.transform,"HatchReveal",Vector2.zero,Vector2.zero);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;
            dimmer=Picture(root,"Dimmer",Vector2.zero,Vector2.zero);dimmer.rectTransform.anchorMin=Vector2.zero;dimmer.rectTransform.anchorMax=Vector2.one;dimmer.color=Color.clear;dimmer.raycastTarget=true;
            var blocker=dimmer.gameObject.AddComponent<Button>();blocker.transition=Selectable.Transition.None;blocker.onClick.AddListener(Continue);
            var light=Node(root,"Glow",new Vector2(0,80),new Vector2(1040,1120));glow=light.gameObject.AddComponent<RoyalPostmanRevealLight>();glow.raycastTarget=false;
            var core=Node(root,"CoreGlow",new Vector2(0,80),new Vector2(670,750));coreGlow=core.gameObject.AddComponent<RoyalPostmanRevealLight>();coreGlow.raycastTarget=false;
            var beam=Node(root,"RotatingRays",new Vector2(0,80),new Vector2(1480,1480));rays=beam.gameObject.AddComponent<RoyalPostmanRevealLight>();rays.rays=true;rays.raycastTarget=false;
            var stars=Node(beam,"PulseSparkles",Vector2.zero,new Vector2(1480,1480));sparkles=stars.gameObject.AddComponent<RoyalPostmanRadialSparkles>();sparkles.raycastTarget=false;
            if(owner.hatchery.particles!=null)foreach(var mote in owner.hatchery.particles.motes)if(mote!=null&&mote.sprite!=null){sparkles.sprite=mote.sprite;break;}
            pet=Picture(root,"Pet",new Vector2(0,90),new Vector2(570,590));pet.preserveAspect=true;
            eggGroup=Node(root,"Egg",new Vector2(0,80),new Vector2(520,620));egg=Picture(eggGroup,"WholeEgg",Vector2.zero,new Vector2(520,620));egg.preserveAspect=true;
            var pieces=RoyalPostmanEggShard.Pieces();shards=new RoyalPostmanEggShard[pieces.Length];directions=new Vector2[pieces.Length];centers=new Vector2[pieces.Length];rests=new Vector2[pieces.Length];
            for(int i=0;i<shards.Length;i++)
            {
                var shard=Node(eggGroup,"Shell"+i,Vector2.zero,new Vector2(520,620)).gameObject.AddComponent<RoyalPostmanEggShard>();shard.raycastTarget=false;shards[i]=shard;
                Vector2 center=Vector2.zero;foreach(var p in pieces[i])center+=p;center/=pieces[i].Length;centers[i]=center;directions[i]=(center-Vector2.one*.5f).normalized;
                var outline=shard.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.19f,.07f,.025f,.9f);outline.effectDistance=new Vector2(1.3f,-1.3f);outline.useGraphicAlpha=true;
            }
            flash=Picture(root,"Flash",Vector2.zero,Vector2.zero);flash.rectTransform.anchorMin=Vector2.zero;flash.rectTransform.anchorMax=Vector2.one;flash.color=Color.clear;
            var template=owner.hatchery.actionLabel;title=Label(root,template,"Title",new Vector2(0,590),new Vector2(960,150),94);nameLabel=Label(root,template,"Name",new Vector2(0,-350),new Vector2(960,120),80);
            foreach(var label in new[]{title,nameLabel}){foreach(var effect in label.GetComponents<BaseMeshEffect>())DestroyImmediate(effect);label.gameObject.AddComponent<RoyalPostmanRewardText>();}
            rarityPlate=Node(root,"RarityCapsule",new Vector2(0,-460),new Vector2(380,86)).gameObject.AddComponent<RoyalPostmanBevelPanel>();rarityPlate.radius=43;rarityPlate.bevel=6;rarityPlate.raycastTarget=false;
            rarityLabel=Label(root,template,"Rarity",new Vector2(0,-460),new Vector2(350,72),52);hint=Label(root,template,"Continue",new Vector2(0,-670),new Vector2(960,70),40);
            close=Instantiate(owner.helpButton,root,false);close.name="CloseReveal";close.onClick=new Button.ButtonClickedEvent();close.onClick.AddListener(Continue);var cr=(RectTransform)close.transform;cr.anchorMin=cr.anchorMax=new Vector2(1,1);cr.pivot=Vector2.one*.5f;cr.anchoredPosition=new Vector2(-95,-100);cr.sizeDelta=new Vector2(130,130);cr.localScale=Vector3.one;close.gameObject.SetActive(true);
            root.gameObject.SetActive(false);
        }
        public void Show(RoyalPostmanPetsScreen owner,PetHatchReceipt receipt,Sprite sourceEgg,Sprite portrait,bool portuguese,Action done)
        {
            Build(owner);ReceiptId=receipt.receiptId;acknowledge=done;Ready=false;opened=Time.realtimeSinceStartupAsDouble;
            var source=owner.hatchery.egg.rectTransform;start=root.InverseTransformPoint(source.position);eggSize=source.rect.size*source.lossyScale.x/root.lossyScale.x;egg.sprite=sourceEgg;pet.sprite=portrait;pet.color=Color.white;
            float aspect=sourceEgg!=null?sourceEgg.rect.width/sourceEgg.rect.height:.8f;var size=new Vector2(560*aspect,560);egg.rectTransform.sizeDelta=size;
            var pieces=RoyalPostmanEggShard.Pieces();for(int i=0;i<shards.Length;i++){shards[i].rectTransform.pivot=centers[i];rests[i]=Vector2.Scale(centers[i]-Vector2.one*.5f,size);shards[i].rectTransform.sizeDelta=size;shards[i].Set(sourceEgg,pieces[i]);shards[i].rectTransform.localRotation=Quaternion.identity;}
            title.text=portuguese?(receipt.isNew?"Novo mascote!":"Mascote recebido!"):(receipt.isNew?"New pet!":"Pet received!");nameLabel.text=portuguese||string.IsNullOrEmpty(receipt.englishName)?receipt.portugueseName:receipt.englishName;
            rarityLabel.text=(portuguese?PT:EN)[Mathf.Clamp(receipt.rarity,0,3)];hint.text=portuguese?"Toque para continuar":"Tap to continue";var tint=Colors[Mathf.Clamp(receipt.rarity,0,3)];glow.color=new Color(1,.68f,.08f);rays.color=Color.Lerp(tint,new Color(1,.94f,.55f),.8f);coreGlow.color=new Color(1,.98f,.72f);sparkles.color=new Color(1,.78f,.13f);rarityPlate.top=tint*.8f;rarityPlate.bottom=tint*.28f;rarityPlate.rim=new Color(1,.79f,.12f);rarityPlate.SetVerticesDirty();
            root.gameObject.SetActive(true);root.SetAsLastSibling();Sample(0);
        }
        void Update(){if(IsOpen)Sample((float)(Time.realtimeSinceStartupAsDouble-opened));}
        public void Tick(){if(IsOpen)Sample((float)(Time.realtimeSinceStartupAsDouble-opened));}
        public void Sample(float elapsed)
        {
            if(root==null)return;float intro=Mathf.SmoothStep(0,1,elapsed/.38f),split=Mathf.Clamp01((elapsed-1.25f)/.75f),burst=Mathf.Max(0,elapsed-2f),reveal=Mathf.Clamp01((elapsed-2.08f)/.62f);
            dimmer.color=new Color(0,.001f,.004f,.975f*intro);var bg=transform.Find("Background") as RectTransform;if(bg!=null){dimmer.rectTransform.offsetMin=bg.offsetMin;dimmer.rectTransform.offsetMax=bg.offsetMax;}
            eggGroup.anchoredPosition=Vector2.Lerp(start,new Vector2(0,80),intro);float from=eggSize.y/560f;float pulse=elapsed<2?1+.025f*Mathf.Sin(elapsed*16):1;eggGroup.localScale=Vector3.one*Mathf.Lerp(from,1,intro)*pulse;eggGroup.localRotation=Quaternion.Euler(0,0,elapsed>.6f&&elapsed<2?Mathf.Sin(elapsed*(18+split*20))*(2+split*6):0);
            egg.gameObject.SetActive(split<=0);float shellFade=1-Mathf.Clamp01(burst/.78f);
            for(int i=0;i<shards.Length;i++)
            {
                var s=shards[i];s.gameObject.SetActive(split>0&&shellFade>0);var direction=directions[i];var travel=direction*(split*4+burst*(650+(i%3)*80))+Vector2.up*(burst*210-burst*burst*500);s.rectTransform.anchoredPosition=rests[i]+travel;s.rectTransform.localRotation=Quaternion.Euler(0,0,burst*(i%2==0?-1:1)*(160+i*22));s.color=new Color(1,1,1,shellFade);
            }
            pet.gameObject.SetActive(reveal>0);pet.rectTransform.localScale=Vector3.one*(reveal*(1+.12f*Mathf.Sin(reveal*Mathf.PI)));pet.color=new Color(1,1,1,reveal);
            float lightPulse=.5f+.5f*Mathf.Cos(Mathf.Max(0,elapsed-2.08f)*Mathf.PI*2/1.7f);
            var gc=glow.color;gc.a=intro*(.32f+reveal*(.52f+.13f*lightPulse));glow.color=gc;glow.rectTransform.localScale=Vector3.one*(1+lightPulse*.06f);var cc=coreGlow.color;cc.a=intro*(.08f+reveal*(.32f+.13f*lightPulse));coreGlow.color=cc;
            var rc=rays.color;rc.a=intro*(.2f+reveal*(.45f+.2f*lightPulse));rays.color=rc;rays.rectTransform.localRotation=Quaternion.Euler(0,0,elapsed*13);sparkles.Sample(Mathf.Max(0,elapsed-2.08f),reveal*.98f);
            flash.color=new Color(1,.96f,.77f,burst>0?Mathf.Max(0,1-burst/.18f)*.85f:0);title.gameObject.SetActive(reveal>0);nameLabel.gameObject.SetActive(reveal>.6f);rarityLabel.gameObject.SetActive(reveal>.7f);rarityPlate.gameObject.SetActive(reveal>.7f);Ready=elapsed>=RevealSeconds;hint.gameObject.SetActive(Ready);close.interactable=Ready;
        }
        public void Continue(){if(!IsOpen||!Ready)return;root.gameObject.SetActive(false);ReceiptId=null;var done=acknowledge;acknowledge=null;done?.Invoke();}
        public void Hide(){if(root!=null)root.gameObject.SetActive(false);ReceiptId=null;acknowledge=null;Ready=false;}
    }
}
