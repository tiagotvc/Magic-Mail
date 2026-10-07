// Fills the paper plane (MARMALADE item, now a propeller ball)'s two MarmaladeFly components with the blurred frames
// of Art/Specials/propeller-spin-sheet.png (a 4x3 grid), so each duplicate blurs its propeller while it flies. Only
// row 1 (frames 4-7) is used: it's the one with real motion blur. Rows 0 and 2 are sharp, posed angles. The resting
// icon (Art/Specials/plane.png) is swapped by overwriting the file directly, so it needs no prefab change here.
// Menu: Royal Aves > Aplicar rotação no avião de papel.
using System.Linq;
using SweetSugar.Scripts.Items;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesPropellerSpinBuilder
    {
        public const string SheetPath = "Assets/RoyalAves/Art/Specials/propeller-spin-sheet.png";
        const string MarmaladePrefabPath = "Assets/SweetSugar/Resources/Items/MARMALADE.prefab";
        const int Columns = 4;
        const int Rows = 3;
        // Row 1 of the 4x3 grid (indices 4-7): the only row with motion blur.
        const int BlurredRowFirst = 4;
        const int BlurredRowCount = 4;

        [MenuItem("Royal Aves/Aplicar rotação no avião de papel")]
        public static void Apply()
        {
            var allFrames = SliceSheet();
            var spinFrames = allFrames.Skip(BlurredRowFirst).Take(BlurredRowCount).ToArray();
            UpdatePrefab(spinFrames);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Rotação aplicada ao avião de papel: {spinFrames.Length} frames com desfoque, nas duas cópias.");
        }

        static Sprite[] SliceSheet()
        {
            AssetDatabase.ImportAsset(SheetPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SheetPath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
            var cellWidth = texture.width / Columns;
            var cellHeight = texture.height / Rows;

            // The sheet is Multiple by the importer (RoyalAvesAssetImporter), so only the sprite rects are set here.
            var rects = new SpriteRect[Columns * Rows];
            for (var i = 0; i < rects.Length; i++)
            {
                var col = i % Columns;
                var row = i / Columns;
                rects[i] = new SpriteRect
                {
                    name = $"propeller_spin_{i:00}",
                    rect = new Rect(col * cellWidth, texture.height - (row + 1) * cellHeight, cellWidth, cellHeight),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = SpriteAlignment.Center,
                    spriteID = GUID.Generate()
                };
            }

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();
            dataProvider.SetSpriteRects(rects);
            dataProvider.Apply();
            importer.SaveAndReimport();

            return AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        }

        static void UpdatePrefab(Sprite[] frames)
        {
            var root = PrefabUtility.LoadPrefabContents(MarmaladePrefabPath);
            try
            {
                var flies = root.GetComponentsInChildren<MarmaladeFly>(true);
                if (flies.Length == 0)
                    throw new System.InvalidOperationException($"MarmaladeFly não encontrado em {MarmaladePrefabPath}");
                foreach (var fly in flies)
                    fly.SpinFrames = frames;
                PrefabUtility.SaveAsPrefabAsset(root, MarmaladePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
