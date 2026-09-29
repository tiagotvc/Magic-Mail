// TextMeshPro fonts of Correio Mágico: Lilita One for titles and buttons, and the user's CorreioMagico font for the
// "postal" texts of the prototype (counters, level button, area labels), drawn as in the web version: cream-to-orange
// letters with a dark blue outline. The TTF is a colour font (COLRv1) that TextMeshPro draws as a plain shape, so the
// gradient and outline come from TextMeshPro (royal-aves/unity-export/LEIA-ME.md).
using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace RoyalAves.EditorTools
{
    static class RoyalAvesFonts
    {
        const string Folder = "Assets/RoyalAves/Fonts/";
        const string FallbackPath = "Assets/SweetSugar/Fonts/VOGUE BOLD-1 SDF.asset";
        static readonly Color PostalTop = new Color32(0xFB, 0xFC, 0xE8, 0xFF);
        static readonly Color PostalBottom = new Color32(0xEE, 0x9A, 0x10, 0xFF);
        static readonly Color PostalOutline = new Color32(0x0D, 0x2E, 0x66, 0xFF);

        // Magic Mail: the user's own alphabet, drawn by an image generator as two sheets and traced into MagicMail.ttf.
        // Both sheets are the same letters, so one font carries them and the two looks are materials: the numbers sheet
        // is a cream face with a dark red outline about 7% of the cap height, the text sheet the same letters in solid
        // dark red. Both have a soft shadow down and to the right.
        static readonly Color MailFace = new Color32(0xFF, 0xFA, 0xF6, 0xFF);
        static readonly Color MailInk = new Color32(0x77, 0x1D, 0x12, 0xFF);
        static readonly Color MailOutline = new Color32(0x7E, 0x13, 0x0D, 0xFF);
        static readonly Color MailShadow = new Color(0.16f, 0.03f, 0.02f, 0.8f);

        public static TMP_FontAsset Display => LoadOrCreate("LilitaOne-Regular.ttf", "LilitaOne SDF");

        /// The traced Magic Mail alphabet.
        public static TMP_FontAsset Mail => LoadOrCreate("MagicMail.ttf", "MagicMail SDF");

        /// Counters and any big number: cream letters outlined in dark red, as in the numbers sheet.
        public static Material MailNumbers => MailPreset("MagicMail Numeros", MailFace, MailOutline);

        /// Running text and labels: solid dark red, as in the text sheet.
        public static Material MailTexts => MailPreset("MagicMail Textos", MailInk, null);

        public static void ApplyMail(TextMeshProUGUI label, bool numbers)
        {
            var font = Mail;
            if (font == null) return;
            label.font = font;
            var material = numbers ? MailNumbers : MailTexts;
            if (material != null) label.fontSharedMaterial = material;
            label.color = Color.white;
            label.enableVertexGradient = false;
        }

        // Unlike the postal preset, this one rewrites its values every time: the look is still being tuned, and a
        // material already on disk would otherwise freeze the old one. Hand edits in the .mat are lost on a re-run.
        static Material MailPreset(string name, Color face, Color? outline)
        {
            var font = Mail;
            if (font == null || font.material == null) return null;
            var path = Folder + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var made = material == null;
            if (made) material = new Material(font.material) { name = name };
            material.SetColor(ShaderUtilities.ID_FaceColor, face);
            // The mock-up's letters are chunkier than the alphabet sheet's: the face is fattened a little.
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.12f);
            if (outline.HasValue)
            {
                material.EnableKeyword(ShaderUtilities.Keyword_Outline);
                material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
                material.SetColor(ShaderUtilities.ID_OutlineColor, outline.Value);
            }
            else
            {
                material.DisableKeyword(ShaderUtilities.Keyword_Outline);
                material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
            }
            material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            material.SetColor(ShaderUtilities.ID_UnderlayColor, MailShadow);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.4f);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.7f);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.1f);
            if (made) AssetDatabase.CreateAsset(material, path);
            else EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }
        public static TMP_FontAsset Postal => LoadOrCreate("CorreioMagico.ttf", "CorreioMagico SDF");

        /// The outlined material of the postal texts (created on first use), for runtime screens that apply it themselves.
        public static Material PostalMaterialAsset => Postal != null ? PostalMaterial(Postal, "CorreioMagico Postal") : null;

        /// Counters (Count, Value, "0/5", ...): the original postal material. Unchanged so nothing
        /// that already uses it shifts look.
        public static void ApplyPostal(TextMeshProUGUI label) => ApplyPostalVariant(label, "CorreioMagico Postal");

        /// Status/labels ("Cheio", ...): its own material clone, so tuning one never touches the other —
        /// this is what Count and Label shared before and why editing one moved both.
        public static void ApplyPostalLabel(TextMeshProUGUI label) => ApplyPostalVariant(label, "CorreioMagico Postal Label");

        static void ApplyPostalVariant(TextMeshProUGUI label, string materialName)
        {
            var font = Postal;
            if (font == null) return;
            label.font = font;
            var material = PostalMaterial(font, materialName);
            if (material != null) label.fontSharedMaterial = material;
            label.color = Color.white;
            label.enableVertexGradient = true;
            label.colorGradient = new VertexGradient(PostalTop, PostalTop, PostalBottom, PostalBottom);
        }

        static Material PostalMaterial(TMP_FontAsset font, string materialName)
        {
            var path = Folder + materialName + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null || font.material == null) return material;
            material = new Material(font.material) { name = materialName };
            material.EnableKeyword(ShaderUtilities.Keyword_Outline);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            material.SetColor(ShaderUtilities.ID_OutlineColor, PostalOutline);
            material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.03f, 0.12f, 0.3f, 0.85f));
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.9f);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.15f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static TMP_FontAsset LoadOrCreate(string sourceFile, string assetName)
        {
            var path = Folder + assetName + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;
            var source = AssetDatabase.LoadAssetAtPath<Font>(Folder + sourceFile);
            if (source != null)
            {
                try
                {
                    // Dynamic atlas: glyphs (accents included) are added as the texts need them.
                    var asset = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
                    asset.name = assetName;
                    AssetDatabase.CreateAsset(asset, path);
                    asset.material.name = assetName + " Material";
                    AssetDatabase.AddObjectToAsset(asset.material, asset);
                    foreach (var atlas in asset.atlasTextures)
                    {
                        atlas.name = assetName + " Atlas";
                        AssetDatabase.AddObjectToAsset(atlas, asset);
                    }
                    AssetDatabase.SaveAssets();
                    return asset;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Royal Aves: não consegui criar a fonte {assetName} ({e.Message}); usando a fonte do Sweet Sugar.");
                }
            }
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackPath);
        }
    }
}
