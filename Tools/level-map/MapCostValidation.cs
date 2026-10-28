using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Text;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;
public static class MapCostValidation
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    public static void Run()
    {
        RoyalAvesLevelMapPreview preview=null;
        int errors=0;
        Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        Application.logMessageReceived+=log;
        try
        {
            ShaderUtil.allowAsyncCompilation=false;
            preview=(RoyalAvesLevelMapPreview)typeof(RoyalAvesLevelMapPreview).GetMethod("CreateScenePreview",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{1080,1920});
            var map=(CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview);
            var w=map.World;float cursor=w.Route.MetersAt(map.Cursor);
            var output=new StringBuilder();
            void Measure(string name,Action action,int count=100)
            {
                for(int i=0;i<5;i++)action();
                long bytes=GC.GetAllocatedBytesForCurrentThread();var sw=Stopwatch.StartNew();
                for(int i=0;i<count;i++)action();
                sw.Stop();bytes=GC.GetAllocatedBytesForCurrentThread()-bytes;
                output.AppendLine(name+": "+(sw.Elapsed.TotalMilliseconds/count).ToString("F4")+" ms, "+(bytes/count)+" B/call");
            }
            Measure("Tick stationary",()=>w.Tick(map.Cursor,1,false));
            int step=0;Measure("Tick scrolling",()=>w.Tick(map.Cursor+(++step%2)*.005f,1,true));
            foreach(var name in new[]{"UpdateProps","UpdateNight","UpdateIceReflections","UpdateShafts"})
            {
                var m=typeof(WorldMapView).GetMethod(name,Private);object[] args={cursor};
                Measure(name,()=>m.Invoke(w,args));
            }
            Measure("RenderNow 1080x1920 CPU submit",()=>w.RenderNow(),20);
            output.AppendLine("Renderers: "+w.GetComponentsInChildren<Renderer>().Length);
            output.AppendLine("Ground vertices: "+w.transform.Find("Chao").GetComponent<MeshFilter>().sharedMesh.vertexCount);
            foreach(var level in new[]{1,21,41,61,81,100,101,150,1})
            {
                map.PreviewAt(level);w.Tick(map.Cursor,1,false);
                var ground=w.transform.Find("Chao").GetComponent<MeshFilter>().sharedMesh;
                if(ground.vertexCount==0)throw new Exception("Missing terrain at "+level);
                foreach(var n in ground.normals)
                    if(float.IsNaN(n.x)||Mathf.Abs(n.magnitude-1)>.002f)throw new Exception("Invalid ground normal at "+level);
                w.RenderNow();
            }
            if(errors!=0)throw new Exception("Unity logged errors");
            output.AppendLine("PASS: terrain and unit normals across levels 1,21,41,61,81,100,101,150 and back; native rendering; zero Unity errors. Timing is an isolated editor benchmark, not device FPS.");
            File.WriteAllText("../map-cost-validation.txt",output.ToString());
        }
        catch(Exception e){UnityEngine.Debug.LogException(e);EditorApplication.Exit(1);return;}
        finally{if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);Application.logMessageReceived-=log;}
        EditorApplication.Exit(0);
    }
}
