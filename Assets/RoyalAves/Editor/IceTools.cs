using System;
using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class IceTools
    {
        public const string PrefabPath = "Assets/SweetSugar/Resources/Blocks/IceBlock.prefab";
        public const string ThinArtPath = "Assets/RoyalAves/Art/Obstacles/ice-thin-v4.png";
        public const string ThickArtPath = "Assets/RoyalAves/Art/Obstacles/ice-thick-v4.png";

        [InitializeOnLoadMethod]
        static void Initialize() => EditorApplication.delayCall += PrepareIfMissing;

        static void PrepareIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(PrefabPath)) return;
            Build();
        }

        [MenuItem("Royal Aves/Elementos/Gelo/Preparar gelo de dois níveis")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Prepare o gelo fora do Play Mode.");
            var thin = ImportArt(ThinArtPath);
            var thick = ImportArt(ThickArtPath);
            var root = new GameObject("IceBlock");
            try
            {
                root.AddComponent<BindLayer>().order = 5;
                var layers = root.AddComponent<LayeredBlock>();
                layers.layers = new[] { CreateLayer(root, thin, 1), CreateLayer(root, thick, 2) };
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                RegisterTarget(prefab, thin, thick);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/ice-prepare.result", "OK: IceBlock preparado; dois níveis, peça presa e meta por célula liberada.");
        }

        static Sprite ImportArt(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            importer.spritePixelsPerUnit = texture.width * .45f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Square CreateLayer(GameObject root, Sprite sprite, int level)
        {
            var node = new GameObject(level == 1 ? "Gelo fino" : "Gelo grosso");
            node.transform.SetParent(root.transform, false);
            var renderer = node.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            // Thin ice is a complete milky veil over the item, rather than an empty frame.
            renderer.color = new Color(1f, 1f, 1f, IceLayer.OpacityForLevel(level));
            renderer.sortingOrder = level + 3;
            var square = node.AddComponent<Square>();
            square.type = SquareTypes.IceBlock;
            square.canMoveIn = true;
            square.canMoveOut = false;
            square.aboveItem = true;
            square.sizeInSquares = Vector2Int.one;
            square.score = 100;
            node.AddComponent<BindLayer>().order = 5;
            node.AddComponent<IceLayer>().remainingLayers = level;
            return square;
        }

        static void RegisterTarget(GameObject prefab, Sprite thin, Sprite thick)
        {
            var config = Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");
            var target = config.targets.FirstOrDefault(t => t.name == "IceBlock");
            if (target == null)
            {
                // Append to preserve every saved target index.
                target = new TargetContainer { name = "IceBlock", localization =
                    JsonUtility.FromJson<LocalizationIndexFolder>(JsonUtility.ToJson(config.targets.First(t => t.name == "WireBlock").localization)) };
                config.targets.Add(target);
            }
            target.collectAction = CollectingTypes.Destroy;
            target.localization.description.index = -1;
            target.localization.description.text = "Quebre o gelo para liberar as peças";
            target.localization.failed.index = -1;
            target.localization.failed.text = "Ainda há peças congeladas";
            target.setCount = SetCount.FromLevel;
            target.prefabs = new System.Collections.Generic.List<GameObject> { prefab };
            var list = new SpriteList();
            list.Add(new SpriteObject { icon = thin });
            list.Add(new SpriteObject { icon = thick, uiSprite = true });
            target.defaultSprites = new SprArrays();
            target.defaultSprites.Add(new SprArray { sprites = list });
            EditorUtility.SetDirty(config);
        }
    }
}
