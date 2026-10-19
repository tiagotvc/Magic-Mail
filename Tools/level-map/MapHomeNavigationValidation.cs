using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class MapHomeNavigationValidation
{
    const BindingFlags Private=BindingFlags.Static|BindingFlags.NonPublic;
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/navigation";
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static int errors,materialWarnings;
    static void OnLog(string message,string stack,LogType type)
    {
        if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;
        if(message.Contains("Instantiating material due to calling renderer.material"))materialWarnings++;
    }
    public static void Run()
    {
        Application.logMessageReceived+=OnLog;
        try
        {
            Directory.CreateDirectory(Output);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var lobby=new GameObject("RoyalAvesLobby",typeof(RectTransform),typeof(Canvas));
            EditorSceneManager.SaveScene(scene,"Assets/SimulatorPreviewValidation.unity");
            var bridge=typeof(RoyalAvesMapSimulatorPreview);
            bridge.GetField("nextFrame",Private).SetValue(null,0d);
            bridge.GetMethod("Update",Private).Invoke(null,null);
            var overlay=(GameObject)bridge.GetField("overlay",Private).GetValue(null);
            Check(overlay!=null,"Simulator did not create the map preview");
            Check((overlay.hideFlags&HideFlags.DontSaveInEditor)!=0,"Preview could be serialized into the user's scene");
            Check(!scene.isDirty,"Preview dirtied the open scene");
            var renderer=(RoyalAvesLevelMapPreview)bridge.GetField("renderer",Private).GetValue(null);
            var instance=BindingFlags.Instance|BindingFlags.NonPublic;
            var map=(CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",instance).GetValue(renderer);
            Check(map!=null&&map.Visible&&map.World!=null,"Simulator does not display the production world");
            Check(!map.World.Cam.enabled,"Editor preview camera is rendering continuously");
            Check(Mathf.Abs(map.World.CameraModel.Kit.illustratedSize-4)<.001f,"Map was not zoomed in");
            var content=map.transform.parent;
            Check(!content.Find("Room").gameObject.activeSelf,"Old agency room is visible");
            Check(!content.Find("HUD").gameObject.activeSelf&&!content.Find("Nav").gameObject.activeSelf,"Retired agency HUD is visible");
            var nav=content.Find("NavegacaoDoMapa");
            var label=nav.GetComponentsInChildren<TextMeshProUGUI>().Single(x=>x.text=="Mapa");
            Check(label.text=="Mapa","Central tab still says Agency");
            Check(nav.Find("FaixaDoMapa")!=null,"Map navigation style missing");
            var liveKit=map.World.CameraModel.Kit;
            Check(liveKit.mapNavIcons.Length==5&&liveKit.mapNavIcons.All(x=>x!=null),"New icon set not bound");
            Check(liveKit.expressiveTrees.Length==4&&liveKit.expressiveTrees.All(x=>x!=null),"New trees not bound");
            Check(map.World.Props.Any(p=>liveKit.expressiveTrees.Contains(p.sprite)),"Approved trees are absent from the actual saved map");
            Check(map.World.Props.Any(p=>p.sprite==liveKit.expressivePostOffice),"Approved buildings are absent from the actual saved map");
            var firstTab=nav.GetComponentsInChildren<Button>().OrderBy(x=>x.transform.GetSiblingIndex()).First();
            Check(firstTab.transform.Find("Label").GetComponent<TextMeshProUGUI>().text=="Mapa","Map is not the first tab");
            var eventTab=nav.GetComponentsInChildren<Button>().Single(x=>x.name=="Eventos");
            var deliveries=0;content.Find("Nav/Eventos").GetComponent<Button>().onClick.AddListener(()=>deliveries++);
            eventTab.onClick.Invoke();
            Check(deliveries==1,"Map menu action was lost or delivered twice");
            Check(map.Visible&&!content.Find("HUD").gameObject.activeSelf&&!content.Find("Nav").gameObject.activeSelf,"A tab reopened the retired UI");
            map.SelectNavigationTab(2);
            var oldCoins=content.Find("HUD/Coins").GetComponentsInChildren<TMP_Text>(true).First();
            oldCoins.text="1234";renderer.RenderScenePreview();
            Check(content.Find("HUDDoMapa/Coins").GetComponentsInChildren<TMP_Text>(true).Any(x=>x.text=="1234"),"Live HUD stopped updating");
            oldCoins.text="0";
            for(var i=0;i<5;i++)
            {
                map.Close();map.Open();
                Check(!content.Find("HUD").gameObject.activeSelf&&!content.Find("Nav").gameObject.activeSelf,"Retired HUD returned after navigation");
                nav=content.Find("NavegacaoDoMapa");label=nav.GetComponentsInChildren<TextMeshProUGUI>().Single(x=>x.text=="Mapa");
                Check(label.text=="Mapa","Home label was lost after reopening");
                Check(nav.GetComponentsInChildren<WorldMapPanelGraphic>().Length==1,"Duplicate navigation panels");
            }
            var input=map.GetComponentsInChildren<CurvedMapInput>().Single(x=>x.name=="Mundo3D");
            var raycaster=content.GetComponentInParent<Canvas>().GetComponent<GraphicRaycaster>();
            Check(raycaster!=null,"No GraphicRaycaster on map canvas");
            var pointer=new PointerEventData(null){button=PointerEventData.InputButton.Left,position=new Vector2(270,480),
                pointerPressRaycast=new RaycastResult{module=raycaster,gameObject=input.gameObject},eligibleForClick=true};
            map.PreviewAt(1);
            Check(map.World.ProjectLevel(1,map.Cursor,map.World.Cam.aspect,out var token),"First level cannot be projected");
            var viewport=(RectTransform)input.transform;
            pointer.position=RectTransformUtility.WorldToScreenPoint(raycaster.eventCamera,viewport.TransformPoint(new Vector2(viewport.rect.xMin+token.x*viewport.rect.width,viewport.rect.yMin+token.y*viewport.rect.height)));
            input.OnPointerClick(pointer);
            var selectedBeforeDrag=(int)typeof(RoyalAvesLevelMapPreview).GetField("selections",instance).GetValue(renderer);
            Check(selectedBeforeDrag==1,"Tapping a level did not call the existing selection callback");
            input.OnInitializePotentialDrag(pointer);input.OnBeginDrag(pointer);
            pointer.delta=new Vector2(0,-96);pointer.position+=pointer.delta;
            input.OnDrag(pointer);
            Check(map.Cursor>1.2f&&!pointer.eligibleForClick,"Left-button drag did not scroll the world");
            input.OnPointerClick(pointer);
            Check((int)typeof(RoyalAvesLevelMapPreview).GetField("selections",instance).GetValue(renderer)==selectedBeforeDrag,"Drag opened a level");
            input.OnEndDrag(pointer);
            var dragEnd=map.Cursor;
            map.PreviewMotion(.04f);
            Check(map.Cursor>dragEnd,"Drag did not retain momentum");
            for(var i=0;i<80;i++)map.PreviewMotion(.05f);
            var wheelStart=map.Cursor;input.OnScroll(new PointerEventData(null){scrollDelta=new Vector2(0,2)});
            for(var i=0;i<12;i++)map.PreviewMotion(.05f);
            Check(map.Cursor<wheelStart,"Wheel did not scroll back");
            map.PreviewAt(1);input.OnBeginDrag(pointer);map.DragByViewport(100,.02f);input.OnEndDrag(pointer);
            Check(map.Cursor==1,"Scrolling passed the beginning of the map");
            input.OnBeginDrag(pointer);map.DragByViewport(-100,.02f);input.OnEndDrag(pointer);
            Check(map.Cursor<map.World.Route.LevelSlots+5,"Scrolling passed the end of the map");
            map.PreviewAt(1);
            Check(RoyalAvesMapSimulatorPreview.HandleInput(EventType.MouseDown,0,new Vector2(270,400),Vector2.zero,960),"Simulator did not capture pointer down");
            RoyalAvesMapSimulatorPreview.HandleInput(EventType.MouseDrag,0,new Vector2(270,500),new Vector2(0,100),960);
            Check(map.Cursor>1.2f,"Simulator edit-mode drag did not move the map");
            RoyalAvesMapSimulatorPreview.HandleInput(EventType.MouseUp,0,new Vector2(270,300),Vector2.zero,960);
            bridge.GetMethod("Cleanup",Private).Invoke(null,null);
            Check(Resources.FindObjectsOfTypeAll<RoyalAves.Meta.WorldMap.WorldMapView>().Length==0,"World/camera leaked after closing preview");
            bridge.GetField("nextFrame",Private).SetValue(null,0d);
            bridge.GetMethod("Update",Private).Invoke(null,null);
            overlay=(GameObject)bridge.GetField("overlay",Private).GetValue(null);
            renderer=(RoyalAvesLevelMapPreview)bridge.GetField("renderer",Private).GetValue(null);
            Check(overlay!=null&&renderer!=null,"Preview did not reopen after cleanup");
            renderer.RenderScenePreview();
            typeof(RoyalAvesLevelMapPreview).GetMethod("Save",instance).Invoke(renderer,new object[]{Path.Combine(Output,"simulator-map.png"),false});
            var islands=UnityEngine.Object.Instantiate(Resources.Load<RoyalAves.Meta.WorldMap.WorldMapIslands>(RoyalAves.Meta.WorldMap.WorldMapIslands.ResourcePath));
            typeof(RoyalAvesLevelMapPreview).GetField("captureIslands",instance).SetValue(renderer,islands);
            foreach(var floor in new[]{RoyalAves.Meta.WorldMap.WorldMapGroundStyle.Lavender,RoyalAves.Meta.WorldMap.WorldMapGroundStyle.Volcanic})
            {
                islands.islands[0].groundStyle=floor;islands.islands[0].customColors=false;
                typeof(RoyalAvesLevelMapPreview).GetMethod("Build",instance).Invoke(renderer,new object[]{540,960,1});
                typeof(RoyalAvesLevelMapPreview).GetMethod("Save",instance).Invoke(renderer,new object[]{Path.Combine(Output,"floor-"+floor.ToString().ToLowerInvariant()+".png"),false});
            }
            var saved="Assets/SimulatorPreviewValidation.unity";
            EditorSceneManager.SaveScene(scene,saved);
            Check(!File.ReadAllText(saved).Contains("Preview temporário do mapa"),"Temporary preview was saved");
            bridge.GetMethod("Cleanup",Private).Invoke(null,null);
            UnityEngine.Object.DestroyImmediate(islands);
            Check(overlay==null,"Preview overlay leaked");
            Check(Resources.FindObjectsOfTypeAll<RoyalAvesLevelMapPreview>().Length==0,"Preview renderer leaked");
            Check(materialWarnings==0,"TMP instantiated materials during preview");
            Check(errors==0,"Unity logged an error during validation");
            UnityEngine.Object.DestroyImmediate(lobby);
            File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: closer camera; production input relay scrolls on left drag and wheel; inertia; drag suppresses level selection; route bounds; Simulator edit-mode drag; map-owned HUD mirrors live counters; five new navigation icons; Map is the first tab; menu actions relay once; retired HUD and navigation stay hidden through five reopen cycles; one styled panel; approved trees and buildings appear in the saved map; preview camera is manual; no dirty/serialized overlay; world/camera/renderer cleanup; zero material-instantiation warnings and zero Unity errors. Native captures: green, lavender and volcanic floors. Full runtime assembly compiled separately.");
            EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
