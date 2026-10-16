using System;
using System.IO;
using RoyalAves.Meta;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    // Native rendering fixture: uses the game's sprites, material and board-cell drawing.
    public static class IceVisualValidation
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            const string request = "Logs/ice-visual.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            bool final = File.ReadAllText(request).Trim() == "final";
            File.Delete(request);
            try { Render(!final); }
            catch (Exception error) { File.WriteAllText("Logs/ice-visual.result", error.ToString()); }
        }

        [MenuItem("Royal Aves/Elementos/Gelo/Comparar legibilidade sobre as peças")]
        public static void Capture()
            => Render(true);

        static void Render(bool comparison)
        {
            var root = new GameObject("IceReadabilityFixture") { hideFlags = HideFlags.HideAndDontSave };
            root.transform.position = new Vector3(10000f, 10000f, 0f);
            var previous = RenderTexture.active;
            int width = comparison ? 1200 : 720;
            var target = new RenderTexture(width, 1440, 24);
            var pixels = new Texture2D(width, 1440, TextureFormat.RGBA32, false);
            try
            {
                var camera = new GameObject("FixtureCamera").AddComponent<Camera>();
                camera.transform.SetParent(root.transform, false);
                camera.transform.localPosition = new Vector3(comparison ? 4f : 2f, -5f, -20f);
                camera.enabled = false;
                camera.orthographic = true;
                camera.orthographicSize = 6.3f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(178, 207, 245, 255);
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 30f;
                var thin = AssetDatabase.LoadAssetAtPath<Sprite>(IceTools.ThinArtPath);
                var thick = AssetDatabase.LoadAssetAtPath<Sprite>(IceTools.ThickArtPath);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(IceTools.PrefabPath);
                var layers = prefab.GetComponent<SweetSugar.Scripts.Blocks.LayeredBlock>().layers;
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("a97c105638bdf8b4a8650670310a4cd3"));
                var names = new[] { "envelope", "stamp", "gift", "ink", "quill", "bow" };
                for (int row = 0; row < names.Length; row++)
                for (int col = 0; col < (comparison ? 5 : 3); col++)
                {
                    var position = new Vector3(col * 2f, -row * 2f, 0f);
                    var cell = Make(root, position, null, 0, material);
                    BoardCellVisual.Apply(cell);
                    var item = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/Pieces/" + names[row] + ".png");
                    var piece = Make(root, position, item, 1, material);
                    piece.transform.localScale = Vector3.one * (1.4f / Mathf.Max(item.bounds.size.x, item.bounds.size.y));
                    if (col == 0) continue;
                    var layerRenderer = layers[(col == 1 || col == 3) ? 0 : 1].GetComponent<SpriteRenderer>();
                    var ice = Make(root, position, comparison ? (col == 1 || col == 3 ? thin : thick) : layerRenderer.sprite, 2, material);
                    ice.color = comparison ? new Color(1f, 1f, 1f, col == 1 ? .55f : col == 2 ? 1f : col == 3 ? .35f : .60f) : layerRenderer.color;
                }
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, width, 1440), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("Logs");
                File.WriteAllBytes(comparison ? "Logs/ice-readability-comparison.png" : "Logs/ice-readability-final.png", pixels.EncodeToPNG());
                File.WriteAllText("Logs/ice-visual.result", comparison ? "OK: Unity render. Columns: bare piece; thin .55; thick 1; thin .35; thick .60." :
                    $"OK: native render using saved prefab. Columns: bare piece; thin alpha={layers[0].GetComponent<SpriteRenderer>().color.a}; thick alpha={layers[1].GetComponent<SpriteRenderer>().color.a}. Six piece sprites.");
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(pixels);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        static SpriteRenderer Make(GameObject root, Vector3 position, Sprite sprite, int order, Material material)
        {
            var node = new GameObject("FixtureSprite");
            node.transform.SetParent(root.transform, false);
            node.transform.localPosition = position;
            var renderer = node.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
