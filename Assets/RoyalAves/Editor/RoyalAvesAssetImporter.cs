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
            ["moldura-metas-azul"] = new Vector4(0, 180, 0, 400), // stars window: band with the wax seal on top
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

        // Letter/digit/accent sheets for the gold sprite font (Royal Aves > Criar fonte de diálogo): these are
        // hand-sliced into one sprite per character (Multiple mode) and must not be forced back to Single.
        public const string DialogFolder = "Assets/RoyalAves/Art/UI/Dialog/";

        public const string ManagerFolder = "Assets/RoyalAves/Art/Characters/Gerente/";
        public const int ManagerMaxSize = 1024;

        // The board frame is a world sprite next to 1.2-unit cells: at 200 px per unit its band is about a third of a cell.
        public const string BoardFrameAsset = GameFolder + "moldura-tabuleiro.png";

        // The frame pieces (corners and straight bands) were all normalised to a 256 px band, so at 256 px per unit the
        // band is exactly one unit: BoardFrame then lays the frame out in "band units" and scales it to the thickness
        // the user picks. The straight bands are tiled, which needs the whole rectangle.
        public const string FramePiecesFolder = GameFolder + "Moldura/";
        public const float FrameBand = 256f;

        // The four board icons refreshed on 2026-10-09 render at 95% of their original size.
        static readonly HashSet<string> CompactPieceAssets = new HashSet<string>
        {
            "Assets/RoyalAves/Art/Pieces/envelope.png",
            "Assets/RoyalAves/Art/Pieces/gift.png",
            "Assets/RoyalAves/Art/Pieces/key.png",
            "Assets/RoyalAves/Art/Pieces/stamp.png",
        };
        const float RefreshedPieceScale = 0.95f;

        // Keep dynamite clear of cell borders, with the same scale in idle and explosion frames.
        const float DynamiteScale = 0.90f;

        public const string ParcelStackFolder = "Assets/RoyalAves/Art/Obstacles/ParcelStack/";
        public const string GrassFloorAsset = "Assets/RoyalAves/Art/Obstacles/grass.png";
        public const string GrassSparseAsset = "Assets/RoyalAves/Art/Obstacles/grass-sparse.png";
        public const float FloorCanvasUnits = 1.95f;
        // Rounded leaf edges overlap adjacent cells to cover the board's grid strokes.
        public const float GrassEdgeOverlap = 1.08f;
        // The board cell is 0.82 units and its square scales children by 0.42.
        // A 1.9-unit canvas therefore stays inside the cell even at full opaque coverage.
        public const float ParcelCanvasUnits = 1.9f;
        public const float ParcelBodyUnits = 1.72f;

        public static void GetParcelFit(string path, out Vector2 scale, out Vector2 offset)
            => GetCellSpriteFit(path, ParcelBodyUnits, ParcelCanvasUnits, out scale, out offset);

        public static void GetGrassFit(string path, out Vector2 scale, out Vector2 offset)
            => GetCellSpriteFit(path, FloorCanvasUnits * GrassEdgeOverlap, FloorCanvasUnits, out scale, out offset);

        static void GetCellSpriteFit(string path, float bodyUnits, float canvasUnits, out Vector2 scale, out Vector2 offset)
        {
            var source = new Texture2D(2, 2);
            try
            {
                if (!source.LoadImage(File.ReadAllBytes(path))) throw new System.InvalidOperationException("PNG inválido: " + path);
                int left = source.width, right = -1, bottom = source.height, top = -1;
                var pixels = source.GetPixels32();
                for (int y = 0; y < source.height; y++)
                    for (int x = 0; x < source.width; x++)
                        if (pixels[y * source.width + x].a >= 128)
                        {
                            left = Mathf.Min(left, x); right = Mathf.Max(right, x);
                            bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
                        }
                if (right < left || top < bottom) throw new System.InvalidOperationException("Sprite vazio: " + path);
                float ppu = CanvasPixelsPerUnit(path, canvasUnits);
                scale = new Vector2(bodyUnits * ppu / (right - left + 1), bodyUnits * ppu / (top - bottom + 1));
                var center = new Vector2((left + right + 1f - source.width) / (2f * ppu), (bottom + top + 1f - source.height) / (2f * ppu));
                offset = -Vector2.Scale(center, scale);
            }
            finally { Object.DestroyImmediate(source); }
        }

        static float ParcelPixelsPerUnit(string path)
            => CanvasPixelsPerUnit(path, ParcelCanvasUnits);

        static float CanvasPixelsPerUnit(string path, float canvasUnits)
        {
            if (!File.Exists(path)) return 128f;
            var header = new byte[24];
            using (var stream = File.OpenRead(path))
                if (stream.Read(header, 0, header.Length) != header.Length) return 128f;
            int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
            int height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
            return Mathf.Max(width, height) / canvasUnits;
        }

        public static void ConfigureParcelStack(TextureImporter importer)
            => ConfigureCellSprite(importer, ParcelPixelsPerUnit(importer.assetPath));

        public static void ConfigureGrassFloor(TextureImporter importer)
            => ConfigureCellSprite(importer, CanvasPixelsPerUnit(importer.assetPath, FloorCanvasUnits));

        static void ConfigureCellSprite(TextureImporter importer, float pixelsPerUnit)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(0.5f, 0.5f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }

        public static float PixelsPerUnit(string path) =>
            path.StartsWith("Assets/RoyalAves/Art/Obstacles/FrankingMachine/") ? 660f :
            path.StartsWith("Assets/RoyalAves/Art/Obstacles/MailCubby/") ? (Path.GetFileName(path).StartsWith("CM_escaninho_carta") ? 2050f : 660f) :
            path == GrassFloorAsset || path == GrassSparseAsset ? CanvasPixelsPerUnit(path, FloorCanvasUnits) :
            path.StartsWith(ParcelStackFolder) ? ParcelPixelsPerUnit(path) :
            (path == RoyalAvesDinamiteBuilder.IdlePath || path == RoyalAvesDinamiteBuilder.SheetPath)
                ? 128f / DynamiteScale :
            CompactPieceAssets.Contains(path) ? CanvasPixelsPerUnit(path, 2f * RefreshedPieceScale) :
            path.StartsWith(FramePiecesFolder) ? FrameBand :
            path == BoardFrameAsset ? 200 : BoardArtFolders.Any(path.StartsWith) ? 128 : 100;

        void OnPreprocessTexture()
        {
            if (assetPath.StartsWith("Assets/RoyalAves/Resources/UI/Hud/"))
            {
                var hudImporter = (TextureImporter)assetImporter;
                hudImporter.textureType = TextureImporterType.Sprite;
                hudImporter.spriteImportMode = SpriteImportMode.Single;
                hudImporter.maxTextureSize = 4096;
                hudImporter.alphaIsTransparency = true;
                hudImporter.mipmapEnabled = false;
                hudImporter.npotScale = TextureImporterNPOTScale.None;
                foreach (var platform in new[] { "Standalone", "Android", "iPhone" })
                {
                    var settings = hudImporter.GetPlatformTextureSettings(platform);
                    settings.name = platform;
                    settings.overridden = true;
                    settings.maxTextureSize = 4096;
                    hudImporter.SetPlatformTextureSettings(settings);
                }
                return;
            }
            if (!assetPath.StartsWith("Assets/RoyalAves/Art/")) return;
            // Snow modal pieces use explicit sprite rectangles to remove transparent export margins.
            // Their builder owns these settings, including the four separate snow corners.
            if (assetPath.StartsWith("Assets/RoyalAves/Art/modal/")) return;
            if (assetPath.StartsWith(DialogFolder)) return; // hand-sliced per-character sheets, left alone
            // Map sheets/backgrounds own their import contract. Forcing Single here
            // erased their sub-sprites on every reimport and displayed whole atlases.
            if (assetPath.StartsWith("Assets/RoyalAves/Art/MapKitV2/") ||
                assetPath.StartsWith("Assets/RoyalAves/Art/MapKitV3/")) return;
            var importer = (TextureImporter)assetImporter;
            if (assetPath == IceTools.ThinArtPath || assetPath == IceTools.ThickArtPath)
            {
                ConfigureCellSprite(importer, CanvasPixelsPerUnit(assetPath,
                    1f / .45f));
                return;
            }
            if (assetPath == "Assets/RoyalAves/Art/Obstacles/ice-two-levels-v2.png")
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 380f;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                return;
            }
            if (assetPath.StartsWith("Assets/RoyalAves/Art/Obstacles/MailCubby/"))
            {
                ConfigureCellSprite(importer, PixelsPerUnit(assetPath));
                importer.maxTextureSize = 4096;
                foreach (var platform in new[] { "Standalone", "Android", "iPhone" })
                {
                    var settings = importer.GetPlatformTextureSettings(platform);
                    settings.name = platform;
                    settings.overridden = true;
                    settings.maxTextureSize = 4096;
                    settings.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SetPlatformTextureSettings(settings);
                }
                return;
            }
            if (assetPath == GrassFloorAsset || assetPath == GrassSparseAsset)
            {
                ConfigureGrassFloor(importer);
                return;
            }
            if (assetPath.StartsWith(ParcelStackFolder))
            {
                ConfigureParcelStack(importer);
                return;
            }
            importer.textureType = TextureImporterType.Sprite;
            // The dinamite sheet and the colour bomb's and propeller's spin sheets are grids of frames (sliced by their
            // builders), so they must stay Multiple.
            var multiSheets = assetPath == RoyalAvesDinamiteBuilder.SheetPath
                || assetPath == RoyalAvesColorBombSpinBuilder.SheetPath
                || assetPath == RoyalAvesPropellerSpinBuilder.SheetPath;
            importer.spriteImportMode = multiSheets ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit(assetPath);
            if (assetPath == RoyalAvesDinamiteBuilder.IdlePath)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = RoyalAvesDinamiteBuilder.SpritePivot;
                importer.SetTextureSettings(settings);
            }
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            if (assetPath.StartsWith("Assets/RoyalAves/Art/Pieces/"))
            {
                // Preserve smooth color and alpha gradients, including on mobile builds.
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                foreach (var platform in new[] { "Standalone", "Android", "iPhone" })
                {
                    var pieceSettings = importer.GetPlatformTextureSettings(platform);
                    pieceSettings.name = platform;
                    pieceSettings.overridden = true;
                    pieceSettings.maxTextureSize = 2048;
                    pieceSettings.format = TextureImporterFormat.RGBA32;
                    pieceSettings.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SetPlatformTextureSettings(pieceSettings);
                }
            }
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
