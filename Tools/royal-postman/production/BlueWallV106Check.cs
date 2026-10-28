using System;using System.IO;using UnityEditor;using UnityEngine;using UnityEngine.UI;using RoyalAves.EditorTools;using RoyalAves.UI.RoyalPostman2026;
public static class BlueWallV106Check
{
    static Color Pixel(RoyalPostmanScreenDocument d,float x,float y)
    {
        var prior=RenderTexture.active;var sample=new Texture2D(1,1,TextureFormat.RGBA32,false);
        try{RenderTexture.active=d.Texture;sample.ReadPixels(new Rect(Mathf.Round(x),Mathf.Round(y),1,1),0,0);sample.Apply();return sample.GetPixel(0,0);}finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(sample);}
    }
    public static void Run()
    {
        const string output="../hatch-flow-v105/blue-wall-v106.txt";var kit=ScriptableObject.CreateInstance<RoyalPostmanRuntimeKit>();
        try
        {
            kit.additionalScreens.Add(new RoyalPostmanRuntimeKit.Screen{id="pets",hideNavigation=true,prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab")});string samples="";
            using(var d=new RoyalPostmanScreenDocument(kit,"pets",720,1280))
            {
                var view=d.Root.GetComponent<RoyalPostmanPetsScreen>();view.Initialize(()=>{},i=>{});view.ShowCurrent(false);var wall=d.Root.transform.Find("Hatchery/BlueWall").GetComponent<RoyalPostmanGradient>();if(wall.enabled)throw new Exception("Opaque old Graphic still enabled");
                foreach(var size in new[]{new Vector2Int(720,1280),new Vector2Int(390,844)})
                {
                    d.Resize(size.x,size.y);d.Render();var timer=d.Bounds((RectTransform)view.hatchery.transform.Find("DurationDisplay"));var footer=d.Bounds((RectTransform)view.hatchery.transform.Find("Footer"));float y=(timer.yMin+footer.yMax)*.5f;
                    if(timer.yMin-footer.yMax<3)throw new Exception("No clear background sampling row");var left=Pixel(d,size.x*.1f,y);var center=Pixel(d,size.x*.5f,y);
                    if(left.b<left.r*2||center.b<center.r*2||left.grayscale-center.grayscale<.035f)throw new Exception("Actual composited screen does not display metallic blue reflection: "+left+" / "+center);
                    samples+=size+" reflection="+left+" center="+center+"; ";
                }
            }
            File.WriteAllText(output,"PASS: actual composed Incubadora preview renders reflected blue at720/390, old opaque Graphic disabled; "+samples+"\n");EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(output,e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
        finally{UnityEngine.Object.DestroyImmediate(kit);}
    }
}
