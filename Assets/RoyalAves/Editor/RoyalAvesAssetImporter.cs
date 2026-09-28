// Import settings for the exported Royal Aves assets: everything under Assets/RoyalAves/Art becomes a UI-ready
// sprite, music streams from disk and short sound effects are decompressed on load.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public class RoyalAvesAssetImporter : AssetPostprocessor
    {
        // Board art is 256 px; at 128 px per unit it fills the same 2-unit cell as the Sweet Sugar art (200 px at 100).
        static readonly string[] BoardArtFolders = { "Assets/RoyalAves/Art/Pieces/", "Assets/RoyalAves/Art/Specials/", "Assets/RoyalAves/Art/Obstacles/" };

        // Rounded panels and buttons drawn after the web prototype's CSS; the 9-slice border keeps their corners when
        // stretched. Border order: left, bottom, right, top (buttons have a drop shadow at the bottom).
        const string GeneratedFolder = "Assets/RoyalAves/Art/UI/Generated/";
        static readonly Dictionary<string, Vector4> SliceBorders = new Dictionary<string, Vector4>
        {
            ["pill"] = new Vector4(34, 34, 34, 34),
            ["panel"] = new Vector4(60, 60, 60, 60),
            ["btn-play"] = new Vector4(50, 58, 50, 50),
            ["btn-area"] = new Vector4(50, 58, 50, 50), // btn-play recoloured yellow, for the lobby's area button
            ["progress-blue"] = new Vector4(27, 27, 27, 27),
            ["btn-green"] = new Vector4(34, 42, 34, 34),
            ["btn-red"] = new Vector4(34, 42, 34, 34),
            ["btn-blue"] = new Vector4(36, 44, 36, 36),
            ["darkbox"] = new Vector4(20, 20, 20, 20),
            ["card"] = new Vector4(32, 32, 32, 32),
            ["meter"] = new Vector4(40, 40, 40, 40),
            ["bar-bg"] = new Vector4(16, 16, 16, 16),
            ["bar-fill"] = new Vector4(16, 16, 16, 16),
            ["nav"] = new Vector4(4, 4, 4, 20),
            ["nav-active"] = new Vector4(8, 4, 8, 4),
            ["hud-frame"] = new Vector4(40, 48, 40, 40),
            ["victory-card"] = new Vector4(66, 66, 66, 66),
            ["banner"] = new Vector4(48, 56, 48, 48),
        };

        // Frames of the level start window that stretch in height only: the plain middle (inside the red body, between
        // the beige goal and booster slots) grows or shrinks; left and right are 0 because the width scales as a whole.
        public const string LevelStartFolder = "Assets/RoyalAves/Art/UI/LevelStart/";
        public static readonly Dictionary<string, Vector4> LevelStartBorders = new Dictionary<string, Vector4>
        {
            ["moldura-metas-caixa-correio"] = new Vector4(0, 190, 0, 340),
            ["moldura-bege"] = new Vector4(0, 361, 0, 490),
        };

        // Game screen frames that are stretched to fit: the board frame grows with the grid (the levels use 17 different
        // grid shapes, from 5x5 to 11x11) and the booster bar spans the screen width, so both keep their corners by
        // 9-slicing. Border order: left, bottom, right, top.
        public const string GameFolder = "Assets/RoyalAves/Art/UI/Game/";
        public static readonly Dictionary<string, Vector4> GameBorders = new Dictionary<string, Vector4>
        {
            ["moldura-tabuleiro"] = new Vector4(240, 240, 240, 240), // the brown corners end at 225 px
            ["barra-reforcos"] = new Vector4(16, 0, 16, 0),          // only the stitched middle repeats
        };

        public const string ManagerFolder = "Assets/RoyalAves/Art/Characters/Gerente/";
        public const int ManagerMaxSize = 1024;

        // The board frame is a world sprite next to 1.2-unit cells: at 200 px per unit its band is about a third of a cell.
        public const string BoardFrameAsset = GameFolder + "moldura-tabuleiro.png";

        // The frame pieces (corners and straight bands) were all normalised to a 256 px band, so at 256 px per unit the
        // band is exactly one unit: BoardFrame then lays the frame out in "band units" and scales it to the thickness
        // the user picks. The straight bands are tiled, which needs the whole rectangle.
        public const string FramePiecesFolder = GameFolder + "Moldura/";
        public const float FrameBand = 256f;

        public static float PixelsPerUnit(string path) =>
            path.StartsWith(FramePiecesFolder) ? FrameBand :
            path == BoardFrameAsset ? 200 : BoardArtFolders.Any(path.StartsWith) ? 128 : 100;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/RoyalAves/Art/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit(assetPath);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            if (assetPath.StartsWith(ManagerFolder)) importer.maxTextureSize = ManagerMaxSize; // 16 expressions of 1254 px
            if (assetPath.StartsWith(LevelStartFolder) &&
                LevelStartBorders.TryGetValue(Path.GetFileNameWithoutExtension(assetPath), out var frameBorder))
                importer.spriteBorder = frameBorder;
            if (assetPath.StartsWith(FramePiecesFolder))
            {
                var pieces = new TextureImporterSettings();
                importer.ReadTextureSettings(pieces);
                pieces.spriteMeshType = SpriteMeshType.FullRect;   // tiling needs the whole rectangle
                importer.SetTextureSettings(pieces);
                importer.maxTextureSize = 2048;
            }
            if (assetPath.StartsWith(GameFolder) &&
                GameBorders.TryGetValue(Path.GetFileNameWithoutExtension(assetPath), out var gameBorder))
            {
                importer.spriteBorder = gameBorder;
                // Sliced and tiled drawing needs the whole rectangle, not the tight mesh Unity builds by default.
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
            }
            if (!assetPath.StartsWith(GeneratedFolder)) return;
            // Small gradients band when compressed.
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            if (SliceBorders.TryGetValue(Path.GetFileNameWithoutExtension(assetPath), out var border))
                importer.spriteBorder = border;
        }

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/RoyalAves/Audio/")) return;
            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            settings.loadType = assetPath.Contains("/Music/") ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = settings;
        }
    }
}
