using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.EditorTools;
using RoyalAves.UI.RoyalPostman2026;

public static class PetTabsV99Review
{
    static void Check(bool ok,string message)=>PetTabsV99Build.Check(ok,message);
    static Color Sample(RoyalPostmanScreenDocument d,Image image)
    {
        d.Render();var bounds=d.Bounds(image.rectTransform);var prior=RenderTexture.active;var pixels=new Texture2D(5,5,TextureFormat.RGBA32,false);
        try{RenderTexture.active=d.Texture;pixels.ReadPixels(new Rect(Mathf.Round(bounds.xMin+bounds.width*.2f),Mathf.Round(bounds.center.y),5,5),0,0);pixels.Apply();Color sum=Color.clear;foreach(var c in pixels.GetPixels())sum+=c;return sum/25f;}
        finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(pixels);}
    }
    public static void Run()
    {
        try
        {
            using(var d=new RoyalPostmanScreenDocument(RoyalPostmanRuntimeKit.Load(),"pets",720,1280))
            {
                var v=d.Root.GetComponent<RoyalPostmanPetsScreen>();v.Initialize(()=>{},i=>{});var h=v.hatchery;var a=v.currentTab.GetComponent<Image>();var b=v.incubatorTab.GetComponent<Image>();
                var source=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/UI/UI assets/Btn_squared_blue_g.png");
                Check(a.sprite==source&&b.sprite==source,"Authored tab artwork replaced");
                Check(h.selectedTabMaterial!=null&&h.unselectedTabMaterial!=null,"Tab materials not wired");
                foreach(var material in new[]{h.selectedTabMaterial,h.unselectedTabMaterial})Check(material.shader.name=="AllIn1SpriteShader/AllIn1SpriteShaderUiMask"&&material.IsKeywordEnabled("HSV_ON"),"Wrong shader or HSV keyword");
                foreach(var language in new[]{RoyalPostmanHeaderTitle.EditorLanguage.Portuguese,RoyalPostmanHeaderTitle.EditorLanguage.English})
                {
                    h.headerTitle.editorLanguage=language;h.Refresh();
                    for(int i=0;i<6;i++)
                    {
                        bool pets=i%2==0;(pets?v.currentTab:v.incubatorTab).onClick.Invoke();
                        Check(v.currentPanel.activeSelf==pets&&v.workshop.activeSelf!=pets,"Tab navigation broken");
                        Check(a.material==(pets?h.selectedTabMaterial:h.unselectedTabMaterial)&&b.material==(pets?h.unselectedTabMaterial:h.selectedTabMaterial),"Selected material did not swap");
                        Check(a.color==Color.white&&b.color==Color.white&&v.currentTab.interactable&&v.incubatorTab.interactable,"Tab dimmed or disabled");
                        bool pt=language==RoyalPostmanHeaderTitle.EditorLanguage.Portuguese;
                        Check(v.currentTab.GetComponentInChildren<Text>().text==(pt?"Mascotes":"Pets")&&v.incubatorTab.GetComponentInChildren<Text>().text==(pt?"Incubadora":"Hatchery"),"Tab localization changed");
                        Check(h.headerTitle.GetComponent<Image>().sprite==(pets?(pt?h.headerTitle.petsPortuguese:h.headerTitle.petsEnglish):(pt?h.headerTitle.portuguese:h.headerTitle.english)),"Header localization/navigation changed");
                    }
                }
                v.ShowCurrent(false);var blue=Sample(d,a);var red=Sample(d,b);
                Check(blue.b>blue.r*1.3f&&red.r>red.b*1.3f&&red.r>red.g*1.2f,"Rendered tabs do not show selected red and inactive blue: "+red+" / "+blue);
                v.currentTab.onClick.Invoke();var selectedPets=Sample(d,a);Check(selectedPets.r>selectedPets.b*1.3f,"Pets selection did not render red");
                File.WriteAllText(PetTabsV99Build.Output+"/pixels.txt","Selected hatchery: "+red+"; inactive pets: "+blue+"; selected pets: "+selectedPets+"\n");
            }
            File.WriteAllText(PetTabsV99Build.Output+"/review.txt","PASS: native compile/render and pixel colors; actual user sprite retained, AllIn1 UI HSV red selected/blue inactive, white tint, both buttons enabled; repeated tab clicks reuse the two material assets; profile/workshop navigation and PT/EN labels/title preserved.\n");EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(PetTabsV99Build.Output+"/review.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
