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
    public static class ParcelStackTools
    {
        const string Menu = "Royal Aves/Elementos/Pilha de Encomendas/";
        const string PrefabPath = "Assets/SweetSugar/Resources/Blocks/SolidBlock.prefab";
        const string RequestPath = "Logs/parcel-stack-prepare.request";
        const string ArtRoot = "Assets/RoyalAves/Art/Obstacles/ParcelStack/";
        static double nextSetupPoll;
        static bool activationPanelShown;
        const string PreviewKey = "MagicMail.WoodenCratePreview";
        static double previewReadyAt;

        [InitializeOnLoadMethod]
        static void ProcessRequestedSetup()
        {
            EditorApplication.update -= ProcessPendingSetup;
            EditorApplication.update += ProcessPendingSetup;
            EditorApplication.delayCall += ProcessPendingSetup;
            previewReadyAt = EditorApplication.timeSinceStartup + 1d;
        }

        static void ProcessPendingSetup()
        {
            if (EditorApplication.timeSinceStartup < nextSetupPoll) return;
            nextSetupPoll = EditorApplication.timeSinceStartup + 1d;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            StartRequestedPreview();
            if (!File.Exists(RequestPath)) return;
            var request = File.ReadAllText(RequestPath).Trim();
            if (request == "preview")
            {
                if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                try
                {
                    Prepare();
                    SessionState.SetBool(MailCubbyLaboratory.EnabledKey, false);
                    SessionState.SetBool(FrankingMachineLaboratory.EnabledKey, false);
                    SessionState.SetBool(ParcelStackLaboratory.EnabledKey, true);
                    SessionState.SetBool(InkLaboratory.EnabledKey, false);
                    SessionState.SetBool(PreviewKey, true);
                    previewReadyAt = EditorApplication.timeSinceStartup + 1d;
                    File.Delete(RequestPath);
                    File.WriteAllText("Logs/parcel-stack-prepare.result", "OK: quatro caixas de madeira preparadas; carregando teste.");
                    EditorApplication.isPlaying = true;
                }
                catch (Exception exception)
                {
                    File.Delete(RequestPath);
                    File.WriteAllText("Logs/parcel-stack-prepare.result", exception.ToString());
                    Debug.LogException(exception);
                }
                return;
            }
            if (request.StartsWith("hit:", StringComparison.Ordinal))
            {
                if (!EditorApplication.isPlaying || !ParcelStackLaboratory.IsActive || LevelManager.THIS?.field == null) return;
                var coordinates = request.Split(':');
                if (coordinates.Length != 3 || !int.TryParse(coordinates[1], out int col) || !int.TryParse(coordinates[2], out int row)) return;
                var cell = LevelManager.THIS.field.GetSquare(col, row);
                if (cell == null) return;
                cell.DestroyBlock();
                File.Delete(RequestPath);
                File.WriteAllText("Logs/parcel-stack-prepare.result", $"OK: impacto na caixa ({col}, {row}); meta imediatamente após o impacto: {LevelManager.THIS.levelData.TargetCounters.Sum(counter => counter.count)}.");
                return;
            }
            if (request == "status")
            {
                if (!EditorApplication.isPlaying || !ParcelStackLaboratory.IsActive || LevelManager.THIS?.field == null) return;
                var manager = LevelManager.THIS;
                var positions = new[] { new Vector2Int(1, 2), new Vector2Int(3, 2), new Vector2Int(1, 4), new Vector2Int(3, 4) };
                var states = positions.Select(position => manager.field.GetSquare(position.x, position.y)?.GetSubSquare()?.GetComponent<ParcelStackLayer>()?.remainingLayers ?? 0);
                int effects = UnityEngine.Object.FindObjectsByType<RoyalAves.Effects.ParcelTearEffect>(FindObjectsSortMode.None).Length;
                File.Delete(RequestPath);
                File.WriteAllText("Logs/parcel-stack-prepare.result", $"OK: meta={manager.levelData.TargetCounters.Sum(counter => counter.count)}; resistências=[{string.Join(",", states)}]; efeitos finais={effects}; pausa={EditorApplication.isPaused}.");
                return;
            }
            if (request == "activate" && !activationPanelShown)
            {
                SessionState.SetBool(MailCubbyLaboratory.EnabledKey, false);
                SessionState.SetBool(FrankingMachineLaboratory.EnabledKey, false);
                SessionState.SetBool(ParcelStackLaboratory.EnabledKey, true);
                SessionState.SetBool(InkLaboratory.EnabledKey, false);
                ParcelStackTestWindow.Open();
                activationPanelShown = true;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(RequestPath);
            activationPanelShown = false;
            try
            {
                Prepare();
                File.WriteAllText("Logs/parcel-stack-prepare.result", request == "activate"
                    ? "OK: laboratório ATIVO com pacotes de 1, 2, 3 e 4 vidas; meta 4."
                    : "OK: prefab, ícones e laboratório atualizados.");
            }
            catch (Exception exception)
            {
                File.WriteAllText("Logs/parcel-stack-prepare.result", exception.ToString());
                Debug.LogException(exception);
            }
        }

        [MenuItem(Menu + "Teste das quatro caixas de madeira")]
        public static void Preview()
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(RequestPath, "preview");
        }

        static void StartRequestedPreview()
        {
            if (!SessionState.GetBool(PreviewKey, false) || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < previewReadyAt) return;
            var manager = LevelManager.THIS;
            if (manager == null || AI.THIS == null || manager.gameStatus != GameState.Map) return;
            SessionState.SetBool(PreviewKey, false);
            manager.BoostPackage = manager.BoostColorfullBomb = manager.BoostStriped = 0;
            Application.runInBackground = true;
            manager.gameStatus = GameState.PrepareGame;
            ParcelStackTestWindow.Open();
            File.WriteAllText("Logs/parcel-stack-prepare.result", "OK: teste de madeira carregado; quatro caixas com resistências 1, 2, 3 e 4, meta 4.");
        }

        [MenuItem(Menu + "Preparar protótipo e laboratório")]
        public static void Prepare()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Pare o Play Mode antes de preparar o laboratório.");
            var art = ImportArt();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var layers = root.GetComponent<LayeredBlock>();
                if (layers.layers.Length < 3 || layers.layers.Length > 4)
                    throw new InvalidOperationException("SolidBlock deve possuir três ou quatro camadas.");
                if (layers.layers.Length == 3)
                {
                    var fourth = UnityEngine.Object.Instantiate(layers.layers[2].gameObject, root.transform);
                    fourth.name = "SolidBlock (3)";
                    layers.layers = layers.layers.Concat(new[] { fourth.GetComponent<Square>() }).ToArray();
                }
                for (int i = 0; i < layers.layers.Length; i++)
                {
                    var layer = layers.layers[i];
                    layer.canMoveIn = layer.canMoveOut = false;
                    layer.cannotBeDestroy = layer.undestroyable = false;
                    var marker = layer.GetComponent<ParcelStackLayer>() ?? layer.gameObject.AddComponent<ParcelStackLayer>();
                    marker.remainingLayers = i + 1;
                    layer.GetComponent<SpriteRenderer>().sortingOrder = i + 1;
                    if (art != null)
                    {
                        layer.GetComponent<SpriteRenderer>().sprite = art[i];
                        RoyalAvesAssetImporter.GetParcelFit(ArtRoot + $"CM_pilha_estado0{i + 1}_00.png", out var scale, out var offset);
                        marker.visualScale = scale;
                        marker.visualOffset = offset;
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            if (art != null) UpdateTargetArt(art);

            Directory.CreateDirectory("Assets/RoyalAves/Laboratory");
            AssetDatabase.Refresh();
            var source = AssetDatabase.LoadAssetAtPath<LevelContainer>("Assets/SweetSugar/Resources/Levels/Level_1.asset");
            var lab = AssetDatabase.LoadAssetAtPath<LevelContainer>(ParcelStackLaboratory.LevelPath);
            if (lab == null)
            {
                lab = ScriptableObject.CreateInstance<LevelContainer>();
                AssetDatabase.CreateAsset(lab, ParcelStackLaboratory.LevelPath);
            }
            // Serialization creates independent nested lists instead of the template's shallow block copy.
            lab.levelData = JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(source.levelData));
            lab.levelData.fields = lab.levelData.fields.Take(1).ToList();
            lab.levelData.currentSublevelIndex = 0;
            lab.levelData.selectedTutorial = 0;
            lab.levelData.limit = 40;
            var field = lab.levelData.fields[0];
            if (field.maxCols < 6 || field.maxRows < 4) throw new InvalidOperationException("A fase fonte precisa de ao menos 6x4 células.");
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
            var positions = new[] { new Vector2Int(1, 2), new Vector2Int(3, 2), new Vector2Int(1, 4), new Vector2Int(3, 4) };
            if (field.maxRows < 5) throw new InvalidOperationException("Laboratório requer ao menos cinco linhas.");
            for (int i = 0; i < 4; i++)
            {
                var cell = field.levelSquares[positions[i].y * field.maxCols + positions[i].x];
                cell.block = SquareTypes.SolidBlock;
                cell.blockLayer = i + 1;
                for (int j = 0; j <= i; j++) cell.blocks.Add(new SquareTypeLayer { squareType = SquareTypes.SolidBlock });
            }
            field.layers = 5;
            lab.levelData.LoadTargetObject();
            lab.levelData.SetTarget(lab.levelData.GetTargetIndex("SolidBlock"));

            var targets = AssetDatabase.LoadAssetAtPath<TargetLevel>(ParcelStackLaboratory.TargetPath);
            if (targets == null)
            {
                targets = ScriptableObject.CreateInstance<TargetLevel>();
                AssetDatabase.CreateAsset(targets, ParcelStackLaboratory.TargetPath);
            }
            var sprites = StateSprites(art ?? Square.GetBlockPrefab(SquareTypes.SolidBlock)
                .Select(p => p.GetComponent<SpriteRenderer>().sprite).Distinct().ToArray());
            targets.targets = new TargetList();
            targets.targets.Add(new TargetObject
            {
                targetType = new TargetType { type = lab.levelData.GetTargetIndex("SolidBlock") },
                sprites = sprites, CountDrawer = new Count { count = 4 },
                ShowTheScoreForStar = new BoolScoreShow()
            });
            EditorUtility.SetDirty(lab);
            EditorUtility.SetDirty(targets);
            AssetDatabase.SaveAssets();
            Debug.Log("[Correio Mágico] Laboratório: quatro caixas de madeira com resistências 1, 2, 3 e 4. Três travessas → duas → uma → sem travessas. A meta só diminui após a quebra final.");
            Selection.activeObject = lab;
        }

        static Sprite[] ImportArt()
        {
            var paths = Enumerable.Range(1, 4).Select(i => ArtRoot + $"CM_pilha_estado0{i}_00.png").ToArray();
            if (paths.Any(path => !File.Exists(path))) return null;
            foreach (var path in paths)
            {
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                RoyalAvesAssetImporter.ConfigureParcelStack(importer);
                importer.SaveAndReimport();
            }
            return paths.Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path)).ToArray();
        }

        static SpriteList StateSprites(Sprite[] art)
        {
            var list = new SpriteList();
            for (int i = 0; i < art.Length; i++)
                list.Add(new SpriteObject { icon = art[i], uiSprite = i == 0 });
            return list;
        }

        static void UpdateTargetArt(Sprite[] art)
        {
            var config = Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");
            int type = config.targets.FindIndex(target => target.name == "SolidBlock");
            if (type < 0) throw new InvalidOperationException("Objetivo SolidBlock não encontrado.");
            config.targets[type].defaultSprites = new SprArrays();
            config.targets[type].defaultSprites.Add(new SprArray { sprites = StateSprites(art) });
            EditorUtility.SetDirty(config);
            // Keep every existing obstacle objective synchronized with the new sprite references.
            foreach (var guid in AssetDatabase.FindAssets("t:TargetLevel", new[] { "Assets/SweetSugar/Resources/Levels/Targets", "Assets/RoyalAves/Laboratory" }))
            {
                var targetLevel = AssetDatabase.LoadAssetAtPath<TargetLevel>(AssetDatabase.GUIDToAssetPath(guid));
                if (targetLevel.targets == null) continue;
                bool changed = false;
                foreach (var target in targetLevel.targets)
                {
                    if (target.targetType.type != type) continue;
                    target.sprites = StateSprites(art);
                    changed = true;
                }
                if (changed) EditorUtility.SetDirty(targetLevel);
            }
        }

        [MenuItem(Menu + "Ativar laboratório para próximo Play")]
        static void Enable()
        {
            Prepare();
            SessionState.SetBool(MailCubbyLaboratory.EnabledKey, false);
            SessionState.SetBool(FrankingMachineLaboratory.EnabledKey, false);
            SessionState.SetBool(ParcelStackLaboratory.EnabledKey, true);
            SessionState.SetBool(InkLaboratory.EnabledKey, false);
            ParcelStackTestWindow.Open();
            Debug.Log("[Correio Mágico] Laboratório ativo. Entre em Play e abra uma fase pelo lobby. Evite ferramentas pagas; elas ainda usam a economia normal.");
        }

        [MenuItem(Menu + "Desativar laboratório")]
        static void Disable() => SessionState.SetBool(ParcelStackLaboratory.EnabledKey, false);
    }
}


