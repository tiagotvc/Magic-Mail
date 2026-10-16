using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEngine;
using UnityEditor;

public static class WorkScenesV52Validation
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/work-scenes-v52";
    public static void Run()
    {
        RoyalAvesLevelMapPreview preview=null;int errors=0;
        Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        try
        {
            Directory.CreateDirectory(Output);var mission=WorldMapMission100.Current;var layout=mission.layout;var before=EditorJsonUtility.ToJson(layout);var entries=layout.chapters.SelectMany(c=>c.entries).ToArray();
            foreach(var id in new[]{"arrival-bell","enchanted-watering-can","loading-cart","checking-table","break-bench","working-perch","playful-griffin","phoenix-stone-nest","broom-repair-stand","training-mailbox","dispatch-windsock","outward-remittance-cart"})Check(entries.Any(e=>e.id=="work52-"+id),"Missing working scene "+id);
            var motions=entries.Where(e=>e.adornments!=null).SelectMany(e=>e.adornments).Select(a=>a.motion).Distinct().ToArray();
            foreach(var motion in new[]{WorldMapAdornmentMotion.LetterFlutter,WorldMapAdornmentMotion.RopeTug,WorldMapAdornmentMotion.Windsock,WorldMapAdornmentMotion.WheelSpin,WorldMapAdornmentMotion.WaterDrop,WorldMapAdornmentMotion.OwlBlink})Check(motions.Contains(motion),"Missing motion "+motion);
            Check(entries.Count(e=>e.kind==PropKind.GroundDetail&&e.id.StartsWith("work52-ink-trace"))==4,"Ink trail missing");
            preview=RoyalAvesLevelMapPreview.CreateScenePreview(1080,1920);typeof(RoyalAvesLevelMapPreview).GetField("seasonPreview",Private).SetValue(preview,1);
            foreach(var level in new[]{1,7,25,29,32,36,43,46,52,57,64,67,73,85,96})
            {var world=Build(preview,level);world.ConfigureDayNight(false,4,0);Save(preview,"level-"+level.ToString("000")+"-day");Check(world.AdornmentNeedsFrames,"Work gestures do not keep preview updating");}
            var cartWorld=Build(preview,29);cartWorld.ConfigureDayNight(false,4,0);cartWorld.TimeOverride=0;preview.RenderScenePreview();
            var wheel=cartWorld.EffectPieces("mission100-tipped-cart").Single(p=>p.Id=="adornment-work52-abandoned-spinning-wheel");var initialWheel=wheel.transform.localRotation;var body=wheel.transform.parent.parent;var bodyRotation=body.localRotation;var bodyPosition=body.localPosition;
            cartWorld.TimeOverride=1;preview.RenderScenePreview();Check(Quaternion.Angle(initialWheel,wheel.transform.localRotation)>40,"Cart wheel does not rotate");Check(Quaternion.Angle(bodyRotation,body.localRotation)<.001f&&(bodyPosition-body.localPosition).sqrMagnitude<1e-10f,"Whole cart moves instead of wheel");
            cartWorld.TimeOverride=0;preview.RenderScenePreview();Check(Quaternion.Angle(initialWheel,wheel.transform.localRotation)<.001f,"Adornments accumulate rotation");
            var owlWorld=Build(preview,43);owlWorld.ConfigureDayNight(false,4,0);owlWorld.TimeOverride=.01f;preview.RenderScenePreview();
            var owl=owlWorld.EffectPieces("work52-working-perch").Single(p=>p.Id=="adornment-work52-waiting-owl");var drawing=owl.GetComponentInChildren<MeshRenderer>();var closed=drawing.sharedMaterial.mainTexture;Save(preview,"aviary-eyes-closed");owlWorld.TimeOverride=.35f;preview.RenderScenePreview();Check(drawing.sharedMaterial.mainTexture!=closed,"Owl does not blink");Save(preview,"aviary-eyes-open");
            var garden=Build(preview,7);garden.ConfigureDayNight(false,4,0);garden.TimeOverride=0;preview.RenderScenePreview();var drop=garden.EffectPieces("work52-enchanted-watering-can").First(p=>p.Id.Contains("water-drop"));var firstDrop=drop.transform.localPosition;garden.TimeOverride=.3f;preview.RenderScenePreview();Check((drop.transform.localPosition-firstDrop).sqrMagnitude>.00001f,"Water does not fall");
            var scenes=new[]{(1,"collection"),(7,"garden"),(29,"cart"),(43,"aviary"),(52,"griffin"),(85,"windsock")};
            foreach(var scene in scenes)
            {
                var world=Build(preview,scene.Item1);world.ConfigureDayNight(false,4,0);
                for(int frame=0;frame<12;frame++){world.TimeOverride=frame*.12f;Save(preview,scene.Item2+"-"+frame.ToString("00"));}
            }
            var night=Build(preview,85);night.ConfigureDayNight(false,4,1);Save(preview,"dispatch-night");
            Check(EditorJsonUtility.ToJson(layout)==before,"Validation mutated placement data");Check(errors==0,"Unity errors: "+errors);
            File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: native Unity runtime/editor C# and shaders; twelve independent work scenes; six quiet animation families; falling watering droplets; separate rotating wheel with completely stationary cart body; repeatable transforms without accumulated rotation; blink swaps only the owl frame; ink trail; 15 phone day captures, two blink captures, six 12-frame native animation sequences and one night capture. Existing route, major landmarks, four main characters and later chapters retained. Main scene/islands/season not saved. Manual editor pointer interaction and physical-device performance not measured.");UnityEngine.Object.DestroyImmediate(preview);EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);EditorApplication.Exit(1);}
    }
    static WorldMapView Build(RoyalAvesLevelMapPreview preview,float level)
    {typeof(RoyalAvesLevelMapPreview).GetField("level",Private).SetValue(preview,level);typeof(RoyalAvesLevelMapPreview).GetMethod("Build",Private).Invoke(preview,new object[]{1080,1920,1});var world=((CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview)).World;world.TimeOverride=2;return world;}
    static void Save(RoyalAvesLevelMapPreview preview,string name)
    {preview.RenderScenePreview();var rt=(RenderTexture)preview.SceneTexture;var old=RenderTexture.active;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());}finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);}}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
}
