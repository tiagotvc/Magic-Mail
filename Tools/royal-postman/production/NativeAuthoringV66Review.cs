using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.EditorTools;

public static class NativeAuthoringV66Review
{
    const string Output = "../native-authoring-v66";
    static byte[] original;
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    public static void Run()
    {
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "/review.txt", "RUNNING");
        original = File.ReadAllBytes(RoyalPostmanNativeAuthoring.PetsPath);
        try
        {
            RoyalPostmanNativeAuthoring.OpenPets();
            EditorApplication.delayCall += Review;
        }
        catch (Exception e) { Fail(e); }
    }

    static void Review()
    {
        try
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            Check(stage != null && stage.assetPath == RoyalPostmanNativeAuthoring.PetsPath, "Wrong native prefab stage");
            var root = stage.prefabContentsRoot;
            Canvas.ForceUpdateCanvases();
            var canvas = root.GetComponentInParent<Canvas>();
            Check(canvas != null, "Native prefab environment has no Canvas");
            Check(Tools.current == Tool.Rect && SceneView.lastActiveSceneView.in2DMode, "Native resize/2D not enabled");
            Check(Selection.activeGameObject == root, "Prefab not selected");
            var rect = (RectTransform)root.transform;
            var editable = root.transform.Find("Body/Workshop/Incubator0/Machine/Lid") as RectTransform;
            Check(editable != null && editable.GetComponent<Image>().sprite != null, "Editable machine layer missing");
            Check(!EditorUtility.IsPersistent(editable) && editable.gameObject.scene == stage.scene, "Layer not native scene object");
            var position = editable.anchoredPosition;
            Undo.RecordObject(editable, "Native position check");
            editable.anchoredPosition += new Vector2(15, -7);
            Undo.FlushUndoRecordObjects();
            Check(editable.anchoredPosition == position + new Vector2(15, -7), "Native move failed");
            Undo.PerformUndo();
            Check(editable.anchoredPosition == position, "Native undo failed");
            var sprites = root.GetComponentsInChildren<Image>(true).Count(i => i.sprite != null);
            Check(sprites > 60, "Missing authored sprites");
            Check(File.ReadAllBytes(RoyalPostmanNativeAuthoring.PetsPath).SequenceEqual(original), "Opening altered production prefab");
            File.WriteAllText(Output + "/review.txt", "PASS: native Prefab Mode; Canvas environment; 2D Rect Tool; native scene selection/move/undo; authored sprites resolved; prefab bytes unchanged.\nRoot: " + rect.rect + "\nCanvas: " + ((RectTransform)canvas.transform).rect + "\nSprites: " + sprites);
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Fail(e); }
    }

    static void Fail(Exception e)
    {
        File.WriteAllText(Output + "/review.txt", e.ToString());
        Debug.LogException(e);
        EditorApplication.Exit(1);
    }
}
