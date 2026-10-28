using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RoyalAves.Meta;
using RoyalAves.UI.RoyalPostman2026;

namespace RoyalAves.EditorTools
{
    public sealed class RoyalPostmanHatcheryTestWindow : EditorWindow
    {
        RoyalPostmanScreenDocument document;
        RoyalPostmanPetsScreen view;
        PetHatchCatalog catalog;
        PetsIncubationState state;
        double began,nextFrame;
        long offset;
        int saves,gold;
        string error;
        long Now=>1700000000+(long)(EditorApplication.timeSinceStartup-began)+offset;
        [MenuItem("Royal Postman/Testar incubadora")]
        public static void Open(){var w=GetWindow<RoyalPostmanHatcheryTestWindow>("Testar incubadora");w.minSize=new Vector2(430,720);w.Show();}
        void OnEnable(){EditorApplication.update+=Tick;}
        void OnDisable(){EditorApplication.update-=Tick;Dispose();}
        void Dispose(){document?.Dispose();document=null;view=null;if(catalog!=null)DestroyImmediate(catalog);catalog=null;}
        void Begin()
        {
            Dispose();error=null;
            try
            {
                document=new RoyalPostmanScreenDocument(RoyalPostmanRuntimeKit.Load(),"pets",540,960){ReadOnly=true};view=document.Root.GetComponent<RoyalPostmanPetsScreen>();view.Initialize(Close,i=>{});
                catalog=CreateInstance<PetHatchCatalog>();catalog.hideFlags=HideFlags.HideAndDontSave;catalog.entries=new PetHatchCatalog.Entry[4];var previewPortrait=view.currentPanel.GetComponent<RoyalPostmanCurrentPet>().hero.sprite;
                for(int i=0;i<4;i++)catalog.entries[i]=new PetHatchCatalog.Entry{eggKind=i,petId="preview-only-"+i,portugueseName="Prévia de mascote",englishName="Pet preview",rarity=i,portrait=previewPortrait};
                view.hatchery.ConfigureSimulation(catalog);view.incubationRules=new PetsIncubationRules{commonEggSeconds=6,rareEggSeconds=8,epicEggSeconds=10,legendaryEggSeconds=12};state=new PetsIncubationState{commonEggs=3,rareEggs=3,epicEggs=3,legendaryEggs=3,accelerationItems=new[]{3,0,0,0}};
                began=EditorApplication.timeSinceStartup;offset=0;saves=0;gold=10000;view.BindInventory(state,()=>saves++,cost=>{if(gold<cost)return false;gold-=cost;return true;},()=>gold,()=>Now);view.RefreshAvailability(100);view.ShowCurrent(false);document.Render();
            }
            catch(Exception e){error=e.Message;Debug.LogException(e);Dispose();}
        }
        void Tick()
        {
            if(EditorApplication.isPlaying||document==null||EditorApplication.timeSinceStartup<nextFrame)return;nextFrame=EditorApplication.timeSinceStartup+1d/30;
            view.hatchery.Refresh();view.hatchery.presentation?.Tick();view.hatchery.Reveal?.Tick();if(view.hatchery.particles.IsRunning)view.hatchery.particles.Sample(EditorApplication.timeSinceStartup);document.Render();Repaint();
        }
        void OnGUI()
        {
            if(EditorApplication.isPlaying){EditorGUILayout.HelpBox("Saia do Play para usar esta simulação isolada.",MessageType.Info);return;}
            using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if(GUILayout.Button("Reiniciar teste",EditorStyles.toolbarButton))Begin();
                if(GUILayout.Button("Concluir atual",EditorStyles.toolbarButton)&&state!=null){offset+=state.Remaining(view.SelectedIncubator,Now);view.hatchery.Refresh();}
                GUILayout.FlexibleSpace();GUILayout.Label("Simulação · 6–12 segundos",EditorStyles.miniLabel);
            }
            EditorGUILayout.HelpBox("Ovos e mascotes provisórios. Este teste não altera seu inventário nem o prefab.",MessageType.None);
            if(document==null){if(error!=null)EditorGUILayout.HelpBox(error,MessageType.Error);if(GUILayout.Button("Iniciar simulação"))Begin();return;}
            var area=GUILayoutUtility.GetRect(100,10000,100,10000,GUILayout.ExpandWidth(true),GUILayout.ExpandHeight(true));float scale=Mathf.Min(area.width/document.Width,area.height/document.Height);var rect=new Rect(area.center.x-document.Width*scale*.5f,area.center.y-document.Height*scale*.5f,document.Width*scale,document.Height*scale);GUI.DrawTexture(rect,document.Texture,ScaleMode.StretchToFill,false);
            var e=Event.current;if(e.type==EventType.MouseDown&&e.button==0&&rect.Contains(e.mousePosition))
            {
                var position=new Vector2((e.mousePosition.x-rect.x)/rect.width*document.Width,(1-(e.mousePosition.y-rect.y)/rect.height)*document.Height);var data=new PointerEventData(EventSystem.current){position=position};var hits=new List<RaycastResult>();document.Root.GetComponentInParent<GraphicRaycaster>().Raycast(data,hits);
                if(hits.Count>0){var button=hits[0].gameObject.GetComponentInParent<Button>();if(button!=null&&button.IsInteractable())button.onClick.Invoke();}e.Use();Repaint();
            }
        }
    }
}
