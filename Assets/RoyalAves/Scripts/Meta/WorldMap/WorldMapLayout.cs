using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Meta.WorldMap
{
    // Resource data shared by the editor preview and the shipped map; no editor-only scene objects.
    public sealed class WorldMapLayout : ScriptableObject
    {
        public const string ResourcePath = "RoyalAvesWorldMapLayout";
        public const string SimpleResourcePath="RoyalAvesWorldMapSimpleLayout";
        public static bool IsExtra(string id)=>id!=null&&(id.StartsWith("thermal-")||id.StartsWith("entrance-")||id.StartsWith("river-"));
        public static WorldMapLayout For(WorldMapKit kit)=>kit.simpleBase&&WorldMapMission100.Active?WorldMapMission100.Current.layout:Resources.Load<WorldMapLayout>(kit.simpleBase?SimpleResourcePath:ResourcePath);
        [Serializable] public sealed class Entry
        {
            public string id;
            public string sourceId;
            public string caption;
            public WorldMapWorkMotion workMotion;
            public float level, lateral, height = 1;
            public bool customSize;
            public bool keepSprite;
            public Sprite sprite, alternate;
            public PropKind kind;
            public int seed;
            public int supportedLevel;
            public bool shadow = true, bottomPivot;
            public bool flipX;
            public WorldMapObjectTheme theme;
            public WorldMapEffectPose snowEffect,lightsEffect,iceEffect;
            public WorldMapPiecePose[] effectPieces;
            public bool customAdornments;
            public WorldMapAdornment[] adornments;
            public bool groundPinned;
            [Range(0,.30f)] public float groundDepth=.06f;
            [Range(-80,80)] public float rotation=50;     // LightShaft
            [Range(.05f,1)] public float intensity=.45f;  // LightShaft
            [Range(.2f,4)] public float thickness=1;      // LightShaft: espessura (largura) do raio
            [Range(0,1)] public float raySpacing=.5f;
            public Sprite EffectiveSprite(WorldMapKit kit)
            {
                var art=WorldMapScenery.UpgradeStyle(sprite==kit.legacyPostOffice&&kit.postOffice!=null?kit.postOffice:sprite,kit);
                if(sourceId!=null&&sourceId.StartsWith("entrance-tree-")&&kit.entranceTrees.Length==2&&
                    int.TryParse(sourceId.Substring("entrance-tree-".Length),out var side)&&side>=0&&side<2&&kit.entranceTrees[side]!=null&&
                    kit.postalTrees.Length==3&&sprite==kit.postalTrees[side==0?0:2])
                    art=WorldMapScenery.UpgradeStyle(kit.entranceTrees[side],kit);
                return keepSprite?art:WorldMapScenery.VariedSprite(art,kind,Mathf.RoundToInt(level),kit);
            }
            public float EffectiveHeight(WorldMapKit kit)=>customSize
                ?height*(kit.simpleBase&&kit.postmanSprite!=null&&kit.simplePostal.Length==6&&sprite==kit.simplePostal[3]?1.45f:1)
                :WorldMapPropSizing.Height(EffectiveSprite(kit),kind,kit);
            public PropPlacement Placement(WorldMapRoute route, WorldMapKit kit) => new PropPlacement
            {
                id=id,caption=caption,workMotion=workMotion,s=route.MetersAt(level),offset=lateral,height=EffectiveHeight(kit),sprite=EffectiveSprite(kit),alt=alternate,
                kind=kind,seed=seed,supportedLevel=supportedLevel,shadow=shadow,bottomPivot=bottomPivot,flipX=flipX,theme=theme,snowEffect=snowEffect,lightsEffect=lightsEffect,iceEffect=iceEffect,effectPieces=effectPieces==null?null:(WorldMapPiecePose[])effectPieces.Clone(),customAdornments=customAdornments,adornments=WorldMapAdornment.Copy(adornments),groundPinned=groundPinned,groundDepth=groundDepth,rotation=rotation,intensity=intensity,thickness=thickness,raySpacing=raySpacing,fixedPlacement=customSize||keepSprite
            };
            public static Entry From(PropPlacement p, WorldMapRoute route) => new Entry
            {
                id=Guid.NewGuid().ToString("N"),caption=p.caption,workMotion=p.workMotion,sourceId=IsExtra(p.id)?p.id:null,level=route.LevelAt(p.s),lateral=p.offset,height=p.height,
                customSize=true,keepSprite=true,sprite=p.sprite,alternate=p.alt,kind=p.kind,seed=p.seed,supportedLevel=p.supportedLevel,shadow=p.shadow,bottomPivot=p.bottomPivot,flipX=p.flipX,theme=p.theme,snowEffect=p.snowEffect,lightsEffect=p.lightsEffect,iceEffect=p.iceEffect,effectPieces=p.effectPieces==null?null:(WorldMapPiecePose[])p.effectPieces.Clone(),customAdornments=p.customAdornments,adornments=WorldMapAdornment.Copy(p.adornments),groundPinned=p.groundPinned,groundDepth=p.groundDepth,rotation=p.rotation>0||p.rotation<0?p.rotation:50,intensity=p.intensity>0?p.intensity:.35f,thickness=p.thickness>0?p.thickness:1,raySpacing=p.raySpacing
            };
        }
        [Serializable] public sealed class Chapter
        {
            public int index;
            public bool manual;
            public List<Entry> entries = new List<Entry>();
        }
        public List<Chapter> chapters = new List<Chapter>();
        public List<string> hiddenExtras=new List<string>();
        public bool ExtraClaimed(string id)=>hiddenExtras.Contains(id)||chapters.Exists(c=>c.entries.Exists(e=>e.sourceId==id));
        public Chapter Find(int index) => chapters.Find(x => x.index == index);

        public void Apply(List<PropPlacement> placements, WorldMapRoute route, WorldMapKit kit,Func<float,WorldMapKit> kitAt=null)
        {
            placements.RemoveAll(p => Find(route.ChapterAtMeters(p.s))?.manual == true);
            foreach (var chapter in chapters)
                if (chapter.manual)
                    foreach (var entry in chapter.entries)
                        if (entry.level<=route.LevelSlots&&entry.sprite != null && (entry.height > .01f||!entry.customSize)) placements.Add(entry.Placement(route,kitAt?.Invoke(route.MetersAt(entry.level))??kit));
            placements.Sort((a,b)=>a.s.CompareTo(b.s));
        }
    }
}

