using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    /// <summary>Opens the actual game asset using Unity's native prefab editing tools.</summary>
    public static class RoyalPostmanNativeAuthoring
    {
        public const string PetsPath = "Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab";

        [MenuItem("Royal Postman/Editar incubadora na Unity", false, 10)]
        public static void OpenPets()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PetsPath);
            if (prefab == null)
            {
                Debug.LogError("Prefab da incubadora não encontrado: " + PetsPath);
                return;
            }

            var stage = PrefabStageUtility.OpenPrefab(PetsPath, null, PrefabStage.Mode.InIsolation);
            if (stage == null) return;

            var root = stage.prefabContentsRoot;
            Selection.activeGameObject = root;
            Tools.current = Tool.Rect;
            var sceneView = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
            sceneView.in2DMode = true;
            var rect = root.transform as RectTransform;
            if (rect != null)
            {
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                sceneView.Frame(new Bounds((corners[0] + corners[2]) * .5f,
                    corners[2] - corners[0]), true);
            }
            sceneView.Show();
            sceneView.Focus();
        }

        [MenuItem("Royal Postman/Editar incubadora na Unity", true)]
        static bool CanOpenPets() => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
