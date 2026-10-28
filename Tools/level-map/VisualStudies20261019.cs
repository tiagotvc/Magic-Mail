using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using RoyalAves.EditorTools;

// Preview-only harness: copied into the isolated project's Editor folder.
public static class VisualStudies20261019
{
    const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
    const string Output = @"C:\Users\Tiago Carvalho\Documents\projetos\Magic-Mail\Tools\level-map\previews\visual-studies-2026-10-19\terrain";
    static Color C(string value) { ColorUtility.TryParseHtmlString(value, out var c); return c; }
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        RoyalAvesLevelMapPreview preview = null;
        int errors = 0;
        Application.LogCallback logger = (m, s, t) => { if (t == LogType.Error || t == LogType.Exception) errors++; };
        Application.logMessageReceived += logger;
        try
        {
            ShaderUtil.allowAsyncCompilation = false;
            var sourceKit = Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath);
            sourceKit.dayNightCycle = false; sourceKit.night = 0;
            var islands = Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);
            preview = RoyalAvesLevelMapPreview.CreateScenePreview(720, 1560);
            var type = preview.GetType();
            CurvedLevelMap Map() => (CurvedLevelMap)type.GetField("map", F).GetValue(preview);
            void Save(float level, string name)
            {
                var map = Map(); map.World.TimeOverride = 0;
                type.GetField("level", F).SetValue(preview, level);
                map.PreviewAt(level + .001f); map.PreviewAt(level);
                type.GetMethod("Save", F).Invoke(preview, new object[] { Output + "/" + name + ".png", false });
            }
            void Palette(string grass, string shadow, string path, string pathShadow)
            {
                Shader.SetGlobalFloat("_VisualStudy", 1);
                Shader.SetGlobalColor("_StudyGrass", C(grass));
                Shader.SetGlobalColor("_StudyGrassShadow", C(shadow));
                Shader.SetGlobalColor("_StudyPath", C(path));
                Shader.SetGlobalColor("_StudyPathShadow", C(pathShadow));
            }
            Shader.SetGlobalFloat("_VisualStudy", 0);
            Save(1, "00-baseline-opening"); Save(5, "00-baseline-scroll");
            Save(12, "00-baseline-crossing"); Save(33, "00-baseline-season");
            Palette("#89B354", "#5F8F38", "#FFE2A0", "#E0B269");
            Save(1, "01-palette-a-opening"); Save(5, "01-palette-a-scroll");
            Palette("#91B54F", "#6B983A", "#FFDF93", "#E5B464");
            Save(1, "02-palette-b-opening"); Save(5, "02-palette-b-scroll");
            Palette("#81B663", "#4F914D", "#FFE7A8", "#DDB96D");
            Save(1, "03-palette-c-opening"); Save(5, "03-palette-c-scroll");
            Palette("#89B354", "#5F8F38", "#FFE2A0", "#E0B269");
            islands.pathSway = 1;
            WorldMapRoute.StudyWave = 1;
            Save(1, "04-curve-current");
            islands.pathSway = 1.35f;
            WorldMapRoute.StudyWave = 1.12f;
            Save(1, "05-curve-moderate"); Save(5, "05-curve-moderate-scroll");
            islands.pathSway = 1.65f;
            WorldMapRoute.StudyWave = 1.22f;
            Save(1, "06-curve-expressive"); Save(5, "06-curve-expressive-scroll");
            islands.pathSway = 1.35f; WorldMapRoute.StudyWave = 1.12f;
            Save(12, "07-selected-crossing"); Save(33, "07-selected-season");
            var oldPitch = sourceKit.pitch; var oldHeight = sourceKit.cameraHeight;
            sourceKit.pitch = oldPitch - 2.5f;
            type.GetMethod("Build", F).Invoke(preview, new object[] { 720, 1560, 1 });
            Save(1, "08-camera-lower");
            sourceKit.pitch = oldPitch; sourceKit.cameraHeight = oldHeight;
            type.GetMethod("Build", F).Invoke(preview, new object[] { 720, 1560, 1 });
            Save(1, "09-camera-original");
            type.GetMethod("Build", F).Invoke(preview, new object[] { 360, 780, 1 });
            Save(1, "10-selected-phone-360");
            if (errors != 0) throw new Exception(errors + " Unity errors");
            File.WriteAllText(Output + "/validation.txt", "PASS: 20 native map captures; original baseline, three chromatic palettes, three curve studies, two camera angles, scrolling/crossing/season and 360px phone. Changes are restricted to isolated preview source and in-memory settings; no player saves or production asset writes.\n");
        }
        catch (Exception e)
        {
            File.WriteAllText(Output + "/validation.txt", "FAIL: " + e);
            Debug.LogException(e); EditorApplication.Exit(1); return;
        }
        finally
        {
            Shader.SetGlobalFloat("_VisualStudy", 0);
            Application.logMessageReceived -= logger;
            if (preview != null) UnityEngine.Object.DestroyImmediate(preview);
        }
        EditorApplication.Exit(0);
    }
}
