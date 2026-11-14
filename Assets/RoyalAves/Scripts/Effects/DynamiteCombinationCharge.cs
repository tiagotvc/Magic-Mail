using System;
using System.Collections;
using UnityEngine;
using SweetSugar.Scripts.Items;

namespace RoyalAves.Effects
{
    // Reuses the actual sprites and burning rope for the rocket/package merge.
    public sealed class DynamiteCombinationCharge : IDisposable
    {
        readonly SpriteRenderer[] views;
        readonly Vector3[] scales,positions;
        readonly int[] orders,layers;
        readonly Item[] items;
        readonly bool[] animators;
        DynamiteFuseEffect fuse;
        public DynamiteCombinationCharge(Item package,Item partner)
        {
            items=new[]{package,partner};views=new[]{package.GetSpriteRenderer(),partner.GetSpriteRenderer()};
            scales=new Vector3[2];positions=new Vector3[2];orders=new int[2];layers=new int[2];animators=new bool[2];
            for(int i=0;i<2;i++)
            {
                var v=views[i];scales[i]=v.transform.localScale;positions[i]=v.transform.position;
                orders[i]=v.sortingOrder;layers[i]=v.sortingLayerID;
                v.sortingLayerName=PowerUpEffectOrder.SortingLayer;v.sortingOrder=PowerUpEffectOrder.Value+8-i;
                if(items[i].anim!=null){animators[i]=items[i].anim.enabled;items[i].anim.enabled=false;}
            }
            fuse=DynamiteFuseEffect.Attach(views[0],.70f,package.GetSpriteRenderers());
        }
        public IEnumerator Play()
        {
            for(float time=0;time<.70f;time+=Time.deltaTime)
            {
                if(views[0]==null || views[1]==null)yield break;
                float t=Mathf.Clamp01(time/.70f);
                float pull=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.55f));
                views[1].transform.position=Vector3.Lerp(positions[1],views[0].transform.position,pull);
                views[1].transform.localScale=scales[1]*Mathf.Lerp(1,.12f,pull);
                views[0].transform.localScale=scales[0]*(1+.55f*Mathf.SmoothStep(0,1,t));
                yield return null;
            }
            foreach(var item in items)if(item!=null)item.HideSprites(true);
            if(fuse!=null){fuse.gameObject.SetActive(false);UnityEngine.Object.Destroy(fuse.gameObject);}
        }
        public void Dispose()
        {
            if(fuse!=null){fuse.gameObject.SetActive(false);UnityEngine.Object.Destroy(fuse.gameObject);}
            for(int i=0;i<2;i++)if(views[i]!=null)
            {
                views[i].transform.localScale=scales[i];views[i].transform.position=positions[i];
                views[i].sortingOrder=orders[i];views[i].sortingLayerID=layers[i];
                if(items[i]!=null && items[i].anim!=null)items[i].anim.enabled=animators[i];
            }
        }
    }
}
