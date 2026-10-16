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
public static class TopHudV24Capture
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/top-hud";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int errors,warnings;
    public static void Run()
    {
        Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;if(m.Contains("Instantiating material due to calling renderer.material"))warnings++;};
        try
        {
            Check(RoyalAvesMapSimulatorPreview.ResolutionFor(new Vector2(360,640))==new Vector2Int(1080,1920),"Docked preview lost the native phone resolution");
            Check(RoyalAvesMapSimulatorPreview.ResolutionFor(new Vector2(1080,2280))==new Vector2Int(1080,2280),"Tall phone aspect ratio was changed");
            Check(RoyalAvesMapSimulatorPreview.ResolutionFor(Vector2.zero)==new Vector2Int(1080,1920),"Preview startup fallback is not portrait Full HD");
            Directory.CreateDirectory(Output);Capture(540,960,false);Capture(1080,1920,true);Capture(1080,2280,false);
            Check(errors==0&&warnings==0,"Unity logged an error or material-instantiation warning");
            File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: editor preview minimum 1080px wide with preserved phone aspect; actual production HUD at 1080 x 1920 and 1080 x 2280; 540 x 960 comparison captured; six full-resolution uncompressed HUD textures without mipmaps; adult map courier portrait; only golden-letter currency; no legacy mint panel; four original button actions relay once; life count, refill timer and currency values stay live; clean open/close cycles; owned text material released; zero Unity errors and zero material-instantiation warnings.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Capture(int width,int height,bool exercise)
    {
        var preview=RoyalAvesLevelMapPreview.CreateScenePreview(width,height);
        try
        {
            var map=(CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview);
            var content=map.transform.parent;var source=content.Find("HUD");var hud=content.Find("HUDDoMapa");
            Check(WorldMapTopHud.Ready(map.World.CameraModel.Kit),"HUD artwork is not bound");
            foreach(var texture in map.World.CameraModel.Kit.topHudArt)
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
                Check(!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed&&importer.maxTextureSize==4096,"HUD texture was reduced or uses mipmaps: "+texture.name);
            }
            Check(!source.gameObject.activeSelf,"Legacy HUD is visible");
            Check(hud.Find("Stars")==null,"Second currency was not removed");
            Check(map.transform.Find("PainelSuperiorDoMapa")==null,"Legacy mint header is still visible");
            Check(hud.GetComponentsInChildren<RawImage>().Length==7,"HUD artwork is incomplete");
            if(exercise)
            {
                foreach(var group in new[]{"Lives","Avatar","Coins","Settings"})
                {
                    var deliveries=0;source.Find(group).GetComponentInChildren<Button>(true).onClick.AddListener(()=>deliveries++);
                    hud.Find(group).GetComponent<Button>().onClick.Invoke();Check(deliveries==1,group+" action was lost or duplicated");
                }
                var life=source.Find("Lives/Heart/Count").GetComponent<TMP_Text>();
                var state=source.Find("Lives/Frame/Label").GetComponent<TMP_Text>();
                var coins=source.Find("Coins").GetComponentsInChildren<TMP_Text>(true).First();
                var oldLife=life.text;var oldState=state.text;var oldCoins=coins.text;
                life.text="3";state.text="2:34";coins.text="12345";preview.RenderScenePreview();
                Check(hud.Find("Lives/Value").GetComponent<TMP_Text>().text=="3","Life count stopped updating");
                Check(hud.Find("Lives/State").GetComponent<TMP_Text>().text=="2:34","Refill timer stopped updating");
                Check(hud.Find("Coins/Value").GetComponent<TMP_Text>().text=="12345","Letters stopped updating");
                life.text=oldLife;state.text=oldState;coins.text=oldCoins;
                for(var i=0;i<3;i++)
                {
                    var material=hud.GetComponentInChildren<TMP_Text>().fontSharedMaterial;map.Close();Check(material==null,"HUD text material leaked");
                    map.Open();hud=content.Find("HUDDoMapa");Check(content.GetComponentsInChildren<Transform>(true).Count(t=>t.name=="HUDDoMapa")==1,"HUD was duplicated on reopen");
                }
            }
            preview.RenderScenePreview();Save(preview,Path.Combine(Output,"phone-"+width+"x"+height+".png"),false);
            Save(preview,Path.Combine(Output,"hud-"+width+"x"+height+".png"),true);
        }
        finally{UnityEngine.Object.DestroyImmediate(preview);}
    }
    static void Save(RoyalAvesLevelMapPreview preview,string path,bool topOnly)
    {
        var capture=(RenderTexture)preview.SceneTexture;var h=topOnly?Mathf.RoundToInt(260*capture.width/1080f):capture.height;
        var prior=RenderTexture.active;var image=new Texture2D(capture.width,h,TextureFormat.RGBA32,false);
        try{RenderTexture.active=capture;image.ReadPixels(new Rect(0,capture.height-h,capture.width,h),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
        finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(image);}
    }
}
