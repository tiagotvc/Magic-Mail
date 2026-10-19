using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PostalHudV55Validation
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/postal-hud-v55";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int errors;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        try
        {
            ShaderUtil.allowAsyncCompilation=false;Directory.CreateDirectory(Output);
            var layout=EditorJsonUtility.ToJson(WorldMapMission100.Current.layout);
            foreach(var size in new[]{new Vector2Int(1080,1920),new Vector2Int(1080,2340),new Vector2Int(750,1334)})
            {
                var preview=RoyalAvesLevelMapPreview.CreateScenePreview(size.x,size.y);
                try
                {
                    var map=(CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview);
                    var content=map.transform.parent;var source=content.Find("HUD");var hud=content.Find("HUDDoMapa");
                    Check(hud.Find("LavenderEnamel")!=null,"New header is missing");
                    Check(hud.GetComponentsInChildren<WorldMapPostalGraphic>().All(g=>!g.raycastTarget),"Decorative artwork blocks input");
                    foreach(var group in new[]{"Lives","Avatar","Coins","Settings"})
                    {
                        int clicks=0;source.Find(group).GetComponentInChildren<Button>(true).onClick.AddListener(()=>clicks++);
                        hud.Find(group).GetComponent<Button>().onClick.Invoke();Check(clicks==1,"Action not relayed once: "+group);
                    }
                    var life=source.Find("Lives/Heart/Count").GetComponent<TMP_Text>();var state=source.Find("Lives/Frame/Label").GetComponent<TMP_Text>();
                    var coins=source.Find("Coins").GetComponentsInChildren<TMP_Text>(true).First();
                    string l=life.text,s=state.text,c=coins.text;life.text="3";state.text="2:34";coins.text="12345";preview.RenderScenePreview();
                    Check(hud.Find("Lives/Value").GetComponent<TMP_Text>().text=="3","Lives binding");
                    Check(hud.Find("Lives/State").GetComponent<TMP_Text>().text=="2:34","Timer binding");
                    Check(hud.Find("Coins/Value").GetComponent<TMP_Text>().text=="12345","Currency binding");
                    life.text=l;state.text=s;coins.text=c;
                    var nav=content.Find("NavegacaoDoMapa");var kit=map.World.CameraModel.Kit;
                    var buttons=nav.GetComponentsInChildren<Button>().OrderBy(b=>b.transform.GetSiblingIndex()).ToArray();
                    int[] order={2,0,1,3,4};Check(buttons.Length==5,"Navigation buttons missing");
                    for(int i=0;i<5;i++)
                    {
                        Check(buttons[i].transform.Find("Icon").GetComponent<Image>().sprite==kit.mapNavIcons[order[i]],"Original icon changed");
                        int deliveries=0;content.Find("Nav/"+buttons[i].name).GetComponent<Button>().onClick.AddListener(()=>deliveries++);
                        buttons[i].onClick.Invoke();Check(deliveries==1,"Navigation relay duplicated");
                        var graphic=nav.GetComponentInChildren<WorldMapPostalGraphic>();Check(graphic.SelectedIndex==i,"Selection does not follow button");
                    }
                    map.SelectNavigationTab(2);map.World.ConfigureDayNight(false,4,0);Save(preview,"day-"+size.x+"x"+size.y);
                    map.World.ConfigureDayNight(false,4,1);Save(preview,"night-"+size.x+"x"+size.y);
                    for(int i=0;i<3;i++){map.Close();map.Open();Check(content.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="HUDDoMapa")==1,"Reopen duplicated HUD");}
                }
                finally{UnityEngine.Object.DestroyImmediate(preview);}
            }
            Check(layout==EditorJsonUtility.ToJson(WorldMapMission100.Current.layout),"Authored map changed");Check(errors==0,"Unity errors: "+errors);
            File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: production HUD renders in Unity editor at 1080x1920, 1080x2340 and 750x1334 in day/night; four top actions and all five navigation actions relay once; live lives/timer/currency bindings; original five sprites retained in order; selection follows every tab; clean repeated open/close; decorative graphics do not intercept input; no authored map changes; zero Unity errors. Physical-device performance and native safe-area insets not measured.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Save(RoyalAvesLevelMapPreview preview,string name)
    {
        preview.RenderScenePreview();var rt=(RenderTexture)preview.SceneTexture;var prior=RenderTexture.active;
        var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());}
        finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(image);}
    }
}
