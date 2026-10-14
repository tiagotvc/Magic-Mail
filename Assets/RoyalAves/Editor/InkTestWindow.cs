using System.Linq;
using RoyalAves.Gameplay;
using RoyalAves.Effects;
using SweetSugar.Scripts.Core;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public sealed class InkTestWindow : EditorWindow
    {
        Vector2 scroll;

        [MenuItem("Royal Aves/Elementos/Grama/Painel de teste")]
        public static void Open() => GetWindow<InkTestWindow>("Teste da grama").minSize = new Vector2(400, 360);
        void OnInspectorUpdate() => Repaint();

        void OnGUI()
        {
            EditorGUILayout.LabelField("Grama — tapete claro e escuro", EditorStyles.boldLabel);
            GrassFeedbackSettings.Speed = EditorGUILayout.Slider("Velocidade", GrassFeedbackSettings.Speed, 0.5f, 1.5f);
            GrassFeedbackSettings.Dispersion = EditorGUILayout.Slider("Dispersão das folhas", GrassFeedbackSettings.Dispersion, 0.5f, 1.25f);
            if (GUILayout.Button("Restaurar efeito padrão")) GrassFeedbackSettings.Reset();
            EditorGUILayout.HelpBox("Teste com 50% de grama clara: as três últimas linhas completas, 18 de 36 células, todas com duas camadas. Meta: limpar as 18 células.", MessageType.Info);
            if (!EditorApplication.isPlaying || !InkLaboratory.IsActive || LevelManager.THIS?.field == null)
            {
                EditorGUILayout.LabelField("Ative pelo menu Grama, entre em Play e abra uma fase.");
                return;
            }
            var manager = LevelManager.THIS;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            var field = manager.field.fieldData;
            for (int index = 0; index < field.maxCols * field.maxRows; index++)
            {
                if (!field.levelSquares[index].blocks.Any(layer => layer.squareType == SweetSugar.Scripts.Blocks.SquareTypes.SugarSquare)) continue;
                var pos = new Vector2Int(index % field.maxCols, index / field.maxCols);
                var cell = manager.field.GetSquare(pos.x, pos.y, true);
                var ink = cell?.subSquares.LastOrDefault(layer => layer.GetComponent<InkFloorLayer>() != null)?.GetComponent<InkFloorLayer>();
                var exposed = cell?.GetSubSquare();
                var parcel = exposed?.GetComponent<ParcelStackLayer>();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"C{pos.x + 1}, L{pos.y + 1}: " + (ink == null ? "removida" : ink.IsRemoving ? "Desfolhando…" : $"{ink.remainingLayers} camada(s)"), GUILayout.Width(170));
                using (new EditorGUI.DisabledScope(ink == null || ink.IsRemoving || (parcel != null && parcel.IsFinishing) || LevelManager.GetGameStatus() != GameState.Playing))
                {
                    if (GUILayout.Button(parcel == null ? "Impacto na célula" : "Impacto no pacote")) cell.DestroyBlock();
                    if (GUILayout.Button("Impacto vizinho")) cell.GetNeighborLeft(true)?.DestroyBlock();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.HelpBox("Impacto na célula remove uma camada exposta. Impacto vizinho mantém a grama e pode danificar o pacote. Teste também matches e especiais no tabuleiro; os botões são apenas atalhos de dano.", MessageType.None);
        }
    }
}
