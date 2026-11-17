using System.IO;
using RoyalAves.Effects;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    /// Cria o prefab da flecha atravessando a linha (Assets/Royal Postman Art/Resources/Effects/Arrow/ArrowFlight.prefab).
    /// Roda sozinho uma vez se faltar; depois ele é seu: ajuste tudo no Inspector.
    public static class ArrowFlightBuilder
    {
        public const string PrefabPath = "Assets/Royal Postman Art/Resources/Effects/Arrow/ArrowFlight.prefab";
        const string Masks = "Assets/Royal Postman Art/VFX/Arrow/Mascaras_PNG/";
        const string VersionKey = "RoyalAves.ArrowFlight.Version";
        const int Version = 12;
        const string ArrowArt = "Assets/Royal Postman Art/Screens/Gameplay/Power ups/Arrow.png";
        const string AdditivePath = "Assets/Royal Postman Art/VFX/Materials/ImpactAdditive.mat";
        const string RimPath = "Assets/Royal Postman Art/VFX/Materials/ArrowRim.mat";
        const string RimShaderPath = "Assets/Royal Postman Art/VFX/Materials/ArrowRim.shader";

        [InitializeOnLoadMethod]
        static void AutoBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                if (!File.Exists(ArrowArt) || !File.Exists(Masks + "01_aura_item_suave.png")) return;
                if (File.Exists(PrefabPath) && EditorPrefs.GetInt(VersionKey, 0) >= Version) return;   // refaz uma vez quando o efeito muda de versão
                try { Build(); }
                catch (System.Exception e) { Debug.LogError("Não consegui criar o prefab da flecha: " + e); }
            };
        }

        [MenuItem("Royal Aves/VFX/Flecha/Recriar prefab do efeito (apaga edições)")]
        static void RebuildMenu()
        {
            if (File.Exists(PrefabPath) && !EditorUtility.DisplayDialog("Recriar prefab",
                    "Isto sobrescreve ArrowFlight.prefab e perde os ajustes feitos nele. Continuar?", "Recriar", "Cancelar")) return;
            Build();
        }

        [MenuItem("Royal Aves/VFX/Flecha/Selecionar prefab do efeito")]
        static void SelectPrefab()
        {
            if (!File.Exists(PrefabPath)) Build();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        public static void Build()
        {
            EnsureFolder("Assets/Royal Postman Art/Resources/Effects/Arrow");
            var root = new GameObject("ArrowFlight");
            try
            {
                var effect = root.AddComponent<ArrowFlightEffect>();
                effect.arrowSprite = FullRect(ArrowArt);
                effect.rimMaterial = EnsureRimMaterial();
                effect.aura = M("01_aura_item_suave");
                effect.tipGlow = M("02_glow_ponta_dourado");
                effect.tailHalo = M("03_cauda_halo_largo");
                effect.tailCore = M("04_cauda_coracao_branco");
                effect.tailGold = M("05_cauda_faixa_dourada");
                effect.ribbonTop = M("06_fita_roxa_superior");
                effect.ribbonBottom = M("07_fita_roxa_inferior");
                effect.ribbonMid = M("08_fita_rosa_intermediaria");
                effect.ribbonHalo = M("09_fita_halo_difuso");
                effect.residue = M("10_residuo_ondulado");
                effect.sparkle4 = M("11_sparkle_quatro_pontas");
                effect.sparkle6 = M("12_sparkle_seis_pontas");
                effect.sparkleMini = M("13_sparkle_mini");
                effect.lightDot = M("14_ponto_luz");
                effect.speedStreak = M("15_risco_velocidade");
                effect.fragments = new[] { M("16_fragmento_irregular"), M("17_fragmento_pentagono"), M("18_fragmento_triangulo"), M("19_fragmento_quadrado") };
                effect.flash = M("20_flash_impacto_celula");
                effect.ring = M("21_anel_impacto_celula");
                effect.whizz = M("22_whizz_frontal");
                effect.additive = AssetDatabase.LoadAssetAtPath<Material>(AdditivePath);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
            EditorPrefs.SetInt(VersionKey, Version);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Prefab do efeito da flecha criado em " + PrefabPath);
        }

        // Mesh Type "Full Rect": com "Tight" a Unity recorta os brilhos macios num polígono (aparecia como um octógono/caixa em volta da ponta).
        static Material EnsureRimMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(RimPath);
            if (material != null) return material;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(RimShaderPath) ?? Shader.Find("RoyalAves/ArrowRim");
            if (shader == null) { Debug.LogError("Shader RoyalAves/ArrowRim não encontrado."); return null; }
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, RimPath);
            return material;
        }

        static Sprite M(string name) => FullRect(Masks + name + ".png");

        static Sprite FullRect(string path)
        {
            var sprite = BoosterIntroBuilder.LoadFull(path);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteMeshType != SpriteMeshType.FullRect)
                {
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }
            return sprite;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace("\\", "/");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
