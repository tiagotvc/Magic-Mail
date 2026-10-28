using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using RoyalAves.EditorTools;

// Only the isolated PreviewProject compiles this harness and the draft geometry.
public static class NativeButtonStudies20261019
{
    const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
    const string Root = @"C:\Users\Tiago Carvalho\Documents\projetos\Magic-Mail\Tools\level-map\previews\visual-studies-2026-10-19";
    static Color C(string value) { ColorUtility.TryParseHtmlString(value, out var c); return c; }
    static void Footer()
    {
        var original = Resources.FindObjectsOfTypeAll<WorldMapPostalGraphic>().Single(g => g.surface == WorldMapPostalGraphic.Surface.Navigation && g.gameObject.activeInHierarchy);
        original.enabled = false;
        foreach (Transform seam in original.transform) seam.gameObject.SetActive(false);
        var panel = new GameObject("StraightPlumStudy", typeof(RectTransform), typeof(CanvasRenderer));
        panel.layer = original.gameObject.layer; panel.transform.SetParent(original.transform.parent, false); panel.transform.SetAsFirstSibling();
        var r = (RectTransform)panel.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        panel.AddComponent<PlumNavigationStudyGraphic>().raycastTarget = false;
        foreach (var label in original.transform.parent.GetComponentsInChildren<TMP_Text>(true)) label.color = new Color32(80,34,90,255);
    }
    public static void Run()
    {
        var output = Root + "/native-buttons"; Directory.CreateDirectory(output);
        RoyalAvesLevelMapPreview preview = null;
        int errors = 0;
        Application.LogCallback logger = (m,s,t) => { if(t==LogType.Error || t==LogType.Exception) errors++; };
        Application.logMessageReceived += logger;
        try
        {
            ShaderUtil.allowAsyncCompilation = false;
            var kit = Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath); kit.dayNightCycle = false; kit.night = 0;
            var features = Resources.Load<LobbyFeaturesConfig>("RoyalAvesFeatures");
            features.displayFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/RoyalAves/Fonts/LilitaOne SDF.asset");
            if (features.displayFont == null) throw new Exception("Rounded font missing");
            Shader.SetGlobalFloat("_VisualStudy", 1);
            Shader.SetGlobalColor("_StudyGrass", C("#89B354")); Shader.SetGlobalColor("_StudyGrassShadow", C("#5F8F38"));
            Shader.SetGlobalColor("_StudyPath", C("#FFE2A0")); Shader.SetGlobalColor("_StudyPathShadow", C("#E0B269"));
            var islands = Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath); islands.pathSway = 1.35f; WorldMapRoute.StudyWave = 1.12f;
            preview = RoyalAvesLevelMapPreview.CreateScenePreview(720,1560);
            var type = preview.GetType();
            type.GetMethod("Build", F).Invoke(preview,new object[]{720,1560,7});
            var map = (CurvedLevelMap)type.GetField("map",F).GetValue(preview); map.World.TimeOverride = 0;
            Footer();
            var report = new System.Text.StringBuilder();
            Quaternion first = Quaternion.identity; float change = 0;
            foreach (int cursor in new[]{1,3,5,7})
            {
                type.GetField("level", F).SetValue(preview,(float)cursor); map.PreviewAt(cursor);
                var nodes = (IEnumerable)typeof(WorldMapView).GetField("nodes",F).GetValue(map.World);
                foreach(var node in nodes)
                {
                    var nt = node.GetType(); if((int)nt.GetField("level").GetValue(node)!=7) continue;
                    var t = (Transform)nt.GetField("t").GetValue(node);
                    if(cursor==1) first=t.localRotation; else change=Mathf.Max(change,Quaternion.Angle(first,t.localRotation));
                    var number = (Transform)nt.GetField("numberT").GetValue(node);
                    if(number==t || number.parent!=t) throw new Exception("Number is not a separate child");
                    report.AppendLine("cursor="+cursor+" target=7 rotation="+t.localRotation.eulerAngles+" separate-number="+number.name);
                }
                type.GetMethod("Save",F).Invoke(preview,new object[]{output+"/button-curve-cursor-"+cursor+".png",false});
            }
            if(change<5) throw new Exception("Button does not change perspective along the curve: "+change);
            report.AppendLine("PASS: actual domed 3D mesh; face/cream rim material, separate TMP number and independent reward/crown objects; existing curved-ground transforms unchanged; target button changes orientation by "+change+" degrees; preview progress only, no save writes.");
            if(errors!=0) throw new Exception(errors+" Unity errors");
            File.WriteAllText(output+"/validation.txt",report.ToString());
        }
        catch(Exception e){File.WriteAllText(output+"/validation.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);return;}
        finally {Application.logMessageReceived-=logger;Shader.SetGlobalFloat("_VisualStudy",0);if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}
        EditorApplication.Exit(0);
    }
}
