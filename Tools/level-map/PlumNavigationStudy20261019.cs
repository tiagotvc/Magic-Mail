using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using RoyalAves.EditorTools;

// Preview-only code: the shipped navigation and all original icon assets stay intact.
public sealed class PlumNavigationStudyGraphic : MaskableGraphic
{
    static Color C(byte r, byte g, byte b, byte a = 255) => new Color32(r, g, b, a);
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); var r = rectTransform.rect;
        void Quad(float left, float bottom, float right, float top, Color low, Color high)
        {
            int n = vh.currentVertCount;
            vh.AddVert(new Vector2(left, bottom), low, Vector2.zero);
            vh.AddVert(new Vector2(left, top), high, Vector2.zero);
            vh.AddVert(new Vector2(right, top), high, Vector2.zero);
            vh.AddVert(new Vector2(right, bottom), low, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
        }
        // One rectangular edge-to-edge panel. No hills, triangles, side radius or metal rails.
        Quad(r.xMin, r.yMin, r.xMax, r.yMax, C(162, 82, 157), C(192, 116, 184));
        Quad(r.xMin, r.yMin + 2, r.xMin + r.width / 5, r.yMax, C(205, 140, 198), C(229, 179, 222));
        Quad(r.xMin, r.yMax - 2, r.xMax, r.yMax, C(255, 235, 255, 45), C(255, 235, 255, 90));
        Quad(r.xMin, r.yMin, r.xMax, r.yMin + 3, C(104, 45, 108), C(152, 76, 149));
        for (int i = 1; i < 5; i++)
        {
            float x = r.xMin + r.width * i / 5;
            Quad(x - .9f, r.yMin + 3, x + .4f, r.yMax - 2, C(111, 44, 117, 135), C(134, 59, 134, 105));
            Quad(x + .4f, r.yMin + 3, x + 1.5f, r.yMax - 2, C(245, 195, 237, 70), C(255, 224, 251, 100));
        }
    }
}

public static class PlumNavigationStudy20261019
{
    const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;
    const string Output = @"C:\Users\Tiago Carvalho\Documents\projetos\Magic-Mail\Tools\level-map\previews\visual-studies-2026-10-19\navigation-corrected";
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        RoyalAvesLevelMapPreview preview = null;
        try
        {
            ShaderUtil.allowAsyncCompilation = false;
            var kit = Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath);
            kit.dayNightCycle = false; kit.night = 0;
            preview = RoyalAvesLevelMapPreview.CreateScenePreview(720, 1560);
            var map = (CurvedLevelMap)preview.GetType().GetField("map", F).GetValue(preview);
            map.World.TimeOverride = 0; map.PreviewAt(1);
            var footer = Resources.FindObjectsOfTypeAll<WorldMapPostalGraphic>().Single(g => g.surface == WorldMapPostalGraphic.Surface.Navigation && g.gameObject.activeInHierarchy);
            var before = footer.transform.parent.GetComponentsInChildren<Image>(true).Where(i => i.name == "Icon").Select(i => i.sprite).ToArray();
            footer.enabled = false;
            foreach (Transform child in footer.transform) child.gameObject.SetActive(false);
            var panel = new GameObject("StraightPlumStudy", typeof(RectTransform), typeof(CanvasRenderer));
            panel.layer = footer.gameObject.layer;
            panel.transform.SetParent(footer.transform.parent, false); panel.transform.SetAsFirstSibling();
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            var graphic = panel.AddComponent<PlumNavigationStudyGraphic>(); graphic.raycastTarget = false;
            foreach (var label in footer.transform.parent.GetComponentsInChildren<TMP_Text>(true)) label.color = new Color32(80, 34, 90, 255);
            Canvas.ForceUpdateCanvases();
            var after = footer.transform.parent.GetComponentsInChildren<Image>(true).Where(i => i.name == "Icon").Select(i => i.sprite).ToArray();
            if (!before.SequenceEqual(after) || before.Length != 5) throw new Exception("Original five icon bindings changed");
            preview.GetType().GetMethod("Save", F).Invoke(preview, new object[] { Output + "/map-original-icons-plum.png", false });
            // Independent UI-only camera: close-up of the actual existing five icons and new background.
            var footerRoot = (RectTransform)footer.transform.parent;
            var canvas = footerRoot.GetComponentInParent<Canvas>();
            var camera = canvas.worldCamera;
            if (camera == null) camera = (Camera)preview.GetType().GetField("camera", F).GetValue(preview);
            var corners = new Vector3[4]; footerRoot.GetWorldCorners(corners);
            var lo = camera.WorldToScreenPoint(corners[0]); var hi = camera.WorldToScreenPoint(corners[2]);
            var target = (RenderTexture)preview.GetType().GetField("capture", F).GetValue(preview);
            var previous = RenderTexture.active; RenderTexture.active = target;
            int x = Mathf.Clamp(Mathf.FloorToInt(lo.x), 0, target.width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(lo.y), 0, target.height - 1);
            int w = Mathf.Clamp(Mathf.CeilToInt(hi.x) - x, 1, target.width - x);
            int h = Mathf.Clamp(Mathf.CeilToInt(hi.y) - y + 46, 1, target.height - y);
            var closeup = new Texture2D(w, h, TextureFormat.RGBA32, false);
            closeup.ReadPixels(new Rect(x, y, w, h), 0, 0); closeup.Apply();
            File.WriteAllBytes(Output + "/bottom-plum-straight-original-icons.png", closeup.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(closeup); RenderTexture.active = previous;
            File.WriteAllText(Output + "/validation.txt", "PASS: rectangular plum bar without triangles or rounded ends; all five production icon sprite references preserved exactly; native full map and UI close-up captures. Preview-only, no production asset writes.\n");
        }
        catch (Exception e) { File.WriteAllText(Output + "/validation.txt", "FAIL: " + e); Debug.LogException(e); EditorApplication.Exit(1); return; }
        finally { if (preview != null) UnityEngine.Object.DestroyImmediate(preview); }
        EditorApplication.Exit(0);
    }
}
