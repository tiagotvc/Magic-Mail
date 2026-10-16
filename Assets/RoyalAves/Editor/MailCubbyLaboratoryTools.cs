using System;
using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class MailCubbyLaboratoryTools
    {
        const string Menu = "Royal Aves/Elementos/Escaninho/";
        const string Request = "Logs/mail-cubby-lab.request";
        const string PreviewKey = "MagicMail.MailCubbyPreview";
        const string VisualPath = "Assets/RoyalAves/Prefabs/Elements/MailCubby.prefab";
        static double nextPoll;

        [MenuItem(Menu + "Teste no tabuleiro 2x2")]
        public static void Preview()
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(Request, "preview");
        }

        [MenuItem(Menu + "Desativar laboratório")]
        public static void Disable()
        {
            SessionState.SetBool(MailCubbyLaboratory.EnabledKey, false);
            SessionState.SetBool(PreviewKey, false);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        }

        [MenuItem(Menu + "Preparar laboratório 2x2")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Pare o Play antes de preparar o laboratório.");
            MailCubbyTools.Build();
            Directory.CreateDirectory("Assets/RoyalAves/Laboratory");
            AssetDatabase.Refresh();
            var visual = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath);
            var letter = visual.GetComponent<MailCubbyAnimation>().letters[0].sprite;
            // Reuse only the existing cell's blocking flags; no changes to the campaign prefab.
            var sourceCell = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SweetSugar/Resources/Blocks/SolidBlock.prefab").GetComponent<LayeredBlock>().layers[0];
            var cellRoot = UnityEngine.Object.Instantiate(sourceCell.gameObject);
            try
            {
                cellRoot.name = "MailCubbyCell";
                foreach (var parcel in cellRoot.GetComponents<ParcelStackLayer>()) UnityEngine.Object.DestroyImmediate(parcel);
                var cell = cellRoot.GetComponent<Square>();
                cell.type = SquareTypes.SolidBlock;
                cell.canMoveIn = cell.canMoveOut = false;
                cell.undestroyable = cell.cannotBeDestroy = false;
                cell.sizeInSquares = Vector2Int.one;
                cellRoot.GetComponent<SpriteRenderer>().sprite = letter;
                cellRoot.GetComponent<SpriteRenderer>().enabled = false;
                cellRoot.AddComponent<MailCubbyCell>().visualPrefab = visual;
                PrefabUtility.SaveAsPrefabAsset(cellRoot, MailCubbyLaboratory.CellPrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(cellRoot); }

            var source = AssetDatabase.LoadAssetAtPath<LevelContainer>("Assets/SweetSugar/Resources/Levels/Level_1.asset");
            var lab = AssetDatabase.LoadAssetAtPath<LevelContainer>(MailCubbyLaboratory.LevelPath);
            if (lab == null) { lab = ScriptableObject.CreateInstance<LevelContainer>(); AssetDatabase.CreateAsset(lab, MailCubbyLaboratory.LevelPath); }
            lab.levelData = JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(source.levelData));
            lab.levelData.fields = lab.levelData.fields.Take(1).ToList();
            lab.levelData.currentSublevelIndex = 0;
            lab.levelData.selectedTutorial = 0;
            lab.levelData.limit = 40;
            var field = lab.levelData.fields[0];
            if (field.maxCols < 6 || field.maxRows < 6) throw new InvalidOperationException("A fase fonte deve ter ao menos 6x6.");
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
            for (int row = 2; row <= 3; row++)
                for (int col = 2; col <= 3; col++)
                {
                    var cell = field.levelSquares[row * field.maxCols + col];
                    cell.block = SquareTypes.SolidBlock;
                    cell.blocks.Add(new SquareTypeLayer { squareType = SquareTypes.SolidBlock });
                }
            field.layers = 2;
            lab.levelData.LoadTargetObject();
            int targetIndex = lab.levelData.GetTargetIndex("SolidBlock");
            lab.levelData.SetTarget(targetIndex);
            var targets = AssetDatabase.LoadAssetAtPath<TargetLevel>(MailCubbyLaboratory.TargetPath);
            if (targets == null) { targets = ScriptableObject.CreateInstance<TargetLevel>(); AssetDatabase.CreateAsset(targets, MailCubbyLaboratory.TargetPath); }
            targets.targets = new TargetList();
            targets.targets.Add(new TargetObject
            {
                targetType = new TargetType { type = targetIndex },
                sprites = new SpriteList { new SpriteObject { icon = letter, uiSprite = true } },
                CountDrawer = new Count { count = 4 },
                ShowTheScoreForStar = new BoolScoreShow()
            });
            EditorUtility.SetDirty(lab);
            EditorUtility.SetDirty(targets);
            AssetDatabase.SaveAssets();
            Debug.Log("Escaninho: laboratório com um objeto 2x2, quatro cartas e 40 jogadas.");
        }

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            nextPoll = EditorApplication.timeSinceStartup + 0.5;
            if (SessionState.GetBool(PreviewKey, false) && EditorApplication.isPlaying)
            {
                var manager = LevelManager.THIS;
                if (manager != null && AI.THIS != null && manager.gameStatus == GameState.Map)
                {
                    SessionState.SetBool(PreviewKey, false);
                    manager.BoostPackage = manager.BoostColorfullBomb = manager.BoostStriped = 0;
                    Application.runInBackground = true;
                    manager.gameStatus = GameState.PrepareGame;
                    MailCubbyTestWindow.Open();
                }
            }
            if (!File.Exists(Request)) return;
            string command = File.ReadAllText(Request).Trim();
            if (command == "status" || command == "hit" || command == "hit-all" || command.StartsWith("hit:"))
            {
                var controller = UnityEngine.Object.FindAnyObjectByType<MailCubbyBoardController>();
                if (controller == null) return;
                if (command == "hit") controller.AcceptImpact(true);
                if (command == "hit-all")
                    foreach (var cell in controller.Cells) cell.DestroyBlock();
                if (command.StartsWith("hit:"))
                {
                    var parts = command.Split(':');
                    if (parts.Length == 3 && int.TryParse(parts[1], out int col) && int.TryParse(parts[2], out int row))
                        LevelManager.THIS.field.GetSquare(col, row)?.DestroyBlock();
                }
                File.WriteAllText("Logs/mail-cubby-lab.result", $"OK: portas={controller.Animation.DoorsAttached}; estoque={controller.Animation.Remaining}; coletadas={controller.Collected}; meta={LevelManager.THIS.levelData.TargetCounters.Sum(t => t.count)}; animando={controller.Animation.IsAnimating}.");
                File.Delete(Request);
                return;
            }
            if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            try
            {
                Prepare();
                SessionState.SetBool(InkLaboratory.EnabledKey, false);
                SessionState.SetBool(ParcelStackLaboratory.EnabledKey, false);
                SessionState.SetBool(FrankingMachineLaboratory.EnabledKey, false);
                SessionState.SetBool(MailCubbyLaboratory.EnabledKey, true);
                File.Delete(Request);
                File.WriteAllText("Logs/mail-cubby-lab.result", "OK: laboratório preparado; objeto 2x2, meta de quatro cartas.");
                if (command == "preview")
                {
                    SessionState.SetBool(PreviewKey, true);
                    EditorApplication.isPlaying = true;
                }
                MailCubbyTestWindow.Open();
            }
            catch (Exception e)
            {
                File.Delete(Request);
                File.WriteAllText("Logs/mail-cubby-lab.result", e.ToString());
                Debug.LogException(e);
            }
        }
    }

    public sealed class MailCubbyTestWindow : EditorWindow
    {
        [MenuItem("Royal Aves/Elementos/Escaninho/Painel do laboratório")]
        public static void Open() { GetWindow<MailCubbyTestWindow>("Teste do Escaninho").minSize = new Vector2(350, 300); }
        void OnInspectorUpdate() => Repaint();
        void OnGUI()
        {
            EditorGUILayout.LabelField("Armário postal — um objeto, quatro células", EditorStyles.boldLabel);
            if (GUILayout.Button("Abrir / reiniciar teste no tabuleiro")) MailCubbyLaboratoryTools.Preview();
            if (GUILayout.Button("Desativar laboratório")) MailCubbyLaboratoryTools.Disable();
            var controller = Application.isPlaying ? UnityEngine.Object.FindAnyObjectByType<MailCubbyBoardController>() : null;
            if (controller == null || controller.Animation == null || LevelManager.THIS == null || LevelManager.THIS.levelData == null)
            {
                EditorGUILayout.HelpBox("O teste usa o mesmo fluxo dos laboratórios de caixa e grama. Abra a cena de jogo e use o botão acima. O Escaninho ocupa o centro, bloqueando quatro células.", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField("Cartas no estoque", controller.Animation.Remaining.ToString());
            EditorGUILayout.LabelField("Portas", controller.Animation.DoorsAttached ? "Primeiro impacto remove ambas" : "Removidas");
            EditorGUILayout.LabelField("Cartas coletadas", controller.Collected.ToString());
            EditorGUILayout.LabelField("Meta restante", LevelManager.THIS.levelData.TargetCounters.Sum(t => t.count).ToString());
            EditorGUILayout.LabelField("Animação", controller.IsBreaking ? "Quebrando a madeira" : controller.IsCleared ? "Quatro células liberadas" : controller.Animation.IsAnimating ? "Carta em entrega" : "Parado");
            controller.Animation.emissionDuration = EditorGUILayout.Slider("Duração do voo", controller.Animation.emissionDuration, 0.3f, 1.5f);
            using (new EditorGUI.DisabledScope(controller.Animation.Remaining == 0 || controller.Animation.IsAnimating || LevelManager.GetGameStatus() != GameState.Playing))
                if (GUILayout.Button(controller.Animation.DoorsAttached ? "Primeiro impacto — arrancar portas" : "Impacto de teste — entregar carta")) controller.AcceptImpact(true);
            EditorGUILayout.HelpBox("1º impacto: arranca as duas portas, sem coletar. 2º–5º: entrega uma carta por impacto, com pirueta e rastro até a meta. Após a última entrega, a madeira quebra e libera as quatro células.", MessageType.None);
        }
    }
}

