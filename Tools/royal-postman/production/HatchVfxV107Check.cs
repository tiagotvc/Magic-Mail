using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.Meta;
using RoyalAves.EditorTools;
using RoyalAves.UI.RoyalPostman2026;

public static class HatchVfxV107Check
{
    const string Output="../hatch-flow-v105/vfx-v107.txt";
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static Color[] Capture(RoyalPostmanScreenDocument d)
    {
        d.Render();var prior=RenderTexture.active;var t=new Texture2D(d.Width,d.Height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=d.Texture;t.ReadPixels(new Rect(0,0,d.Width,d.Height),0,0);t.Apply();return t.GetPixels();}
        finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(t);}
    }
    static float Difference(Color[] a,Color[] b,int width,int height,Rect rect,out int brightened)
    {
        double total=0;int count=0;brightened=0;
        for(int y=Mathf.Max(0,Mathf.CeilToInt(rect.yMin));y<Mathf.Min(height,rect.yMax);y++)
        for(int x=Mathf.Max(0,Mathf.CeilToInt(rect.xMin));x<Mathf.Min(width,rect.xMax);x++)
        {
            int p=y*width+x;var diff=a[p]-b[p];total+=(Mathf.Abs(diff.r)+Mathf.Abs(diff.g)+Mathf.Abs(diff.b))/3;count++;
            if(Mathf.Max(diff.r,Mathf.Max(diff.g,diff.b))>.15f)brightened++;
        }
        return (float)(total/Mathf.Max(1,count));
    }
    static float Mean(Color[] pixels,int width,int height,Rect region)
    {
        double sum=0;int count=0;for(int y=(int)region.yMin;y<Mathf.Min(height,region.yMax);y++)for(int x=(int)region.xMin;x<Mathf.Min(width,region.xMax);x++){sum+=pixels[y*width+x].grayscale;count++;}return (float)(sum/Mathf.Max(1,count));
    }
    public static void Run()
    {
        var kit=ScriptableObject.CreateInstance<RoyalPostmanRuntimeKit>();var catalog=ScriptableObject.CreateInstance<PetHatchCatalog>();
        try
        {
            kit.additionalScreens.Add(new RoyalPostmanRuntimeKit.Screen{id="pets",hideNavigation=true,prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab")});string metrics="";
            using(var d=new RoyalPostmanScreenDocument(kit,"pets",720,1280){ReadOnly=true})
            {
                var v=d.Root.GetComponent<RoyalPostmanPetsScreen>();v.Initialize(()=>{},i=>{});var h=v.hatchery;
                catalog.entries=new PetHatchCatalog.Entry[4];for(int i=0;i<4;i++)catalog.entries[i]=new PetHatchCatalog.Entry{eggKind=i,petId="test-"+i,portugueseName="Prévia de mascote",englishName="Pet preview",rarity=i,portrait=v.currentPanel.GetComponent<RoyalPostmanCurrentPet>().hero.sprite};
                h.ConfigureSimulation(catalog);long now=1700000000;int saves=0;var state=new PetsIncubationState{commonEggs=3,rareEggs=3,epicEggs=3,legendaryEggs=3,accelerationItems=new[]{3,0,0,0}};
                v.BindInventory(state,()=>saves++,n=>false,()=>0,()=>now);v.RefreshAvailability(100);v.ShowCurrent(false);
                v.items[1].button.onClick.Invoke();h.incubate.onClick.Invoke();Check(state.Occupied(0)&&h.particles.IsRunning&&saves==1,"Running simulation did not start");
                foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(390,844)})
                {
                    d.Resize(size.x,size.y);h.particles.SetRunning(false);var cold=Capture(d);h.particles.SetRunning(true);h.particles.Sample(.8);var warm=Capture(d);var area=d.Bounds(h.particles.rearLayer);
                    float heat=Difference(warm,cold,d.Width,d.Height,area,out int heatPixels);Check(heat>.008f&&heatPixels>30,"Heat not visibly present in composed chamber: "+heat+" / "+heatPixels);
                    foreach(var mote in h.particles.motes)if(mote!=null)mote.enabled=false;var noMotes=Capture(d);foreach(var mote in h.particles.motes)if(mote!=null)mote.enabled=true;
                    float motes=Difference(warm,noMotes,d.Width,d.Height,area,out int motePixels);Check(motePixels>20,"Heating stars not visible at actual output size: "+motePixels);
                    metrics+=size+" heat delta="+heat.ToString("F4")+" bright pixels="+heatPixels+" heat-star pixels="+motePixels+"; ";
                }
                now=state.finishes[0];h.Refresh();Check(h.Reveal.IsOpen&&state.pendingHatches.Count==1&&saves==2,"Completion/receipt changed");
                var root=d.Root.transform.Find("HatchReveal");var rays=root.Find("RotatingRays").GetComponent<RoyalPostmanRevealLight>();var stars=rays.GetComponentInChildren<RoyalPostmanRadialSparkles>();var dimmer=root.Find("Dimmer").GetComponent<Image>();
                Check(stars!=null&&stars.sprite!=null&&stars.transform.parent==rays.transform&&!stars.raycastTarget,"Outward stars missing or not attached to rotating rays");
                foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(390,844)})
                {
                    d.Resize(size.x,size.y);h.Reveal.Sample(3.5f);var withStars=Capture(d);Check(dimmer.color.a>=.97f,"Reveal dimmer is too transparent");
                    float background=Mean(withStars,d.Width,d.Height,new Rect(d.Width*.23f,d.Height*.95f,d.Width*.42f,d.Height*.025f));Check(background<.08f,"Original header is still too bright: "+background);
                    stars.enabled=false;var noStars=Capture(d);stars.enabled=true;float sparkleDelta=Difference(withStars,noStars,d.Width,d.Height,new Rect(0,0,d.Width,d.Height),out int sparklePixels);Check(sparklePixels>40,"Reveal stars not visibly rendered: "+sparklePixels);
                    float high=rays.color.a;h.Reveal.Sample(3.78f);Capture(d);float low=rays.color.a;Check(Mathf.Abs(high-low)>.025f,"Ray opacity is not pulsing");
                    h.Reveal.Sample(4.35f);var later=Capture(d);float motion=Difference(later,withStars,d.Width,d.Height,new Rect(d.Width*.1f,d.Height*.3f,d.Width*.8f,d.Height*.45f),out _);Check(motion>.008f,"Reveal effects have no visible motion: "+motion);
                    metrics+=size+" background="+background.ToString("F4")+" reveal-star pixels="+sparklePixels+" pulse/motion="+motion.ToString("F4")+"; ";
                }
                h.Reveal.Sample(4);h.Reveal.Continue();Check(state.pendingHatches.Count==0&&saves==3,"Continue stopped acknowledging exactly once");
                for(int kind=0;kind<4;kind++)
                {
                    v.items[kind].button.onClick.Invoke();h.incubate.onClick.Invoke();now=state.finishes[0];h.Refresh();foreach(float frame in new[]{.6f,1.6f,2.3f,3.5f}){h.Reveal.Sample(frame);d.Render();}Check(h.Reveal.Ready,"Tier reveal stuck");h.Reveal.Continue();
                }
                File.WriteAllText(Output,"PASS: native Unity compile/render; heating glow and larger stars visibly affect the composed chamber; near-black background; two outward sparkle waves under rotating rays; ray/glow pulse and motion; all four rarity animations and continue; 720/390. "+metrics+"\n");
            }
            EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Output,e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
        finally{UnityEngine.Object.DestroyImmediate(catalog);UnityEngine.Object.DestroyImmediate(kit);}
    }
}
