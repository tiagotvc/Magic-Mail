// Used ONLY in the isolated preview project. Never copied into the game Assets directory.
using System;
using System.Collections.Generic;
using UnityEngine;
namespace SweetSugar.Scripts.GUI.Boost
{
    public enum BoostType { ExtraMoves,Packages,Stripes,ExtraTime,Bomb,MulticolorCandy,FreeMove,ExplodeArea,Marmalade,None }
    [Serializable] public class BoostProduct { public BoostType boostType;public Sprite icon; }
    public class BoostShop:MonoBehaviour {public List<BoostProduct> boostProducts=new List<BoostProduct>();}
    public class BoostIcon:MonoBehaviour
    {
        public BoostType type;public GameObject check;public int calls;
        public void ActivateBoost(){if(transform.Find("Lock").gameObject.activeSelf)return;calls++;check.SetActive(!check.activeSelf);}
    }
}
namespace SweetSugar.Scripts.GUI {public class AnimationEventManager:MonoBehaviour {public void Play(){throw new Exception("Unexpected fallback start");}}}
namespace SweetSugar.Scripts.Level
{
    public class BindingTarget {public Sprite extraObject;public int count;}
    public class BindingLevel {public BindingTarget[] GetTargetContainersForUI()=>new[]{new BindingTarget{extraObject=RoyalAves.UI.RoyalPostman2026.RoyalPostmanRuntimeKit.Load().rocket,count=7},new BindingTarget{extraObject=RoyalAves.UI.RoyalPostman2026.RoyalPostmanRuntimeKit.Load().bomb,count=9}};}
    public static class LoadingManager {public static BindingLevel LoadForPlay(int level)=>new BindingLevel();}
}
namespace SweetSugar.Scripts.AdsEvents
{
    public class AdsManager
    {
        public static AdsManager THIS;public int shown;public bool ready=true;
        public bool GetRewardedUnityAdsReady()=>ready;
        public void ShowRewardedAds(){shown++;}
    }
}
