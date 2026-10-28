using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using RoyalAves.EditorTools;
using RoyalAves.UI.RoyalPostman2026;

public static class PetsV1Checks
{
    const string Output = "../pets-v1";
    static void Check(bool ok, string error) { if (!ok) throw new Exception(error); }
    static Color32[] Pixels(RoyalPostmanScreenDocument d)
    {
        d.Render(); var previous = RenderTexture.active; RenderTexture.active = d.Texture;
        var texture = new Texture2D(d.Width, d.Height, TextureFormat.RGBA32, false);
        try { texture.ReadPixels(new Rect(0, 0, d.Width, d.Height), 0, 0); texture.Apply(); return texture.GetPixels32(); }
        finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(texture); }
    }
    static void Hits(RoyalPostmanScreenDocument d, Button button, EventSystem events)
    {
        var hits = new System.Collections.Generic.List<RaycastResult>();
        var raycaster = d.Root.GetComponentInParent<GraphicRaycaster>();
        var data = new PointerEventData(events) { position = d.Camera.WorldToScreenPoint(button.transform.position) };
        raycaster.Raycast(data, hits);
        Check(hits.Count > 0 && (hits[0].gameObject.transform == button.transform || hits[0].gameObject.transform.IsChildOf(button.transform)), "Unreachable button: " + button.name);
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output); File.WriteAllText(Output + "/validation.txt", "RUNNING");
        int errors = 0; Application.LogCallback handler = (m, s, t) => { if (t == LogType.Error || t == LogType.Exception) errors++; };
        Application.logMessageReceived += handler;
        try
        {
            ShaderUtil.allowAsyncCompilation = false;
            const string homePath = "Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Home.prefab";
            const string navPath = "Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Navigation.prefab";
            var homeBefore = File.ReadAllBytes(homePath); var navBefore = File.ReadAllBytes(navPath);
            RoyalPostmanPetsBuilder.Build(); var kit = RoyalPostmanRuntimeKit.Load();
            foreach (var size in new[] { new Vector2Int(360, 800), new Vector2Int(720, 1280), new Vector2Int(1080, 1920) })
                using (var d = new RoyalPostmanScreenDocument(kit, "pets", size.x, size.y))
                {
                    var view = d.Root.GetComponent<RoyalPostmanPetsScreen>();
                    Check(view.incubators.All(s => !s.egg.gameObject.activeSelf), "Egg visible in an inactive incubator");
                    Check(d.Locked(d.Root.transform.Find("Navigation")), "Shared bottom HUD is editable outside Home");
                    int back = 0, navigation = -1;
                    view.Initialize(() => back++, i => navigation = i);
                    d.Export(Output + "/pets-" + size.x + ".png");
                    Canvas.ForceUpdateCanvases();
                    Check(view.inventory.vertical && !view.inventory.horizontal, "Inventory is not vertical");
                    Check(view.inventory.content.rect.height > view.inventory.viewport.rect.height, "Inventory cannot scroll");
                    var eventsObject = new GameObject("TestEventSystem", typeof(EventSystem));
                    try { Hits(d, view.backButton, eventsObject.GetComponent<EventSystem>()); Hits(d, view.items[0].button, eventsObject.GetComponent<EventSystem>()); Hits(d, view.incubators[0].useItem, eventsObject.GetComponent<EventSystem>()); }
                    finally { UnityEngine.Object.DestroyImmediate(eventsObject); }
                    view.backButton.onClick.Invoke(); Check(back == 1, "Back callback lost");
                    view.helpButton.onClick.Invoke(); Check(view.helpPanel.activeSelf, "Help did not open");
                    view.dismissHelp.onClick.Invoke(); Check(!view.helpPanel.activeSelf, "Help did not close");
                    view.currentTab.onClick.Invoke(); Check(view.currentPanel.activeSelf && !view.workshop.activeSelf, "Current tab failed");
                    view.incubatorTab.onClick.Invoke(); Check(!view.currentPanel.activeSelf && view.workshop.activeSelf, "Incubator tab failed");
                    view.SelectItem(1); Check(view.items[1].selection.activeSelf && view.hintTitle.text == view.items[1].title, "Item selection failed");
                    int useSlot = -1; string itemId = null; view.UseItemRequested = (slot, item) => { useSlot = slot; itemId = item; };
                    view.incubators[0].useItem.onClick.Invoke(); Check(useSlot == 0 && itemId == view.items[1].id, "Item callback failed");
                    view.SetIncubation(0, true, AssetDatabase.LoadAssetAtPath<Sprite>(RoyalPostmanArtPaths.Sprite("pets-egg-v1")), 45, 100, "2h 15m");
                    Check(view.incubators.Count(s => s.egg.gameObject.activeSelf) == 1, "Active state did not show exactly one egg");
                    if (size.x == 720) d.Export(Output + "/pets-active-state.png");
                    view.SetIncubation(0, false, null, 0, 100, ""); Check(view.incubators.All(s => !s.egg.gameObject.activeSelf), "Egg remained after ending incubation");
                    d.Root.transform.Find("Navigation/Tab2").GetComponent<Button>().onClick.Invoke(); Check(navigation == 2, "Bottom navigation lost callback");
                    view.inventory.verticalNormalizedPosition = 0; d.Render();
                    if (size.x == 720) d.Export(Output + "/pets-inventory-scrolled.png");
                }
            using (var d = new RoyalPostmanScreenDocument(kit, "pets", 720, 1280))
            {
                var label = d.Root.transform.Find("Header/Title").GetComponent<RectTransform>(); var original = label.anchoredPosition;
                label.anchoredPosition += Vector2.right * 11; d.Dirty = true; d.Save();
                using (var reopened = new RoyalPostmanScreenDocument(kit, "pets", 720, 1280))
                    Check(reopened.Root.transform.Find("Header/Title").GetComponent<RectTransform>().anchoredPosition == original + Vector2.right * 11, "Pets editor changes did not persist");
                label.anchoredPosition = original; d.Save();
            }
            using (var d = new RoyalPostmanScreenDocument(kit, "pets", 720, 1280))
            {
                var expected = Pixels(d); d.Export(Output + "/editor-parity.png"); d.Root.SetActive(false);
                var runtime = RoyalPostmanPetsScreen.Create((RectTransform)d.Root.transform.parent, () => { }, i => { }); runtime.Open();
                var actual = Pixels(d); d.Export(Output + "/runtime-parity.png"); int changed = expected.Where((pixel, i) => !pixel.Equals(actual[i])).Count();
                Check(changed == 0, "Editor/runtime differ: " + changed + " pixels");
                UnityEngine.Object.DestroyImmediate(runtime.gameObject);
            }
            Check(homeBefore.SequenceEqual(File.ReadAllBytes(homePath)), "Home was modified");
            Check(navBefore.SequenceEqual(File.ReadAllBytes(navPath)), "Shared navigation was modified");
            Check(AssetDatabase.LoadAssetAtPath<GameObject>(RoyalPostmanPetsBuilder.PrefabPath).transform.Find("Navigation") == null, "Pets saved a local bottom HUD override");
            Check(errors == 0, "Unity logged errors: " + errors);
            File.WriteAllText(Output + "/validation.txt", "PASS: native Pets captures at 360x800, 720x1280, 1080x1920; vertical right inventory scroll and raycast targets; back/help/tabs/item/navigation callbacks; empty incubators by default; egg only in active state; Pets editor save/reopen; shared HUD locked and not serialized locally; editor/runtime pixel equality (0 differing pixels); Home and Navigation unchanged byte-for-byte; zero Unity errors. Item quantities/unlock labels are reference examples; incubation economy/backend is not implemented by this view.\n");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { File.WriteAllText(Output + "/validation.txt", "FAIL: " + error); Debug.LogException(error); EditorApplication.Exit(1); }
        finally { Application.logMessageReceived -= handler; }
    }
}
