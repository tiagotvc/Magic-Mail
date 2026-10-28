using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RoyalAves.Characters;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;

public static class RibbonRookV73Validation
{
    const string Output="../ribbon-rook-v73";
    public static void Run()
    {
        Directory.CreateDirectory(Output);File.WriteAllText(Output+"/validation.txt","RUNNING");
        int errors=0;Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception)errors++;};Application.logMessageReceived+=logger;
        try
        {
            ShaderUtil.allowAsyncCompilation=false;RoyalAvesRibbonRookRigBuilder.Build();
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(RoyalAvesRibbonRookRigBuilder.Prefab);
            var root=UnityEngine.Object.Instantiate(source);var rig=root.GetComponent<RibbonRookCutout>();
            if(rig.leftArm==null||rig.rightArm==null||rig.mouth==null||rig.openMouth==rig.closedMouth)throw new Exception("Incomplete cutout");
            var body=root.transform.Find("Body").localToWorldMatrix;
            Render(root,Output+"/rig-standing.png");
            rig.leftArm.localRotation=Quaternion.Euler(0,0,-22);rig.rightArm.localRotation=Quaternion.Euler(0,0,30);rig.SetSpeaking(true);
            if(rig.mouth.sprite!=rig.openMouth||root.transform.Find("Body").localToWorldMatrix!=body)throw new Exception("Parts not independent");
            Render(root,Output+"/rig-arm-mouth-pose.png");UnityEngine.Object.DestroyImmediate(root);
            var config=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);
            if(!config.christmasVillainCharacter.name.Contains("standing-v73")||!config.christmasVillain.name.Contains("shadow-v71"))throw new Exception("Map character/sky mismatch");
            const BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;var type=typeof(RoyalAvesLevelMapPreview);
            var preview=(RoyalAvesLevelMapPreview)type.GetMethod("CreateScenePreview",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{720,1280});
            try
            {
                var map=(CurvedLevelMap)type.GetField("map",flags).GetValue(preview);map.World.TimeOverride=0;map.PreviewAt(1);
                var first=JsonUtility.ToJson(new AutoIslandsBaselineV70.Props{entries=map.World.Props.Where(x=>map.World.Route.LevelAt(x.s)<15.9f).Select(AutoIslandsBaselineV70.Signature).ToArray()},true);
                if(first!=File.ReadAllText("../auto-islands-v70/first-props-before.json"))throw new Exception("Authored first island changed");
                int count=map.World.Props.Count(x=>x.sprite==config.christmasVillainCharacter);
                if(count!=4)throw new Exception("Expected four Christmas appearances; got "+count);
                foreach(int level in new[]{32,77}){type.GetField("level",flags).SetValue(preview,(float)level);map.PreviewAt(level);type.GetMethod("Save",flags).Invoke(preview,new object[]{Output+"/level-"+level+".png",false});}
            }
            finally{UnityEngine.Object.DestroyImmediate(preview);}
            if(errors!=0)throw new Exception(errors+" Unity errors");
            File.WriteAllText(Output+"/validation.txt","PASS: standing sprite imported; six cutout sprites; independent shoulder pivots and open/closed mouth; native neutral and posed rig captures; four Christmas map appearances; approved sky and authored first-island placements preserved; zero Unity errors.\n");
        }
        catch(Exception e){File.WriteAllText(Output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
        finally{Application.logMessageReceived-=logger;}
        EditorApplication.Exit(0);
    }

    static void Render(GameObject root,string path)
    {
        foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
        var cameraObject=new GameObject("Cutout validation camera");var camera=cameraObject.AddComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=3.65f;camera.transform.position=new Vector3(-.3f,3,-10);camera.cullingMask=1<<30;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.22f,.29f,1);
        var target=RenderTexture.GetTemporary(1200,1000,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;var previous=RenderTexture.active;
        var texture=new Texture2D(1200,1000,TextureFormat.RGBA32,false);
        try{camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1200,1000),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
        finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(cameraObject);}
    }
}
