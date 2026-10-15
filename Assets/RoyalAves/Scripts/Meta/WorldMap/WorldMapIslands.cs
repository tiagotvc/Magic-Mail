using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Meta.WorldMap
{
    public enum WorldMapWeather { Clear, Wind, Rain, Storm, Snow, Embers }
    public enum WorldMapGroundStyle { Green, Lavender, Volcanic, Lagoon, Sand, Ice, Rose, Crystal }
    public enum WorldMapBankShape { Rounded, Angular, Scalloped }
    public enum WorldMapCrossing { Logs, Planks, Stones, OpenWater }
    public enum WorldMapWaterMotion { Wakes, Ripples, Calm }

    // One Resource is the source of truth for both the editor and the shipped map.
    [CreateAssetMenu(fileName="RoyalAvesWorldMapIslands", menuName="Royal Aves/Ilhas do mapa")]
    public sealed class WorldMapIslands : ScriptableObject
    {
        public const string ResourcePath="RoyalAvesWorldMapIslands";
        public bool unifiedBase=true;
        [Min(20)] public int levelsPerIsland=20;
        [Range(.6f,2.2f)] public float lateralCurvature=1.45f;
        public bool postProcessing=true;
        [Range(0,.7f)] public float bloom=.18f;
        [Range(0,20)] public float vibrance=7;
        public int formatVersion=2;
        [Serializable] public sealed class Island
        {
            public string id=Guid.NewGuid().ToString("N");
            public string title="Nova ilha";
            [HideInInspector] public int levelSlots=20; // retained for migration; uniform size is owned by the map
            public WorldMapTerrainTheme terrain;
            public WorldMapGroundStyle groundStyle;
            [Range(.9f,3.5f)] public float channelWidth=1.9f;
            [Range(.08f,.55f)] public float channelDepth=.23f;
            public WorldMapBankShape bankShape;
            public WorldMapCrossing crossing;
            public WorldMapWaterMotion waterMotion;
            public bool channelRocks;
            public bool customChannelColor;
            public Color channelColor=new Color32(35,175,225,255);
            [Range(0,9)] public int palette;
            public bool customColors;
            public Color ground=new Color32(104,183,138,255), path=new Color32(185,230,158,255), water=new Color32(73,191,209,255);
            [Tooltip("Atlas de chão com quatro quadrantes no padrão dos materiais do mapa. Vazio usa o chão do tema.")]
            public Sprite groundMaterials;
            [Tooltip("Fundo independente do chão. Vazio usa o gradiente do tema.")]
            public Sprite background;
            public Color skyTop=new Color32(255,210,88,255), skyBottom=new Color32(255,235,123,255);
            public Sprite cloudSmall, cloudWide;
            [HideInInspector] public bool volcanoes;
            public bool meteors;
            public WorldMapWeather weather;
            [Range(0,1)] public float weatherIntensity=1;

            public MapPalette Palette
            {
                get
                {
                    var p=ThemePalette(terrain,palette);
                    if(customColors)
                    {
                        p.outerA=ground;p.outerB=ground*.94f;p.pathA=path;p.pathB=path*.94f;
                        p.water=water;p.rim=Color.Lerp(ground,Color.white,.25f);p.face=ground*.72f;
                    }
                    return p;
                }
            }
        }
        public static MapPalette IllustratedPalette(Island island)
        {
            var p=WorldMapRoute.BasePalette(island.palette==1);
            string lawn=null,lane=null,liquid=null;
            switch(island.groundStyle)
            {
                case WorldMapGroundStyle.Lavender:lawn="#6952C2";lane="#BA96F2";liquid="#32BADD";break;
                case WorldMapGroundStyle.Volcanic:lawn="#823D70";lane="#FFC066";liquid="#F69524";break;
                case WorldMapGroundStyle.Lagoon:lawn="#169DAC";lane="#84DFD7";liquid="#258DD4";break;
                case WorldMapGroundStyle.Sand:lawn="#D79B36";lane="#FFE2A1";liquid="#27B8D5";break;
                case WorldMapGroundStyle.Ice:lawn="#489DD4";lane="#BDEFF6";liquid="#4776CA";break;
                case WorldMapGroundStyle.Rose:lawn="#B94F91";lane="#FFC2DC";liquid="#57B5D3";break;
                case WorldMapGroundStyle.Crystal:lawn="#5845AF";lane="#B4A0EE";liquid="#38BBD4";break;
            }
            Color C(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}
            if(lawn!=null){p.outerA=C(lawn);p.pathA=C(lane);p.water=C(liquid);}
            if(island.customColors){p.outerA=island.ground;p.pathA=island.path;p.water=island.water;}
            if(lawn!=null||island.customColors)
            {
                p.outerB=p.outerA*.94f;p.pathB=p.pathA*.94f;
                p.rim=Color.Lerp(p.outerA,Color.white,.24f);p.face=p.outerA*.62f;
                p.haze=Color.Lerp(p.outerA,Color.white,.82f);
            }
            if(island.customChannelColor)p.water=island.channelColor;
            return p;
        }
        public List<Island> islands=new List<Island>();
        public int Capacity=>islands.Count*Mathf.Max(20,levelsPerIsland);
        public int FirstLevel(int index)
        =>Mathf.Clamp(index,0,islands.Count)*Mathf.Max(20,levelsPerIsland)+1;
        public int IndexAtLevel(float level)
        {
            return Mathf.Clamp(Mathf.FloorToInt((level-1)/Mathf.Max(20,levelsPerIsland)),0,Mathf.Max(0,islands.Count-1));
        }
        public Island AtLevel(float level)=>islands.Count==0?null:islands[IndexAtLevel(level)];
        public Island Append()
        {var item=DefaultIsland(islands.Count);item.levelSlots=levelsPerIsland;if(unifiedBase){item.title="Ilha "+(islands.Count+1);item.terrain=WorldMapTerrainTheme.GreenLand;item.palette=islands.Count%3==1?1:0;item.weather=WorldMapWeather.Clear;item.meteors=false;item.skyTop=new Color32(255,205,74,255);item.skyBottom=new Color32(255,237,145,255);}islands.Add(item);return item;}
        public static Island DefaultIsland(int index)
        {
            var themes=new[]{WorldMapTerrainTheme.GreenLand,WorldMapTerrainTheme.ForestLand,WorldMapTerrainTheme.CoastLand,WorldMapTerrainTheme.AuroraLand,WorldMapTerrainTheme.LavaLand,WorldMapTerrainTheme.DesertLand,WorldMapTerrainTheme.OrchardLand,WorldMapTerrainTheme.MarshLand,WorldMapTerrainTheme.CloudLand,WorldMapTerrainTheme.CrystalLand};
            var names=new[]{"Jardim das Cartas","Bosque Encantado","Recife das Pérolas","Serra da Aurora","Vale Termal","Dunas do Sol","Pomar das Entregas","Lagoa dos Vagalumes","Jardim das Nuvens","Gruta dos Cristais"};
            var n=index%10;var item=new Island{title=names[n],terrain=themes[n],palette=n,meteors=n==4,
                weather=n==3?WorldMapWeather.Snow:n==4?WorldMapWeather.Embers:n==1?WorldMapWeather.Wind:WorldMapWeather.Clear};
            Color C(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}
            item.skyTop=C(new[]{"#FFC94A","#6450B8","#07539D","#3741A7","#FF9845","#FFB64A","#EF934D","#164E76","#54B9EF","#242363"}[n]);
            item.skyBottom=C(new[]{"#FFE879","#DDA9F2","#31CBCD","#B9E8F6","#FFD677","#FFE398","#FFE59E","#63CEBE","#D3F7FF","#9B6CE1"}[n]);return item;
        }
        public static MapPalette ThemePalette(WorldMapTerrainTheme theme,int palette=0)
        {
            var p=theme==WorldMapTerrainTheme.LavaLand?WorldMapRoute.LavaPalette:WorldMapRoute.PaletteOfChapter(palette);
            string outer=null,path=null,water=null;
            switch(theme)
            {
                case WorldMapTerrainTheme.ForestLand:outer="#55358F";path="#BA91EF";water="#3978CA";break;
                case WorldMapTerrainTheme.CoastLand:outer="#087F9F";path="#71D0C5";water="#176EC2";break;
                case WorldMapTerrainTheme.AuroraLand:outer="#317ABD";path="#A8DEEF";water="#535FD1";break;
                case WorldMapTerrainTheme.DesertLand:outer="#C57C2E";path="#FFD779";water="#24A9C8";break;
                case WorldMapTerrainTheme.OrchardLand:outer="#A44743";path="#FFCF88";water="#3AB9BA";break;
                case WorldMapTerrainTheme.MarshLand:outer="#176E75";path="#78C8B3";water="#2461A0";break;
                case WorldMapTerrainTheme.CloudLand:outer="#638ACD";path="#C7EFF4";water="#8294E1";break;
                case WorldMapTerrainTheme.CrystalLand:outer="#403280";path="#AA9AEB";water="#22A8C9";break;
            }
            if(outer!=null)
            {
                Color C(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}
                p.outerA=C(outer);p.outerB=p.outerA*.94f;p.pathA=C(path);p.pathB=p.pathA*.94f;
                p.rim=Color.Lerp(p.outerA,Color.white,.22f);p.face=p.outerA*.64f;p.water=C(water);p.haze=Color.Lerp(p.outerA,Color.white,.8f);
            }
            return p;
        }
    }

    public static class WorldMapLevelCatalog
    {
        static HashSet<int> ids;
        public static int Count { get { Ensure();return ids.Count; } }
        static int lastLevel;
        public static int LastLevel { get { Ensure();return lastLevel; } }
        public static bool Exists(int number){Ensure();return ids.Contains(number);}
        public static int NextExisting(int number)
        {Ensure();var next=Mathf.Max(1,number);while(next<=lastLevel&&!ids.Contains(next))next++;return next;}
        static void Ensure(){if(ids==null)Refresh();}
        public static void Refresh()
        {
            ids=new HashSet<int>();lastLevel=0;
            foreach(var asset in Resources.LoadAll<ScriptableObject>("Levels"))
                if(asset.name.StartsWith("Level_",StringComparison.Ordinal)&&int.TryParse(asset.name.Substring(6),out var n)&&n>0)
                {ids.Add(n);lastLevel=Mathf.Max(lastLevel,n);}
        }
    }
}
