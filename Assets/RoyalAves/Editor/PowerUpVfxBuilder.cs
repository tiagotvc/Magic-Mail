using System.IO;
using RoyalAves.Effects;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    /// Cria o prefab dos efeitos mágicos de match, bomba e line cleaner (Resources/Effects/PowerUps/PowerUpVfx.prefab) com as
    /// 30 máscaras de Royal Postman Art/VFX/Power ups. Roda sozinho se o prefab não existir (ou mudar de versão);
    /// depois ajuste tudo no Inspector ou na janela Royal Aves/VFX/Power-ups/Preview.
    public static class PowerUpVfxBuilder
    {
        public const string PrefabPath = "Assets/Royal Postman Art/Resources/Effects/PowerUps/PowerUpVfx.prefab";
        const string Masks = "Assets/Royal Postman Art/VFX/Power ups/Mascaras_PNG/";
        const string AdditivePath = "Assets/Royal Postman Art/VFX/Materials/ImpactAdditive.mat";
        const string VersionKey = "RoyalAves.PowerUpVfx.Version";
        const int Version = 16;

        [InitializeOnLoadMethod]
        static void AutoBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                if (!File.Exists(Masks + "00_Comuns/01_radial_glow.png")) return;
                if (File.Exists(PrefabPath) && EditorPrefs.GetInt(VersionKey, 0) >= Version) return;
                try { Build(); }
                catch (System.Exception e) { Debug.LogError("Não consegui criar o prefab dos efeitos de power-up: " + e); }
            };
        }

        [MenuItem("Royal Aves/VFX/Power-ups/Recriar prefab (apaga edições)")]
        static void RebuildMenu()
        {
            if (File.Exists(PrefabPath) && !EditorUtility.DisplayDialog("Recriar prefab",
                    "Isto sobrescreve PowerUpVfx.prefab e perde os ajustes feitos nele. Continuar?", "Recriar", "Cancelar")) return;
            Build();
        }

        [MenuItem("Royal Aves/VFX/Power-ups/Selecionar prefab")]
        static void SelectPrefab()
        {
            if (!File.Exists(PrefabPath)) Build();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        static Sprite M(string relative)
        {
            var path = Masks + relative + ".png";
            var sprite = BoosterIntroBuilder.LoadFull(path);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteMeshType != SpriteMeshType.FullRect)
                {
                    settings.spriteMeshType = SpriteMeshType.FullRect;     // brilhos macios não podem ser recortados num polígono
                    importer.SetTextureSettings(settings);
                    importer.SaveAndReimport();
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }
            return sprite;
        }

        public static void Build()
        {
            EnsureFolder("Assets/Royal Postman Art/Resources/Effects/PowerUps");
            var root = new GameObject("PowerUpVfx");
            try
            {
                var p = root.AddComponent<PowerUpVfxPlayer>();
                p.radialGlow = M("00_Comuns/01_radial_glow");
                p.sparkle4 = M("00_Comuns/02_sparkle_4_pontas");
                p.sparkle6 = M("00_Comuns/03_sparkle_6_pontas");
                p.fragments = new[] { M("00_Comuns/04_fragmento_losango"), M("00_Comuns/05_fragmento_lasca"), M("00_Comuns/06_fragmento_triangulo") };
                p.dot = M("00_Comuns/07_particula_ponto");

                p.matchRing = M("01_Match_Normal/01_anel_unico_suave");
                p.matchFlash = M("01_Match_Normal/02_micro_clarao");
                p.matchRingFragmented = M("01_Match_Normal/03_anel_fragmentado");
                p.matchSparkleTrail = M("01_Match_Normal/04_rastro_sparkle_curto");

                p.bombFlash = M("02_Bomba_Area/01_flash_central_intenso");
                p.bombRingThin = M("02_Bomba_Area/02_anel_choque_fino");
                p.bombRingWide = M("02_Bomba_Area/03_anel_choque_largo");
                p.bombRaysLong = M("02_Bomba_Area/04_raios_radiais_longos");
                p.bombRaysShort = M("02_Bomba_Area/05_raios_impacto_curtos");
                p.bombAura = M("02_Bomba_Area/06_aura_dissipacao");

                p.lineCore = M("03_Line_Cleaner/01_feixe_nucleo_branco");
                p.lineGlow = M("03_Line_Cleaner/02_feixe_glow_largo");
                p.ribbonA = M("03_Line_Cleaner/03_fita_magica_A");
                p.ribbonB = M("03_Line_Cleaner/04_fita_magica_B");
                p.lineTip = M("03_Line_Cleaner/05_ponta_impacto");
                p.lineBreakRing = M("03_Line_Cleaner/06_micro_anel_quebra");
                p.lineDust = M("03_Line_Cleaner/07_poeira_luminosa_canal");

                p.seekerRing = M("04_Seeker_Impacto/01_marcador_alvo_anel");
                p.seekerFlash = M("04_Seeker_Impacto/02_flash_contato");
                p.seekerStar = M("04_Seeker_Impacto/03_estrela_impacto");
                p.seekerArc = M("04_Seeker_Impacto/04_arco_espiral");
                p.seekerTrail = M("04_Seeker_Impacto/05_rastro_curto_de_chegada");
                p.seekerMicroStar = M("04_Seeker_Impacto/06_micro_estrela_radial");
                p.additive = AssetDatabase.LoadAssetAtPath<Material>(AdditivePath);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
            EditorPrefs.SetInt(VersionKey, Version);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Prefab dos efeitos de power-up criado em " + PrefabPath);
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
