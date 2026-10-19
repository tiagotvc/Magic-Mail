using System.IO;
using RoyalAves.Effects;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    [InitializeOnLoad]
    public static class RocketPropulsorPreview
    {
        const string Request = "Logs/rocket-propulsor-preview.request";
        static double captureAt;
        static GameObject cameraRoot;
        static GameObject reference;
        static float previousTimeScale;
        static readonly Vector3 Origin = new Vector3(1000, 1000, 0);

        static RocketPropulsorPreview() { EditorApplication.update += Poll; }

        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!Application.isPlaying)
            {
                if (File.Exists(Request) && !EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorApplication.isPlaying = true;
                return;
            }
            if (File.Exists(Request)) EditorApplication.isPaused = false;
            if (captureAt > 0)
            {
                EditorApplication.isPaused = false;
                Time.timeScale = 1f;
            }
            if (captureAt > 0 && Time.time >= captureAt)
            {
                captureAt = 0;
                var camera = cameraRoot.GetComponent<Camera>();
                var target = new RenderTexture(1024, 1024, 24);
                var pixels = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
                var previous = RenderTexture.active;
                try
                {
                    camera.targetTexture = target;
                    camera.Render();
                    RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0);
                    pixels.Apply();
                    File.WriteAllBytes("Logs/rocket-propulsor-preview.png", pixels.EncodeToPNG());
                    File.WriteAllText("Logs/rocket-propulsor-preview.result", "OK: horizontal and vertical RocketSplitEffect.Play invoked; capture generated. Original rocket shown above for scale.");
                }
                finally
                {
                    camera.targetTexture = null;
                    RenderTexture.active = previous;
                    Object.Destroy(target);
                    Object.Destroy(pixels);
                    Object.Destroy(cameraRoot);
                    Object.Destroy(reference);
                    Time.timeScale = previousTimeScale;
                }
            }
            if (!File.Exists(Request)) return;
            File.Delete(Request);
            previousTimeScale = Time.timeScale;
            Time.timeScale = 1f;
            Application.runInBackground = true;
            reference = new GameObject("RocketSizeReference");
            reference.transform.position = Origin + Vector3.up * 1.4f;
            reference.transform.localScale = Vector3.one * (0.42f * 0.88f);
            reference.AddComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/Specials/capsule-horizontal.png");
            for (var i = 0; i < 2; i++)
            {
                var source = new GameObject("RocketPreviewSource");
                source.transform.position = Origin + Vector3.down * i * 1.2f;
                source.transform.localScale = reference.transform.localScale;
                source.AddComponent<SpriteRenderer>().sprite = reference.GetComponent<SpriteRenderer>().sprite;
                if (i == 1) source.transform.rotation = Quaternion.Euler(0, 0, 90);
                var axis = i == 0 ? Vector3.right : Vector3.up;
                RocketSplitEffect.Play(source, i == 0, source.transform.position - axis * 2f,
                    source.transform.position + axis * 2f, new Vector3[0], _ => { });
                Object.Destroy(source, 1f);
            }
            cameraRoot = new GameObject("RocketPreviewCamera");
            var previewCamera = cameraRoot.AddComponent<Camera>();
            previewCamera.enabled = false;
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = 2.2f;
            previewCamera.transform.position = Origin + Vector3.back * 20f;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.08f, 0.08f, 0.08f);
            captureAt = Time.time + 0.14d;
        }
    }
}
