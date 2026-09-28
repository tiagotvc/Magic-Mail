// Shared checks for the Royal Aves editor menus.
using UnityEditor;
using UnityEditor.SceneManagement;

namespace RoyalAves.EditorTools
{
    static class RoyalAvesTools
    {
        // The tools open and save scenes, which Unity does not allow in Play mode.
        public static bool CanRunTool()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Royal Aves", "Pare o modo Play (botão ▶ no topo do Unity) antes de usar este comando.", "OK");
                return false;
            }
            return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }
    }
}
