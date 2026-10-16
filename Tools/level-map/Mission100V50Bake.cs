using System;
using System.IO;
using System.Linq;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

public static class Mission100V50Bake
{
    const string Root="Assets/RoyalAves/Resources/";
    static Sprite Art(string name,string folder="Props")=>Resources.Load<WorldMapLibraryItem>("MapLibrary/"+folder+"/"+name)?.sprite??AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/"+folder+"/"+name+".png");
    public static void Frame(WorldMapLayout layout,WorldMapRoute route)
    {
        var all=layout.chapters.SelectMany(c=>c.entries).Where(e=>e.id.StartsWith("mission100-")).ToArray();
        bool Decoration(WorldMapLayout.Entry e)=>new[]{"tree-","shrub-","mailbox-","lamp-"}.Any(p=>e.id.StartsWith("mission100-"+p));
        var important=all.Where(e=>!Decoration(e)).ToArray();
        foreach(var e in important)
        {
            float side=Mathf.Sign(e.lateral);
            if(e.kind==PropKind.Character||e.kind==PropKind.Landmark||e.id.Contains("wagon")||e.id.Contains("parcel-pile"))e.lateral=side*1.42f-route.CenterX(route.MetersAt(e.level));
        }
        // Keep the few large shapes around each landmark clear, rather than hiding the cast.
        foreach(var e in all.Where(Decoration))
        {
            int number=int.Parse(e.id.Substring(e.id.LastIndexOf('-')+1));float side=(number/3)%2==0?-1:1;
            e.lateral=e.id.StartsWith("mission100-tree-")?side*2.10f:e.id.StartsWith("mission100-lamp-")?side*1.60f:e.id.StartsWith("mission100-mailbox-")?-side*1.62f:-side*1.65f;
            if(e.id=="mission100-tree-99"){e.level=97.0f;e.lateral=2.05f;continue;}
            var nearby=important.FirstOrDefault(p=>(p.kind==PropKind.Character||p.kind==PropKind.Landmark||p.id.Contains("wagon")||p.id.Contains("parcel-pile"))&&Mathf.Sign(e.lateral)==Mathf.Sign(p.lateral)&&Mathf.Abs(e.level-p.level)<1.25f);
            if(nearby!=null)e.lateral=-Mathf.Sign(e.lateral)*Mathf.Abs(e.lateral);
        }
    }
    public static void Run()
    {
        try
        {
            var original=Resources.Load<WorldMapLayout>(WorldMapLayout.SimpleResourcePath);var kit=Resources.Load<WorldMapKit>("RoyalAvesWorldMapKit");var islands=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);
            var layout=UnityEngine.Object.Instantiate(original);layout.name=WorldMapMission100.LayoutResource;
            foreach(var c in layout.chapters)c.entries.RemoveAll(e=>e.level<=100.1f);
            WorldMapLayout.Entry Add(float level,float side,Sprite sprite,PropKind kind,float height,string key)
            {
                if(sprite==null)throw new Exception("Missing mission sprite "+key);
                int index=Mathf.Max(0,(Mathf.RoundToInt(level)-1)/WorldMapEditorial.LevelsPerChapter);var chapter=layout.Find(index);if(chapter==null){chapter=new WorldMapLayout.Chapter{index=index};layout.chapters.Add(chapter);}chapter.manual=true;
                var e=new WorldMapLayout.Entry{id="mission100-"+key,level=level,lateral=side,sprite=sprite,kind=kind,height=height,keepSprite=true,customSize=true,shadow=true,theme=WorldMapObjectTheme.Season};chapter.entries.Add(e);return e;
            }
            Sprite Tree(int i)=>Art(new[]{"green-rounded-tree-v27","pine-classic-glossy-v29","coral-fan-tree-v20"}[i%3],"Arvores");
            var shrub=Art("shrub-green-glossy-v29");var redShrub=Art("shrub-coral-v15");var box=Art("mailbox-glossy-v44");var lamp=Art("lamp-straight-glossy-v46");var sign=Art("postal-wayfinding-v50");
            var courier=Art("courier-wizard-v50","Personagens");var organizer=Art("organizer-wizard-v50","Personagens");var inventor=Art("inventor-wizard-v50","Personagens");var goblin=Art("scribble-v50","Personagens");
            var wagon=Art("scribble-wagon-v50");var parcelMonster=Art("parcel-monster-v50");
            // Small alternating clusters; major architecture is spaced ten levels apart.
            for(int level=3;level<=99;level+=3)
            {
                float side=(level/3)%2==0?-1:1;
                Add(level+.2f,side*2.10f,Tree(level/3),PropKind.Tree,level%2==0?1.20f:1.34f,"tree-"+level);
                Add(level+1.1f,-side*1.65f,(level/3)%3==0?redShrub:shrub,PropKind.Small,.46f,"shrub-"+level);
                if(level%9==3)Add(level-.4f,-side*1.62f,box,PropKind.Mailbox,.52f,"mailbox-"+level);
                if(level%12==3)Add(level+1.5f,side*1.60f,lamp,PropKind.Lamp,.92f,"lamp-"+level);
            }
            void Sign(float level,float side,string text,string id){var e=Add(level,side,sign,PropKind.Small,.48f,id);e.caption=text;e.theme=WorldMapObjectTheme.None;}
            Sign(1,-1.60f,"MAGIC MAIL","welcome-sign");Add(1.4f,1.5f,box,PropKind.Mailbox,.63f,"signature-mailbox");Add(3.3f,-1.72f,courier,PropKind.Character,1.45f,"meet-courier");
            Add(6.8f,1.72f,kit.bench,PropKind.Small,.37f,"entry-bench");
            Add(10,1.98f,Art("postal-archway-v19","Construcoes"),PropKind.Landmark,1.10f,"entry-gate");
            Add(19.8f,-1.95f,Art("central-headquarters-v50","Construcoes"),PropKind.Landmark,1.48f,"headquarters");
            Sign(22,-1.62f,"PARCEL YARD","yard-sign");Add(24,1.72f,organizer,PropKind.Character,1.06f,"meet-organizer");
            var tipped=Add(30,-1.80f,kit.parcelCart??wagon,PropKind.Small,.64f,"tipped-cart");
            tipped.effectPieces=new[]{new WorldMapPiecePose{id="adornment-spilled-envelope",pose=new WorldMapEffectPose{rotation=-18}}};
            tipped.customAdornments=true;tipped.adornments=new[]{new WorldMapAdornment{id="adornment-spilled-envelope",label="Spilled letter",sprite=kit.envelopeCream,size=.20f,anchor=new Vector2(.8f,.2f)}};
            Add(31.6f,1.65f,goblin,PropKind.Character,.66f,"first-scribble");
            for(int i=0;i<3;i++)Add(29.5f+i*.5f,-1.30f-i*.16f,kit.parcels,PropKind.Small,.23f+i*.025f,"spilled-parcels-"+i);
            Add(34.5f,1.82f,Art("parcel-shelter-v17","Construcoes"),PropKind.Landmark,.78f,"loading-shelter");
            Add(40,-1.92f,Art("sorting-hall-v50","Construcoes"),PropKind.Landmark,1.22f,"sorting-hall");
            Sign(42,1.64f,"AVIARY","aviary-sign");Add(45.3f,-1.75f,kit.simplePostal[4],PropKind.Character,.50f,"owl-guide");
            Add(50,1.85f,parcelMonster,PropKind.Small,.98f,"special-parcel-pile");Add(48,-1.75f,courier,PropKind.Character,1.40f,"courier-pile");
            Add(54.2f,-1.80f,Art("courier-stables-v19","Construcoes"),PropKind.Landmark,.88f,"creature-shelter");
            Add(60,1.86f,Art("messenger-aviary-v19","Construcoes"),PropKind.Landmark,1.18f,"owl-tower");
            var nests=Add(57,-1.95f,Tree(0),PropKind.Tree,1.20f,"nest-tree");nests.customAdornments=true;nests.adornments=new[]{WorldMapAdornment.Create(kit.envelopePurple,"Delivery pouch"),WorldMapAdornment.Create(kit.simplePostal[4],"Owl on perch")};nests.adornments[0].anchor=new Vector2(.28f,.58f);nests.adornments[0].size=.20f;nests.adornments[1].anchor=new Vector2(.68f,.75f);nests.adornments[1].size=.22f;
            Sign(62,-1.62f,"WORKSHOP","workshop-sign");Add(65,1.78f,inventor,PropKind.Character,.88f,"meet-inventor");
            var workshop=Add(70,-1.95f,Art("seal-workshop-v19","Construcoes"),PropKind.Landmark,1.25f,"workshop");workshop.customAdornments=true;var floating=WorldMapAdornment.Create(kit.parcels,"Enchanted chimney parcel");floating.anchor=new Vector2(.80f,1.10f);floating.size=.24f;workshop.adornments=new[]{floating};
            for(int i=0;i<3;i++)Add(72+i*1.7f,i%2==0?1.80f:-1.80f,Art("training-hoop-v50"),PropKind.Small,.62f+i*.06f,"training-hoop-"+i);
            Add(80,1.85f,wagon,PropKind.Small,.72f,"wagon-glimpse");Sign(82,-1.65f,"DISPATCH","dispatch-sign");
            Add(89.8f,1.90f,Art("dispatch-tower-v50","Construcoes"),PropKind.Landmark,1.92f,"dispatch-tower");
            Add(92,-1.75f,courier,PropKind.Character,1.42f,"courier-finale");Add(94.1f,1.78f,organizer,PropKind.Character,1.04f,"organizer-finale");Add(96,-1.78f,inventor,PropKind.Character,.86f,"inventor-finale");
            Add(100,-1.85f,Art("postal-archway-v19","Construcoes"),PropKind.Landmark,1.15f,"exit-gate");Add(99.5f,1.80f,wagon,PropKind.Small,.80f,"final-wagon");Add(99.9f,1.63f,goblin,PropKind.Character,.68f,"final-scribble");
            Sign(98.2f,-1.60f,"ROYAL EXIT","exit-sign");
            foreach(var c in layout.chapters)c.entries.Sort((a,b)=>a.level.CompareTo(b.level));
            AssetDatabase.CreateAsset(layout,Root+WorldMapMission100.LayoutResource+".asset");
            var mission=ScriptableObject.CreateInstance<WorldMapMission100>();mission.layout=layout;
            WorldMapForearm Arm(Rect region,Rect held,Vector2 elbow,float angle)=>new WorldMapForearm{region=region,heldItem=held,elbow=elbow,degrees=angle};
            mission.characters=new[]{
                new WorldMapMissionCharacter{sprite=courier,role="The Courier",wand="Crooked wood / amber",height=1.45f,forearms=new[]{Arm(new Rect(.55f,.60f,.44f,.13f),new Rect(.83f,.64f,.16f,.30f),new Vector2(.55f,.67f),1.7f)}},
                new WorldMapMissionCharacter{sprite=organizer,role="The Organizer",wand="Straight violet / amethyst",height=1.06f,forearms=new[]{Arm(new Rect(.73f,.48f,.24f,.18f),new Rect(.84f,.55f,.15f,.24f),new Vector2(.74f,.54f),1.5f),Arm(new Rect(.07f,.44f,.26f,.19f),new Rect(.06f,.47f,.24f,.18f),new Vector2(.27f,.51f),1.2f)}},
                new WorldMapMissionCharacter{sprite=inventor,role="The Inventor",wand="Brass fork / turquoise",height=.88f,forearms=new[]{Arm(new Rect(.13f,.40f,.17f,.26f),new Rect(.08f,.45f,.19f,.40f),new Vector2(.28f,.47f),1.7f),Arm(new Rect(.74f,.34f,.23f,.26f),new Rect(),new Vector2(.74f,.42f),1.3f)}},
                new WorldMapMissionCharacter{sprite=goblin,role="Scribble",wand="Stolen Royal Stamp",height=.66f,forearms=new[]{Arm(new Rect(.05f,.30f,.23f,.18f),new Rect(),new Vector2(.285f,.41f),1.4f),Arm(new Rect(.70f,.25f,.27f,.22f),new Rect(.78f,.22f,.18f,.28f),new Vector2(.70f,.41f),1.5f)}}};
            var levels=new[]{1,10,20,30,40,50,60,70,80,90,100};var titles=new[]{"Welcome to Magic Mail","The Central Gate","Headquarters","The Royal Stamp Is Missing","Letters on the Loose","Parcel Panic","The Owl Tower","Flight Practice","An Overloaded Escape","Dispatch Tower","Stop Scribble's Wagon"};
            var descriptions=new[]{"Meet the Courier and begin your first day.","Enter the Magic Mail grounds.","Prepare the royal delivery.","Scribble stole the Royal Stamp. Follow the spilled parcels.","Recover the scattered letters at the Sorting Hall.","Scribble enchanted a pile of parcels. Clear the distraction.","The messenger owls point the way forward.","The Inventor helps the team prepare for pursuit.","Scribble's overloaded wagon is heading for the exit.","The team regroups beneath the dispatch beacon.","Catch the wagon and recover the Royal Stamp. A dark-sealed coin hints at a larger plot."};
            mission.beats=levels.Select((l,i)=>new WorldMapMissionBeat{level=l,title=titles[i],description=descriptions[i],special=l==50||l==100}).ToArray();
            AssetDatabase.CreateAsset(mission,Root+WorldMapMission100.ResourcePath+".asset");Frame(layout,new WorldMapRoute(islands));EditorUtility.SetDirty(layout);AssetDatabase.SaveAssets();
            File.WriteAllText("C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/mission100-bake.txt","Authored "+layout.chapters.Sum(c=>c.entries.Count(e=>e.id.StartsWith("mission100-")))+" props; preserved later layout chapters; original layout untouched.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
