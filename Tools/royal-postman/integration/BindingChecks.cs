using System;
using System.IO;
using System.Reflection;
using System.Linq;
using RoyalAves.Meta;
using RoyalAves.UI.RoyalPostman2026;
using SweetSugar.Scripts.GUI.Boost;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public static class RoyalPostmanBindingChecks
{
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Invoke(object target,string method)=>target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
    public static void Run()
    {
        var scene=EditorSceneManager.NewPreviewScene();GameObject canvas=null;
        try
        {
            canvas=new GameObject("BindingChecks",typeof(RectTransform),typeof(Canvas));SceneManager.MoveGameObjectToScene(canvas,scene);
            var parent=(RectTransform)canvas.transform;parent.sizeDelta=new Vector2(1080,1920);
            int plays=0,profiles=0,selected=-1,starts=0,closes=0,coinOpens=0,lifeOpens=0,settingsOpens=0;bool covered=false;
            var home=RoyalPostmanLiveHome.Create(parent,()=>plays++,()=>coinOpens++,()=>lifeOpens++,()=>profiles++,()=>settingsOpens++,()=>{},i=>selected=i,()=>covered);
            Require(home!=null,"Runtime home missing");home.transform.Find("LevelButton").GetComponent<Button>().onClick.Invoke();
            home.transform.Find("Avatar").GetComponent<Button>().onClick.Invoke();home.transform.Find("Navigation/Tab4").GetComponent<Button>().onClick.Invoke();
            Require(plays==1&&profiles==1&&selected==4,"Home callbacks failed");
            home.transform.Find("CoinPlus").GetComponent<Button>().onClick.Invoke();
            home.transform.Find("LifePlus").GetComponent<Button>().onClick.Invoke();
            home.transform.Find("SettingsButton").GetComponent<Button>().onClick.Invoke();
            Require(coinOpens==1&&lifeOpens==1&&settingsOpens==1,"Top HUD actions lost callbacks");
            foreach(string name in new[]{"CoinPlus","LifePlus","SettingsButton"})
            {
                var node=home.transform.Find(name);var press=node.GetComponent<RoyalPostmanPaintedButton>();
                Require(node.Find("FixedGoldFrame")==null,"Top circular controls must have no gold frame");
                var shoulder=node.Find("FixedShoulder").GetComponent<RectTransform>();var fixedPosition=shoulder.anchoredPosition;
                var rest=press.movingCenter.anchoredPosition;press.SetPressed(true);
                Require(shoulder.anchoredPosition==fixedPosition&&press.movingCenter.anchoredPosition.y<rest.y,"Circular relief does not move independently");press.SetPressed(false);
            }
            Require(home.GetComponentInChildren<RoyalPostmanAvatar>().portrait.sprite.name=="top-avatar-portrait-v7","New closeup avatar was not bound");
            foreach(string name in new[]{"LevelButton","PetButton"})
            {
                var node=home.transform.Find(name);var rim=node.Find("FixedGoldFrame").GetComponent<Image>();
                var control=node.GetComponent<RoyalPostmanPaintedButton>();var face=control.movingCenter.Find("Face").GetComponent<Image>();
                Require(rim.material.GetFloat("_ButtonPart")==1&&face.material.GetFloat("_ButtonPart")==2,"Gold and colored center are not isolated UI parts");
                var rimPosition=rim.rectTransform.anchoredPosition;var rest=control.movingCenter.anchoredPosition;
                control.SetPressed(true);Require(rim.rectTransform.anchoredPosition==rimPosition&&control.movingCenter.anchoredPosition.y<rest.y,"Independent center press failed");control.SetPressed(false);
            }
            Require(home.transform.Find("LevelButton").GetComponent<RectTransform>().sizeDelta==new Vector2(440,220),"Level button must be taller and less wide");
            home.SelectTab(3);
            Require(home.transform.Find("Navigation/Tab3/ActiveColor").gameObject.activeSelf&&!home.transform.Find("Navigation/Tab2/ActiveColor").gameObject.activeSelf,"Selected color must follow current tab");
            Require(home.transform.Find("Navigation/ContinuousBar")!=null,"Continuous navigation background missing");
            covered=true;home.Refresh();Require(!home.transform.Find("Background").gameObject.activeSelf&&home.transform.Find("Navigation").gameObject.activeSelf,"Menu covering failed");
            covered=false;home.Refresh();Require(home.transform.Find("Background").gameObject.activeSelf,"Return home failed");
            home.SelectTab(3);Require(home.transform.Find("Navigation/Tab3/SelectedLabel").gameObject.activeSelf&&!home.transform.Find("Navigation/Tab2/SelectedLabel").gameObject.activeSelf,"Selection failed");
            var manualIcon=home.transform.Find("Navigation/Tab3/Icon").GetComponent<RectTransform>();
            manualIcon.anchoredPosition=new Vector2(17,29);manualIcon.sizeDelta=new Vector2(113,127);
            home.SelectTab(2);home.SelectTab(3);
            Require(manualIcon.anchoredPosition==new Vector2(17,29)&&manualIcon.sizeDelta==new Vector2(113,127),"Navigation overwrote manually authored icon layout");
            var authoringKit=RoyalPostmanRuntimeKit.Load();var originalHome=authoringKit.home;
            var customTemplate=UnityEngine.Object.Instantiate(originalHome,parent,false);customTemplate.transform.Find("PetButton").gameObject.SetActive(false);
            RoyalPostmanLiveHome customView=null;
            try
            {
                authoringKit.home=customTemplate;
                customView=RoyalPostmanLiveHome.Create(parent,()=>{},()=>{},()=>{},()=>{},()=>{},()=>{},i=>{},()=>covered);
                covered=true;customView.Refresh();covered=false;customView.Refresh();
                Require(!customView.transform.Find("PetButton").gameObject.activeSelf,"Return home overwrote authored visibility");
            }
            finally
            {
                authoringKit.home=originalHome;
                if(customView!=null)UnityEngine.Object.DestroyImmediate(customView.gameObject);
                UnityEngine.Object.DestroyImmediate(customTemplate);
            }
            var menu=new GameObject("MenuPlay",typeof(RectTransform));menu.transform.SetParent(parent,false);menu.SetActive(false);
            var play=Node(menu.transform,"Play").gameObject.AddComponent<Button>();play.onClick.AddListener(()=>starts++);
            var close=Node(menu.transform,"Close").gameObject.AddComponent<Button>();close.onClick.AddListener(()=>closes++);
            var kit=RoyalPostmanRuntimeKit.Load();var shop=Node(menu.transform,"BoostShop").gameObject.AddComponent<BoostShop>();
            shop.boostProducts.Add(new BoostProduct{boostType=BoostType.Stripes,icon=kit.rocket});shop.boostProducts.Add(new BoostProduct{boostType=BoostType.Packages,icon=kit.bomb});
            var free=Node(menu.transform,"Free").gameObject.AddComponent<FreeAdBoosterButton>();typeof(FreeAdBoosterButton).GetField("boostShop",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(free,shop);
            var backends=new BoostIcon[3];var types=new[]{BoostType.Stripes,BoostType.Packages,BoostType.MulticolorCandy};
            for(int i=0;i<3;i++){backends[i]=Node(menu.transform,"Backend"+i).gameObject.AddComponent<BoostIcon>();backends[i].type=types[i];backends[i].check=Node(backends[i].transform,"Check").gameObject;backends[i].check.SetActive(false);Node(backends[i].transform,"Lock").gameObject.SetActive(false);}
            RoyalPostmanLivePlayModal.Install(menu);var adapter=menu.GetComponent<RoyalPostmanLivePlayModal>();adapter.Opened();var view=menu.transform.Find("RoyalPostmanPlayModal");
            view.Find("PlayButton").GetComponent<Button>().onClick.Invoke();view.Find("CloseButton").GetComponent<Button>().onClick.Invoke();Require(starts==1&&closes==1,"Legacy start/close callbacks lost");
            Require(view.Find("GoalCount0").GetComponent<Text>().text=="7"&&view.Find("GoalCount1").GetComponent<Text>().text=="9"&&!view.Find("Goal2").gameObject.activeSelf,"Targets not bound to level data");
            view.Find("AvailableButton").GetComponent<Button>().onClick.Invoke();Invoke(adapter,"Sync");Require(backends[0].calls==1&&view.Find("AvailableButton").GetComponent<RoyalPostmanBoosterButton>().selection.activeSelf,"Booster callback/state failed");
            backends[0].transform.Find("Lock").gameObject.SetActive(true);Invoke(adapter,"Sync");Require(!view.Find("AvailableButton").GetComponent<Button>().interactable,"Locked booster clickable");
            Invoke(free,"OnEnable");typeof(FreeAdBoosterButton).GetField("timer",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(free,100f);Invoke(free,"Update");
            var badge=view.Find("FreeButton/RewardBadge/MovingCenter/VisibleReward").GetComponent<Image>();Require(badge.sprite==kit.bomb,"Badge did not rotate into the new artwork");
            SweetSugar.Scripts.Core.InitScript.Instance=new SweetSugar.Scripts.Core.InitScript();SweetSugar.Scripts.AdsEvents.AdsManager.THIS=new SweetSugar.Scripts.AdsEvents.AdsManager();
            var freeButton=view.Find("FreeButton").GetComponent<Button>();freeButton.onClick.Invoke();freeButton.onClick.Invoke();Invoke(free,"Update");
            Require(SweetSugar.Scripts.Core.InitScript.Instance.prepared==BoostType.Packages&&SweetSugar.Scripts.AdsEvents.AdsManager.THIS.shown==1&&!freeButton.interactable&&badge.sprite==kit.bomb,"Reward snapshot / duplicate protection failed");
            SweetSugar.Scripts.Core.InitScript.Instance.completed();Require(freeButton.interactable,"Reward completion not connected");
            var file=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Tools/royal-postman/integration/BINDING-CHECKS.txt");Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllText(file,"PASS home play/profile/navigation callbacks\nPASS covered menus and return home\nPASS selected navigation labels\nPASS legacy Play/Close callbacks\nPASS dynamic goal sprites/counts\nPASS booster activation/selection/lock\nPASS real FreeAdBoosterButton with isolated ad/wallet facades: rotating artwork, captured item, one ad per click, freeze while pending, completion re-enable\nNo player progress or wallet writes. This is a binding test, not an ad SDK/network completion test.\n");Debug.Log("ROYAL_POSTMAN_BINDINGS_OK");
        }
        finally{if(canvas!=null)UnityEngine.Object.DestroyImmediate(canvas);EditorSceneManager.ClosePreviewScene(scene);}
    }
    static RectTransform Node(Transform parent,string name){var node=new GameObject(name,typeof(RectTransform));node.transform.SetParent(parent,false);return (RectTransform)node.transform;}
}
