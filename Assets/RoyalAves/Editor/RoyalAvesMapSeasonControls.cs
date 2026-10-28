using System;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    internal static class RoyalAvesMapSeasonControls
    {
        internal const string AssetPath="Assets/RoyalAves/Resources/RoyalAvesSeasons.asset";
        internal static Season SaveActive(int previewMode)
        {
            var config=AssetDatabase.LoadAssetAtPath<SeasonConfig>(AssetPath);
            if(config==null)throw new InvalidOperationException("The season configuration is missing.");
            var kit=Resources.Load<WorldMapKit>(WorldMapKit.ResourcePath);
            Undo.RecordObject(config,"Save active season");
            var season=ApplyPreview(config,previewMode);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
            // Saved seasons, rather than an old preview flag, are the runtime source of truth.
            if(kit!=null&&(kit.christmasPreview||kit.seasonPreview!=0))
            {
                Undo.RecordObject(kit,"Use saved active season");kit.christmasPreview=false;kit.seasonPreview=0;
                EditorUtility.SetDirty(kit);AssetDatabase.SaveAssetIfDirty(kit);
            }
            return season;
        }
        internal static Season ApplyPreview(SeasonConfig config,int previewMode)
        {
            if(config==null)throw new ArgumentNullException(nameof(config));
            Season selected=config.Active;
            if(previewMode==2)
            {
                selected=config.seasons.Find(s=>s.id=="christmas"&&s.map?.snow==true)
                    ??config.seasons.Find(s=>s.map?.snow==true);
                if(selected==null)
                {
                    // Preserve the active season's art/layers when adding its Christmas variant.
                    selected=config.Active!=null?JsonUtility.FromJson<Season>(JsonUtility.ToJson(config.Active)):new Season();
                    selected.id=UniqueId(config,"christmas");selected.title="Christmas";
                    if(selected.map==null)selected.map=new MapSeasonLook();selected.map.snow=true;
                    config.seasons.Add(selected);
                }
            }
            else if(previewMode==1)
            {
                bool Plain(Season s)=>s.map==null||!s.map.snow&&s.map.groundMaterials==null&&(s.map.overlays==null||s.map.overlays.Count==0);
                selected=config.seasons.Find(s=>s.id=="padrao"&&Plain(s))??config.seasons.Find(Plain);
                if(selected==null){selected=new Season{id=UniqueId(config,"default"),title="Default"};config.seasons.Add(selected);}
            }
            if(selected==null){selected=new Season{id=UniqueId(config,"padrao")};config.seasons.Add(selected);}
            config.activeSeasonId=selected.id;
            return selected;
        }
        static string UniqueId(SeasonConfig config,string prefix)
        {
            string id=prefix;int suffix=2;while(config.seasons.Exists(s=>s.id==id))id=prefix+"-"+suffix++;
            return id;
        }
    }
}
