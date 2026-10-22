using System.IO;
using RoyalAves.Effects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoyalAves.EditorTools
{
    public static class RouteCentralPrototypeTools
    {
        const string Art = "Assets/RoyalAves/Art/Obstacles/RouteCentral/CM_central_pocoes_prototipo.png";
        const string ScenePath = "Assets/RoyalAves/Laboratory/RouteCentralVisual.unity";

        [InitializeOnLoadMethod]
        static void QueueBuild()
        {
            EditorApplication.delayCall += EnsureAssets;
        }

        static void EnsureAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || AssetDatabase.IsAssetImportWorkerProcess()) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += EnsureAssets;
                return;
            }
            if (!File.Exists(ScenePath)) Build();
        }

        [MenuItem("Royal Aves/Elementos/Central de Rotas/Testar visual e tinta")]
        public static void Preview()
        {
            if (EditorApplication.isPlaying) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Royal Aves/Elementos/Central de Rotas/Gerar prefab visual")]
        public static void Build()
        {
            AssetDatabase.ImportAsset(Art);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Art);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            importer.spritePixelsPerUnit = width / RouteCentralPrototype.CanvasSize;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var root = new GameObject("CentralDeRotas_VisualPrototype");
                var prototype = root.AddComponent<RouteCentralPrototype>();
                prototype.artwork = AssetDatabase.LoadAssetAtPath<Sprite>(Art);
                prototype.standArtwork = ImportLayer("CM_central_estante.png");
                prototype.flaskArtwork = ImportLayer("CM_central_frascos.png");
                prototype.breakSheet = ImportLayer("CM_frasco_quebra_sheet.png").texture;
                var preview = root.AddComponent<SpriteRenderer>();
                preview.sprite = prototype.artwork;
                preview.sortingLayerName = PowerUpEffectOrder.SortingLayer;
                preview.sortingOrder = 10;
                Directory.CreateDirectory("Assets/RoyalAves/Prefabs/Elements");
                PrefabUtility.SaveAsPrefabAsset(root, "Assets/RoyalAves/Prefabs/Elements/RouteCentralVisual.prefab");
                prototype.interactivePreview = true;
                // The master sprite is four units wide; the obstacle occupies two cells.
                root.transform.localScale = Vector3.one * .5f;
                BuildBoard();
                var camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.transform.position = new Vector3(0, 0, -10);
                camera.orthographic = true;
                camera.orthographicSize = 5.4f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.13f, .12f, .2f);
                Directory.CreateDirectory("Assets/RoyalAves/Laboratory");
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("Central de Rotas: prefab e tela visual gerados. No teste, clique em cada poção para quebrar e escorrer a tinta.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            }
        }

        static void BuildBoard()
        {
            var board = new GameObject("PreviewBoard_8x8");
            string[] paths = { "Assets/RoyalAves/Art/Pieces/gift.png", "Assets/RoyalAves/Art/Pieces/envelope.png" };
            var icons = new[] { AssetDatabase.LoadAssetAtPath<Sprite>(paths[0]), AssetDatabase.LoadAssetAtPath<Sprite>(paths[1]) };
            var tileSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                {
                    var node = new GameObject("Cell_" + row + "_" + col);
                    node.transform.SetParent(board.transform, false);
                    node.transform.localPosition = new Vector3(col - 3.5f, row - 3.5f, 1);
                    var tile = node.AddComponent<SpriteRenderer>();
                    tile.sprite = tileSprite;
                    tile.drawMode = SpriteDrawMode.Sliced;
                    tile.size = Vector2.one * .94f;
                    tile.color = (row + col) % 2 == 0 ? new Color(.3f, .34f, .49f) : new Color(.34f, .39f, .54f);
                    if (row >= 3 && row <= 4 && col >= 3 && col <= 4) continue;
                    var sprite = icons[(row + col) % 2];
                    if (sprite == null) continue;
                    var piece = new GameObject("Piece");
                    piece.transform.SetParent(node.transform, false);
                    piece.transform.localPosition = new Vector3(0, 0, -.1f);
                    piece.transform.localScale = Vector3.one * (.72f / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y));
                    var renderer = piece.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.sortingOrder = 1;
                }
        }

        static Sprite ImportLayer(string name)
        {
            string path = "Assets/RoyalAves/Resources/RouteCentral/" + name;
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new System.InvalidOperationException("Camada da central ausente: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            importer.spritePixelsPerUnit = width / RouteCentralPrototype.CanvasSize;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
