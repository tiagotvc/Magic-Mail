using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using RoyalAves.Meta.WorldMap;

public static class ArtV51Upgrade
{
    const string Manifest="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/art-v51-manifest.json";
    [Serializable] class Item {public string name,oldName,folder;}
    [Serializable] class Items {public Item[] items;}
    public static void Run()
    {
        try
        {
            var items=JsonUtility.FromJson<Items>("{\"items\":"+File.ReadAllText(Manifest)+"}").items;
            var mission=WorldMapMission100.Current;var layout=mission.layout;int changed=0;
            foreach(var a in items)
            {
                Sprite Load(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/"+a.folder+"/"+name+".png");
                var old=Load(a.oldName);var fresh=Load(a.name);if(old==null||fresh==null)throw new Exception("Missing art "+a.name);
                foreach(var e in layout.chapters.SelectMany(c=>c.entries).Where(e=>e.id.StartsWith("mission100-")&&e.sprite==old))
                {e.sprite=fresh;if(a.name=="organizer-owl-v51")e.height=1.20f;if(a.name=="inventor-turtle-v51")e.height=1.04f;if(a.name=="scribble-ink-v51")e.height=.68f;changed++;}
                foreach(var c in mission.characters.Where(c=>c.sprite==old))c.sprite=fresh;
            }
            WorldMapForearm Arm(Rect r,Rect held,Vector2 pivot,float angle,float speed,float phase=0)=>new WorldMapForearm{region=r,heldItem=held,elbow=pivot,degrees=angle,speed=speed,phase=phase};
            var owl=mission.characters[1];owl.height=1.20f;owl.facingLeft=true;owl.role="The Organizer — Owl Wizard";
            owl.forearms=new[]{Arm(new Rect(.028f,.31f,.255f,.42f),new Rect(.16f,.68f,.12f,.23f),new Vector2(.29f,.575f),3.5f,1.35f)};
            var turtle=mission.characters[2];turtle.height=1.04f;turtle.facingLeft=false;turtle.role="The Inventor — Elder Turtle Wizard";
            turtle.bodyBacking=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Rigs/TurtleV51/turtle-body-v51.png");
            turtle.forearmSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Rigs/TurtleV51/turtle-forearms-v51.png");
            if(turtle.bodyBacking==null||turtle.forearmSprite==null)throw new Exception("Turtle rig layers missing");
            turtle.forearms=new[]{Arm(new Rect(.02f,.25f,.43f,.45f),new Rect(),new Vector2(.36f,.434f),3.8f,1.20f),Arm(new Rect(.66f,.18f,.32f,.37f),new Rect(),new Vector2(.69f,.44f),3.1f,1.75f,1.2f)};
            turtle.forearms[0].sourceElbow=new Vector2(.385f,.446f);turtle.forearms[0].sourceScale=Vector2.one*.68f;
            turtle.forearms[1].sourceElbow=new Vector2(.87f,.445f);turtle.forearms[1].sourceScale=Vector2.one*.68f;
            var ink=mission.characters[3];ink.height=.68f;ink.facingLeft=true;ink.role="Scribble — Ink Sprite";
            ink.forearms=new[]{Arm(new Rect(.655f,.405f,.12f,.14f),new Rect(.73f,.38f,.17f,.23f),new Vector2(.67f,.43f),3.8f,.85f)};
            mission.characters[0].forearms[0].degrees=2.5f;
            EditorUtility.SetDirty(layout);EditorUtility.SetDirty(mission);AssetDatabase.SaveAssets();
            File.WriteAllText("C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/art-v51-upgrade.txt","Updated "+changed+" mission placements. Preserved placement ids, levels, lateral anchors, themes, effects, later chapters and original layout. Human courier + owl organizer + turtle inventor + ink sprite; per-joint timings and masks. Eight glossy building variants.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
