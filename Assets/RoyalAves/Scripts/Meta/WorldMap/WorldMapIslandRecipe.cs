using System;
using UnityEngine;

namespace RoyalAves.Meta.WorldMap
{
    [Serializable] public sealed class WorldMapIslandRecipe
    {
        public string title;
        public bool christmas;
        public WorldMapGroundStyle groundStyle;
        public WorldMapLiquid liquid;
        public Color skyTop=new Color32(28,111,182,255),skyBottom=new Color32(105,225,220,255);
        public Sprite[] trees=Array.Empty<Sprite>(),buildings=Array.Empty<Sprite>(),groundProps=Array.Empty<Sprite>(),story=Array.Empty<Sprite>();
        public Sprite[] monuments=Array.Empty<Sprite>();
        public Sprite[] mist=Array.Empty<Sprite>();
        [Range(.25f,1.2f)] public float mistSize=.65f;
        [Range(.05f,1)] public float mistOpacity=.68f;
        [Range(.8f,2)] public float monumentSize=1.3f;
        [Range(.8f,2.4f)] public float treeSize=1.48f,buildingSize=1.28f;
        [Range(.4f,1.4f)] public float propSize=.72f;
        [Range(.6f,1.5f)] public float density=1;
        public Sprite storyCover;
        [Range(.6f,1.6f)] public float storySize=.95f;
        [Range(.4f,1.2f)] public float storyCoverSize=.73f;

        // Shared editable recipes; never reconfigure island zero, which is hand-authored.
        public static void Configure(WorldMapIslands map,int index)
        {
            if(index<1||index>=map.islands.Count||map.recipes.Count<7)return;
            var island=map.islands[index];bool seasonal=index%3==2;
            int normal=index-index/3;
            island.recipeIndex=seasonal?5+(index/3)%2:normal%5;
            island.sceneryVariant=seasonal?index/6:normal/5;
            var recipe=map.recipes[island.recipeIndex];
            island.title=recipe.title+" "+(island.sceneryVariant+1);
            island.groundStyle=recipe.groundStyle;island.terrain=WorldMapTerrainTheme.GreenLand;
            island.liquid=recipe.liquid;island.skyTop=recipe.skyTop;island.skyBottom=recipe.skyBottom;island.background=null;
            island.customColors=island.customPathColor=island.customChannelColor=false;
            island.palette=0;island.channelWidth=seasonal?2.45f:2.1f;island.channelDepth=.18f;
            island.bankShape=(WorldMapBankShape)(index%3);island.waterMotion=WorldMapWaterMotion.Ripples;
            island.lakeLevels=!seasonal&&normal%3==1?3:0;
            island.crossing=island.lakeLevels>0?WorldMapCrossing.Stones:WorldMapCrossing.Planks;
            island.waterPlants=!seasonal&&recipe.liquid!=WorldMapLiquid.Lava;
            island.channelRocks=false;island.sideWaterfall=!seasonal&&island.lakeLevels>0;
            island.waterfallLateral=2.45f;island.waterfallSize=1.5f;island.waterfallOnRight=index%2==1;
            island.waterfallSprite=null;island.stones.Clear();island.plants.Clear();
            island.weather=seasonal?WorldMapWeather.Snow:WorldMapWeather.Clear;
            island.weatherIntensity=seasonal?.25f:0;island.meteors=false;
        }
    }
}
