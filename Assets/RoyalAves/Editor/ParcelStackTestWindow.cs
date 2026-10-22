using RoyalAves.Gameplay;
using RoyalAves.Effects;
using SweetSugar.Scripts.Core;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public sealed class ParcelStackTestWindow : EditorWindow
    {
        static readonly Vector2Int[] Positions =
        {
            new Vector2Int(1, 2), new Vector2Int(3, 2), new Vector2Int(1, 4)
        };

        [MenuItem("Royal Aves/Elementos/Pilha de Encomendas/Painel de teste dos efeitos")]
        public static void Open()
        {
            var window = GetWindow<ParcelStackTestWindow>("Teste das caixas");
            window.minSize = new Vector2(340f, 320f);
        }

        void OnInspectorUpdate() => Repaint();

        void OnGUI()
        {
            EditorGUILayout.LabelField("Caixas de madeira — 1, 2 e 3 vidas", EditorStyles.boldLabel);
            ParcelFeedbackSettings.Speed = EditorGUILayout.Slider("Velocidade do efeito", ParcelFeedbackSettings.Speed, 0.5f, 1.5f);
            ParcelFeedbackSettings.Intensity = EditorGUILayout.Slider("Intensidade", ParcelFeedbackSettings.Intensity, 0.5f, 1.25f);
            ParcelSoundEffects.Volume = EditorGUILayout.Slider("Volume dos pacotes", ParcelSoundEffects.Volume, 0f, 1f);
            if (GUILayout.Button("Restaurar efeitos padrão")) ParcelFeedbackSettings.Reset();
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Travessa superior")) ParcelSoundEffects.Play(3);
                if (GUILayout.Button("Travessa inferior")) ParcelSoundEffects.Play(2);
                if (GUILayout.Button("Conclusão")) ParcelSoundEffects.Play(1);
                EditorGUILayout.EndHorizontal();
            }
            if (!EditorApplication.isPlaying || !ParcelStackLaboratory.IsActive || LevelManager.THIS == null)
            {
                EditorGUILayout.HelpBox("Entre em Play e abra uma fase para carregar o laboratório. Os botões abaixo permitem acertar cada pacote sem gastar jogadas ou boosters.", MessageType.Info);
                EditorGUILayout.LabelField("Laboratório", SessionState.GetBool(ParcelStackLaboratory.EnabledKey, false) ? "Ativo para a próxima fase" : "Desativado");
                return;
            }
            var manager = LevelManager.THIS;
            if (manager.field == null) return;
            for (int i = 0; i < Positions.Length; i++)
            {
                var position = Positions[i];
                var cell = manager.field.GetSquare(position.x, position.y, true);
                var parcel = cell != null ? cell.GetSubSquare().GetComponent<ParcelStackLayer>() : null;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Inicial: {i + 1} vida(s)", GUILayout.Width(125f));
                EditorGUILayout.LabelField(parcel == null ? "Concluído" : parcel.IsFinishing ? "Finalizando…" : $"Restam {parcel.remainingLayers}", GUILayout.Width(100f));
                using (new EditorGUI.DisabledScope(parcel == null || parcel.IsFinishing || LevelManager.GetGameStatus() != GameState.Playing))
                    if (GUILayout.Button("Acertar")) cell.DestroyBlock();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Meta: um objetivo por pacote concluído.");
            EditorGUILayout.HelpBox("Cada tábua removida e a quebra final soltam lascas irregulares de madeira, que caem até sair da tela. A meta conta somente na quebra final. Use o menu de teste das quatro caixas para reiniciar.", MessageType.None);
        }
    }
}
