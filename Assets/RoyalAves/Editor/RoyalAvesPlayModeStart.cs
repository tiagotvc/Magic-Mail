// Play always starts on the title screen, whatever scene happens to be open in the editor.
// Without this, pressing Play with gameStatic.unity open runs Sweet Sugar's old path map: the lobby of Correio Mágico
// lives in game.unity (the scene MapSwitcher points to), so the game only looked right after finishing a level, when
// the level loader brings that scene in.
// Menu: Royal Aves > Começar o Play pela tela inicial (a marca de seleção mostra se está ligado).
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    [InitializeOnLoad]
    static class RoyalAvesPlayModeStart
    {
        const string TitleScene = "Assets/SweetSugar/Scenes/main.unity";
        const string MenuPath = "Royal Aves/Começar o Play pela tela inicial";
        const string PrefKey = "RoyalAves.PlayModeStartScene";

        static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        static RoyalAvesPlayModeStart()
        {
            // The asset database is not ready while the editor is still loading.
            EditorApplication.delayCall += Apply;
        }

        static void Apply()
        {
            if (!Enabled)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(TitleScene);
            if (scene == null)
            {
                Debug.LogWarning($"Royal Aves: não achei {TitleScene}, então o Play continua começando pela cena aberta.");
                return;
            }
            if (EditorSceneManager.playModeStartScene != scene)
                EditorSceneManager.playModeStartScene = scene;
        }

        [MenuItem(MenuPath, false, 200)]
        static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuPath, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }
    }
}
