using System;
using System.Collections;
using System.IO;
using System.Linq;
using RoyalAves.Meta;
using SweetSugar.Scripts.Core;
using UnityEngine;
using UnityEngine.UI;

public sealed class SnowOpeningProbe : MonoBehaviour
{
    public static bool Done;
    public static string Result;
    public IEnumerator Start()
    {
        foreach (var level in new[] { 1, 20, 81 })
        {
            var menu = SweetSugar.Scripts.System.MenuReference.THIS.MenuPlay;
            menu.SetActive(false);
            InitScript.OpenMenuPlay(level);
            var view = menu.GetComponentInChildren<LevelGoalsView>();
            var icons = view.transform.Cast<Transform>().Where(t => t.gameObject.activeSelf && t.name.StartsWith("Meta ")).ToArray();
            var sprites = icons.Select(t => t.GetComponent<Image>().sprite).ToArray();
            if (sprites.Length == 0) { Fail("No goals immediately after opening " + level); yield break; }
            for (var frame = 0; frame < 36; frame++)
            {
                yield return new WaitForEndOfFrame();
                var current = view.transform.Cast<Transform>().Where(t => t.gameObject.activeSelf && t.name.StartsWith("Meta ")).ToArray();
                if (!menu.activeInHierarchy || !current.Select(t => t.GetComponent<Image>().sprite).SequenceEqual(sprites))
                { Fail("Goal changed or popup disappeared at " + level + "/" + frame); yield break; }
                foreach (var icon in current)
                {
                    if (icon.GetComponent<CanvasRenderer>().cull || ((RectTransform)icon).rect.width < 1)
                    { Fail("Culled goal at " + level + "/" + frame + ": " + icon.name); yield break; }
                    foreach (var label in icon.GetComponentsInChildren<TMPro.TextMeshProUGUI>())
                        if (label.canvasRenderer.cull || label.text == "")
                        { Fail("Culled quantity at " + level + "/" + frame); yield break; }
                }
                if (frame == 0 || frame == 4 || frame == 20)
                    ScreenCapture.CaptureScreenshot("Tools/level-start/previews/open-" + level + "-frame-" + frame + ".png");
            }
            var window = menu.transform.Find("Image") ?? menu.transform.Find("MenuPlayBg");
            window.Find("Close").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(.7f);
            if (menu.activeSelf) { Fail("Close failed at " + level); yield break; }
        }
        Result = "PASS: goals and quantities ready synchronously; sprites unchanged and never culled over the first 36 rendered frames at levels 1, 20 and 81; Close/reopen works. No inventory spent.";
        File.WriteAllText("Tools/level-start/previews/opening-validation.txt", Result); Done = true;
    }
    static void Fail(string message)
    { Result = "FAIL: " + message; File.WriteAllText("Tools/level-start/previews/opening-validation.txt", Result); Done = true; }
}
