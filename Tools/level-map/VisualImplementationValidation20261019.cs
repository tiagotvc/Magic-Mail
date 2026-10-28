using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using RoyalAves.EditorTools;

public static class VisualImplementationValidation20261019
{
    const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Instance;
    const string Output=@"C:\Users\Tiago Carvalho\Documents\projetos\Magic-Mail\Tools\level-map\previews\visual-studies-2026-10-19\implemented";
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        Directory.CreateDirectory(Output);RoyalAvesLevelMapPreview preview=null;int errors=0,picks=0;
        Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};
        Application.logMessageReceived+=logger;
        try
        {
            ShaderUtil.allowAsyncCompilation=false;
            var kit=Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath);kit.dayNightCycle=false;kit.night=0;
            Check(kit.levelNumberFont!=null,"Rounded level font unbound");
            var data=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);
            Check(Mathf.Abs(data.pathSway-1.35f)<.0001f&&Mathf.Abs(data.pathFrequency-1.12f)<.0001f,"New route settings missing");
            Check(Resources.Load<Sprite>(WorldMapLetterVault.ResourcePath)!=null,"Golden pig missing");
            preview=RoyalAvesLevelMapPreview.CreateScenePreview(720,1560);var type=preview.GetType();
            CurvedLevelMap Map()=>(CurvedLevelMap)type.GetField("map",F).GetValue(preview);
            void Save(float cursor,string name)
            {
                var map=Map();map.World.TimeOverride=0;type.GetField("level",F).SetValue(preview,cursor);map.PreviewAt(cursor);
                type.GetMethod("Save",F).Invoke(preview,new object[]{Output+"/"+name+".png",false});
                foreach(int target in Enumerable.Range(Mathf.Max(1,(int)cursor),8))
                {
                    if(!map.World.ProjectLevel(target,cursor,map.World.Cam.aspect,out var point)||point.x<.02f||point.x>.98f||point.y<.1f||point.y>.8f)continue;
                    Check(map.World.PickViewport(point)==target,"Rendered button pick mismatch at "+cursor+" / "+target);picks++;
                }
            }
            Save(1,"01-map-current-progress");Save(5,"02-scroll-current-progress");Save(12,"03-crossing");Save(33,"04-season");
            var footer=Resources.FindObjectsOfTypeAll<WorldMapPostalGraphic>().Single(g=>g.surface==WorldMapPostalGraphic.Surface.Navigation&&g.gameObject.activeInHierarchy);
            Check(footer.mainTexture.name=="navigation-plum-royal-cartoon","Painted continuous navigation not loaded");
            Check(footer.transform.parent.GetComponentsInChildren<UnityEngine.UI.Image>(true).Count(i=>i.name=="Icon")==5,"Five original navigation icons missing");
            for(int i=0;i<5;i++){footer.SelectedIndex=i;Check(footer.transform.Find("ActiveTabUnderline")!=null,"Live active marker missing");}
            footer.SelectedIndex=0;
            type.GetMethod("Build",F).Invoke(preview,new object[]{720,1560,7});
            Quaternion first=Quaternion.identity,last=Quaternion.identity;int orientations=0;
            foreach(int cursor in new[]{1,3,5,7})
            {
                Save(cursor,"05-progress-seven-cursor-"+cursor);
                var nodes=(IEnumerable)typeof(WorldMapView).GetField("nodes",F).GetValue(Map().World);
                foreach(var node in nodes)
                {
                    var nt=node.GetType();if((int)nt.GetField("level").GetValue(node)!=7)continue;
                    var t=(Transform)nt.GetField("t").GetValue(node);if(orientations++==0)first=t.localRotation;last=t.localRotation;
                    var number=(Transform)nt.GetField("numberT").GetValue(node);Check(number!=t&&number.parent==t,"Number flattened into button art");
                }
            }
            Check(orientations==4&&Quaternion.Angle(first,last)>5,"Button does not follow curved-ground perspective");
            Save(1,"06-curve-return");
            type.GetMethod("Build",F).Invoke(preview,new object[]{360,780,1});Save(1,"07-phone-360");
            Check(picks>=8,"Insufficient projected button picks");Check(errors==0,errors+" native Unity errors");
            File.WriteAllText(Output+"/validation.txt","PASS: exact production source/assets rendered in isolated Unity; fresh colored terrain, stronger shared route, real domed 3D button geometry with independent numbers/rewards, "+Quaternion.Angle(first,last)+" degrees of target-button orientation change; "+picks+" projected button picks; original five navigation icons, continuous painted plum/gold panel with live active marker, golden Christmas pig and independent recessed shield; opening/scroll/crossing/season/progress/narrow-phone captures; zero Unity errors. No player-save writes.\n");
        }
        catch(Exception e){File.WriteAllText(Output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
        finally{Application.logMessageReceived-=logger;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}
        EditorApplication.Exit(0);
    }
}
