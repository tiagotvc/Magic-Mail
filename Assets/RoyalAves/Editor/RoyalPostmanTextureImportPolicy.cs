using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public sealed class RoyalPostmanTextureImportPolicy : AssetPostprocessor
    {
        public override int GetPostprocessOrder()=>1000;
        public override uint GetVersion()=>3;
        public static bool Owns(string path)=>path.StartsWith("Assets/RoyalAves/Art/RoyalPostmanUI2026/")||path.StartsWith("Assets/RoyalAves/Art/UI/")&&path.Contains("/RoyalPostman/");
        void OnPreprocessTexture()
        {
            if(!Owns(assetPath))return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;importer.alphaIsTransparency=true;
            // Average minification for large Pets sprites while preserving native pixels at zoom.
            // Other UI imports stay unchanged.
            bool pets=assetPath.Contains("/RoyalPostman/Pets/")||assetPath.Contains("/RoyalPostman/IncubatorControlsV70/")||assetPath.Contains("/RoyalPostman/HeaderV86/")||assetPath.Contains("/RoyalPostman/HatcheryV87/")||assetPath.Contains("/RoyalPostman/AcceleratorBadgeV98/")||assetPath.Contains("/RoyalPostman/BlueEnamelV103/");
            bool settingsArt=assetPath.Contains("/RoyalPostman/SettingsV161/");
            bool menuIcon=assetPath.EndsWith("/nav-ranking-v22.png")||assetPath.EndsWith("/navigation-teams-individual-v3.png")||assetPath.EndsWith("/navigation-album-individual-v3.png")||assetPath.EndsWith("/navigation-clipboard-individual-v3.png")||assetPath.EndsWith("/courier-portrait.png")||assetPath.EndsWith("/booster-bomb.png")||assetPath.EndsWith("/coin-envelope.png")||assetPath.EndsWith("/gear-cream.png");
            importer.mipmapEnabled=pets||settingsArt||menuIcon;importer.mipmapFilter=TextureImporterMipFilter.BoxFilter;importer.npotScale=TextureImporterNPOTScale.None;
            importer.maxTextureSize=8192;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.filterMode=pets||settingsArt||menuIcon?FilterMode.Trilinear:FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
            var settings=importer.GetDefaultPlatformTextureSettings();
            settings.maxTextureSize=8192;settings.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(settings);
            // Old Android overrides used ASTC even when the default was uncompressed.
            // Let these UI sources inherit the same lossless import on each target.
            foreach(var platform in new[]{"Standalone","Android","iPhone","WebGL"})
                importer.ClearPlatformTextureSettings(platform);
        }
    }
}
