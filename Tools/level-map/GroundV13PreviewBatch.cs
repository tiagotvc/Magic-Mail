using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RoyalAves.Meta;
using RoyalAves.EditorTools;

// Visual comparison using the same authored layout and native production renderer.
public static class GroundV13PreviewBatch
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    public static void Execute(bool before)
    {
        var output=Path.Combine("C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/v13",before?"before":"after");
        Directory.CreateDirectory(output);
        var window=ScriptableObject.CreateInstance<RoyalAvesLevelMapPreview>();
        var type=typeof(RoyalAvesLevelMapPreview);
        try
        {
            foreach(var shot in new[]{("entrance",1f,1,720,1600),("route",8f,8,720,1600),("crossing",19f,19,720,1600),("compact",1f,1,540,960)})
            {
                type.GetField("level",Private).SetValue(window,shot.Item2);
                type.GetMethod("Build",Private).Invoke(window,new object[]{shot.Item4,shot.Item5,shot.Item3});
                var map=(CurvedLevelMap)type.GetField("map",Private).GetValue(window);
                map.World.TimeOverride=1;
                type.GetMethod("Save",Private).Invoke(window,new object[]{Path.Combine(output,shot.Item1+".png"),false});
            }
        }
        finally{type.GetMethod("Cleanup",Private).Invoke(window,null);Object.DestroyImmediate(window);}
        File.WriteAllText(Path.Combine(output,"render.txt"),"Native Unity captures: entrance, middle, crossing and compact screen; current authored layout; fixed animation time. No source layouts or island settings written by this capture.");
    }
}
