using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RoyalAves.EditorTools;
using RoyalAves.UI.RoyalPostman2026;
using RoyalAves.Meta;

public static class IncubatorControlsV70Review
{
    const string Output = IncubatorControlsV70Build.Output;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            foreach(string name in new[]{"incubate-fixed-rim","incubate-raised-face","timer-track"})
            {
                string path=IncubatorControlsV70Build.Art+name+".png";
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                Check(texture.mipmapCount>1&&texture.filterMode==FilterMode.Trilinear&&importer.textureCompression==TextureImporterCompression.Uncompressed,"Quality import: "+name);
            }
            using(var d=new RoyalPostmanScreenDocument(RoyalPostmanRuntimeKit.Load(),"pets",720,1280))
            {
                var view=d.Root.GetComponent<RoyalPostmanPetsScreen>();
                long now=1700000000;int saves=0,starts=0;
                var stock=new PetsIncubationState{commonEggs=2,rareEggs=1,epicEggs=1,legendaryEggs=1,accelerationItems=new[]{1,0,0,0}};
                view.Initialize(()=>{},i=>{});
                view.UseItemRequested+=(i,id)=>starts++;
                Action bind=()=>{view.BindInventory(stock,()=>saves++,cost=>false,()=>0,()=>now);view.RefreshAvailability(100);};
                bind();view.ShowCurrent(false);d.Render();
                Check(view.incubators.Length==4,"Four machines lost");
                for(int i=0;i<4;i++)
                {
                    view.SelectIncubator(i);d.Render();var slot=view.incubators[i];
                    Check(slot.useItem.GetComponent<Image>().sprite.name=="incubate-fixed-rim","Fixed rim missing");
                    var moving=(RectTransform)slot.useItem.transform.Find("MovingCenter");
                    Check(moving.Find("RaisedFace").GetComponent<Image>().sprite.name=="incubate-raised-face","Separate face missing");
                    Check(moving.Find("PawIcon").GetComponent<Image>().sprite!=null,"Separate paw missing");
                    Check(!moving.Find("Label").gameObject.activeSelf,"Placeholder label remains");
                    Check(slot.useItem.targetGraphic.transform.IsChildOf(moving),"Disabled tint targets fixed rim");
                    d.Export(Output+"/rarity-"+i+".png");
                }
                view.SelectIncubator(0);d.Render();
                var first=view.incubators[0];var press=first.useItem.GetComponent<RoyalPostmanPaintedButton>();
                var center=press.movingCenter;var rest=center.anchoredPosition;var scale=center.localScale;
                var rim=(RectTransform)first.useItem.transform;var rimPosition=rim.anchoredPosition;var rimScale=rim.localScale;
                var eventGO=new GameObject("V70Events",typeof(EventSystem));
                try
                {
                    var events=eventGO.GetComponent<EventSystem>();
                    var data=new PointerEventData(events){button=PointerEventData.InputButton.Left,position=d.Camera.WorldToScreenPoint(rim.position)};
                    var hits=new System.Collections.Generic.List<RaycastResult>();
                    var raycaster=d.Root.GetComponentInParent<GraphicRaycaster>();raycaster.Raycast(data,hits);
                    Check(hits.Exists(h=>h.gameObject==first.useItem.gameObject),"Paw button is not raycastable");
                    press.OnPointerDown(data);
                    Check(center.anchoredPosition==rest+Vector2.down*8&&center.localScale==scale*.96f,"Raised face does not sink on press");
                    Check(rim.anchoredPosition==rimPosition&&rim.localScale==rimScale,"Fixed rim moved on press");
                    d.Export(Output+"/pressed.png");
                    press.OnPointerUp(data);
                    Check(center.anchoredPosition==rest&&center.localScale==scale,"Face does not return on release");
                    first.useItem.onClick.Invoke();
                    Check(stock.commonEggs==1&&stock.Occupied(0)&&starts==1&&saves==1,"Paw click changed stock/start behavior");
                    Check(!first.useItem.interactable,"Incubating paw still enabled");
                    var disabledRest=center.anchoredPosition;press.OnPointerDown(data);
                    Check(center.anchoredPosition==disabledRest,"Disabled button accepts press");
                }
                finally{UnityEngine.Object.DestroyImmediate(eventGO);}
                now+=1800;bind();d.Render();
                Check(first.progress.text.Contains("00:30:00"),"Time bar is not showing remaining time");
                Check(Mathf.Abs(first.progressFill.fillAmount-.5f)<.001f,"Elapsed progress incorrect");
                var clip=first.progressFill.GetComponent<RoyalPostmanProgressClip>();clip.Refresh();
                var parent=(RectTransform)clip.clipping.parent;
                Check(Mathf.Abs(clip.clipping.rect.width-parent.rect.width*.5f)<.1f,"Rounded fill width mismatch");
                d.Export(Output+"/running.png");
                var bar=first.progress.transform.parent as RectTransform;
                var originalSize=bar.sizeDelta;
                bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,930);Canvas.ForceUpdateCanvases();clip.Refresh();
                Check(Mathf.Abs(clip.clipping.rect.width-parent.rect.width*.5f)<.1f,"Resized bar fill no longer follows parent");
                bar.sizeDelta=originalSize;Canvas.ForceUpdateCanvases();clip.Refresh();
                first.progressFill.fillAmount=0;clip.Refresh();Check(clip.clipping.rect.width<.01f,"Empty bar not empty");
                first.progressFill.fillAmount=1;clip.Refresh();Check(Mathf.Abs(clip.clipping.rect.width-parent.rect.width)<.01f,"Full bar not full");
                Check(RoyalPostmanPetsScreen.FormatIncubationTime(172800)=="48:00:00","48h timer changed");
                d.Resize(390,844);bind();d.Export(Output+"/running-390.png");
                var legacyGO=new GameObject("LegacyClip",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(RoyalPostmanProgressClip));
                try
                {
                    var legacy=legacyGO.GetComponent<RoyalPostmanProgressClip>();legacy.valueSource=legacyGO.GetComponent<Image>();
                    legacy.clipping=(RectTransform)legacyGO.transform;legacy.fullWidth=100;legacy.valueSource.fillAmount=.5f;legacy.Refresh();
                    Check(!legacy.useParentWidth&&Mathf.Abs(legacy.clipping.rect.width-50)<.01f,"Legacy progress behavior changed");
                }
                finally{UnityEngine.Object.DestroyImmediate(legacyGO);}
            }
            File.WriteAllText(Output+"/review.txt","PASS: four editable fixed-rim/raised-face/paw assemblies; fixed rim stays still during8px/.96 press; release/reset; disabled guard; native raycast; one egg/start/save per click; remaining time and48h; rounded fill0/50/100%; bar resize; old clip behavior;720x1280 and390x844 captures.\n");
            EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Output+"/review.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
