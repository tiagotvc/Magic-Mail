using System;
using System.IO;
using System.Linq;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RoyalAves.EditorTools
{
    // Prepara o mapa 3D de forma aditiva: layer "WorldMap", um Universal Renderer extra na lista do URP Asset
    // (o Renderer2D padrao e a camera do jogo ficam intactos) e o WorldMapKit em Resources.
    public static class RoyalAvesWorldMapSetup
    {
        const string RendererPath = "Assets/Settings/MapWorldRenderer.asset";
        const string KitPath = "Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset";
        const string Sprites = "Assets/RoyalAves/Art/MapKitV2/Sprites/";
        const string Shaders = "Assets/RoyalAves/Art/MapKitV2/Shaders/";
        const string OldMaps = "Assets/RoyalAves/Art/Maps/";

        [InitializeOnLoadMethod]
        static void Auto()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                try { Ensure(false); }
                catch (Exception e) { Debug.LogWarning("[MapaMundo3D] configuracao automatica falhou: " + e.Message); }
            };
        }

        [MenuItem("Royal Aves/Mapa 3D/Configurar layer, renderer e kit")]
        static void Menu() => Ensure(true);

        public static WorldMapKit Ensure(bool log)
        {
            RoyalAvesMapIslandControls.GetOrCreate();
            RoyalAvesMapLayoutControls.GetOrCreate();
            EnsurePostProfile();
            EnsureLayer(log);
            EnsureSheetModes(log);
            var rendererIndex = EnsureRenderer(log);
            var kit = AssetDatabase.LoadAssetAtPath<WorldMapKit>(KitPath);
            var created = kit == null;
            if (created)
            {
                kit = ScriptableObject.CreateInstance<WorldMapKit>();
                AssetDatabase.CreateAsset(kit, KitPath);
            }
            var changed = created;
            changed|=Set(ref kit.glow,AssetDatabase.LoadAssetAtPath<Shader>(Shaders+"MapGlow.shader"));
            changed|=Set(ref kit.beam,AssetDatabase.LoadAssetAtPath<Shader>(Shaders+"MapBeam.shader"));
            changed|=Set(ref kit.sparkle,AssetDatabase.LoadAssetAtPath<Shader>(Shaders+"MapSparkle.shader"));
            changed|=Set(ref kit.vapor,AssetDatabase.LoadAssetAtPath<Shader>(Shaders+"MapVapor.shader"));
            changed|=Set(ref kit.navigationSurface,AssetDatabase.LoadAssetAtPath<Shader>(Shaders+"MapNavigation.shader"));
            changed|=Set(ref kit.navigationPaint,AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RoyalAves/Art/MapKitV3/MapUI/nav-surface-v23.png"));
            changed|=Set(ref kit.navigationSelectedPaint,AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RoyalAves/Art/MapKitV3/MapUI/nav-selected-v23.png"));
            if (kit.tuningVersion < 14)
            {
                kit.radius = 26;
                kit.cameraHeight = 4.9f;
                kit.cameraBack = 4.4f;
                kit.pitch = 32;
                kit.fov = 48;
                kit.tokenScale = 0.62f;
                kit.illustratedProjection = true;
                kit.illustratedSize = 4.6f;
                kit.focusAhead = -2.55f;
                kit.tuningVersion = 14;
                changed = true;
            }
            if(kit.tuningVersion<15)
            {
                kit.illustratedSize=4.0f;
                kit.tuningVersion=15;
                changed=true;
            }
            changed |= Set(ref kit.surface, LoadShader(Shaders + "MapSurface.shader"));
            changed |= Set(ref kit.card, LoadShader(Shaders + "MapCard.shader"));
            changed |= Set(ref kit.shadow, LoadShader(Shaders + "MapShadow.shader"));
            changed |= Set(ref kit.water, LoadShader(Shaders + "MapWater.shader"));
            if (rendererIndex >= 0 && kit.rendererIndex != rendererIndex) { kit.rendererIndex = rendererIndex; changed = true; }
            changed |= Set(ref kit.dogIdle, Slice("dog-courier-poses-glow-spaced.png", "dog_idle"));
            changed |= Set(ref kit.dogWag, Slice("dog-courier-poses-glow-spaced.png", "dog_wag"));
            changed |= Set(ref kit.dogSniff, Slice("dog-courier-poses-glow-spaced.png", "dog_sniff"));
            changed |= Set(ref kit.dogCarry, Slice("dog-courier-poses-glow-spaced.png", "dog_carry"));
            changed |= Set(ref kit.mailboxClosed, Slice("mailbox-poses.png", "mailbox_closed"));
            changed |= Set(ref kit.mailboxOpen, Slice("mailbox-poses.png", "mailbox_open"));
            changed |= Set(ref kit.lamp, Slice("postal-lamp.png", "postal_lamp"));
            changed |= Set(ref kit.tree, Slice("round-purple-tree-glow.png", "round_purple_tree"));
            changed |= Set(ref kit.parcels, Slice("small-postal-props-glow-spaced.png", "prop_parcels"));
            changed |= Set(ref kit.flowers, Slice("small-postal-props-glow-spaced.png", "prop_flowers"));
            changed |= Set(ref kit.sign, Slice("small-postal-props-glow-spaced.png", "prop_sign"));
            changed |= Set(ref kit.shrub, Slice("small-postal-props-glow-spaced.png", "prop_shrub"));
            changed |= Set(ref kit.boat, Slice("postal-region-landmarks-glow.png", "landmark_boat"));
            changed |= Set(ref kit.windmill, Slice("postal-region-landmarks-glow.png", "landmark_windmill"));
            changed |= Set(ref kit.lighthouse, Slice("postal-region-landmarks-glow.png", "landmark_lighthouse"));
            changed |= Set(ref kit.hut, Slice("postal-region-landmarks-glow.png", "landmark_mountain_hut"));
            changed |= Set(ref kit.clock, Slice("postal-region-landmarks-glow.png", "landmark_clock"));
            changed |= Set(ref kit.agency, Slice("postal-region-landmarks-glow.png", "landmark_grand_agency"));
            changed |= Set(ref kit.tunnel, Slice("postal-region-landmarks-glow.png", "landmark_tunnel"));
            changed |= Set(ref kit.kite, Slice("postal-region-landmarks-glow.png", "landmark_kite"));
            changed |= Set(ref kit.pine, AssetDatabase.LoadAssetAtPath<Sprite>(OldMaps + "cartoon-pine.png"));
            changed |= Set(ref kit.legacyPostOffice, AssetDatabase.LoadAssetAtPath<Sprite>(OldMaps + "cartoon-post-office.png"));
            changed |= Set(ref kit.postOffice,V3("GreenLand","postal-house-cartoon-v2","house")??V3("GreenLand","green-post-office-polished","house")??kit.legacyPostOffice);
            changed |= Set(ref kit.bridgeDeck,V3("Shared","postal-bridge-cartoon-v2","mint")??V3("Shared","postal-bridge-variants","mint")??V3("GreenLand","green-bridge-deck","deck"));
            changed |= Set(ref kit.factory, AssetDatabase.LoadAssetAtPath<Sprite>(OldMaps + "cartoon-sorting-factory.png"));
            changed |= Set(ref kit.cloudSmall, V3("Shared", "shared-clouds", "puff-small"));
            changed |= Set(ref kit.cloudWide, V3("Shared", "shared-clouds", "puff-wide"));
            changed |= Set(ref kit.cloudStrip, V3("Shared", "shared-clouds", "cloud-strip"));
            changed |= Set(ref kit.envelopeCream, V3("Shared", "shared-mail-floaters", "envelope-cream"));
            changed |= Set(ref kit.envelopePurple, V3("Shared", "shared-mail-floaters", "envelope-purple"));
            changed |= Set(ref kit.envelopeTeal, V3("Shared", "shared-mail-floaters", "envelope-teal"));
            changed |= Set(ref kit.factoryBody, V3("GreenLand", "green-letter-factory", "main-building"));
            changed |= Set(ref kit.factoryChimney, V3("GreenLand", "green-letter-factory", "chimney"));
            changed |= Set(ref kit.factoryChute, V3("GreenLand", "green-letter-factory", "side-letter-chute"));
            changed |= Set(ref kit.fountain, V3("GreenLand", "green-postal-fountain", "envelope-fountain"));
            changed |= Set(ref kit.bench, V3("GreenLand", "green-garden-props", "envelope-bench"));
            changed |= Set(ref kit.topiary, V3("GreenLand", "green-garden-props", "seal-topiary"));
            changed |= Set(ref kit.parcelCart, V3("GreenLand", "green-garden-props", "parcel-cart"));
            changed |= Set(ref kit.postalGate, V3("GreenLand", "green-route-landmarks", "postal-gate"));
            changed |= Set(ref kit.skyClear, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/GreenLand/sky-green-clear-blue.png"));
            changed |= Set(ref kit.skyWarm, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/GreenLand/sky-green-warm-sunset.png"));
            changed |= Set(ref kit.skyOvercast, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/GreenLand/sky-green-overcast-blue-gray.png"));
            changed |= Set(ref kit.skyLilac, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/GreenLand/sky-green-garden-lilac.png"));
            changed |= Set(ref kit.snowPineUpper, V3("Christmas", "christmas-snow-foliage", "pine-upper"));
            changed |= Set(ref kit.snowPineMiddle, V3("Christmas", "christmas-snow-foliage", "pine-middle"));
            changed |= Set(ref kit.snowPineLower, V3("Christmas", "christmas-snow-foliage", "pine-lower"));
            changed |= Set(ref kit.snowRound, V3("Christmas", "christmas-snow-foliage", "round-crown"));
            changed |= Set(ref kit.snowMailbox, V3("Christmas", "christmas-snow-small-props", "mailbox-arch"));
            changed |= Set(ref kit.snowLamp, V3("Christmas", "christmas-snow-small-props", "lamp-cap"));
            changed |= Set(ref kit.snowRoof, V3("Christmas", "christmas-snow-roofs", "gable-left"));
            changed |= Set(ref kit.groundPaint, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/GreenLand/ground-painted-neutral.png"));
            changed |= Set(ref kit.groundIllustration, AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/GreenLand/ground-green-layer.png"));
            changed |= Set(ref kit.groundBrushLawn, AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RoyalAves/Art/MapKitV3/GroundPaint/lawn-brush-v21.png"));
            changed |= Set(ref kit.groundBrushPath, AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RoyalAves/Art/MapKitV3/GroundPaint/path-brush-v21.png"));
            changed |= Set(ref kit.groundLavaIllustration,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/LavaLand/ground-lava-layer.png"));
            changed |= Set(ref kit.skyGoldenOnly,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Shared/sky-golden-layer.png"));
            changed |= Set(ref kit.warmCloudSmall,V3("Shared","warm-cloud-layers","small"));
            changed |= Set(ref kit.warmCloudWide,V3("Shared","warm-cloud-layers","wide"));
            changed |= Set(ref kit.lavaCloudSmall,V3("LavaLand","lava-cloud-layers","small"));
            changed |= Set(ref kit.lavaCloudWide,V3("LavaLand","lava-cloud-layers","wide"));
            changed |= Set(ref kit.volcanoSingle,V3("LavaLand","lava-horizon-volcanoes","round-single"));
            changed |= Set(ref kit.volcanoTwin,V3("LavaLand","lava-horizon-volcanoes","twin-cones"));
            changed |= Set(ref kit.volcanoCaldera,V3("LavaLand","lava-horizon-volcanoes","wide-caldera"));
            var boundsPath = "Assets/RoyalAves/Art/MapKitV3/GreenLand/ground-green-layer-bounds.json";
            if (File.Exists(boundsPath))
            {
                var bounds = JsonUtility.FromJson<GroundBoundsFile>(File.ReadAllText(boundsPath));
                if (bounds?.rows != null && bounds.rows.Length == 64 && (kit.groundLaneBounds == null || !kit.groundLaneBounds.SequenceEqual(bounds.rows)))
                {
                    kit.groundLaneBounds = bounds.rows;
                    changed = true;
                }
            }
            var lavaBoundsPath="Assets/RoyalAves/Art/MapKitV3/LavaLand/ground-lava-layer-bounds.json";
            if(File.Exists(lavaBoundsPath))
            {
                var bounds=JsonUtility.FromJson<GroundBoundsFile>(File.ReadAllText(lavaBoundsPath));
                if(bounds?.rows!=null&&bounds.rows.Length==64&&(kit.groundLavaLaneBounds==null||!kit.groundLavaLaneBounds.SequenceEqual(bounds.rows)))
                {kit.groundLavaLaneBounds=bounds.rows;changed=true;}
            }
            changed |= Set(ref kit.levelCrown,V3("Shared","level-emblems-v3","crown")??AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Shared/postal-level-crown.png"));
            changed |= Set(ref kit.levelStar,V3("Shared","level-emblems-v3","star"));
            changed |= Set(ref kit.levelFaceActive,V3("Shared","premium-level-faces-v2","active")??V3("Shared","level-faces-magic-mail","active"));
            changed |= Set(ref kit.levelFaceCompleted,V3("Shared","premium-level-faces-v2","completed")??V3("Shared","level-faces-magic-mail","completed"));
            changed |= Set(ref kit.levelFaceUpcoming,V3("Shared","premium-level-faces-v2","upcoming")??V3("Shared","level-faces-magic-mail","upcoming"));
            changed |= Set(ref kit.levelFaceActive,V3("Shared","premium-level-faces-v2","active"));
            changed |= Set(ref kit.levelFaceCompleted,V3("Shared","premium-level-faces-v2","completed"));
            changed |= Set(ref kit.levelFaceUpcoming,V3("Shared","premium-level-faces-v2","upcoming"));
            changed |= Set(ref kit.bridgeDeck,V3("Shared","postal-bridge-cartoon-v2","mint")??V3("Shared","postal-bridge-variants","mint"));
            changed |= Set(ref kit.bridgeTimber,V3("Shared","postal-bridge-cartoon-v2","timber")??V3("Shared","postal-bridge-variants","timber"));
            changed |= Set(ref kit.bridgeLogs,V3("Shared","postal-bridge-cartoon-v2","logs")??V3("Shared","postal-bridge-variants","logs"));
            changed |= Set(ref kit.hardBadge,V3("Shared","level-emblems-v3","skull-teal")??V3("Shared","postal-difficulty-badges","hard"));
            changed |= Set(ref kit.expressBadge,V3("Shared","level-emblems-v3","skull-violet")??V3("Shared","postal-difficulty-badges","express"));
            changed |= Set(ref kit.terrainMaterials,V3("Shared","painted-ground-materials","atlas"));
            changed |= Set(ref kit.mailWindmill,V3("GreenLand","postal-scenery-cartoon-v2","windmill")??V3("GreenLand","postal-scenery-variety","windmill"));
            changed |= Set(ref kit.curvedLantern,V3("GreenLand","postal-scenery-cartoon-v2","lantern")??V3("GreenLand","postal-scenery-variety","lantern"));
            changed |= Set(ref kit.autumnTree,V3("GreenLand","postal-scenery-cartoon-v2","autumn-tree")??V3("GreenLand","postal-scenery-variety","autumn-tree"));
            changed |= Set(ref kit.postalPine,V3("GreenLand","postal-scenery-cartoon-v2","pine")??V3("GreenLand","postal-scenery-variety","pine"));
            changed |= Set(ref kit.mailKiosk,V3("GreenLand","postal-scenery-cartoon-v2","kiosk")??V3("GreenLand","postal-scenery-variety","kiosk"));
            changed |= Set(ref kit.shrubCoral,V3("GreenLand","postal-ground-details-v3","coral-shrub"));
            changed |= Set(ref kit.shrubGold,V3("GreenLand","postal-ground-details-v3","gold-shrub"));
            changed |= Set(ref kit.shrubMint,V3("GreenLand","postal-ground-details-v3","mint-shrub"));
            changed |= Set(ref kit.detailTulips,V3("GreenLand","postal-ground-details-v3","tulips"));
            changed |= Set(ref kit.detailBells,V3("GreenLand","postal-ground-details-v3","bells"));
            changed |= Set(ref kit.detailRocks,V3("GreenLand","postal-ground-details-v3","rocks"));
            changed |= Set(ref kit.bellFlowers,V3("GreenLand","postal-scenery-cartoon-v2","flowers")??V3("GreenLand","postal-scenery-variety","flowers"));
            changed |= Set(ref kit.lavaTree,V3("LavaLand","lava-thermal-flora","ember-tree"));
            changed |= Set(ref kit.lavaShrub,V3("LavaLand","lava-thermal-flora","ash-shrub"));
            changed |= Set(ref kit.lavaFlower,V3("LavaLand","lava-thermal-flora","thermal-flower"));
            changed |= Set(ref kit.lavaLamp,V3("LavaLand","lava-postal-lamp","ember-postal-lamp"));
            changed |= Set(ref kit.lavaMailbox,V3("LavaLand","lava-mailbox","basalt-mailbox-closed"));
            changed |= Set(ref kit.lavaMailboxOpen,V3("LavaLand","lava-mailbox","basalt-mailbox-open"));
            changed |= Set(ref kit.lavaForge,V3("LavaLand","lava-forge-factory","forge-building"));
            changed |= Set(ref kit.lavaOffice,V3("LavaLand","lava-landmark-set","thermal-post-office"));
            changed |= Set(ref kit.lavaObservatory,V3("LavaLand","lava-landmark-set","postal-observatory"));
            changed |= Set(ref kit.lavaCrystal,V3("LavaLand","lava-crystals","teal-crystal"));
            changed |= Set(ref kit.lavaBoulder,V3("LavaLand","lava-basalt-props","basalt-cluster"));
            Sprite[] Family(string folder,string file)=>new[]{"tree","office","lamp","mailbox","shrub","flowers"}.Select(part=>V3(folder,file,part)).ToArray();
            bool FamilySet(ref Sprite[] target,Sprite[] value)
            {if(value.Any(x=>x==null)||target!=null&&target.SequenceEqual(value))return false;target=value;return true;}
            changed|=FamilySet(ref kit.gardenFamily,Family("GreenLand","garden-vivid-v4"));
            changed|=FamilySet(ref kit.forestFamily,Family("ForestLand","forest-postal-v4"));
            changed|=FamilySet(ref kit.coastFamily,Family("CoastLand","coral-submerged-v5"));
            changed|=FamilySet(ref kit.auroraFamily,Family("AuroraLand","aurora-postal-v4"));
            changed|=FamilySet(ref kit.desertFamily,Family("DesertLand","desert-postal-v5"));
            changed|=FamilySet(ref kit.orchardFamily,Family("OrchardLand","orchard-postal-v5"));
            changed|=FamilySet(ref kit.marshFamily,Family("MarshLand","marsh-postal-v5"));
            changed|=FamilySet(ref kit.cloudFamily,Family("CloudLand","cloud-postal-v5"));
            changed|=FamilySet(ref kit.crystalFamily,Family("CrystalLand","crystal-postal-v5"));
            changed|=Set(ref kit.geyserBase,V3("Shared","layered-effects-v5","geyser-base"));
            changed|=Set(ref kit.geyserWater,V3("Shared","layered-effects-v5","geyser-water"));
            changed|=Set(ref kit.geyserSteam,V3("Shared","layered-effects-v5","geyser-steam"));
            changed|=Set(ref kit.meteorSprite,V3("Shared","layered-effects-v5","meteor"));
            changed|=Set(ref kit.snowTerrainMaterials,V3("Christmas","snow-ground-v5","atlas"));
            changed|=FamilySet(ref kit.simpleNature,new[]{"round-tree","pine","shrub","flowers","pebbles","fence"}.Select(part=>V3("SimpleBase","simple-nature-v6",part)).ToArray());
            changed|=FamilySet(ref kit.simplePostal,new[]{"house","workshop","lamp","courier","owl","mailbox"}.Select(part=>V3("SimpleBase","simple-postal-v6",part)).ToArray());
            changed|=Set(ref kit.simpleBridge,V3("SimpleBase","simple-bridge-v6","deck"));
            changed|=Set(ref kit.simpleCrown,V3("SimpleBase","simple-emblems-v6","crown"));
            changed|=Set(ref kit.simpleStar,V3("SimpleBase","simple-emblems-v6","star"));
            changed|=Set(ref kit.simpleHard,V3("SimpleBase","simple-emblems-v6","hard"));
            changed|=Set(ref kit.simpleExpress,V3("SimpleBase","simple-emblems-v6","express"));
            changed|=Set(ref kit.simpleCloudSmall,V3("SimpleBase","simple-clouds-v6","small"));
            changed|=Set(ref kit.simpleCloudWide,V3("SimpleBase","simple-clouds-v6","wide"));
            changed|=FamilySet(ref kit.postalTrees,new[]{"round","tall","wide"}.Select(part=>V3("SimpleBase","postal-trees-v9",part)).ToArray());
            changed|=FamilySet(ref kit.entranceTrees,new[]{"round-violet","wide-teal"}.Select(part=>V3("SimpleBase","entrance-trees-v10",part)).ToArray());
            changed|=FamilySet(ref kit.postalTreeDetails,new[]{"leaf-gold","leaf-coral","letter-cream","letter-teal"}.Select(part=>V3("SimpleBase","postal-tree-details-v9",part)).ToArray());
            changed|=Set(ref kit.treeLeafShader,Shader.Find("RoyalAves/WorldMap/Leaf"));
            changed|=Set(ref kit.postmanSprite,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Characters/postman-map-v12.png"));
            changed|=FamilySet(ref kit.expressiveTrees,new[]{"crooked-canopy-v20","coral-fan-tree-v20","teal-wonky-pine-v20","crooked-mail-palm-v20"}
                .Select(name=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/Arvores/"+name+".png")).ToArray());
            changed|=Set(ref kit.expressivePostOffice,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/Construcoes/forest-post-v19.png"));
            changed|=Set(ref kit.expressiveWorkshop,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/Construcoes/seal-workshop-v19.png"));
            changed|=Set(ref kit.mapUiTemplate,AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RoyalAves/Prefabs/RoyalAvesLobby.prefab"));
            var mapIcons=new[]{"events","ranking","map","teams","collection"}.Select(name=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/MapUI/nav-"+name+"-v22.png")).ToArray();
            if(mapIcons.All(sprite=>sprite!=null))changed|=FamilySet(ref kit.mapNavIcons,mapIcons);
            changed|=FamilySet(ref kit.groundDetails,new[]{"water-puddle-v14","lava-opening-v14"}.Select(name=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/GroundDetails/"+name+".png")).ToArray());
            changed|=Set(ref kit.waterRipple,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/Efeitos/water-ripple-v15.png"));
            changed|=Set(ref kit.waterRocks,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/Props/water-stones-v16.png"));
            changed|=FamilySet(ref kit.geyserFrames,new[]{"geyser-idle","geyser-rise","geyser-burst","geyser-fall"}.Select(part=>V3("LavaLand","thermal-geyser-v4",part)).ToArray());
            var navigation=new[]{"events","ranking","agency","teams","collection"}.Select(part=>V3("Shared","postal-navigation-cartoon-v2",part)??V3("Shared","postal-navigation-icons",part)).ToArray();
            if(navigation.All(s=>s!=null)&&(kit.navIcons==null||!kit.navIcons.SequenceEqual(navigation))){kit.navIcons=navigation;changed=true;}
            if (changed)
            {
                EditorUtility.SetDirty(kit);
                AssetDatabase.SaveAssets();
                if (log) Debug.Log("[MapaMundo3D] WorldMapKit atualizado: " + KitPath + $" (renderer {kit.rendererIndex}, pronto: {kit.Ready}).");
            }
            return kit;
        }

        [Serializable] sealed class GroundBoundsFile { public Vector2[] rows; }

        static bool Set<T>(ref T field, T value) where T : UnityEngine.Object
        {
            if (value == null || field == value) return false;
            field = value;
            return true;
        }

        static Shader LoadShader(string path) => AssetDatabase.LoadAssetAtPath<Shader>(path);

        static readonly string[] Sheets =
        {
            "dog-courier-poses-glow-spaced.png", "mailbox-poses.png", "small-postal-props-glow-spaced.png", "postal-region-landmarks-glow.png"
        };

        [Serializable] class SpriteManifest { public Sheet[] images; }
        [Serializable] class Sheet { public string path; public bool recommended; public SpritePart[] sprites; }
        [Serializable] class SpritePart { public string name; public SpriteRect rect; public Vector2 pivot; }
        [Serializable] class SpriteRect { public float x, y, width, height; }

        // Restaurar o contrato completo de recortes, nao apenas mudar Single -> Multiple.
        // Sem retangulos registrados no importer, o fallback antigo mostrava o atlas inteiro.
        static void EnsureSheetModes(bool log)
        {
            foreach (var file in new[] { "mapkit-v2-image-manifest.json", "mapkit-v3-image-manifest.json" })
                RestoreManifest(file, log);
        }

        static void RestoreManifest(string file, bool log)
        {
            var manifestPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Tools/level-map/" + file);
            if (!File.Exists(manifestPath)) return;
            var manifest = JsonUtility.FromJson<SpriteManifest>(File.ReadAllText(manifestPath));
            foreach (var sheet in manifest.images)
            {
                if (!sheet.recommended || sheet.sprites == null || sheet.sprites.Length == 0) continue;
                var importer = AssetImporter.GetAtPath(sheet.path) as TextureImporter;
                if (importer == null) continue;
                var multiple = sheet.sprites.Length > 1 || sheet.path.EndsWith("/green-bridge-deck.png") || sheet.path.EndsWith("/postal-house-cartoon-v2.png");
                var names = AssetDatabase.LoadAllAssetsAtPath(sheet.path).OfType<Sprite>().Select(s => s.name).ToArray();
                var settings = new TextureImporterSettings();
                var filteredPostman=sheet.path.EndsWith("/postman-map-v12.png");
                var meshType=sheet.path.Contains("thermal-geyser-v4")?SpriteMeshType.FullRect:SpriteMeshType.Tight;
                importer.ReadTextureSettings(settings);
                #pragma warning disable 0618
                var currentRects = importer.spritesheet;
                #pragma warning restore 0618
                var rectsCorrect = !multiple || sheet.sprites.All(s => currentRects.Any(r => r.name == s.name && r.rect == new Rect(s.rect.x, s.rect.y, s.rect.width, s.rect.height)));
                var correct = importer.textureType == TextureImporterType.Sprite &&
                    importer.textureShape == TextureImporterShape.Texture2D &&
                    importer.spriteImportMode == (multiple ? SpriteImportMode.Multiple : SpriteImportMode.Single) &&
                    settings.spriteMeshType == meshType &&
                    (!filteredPostman || (importer.mipmapEnabled && importer.filterMode==FilterMode.Trilinear && importer.mipMapsPreserveCoverage)) &&
                    rectsCorrect && (!multiple || sheet.sprites.All(s => names.Contains(s.name)));
                if (correct) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 4096;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                settings.spriteMeshType = meshType;
                settings.textureType = TextureImporterType.Sprite;
                importer.SetTextureSettings(settings);
                importer.textureShape=TextureImporterShape.Texture2D;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
                importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;
                if(filteredPostman)
                {
                    importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
                    importer.mipMapsPreserveCoverage=true;importer.alphaTestReferenceValue=.5f;
                }
                importer.spriteImportMode = multiple ? SpriteImportMode.Multiple : SpriteImportMode.Single;
                if (multiple)
                {
#pragma warning disable 0618
                    importer.spritesheet = sheet.sprites.Select(s => new SpriteMetaData
                    {
                        name = s.name,
                        rect = new Rect(s.rect.x, s.rect.y, s.rect.width, s.rect.height),
                        alignment = 9,
                        pivot = s.pivot
                    }).ToArray();
#pragma warning restore 0618
                }
                importer.SaveAndReimport();
                if (log) Debug.Log("[MapaMundo3D] recortes restaurados: " + sheet.path);
            }
        }

        static Sprite Slice(string file, string name)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(Sprites + file).OfType<Sprite>().ToArray();
            var named = all.FirstOrDefault(s => s.name == name);
            // Somente arquivos realmente individuais podem usar o unico sprite.
            if (named != null) return named;
            var single = !Sheets.Contains(file);
            return single && all.Length == 1 ? all[0] : null;
        }

        static Sprite V3(string folder, string family, string part)
        {
            var prefix = "Assets/RoyalAves/Art/MapKitV3/" + folder + "/" + family;
            var path = File.Exists(prefix + "-cartoon.png") ? prefix + "-cartoon.png" : prefix + ".png";
            var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            return sprites.FirstOrDefault(s=>s.name==(family+"_"+part).Replace('-','_'))??(sprites.Length==1?sprites[0]:null);
        }

        static void EnsureLayer(bool log)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;
            var manager = new SerializedObject(assets[0]);
            var layers = manager.FindProperty("layers");
            for (var i = 0; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == WorldMapKit.LayerName) return;
            for (var i = 11; i < layers.arraySize; i++)
            {
                var layer = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(layer.stringValue)) continue;
                layer.stringValue = WorldMapKit.LayerName;
                manager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                if (log) Debug.Log($"[MapaMundo3D] Layer '{WorldMapKit.LayerName}' criado no slot {i}.");
                return;
            }
            Debug.LogWarning("[MapaMundo3D] Sem slot livre de layer para 'WorldMap'.");
        }

        // Retorna o indice do renderer 3D na lista do URP Asset (cria o asset e adiciona se faltar).
        static int EnsureRenderer(bool log)
        {
            // O URP realmente em uso (Graphics/Quality settings), nao um asset avulso do template 2D.
            var urp = (QualitySettings.renderPipeline ?? GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
            if (urp == null) return -1;
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(data, RendererPath);
                if (log) Debug.Log("[MapaMundo3D] Criado " + RendererPath);
            }
            var post=AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            if(data.postProcessData!=post){data.postProcessData=post;EditorUtility.SetDirty(data);}
            var serialized = new SerializedObject(urp);
            var alpha=serialized.FindProperty("m_AllowPostProcessAlphaOutput");
            if(alpha!=null&&!alpha.boolValue){alpha.boolValue=true;serialized.ApplyModifiedProperties();EditorUtility.SetDirty(urp);AssetDatabase.SaveAssets();}
            var list = serialized.FindProperty("m_RendererDataList");
            for (var i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == data) return i;
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = data;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(urp);
            AssetDatabase.SaveAssets();
            if (log) Debug.Log("[MapaMundo3D] Renderer 3D adicionado ao URP Asset; Renderer2D padrao preservado.");
            return list.arraySize - 1;
        }
        static void EnsurePostProfile()
        {
            const string path="Assets/RoyalAves/Resources/RoyalAvesMapPostProfile.asset";
            if(AssetDatabase.LoadAssetAtPath<VolumeProfile>(path)!=null)return;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);
            var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.18f);bloom.threshold.Override(.95f);bloom.scatter.Override(.5f);
            var color=profile.Add<ColorAdjustments>(true);color.saturation.Override(7);color.contrast.Override(3);
            AssetDatabase.AddObjectToAsset(bloom,profile);AssetDatabase.AddObjectToAsset(color,profile);
            EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
        }
    }
}
