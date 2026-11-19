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
        const string LightBallMasks = "Assets/Royal Postman Art/VFX/Light ball/Mascaras_PNG/";
        const string DoubleMasks = "Assets/Royal Postman Art/VFX/Light ball/doubles/Mascaras_PNG/";
        static readonly string[] DoubleNames =
        {
            "01_soft_core_glow", "02_intense_flash", "03_ring_thin", "04_ring_thick", "05_orbit_thin", "06_orbit_thick", "07_spiral_ribbon", "08_spiral_double",
            "09_vortex_small", "10_vortex_large", "11_radial_burst", "12_radial_burst_soft", "13_magic_streak", "14_sparkle_ring", "15_sparkle_cluster", "16_fragment_cluster",
        };
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
                try
                {
                    if (File.Exists(PrefabPath) && EditorPrefs.GetInt(VersionKey, 0) >= Version) LinkLightBallMasks();
                    else Build();
                }
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

        static Sprite M(string relative) => Load(Masks + relative + ".png");

        /// Liga as 14 máscaras do Light Ball no prefab que já existe, sem recriá-lo (os ajustes feitos nele ficam).
        static void LinkLightBallMasks()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var current = asset != null ? asset.GetComponent<PowerUpVfxPlayer>() : null;
            if (current == null) return;
            var single = File.Exists(LightBallMasks + "06_connection_beam_core.png") && (current.lbBeamCore == null || current.lbStars == null);
            var pair = File.Exists(DoubleMasks + DoubleNames[0] + ".png") &&
                       (current.doubleMasks == null || current.doubleMasks.Length < DoubleNames.Length || System.Array.Exists(current.doubleMasks, m => m == null));
            var hatArt = current.hatIcon == null || current.useButtonBorder == null || current.useButtonCenter == null || current.useButtonFont == null;
            if (!single && !pair && !hatArt) return;
            AssetDatabase.Refresh();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                AssignLightBall(root.GetComponent<PowerUpVfxPlayer>());
                AssignDoubleBall(root.GetComponent<PowerUpVfxPlayer>());
                AssignHat(root.GetComponent<PowerUpVfxPlayer>());
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Debug.Log("Máscaras do Light Ball ligadas em " + PrefabPath);
        }

        /// Arte do Chapéu de Mago: figura do chapéu e o botão verde da modal de power-ups (só preenche o que estiver vazio).
        static void AssignHat(PowerUpVfxPlayer p)
        {
            if (p.hatIcon == null) p.hatIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/Boosters/cap.png");
            if (p.useButtonBorder == null) p.useButtonBorder = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Royal Postman Art/Screens/Modal/button_border.png");
            if (p.useButtonCenter == null) p.useButtonCenter = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Royal Postman Art/Screens/Modal/button_center.png");
            if (p.useButtonFont == null) p.useButtonFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/RoyalAves/Fonts/LilitaOne SDF.asset");
        }

        static void AssignDoubleBall(PowerUpVfxPlayer p)
        {
            if (!File.Exists(DoubleMasks + DoubleNames[0] + ".png")) return;
            var sprites = new Sprite[DoubleNames.Length];
            for (var i = 0; i < sprites.Length; i++) sprites[i] = Load(DoubleMasks + DoubleNames[i] + ".png");
            p.doubleMasks = sprites;
        }

        static void AssignLightBall(PowerUpVfxPlayer p)
        {
            if (!File.Exists(LightBallMasks + "06_connection_beam_core.png")) return;
            Sprite L(string name) => Load(LightBallMasks + name + ".png");
            p.lbCoreGlow = L("01_ball_core_glow");
            p.lbPulseRing = L("02_ball_pulse_ring");
            p.lbOrbitEllipse = L("03_ball_orbit_ellipse");
            p.lbOrbitArc = L("04_ball_orbit_arc");
            p.lbChargeFlare = L("05_ball_charge_flare");
            p.lbBeamCore = L("06_connection_beam_core");
            p.lbBeamHalo = L("07_connection_beam_halo");
            p.lbRibbonWave = L("08_connection_ribbon_wave");
            p.lbRibbonSecondary = L("09_connection_ribbon_secondary");
            p.lbTargetHalo = L("10_target_precharge_halo");
            p.lbTargetPreflash = L("11_target_preflash");
            p.lbTargetPop = L("12_target_pop_burst");
            p.lbFinalBurst = L("13_final_central_burst");
            p.lbStars = L("14_scattered_twinkling_stars");
        }

        static Sprite Load(string path)
        {
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
                AssignLightBall(p);
                AssignDoubleBall(p);
                AssignHat(p);
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
