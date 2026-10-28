using System;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>Small UI sprites, clipped by the jar's interior masks and sorted around the egg.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class RoyalPostmanIncubatorParticles : MonoBehaviour
    {
        public RectTransform rearLayer, frontLayer;
        public Image[] motes = Array.Empty<Image>();
        public Color tint = new Color(.35f,1,1,1);
        public bool IsRunning { get; private set; }
        RoyalPostmanRevealLight warmth,heatCore;
        public void EnableWarmth()
        {
            if(warmth!=null||rearLayer==null)return;
            var r=(RectTransform)new GameObject("Warmth",typeof(RectTransform)).transform;r.SetParent(rearLayer,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=new Vector2(0,-rearLayer.rect.height*.18f);r.sizeDelta=new Vector2(rearLayer.rect.width*1.12f,rearLayer.rect.height*1.08f);r.SetAsFirstSibling();
            warmth=r.gameObject.AddComponent<RoyalPostmanRevealLight>();warmth.raycastTarget=false;warmth.color=new Color(1,.29f,.015f,.52f);tint=new Color(1,.64f,.19f,1);
            var core=(RectTransform)new GameObject("HeatCore",typeof(RectTransform)).transform;core.SetParent(r,false);core.anchorMin=core.anchorMax=core.pivot=Vector2.one*.5f;core.sizeDelta=r.sizeDelta*.73f;
            heatCore=core.gameObject.AddComponent<RoyalPostmanRevealLight>();heatCore.raycastTarget=false;heatCore.color=new Color(1,.96f,.54f,.72f);
            int count=motes.Length;if(count==0)return;var extra=new Image[count];Array.Copy(motes,extra,count);Array.Resize(ref motes,count*3);
            for(int i=count;i<motes.Length;i++){var source=extra[i%count];if(source==null)continue;var image=Instantiate(source,source.transform.parent,false);image.name="HeatMote"+i;image.raycastTarget=false;motes[i]=image;}
            for(int i=0;i<motes.Length;i++)if(motes[i]!=null)motes[i].rectTransform.sizeDelta=Vector2.one*(24+(i%4)*5);
        }

        public void SetRunning(bool running)
        {
            IsRunning=running;
            if(rearLayer!=null)rearLayer.gameObject.SetActive(running);
            if(frontLayer!=null)frontLayer.gameObject.SetActive(running);
            if(running)Sample(Time.realtimeSinceStartupAsDouble);
        }
        void Update(){if(IsRunning)Sample(Time.realtimeSinceStartupAsDouble);}
        public void Sample(double time)
        {
            if(warmth!=null){float heat=.5f+.5f*Mathf.Sin((float)time*2.6f);warmth.color=new Color(1,.29f,.015f,.46f+.17f*heat);warmth.rectTransform.localScale=new Vector3(1+heat*.04f,1+heat*.08f,1);heatCore.color=new Color(1,.96f,.54f,.58f+.2f*heat);}
            for(int i=0;i<motes.Length;i++)
            {
                var image=motes[i];if(image==null)continue;
                var parent=image.rectTransform.parent as RectTransform;if(parent==null)continue;
                float phase=Mathf.Repeat((float)(time*(warmth!=null?.3:.11))+i*.173f,1);
                float side=(i%2==0?-1:1);
                float x=side*parent.rect.width*((warmth!=null?.29f:.24f)+.065f*Mathf.Sin(i*1.7f+(float)time*.9f));
                float y=(phase-.5f)*parent.rect.height*.78f;
                image.rectTransform.anchoredPosition=new Vector2(x,y);
                float pulse=.55f+.45f*Mathf.Sin((float)time*1.4f+i);
                image.rectTransform.localScale=Vector3.one*(warmth!=null?.9f+.35f*pulse:.7f+.3f*pulse);
                var c=warmth!=null?(i%3==0?new Color(1,.43f,.035f):new Color(1,.98f,.79f)):Color.Lerp(tint,Color.white,i%3==0?.8f:.25f);
                c.a=Mathf.Sin(phase*Mathf.PI)*(warmth!=null?.88f+.12f*pulse:.5f+.4f*pulse);
                image.color=c;
            }
        }
    }
}

