using System;
using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.System.Pool;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public sealed class FlyingLetterTools : EditorWindow
    {
        const string Menu = "Royal Aves/Elementos/Ovo mágico/";
        const string PreviewKey = "MagicMail.FlyingLetterPreview";
        const string Request = "Logs/flying-letter.request";
        const string Result = "Logs/flying-letter.result";
        const string Prefab = "Assets/SweetSugar/Resources/Items/FLYING_LETTER.prefab";
        static double nextPoll;

        [InitializeOnLoadMethod]
        static void Initialize() { EditorApplication.update -= Poll; EditorApplication.update += Poll; }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            nextPoll = EditorApplication.timeSinceStartup + 0.5;
            if (SessionState.GetBool(PreviewKey, false) && EditorApplication.isPlaying &&
                LevelManager.THIS != null && AI.THIS != null && LevelManager.THIS.gameStatus == GameState.Map)
            {
                SessionState.SetBool(PreviewKey, false);
                LevelManager.THIS.BoostPackage = LevelManager.THIS.BoostColorfullBomb = LevelManager.THIS.BoostStriped = 0;
                LevelManager.THIS.gameStatus = GameState.PrepareGame;
            }
            if (!File.Exists(Request)) return;
            string command = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            try
            {
                if (command == "validate") Validate();
                else if (command == "preview") Preview();
                else if (command == "status") File.WriteAllText(Result, Status());
                else if ((command == "break" || command == "capture") && EditorApplication.isPlaying && FlyingLetterLaboratory.IsActive)
                {
                    var letter = LevelManager.THIS.field.GetItems().OfType<ItemFlyingLetter>().FirstOrDefault();
                    bool broken = letter != null && letter.TryBreak();
                    File.WriteAllText(Result, "broken=" + broken + "; " + Status());
                }
            }
            catch (Exception error) { File.WriteAllText(Result, error.ToString()); Debug.LogException(error); }
        }

        [MenuItem(Menu + "Painel do protótipo")]
        public static void Open() => GetWindow<FlyingLetterTools>("Ovo mágico");

        static void Enable()
        {
            SessionState.SetBool(ParcelStackLaboratory.EnabledKey, false);
            SessionState.SetBool(InkLaboratory.EnabledKey, false);
            SessionState.SetBool(MailCubbyLaboratory.EnabledKey, false);
            SessionState.SetBool(FrankingMachineLaboratory.EnabledKey, false);
            SessionState.SetBool(FlyingLetterLaboratory.EnabledKey, true);
        }

        [MenuItem(Menu + "Testar protótipo")]
        public static void Preview()
        {
            Validate();
            Enable();
            Open();
            if (EditorApplication.isPlaying)
            {
                LevelManager.THIS.gameStatus = GameState.PrepareGame;
                return;
            }
            SessionState.SetBool(PreviewKey, true);
            EditorApplication.isPlaying = true;
        }

        [MenuItem(Menu + "Desativar laboratório")]
        public static void Disable()
        {
            SessionState.SetBool(FlyingLetterLaboratory.EnabledKey, false);
            SessionState.SetBool(PreviewKey, false);
        }

        [MenuItem(Menu + "Validar protótipo")]
        public static void Validate()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            var item = prefab != null ? prefab.GetComponent<ItemFlyingLetter>() : null;
            if (item == null || item.Combinable || !item.DestroyByNeighbour || !item.IsExplodable() || item.currentType != ItemsTypes.FLYING_LETTER)
                throw new Exception("Prefab do Ovo mágico inválido.");
            var preview = SweetSugar.Scripts.Editor.LevelMakerEditor.ResolveItemPreviewSprite(prefab, 46, 0);
            if (preview == null || preview != prefab.GetComponentInChildren<SpriteRenderer>().sprite)
                throw new Exception("Prévia do ovo sem componente de cor inválida.");
            var source = AssetDatabase.LoadAssetAtPath<LevelContainer>(FlyingLetterLaboratory.LevelPath);
            var targets = AssetDatabase.LoadAssetAtPath<TargetLevel>(FlyingLetterLaboratory.TargetPath);
            if (source == null || targets == null || ItemFlyingLetter.CountLetters(source.levelData) != 4)
                throw new Exception("Laboratório deve conter quatro ovos.");
            var config = Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");
            int index = config.targets.FindIndex(t => t.name == "FlyingLetters");
            if (index < 0 || targets.targets[0].targetType.type != index || config.targets[index].prefabs[0] != prefab)
                throw new Exception("Meta do Ovo mágico inválida.");
            var pool = Resources.Load<PoolerScriptable>("Scriptable/PoolSettings");
            if (!pool.itemsToPool.Any(p => p.objectToPool == prefab && p.shouldExpand)) throw new Exception("Pool não configurado.");
            Directory.CreateDirectory("Logs");
            File.WriteAllText(Result, "PASS: prévia do Level Editor com o prefab real sem componente de cor; Ovo mágico não combinável, dano adjacente/especial, pool, meta própria e laboratório com quatro ovos.");
        }

        static string Status()
        {
            var manager = LevelManager.THIS;
            if (manager == null || !FlyingLetterLaboratory.IsActive || manager.field == null) return "Laboratório não está em execução.";
            int remaining = manager.levelData.TargetCounters.Where(t => t.targetLevel.name == "FlyingLetters").Sum(t => t.count);
            return $"Ovos no tabuleiro: {manager.field.GetItems().OfType<ItemFlyingLetter>().Count()}; meta: {remaining}.";
        }

        void OnInspectorUpdate() => Repaint();
        void OnGUI()
        {
            EditorGUILayout.LabelField("Ovo mágico — protótipo", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Obstáculo móvel de um impacto. Não forma matches. Faça um match em um dos quatro lados ou acerte com um especial: ele quebra no tabuleiro e a meta diminui na hora. Cai e usa o fluxo de portais das peças.", MessageType.Info);
            if (GUILayout.Button("Testar / reiniciar laboratório")) Preview();
            if (GUILayout.Button("Voltar às fases normais no próximo carregamento")) Disable();
            EditorGUILayout.LabelField(Status(), EditorStyles.wordWrappedLabel);
            EditorGUILayout.HelpBox("Primeira jogada: troque a peça em (2,3) com (2,2), contando a partir de zero. O match quebra o ovo em (2,1). O foguete vertical em (4,4) permite testar o ovo em (4,2).", MessageType.None);
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || !FlyingLetterLaboratory.IsActive))
                if (GUILayout.Button("Quebrar um ovo — diagnóstico"))
                    LevelManager.THIS.field.GetItems().OfType<ItemFlyingLetter>().FirstOrDefault()?.TryBreak();
        }
    }
}
