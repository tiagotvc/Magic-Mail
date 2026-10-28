using System.IO;using System.Linq;using UnityEditor;
namespace RoyalAves.EditorTools
{
 public static class RoyalPostmanArtPaths
 {
  public const string Root="Assets/RoyalAves/Art/UI/";
  public static string Group(string name){
   if(name.StartsWith("navigation")||name.StartsWith("nav-"))return "Tab";
   if(name.Contains("avatar")||name.Contains("portrait")||name.Contains("gear")||name.Contains("symbol")||name=="top-plus-v15"||name.Contains("coin")||name.Contains("heart")||name.StartsWith("goal-")||name.StartsWith("booster-")||name=="paw-cream"||name=="lock-gray")return "Icon";
   if(name.StartsWith("hero-")||name.StartsWith("home-")||name.StartsWith("modal-")||name.StartsWith("panel-")||name.StartsWith("socket-")||name.Contains("value-"))return "Menu";
   return "Button";
  }
  public static string Resolve(string id,string extension,string child=""){
   var found=AssetDatabase.FindAssets(id,new[]{Root}).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p=>Path.GetFileName(p)==id+extension);
   if(!string.IsNullOrEmpty(found))return found;
   string legacy="Assets/RoyalAves/Art/RoyalPostmanUI2026/"+child+id+extension;if(File.Exists(legacy))return legacy;
   string folder=Root+Group(id)+"/RoyalPostman/"+child;Directory.CreateDirectory(folder);return folder+id+extension;
  }
  public static string Texture(string id)=>Resolve(id,".png");
  public static string Sprite(string id)=>Resolve(id,".asset","Sprites/");
  public static string Material(string id)=>Resolve(id,".mat","Materials/");
 }
}

