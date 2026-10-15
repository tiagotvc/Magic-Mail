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
    public static class FrankingMachineLaboratoryTools
    {
        const string Menu = "Royal Aves/Elementos/Máquina de Franquear/";
        const string Request = "Logs/franking-machine-lab.request";
        const string PreviewKey = "MagicMail.FrankingMachinePreview";
        const string VisualPath = "Assets/RoyalAves/Prefabs/Elements/FrankingMachine.prefab";
        static double nextPoll;

        [MenuItem(Menu + "Teste no tabuleiro")]
        public static void Preview()
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(Request, "preview");
        }

        [MenuItem(Menu + "Desativar laboratório")]
        public static void Disable()
        {
            SessionState.SetBool(FrankingMachineLaboratory.EnabledKey, false);
            SessionState.SetBool(PreviewKey, false);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        }

        [MenuItem(Menu + "Preparar laboratório")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Pare o Play antes de preparar o laboratório.");
            FrankingMachineTools.Build();
            Directory.CreateDirectory("Assets/RoyalAves/Laboratory");
            AssetDatabase.Refresh();
            var visual = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath);
            var letter = visual.GetComponent<MailCubbyAnimation>().letters[0].sprite;
            // Reuse only the existing cell's blocking flags; no changes to the campaign prefab.
            var sourceCell = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SweetSugar/Resources/Blocks/SolidBlock.prefab").GetComponent<LayeredBlock>().layers[0];
            var cellRoot = UnityEngine.Object.Instantiate(sourceCell.gameObject);
            try
            {
                cellRoot.name = "FrankingMachineCell";
                foreach (var parcel in cellRoot.GetComponents<ParcelStackLayer>()) UnityEngine.Object.DestroyImmediate(parcel);
                var cell = cellRoot.GetComponent<Square>();
                cell.type = SquareTypes.SolidBlock;
                cell.canMoveIn = cell.canMoveOut = false;
                cell.undestroyable = cell.cannotBeDestroy = false;
                cell.sizeInSquares = Vector2Int.one;
                cellRoot.GetComponent<SpriteRenderer>().sprite = letter;
                cellRoot.GetComponent<SpriteRenderer>().enabled = false;
                cellRoot.AddComponent<FrankingMachineCell>().visualPrefab = visual;
                PrefabUtility.SaveAsPrefabAsset(cellRoot, FrankingMachineLaboratory.CellPrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(cellRoot); }

            var source = AssetDatabase.LoadAssetAtPath<LevelContainer>("Assets/SweetSugar/Resources/Levels/Level_1.asset");
            var lab = AssetDatabase.LoadAssetAtPath<LevelContainer>(FrankingMachineLaboratory.LevelPath);
            if (lab == null) { lab = ScriptableObject.CreateInstance<LevelContainer>(); AssetDatabase.CreateAsset(lab, FrankingMachineLaboratory.LevelPath); }
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
            field.frankingMachines = new System.Collections.Generic.List<FrankingMachinePlacement>();
            var machineCell = field.levelSquares[2 * field.maxCols + 2];
            machineCell.block = SquareTypes.FrankingMachine;
            machineCell.blocks.Add(new SquareTypeLayer { squareType = SquareTypes.FrankingMachine });
            field.layers = 2;
            lab.levelData.LoadTargetObject();
            int targetIndex = lab.levelData.GetTargetIndex("MachineLetters");
            lab.levelData.SetTarget(targetIndex);
            var targets = AssetDatabase.LoadAssetAtPath<TargetLevel>(FrankingMachineLaboratory.TargetPath);
            if (targets == null) { targets = ScriptableObject.CreateInstance<TargetLevel>(); AssetDatabase.CreateAsset(targets, FrankingMachineLaboratory.TargetPath); }
            targets.targets = new TargetList();
            targets.targets.Add(new TargetObject
            {
                targetType = new TargetType { type = targetIndex },
                sprites = new SpriteList { new SpriteObject { icon = letter, uiSprite = true } },
                CountDrawer = new Count { count = 8 },
                ShowTheScoreForStar = new BoolScoreShow()
            });
            EditorUtility.SetDirty(lab);
            EditorUtility.SetDirty(targets);
            AssetDatabase.SaveAssets();
            Debug.Log("Máquina de Franquear: laboratório com máquina 1x1 dentro do grid, oito cartas e 40 jogadas.");
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
                    FrankingMachineTestWindow.Open();
                }
            }
            if (!File.Exists(Request)) return;
            string command = File.ReadAllText(Request).Trim();
            if (command == "status" || command == "hit" || command == "hit-all" || command.StartsWith("hit:"))
            {
                var controller = UnityEngine.Object.FindAnyObjectByType<FrankingMachineCell>();
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
                File.WriteAllText("Logs/franking-machine-lab.result", $"OK: emitidas={controller.Collected}; meta={LevelManager.THIS.levelData.TargetCounters.Sum(t => t.count)}; animando={controller.Animation.IsAnimating}.");
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
                SessionState.SetBool(MailCubbyLaboratory.EnabledKey, false);
                SessionState.SetBool(FrankingMachineLaboratory.EnabledKey, true);
                File.Delete(Request);
                File.WriteAllText("Logs/franking-machine-lab.result", "OK: laboratório preparado; gerador 1x1, meta de oito cartas.");
                if (command == "preview")
                {
                    SessionState.SetBool(PreviewKey, true);
                    EditorApplication.isPlaying = true;
                }
                FrankingMachineTestWindow.Open();
            }
            catch (Exception e)
            {
                File.Delete(Request);
                File.WriteAllText("Logs/franking-machine-lab.result", e.ToString());
                Debug.LogException(e);
            }
        }
    }

    public sealed class FrankingMachineTestWindow : EditorWindow
    {
        [MenuItem("Royal Aves/Elementos/Máquina de Franquear/Painel do laboratório")]
        public static void Open() { GetWindow<FrankingMachineTestWindow>("Teste do Máquina de Franquear").minSize = new Vector2(350, 300); }
        void OnInspectorUpdate() => Repaint();
        void OnGUI()
        {
            EditorGUILayout.LabelField("Máquina de Franquear — gerador de uma célula", EditorStyles.boldLabel);
            if (GUILayout.Button("Abrir / reiniciar teste no tabuleiro")) FrankingMachineLaboratoryTools.Preview();
            if (GUILayout.Button("Desativar laboratório")) FrankingMachineLaboratoryTools.Disable();
            var controller = Application.isPlaying ? UnityEngine.Object.FindAnyObjectByType<FrankingMachineCell>() : null;
            if (controller == null || controller.Animation == null || LevelManager.THIS == null || LevelManager.THIS.levelData == null)
            {
                EditorGUILayout.HelpBox("Abra o teste no tabuleiro. A máquina ocupa uma célula e gera cartas até atingir a meta.", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField("Gerador", "Fixo, uma carta por impacto");
            
            EditorGUILayout.LabelField("Cartas coletadas", controller.Collected.ToString());
            EditorGUILayout.LabelField("Meta restante", LevelManager.THIS.levelData.TargetCounters.Sum(t => t.count).ToString());
            EditorGUILayout.LabelField("Animação", controller.IsBreaking ? "Quebrando a madeira" : controller.IsCleared ? "Quatro células liberadas" : controller.Animation.IsAnimating ? "Carta em entrega" : "Parado");
            controller.Animation.emissionDuration = EditorGUILayout.Slider("Duração do voo", controller.Animation.emissionDuration, 0.3f, 1.5f);
            using (new EditorGUI.DisabledScope(controller.Animation.IsAnimating || LevelManager.THIS.levelData.TargetCounters.All(t => t.count == 0) || LevelManager.GetGameStatus() != GameState.Playing))
                if (GUILayout.Button("Acionar máquina — entregar carta")) controller.AcceptImpact(true);
            EditorGUILayout.HelpBox("Cada impacto válido produz uma carta, com pirueta e rastro até a meta. O gerador permanece fixo e para ao completar o objetivo.", MessageType.None);
        }
    }
}



