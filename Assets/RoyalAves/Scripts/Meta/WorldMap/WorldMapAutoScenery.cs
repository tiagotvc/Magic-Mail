using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Meta.WorldMap
{
    // Stable per-island composition. Objects reserve their complete visible footprint on the banks.
    public static class WorldMapAutoScenery
    {
        public static void Populate(List<PropPlacement> list,WorldMapRoute route,WorldMapKit kit,int index,int limit)
        {
            var map=route.Islands;var island=map.islands[index];var recipe=map.Recipe(island);
            if(recipe==null)return;
            int first=map.FirstLevel(index),last=Mathf.Min(limit,Mathf.Min(map.Capacity,first+map.levelsPerIsland-1));
            var start=route.MetersAt(first+.25f);var end=route.MetersAt(last-.45f);
            foreach(var gap in route.Gaps)if(gap.afterLevel>=first&&gap.afterLevel<=last)end=Mathf.Min(end,gap.start-.85f);
            int variant=island.sceneryVariant,ordinal=0;
            Sprite Pick(Sprite[] sprites,int n)=>sprites==null||sprites.Length==0?null:sprites[(n+variant)%sprites.Length];
            void Add(float progress,int side,Sprite sprite,PropKind kind,float size)
            {
                if(sprite==null||end<start)return;
                float width=WorldMapCards.Width(sprite,size);
                bool flat=kind==PropKind.GroundDetail;
                float lateral=flat?1.68f:kind==PropKind.Character?Mathf.Max(1.74f,WorldMapTerrain.PathHalf+width*.35f+.18f):Mathf.Max(1.88f,WorldMapTerrain.PathHalf+width*.32f+.12f);
                var p=new PropPlacement{id="auto-island-"+island.id+"-"+ordinal++,s=Mathf.Lerp(start,end,progress),offset=side*lateral,
                    sprite=sprite,kind=kind,height=size,seed=index*193+ordinal,shadow=!flat,bottomPivot=!flat,fixedPlacement=true,
                    groundPinned=!flat&&kind!=PropKind.Character,groundDepth=.025f,theme=recipe.christmas?WorldMapObjectTheme.Ice:WorldMapObjectTheme.None};
                // Deterministic clearance on each bank; move a candidate only within its own island.
                float origin=p.s;bool placed=false;
                for(int attempt=0;attempt<11;attempt++)
                {
                    p.s=Mathf.Clamp(origin+(attempt==0?0:((attempt+1)/2)*.32f*(attempt%2==0?-1:1)),start,end);
                    if(route.InGap(p.s,.65f)||route.IslandIndexAt(p.s)!=index)continue;
                    bool blocked=false;
                    foreach(var other in list)
                    {
                        if(Mathf.Sign(other.offset)!=side||route.IslandIndexAt(other.s)!=index)continue;
                        bool otherFlat=other.kind==PropKind.GroundDetail;
                        float clearance=flat||otherFlat
                            ?Mathf.Max(1.15f,(flat?size:other.height)+(flat?other.height:size)*.60f)
                            :Mathf.Max(1.45f,(size+other.height)*.67f);
                        if(Mathf.Abs(other.s-p.s)<clearance){blocked=true;break;}
                    }
                    if(!blocked){placed=true;break;}
                }
                if(placed)list.Add(p);
            }
            if(recipe.christmas)
            {
                Add(.13f,-1,Pick(recipe.buildings,0),PropKind.Landmark,recipe.buildingSize);
                Add(.49f,1,Pick(recipe.buildings,1),PropKind.Landmark,recipe.buildingSize*1.10f);
                Add(.84f,-1,Pick(recipe.buildings,2),PropKind.Landmark,recipe.buildingSize*.95f);
            }
            else
            {
                Add(.17f,-1,Pick(recipe.buildings,0),PropKind.Landmark,recipe.buildingSize);
                Add(.65f,1,Pick(recipe.buildings,1),PropKind.Landmark,recipe.buildingSize*1.04f);
            }
            // Story beats reserve their place before optional vegetation and ground accents.
            if(recipe.christmas)
            {
                for(int i=0;i<Mathf.Min(2,recipe.story.Length);i++)
                {
                    int before=list.Count;
                    var character=Pick(recipe.story,i);
                    bool fullCharacter=character!=null&&character==map.christmasVillainCharacter;
                    Add(i==0?.48f:.28f,i==0?-1:1,character,PropKind.Character,fullCharacter?map.villainMapSize:recipe.storySize);
                    if(list.Count==before)continue;
                    var actor=list[list.Count-1];actor.shadow=fullCharacter;
                    if(fullCharacter)actor.offset=Mathf.Sign(actor.offset)*Mathf.Max(1.55f,WorldMapTerrain.PathHalf+WorldMapCards.Width(character,actor.height)*.28f);
                    list[list.Count-1]=actor;
                    if(fullCharacter)continue;
                    var cover=recipe.storyCover!=null?recipe.storyCover:kit.shrubMint!=null?kit.shrubMint:kit.shrub;
                    if(cover!=null)list.Add(new PropPlacement{id=actor.id+"-cover",s=actor.s-.13f,offset=actor.offset,sprite=cover,kind=PropKind.Small,height=recipe.storyCoverSize,
                        seed=actor.seed+1000,shadow=true,bottomPivot=true,fixedPlacement=true,theme=WorldMapObjectTheme.Ice,groundPinned=true,groundDepth=.025f});
                }
            }
            // Give the large frontal stamp its own bank space before optional vegetation.
            int beforeMonument=list.Count,monumentOrdinal=ordinal;
            Add(recipe.christmas?.66f:.57f,-1,Pick(recipe.monuments,0),PropKind.Landmark,recipe.monumentSize);
            if(list.Count>beforeMonument){var monument=list[list.Count-1];monument.id="auto-island-"+island.id+"-monument";monument.offset=-1.65f;monument.theme=WorldMapObjectTheme.None;monument.groundDepth=.06f;list[list.Count-1]=monument;}
            ordinal=monumentOrdinal; // Keep existing tree/prop source IDs stable for saved overrides.
            float[] left=recipe.christmas?new[]{.33f,.66f}:new[]{.38f,.79f};
            float[] right=recipe.christmas?new[]{.04f,.72f,.96f}:new[]{.04f,.40f,.94f};
            int tree=0;
            foreach(var t in left)Add(t,-1,Pick(recipe.trees,tree++),PropKind.Tree,recipe.treeSize*(tree%2==0?1.06f:.95f));
            foreach(var t in right)Add(t,1,Pick(recipe.trees,tree++),PropKind.Tree,recipe.treeSize*(tree%2==0?1.06f:.95f));
            if(recipe.density>1.1f)Add(.57f,-1,Pick(recipe.trees,tree++),PropKind.Tree,recipe.treeSize*.83f);
            int groundCount=Mathf.Max(2,Mathf.RoundToInt(6*recipe.density*Mathf.Min(1,(last-first+1)/15f)));
            for(int i=0;i<groundCount;i++)Add(.08f+(i+.5f)/groundCount*.82f,i%2==0?-1:1,Pick(recipe.groundProps,i),PropKind.GroundDetail,recipe.propSize*(i%2==0?.9f:1.08f));
            // Low translucent ribbons enter from the edges. They neither displace scenery nor
            // consume its ordinals, keeping all existing automatic source IDs stable.
            for(int i=0;i<2;i++)
            {
                var sprite=Pick(recipe.mist,i);if(sprite==null||end<start)continue;
                float s=Mathf.Lerp(start,end,i==0?.30f:.76f);
                if(route.InGap(s,.3f))continue;
                float width=WorldMapCards.Width(sprite,recipe.mistSize);
                list.Add(new PropPlacement{id="auto-island-"+island.id+"-mist-"+i,s=s,offset=(i==0?1:-1)*(WorldMapTerrain.PathHalf+width*.5f+.22f),
                    sprite=sprite,kind=PropKind.Mist,height=recipe.mistSize,intensity=recipe.mistOpacity,seed=index*193+i,
                    bottomPivot=true,fixedPlacement=true,theme=WorldMapObjectTheme.None,flipX=i==1});
            }
        }
    }
}
