using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Items._Interfaces;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.System;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class PowerUpRulesValidation
    {
        const string ActiveKey = "MagicMail.PowerUpSwapValidation";
        static int stage, caseIndex, initialLaunches;
        static double nextAt, deadline;
        static Item dragging;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
            deadline = EditorApplication.timeSinceStartup + 60d;
        }

        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (File.Exists("Logs/multicolor-electric.request"))
            {
                File.Delete("Logs/multicolor-electric.request");
                try { ValidateElectricArcs(); }
                catch (Exception error) { File.WriteAllText("Logs/multicolor-electric.result", error.ToString()); Debug.LogException(error); }
            }
            const string request = "Logs/power-up-rules.request";
            try
            {
                if (File.Exists(request))
                {
                    string mode = File.ReadAllText(request).Trim();
                    File.Delete(request);
                    if (EditorApplication.isPlaying) throw new Exception("Start the fixture outside Play Mode.");
                    ValidateGeometry();
                    if (mode == "play")
                    {
                        stage = caseIndex = 0;
                        SessionState.SetBool(ActiveKey, true);
                        deadline = EditorApplication.timeSinceStartup + 60d;
                        EditorApplication.isPlaying = true;
                    }
                }
                if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying) return;
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Swap validation timed out.");
                var manager = LevelManager.THIS;
                if (manager == null || AI.THIS == null) return;
                if (stage == 0)
                {
                    if (manager.gameStatus == GameState.Map) { stage = 1; manager.gameStatus = GameState.PrepareGame; }
                    return;
                }
                if (manager.field == null || EditorApplication.timeSinceStartup < nextAt) return;
                if (stage == 1)
                {
                    if (manager.gameStatus != GameState.WaitForPopup && manager.gameStatus != GameState.Playing) return;
                    manager.gameStatus = GameState.Playing;
                    manager.levelData.limit = 999;
                    foreach (var goal in manager.levelData.TargetCounters)
                    {
                        goal.targetLevel = goal.targetLevel.DeepCopy();
                        goal.targetLevel.setCount = SetCount.Manually;
                        goal.count = 999;
                    }
                    stage = 2;
                }
                if (stage == 2) { PrepareSwap(manager); stage = 3; nextAt = EditorApplication.timeSinceStartup + .4d; return; }
                if (stage == 3)
                {
                    initialLaunches = ItemMarmalade.LaunchCount;
                    var power = manager.field.GetSquare(caseIndex < 2 ? 3 : 4, 3).Item;
                    power.dontDestroyOnThisMove = true; // The explicit player move must bypass passive protection.
                    manager.DragBlocked = false;
                    manager.findMatchesStarted = true;
                    var pair = dragging.SwitchDirection(Vector3.left);
                    Expect(pair.currentItem != null && pair.neighborItem != null, "Swap was not accepted.");
                    stage = 4; nextAt = EditorApplication.timeSinceStartup + .7d; return;
                }
                if (stage == 4)
                {
                    Expect(ItemMarmalade.LaunchCount == initialLaunches + 1,
                        $"Helix did not launch once: case {caseIndex} (0/1 dragged helix; 2/3 dragged normal; odd cases with match).");
                    caseIndex++;
                    if (caseIndex == 4)
                    {
                        File.AppendAllText("Logs/power-up-rules.result", "\nPASS PLAY: four actual swaps launched the helix exactly once; with/without a match; dragging helix or ordinary piece; passive protection set.");
                        SessionState.SetBool(ActiveKey, false);
                        EditorApplication.isPlaying = false;
                        return;
                    }
                    stage = 2; nextAt = EditorApplication.timeSinceStartup + 2d;
                }
            }
            catch (Exception error)
            {
                File.WriteAllText("Logs/power-up-rules.result", "FAIL: " + error);
                bool running = SessionState.GetBool(ActiveKey, false);
                SessionState.SetBool(ActiveKey, false);
                if (running) EditorApplication.isPlaying = false;
                Debug.LogException(error);
            }
        }

        static void PrepareSwap(LevelManager manager)
        {
            manager.StopAllCoroutines();
            manager.findMatchesStarted = true;
            manager.DragBlocked = false;
            var field = manager.field;
            Expect(field.fieldData.maxCols >= 6 && field.fieldData.maxRows >= 5, "Fixture needs a board at least 6x5.");
            foreach (var cell in field.GetSquares())
            {
                if (cell == null) continue;
                if (cell.Item != null)
                {
                    cell.Item.StopAllCoroutines();
                    cell.Item.NextType = ItemsTypes.NONE;
                    cell.Item.dontDestroyOnThisMove = cell.Item.dontDestroyForThisCombine = false;
                    cell.Item.DestroyBehaviour();
                }
                foreach (var layer in cell.subSquares.ToArray()) UnityEngine.Object.Destroy(layer.gameObject);
                cell.subSquares.Clear();
                cell.type = SquareTypes.EmptySquare;
                cell.canMoveIn = cell.canMoveOut = true;
                cell.undestroyable = cell.cannotBeDestroy = false;
                cell.Item = cell.GenItem(false, ItemsTypes.NONE, (cell.col + cell.row * 2) % 5, noAnim: true);
            }
            bool powerDragged = caseIndex < 2;
            var powerCell = field.GetSquare(powerDragged ? 3 : 4, 3);
            var normalCell = field.GetSquare(powerDragged ? 4 : 3, 3);
            int destinationCol = powerDragged ? 3 : 4;
            normalCell.Item.GetComponent<IColorableComponent>().SetColor(0);
            if (caseIndex % 2 == 1)
            {
                field.GetSquare(destinationCol, 2).Item.GetComponent<IColorableComponent>().SetColor(0);
                field.GetSquare(destinationCol, 4).Item.GetComponent<IColorableComponent>().SetColor(0);
            }
            powerCell.Item.DestroyBehaviour();
            powerCell.Item = powerCell.GenItem(false, ItemsTypes.MARMALADE, noAnim: true);
            dragging = field.GetSquare(3, 3).Item;
        }

        [MenuItem("Royal Aves/Validação/Power-ups e alcance 7x7 da dinamite")]
        public static void ValidateGeometry()
        {
            var root = new GameObject("PowerUpRulesFixture") { hideFlags = HideFlags.HideAndDontSave };
            root.SetActive(false);
            try
            {
                var field = root.AddComponent<FieldBoard>();
                field.fieldData = new FieldData { maxRows = 9, maxCols = 9 };
                field.squaresArray = new Square[81];
                for (int row = 0; row < 9; row++)
                for (int col = 0; col < 9; col++)
                {
                    var node = new GameObject("Cell"); node.transform.SetParent(root.transform, false);
                    var square = node.AddComponent<Square>(); square.field = field; square.row = row; square.col = col;
                    square.type = SquareTypes.EmptySquare;
                    field.squaresArray[row * 9 + col] = square;
                }
                var clear = new ImpactBarrierSnapshot(Array.Empty<Vector2Int>());
                var center = field.GetSquare(4, 4);
                Expect(ImpactBarrierSnapshot.AreaAround(center, ItemDestroyAnimation.BlastRadius, clear).Count == 49, "Dynamite center must cover 49 cells.");
                Expect(ImpactBarrierSnapshot.AreaAround(field.GetSquare(0, 0), 3, clear).Count == 16, "Corner must clip to 16 unique cells.");
                Expect(ImpactBarrierSnapshot.AreaAround(field.GetSquare(0, 4), 3, clear).Count == 28, "Edge must clip to 28 cells.");
                Expect(ImpactBarrierSnapshot.AreaAround(center, 1, clear).Count == 9, "Other adjacency effects must remain 3x3.");
                var blocked = new ImpactBarrierSnapshot(new[] { new Vector2Int(5, 4) });
                var blast = ImpactBarrierSnapshot.AreaAround(center, 3, blocked);
                Expect(blast.Contains(field.GetSquare(5, 4)) && !blast.Contains(field.GetSquare(6, 4)), "A crate must receive but block the blast behind it.");
                foreach (var type in new[] { ItemsTypes.MARMALADE, ItemsTypes.PACKAGE, ItemsTypes.MULTICOLOR, ItemsTypes.HORIZONTAL_STRIPED, ItemsTypes.VERTICAL_STRIPED })
                    Expect(Item.IsPlayerPowerUpType(type), "Missing movable power-up: " + type);
                foreach (var type in new[] { ItemsTypes.NONE, ItemsTypes.SPIRAL, ItemsTypes.INGREDIENT, ItemsTypes.TimeBomb })
                    Expect(!Item.IsPlayerPowerUpType(type), "Ordinary/obstacle type treated as power-up: " + type);
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/power-up-rules.result", "PASS: native blast geometry (49 center, 16 corner, 28 edge); default adjacency 9; crate barriers; all five player power-up types.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void Expect(bool condition, string message) { if (!condition) throw new Exception(message); }

        static void ValidateElectricArcs()
        {
            var root = new GameObject("ElectricArcFixture");
            try
            {
                var source = new GameObject("Source").transform; source.SetParent(root.transform, false);
                for (int i = 0; i < 16; i++)
                {
                    var target = new GameObject("Target").transform; target.SetParent(root.transform, false);
                    target.position = new Vector3(Mathf.Cos(i * .65f), Mathf.Sin(i * .65f)) * (.2f + i * .3f);
                    var node = new GameObject("ElectricArc"); node.transform.SetParent(root.transform, false);
                    var arc = node.AddComponent<SweetSugar.Scripts.Effects.Lightning>();
                    arc.SetLight(source.position, target.position, .82f, true, source, target);
                    arc.Sample(SweetSugar.Scripts.Effects.Lightning.TravelTime * .5f);
                    var travelling = arc.GetComponent<LineRenderer>();
                    Expect(Vector3.Distance(travelling.GetPosition(travelling.positionCount - 1), target.position) > .01f,
                        "Electric arc must travel before contacting its target.");
                    foreach (float time in new[] { .06f, .12f, .6f, 2f })
                    {
                        source.position += new Vector3(.01f, -.01f);
                        target.position += new Vector3(-.01f, .02f);
                        arc.Sample(time);
                        var line = arc.GetComponent<LineRenderer>();
                        Expect(Vector3.Distance(line.GetPosition(0), source.position) < .0001f, "Electric arc detached from source.");
                        Expect(Vector3.Distance(line.GetPosition(line.positionCount - 1), target.position) < .0001f, "Electric arc missed its target.");
                        // The ribbon includes transparent glow margins; the visible white core is much narrower.
                        Expect(line.widthMultiplier <= .82f * .0451f, "Electric ribbon is too thick.");
                        Expect(line.sharedMaterial != null && !ShaderUtil.ShaderHasError(line.sharedMaterial.shader),
                            "Electric shader is missing or failed compilation.");
                        foreach (var renderer in arc.GetComponentsInChildren<LineRenderer>())
                        for (int point = 0; point < renderer.positionCount; point++)
                            Expect(!float.IsNaN(renderer.GetPosition(point).x), "Invalid electric geometry.");
                    }
                    Expect(arc.GetComponentsInChildren<LineRenderer>().Length == 4, "Missing halo/branches.");
                }
                Expect(root.GetComponentsInChildren<SweetSugar.Scripts.Effects.Lightning>().Length == 16, "One electric arc is required per selected piece.");
                CaptureElectricPreview(root);
                File.WriteAllText("Logs/multicolor-electric.result", "PASS: 16 simultaneous electric arcs; 64 samples with moving endpoints; exact source/target contacts; thin cores; halo and branches; finite geometry.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void CaptureElectricPreview(GameObject root)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("a97c105638bdf8b4a8650670310a4cd3"));
            var piece = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/Pieces/bow.png");
            foreach (Transform child in root.transform)
            {
                if (child.name != "Target" && child.name != "Source") continue;
                var renderer = child.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = child.name == "Source"
                    ? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/Specials/orb.png") : piece;
                if (renderer.sprite == null) renderer.sprite = piece;
                renderer.sharedMaterial = material;
                renderer.sortingLayerName = RoyalAves.Effects.PowerUpEffectOrder.SortingLayer;
                renderer.sortingOrder = 110;
                child.localScale = Vector3.one * (.55f / Mathf.Max(renderer.sprite.bounds.size.x, renderer.sprite.bounds.size.y));
            }
            foreach (var renderer in root.GetComponentsInChildren<Renderer>()) renderer.gameObject.layer = 31;
            var node = new GameObject("ArcPreviewCamera"); node.transform.SetParent(root.transform, false);
            var camera = node.AddComponent<Camera>(); camera.enabled = false;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true; camera.orthographicSize = 5.2f;
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .20f, .30f);
            var target = new RenderTexture(1024, 1024, 24);
            var pixels = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0); pixels.Apply();
                File.WriteAllBytes("Logs/multicolor-electric-preview.png", pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null; target.Release();
                UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
    }
}
