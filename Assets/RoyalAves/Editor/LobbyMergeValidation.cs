using System;
using System.IO;
using System.Linq;
using RoyalAves.Meta;
using RoyalAves.UI.RoyalPostman2026;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.System;

namespace RoyalAves.EditorTools
{
    // Run in batch mode: validates Unity's imported prefab and the real scene, without saving them.
    public static class LobbyMergeValidation
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("LobbyLiveRestartPending", false)) return;
            SessionState.SetBool("LobbyLiveRestartPending", false);
            EditorApplication.delayCall += () => EditorApplication.EnterPlaymode();
        }

        static void Poll()
        {
            const string victoryRequest = "Logs/victory-canvas-recovery.request";
            if (File.Exists(victoryRequest) && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                File.Delete(victoryRequest);
                var victory = UnityEngine.Object.FindAnyObjectByType<VictoryScreen>();
                if (victory != null && victory.gameObject.activeInHierarchy)
                {
                    // Awake is not rerun after editor hot reload; restore the reference only,
                    // without restarting level logic or entering the Win state again.
                    if (LevelManager.THIS == null)
                        LevelManager.THIS = UnityEngine.Object.FindAnyObjectByType<LevelManager>();
                    victory.GetComponent<Canvas>().enabled = true;
                    Canvas.ForceUpdateCanvases();
                    var controller = victory.GetComponentInParent<LobbyController>();
                    // A script reload clears nonserialized callbacks. Restore only navigation;
                    // never call ShowVictory again, since that would grant rewards twice.
                    var serialized = new SerializedObject(victory);
                    foreach (var entry in new[] { (field: "continueButton", method: "ContinueToLobby"), (field: "replayButton", method: "ReplayLevel") })
                    {
                        var button = serialized.FindProperty(entry.field).objectReferenceValue as UnityEngine.UI.Button;
                        var action = typeof(LobbyController).GetMethod(entry.method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        button.onClick.RemoveAllListeners();
                        button.onClick.AddListener(() => action.Invoke(controller, null));
                        button.interactable = true;
                    }
                    var continueButton = serialized.FindProperty("continueButton").objectReferenceValue as UnityEngine.UI.Button;
                    var rect = (RectTransform)continueButton.transform;
                    var canvas = victory.GetComponent<Canvas>();
                    var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) {
                        position = RectTransformUtility.WorldToScreenPoint(canvas.rootCanvas.worldCamera, rect.TransformPoint(rect.rect.center))
                    };
                    var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                    UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
                    bool reachable = hits.Count > 0 && hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>() == continueButton;
                    File.WriteAllText("Logs/victory-canvas-recovery.result", (reachable ? "PASS" : "FAIL") + ": victory canvas enabled; Continue pointer target=" + (hits.Count > 0 ? hits[0].gameObject.name : "none") + "; rewards not repeated.");
                }
                else File.WriteAllText("Logs/victory-canvas-recovery.result", "No active victory screen; no rewards or progress changed.");
            }
            const string restartRequest = "Logs/lobby-live-restart.request";
            if (File.Exists(restartRequest) && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                File.Delete(restartRequest);
                SessionState.SetBool("LobbyLiveRestartPending", true);
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
                else if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetBool("LobbyLiveRestartPending", false);
                    EditorApplication.delayCall += () => EditorApplication.EnterPlaymode();
                }
            }
            const string clickRequest = "Logs/lobby-live-click.request";
            if (File.Exists(clickRequest) && EditorApplication.isPlaying && !EditorApplication.isCompiling &&
                !EditorApplication.isUpdating && Time.frameCount > 30 && LevelManager.THIS != null &&
                LevelManager.THIS.gameStatus == GameState.Map && UnityEngine.EventSystems.EventSystem.current != null)
            {
                File.Delete(clickRequest);
                try
                {
                    var home = UnityEngine.Object.FindAnyObjectByType<RoyalPostmanLiveHome>();
                    ClickPlay(home);
                    SessionState.SetBool("LobbyLiveClickPending", true);
                    SessionState.SetFloat("LobbyLiveClickAt", (float)EditorApplication.timeSinceStartup + 1f);
                }
                catch (Exception error) { File.WriteAllText("Logs/lobby-live-click.result", "FAIL: " + error); }
            }
            if (SessionState.GetBool("LobbyLiveClickPending", false) && EditorApplication.timeSinceStartup > SessionState.GetFloat("LobbyLiveClickAt", 0))
            {
                SessionState.SetBool("LobbyLiveClickPending", false);
                bool opened = MenuReference.THIS != null && MenuReference.THIS.MenuPlay != null && MenuReference.THIS.MenuPlay.activeInHierarchy;
                File.WriteAllText("Logs/lobby-live-click.result", opened ? "PASS: actual home button raycast and pointer click opened MenuPlay in the user's live editor." : "FAIL: pointer click did not open MenuPlay.");
            }
            const string diagnosticRequest = "Logs/lobby-live-diagnostic.request";
            if (File.Exists(diagnosticRequest) && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                File.Delete(diagnosticRequest);
                var report = new System.Text.StringBuilder();
                report.AppendLine("Playing=" + EditorApplication.isPlaying + "; paused=" + EditorApplication.isPaused + "; frame=" + Time.frameCount + "; state=" + (LevelManager.THIS != null ? LevelManager.THIS.gameStatus.ToString() : "no manager"));
                report.AppendLine("EventSystem.current=" + UnityEngine.EventSystems.EventSystem.current);
                foreach (var input in UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    report.AppendLine("EventSystem=" + input.name + "; enabled=" + input.enabled + "; active=" + input.gameObject.activeInHierarchy + "; module=" + input.currentInputModule);
                foreach (var manager in UnityEngine.Object.FindObjectsByType<LevelManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    report.AppendLine("Manager=" + manager.name + "; enabled=" + manager.enabled + "; active=" + manager.gameObject.activeInHierarchy + "; state=" + manager.gameStatus);
                foreach (var lobby in UnityEngine.Object.FindObjectsByType<LobbyController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    report.AppendLine("Lobby=" + lobby.name + "; active=" + lobby.gameObject.activeInHierarchy + "; scale=" + lobby.transform.lossyScale);
                    foreach (var home in lobby.GetComponentsInChildren<RoyalPostmanLiveHome>(true))
                    {
                        var button = home.transform.Find("LevelButton")?.GetComponent<UnityEngine.UI.Button>();
                        report.AppendLine("Home=" + home.gameObject.activeInHierarchy + "; button=" + button + "; interactable=" + (button != null && button.IsInteractable()));
                        if (button != null && UnityEngine.EventSystems.EventSystem.current != null)
                        {
                            var rect = (RectTransform)button.transform;
                            var canvas = button.GetComponentInParent<Canvas>();
                            var screen = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.TransformPoint(rect.rect.center));
                            var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                            report.AppendLine("Play screen=" + screen + "; Screen=" + Screen.width + "x" + Screen.height + "; rect=" + rect.rect + "; scale=" + rect.lossyScale.ToString("F6") + "; canvasRect=" + ((RectTransform)canvas.transform).rect);
                            foreach (var raycaster in lobby.GetComponentsInChildren<UnityEngine.UI.GraphicRaycaster>(true)) report.AppendLine("Raycaster=" + raycaster.name + "; enabled=" + raycaster.enabled + "; active=" + raycaster.isActiveAndEnabled + "; eventCamera=" + raycaster.eventCamera);
                            foreach (var graphic in button.GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) report.AppendLine("Button graphic=" + graphic.name + "; active=" + graphic.isActiveAndEnabled + "; raycast=" + graphic.raycastTarget + "; depth=" + graphic.depth);
                            foreach (var group in button.GetComponentsInParent<CanvasGroup>(true)) report.AppendLine("Group=" + group.name + "; blocks=" + group.blocksRaycasts + "; interactive=" + group.interactable + "; alpha=" + group.alpha);
                            UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = screen }, hits);
                            foreach (var hit in hits.Take(12)) report.AppendLine("Play hit=" + hit.gameObject.name + "; buttonOwner=" + hit.gameObject.GetComponentInParent<UnityEngine.UI.Button>());
                        }
                    }
                    foreach (var canvas in lobby.GetComponentsInChildren<Canvas>(true))
                        report.AppendLine("Canvas=" + canvas.name + "; active=" + canvas.gameObject.activeInHierarchy + "; mode=" + canvas.renderMode + "; camera=" + canvas.worldCamera + "; plane=" + canvas.planeDistance);
                }
                foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    report.AppendLine("Camera=" + camera.name + "; active=" + camera.isActiveAndEnabled + "; mask=" + camera.cullingMask + "; depth=" + camera.depth + "; near/far=" + camera.nearClipPlane + "/" + camera.farClipPlane);
                File.WriteAllText("Logs/lobby-live-diagnostic.result", report.ToString());
            }
            if (SessionState.GetInt("LobbyMergeSmokeStage", 0) > 0) { PollPlaySmoke(); return; }
            const string request = "Logs/lobby-merge-native.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            Run();
        }

        // Batch-only smoke: exercise startup and the actual pre-play route, without starting a
        // level, spending a life, changing progress or saving a scene/prefab.
        public static void RunPlaySmoke()
        {
            Require(Application.isBatchMode, "Play smoke must run in an isolated batch editor.");
            EditorSceneManager.OpenScene("Assets/SweetSugar/Scenes/game.unity", OpenSceneMode.Single);
            SessionState.SetInt("LobbyMergeSmokeStage", 1);
            SessionState.SetFloat("LobbyMergeSmokeDeadline", (float)EditorApplication.timeSinceStartup + 90f);
            EditorApplication.EnterPlaymode();
        }

        static void PollPlaySmoke()
        {
            try
            {
                Require(EditorApplication.timeSinceStartup < SessionState.GetFloat("LobbyMergeSmokeDeadline", 0),
                    "Startup/pre-play timed out.");
                if (!EditorApplication.isPlaying || LevelManager.THIS == null || LobbyController.Instance == null) return;
                int stage = SessionState.GetInt("LobbyMergeSmokeStage", 0);
                if (stage == 1)
                {
                    if (LevelManager.THIS.gameStatus != GameState.Map || Time.frameCount < 20) return;
                    var lobby = LobbyController.Instance;
                    var content = new SerializedObject(lobby).FindProperty("content").objectReferenceValue as GameObject;
                    Require(content != null && content.activeInHierarchy, "Lobby content is hidden after startup.");
                    Require(content.GetComponentInChildren<RoyalPostmanLiveHome>() != null, "The installed home is not visible.");
                    var home = content.GetComponentInChildren<RoyalPostmanLiveHome>();
                    ClickPlay(home);
                    SessionState.SetInt("LobbyMergeSmokeStage", 2);
                    SessionState.SetFloat("LobbyMergeSmokeCheckAt", (float)EditorApplication.timeSinceStartup + 1f);
                }
                else if (stage == 2 && EditorApplication.timeSinceStartup > SessionState.GetFloat("LobbyMergeSmokeCheckAt", 0))
                {
                    Require(MenuReference.THIS != null && MenuReference.THIS.MenuPlay != null &&
                        MenuReference.THIS.MenuPlay.activeInHierarchy, "Actual pre-play popup did not open.");
                    File.WriteAllText("Logs/lobby-merge-play-smoke.result", "PASS: actual rendered home button raycast and pointer click opened MenuPlay. No level started, life spent or scene saved.");
                    SessionState.SetInt("LobbyMergeSmokeStage", 0);
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception error)
            {
                File.WriteAllText("Logs/lobby-merge-play-smoke.result", "FAIL: " + error);
                SessionState.SetInt("LobbyMergeSmokeStage", 0);
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        static void ClickPlay(RoyalPostmanLiveHome home)
        {
                    Require(home != null, "Home is missing.");
                    var button = home.transform.Find("LevelButton").GetComponent<UnityEngine.UI.Button>();
                    Canvas.ForceUpdateCanvases();
                    var rect = (RectTransform)button.transform;
                    var canvas = button.GetComponentInParent<Canvas>();
                    var eventSystem = UnityEngine.EventSystems.EventSystem.current;
                    Require(eventSystem != null && button.IsInteractable(), "Play button or EventSystem unavailable.");
                    var pointer = new UnityEngine.EventSystems.PointerEventData(eventSystem) {
                        position = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.TransformPoint(rect.rect.center)),
                        button = UnityEngine.EventSystems.PointerEventData.InputButton.Left
                    };
                    var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                    eventSystem.RaycastAll(pointer, hits);
                    Require(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>() == button,
                        "Play is blocked in actual raycast: " + string.Join(", ", hits.Take(8).Select(hit => hit.gameObject.name)));
                    UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
        }

        public static void Run()
        {
            try
            {
                const string path = "Assets/RoyalAves/Prefabs/RoyalAvesLobby.prefab";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Require(prefab != null && prefab.GetComponent<LobbyController>() != null,
                    "Imported prefab root must contain LobbyController.");
                Require(prefab.GetComponentsInChildren<SettingsScreen>(true).Count(screen => screen.GetComponent<RoyalPostmanSettingsFit>() != null) == 1,
                    "The shared settings screen must remain under the lobby.");
                var kit = new SerializedObject(Resources.Load<UnityEngine.Object>("RoyalAvesWorldMapKit"));
                Require(kit.FindProperty("mapUiTemplate").objectReferenceValue == prefab,
                    "Map kit must reference the lobby root, not its settings window.");
                var scene = EditorSceneManager.OpenPreviewScene("Assets/SweetSugar/Scenes/game.unity");
                try
                {
                var lobbies = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LobbyController>(true)).ToArray();
                Require(lobbies.Length == 1 && lobbies[0].gameObject.activeInHierarchy && lobbies[0].enabled,
                    "The real game scene must contain one enabled active lobby.");
                Require(lobbies[0].GetComponent<Canvas>().enabled, "The real home canvas must be enabled in the scene.");
                var victories = lobbies[0].GetComponentsInChildren<VictoryScreen>(true);
                Require(victories.Length == 1 && victories[0].GetComponent<Canvas>().enabled,
                    "The real victory canvas must be enabled even while its GameObject is hidden until a win.");
                var serialized = new SerializedObject(lobbies[0]);
                foreach (var field in new[] { "content", "playButton", "coinsButton", "livesButton", "modal" })
                    Require(serialized.FindProperty(field).objectReferenceValue != null, "Missing lobby field: " + field);
                Require(lobbies[0].GetComponentsInChildren<SettingsScreen>(true).Count(screen => screen.GetComponent<RoyalPostmanSettingsFit>() != null) == 1,
                    "Scene instance must retain the settings screen.");
                File.WriteAllText("Logs/lobby-merge-native.result", "PASS: imported lobby root, settings hierarchy, map reference, active scene controller and required UI references. Scene not saved. Play Mode not executed.");
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
            catch (Exception error)
            {
                File.WriteAllText("Logs/lobby-merge-native.result", "FAIL: " + error);
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
