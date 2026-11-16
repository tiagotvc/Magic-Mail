using System.IO;
using RoyalAves.Effects;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    /// Cria o prefab do efeito de dica (Assets/Royal Postman Art/Resources/Effects/Hint/HintEffect.prefab) a partir das
    /// máscaras de Royal_Postman_Hint_VFX_Universal. Roda sozinho uma vez se o prefab não existir; depois ele é seu:
    /// ajuste cores, tamanhos e tempos direto no Inspector (ou na janela Royal Aves/VFX/Hint Preview).
    public static class HintEffectBuilder
    {
        const string Masks = "Assets/Royal Postman Art/VFX/Royal_Postman_Hint_VFX_Universal/Assets/HintMasks/";
        public const string PrefabPath = "Assets/Royal Postman Art/Resources/Effects/Hint/HintEffect.prefab";
        const string AdditivePath = "Assets/Royal Postman Art/VFX/Materials/ImpactAdditive.mat";

        [InitializeOnLoadMethod]
        static void AutoBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                if (File.Exists(PrefabPath) || !File.Exists(Masks + "01_border_edge_core.png")) return;
                try { Build(); }
                catch (System.Exception e) { Debug.LogError("Não consegui criar o prefab da dica: " + e); }
            };
        }

        [MenuItem("Royal Aves/VFX/Hint/Recriar prefab do efeito de dica (apaga edições)")]
        static void RebuildMenu()
        {
            if (File.Exists(PrefabPath) && !EditorUtility.DisplayDialog("Recriar prefab",
                    "Isto sobrescreve HintEffect.prefab e perde os ajustes feitos nele. Continuar?", "Recriar", "Cancelar")) return;
            Build();
        }

        [MenuItem("Royal Aves/VFX/Hint/Selecionar prefab do efeito de dica")]
        static void SelectPrefab()
        {
            if (!File.Exists(PrefabPath)) Build();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        static Sprite Mask(string file, bool repeat = false)
        {
            var path = Masks + file;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                var changed = importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single ||
                              importer.mipmapEnabled || !importer.alphaIsTransparency ||
                              importer.wrapMode != (repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp) ||
                              importer.textureCompression != TextureImporterCompression.Uncompressed;
                if (changed)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.spritePixelsPerUnit = 256f;
                    importer.SaveAndReimport();
                }
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static void Build()
        {
            EnsureFolder("Assets/Royal Postman Art/Resources/Effects/Hint");
            var root = new GameObject("HintEffect");
            try
            {
                var effect = root.AddComponent<HintEffect>();
                root.AddComponent<HintDirector>();
                effect.edgeCore = Mask("01_border_edge_core.png", true);
                effect.edgeHalo = Mask("02_border_edge_halo.png", true);
                effect.sparkleFour = Mask("06_sparkle_four_point.png");
                effect.sparkleSix = Mask("07_sparkle_six_point.png");
                effect.sparkleTiny = Mask("08_sparkle_tiny_cross.png");
                effect.softDot = Mask("09_sparkle_soft_dot.png");
                effect.cellGlow = Mask("10_highlight_cell_soft.png");
                effect.nudgeTrail = Mask("12_nudge_trail_horizontal.png");
                effect.nudgeGlow = Mask("14_nudge_local_glow.png");
                effect.additive = AssetDatabase.LoadAssetAtPath<Material>(AdditivePath);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Prefab do efeito de dica criado em " + PrefabPath);
        }

        static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
