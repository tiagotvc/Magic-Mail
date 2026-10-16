using System;
using System.IO;
using RoyalAves.Effects;
using RoyalAves.Gameplay;
using RoyalAves.Meta;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class IceBreakValidation
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            const string request = "Logs/ice-break.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request);
            try { Capture(); }
            catch (Exception error) { File.WriteAllText("Logs/ice-break.result", error.ToString()); Debug.LogException(error); }
        }

        [MenuItem("Royal Aves/Elementos/Gelo/Validar e capturar quebra dos dois nÃƒÂ­veis")]
        public static void Capture()
        {
            var root = new GameObject("IceBreakValidation") { hideFlags = HideFlags.HideAndDontSave };
            root.transform.position = new Vector3(12000f, 12000f, 0f);
            var target = new RenderTexture(2400, 1000, 24);
            var pixels = new Texture2D(2400, 1000, TextureFormat.RGBA32, false);
            var previous = RenderTexture.active;
            try
            {
                var camera = new GameObject("IceBreakCamera").AddComponent<Camera>();
                camera.transform.SetParent(root.transform, false);
                camera.transform.localPosition = new Vector3(8.5f, -1.8f, -20f);
                camera.enabled = false;
                camera.orthographic = true;
                camera.orthographicSize = 4f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color32(178, 207, 245, 255);
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 30f;
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("a97c105638bdf8b4a8650670310a4cd3"));
                var thin = AssetDatabase.LoadAssetAtPath<Sprite>(IceTools.ThinArtPath);
                var thick = AssetDatabase.LoadAssetAtPath<Sprite>(IceTools.ThickArtPath);
                var piece = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/Pieces/stamp.png");
                var times = new[] { -1f, .04f, .12f, .24f, .42f, .68f };
                int checkedEffects = 0;
                for (int row = 0; row < 2; row++)
                for (int col = 0; col < times.Length; col++)
                {
                    bool isThick = row == 0;
                    var position = new Vector3(col * 3.4f, -row * 3.6f, 0f);
                    BoardCellVisual.Apply(Make(root, position, null, 0, material));
                    var item = Make(root, position, piece, 1, material);
                    item.transform.localScale = Vector3.one * (1.4f / Mathf.Max(piece.bounds.size.x, piece.bounds.size.y));
                    if (isThick && times[col] >= 0f)
                    {
                        var revealed = Make(root, position, thin, 2, material);
                        revealed.color = new Color(1f, 1f, 1f, IceLayer.OpacityForLevel(1));
                    }
                    var source = Make(root, position, isThick ? thick : thin, 3, material);
                    source.color = new Color(1f, 1f, 1f, IceLayer.OpacityForLevel(isThick ? 2 : 1));
                    if (times[col] < 0f) continue;
                    var effect = IceBreakEffect.Play(source, isThick, false);
                    source.enabled = false;
                    effect.Sample(times[col]);
                    if (effect.ShardCount != (isThick ? 5 : 7)) throw new Exception("Unexpected ice shard count.");
                    var mesh = effect.GetComponent<MeshFilter>().sharedMesh;
                    var vertices = mesh.vertices;
                    foreach (var vertex in vertices)
                        if (float.IsNaN(vertex.x) || float.IsNaN(vertex.y)) throw new Exception("Invalid shard geometry.");
                    if (times[col] >= effect.Duration)
                        foreach (var color in mesh.colors)
                            if (color.a > .001f) throw new Exception("Ice debris must fully fade at completion.");
                    checkedEffects++;
                }
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 2400, 1000), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("Docs");
                File.WriteAllBytes("Docs/Gelo_Quebra_Unity.png", pixels.EncodeToPNG());
                File.WriteAllText("Logs/ice-break.result", $"PASS: {checkedEffects} native effect samples; 5 thick / 7 thin organic shards; finite geometry; fade at completion. Top: thick -> thin. Bottom: thin -> clear. Columns: before, .04, .12, .24, .42, .68 seconds.");
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
            var node = new GameObject("IceBreakFixtureSprite");
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
