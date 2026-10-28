using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    /// <summary>Preserve user PNG/sprite identities while importing their UI art without lossy compression.</summary>
    public sealed class RoyalPostmanUserSpriteImportPolicy : AssetPostprocessor
    {
        public const string Root="Assets/RoyalAves/Art/UI/UI assets/";
        public override int GetPostprocessOrder()=>1100;
        public override uint GetVersion()=>1;
        public static bool Owns(string path)=>path.StartsWith(Root,System.StringComparison.Ordinal);
        public static void Configure(TextureImporter importer)
        {
            // In particular, do not change textureType/spriteMode: existing Image references must survive.
            importer.alphaIsTransparency=true;importer.crunchedCompression=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=8192;
            importer.mipmapEnabled=true;importer.mipmapFilter=TextureImporterMipFilter.BoxFilter;
            importer.filterMode=FilterMode.Trilinear;importer.wrapMode=TextureWrapMode.Clamp;
            var defaults=importer.GetDefaultPlatformTextureSettings();
            defaults.maxTextureSize=8192;defaults.textureCompression=TextureImporterCompression.Uncompressed;
            defaults.crunchedCompression=false;importer.SetPlatformTextureSettings(defaults);
            foreach(var platform in new[]{"Standalone","Android","iPhone","WebGL","VisionOS","tvOS"})
                importer.ClearPlatformTextureSettings(platform);
        }
        void OnPreprocessTexture(){if(Owns(assetPath))Configure((TextureImporter)assetImporter);}
    }
}

