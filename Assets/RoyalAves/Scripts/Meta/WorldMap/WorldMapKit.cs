using UnityEngine;

namespace RoyalAves.Meta.WorldMap
{
    public enum WorldMapTerrainTheme { GreenLand, LavaLand, ForestLand, CoastLand, AuroraLand, DesertLand, OrchardLand, MarshLand, CloudLand, CrystalLand }
    // Referencias de arte/shaders e parametros de camera do mapa 3D. Fica em Resources para ser incluido no build.
    // Preenchido por RoyalAvesWorldMapSetup (menu Royal Aves/Mapa 3D).
    public sealed class WorldMapKit : ScriptableObject
    {
        public const string ResourcePath = "RoyalAvesWorldMapKit";
        public const string LayerName = "WorldMap";

        [Header("Shaders (URP Unlit)")]
        public Shader surface;
        public Shader card;
        public Shader shadow;
        public Shader water;
        public Shader glow;
        public Shader windowGlow;
        public Shader beam;
        public Shader sparkle;
        public Shader vapor;
        public Shader navigationSurface;
        public Texture2D navigationPaint,navigationSelectedPaint;
        public Vector4 navigationPaintRect=new Vector4(.01105481345f,.27486187845f,.97835099033f,.49033149171f);
        public Vector4 navigationSelectedPaintRect=new Vector4(.02303086135f,.26243093923f,.95485951175f,.49309392265f);
        [Header("HUD superior: fundo, contador, retrato, coração, carta, configurações")]
        public Texture2D[] topHudArt=new Texture2D[0];
        public Vector4[] topHudRects=new Vector4[0];
        [Header("Evento surpresa de Natal")]
        public bool santaEventEnabled=true;
        public Sprite santaFriendly,santaVillain;
        [Min(1)] public float santaFirstDelay=4;
        [Min(1)] public float santaVisibleSeconds=4;
        [Min(2)] public float santaInterval=10;
        [TextArea] public string santaFriendlyMessage="Você encontrou o Papai Noel! Evento especial de Natal em breve…";
        [TextArea] public string santaVillainMessage="Ho, ho, ho… Você encontrou o Noel do mal! Um desafio especial vem aí!";

        [Tooltip("Indice do Universal Renderer 3D na lista do URP Asset. -1 = nao configurado.")]
        public int rendererIndex = -1;

        [Header("Camera e curva")]
        [Range(12, 400)] public float radius = 26;
        [Range(1.5f, 12)] public float cameraHeight = 4.9f;
        [Range(2, 20)] public float cameraBack = 4.4f;
        [Range(0, 40)] public float pitch = 32;
        [Range(20, 60)] public float fov = 48;
        [Tooltip("Metros a frente do cursor onde o medalhao da fase atual fica.")]
        [Range(-4, 6)] public float focusAhead = -2.55f;
        [Range(0.5f, 2)] public float renderScale = 1;
        [Range(0.3f, 2.5f)] public float tokenScale = 0.62f;
        [Header("Dia e noite")]
        [Range(0, 1), Tooltip("0 = dia, 1 = noite. Escurece o mundo, troca sol por lua e acende luzes.")] public float night;
        public bool dayNightCycle=true;
        [Range(.5f,30)] public float dayNightCycleMinutes=4;
        public static float NightAt(float seconds,float minutes)
        {
            var phase=Mathf.Repeat(seconds/Mathf.Max(30,minutes*60),1);
            return Mathf.SmoothStep(0,1,Mathf.InverseLerp(.20f,.80f,.5f-.5f*Mathf.Cos(phase*Mathf.PI*2)));
        }
        public bool illustratedProjection = true;
        public float illustratedSize = 4.0f;
        [Tooltip("Usado pelo setup do editor para migrar valores ajustados em versoes anteriores.")]
        public int tuningVersion;

        [Header("Sprites do MapKitV2")]
        public Sprite dogIdle, dogWag, dogSniff, dogCarry;
        public Sprite mailboxClosed, mailboxOpen, lamp, tree;
        public Sprite parcels, flowers, sign, shrub;
        public Sprite boat, windmill, lighthouse, hut, clock, agency, tunnel, kite;

        [Header("Sprites anteriores (Art/Maps)")]
        public Sprite pine, postOffice, factory;

        [Header("MapKitV3: sprites independentes")]
        public Sprite cloudSmall, cloudWide, cloudStrip;
        public Sprite envelopeCream, envelopePurple, envelopeTeal;
        public Sprite factoryBody, factoryChimney, factoryChute;
        public Sprite fountain, bench, topiary, parcelCart, postalGate;
        public Sprite skyClear, skyWarm, skyOvercast, skyLilac;
        public Sprite snowPineUpper, snowPineMiddle, snowPineLower, snowRound;
        public Sprite snowMailbox, snowLamp, snowRoof;
        [Header("Coberturas de neve pintadas por asset")]
        public WorldMapSnowOverlay[] paintedSnow=new WorldMapSnowOverlay[0];
        [Header("Luzes e gelo de Natal")]
        public Sprite[] christmasBulbs=new Sprite[0];
        public Sprite christmasIcicles;
        public WorldMapTreeDecorationProfile[] treeDecorationProfiles=new WorldMapTreeDecorationProfile[0];
        public Sprite groundPaint, groundIllustration, levelCrown;
        public Texture2D groundBrushLawn,groundBrushPath;
        public Sprite[] expressiveTrees=new Sprite[0];
        public Sprite expressivePostOffice,expressiveWorkshop;
        public GameObject mapUiTemplate;
        public Sprite[] mapNavIcons=new Sprite[0];
        public Sprite legacyPostOffice, bridgeDeck, skyGoldenOnly;
        public Sprite groundLavaIllustration, warmCloudSmall, warmCloudWide, lavaCloudSmall, lavaCloudWide;
        public Sprite volcanoSingle, volcanoTwin, volcanoCaldera;
        public WorldMapTerrainTheme terrainTheme;
        public Vector2[] groundLavaLaneBounds = new Vector2[0];
        public bool Lava => terrainTheme==WorldMapTerrainTheme.LavaLand;
        public Sprite GroundArt => Lava&&groundLavaIllustration!=null?groundLavaIllustration:groundIllustration;
        public Vector2[] GroundBounds => Lava&&groundLavaLaneBounds.Length==64?groundLavaLaneBounds:groundLaneBounds;
        public Vector2[] groundLaneBounds = new Vector2[0];
        public Sprite levelFaceActive,levelFaceCompleted,levelFaceUpcoming;
        public Sprite terrainMaterials,bridgeTimber,bridgeLogs;
        public Sprite autumnTree,postalPine,mailWindmill,mailKiosk,bellFlowers,curvedLantern;
        public Sprite lavaTree,lavaShrub,lavaFlower,lavaLamp,lavaMailbox,lavaMailboxOpen,lavaForge,lavaOffice,lavaObservatory,lavaCrystal,lavaBoulder;
        public Sprite[] navIcons=new Sprite[0];
        public Sprite hardBadge,expressBadge,levelStar;
        public Sprite shrubCoral,shrubGold,shrubMint,detailTulips,detailBells,detailRocks;
        public Sprite[] gardenFamily=new Sprite[0],forestFamily=new Sprite[0],coastFamily=new Sprite[0],auroraFamily=new Sprite[0],geyserFrames=new Sprite[0];
        public Sprite[] desertFamily=new Sprite[0],orchardFamily=new Sprite[0],marshFamily=new Sprite[0],cloudFamily=new Sprite[0],crystalFamily=new Sprite[0];
        public Sprite geyserBase,geyserWater,geyserSteam,meteorSprite;
        public Sprite snowTerrainMaterials;
        public Sprite[] simpleNature=new Sprite[0],simplePostal=new Sprite[0];
        public Sprite simpleBridge,simpleCrown,simpleStar,simpleHard,simpleExpress;
        public Sprite simpleCloudSmall,simpleCloudWide;
        public Sprite[] postalTrees=new Sprite[0],postalTreeDetails=new Sprite[0];
        public Sprite[] entranceTrees=new Sprite[0];
        public Shader treeLeafShader;
        public Sprite postmanSprite;
        public Sprite[] groundDetails=new Sprite[0];
        public Sprite waterRipple;
        public Sprite waterRocks;
        [System.NonSerialized] public bool simpleBase;
        [System.NonSerialized] public int simplePalette;
        [System.NonSerialized] public bool customGround;
        [System.NonSerialized] public bool simpleGroundVariant;
        [System.NonSerialized] public Color simpleLawnColor,simplePathColor;
        [System.NonSerialized] public bool simpleGreenGradient,simplePathOverride;
        [System.NonSerialized] public Vector2 simpleIslandSpan;
        public Sprite[] Family => terrainTheme==WorldMapTerrainTheme.ForestLand?forestFamily:terrainTheme==WorldMapTerrainTheme.CoastLand?coastFamily:terrainTheme==WorldMapTerrainTheme.AuroraLand?auroraFamily:terrainTheme==WorldMapTerrainTheme.DesertLand?desertFamily:terrainTheme==WorldMapTerrainTheme.OrchardLand?orchardFamily:terrainTheme==WorldMapTerrainTheme.MarshLand?marshFamily:terrainTheme==WorldMapTerrainTheme.CloudLand?cloudFamily:terrainTheme==WorldMapTerrainTheme.CrystalLand?crystalFamily:terrainTheme==WorldMapTerrainTheme.GreenLand?gardenFamily:null;
        [HideInInspector] public int seasonPreview;
        public bool christmasPreview;
        public bool Christmas => seasonPreview==1?false:seasonPreview==2||christmasPreview||SeasonLook?.snow==true;
        public RoyalAves.Meta.MapSeasonLook SeasonLook => RoyalAves.Meta.SeasonConfig.Instance?.Active?.map;

        public bool Ready => surface != null && card != null && shadow != null && water != null && rendererIndex >= 0;
    }

    [System.Serializable]
    public sealed class WorldMapTreeDecorationProfile
    {
        public Sprite source;
        public Vector2 leftIce,rightIce;
    }

    [System.Serializable]
    public sealed class WorldMapSnowOverlay
    {
        public Sprite source;
        [Tooltip("Camada transparente no mesmo enquadramento da imagem original.")]
        public Sprite overlay;
    }
}
