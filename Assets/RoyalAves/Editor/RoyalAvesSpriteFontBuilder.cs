// Builds a TextMeshPro Sprite Asset directly from an already-sliced spritesheet (Sprite Mode: Multiple,
// each sub-sprite named after the character it draws). Single-character sprite names get the REAL Unicode
// value of that character (not TMP's usual placeholder), so typing the letter itself — not just a
// <sprite name="X"> tag — renders the sprite via TMP's fallback chain.
//
// To plug in a different style later: slice its sheet the same way in the Sprite Editor (Multiple mode,
// one sub-sprite per character, named after the character), then call
// RoyalAvesSpriteFontBuilder.BuildFromTexture(texture, savePath) — it is not tied to this specific art.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

namespace RoyalAves.Editor
{
    public static class RoyalAvesSpriteFontBuilder
    {
        const string DialogFolder = "Assets/RoyalAves/Art/UI/Dialog/";
        const string OutputFolder = "Assets/RoyalAves/Resources/Fonts/";

        [MenuItem("Royal Aves/Criar fonte de diálogo (sprites douradas)")]
        static void BuildDialogFont()
        {
            var letters = AssetDatabase.LoadAssetAtPath<Texture2D>(DialogFolder + "Cartela 6x6 de letras e números dourados.png");
            var accents = AssetDatabase.LoadAssetAtPath<Texture2D>(DialogFolder + "Sprite de letras acentuadas douradas.png");
            var digits = AssetDatabase.LoadAssetAtPath<Texture2D>(DialogFolder + "Sprites de dígitos dourados.png");
            if (letters == null || accents == null || digits == null)
            {
                Debug.LogError("Royal Aves: alguma das 3 pranchas esperadas em " + DialogFolder + " não foi encontrada.");
                return;
            }

            EnsureFolder(OutputFolder);
            var lettersAsset = BuildFromTexture(letters, OutputFolder + "DialogoLetras.asset");
            var accentsAsset = BuildFromTexture(accents, OutputFolder + "DialogoAcentos.asset");
            var digitsAsset = BuildFromTexture(digits, OutputFolder + "DialogoDigitos.asset");
            if (lettersAsset == null || accentsAsset == null || digitsAsset == null) return;

            // One sprite asset can't mix glyphs from three different source textures directly (each needs
            // its own material/atlas) — chaining them as fallbacks gets the same result: assign
            // "DialogoLetras" anywhere and a missing character is looked up in Acentos, then Dígitos.
            lettersAsset.fallbackSpriteAssets = new List<TMP_SpriteAsset> { accentsAsset, digitsAsset };
            EditorUtility.SetDirty(lettersAsset);
            AssetDatabase.SaveAssets();

            Selection.activeObject = lettersAsset;
            EditorUtility.DisplayDialog("Fonte de diálogo",
                "Criado " + OutputFolder + "DialogoLetras.asset (Acentos e Dígitos encadeados como fallback).\n" +
                "Usa ele no campo \"Sprite Asset\" de um TextMeshProUGUI — digitar o texto normalmente já mostra os sprites.",
                "OK");
        }

        // Carteila.png is the 49-character sheet (A-Z, 0-9, accents) that replaced the original 3 separate
        // sheets: one texture, no fallback chain needed. Rebuilds DialogoLetras.asset in place, so anything
        // already pointing at it (LevelStartTitle, SpriteWordText, ...) picks up the new art without rewiring.
        [MenuItem("Royal Aves/Criar fonte de diálogo (prancha única)")]
        static void BuildDialogFontSingleSheet()
        {
            var sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(DialogFolder + "Carteila.png");
            if (sheet == null)
            {
                Debug.LogError("Royal Aves: " + DialogFolder + "Carteila.png não encontrada.");
                return;
            }

            EnsureFolder(OutputFolder);
            var asset = BuildFromTexture(sheet, OutputFolder + "DialogoLetras.asset");
            if (asset == null) return;

            // Drop the old fallback chain, if this asset was previously built from the 3-sheet setup.
            if (asset.fallbackSpriteAssets != null && asset.fallbackSpriteAssets.Count > 0)
            {
                asset.fallbackSpriteAssets.Clear();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }

            Selection.activeObject = asset;
            EditorUtility.DisplayDialog("Fonte de diálogo",
                "Atualizado " + OutputFolder + "DialogoLetras.asset a partir de Carteila.png (49 caracteres, uma prancha só).",
                "OK");
        }

        // CorreioMagico SDF (the body font every other label on screen uses) was sampled at 90 pt
        // (Assets/RoyalAves/Fonts/CorreioMagico SDF.asset, m_PointSize: 90). TMP renders a sprite glyph's
        // raw pixel metrics as if they were font units at THAT point size, so a 179 px letter cell came out
        // ~2x too big and overlapping. Scaling each glyph to its sheet's own cell height keeps it matched
        // to body text regardless of how big the source art is.
        const float ReferencePointSize = 90f;

        /// Reusable: builds (or rebuilds, if it already exists at savePath) a Sprite Asset from any texture
        /// already sliced into named sub-sprites — not tied to this specific art style.
        public static TMP_SpriteAsset BuildFromTexture(Texture2D source, string savePath, float targetPointSize = ReferencePointSize)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(source)).OfType<Sprite>().ToArray();
            if (sprites.Length == 0)
            {
                Debug.LogWarning("Royal Aves: " + source.name + " não tem sprites fatiados (Sprite Mode: Multiple, com sub-sprites nomeados).", source);
                return null;
            }

            var asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(savePath);
            var isNew = asset == null;
            if (isNew)
            {
                asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                AssetDatabase.CreateAsset(asset, savePath);
            }
            // version is "internal set" (TMP's own assembly only) — a blank version makes TMP try to
            // auto-"upgrade" from the legacy spriteInfoList the moment material/UpdateLookupTables touch
            // it, which is null on a fresh asset and throws. SerializedObject bypasses the C# access
            // modifier to set the backing field directly, so that migration path never triggers.
            var versionProperty = new SerializedObject(asset).FindProperty("m_Version");
            if (versionProperty != null && string.IsNullOrEmpty(versionProperty.stringValue))
            {
                versionProperty.stringValue = "1.1.0";
                versionProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
            asset.spriteSheet = source;

            var glyphTable = new List<TMP_SpriteGlyph>();
            var characterTable = new List<TMP_SpriteCharacter>();
            for (var i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                var glyphScale = sprite.rect.height > 0 ? targetPointSize / sprite.rect.height : 1f;
                // Bearing anchors the glyph bottom-left at the pen position, matching Unity's own TMP
                // Sprite Asset generator (TMP_SpriteAssetMenu.cs). Sprite.pivot is in PIXELS, not
                // normalised 0-1 — using it here put a ~half-cell offset on every glyph, stacking each
                // character almost on top of the last instead of advancing the pen sideways.
                var glyph = new TMP_SpriteGlyph
                {
                    index = (uint)i,
                    metrics = new GlyphMetrics(sprite.rect.width, sprite.rect.height, 0, sprite.rect.height, sprite.rect.width),
                    glyphRect = new GlyphRect(sprite.rect),
                    scale = glyphScale,
                    sprite = sprite,
                };
                glyphTable.Add(glyph);

                var character = new TMP_SpriteCharacter(0xFFFE, glyph) { name = sprite.name, scale = 1f };
                if (sprite.name.Length == 1) character.unicode = sprite.name[0];
                characterTable.Add(character);
            }
            // spriteGlyphTable/spriteCharacterTable are get-only from outside the TMPro assembly — mutate
            // the lists they already hold instead of reassigning the properties.
            asset.spriteGlyphTable.Clear();
            asset.spriteGlyphTable.AddRange(glyphTable);
            asset.spriteCharacterTable.Clear();
            asset.spriteCharacterTable.AddRange(characterTable);

            if (asset.material == null)
            {
                var shader = Shader.Find("TextMeshPro/Sprite");
                var material = new Material(shader);
                material.SetTexture(ShaderUtilities.ID_MainTex, source);
                material.name = asset.name + " Material";
                asset.material = material;
                AssetDatabase.AddObjectToAsset(material, asset);
            }

            asset.UpdateLookupTables();
            EditorUtility.SetDirty(asset);
            if (isNew) AssetDatabase.SaveAssets();
            return asset;
        }

        static void EnsureFolder(string path)
        {
            path = path.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
