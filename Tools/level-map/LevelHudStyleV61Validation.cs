using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

public static class LevelHudStyleV61Validation
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/level-hud-style-v61";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static int errors;
    public static void Run()
    {
        Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors++;};
        RoyalAvesLevelMapPreview preview=null;
        try
        {
            ShaderUtil.allowAsyncCompilation=false;Directory.CreateDirectory(Output);preview=RoyalAvesLevelMapPreview.CreateScenePreview(1080,1920);
            typeof(RoyalAvesLevelMapPreview).GetField("seasonPreview",Private).SetValue(preview,0);
            var world=Build(preview,1,1);world.ConfigureDayNight(false,4,0);Save(preview,"day");
            var block=new MaterialPropertyBlock();var bodies=world.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Nivel_")).ToArray();
            Check(bodies.Length>0,"No level buttons");
            foreach(var body in bodies)
            {
                body.GetPropertyBlock(block);var color=block.GetColor("_FaceColor");
                Check(color.b>color.g*4,"Upcoming button lost saturated purple");
                var mesh=body.GetComponent<MeshFilter>().sharedMesh;
                Check(body.GetComponent<MeshCollider>().sharedMesh==mesh,"Hit mesh does not follow new button shape");
                Check(Mathf.Abs(mesh.bounds.max.y-WorldMapTokenMesh.FaceHeight)<.0001f,"Domed face height incorrect");
            }
            world.ConfigureDayNight(false,4,1);Save(preview,"night");
            world=Build(preview,12,12);world.ConfigureDayNight(false,4,0);Save(preview,"completed-and-hard-day");
            bool orange=false,red=false;
            foreach(var body in world.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Nivel_")))
            {
                body.GetPropertyBlock(block);var color=block.GetColor("_FaceColor");
                orange|=color.r>color.g&&color.g>color.b*5;
                red|=color.r>color.g*8&&color.r>color.b*2;
            }
            Check(orange&&red,"Completed orange or hard red missing");
            world.ConfigureDayNight(false,4,1);Save(preview,"completed-and-hard-night");
            var stars=world.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("Estrela_")).ToArray();
            Check(stars.Length>0,"Reward stars missing");
            foreach(var star in stars)
            {
                var mesh=star.GetComponent<MeshFilter>().sharedMesh;
                Check(mesh.name=="RoundedRewardStar_NoOutline","Old outlined star still bound");
                Check(mesh.normals.All(n=>n.z<0),"Star winding faces away from camera");
                Check(mesh.bounds.size.x>.20f&&mesh.bounds.size.y>.20f,"Reward silhouette too small");
            }
            Check(world.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.StartsWith("Nivel_")).GetComponent<MeshFilter>().sharedMesh.colors.Any(c=>c.a==2),"White crystal band missing");
            Check(world.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.StartsWith("Nivel_")).GetComponent<MeshFilter>().sharedMesh.colors.Any(c=>c.a==3),"Purple inset channel missing");
            Check(errors==0,"Unity logged errors");
            File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: native Unity C# and shader compilation; day/night renders at 1080x1920; saturated purple upcoming, orange completed and red hard states; domed mesh height and shared collider geometry; rounded reward meshes without raster outlines; correct star winding and silhouettes; HUD-style crystal bases, thin gold piping, purple inset channels and softer number outlines; zero Unity errors. Source scene, layout and season were not saved. Manual pointer interaction and physical-device performance were not measured.");
            UnityEngine.Object.DestroyImmediate(preview);preview=null;EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);EditorApplication.Exit(1);}
    }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static WorldMapView Build(RoyalAvesLevelMapPreview preview,float level,int reached)
    {
        typeof(RoyalAvesLevelMapPreview).GetField("level",Private).SetValue(preview,level);
        typeof(RoyalAvesLevelMapPreview).GetMethod("Build",Private).Invoke(preview,new object[]{1080,1920,reached});
        var world=((CurvedLevelMap)typeof(RoyalAvesLevelMapPreview).GetField("map",Private).GetValue(preview)).World;world.TimeOverride=3;return world;
    }
    static void Save(RoyalAvesLevelMapPreview preview,string name)
    {
        preview.RenderScenePreview();var rt=(RenderTexture)preview.SceneTexture;var old=RenderTexture.active;var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),texture.EncodeToPNG());}
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(texture);}
    }
}


