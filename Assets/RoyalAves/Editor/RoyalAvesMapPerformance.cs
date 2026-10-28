using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;

namespace RoyalAves.EditorTools
{
    // On-demand measurement. The private "start" request runs a disposable Play session;
    // the menu only samples an existing session. Project settings are never changed.
    [InitializeOnLoad]
    public static class RoyalAvesMapPerformance
    {
        static readonly List<float> frames=new List<float>();
        static double started;
        static int lastFrame;
        static bool sampling;
        static readonly Dictionary<string,ProfilerRecorder> recorders=new Dictionary<string,ProfilerRecorder>();
        static readonly Dictionary<string,List<long>> samples=new Dictionary<string,List<long>>();
        static RoyalAvesMapPerformance(){EditorApplication.update+=Poll;}
        [MenuItem("Royal Aves/Mapa de n\u00edveis/Profile Play Mode (10s)")]
        static void Capture()
        {
            Directory.CreateDirectory("Temp");
            if(!EditorApplication.isPlaying)
            {File.WriteAllText("Temp/map-play-profile.txt","Play Mode inactive; no frame timings sampled.");return;}
            frames.Clear();started=EditorApplication.timeSinceStartup;lastFrame=-1;sampling=true;
            var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
            foreach(var handle in handles)
            {
                var d=ProfilerRecorderHandle.GetDescription(handle);
                if(d.Name!="Main Thread"&&d.Name!="Render Thread"&&d.Name!="GC Allocated In Frame"&&d.Name!="Draw Calls Count"&&d.Name!="MagicMail.Map.Tick"&&d.Name!="Gfx.WaitForPresentOnGfxThread")continue;
                recorders[d.Name]=ProfilerRecorder.StartNew(d.Category,d.Name,1);samples[d.Name]=new List<long>();
            }
        }
        static void Poll()
        {
            if(File.Exists("Temp/map-play-profile.request"))
            {
                var request=File.ReadAllText("Temp/map-play-profile.request").Trim();File.Delete("Temp/map-play-profile.request");
                if(request=="start"&&!EditorApplication.isPlaying)
                {File.WriteAllText("Temp/map-play-profile.autostart","pending");EditorApplication.EnterPlaymode();return;}
                Capture();
            }
            if(!sampling&&File.Exists("Temp/map-play-profile.autostart")&&EditorApplication.isPlaying&&!EditorApplication.isCompiling)Capture();
            if(!sampling)return;
            if(EditorApplication.isPlaying&&Time.frameCount!=lastFrame)
            {
                lastFrame=Time.frameCount;
                if(EditorApplication.timeSinceStartup-started>3)
                {
                    frames.Add(Time.unscaledDeltaTime*1000);
                    foreach(var pair in recorders)if(pair.Value.Valid)samples[pair.Key].Add(pair.Value.LastValue);
                }
            }
            if(EditorApplication.isPlaying&&EditorApplication.timeSinceStartup-started<10)return;
            sampling=false;
            frames.Sort();
            var worlds=UnityEngine.Object.FindObjectsByType<WorldMapView>(FindObjectsSortMode.None);
            var cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            var lines=new List<string>{"Frames sampled: "+frames.Count};
            if(frames.Count>0)lines.Add($"Frame ms avg {frames.Average():F2}, median {frames[frames.Count/2]:F2}, p95 {frames[Math.Min(frames.Count-1,(int)(frames.Count*.95))]:F2}");
            lines.Add($"Target FPS {Application.targetFrameRate}, vsync {QualitySettings.vSyncCount}, time scale {Time.timeScale}");
            lines.Add("Map instances: "+worlds.Length);
            foreach(var pair in recorders)
            {
                var values=samples[pair.Key];
                if(values.Count>0)lines.Add(pair.Key+": avg raw "+values.Average().ToString("F0")+", max "+values.Max());
                pair.Value.Dispose();
            }
            recorders.Clear();samples.Clear();
            lines.Add("Graphics-driver allocated MB (whole editor): "+(UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver()/1048576f).ToString("F1"));
            foreach(var w in worlds)lines.Add($"Map {w.name}, active {w.gameObject.activeInHierarchy}, renderers {w.GetComponentsInChildren<Renderer>().Length}");
            foreach(var c in cameras)lines.Add($"Camera {c.name}, enabled {c.enabled}, active {c.gameObject.activeInHierarchy}, target {(c.targetTexture!=null?c.targetTexture.width+"x"+c.targetTexture.height:"screen")}, HDR {c.allowHDR}, MSAA {c.allowMSAA}");
            File.WriteAllLines("Temp/map-play-profile.txt",lines);
            if(File.Exists("Temp/map-play-profile.autostart"))
            {File.Delete("Temp/map-play-profile.autostart");EditorApplication.ExitPlaymode();}
        }
    }
}
