using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RoyalAves.Meta.WorldMap;

public static class EntranceV54Bake
{
    public static void Run()
    {
        try
        {
            var mission=WorldMapMission100.Current;var layout=mission.layout;var route=new WorldMapRoute(Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath));
            if(mission.polishedEntrance)throw new Exception("Entrance already composed; edit existing anchors instead of rebuilding.");
            var old=layout.chapters.SelectMany(c=>c.entries).ToArray();var later=old.Where(e=>e.level>20).Select(e=>JsonUtility.ToJson(e)).ToArray();
            WorldMapLayout.Entry Find(string id)=>old.Single(e=>e.id==id);
            void Place(string id,float level,float worldX,float height)
            {var e=Find(id);e.level=level;e.lateral=worldX-route.CenterX(route.MetersAt(level));e.height=height;e.customSize=true;e.groundPinned=true;e.groundDepth=.022f;}
            var lamp=Find("mission100-lamp-3");
            // Alternating silhouettes, different heights and clear space around every object.
            // Keep all landmark IDs and their independent animation pieces.
            Place("mission100-welcome-sign",.8f,-1.42f,.48f);
            Place("mission100-signature-mailbox",2.0f,1.65f,.80f);
            Place("magic53-envelope-tree",3.9f,-1.85f,1.58f);
            Place("mission100-meet-courier",5.3f,1.30f,1.28f);
            Place("mission100-lamp-3",8.4f,2.10f,.98f);
            Place("mission100-entry-gate",10.5f,-1.60f,1.20f);
            Place("work52-garden-planter",12.2f,1.65f,.42f);
            Place("magic53-ribbon-arch",13.8f,-1.72f,.88f);
            Place("work52-enchanted-watering-can",15.2f,1.43f,.46f);
            Place("magic53-seal-flowers",16.6f,-1.90f,.34f);
            Place("mission100-entry-bench",18.2f,1.78f,.44f);
            int removed=0;foreach(var c in layout.chapters)removed+=c.entries.RemoveAll(e=>e.level<=20&&e.id!="mission100-lamp-3"&&(e.id.StartsWith("mission100-tree-")||e.id.StartsWith("mission100-shrub-")||e.id.StartsWith("mission100-mailbox-")||e.id.StartsWith("mission100-lamp-")||e.id=="work52-arrival-bell"||e.id=="work52-hours-board"));
            Sprite Art(string key)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/Props/"+key+".png")??throw new Exception("Missing "+key);
            void Hedge(string id,float level,float x,float height)
            {var chapter=layout.Find(route.ChapterAtMeters(route.MetersAt(level)));chapter.entries.Add(new WorldMapLayout.Entry{id="entrance54-"+id,sprite=Art("shrub-green-glossy-v29"),level=level,lateral=x-route.CenterX(route.MetersAt(level)),height=height,customSize=true,keepSprite=true,kind=PropKind.Small,shadow=true,groundPinned=true,groundDepth=.035f,theme=WorldMapObjectTheme.Season});}
            Hedge("separate-garden-shrub",7.0f,-1.93f,.62f);
            foreach(var id in new[]{"entrance-tree-0","entrance-tree-1"})if(!layout.hiddenExtras.Contains(id))layout.hiddenExtras.Add(id);
            foreach(var c in layout.chapters)c.entries.Sort((a,b)=>a.level.CompareTo(b.level));
            var after=layout.chapters.SelectMany(c=>c.entries).Where(e=>e.level>20).Select(e=>JsonUtility.ToJson(e)).ToArray();if(!later.OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(after.OrderBy(x=>x,StringComparer.Ordinal)))throw new Exception("Composition changed later campaign data");
            mission.polishedEntrance=true;EditorUtility.SetDirty(mission);EditorUtility.SetDirty(layout);AssetDatabase.SaveAssets();
            File.WriteAllText("C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/entrance-v54-bake.txt","Recomposed opening with separate alternating objects, larger envelope tree and mailbox; distinct heights and no repeated level anchors; removed "+removed+" scattered defaults and two small sign/bell props from the composition (art retained in library). All later-than-20 entries retained. Compact satin HUD and distinct parchment road enabled. Scene, original layout, island settings and season not saved.");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
