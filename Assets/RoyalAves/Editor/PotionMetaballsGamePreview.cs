using System.IO;
using RoyalAves.Effects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    [InitializeOnLoad]
    public static class PotionMetaballsGamePreview
    {
        const string Key = "MagicMail.MetaballsGamePreview";
        const string PendingKey = "MagicMail.MetaballsGamePreview.Pending";
        const string Request = "Logs/potion-metaballs-game.request";
        static PotionMetaballsGamePreview()
        {
            EditorApplication.update += Poll;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) SessionState.SetBool(Key, false);
            };
        }

        [MenuItem("Royal Aves/Elementos/Poções/Testar metaballs no jogo")]
        public static void Open()
        {
            SessionState.SetBool(Key, false);
            SessionState.SetBool(PendingKey, false);
            RouteCentralLaboratoryTools.PreviewMetaballs();
        }

        [MenuItem("Royal Aves/Elementos/Poções/Prévia visual metaballs (sem jogadas)")]
        public static void OpenVisual()
        {
            // Always enter the dedicated scene. Reusing an active game/map
            // leaves its camera, UI and board layout in charge of the preview.
            SessionState.SetBool(PendingKey, true);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
                return;
            }
            OpenPending();
        }

        static void OpenPending()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(PendingKey, false);
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/RoyalAves/Laboratory/RouteCentralVisual.unity");
            SessionState.SetBool(Key, true);
            EditorApplication.isPaused = false;
            EditorApplication.isPlaying = true;
            var gameView = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameView != null) EditorWindow.GetWindow(gameView).Focus();
        }

        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (File.Exists(Request))
            {
                File.Delete(Request);
                Open();
                return;
            }
            if (SessionState.GetBool(PendingKey, false)) { OpenPending(); return; }
            if (!EditorApplication.isPlaying || !SessionState.GetBool(Key, false)) return;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/RoyalAves/Shaders/PotionMetaballsPreview.shader");
            if (shader == null || !shader.isSupported) return;
            foreach (var prototype in Object.FindObjectsByType<RouteCentralPrototype>(FindObjectsSortMode.None))
            {
                prototype.metaballsPreview = true;
                prototype.metaballsShader = shader;
            }
        }
    }
}
