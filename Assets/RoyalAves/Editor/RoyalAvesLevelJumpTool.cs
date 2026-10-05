// Jumps the saved progress straight to any level, so the lobby's "Nível N" button offers exactly that one — for
// testing a level far into the game without playing (or faking) every level before it.
// Menu: Royal Aves > Ir para o nível...
//
// Levels are won in order and the save only remembers "the highest one won" (MetaProgress.SetNextLevel), so jumping
// to 100 does mark 1-99 as already won; there is no way to test level 100 without that. It does not hand out the
// stars those levels would have given — this only moves the level counter, not the agency's upgrades.
using RoyalAves.Meta;
using SweetSugar.Scripts.Level;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public class RoyalAvesLevelJumpTool : EditorWindow
    {
        int level = 1;

        [MenuItem("Royal Aves/Ir para o nível...")]
        static void Open()
        {
            var window = GetWindow<RoyalAvesLevelJumpTool>(true, "Ir para o nível", true);
            window.level = Mathf.Max(1, MetaProgress.NextLevel);
            window.minSize = window.maxSize = new Vector2(280, 90);
        }

        void OnGUI()
        {
            var total = LoadingManager.GetLastLevelNum();
            EditorGUILayout.LabelField($"Fases no jogo: 1 a {total}");
            level = EditorGUILayout.IntField("Nível", level);
            level = Mathf.Clamp(level, 1, Mathf.Max(1, total));

            GUILayout.Space(8);
            if (GUILayout.Button($"Ir para o nível {level}"))
            {
                MetaProgress.SetNextLevel(level);
                if (EditorApplication.isPlaying && LobbyController.Instance != null) LobbyController.Instance.RefreshNow();
                Debug.Log($"[Correio Mágico] Próximo nível ajustado para {level} (marca 1-{level - 1} como já vencidos).");
                Close();
            }
        }
    }
}
