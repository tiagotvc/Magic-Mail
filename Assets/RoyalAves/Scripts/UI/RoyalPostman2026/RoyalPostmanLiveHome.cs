using System;
using System.Collections.Generic;
using RoyalAves.Meta;
using SweetSugar.Scripts.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    public sealed class RoyalPostmanLiveHome : MonoBehaviour
    {
        Func<bool> covered;
        LobbyFeaturesConfig features;
        Sprite defaultPortrait;
        bool? wasCovered;
        int lastAvatar=-1;
        bool paintReady;
        readonly Dictionary<Transform,bool> authoredVisibility=new Dictionary<Transform,bool>();
        public static RoyalPostmanLiveHome Create(RectTransform parent,Action play,Action coins,Action lives,Action profile,Action settings,Action pet,Action<int> tab,Func<bool> covered)
        {
            var kit=RoyalPostmanRuntimeKit.Load();if(kit==null||kit.home==null)return null;
            var root=kit.InstantiateScreen("home",parent);root.name="RoyalPostmanHome";
            var home=root.AddComponent<RoyalPostmanLiveHome>();home.covered=covered;home.features=LobbyFeaturesConfig.Instance;
            foreach(Transform child in root.transform)home.authoredVisibility[child]=child.gameObject.activeSelf;
            home.Bind("LevelButton",play);home.Bind("CoinPlus",coins);home.Bind("LifePlus",lives);home.Bind("Avatar",profile);
            home.Bind("SettingsButton",settings);home.Bind("PetButton",pet);
            home.Bind("CoinCounter",coins);home.Bind("Coin",coins);home.Bind("LifeCounter",lives);home.Bind("Life",lives);
            for(int i=0;i<5;i++){int index=i;home.Bind("Navigation/Tab"+i,()=>tab(index));}
            home.defaultPortrait=root.GetComponentInChildren<RoyalPostmanAvatar>(true).portrait.sprite;
            home.Refresh();return home;
        }
        void Bind(string path,Action action)
        {
            var node=transform.Find(path);var button=node.GetComponent<Button>();
            if(button==null)
            {
                var hit=node.GetComponent<Graphic>();
                if(hit==null){var image=node.gameObject.AddComponent<Image>();image.color=Color.clear;hit=image;}
                hit.raycastTarget=true;button=node.gameObject.AddComponent<Button>();button.targetGraphic=hit;button.transition=Selectable.Transition.None;
            }
            button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>action?.Invoke());
        }
        void LateUpdate()=>Refresh();
        public void Refresh()
        {
            if(!paintReady)paintReady=RoyalPostmanRuntimeKit.RestorePaint(transform,RoyalPostmanRuntimeKit.Load()?.home,"Background");
            SetText("CoinValue",CoinFormat.Short(InitScript.Gems));
            SetText("LivesValue",InitScript.lifes.ToString());
            int cap=InitScript.Instance!=null?InitScript.Instance.CapOfLife:5;
            int seconds=Mathf.Max(0,Mathf.CeilToInt(InitScript.RestLifeTimer));
            SetText("LifeStatus",InitScript.lifes>=cap?"Cheio":$"{seconds/60:00}:{seconds%60:00}");
            int level=MetaProgress.NextLevel;bool exists=RoyalAves.Meta.WorldMap.WorldMapLevelCatalog.Exists(level);
            SetText("LevelButton/MovingCenter/LevelLabel",exists?$"Nível {level}":"Em breve");transform.Find("LevelButton").GetComponent<Button>().interactable=exists;
            if(lastAvatar!=MetaProgress.Avatar)
            {
                lastAvatar=MetaProgress.Avatar;var portrait=lastAvatar>0&&features!=null&&lastAvatar<features.avatars.Count?features.avatars[lastAvatar]:defaultPortrait;
                GetComponentInChildren<RoyalPostmanAvatar>(true).SetPortrait(portrait!=null?portrait:defaultPortrait);
            }
            bool hide=covered!=null&&covered();
            if(hide!=wasCovered)
            {
                wasCovered=hide;
                foreach(Transform child in transform)
                    if(child.name!="Navigation")child.gameObject.SetActive(!hide&&(!authoredVisibility.TryGetValue(child,out var visible)||visible));
            }
        }
        void SetText(string path,string value){var text=transform.Find(path).GetComponent<Text>();if(text.text!=value)text.text=value;}
        public void SelectTab(int index)
        {
            transform.Find("Navigation").GetComponent<RoyalPostmanNavigation>().Select(index<0?2:index);
        }
    }
}
