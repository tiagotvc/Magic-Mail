using System.Collections;
using System.Linq;
using RoyalAves.Meta;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.GUI;
using SweetSugar.Scripts.GUI.Boost;
using SweetSugar.Scripts.Level;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    // Presentation adapter: the existing MenuPlay and BoostIcons remain the gameplay authority.
    public sealed class RoyalPostmanLivePlayModal : MonoBehaviour
    {
        Transform view;
        BoostIcon[] backends;
        RoyalPostmanBoosterButton[] buttons;
        RoyalPostmanRuntimeKit kit;
        bool paintReady;
        public static void Install(GameObject menu)
        {
            if(menu==null||menu.GetComponent<RoyalPostmanLivePlayModal>()!=null)return;
            var kit=RoyalPostmanRuntimeKit.Load();if(kit==null||kit.playModal==null)return;
            var adapter=menu.AddComponent<RoyalPostmanLivePlayModal>();adapter.kit=kit;adapter.Build();
            if(menu.activeInHierarchy)adapter.Opened();
        }
        void Build()
        {
            var originals=GetComponentsInChildren<Button>(true);
            var play=originals.FirstOrDefault(b=>b.name=="Play");
            var close=originals.FirstOrDefault(b=>b.name=="Close");
            var free=GetComponentsInChildren<FreeAdBoosterButton>(true).FirstOrDefault(b=>b.gameObject.activeSelf);
            backends=GetComponentsInChildren<BoostIcon>(true).Where(b=>b.gameObject.activeSelf&&
                (b.type==BoostType.Stripes||b.type==BoostType.Packages||b.type==BoostType.MulticolorCandy))
                .OrderBy(b=>b.type==BoostType.Stripes?0:b.type==BoostType.Packages?1:2).Take(3).ToArray();
            // Hide only drawing/raycasting, keeping all original callbacks and animation-driven boost resets alive.
            foreach(var graphic in GetComponentsInChildren<Graphic>(true))graphic.enabled=false;
            view=Instantiate(kit.playModal,transform,false).transform;view.name="RoyalPostmanPlayModal";view.gameObject.AddComponent<RoyalPostmanViewport>();
            foreach(var child in view.GetComponentsInChildren<Transform>(true))child.gameObject.layer=gameObject.layer;
            var playButton=view.Find("PlayButton").GetComponent<Button>();
            playButton.onClick.AddListener(()=>{if(play!=null)play.onClick.Invoke();else GetComponent<AnimationEventManager>()?.Play();});
            view.Find("CloseButton").GetComponent<Button>().onClick.AddListener(()=>{if(close!=null)close.onClick.Invoke();else gameObject.SetActive(false);});
            var freeButton=view.Find("FreeButton").GetComponent<Button>();
            if(free!=null)
            {
                free.BindPresentation(view.Find("FreeButton/RewardBadge/MovingCenter/VisibleReward").GetComponent<Image>(),freeButton,SpriteFor);
                freeButton.onClick.AddListener(free.WatchAdForFreeBooster);
            }
            else freeButton.interactable=false;
            string[] names={"AvailableButton","SelectedButton","LockedButton"};buttons=new RoyalPostmanBoosterButton[3];
            for(int i=0;i<3;i++)
            {
                var node=view.Find(names[i]);buttons[i]=node.GetComponent<RoyalPostmanBoosterButton>();
                if(i>=backends.Length){node.gameObject.SetActive(false);view.Find(new[]{"AvailableTightInset","SelectedTightInset","LockedTightInset"}[i]).gameObject.SetActive(false);continue;}
                var backend=backends[i];node.Find("MovingCenter/BoosterIcon").GetComponent<Image>().sprite=SpriteFor(backend.type);
                node.GetComponent<Button>().onClick.AddListener(backend.ActivateBoost);
            }
            Sync();
        }
        Sprite SpriteFor(BoostType type)=>type==BoostType.Stripes?kit.rocket:type==BoostType.Packages?kit.bomb:type==BoostType.MulticolorCandy?kit.orb:null;
        void OnEnable(){if(view!=null)StartCoroutine(RefreshAfterLoad());}
        IEnumerator RefreshAfterLoad(){yield return null;RefreshGoals();Sync();}
        void LateUpdate(){if(view!=null)Sync();}
        void Sync()
        {
            if(!paintReady)paintReady=RoyalPostmanRuntimeKit.RestorePaint(view,kit.playModal,"RedBody");
            view.Find("Title").GetComponent<Text>().text=$"Nível {PlayerPrefs.GetInt("OpenLevel",1)}";
            for(int i=0;i<backends.Length;i++)
            {
                var backend=backends[i];var locked=backend.transform.Find("Lock");
                var state=locked!=null&&locked.gameObject.activeSelf?RoyalPostmanBoosterButton.VisualState.Locked:
                    backend.check!=null&&backend.check.activeSelf?RoyalPostmanBoosterButton.VisualState.Selected:RoyalPostmanBoosterButton.VisualState.Available;
                buttons[i].SetState(state);buttons[i].SetQuantity(PlayerPrefs.GetInt(backend.type.ToString()));
            }
        }
        public void RefreshGoals()
        {
            var level=LoadingManager.LoadForPlay(Mathf.Max(1,PlayerPrefs.GetInt("OpenLevel",1)));
            var targets=level.GetTargetContainersForUI();
            int count=targets.Length;float step=Mathf.Min(250,780f/Mathf.Max(1,count));
            for(int i=0;i<Mathf.Max(3,count);i++)
            {
                Transform icon=view.Find("Goal"+i),label=view.Find("GoalCount"+i);
                if(icon==null){icon=Instantiate(view.Find("Goal0"),view,false);icon.name="Goal"+i;label=Instantiate(view.Find("GoalCount0"),view,false);label.name="GoalCount"+i;}
                icon.gameObject.SetActive(i<count);label.gameObject.SetActive(i<count);if(i>=count)continue;
                var image=icon.GetComponent<Image>();image.sprite=targets[i].extraObject;image.enabled=image.sprite!=null;
                float x=(i-(count-1)*.5f)*step;
                var iconRect=(RectTransform)icon;iconRect.anchoredPosition=new Vector2(x,275);iconRect.sizeDelta=new Vector2(Mathf.Min(182,step*.85f),180);
                var text=label.GetComponent<Text>();text.text=targets[i].count.ToString();text.rectTransform.anchoredPosition=new Vector2(x,160);
            }
        }
        public void Opened(){RefreshGoals();Sync();}
    }
}
