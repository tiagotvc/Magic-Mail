using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RoyalAves.Meta.WorldMap;

// Runs only in the isolated validation project. Never rebuild over an authored V53 layout.
public static class MagicLandscapesV53Bake
{
    static Sprite Art(string key)
    {
        foreach(var folder in new[]{"Props","Construcoes","Personagens","Arvores"})
        {var s=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/MapKitV3/Library/"+folder+"/"+key+".png");if(s!=null)return s;}
        throw new Exception("Missing landscape art: "+key);
    }
    public static void Run()
    {
        try
        {
            var mission=WorldMapMission100.Current;var layout=mission.layout;var kit=Resources.Load<WorldMapKit>("RoyalAvesWorldMapKit");var route=new WorldMapRoute(Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath));
            var original=layout.chapters.SelectMany(c=>c.entries).ToArray();
            if(original.Any(e=>e.id.StartsWith("magic53-")))throw new Exception("V53 already authored; adjust existing entries instead of rebuilding.");
            var zones=new List<(float level,float x)>();
            WorldMapLayout.Entry Add(float level,float x,string key,float h,string id,PropKind kind=PropKind.Small)
            {
                int index=route.ChapterAtMeters(route.MetersAt(level));var chapter=layout.Find(index);if(chapter==null){chapter=new WorldMapLayout.Chapter{index=index,manual=true};layout.chapters.Add(chapter);}
                chapter.manual=true;var e=new WorldMapLayout.Entry{id="magic53-"+id,sprite=Art(key),level=level,lateral=x-route.CenterX(route.MetersAt(level)),height=h,customSize=true,keepSprite=true,kind=kind,shadow=kind!=PropKind.GroundDetail,theme=WorldMapObjectTheme.None};chapter.entries.Add(e);if(kind!=PropKind.GroundDetail)zones.Add((level,x));return e;
            }
            void Part(WorldMapLayout.Entry owner,Sprite sprite,string id,float size,Vector2 anchor,WorldMapAdornmentMotion motion=WorldMapAdornmentMotion.None,Sprite alternate=null,bool emissive=false)
            {
                owner.customAdornments=true;owner.adornments=(owner.adornments??Array.Empty<WorldMapAdornment>()).Concat(new[]{new WorldMapAdornment{id="adornment-magic53-"+id,label=id.Replace('-',' '),sprite=sprite,size=size/owner.height,anchor=anchor,motion=motion,alternate=alternate,emissive=emissive,lightColor=new Color(1,.75f,.35f)}}).ToArray();
            }
            void Pose(WorldMapLayout.Entry e,string id,float rotation=0,Vector2? scale=null)=>e.effectPieces=WorldMapPiecePose.Set(e.effectPieces,"adornment-magic53-"+id,new WorldMapEffectPose{rotation=rotation,scale=scale??Vector2.one});
            // Gardens: familiar rounded foliage becomes a living postal tree.
            var tree=Add(5.2f,-1.38f,"green-rounded-tree-v27",.95f,"envelope-tree",PropKind.Tree);
            var branchPoints=new[]{new Vector2(.29f,.52f),new Vector2(.56f,.72f),new Vector2(.78f,.48f),new Vector2(.47f,.43f)};
            for(int i=0;i<branchPoints.Length;i++){Part(tree,kit.envelopeCream,"tree-letter-"+i,.14f,branchPoints[i],i==1?WorldMapAdornmentMotion.LetterFly:WorldMapAdornmentMotion.None);Pose(tree,"tree-letter-"+i,i%2==0?-12:9);}
            var flower=Add(12.8f,-1.42f,"seal-flower-v53",.31f,"seal-flowers");Part(flower,Art("postage-stamp-v53"),"flower-seal-impression",.075f,new Vector2(.5f,.64f),WorldMapAdornmentMotion.SealMark);
            var arch=Add(16.7f,1.35f,"ribbon-arch-frame-v53",.92f,"ribbon-arch",PropKind.Landmark);
            Part(arch,Art("ribbon-tail-v53"),"ribbon-tail-left",.32f,new Vector2(.42f,.74f),WorldMapAdornmentMotion.RibbonSway);Part(arch,Art("ribbon-tail-v53"),"ribbon-tail-right",.29f,new Vector2(.59f,.73f),WorldMapAdornmentMotion.RibbonSway);Pose(arch,"ribbon-tail-left",16);Pose(arch,"ribbon-tail-right",-13);
            // Ink Springs: structural painting, flowing liquid, pool and drops are independent pieces.
            var spring=Add(23.4f,-1.35f,"ink-spring-structure-v53",1.12f,"ink-spring",PropKind.Landmark);
            Part(spring,Art("ink-fall-layer-v53"),"falling-ink",.79f,new Vector2(.48f,.47f),WorldMapAdornmentMotion.InkFall);
            Part(spring,Art("ink-pool-layer-v53"),"spring-pool",.16f,new Vector2(.48f,.12f),WorldMapAdornmentMotion.PoolRipple);
            for(int i=0;i<3;i++)Part(spring,Art("ink-drop-v53"),"upward-drop-"+i,.055f,new Vector2(i%2==0?.36f:.61f,.27f+i*.08f),WorldMapAdornmentMotion.InkRise);
            var sourceStream=Add(23.0f,-1.24f,"ink-stream-v53",.74f,"source-stream",PropKind.GroundDetail);sourceStream.workMotion=WorldMapWorkMotion.InkStream;
            var mill=Add(32.3f,1.40f,"quill-mill-base-v53",.93f,"quill-mill",PropKind.Landmark);Part(mill,Art("quill-mill-rotor-v53"),"quill-rotor",.77f,new Vector2(.51f,.48f),WorldMapAdornmentMotion.QuillSpin);
            var millStream=Add(32.0f,1.28f,"ink-stream-v53",.55f,"mill-stream",PropKind.GroundDetail);millStream.workMotion=WorldMapWorkMotion.InkStream;
            var bridge=Add(36.5f,-1.33f,"envelope-bridge-v53",.44f,"envelope-bridge",PropKind.Landmark);
            var crossing=Add(36.5f,-1.33f,"ink-stream-v53",.72f,"bridge-stream",PropKind.GroundDetail);crossing.workMotion=WorldMapWorkMotion.InkStream;
            Part(bridge,Art("postage-stamp-v53"),"bridge-seal",.065f,new Vector2(.84f,.55f),WorldMapAdornmentMotion.SealGlow,emissive:true);
            // Messenger Grove: a floating stop, one owl lands during the idle loop.
            var islet=Add(47.1f,1.43f,"floating-delivery-islet-v53",.67f,"floating-islet");islet.workMotion=WorldMapWorkMotion.FloatingIslet;islet.shadow=false;
            Part(islet,Art("working-owl-v52"),"visiting-owl",.17f,new Vector2(.31f,.82f),WorldMapAdornmentMotion.OwlLanding,Art("working-owl-blink-v52"));
            // Stampworks: factory chimney transforms bubbles into stamps; tubes route real letters.
            var pipes=Add(63.0f,-1.43f,"mail-tubes-v53",.60f,"mail-tubes");Part(pipes,kit.envelopeCream,"incoming-letter",.12f,new Vector2(.19f,.47f),WorldMapAdornmentMotion.TubeIn);Part(pipes,Art("oversize-parcel-v52"),"outgoing-parcel",.14f,new Vector2(.77f,.70f),WorldMapAdornmentMotion.TubeOut);
            var wax=Add(71.7f,1.43f,"wax-pond-v53",.41f,"wax-pond");for(int i=0;i<2;i++)Part(wax,Art("magic-bubble-v53"),"wax-bubble-"+i,.085f,new Vector2(.35f+i*.28f,.47f),WorldMapAdornmentMotion.WaxBubble);Part(wax,Art("postage-stamp-v53"),"wax-envelope-mark",.09f,new Vector2(.51f,.43f),WorldMapAdornmentMotion.SealMark);
            var factory=Add(77.5f,-1.44f,"stamp-factory-v53",1.12f,"stamp-factory",PropKind.Landmark);for(int i=0;i<3;i++)Part(factory,Art("magic-bubble-v53"),"chimney-stamp-"+i,.12f,new Vector2(.74f,.92f),WorldMapAdornmentMotion.StampBubble,Art("postage-stamp-v53"));
            // Grand Dispatch: package architecture and a tethered mail cloud feed the final tower.
            var mountain=Add(83.1f,-1.40f,"parcel-mountain-v53",1.05f,"parcel-mountain",PropKind.Landmark);Part(mountain,Art("oversize-parcel-v52"),"restless-parcel",.19f,new Vector2(.81f,.56f),WorldMapAdornmentMotion.ParcelWiggle);Part(mountain,kit.envelopeCream,"depot-letter",.10f,new Vector2(.36f,.33f),WorldMapAdornmentMotion.LetterFly);
            var cloud=Add(91.3f,-1.43f,"mail-cloud-v53",.54f,"mail-cloud");cloud.workMotion=WorldMapWorkMotion.FloatingIslet;cloud.shadow=false;
            // A hanging rope and collection funnel are separate existing props, not painted onto the cloud.
            Part(cloud,Art("rope-toy-v52"),"cloud-tether",.06f,new Vector2(.55f,-.09f));Pose(cloud,"cloud-tether",90,new Vector2(2.4f,.55f));
            for(int i=0;i<3;i++)Part(cloud,kit.envelopeCream,"rain-letter-"+i,.095f,new Vector2(.34f+i*.16f,.35f),WorldMapAdornmentMotion.LetterRain);
            Add(91.3f,-1.43f,"mail-tubes-v53",.27f,"cloud-collection-funnel");
            // Fifth silhouette: a strong worker without wizard clothing or a wand.
            var gorilla=Art("gorilla-loader-v53");mission.characters=mission.characters.Concat(new[]{new WorldMapMissionCharacter{sprite=gorilla,role="The Loader — Gorilla",wand="",height=1.12f,forearms=new[]{new WorldMapForearm{region=new Rect(.015f,.08f,.30f,.40f),elbow=new Vector2(.21f,.46f),degrees=1.1f,speed=.85f}}}}).ToArray();
            Add(27.4f,1.38f,"gorilla-loader-v53",1.12f,"gorilla-loader",PropKind.Character);
            // Update only our routine anchors after native visual review. All remain independently editable.
            foreach(var e in original)
            {
                if(e.adornments==null)continue;
                for(int i=0;i<e.adornments.Length;i++)
                {
                    var a=e.adornments[i];
                    if(a.id=="adornment-work52-postal-balance")a.anchor=new Vector2(.50f,1.05f);
                    if(a.id=="adornment-work52-table-stamp")a.anchor=new Vector2(.20f,.80f);
                    if(a.id=="adornment-work52-waiting-owl")a.anchor=new Vector2(.45f,1.04f);
                    if(a.id=="adornment-work52-sleeping-courier")a.anchor=new Vector2(.5f,.73f);
                    if(a.id=="adornment-work52-windsock-cloth")a.anchor=new Vector2(.625f,.909f);
                    if(a.sprite!=null&&a.sprite.name=="mailcart-wheel-v52")a.anchor=new Vector2(.56f,.285f);
                    e.adornments[i]=a;
                }
            }
            // Give each major attraction air. Never remove non-campaign/user placements.
            int cleared=0;foreach(var chapter in layout.chapters)
            {
                cleared+=chapter.entries.RemoveAll(e=>e.id.StartsWith("mission100-")&&(e.id.StartsWith("mission100-tree-")||e.id.StartsWith("mission100-shrub-")||e.id.StartsWith("mission100-mailbox-")||e.id.StartsWith("mission100-lamp-"))&&zones.Any(z=>Mathf.Abs(e.level-z.level)<1.3f&&Mathf.Sign(e.lateral)==Mathf.Sign(z.x)));
                chapter.entries.Sort((a,b)=>a.level.CompareTo(b.level));
            }
            // The spring/mill replace blank garden background, not the pursuit landmarks.
            foreach(var e in original.Where(e=>e.id=="mission100-first-scribble"))e.level=30.0f;
            EditorUtility.SetDirty(layout);EditorUtility.SetDirty(mission);AssetDatabase.SaveAssets();
            File.WriteAllText("C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/magic-landscapes-v53-bake.txt","12 magic landscape attractions, modular liquid/rotor/tails/letters/stamps; fifth non-wizard gorilla silhouette. Corrected routine prop attachment points. Cleared "+cleared+" nearby campaign background defaults. Original scene, original layout, islands and season not saved.");EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
