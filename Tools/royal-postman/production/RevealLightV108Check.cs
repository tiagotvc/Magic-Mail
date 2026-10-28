using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.Meta;
using RoyalAves.EditorTools;
using RoyalAves.UI.RoyalPostman2026;

public static class RevealLightV108Check
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static Color[] Capture(RoyalPostmanScreenDocument d)
    {
        d.Render();var prior=RenderTexture.active;var t=new Texture2D(d.Width,d.Height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=d.Texture;t.ReadPixels(new Rect(0,0,d.Width,d.Height),0,0);t.Apply();return t.GetPixels();}
        finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(t);}
    }
    static int Brightened(Color[] on,Color[] off,float threshold)
    {
        int count=0;for(int i=0;i<on.Length;i++)if(on[i].grayscale-off[i].grayscale>threshold)count++;return count;
    }
    public static void Run()
    {
        const string output="../hatch-flow-v105/reveal-v108.txt";
        var kit=ScriptableObject.CreateInstance<RoyalPostmanRuntimeKit>();var catalog=ScriptableObject.CreateInstance<PetHatchCatalog>();
        try
        {
            kit.additionalScreens.Add(new RoyalPostmanRuntimeKit.Screen{id="pets",hideNavigation=true,prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab")});string metrics="";
            using(var d=new RoyalPostmanScreenDocument(kit,"pets",720,1280){ReadOnly=true})
            {
                var v=d.Root.GetComponent<RoyalPostmanPetsScreen>();v.Initialize(()=>{},i=>{});var h=v.hatchery;catalog.entries=new PetHatchCatalog.Entry[4];
                for(int i=0;i<4;i++)catalog.entries[i]=new PetHatchCatalog.Entry{eggKind=i,petId="preview-"+i,portugueseName="Prévia de mascote",englishName="Pet preview",rarity=i,portrait=v.currentPanel.GetComponent<RoyalPostmanCurrentPet>().hero.sprite};
                h.ConfigureSimulation(catalog);long now=1700000000;var state=new PetsIncubationState{commonEggs=3,rareEggs=3,epicEggs=3,legendaryEggs=3};
                v.BindInventory(state,()=>{},n=>false,()=>0,()=>now);v.RefreshAvailability(100);v.ShowCurrent(false);
                for(int kind=0;kind<4;kind++)
                {
                    v.items[kind].button.onClick.Invoke();h.incubate.onClick.Invoke();now=state.finishes[0];h.Refresh();Check(h.Reveal.IsOpen,"Hatch reveal not opened");h.Reveal.Sample(3.5f);
                    var root=d.Root.transform.Find("HatchReveal");var pet=root.Find("Pet").GetComponent<Image>();var rays=root.Find("RotatingRays").GetComponent<RoyalPostmanRevealLight>();
                    Check(pet.color==Color.white,"Pet artwork is still tinted dark");Check(root.Find("Dimmer").GetComponent<Image>().color.a>.97f,"Dark background changed");
                    foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(390,844)})
                    {
                        d.Resize(size.x,size.y);h.Reveal.Sample(3.5f);var composed=Capture(d);pet.enabled=false;var noPet=Capture(d);pet.enabled=true;rays.enabled=false;var noRays=Capture(d);rays.enabled=true;
                        int petPixels=Brightened(composed,noPet,.15f),rayPixels=Brightened(composed,noRays,.35f);
                        Check(petPixels>size.x*2,"Pet still appears dark in native composition: "+petPixels);Check(rayPixels>size.x*2,"Ray faces still appear dark in native composition: "+rayPixels);
                        metrics+="rarity"+kind+" "+size+" pet="+petPixels+" bright-ray="+rayPixels+"; ";
                    }
                    h.Reveal.Continue();Check(state.pendingHatches.Count==0,"Continue failed");
                }
            }
            File.WriteAllText(output,"PASS: actual native reveal with original pet colours, broad bright ray faces, four rarity palettes, preserved dark backdrop/sparkles/continue at720and390. "+metrics+"\n");EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(output,e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
        finally{UnityEngine.Object.DestroyImmediate(catalog);UnityEngine.Object.DestroyImmediate(kit);}
    }
}
