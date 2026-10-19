using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RoyalAves.Meta.WorldMap;

public static class WorkScenesV52Bake
{
    static Sprite Art(string key)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/Props/"+key+".png")??AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/Personagens/"+key+".png");
    public static void Run()
    {
        try
        {
            var mission=WorldMapMission100.Current;var layout=mission.layout;var kit=Resources.Load<WorldMapKit>("RoyalAvesWorldMapKit");var islands=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);var route=new WorldMapRoute(islands);
            if(layout.chapters.SelectMany(c=>c.entries).Any(e=>e.id.StartsWith("work52-")))throw new Exception("Work scenes already exist. Adjust them in the map editor instead of rebuilding.");
            var centers=new List<(float level,float side)>();int count=0;
            WorldMapLayout.Entry Add(float level,float side,Sprite sprite,float height,string key,PropKind kind=PropKind.Small)
            {
                if(sprite==null)throw new Exception("Missing work prop "+key);
                var index=route.ChapterAtMeters(route.MetersAt(level));var chapter=layout.Find(index);if(chapter==null){chapter=new WorldMapLayout.Chapter{index=index,manual=true};layout.chapters.Add(chapter);}chapter.manual=true;
                var e=new WorldMapLayout.Entry{id="work52-"+key,sprite=sprite,level=level,lateral=side-route.CenterX(route.MetersAt(level)),height=height,customSize=true,keepSprite=true,kind=kind,shadow=kind!=PropKind.GroundDetail,theme=WorldMapObjectTheme.Season};chapter.entries.Add(e);count++;return e;
            }
            WorldMapLayout.Entry Prop(float level,float side,string key,float size,string id)=>Add(level,side,Art(key),size,id);
            void Scene(float level,float side)=>centers.Add((level,side));
            WorldMapAdornment Part(WorldMapLayout.Entry owner,Sprite sprite,string id,float size,Vector2 anchor,WorldMapAdornmentMotion motion=WorldMapAdornmentMotion.None,bool behind=false)
            {
                if(sprite==null)throw new Exception("Missing adornment "+id);
                var p=new WorldMapAdornment{id="adornment-work52-"+id,label=id.Replace('-',' '),sprite=sprite,size=size/owner.height,anchor=anchor,motion=motion,behind=behind,lightColor=Color.white};
                owner.customAdornments=true;owner.adornments=(owner.adornments??Array.Empty<WorldMapAdornment>()).Concat(new[]{p}).ToArray();return p;
            }
            void PiecePose(WorldMapLayout.Entry e,string id,float angle)=>e.effectPieces=WorldMapPiecePose.Set(e.effectPieces,"adornment-work52-"+id,new WorldMapEffectPose{rotation=angle});
            var existing=layout.chapters.SelectMany(c=>c.entries).ToArray();
            // Entry: the collection box is busy; the letter is a separate moving piece.
            var box=existing.First(e=>e.id=="mission100-signature-mailbox");Part(box,kit.envelopeCream,"eager-letter",.18f,new Vector2(.49f,.61f),WorldMapAdornmentMotion.LetterFlutter);Scene(2,1.45f);
            Prop(4.8f,-1.47f,"bell-pedestal-v52",.42f,"arrival-bell");
            var hours=Add(5.5f,1.47f,Art("postal-wayfinding-v50"),.40f,"hours-board");hours.caption="POST HOURS\n8 AM – 6 PM";
            // Garden: watering can works while the existing bench is free.
            Scene(8,1.42f);Prop(8.1f,1.67f,"flower-planter-v52",.27f,"garden-planter");
            var can=Prop(8.1f,1.28f,"watering-can-v52",.30f,"enchanted-watering-can");can.workMotion=WorldMapWorkMotion.WateringCan;
            for(int i=0;i<3;i++)Part(can,Art("water-drop-v52"),"water-drop-"+i,.045f,new Vector2(.90f,.60f),WorldMapAdornmentMotion.WaterDrop);
            // Parcel yard: an oversize parcel will not fit in the cart.
            Scene(26,-1.40f);var loaded=Prop(26,-1.40f,"mailcart-body-v52",.34f,"loading-cart");Part(loaded,Art("mailcart-wheel-v52"),"loading-wheel",.15f,new Vector2(.63f,.21f));Part(loaded,Art("oversize-parcel-v52"),"oversize-load",.35f,new Vector2(.45f,.79f));
            Prop(26.7f,-1.68f,"wood-pallet-v52",.12f,"loading-pallet");Prop(26.7f,-1.55f,"mailbag-v52",.23f,"loading-mailbag");
            // Weighing and checking: scale, stamp and table remain independently editable.
            Scene(33,-1.42f);var table=Prop(33,-1.42f,"packing-table-v52",.35f,"checking-table");Part(table,Art("postal-scale-v52"),"postal-balance",.32f,new Vector2(.55f,1.45f));Part(table,Art("royal-stamp-v52"),"table-stamp",.10f,new Vector2(.18f,1.10f));
            // Break: abandoned lunch and an inquisitive owl.
            Scene(37,1.43f);var bench=Add(37,1.43f,kit.bench,.30f,"break-bench");Part(bench,Art("postal-thermos-v52"),"lunch-thermos",.12f,new Vector2(.30f,.69f));Part(bench,Art("postal-mug-v52"),"lunch-mug",.08f,new Vector2(.67f,.69f));Prop(37.1f,1.77f,"mailbag-v52",.21f,"resting-delivery-bag");var watcher=Prop(37.7f,1.18f,"working-owl-v52",.25f,"lunch-watcher");watcher.alternate=Art("working-owl-blink-v52");watcher.workMotion=WorldMapWorkMotion.OwlBlink;
            // Aviary: one courier waits, one sleeps after the round.
            Scene(44,-1.45f);var perch=Prop(44,-1.45f,"owl-perch-v52",.50f,"working-perch");var owl=Part(perch,Art("working-owl-v52"),"waiting-owl",.25f,new Vector2(.45f,1.22f),WorldMapAdornmentMotion.OwlBlink);perch.adornments[perch.adornments.Length-1].alternate=Art("working-owl-blink-v52");Part(perch,Art("mailbag-v52"),"perch-mailbag",.18f,new Vector2(.82f,.63f));
            var nest=Prop(44.8f,-1.77f,"woven-nest-v52",.13f,"sleeping-owl-nest");Part(nest,Art("working-owl-blink-v52"),"sleeping-courier",.22f,new Vector2(.5f,.95f));Prop(44.5f,-1.10f,"feeding-bowl-v52",.13f,"aviary-bowl");
            // Larger creatures: a griffin tugs at a rope beside an empty bag.
            Scene(53,1.43f);var griff=Prop(53,1.43f,"griffin-cub-v52",.40f,"playful-griffin");Part(griff,Art("rope-toy-v52"),"griffin-rope",.12f,new Vector2(.18f,.64f),WorldMapAdornmentMotion.RopeTug);Prop(53,1.03f,"mailbag-v52",.18f,"griffin-empty-bag");Prop(53.8f,1.74f,"creature-basket-v52",.22f,"griffin-basket");
            // Phoenix: heatproof nest and a separately placed egg.
            Scene(58,-1.44f);var stone=Prop(58,-1.44f,"stone-nest-v52",.20f,"phoenix-stone-nest");Part(stone,Art("phoenix-v52"),"brooding-phoenix",.33f,new Vector2(.5f,1.12f));Part(stone,Art("phoenix-egg-v52"),"phoenix-egg",.10f,new Vector2(.48f,.68f));Prop(58.5f,-1.10f,"feeding-bowl-v52",.12f,"phoenix-heatproof-bowl");
            // Workshop exterior: broom on a repair stand, toolbox open nearby.
            Scene(68,-1.46f);var stand=Prop(68,-1.46f,"owl-perch-v52",.35f,"broom-repair-stand");Part(stand,Art("repair-broom-v52"),"broom-under-repair",.44f,new Vector2(.50f,.82f));PiecePose(stand,"broom-under-repair",-24);Prop(68.5f,-1.13f,"toolbox-v52",.19f,"open-toolbox");
            // Training: a letter struggles into a target mailbox.
            Scene(74.2f,-1.44f);var target=Prop(74.2f,-1.44f,"mailbox-glossy-v44",.40f,"training-mailbox");Part(target,kit.envelopeCream,"training-letter",.15f,new Vector2(.43f,.55f),WorldMapAdornmentMotion.LetterFlutter);Prop(74.8f,-1.10f,"oversize-parcel-v52",.20f,"training-parcel");
            // Dispatch: ready bags, waiting couriers and the windsock.
            Scene(86.5f,1.42f);var wind=Prop(86.5f,1.42f,"windsock-pole-v52",.62f,"dispatch-windsock");Part(wind,Art("windsock-cloth-v52"),"windsock-cloth",.16f,new Vector2(.86f,.91f),WorldMapAdornmentMotion.Windsock);
            var rack=Prop(87.2f,1.05f,"owl-perch-v52",.38f,"ready-bag-rack");Part(rack,Art("mailbag-v52"),"ready-bag-a",.17f,new Vector2(.27f,.62f));Part(rack,Art("mailbag-v52"),"ready-bag-b",.17f,new Vector2(.78f,.62f));Prop(87.6f,1.74f,"working-owl-v52",.22f,"impatient-courier");
            // Exit: the first proper outward delivery is prepared beside the pursuit finale.
            Scene(97,-1.43f);var outward=Prop(97,-1.43f,"mailcart-body-v52",.31f,"outward-remittance-cart");Part(outward,Art("mailcart-wheel-v52"),"outward-wheel",.14f,new Vector2(.63f,.21f));Part(outward,Art("oversize-parcel-v52"),"secured-remittance",.26f,new Vector2(.43f,.73f));
            // Pursuit clues become more explicit along the road.
            Prop(28.8f,1.15f,"ink-pot-v52",.16f,"spilled-ink");
            for(int i=0;i<4;i++){var print=Add(29+i*.30f,1.12f+(i%2)*.14f,Art("ink-footprint-v52"),.10f,"ink-trace-"+i,PropKind.GroundDetail);print.theme=WorldMapObjectTheme.None;print.flipX=i%2==0;}
            var oldCart=existing.First(e=>e.id=="mission100-tipped-cart");oldCart.sprite=Art("mailcart-body-v52");oldCart.height=.34f;Part(oldCart,Art("mailcart-wheel-v52"),"abandoned-spinning-wheel",.15f,new Vector2(.63f,.21f),WorldMapAdornmentMotion.WheelSpin);
            Prop(35.8f,-1.12f,"open-parcel-v52",.23f,"escaped-parcel");
            var torn=Prop(46.8f,1.39f,"torn-mailbag-v52",.24f,"torn-pursuit-bag");Part(torn,kit.envelopeCream,"lost-letter-a",.09f,new Vector2(.82f,.16f));Part(torn,kit.envelopeCream,"lost-letter-b",.09f,new Vector2(1.05f,.08f));
            var wrong=Add(64,-1.42f,Art("postal-wayfinding-v50"),.35f,"wrong-direction-sign");wrong.caption="AVIARY →";wrong.flipX=true;
            var branch=existing.FirstOrDefault(e=>e.id=="mission100-tree-75");if(branch!=null){Part(branch,Art("oversize-parcel-v52"),"snagged-parcel",.23f,new Vector2(.65f,.50f));PiecePose(branch,"snagged-parcel",18);}
            // Make room only among our background defaults, retaining landmarks and user layout data.
            int cleared=0;foreach(var chapter in layout.chapters){cleared+=chapter.entries.RemoveAll(e=>e.id.StartsWith("mission100-")&&(e.id.StartsWith("mission100-tree-")||e.id.StartsWith("mission100-shrub-")||e.id.StartsWith("mission100-mailbox-")||e.id.StartsWith("mission100-lamp-"))&&centers.Any(c=>Mathf.Abs(e.level-c.level)<.9f&&Mathf.Sign(e.lateral)==Mathf.Sign(c.side)));chapter.entries.Sort((a,b)=>a.level.CompareTo(b.level));}
            EditorUtility.SetDirty(layout);AssetDatabase.SaveAssets();File.WriteAllText("C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/work-scenes-v52-bake.txt","Added "+count+" independent placements plus modular adornments in twelve working scenes; six quiet animation families; cleared "+cleared+" nearby background defaults. Existing landmarks, characters, authored anchors and later regions retained. Original layout/scene/islands/season not saved.");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
