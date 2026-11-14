using System;
using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace RoyalAves.EditorTools
{
 public static class PendingElementEditorTools
 {
  [MenuItem("Royal Aves/Elementos/Preparar Pocoes, Bolsa e Sinalizador no Level Editor")]
  public static void Build()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before registering editor elements.");
   foreach(var key in LightBallLaboratory.OtherKeys)SessionState.SetBool(key,false);
   LightBallLaboratory.Disable();InkwellLaboratory.Disable();PostalTapeLaboratory.Disable();SortingSignalLaboratory.Disable();
   AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   var config=Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");
   foreach(var pair in new[]{(SquareTypes.RoutePotions,RouteCentralLaboratory.CellPath),(SquareTypes.BagOpenings,MailbagLaboratory.CellPath),(SquareTypes.SortingSignals,SortingSignalLaboratory.PrefabPath)})
   {
    var source=AssetDatabase.LoadAssetAtPath<GameObject>(pair.Item2);if(source==null)throw new InvalidOperationException("Missing approved element: "+pair.Item2);
    var scene=EditorSceneManager.NewPreviewScene();var root=UnityEngine.Object.Instantiate(source);SceneManager.MoveGameObjectToScene(root,scene);root.name=pair.Item1.ToString();
    try
    {
     var square=root.GetComponent<Square>();square.type=pair.Item1;square.sizeInSquares=Vector2Int.one;square.canMoveIn=square.canMoveOut=false;
     var renderer=root.GetComponent<SpriteRenderer>();square.mainRenderer=renderer;
     if(pair.Item1==SquareTypes.RoutePotions)renderer.sprite=root.GetComponent<RouteCentralCell>().artwork;
     if(pair.Item1==SquareTypes.BagOpenings)
     {
      var bag=root.GetComponent<BagOpeningStation>();renderer.sprite=bag.frames[0];
      if(!RoyalAves.Effects.Soft3DElementStyle.Has(bag))bag.scrollFrames=Enumerable.Range(0,16).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Laboratory/ScrollOpeningFrame"+i.ToString("00")+".asset")).ToArray();
      if(bag.frames.Length!=16||bag.frames.Any(s=>s==null)||bag.scrollFrames.Any(s=>s==null))throw new InvalidOperationException("Incomplete approved bag animation sprites.");
     }
     var bind=root.GetComponent<BindLayer>();if(bind==null)bind=root.AddComponent<BindLayer>();bind.order=4;
     var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/SweetSugar/Resources/Blocks/"+pair.Item1+".prefab");
     var goal=config.targets.Single(t=>t.name==pair.Item1.ToString());goal.prefabs=new System.Collections.Generic.List<GameObject>{prefab};goal.setCount=pair.Item1==SquareTypes.BagOpenings?SetCount.Manually:SetCount.FromLevel;
    }
    finally{EditorSceneManager.ClosePreviewScene(scene);}
   }
   EditorUtility.SetDirty(config);AssetDatabase.SaveAssetIfDirty(config);AssetDatabase.SaveAssets();
  }
  [InitializeOnLoadMethod]static void Listen()=>EditorApplication.update+=Poll;
  static void Poll()
  {
   const string request="Logs/pending-elements-editor.request";if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
   if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete(request);
   try{Build();File.WriteAllText("Logs/pending-elements-editor.result","READY: Pocoes 2x2, Bolsa fixa and Sinalizador registered in normal Level Editor; original art reused; tape remains no-goal.");}catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/pending-elements-editor.result",e.ToString());}
  }
 }
}
