using System;
using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class InkTools
    {
        const string Menu = "Royal Aves/Elementos/Grama/";
        const string Request = "Logs/ink-prepare.request";
        const string GamePrefabPath = "Assets/SweetSugar/Resources/Blocks/SugarSquare.prefab";
        const string PreviewKey = "MagicMail.GrassHalfPreview";
        static double nextPoll;
        static double previewReadyAt;
        static bool liveUpdateDone;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
            previewReadyAt = EditorApplication.timeSinceStartup + 1d;
        }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 1d;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            StartRequestedPreview();
            if (!File.Exists(Request)) return;
            var request = File.ReadAllText(Request).Trim();
            if (request.StartsWith("hit:", StringComparison.Ordinal))
            {
                if (!EditorApplication.isPlaying || !InkLaboratory.IsActive || LevelManager.THIS?.field == null)
                {
                    File.WriteAllText("Logs/ink-prepare.result", $"Aguardando teste: Play={EditorApplication.isPlaying}, laboratório={InkLaboratory.IsActive}, tabuleiro={LevelManager.THIS?.field != null}.");
                    return;
                }
                var coordinates = request.Split(':');
                if (coordinates.Length != 3 || !int.TryParse(coordinates[1], out int col) ||
                    !int.TryParse(coordinates[2], out int row)) return;
                var cell = LevelManager.THIS.field.GetSquare(col, row);
                if (cell == null) return;
                cell.DestroyBlock();
                File.WriteAllText("Logs/ink-prepare.result", $"OK: impacto de teste na grama ({col}, {row}).");
                File.Delete(Request);
                return;
            }
            if (request == "capture")
            {
                if (!EditorApplication.isPlaying) return;
                CapturePreview();
                File.Delete(Request);
                return;
            }
            if (request == "half-clear")
            {
                if (EditorApplication.isPlaying)
                {
                    // Author the fixture in Edit Mode: preview-prefab behaviours must not run
                    // against a live board, and the store must get a clean Play initialization.
                    EditorApplication.isPlaying = false;
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                try
                {
                    Prepare();
                    Activate();
                    SessionState.SetBool(PreviewKey, true);
                    previewReadyAt = EditorApplication.timeSinceStartup + 1d;
                    File.Delete(Request);
                    File.WriteAllText("Logs/ink-prepare.result", "OK: teste 50% preparado; 18/36 células com grama clara de duas camadas. Carregando a tela de jogo.");
                    EditorApplication.isPlaying = true;
                }
                catch (Exception exception)
                {
                    File.WriteAllText("Logs/ink-prepare.result", exception.ToString());
                    Debug.LogException(exception);
                    File.Delete(Request);
                }
                return;
            }
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (request == "apply" && EditorApplication.isPlaying && !liveUpdateDone)
                {
                    try
                    {
                        int count = UpdateVisibleGrass(ApplyGameAssets());
                        File.WriteAllText("Logs/ink-prepare.result", $"OK: prefab do jogo e ícones salvos; tela de jogo atualizada, {count} camadas de grama.");
                        File.Delete(Request);
                        liveUpdateDone = true;
                    }
                    catch (Exception exception)
                    {
                        File.WriteAllText("Logs/ink-prepare.result", exception.ToString());
                        Debug.LogException(exception);
                        liveUpdateDone = true;
                    }
                }
                return;
            }
            var activate = request == "activate" || request == "apply";
            liveUpdateDone = false;
            try
            {
                Prepare();
                if (activate) Activate();
                File.WriteAllText("Logs/ink-prepare.result", "OK: prefab do jogo e laboratório atualizados; tapete claro/escuro preenchendo a célula, com desfolhar. " +
                    (activate ? "Laboratório ativo." : "Ative a grama pelo menu para testar."));
                File.Delete(Request);
            }
            catch (InvalidOperationException) when (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // A script reload can restore Play between the guard and the preparation call.
                // Keep the request so the next editor tick can update the active game instead.
            }
            catch (Exception exception)
            {
                File.WriteAllText("Logs/ink-prepare.result", exception.ToString());
                Debug.LogException(exception);
                File.Delete(Request);
            }
        }

        [MenuItem(Menu + "Capturar visual em Play")]
        internal static void CapturePreview()
        {
            if (!EditorApplication.isPlaying || LevelManager.THIS?.field == null) return;
            Application.runInBackground = true;
            Directory.CreateDirectory("Logs");
            var field = LevelManager.THIS.field;
            var first = field.GetSquare(0, 0).transform.position;
            var last = field.GetSquare(field.fieldData.maxCols - 1, field.fieldData.maxRows - 1).transform.position;
            var center = (first + last) * 0.5f;
            var root = new GameObject("GrassPreviewCamera");
            var camera = root.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(Mathf.Abs(first.x - last.x), Mathf.Abs(first.y - last.y)) * 0.5f + 0.7f;
            camera.transform.position = center + Vector3.back * 20f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.08f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            var target = new RenderTexture(1024, 1024, 24);
            var pixels = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
                pixels.Apply();
                File.WriteAllBytes("Logs/grass-preview.png", pixels.EncodeToPNG());
                var grass = field.GetSquares().Select(cell => cell.subSquares.LastOrDefault(layer => layer.GetComponent<InkFloorLayer>() != null))
                    .Where(layer => layer != null).Select(layer => layer.GetComponent<InkFloorLayer>()).ToArray();
                File.WriteAllText("Logs/ink-prepare.result", $"OK: captura em Logs/grass-preview.png; {grass.Length} células com grama, {grass.Count(layer => layer.remainingLayers == 1)} escuras.");
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(pixels);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [MenuItem(Menu + "Preparar visual e laboratório")]
        public static void Prepare()
        {
            Directory.CreateDirectory("Assets/RoyalAves/Laboratory");
            var sprites = ApplyGameAssets();

            var lab = AssetDatabase.LoadAssetAtPath<LevelContainer>(InkLaboratory.LevelPath);
            if (lab == null)
            {
                lab = ScriptableObject.CreateInstance<LevelContainer>();
                AssetDatabase.CreateAsset(lab, InkLaboratory.LevelPath);
            }
            var template = AssetDatabase.LoadAssetAtPath<LevelContainer>("Assets/SweetSugar/Resources/Levels/Level_1.asset");
            lab.levelData = JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(template.levelData));
            lab.levelData.fields = lab.levelData.fields.Take(1).ToList();
            lab.levelData.currentSublevelIndex = lab.levelData.selectedTutorial = 0;
            lab.levelData.limit = 40;
            var field = lab.levelData.fields[0];
            if (field.maxCols < 5 || field.maxRows < 5) throw new InvalidOperationException("Laboratório exige 5x5 células.");
            foreach (var cell in field.levelSquares)
            {
                cell.blocks.Clear();
                cell.blocks.Add(new SquareTypeLayer { squareType = SquareTypes.EmptySquare });
                cell.block = SquareTypes.EmptySquare;
                cell.blockLayer = 1;
                cell.obstacle = SquareTypes.NONE;
                cell.spawners.Clear();
                cell.item = new ItemForEditor();
            }
            int grassCount = 0;
            if (field.maxRows != 6) throw new InvalidOperationException("O teste com três linhas de grama e cobertura de 50% requer seis linhas.");
            for (int row = 0; row < field.maxRows; row++)
            {
                for (int col = 0; col < field.maxCols; col++)
                {
                    if (row < field.maxRows - 3) continue;
                    var cell = field.levelSquares[row * field.maxCols + col];
                    cell.block = SquareTypes.SugarSquare;
                    cell.blockLayer = 2;
                    for (int layer = 0; layer < 2; layer++)
                        cell.blocks.Add(new SquareTypeLayer { squareType = SquareTypes.SugarSquare });
                    grassCount++;
                }
            }
            field.layers = field.levelSquares.Max(cell => cell.blocks.Count);
            lab.levelData.LoadTargetObject();
            int inkType = lab.levelData.GetTargetIndex("SugarSquare");
            lab.levelData.SetTarget(inkType);
            var targets = AssetDatabase.LoadAssetAtPath<TargetLevel>(InkLaboratory.TargetPath);
            if (targets == null)
            {
                targets = ScriptableObject.CreateInstance<TargetLevel>();
                AssetDatabase.CreateAsset(targets, InkLaboratory.TargetPath);
            }
            var inkSprites = new SpriteList();
            for (int i = 0; i < sprites.Length; i++) inkSprites.Add(new SpriteObject { icon = sprites[i], uiSprite = i == 1 });
            targets.targets = new TargetList();
            targets.targets.Add(new TargetObject
            {
                targetType = new TargetType { type = inkType }, sprites = inkSprites,
                CountDrawer = new Count { count = grassCount }, ShowTheScoreForStar = new BoolScoreShow()
            });
            EditorUtility.SetDirty(lab);
            EditorUtility.SetDirty(targets);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Correio Mágico] Teste 50%: {grassCount}/{field.maxCols * field.maxRows} células com grama clara; todas com duas camadas.");
        }

        static void StartRequestedPreview()
        {
            if (!SessionState.GetBool(PreviewKey, false) || !EditorApplication.isPlaying ||
                EditorApplication.timeSinceStartup < previewReadyAt) return;
            var manager = LevelManager.THIS;
            if (manager == null || AI.THIS == null || manager.gameStatus != GameState.Map) return;
            SessionState.SetBool(PreviewKey, false);
            // Debug entry skips the lobby's life/booster purchase path.
            manager.BoostPackage = manager.BoostColorfullBomb = manager.BoostStriped = 0;
            // Keep the lab's animations updating while its editor controls have focus.
            Application.runInBackground = true;
            manager.gameStatus = GameState.PrepareGame;
            int total = manager.field.squaresArray.Length;
            int clear = manager.field.GetSquares().Count(cell =>
                cell.GetSubSquare().GetComponent<InkFloorLayer>()?.remainingLayers == 2);
            File.WriteAllText("Logs/ink-prepare.result", $"OK: tela de teste carregada; {clear}/{total} células com grama clara (50%), meta {clear}.");
        }

        [MenuItem(Menu + "Teste com 50% de grama clara")]
        public static void HalfClearTest()
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(Request, "half-clear");
        }

        static Sprite[] ImportArt()
        {
            var paths = new[] { RoyalAvesAssetImporter.GrassSparseAsset, RoyalAvesAssetImporter.GrassFloorAsset };
            if (paths.Any(path => !File.Exists(path))) throw new FileNotFoundException("Sprites grass e grass-sparse não encontrados.");
            foreach (var path in paths)
            {
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                RoyalAvesAssetImporter.ConfigureGrassFloor(importer);
                importer.SaveAndReimport();
            }
            return paths.Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path)).ToArray();
        }

        static Sprite[] ApplyGameAssets()
        {
            var sprites = ImportArt();
            var source = PrefabUtility.LoadPrefabContents(GamePrefabPath);
            try
            {
                var layers = source.GetComponent<LayeredBlock>().layers;
                if (layers.Length != 2) throw new InvalidOperationException("O piso precisa de duas camadas.");
                for (int i = 0; i < layers.Length; i++)
                {
                    var layer = layers[i];
                    layer.canMoveIn = layer.canMoveOut = true;
                    layer.aboveItem = layer.cannotBeDestroy = layer.undestroyable = false;
                    var marker = layer.GetComponent<InkFloorLayer>() ?? layer.gameObject.AddComponent<InkFloorLayer>();
                    marker.remainingLayers = i + 1;
                    RoyalAvesAssetImporter.GetGrassFit(i == 0 ? RoyalAvesAssetImporter.GrassSparseAsset : RoyalAvesAssetImporter.GrassFloorAsset,
                        out var scale, out var offset);
                    marker.visualScale = scale;
                    marker.visualOffset = offset;
                    var renderer = layer.GetComponent<SpriteRenderer>();
                    renderer.sprite = sprites[i];
                    renderer.color = Color.white;
                }
                PrefabUtility.SaveAsPrefabAsset(source, GamePrefabPath);
                PrefabUtility.SaveAsPrefabAsset(source, InkLaboratory.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(source); }
            UpdateTargetArt(sprites);
            AssetDatabase.SaveAssets();
            return sprites;
        }

        static void UpdateTargetArt(Sprite[] sprites)
        {
            var config = Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");
            int type = config.targets.FindIndex(target => target.name == "SugarSquare");
            if (type < 0) throw new InvalidOperationException("Objetivo SugarSquare não encontrado.");
            config.targets[type].defaultSprites = new SprArrays();
            config.targets[type].defaultSprites.Add(new SprArray { sprites = StateSprites(sprites) });
            EditorUtility.SetDirty(config);
            foreach (var guid in AssetDatabase.FindAssets("t:TargetLevel", new[] { "Assets/SweetSugar/Resources/Levels/Targets", "Assets/RoyalAves/Laboratory" }))
            {
                var targets = AssetDatabase.LoadAssetAtPath<TargetLevel>(AssetDatabase.GUIDToAssetPath(guid));
                if (targets.targets == null) continue;
                bool changed = false;
                foreach (var target in targets.targets)
                {
                    if (target.targetType.type != type) continue;
                    target.sprites = StateSprites(sprites);
                    changed = true;
                }
                if (changed) EditorUtility.SetDirty(targets);
            }
        }

        static SpriteList StateSprites(Sprite[] sprites)
        {
            var list = new SpriteList();
            for (int i = 0; i < sprites.Length; i++) list.Add(new SpriteObject { icon = sprites[i], uiSprite = i == 1 });
            return list;
        }

        [MenuItem(Menu + "Aplicar na tela de jogo")]
        public static void ApplyToGame()
        {
            if (EditorApplication.isPlaying) UpdateVisibleGrass(ApplyGameAssets());
            else { Prepare(); Activate(); }
        }

        static int UpdateVisibleGrass(Sprite[] sprites = null)
        {
            if (sprites == null) sprites = ImportArt();
            int count = 0;
            foreach (var marker in Resources.FindObjectsOfTypeAll<InkFloorLayer>())
            {
                if (!marker.isActiveAndEnabled || !marker.gameObject.scene.IsValid() || marker.IsRemoving) continue;
                int index = Mathf.Clamp(marker.remainingLayers - 1, 0, 1);
                var renderer = marker.GetComponent<SpriteRenderer>();
                renderer.sprite = sprites[index];
                renderer.color = Color.white;
                RoyalAvesAssetImporter.GetGrassFit(index == 0 ? RoyalAvesAssetImporter.GrassSparseAsset : RoyalAvesAssetImporter.GrassFloorAsset,
                    out var scale, out var offset);
                marker.ApplyVisualFit(scale, offset);
                count++;
            }
            Debug.Log($"[Correio Mágico] Tela de jogo atualizada: {count} camadas de grama, tapete claro/escuro encaixado na célula.");
            return count;
        }

        static void Activate()
        {
            SessionState.SetBool(ParcelStackLaboratory.EnabledKey, false);
            SessionState.SetBool(MailCubbyLaboratory.EnabledKey, false);
            SessionState.SetBool(FrankingMachineLaboratory.EnabledKey, false);
            SessionState.SetBool(InkLaboratory.EnabledKey, true);
            InkTestWindow.Open();
        }

        [MenuItem(Menu + "Ativar laboratório para próximo Play")]
        static void Enable() { Prepare(); Activate(); }

        [MenuItem(Menu + "Desativar laboratório")]
        static void Disable() => SessionState.SetBool(InkLaboratory.EnabledKey, false);
    }
}


