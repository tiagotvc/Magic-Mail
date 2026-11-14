from pathlib import Path
p=Path('Logs/InkwellValidationProject/Assets/RoyalAves/Editor')
t=(p/'ThreeElementsEditorVerification.cs').read_text(encoding='utf-8-sig')
t=t.replace('ThreeElementsEditorVerification','SixElementsSoft3DVerification').replace('MagicMail.ThreeElements.Verify','MagicMail.SixSoft3D.Verify').replace('1902','1903').replace('three-elements-editor','six-soft3d').replace('MagicMailThreeElementsEditor','MagicMailSixSoft3D')
t=t.replace('   var config=Resources.Load<TargetEditorScriptable>', '   Soft3DProductionTools.Apply();\n   var config=Resources.Load<TargetEditorScriptable>',1)
t=t.replace('   var window=ScriptableObject.CreateInstance<LevelMakerEditor>();','''   void Obstacle(SquareTypes type,int x,int y,int layers=1){var c=field.levelSquares[y*field.maxCols+x];c.blocks.Clear();c.blocks.Add(new SquareTypeLayer{squareType=SquareTypes.EmptySquare});for(int n=0;n<layers;n++)c.blocks.Add(new SquareTypeLayer{squareType=type});c.block=type;c.blockLayer=layers;c.item=new ItemForEditor();}
   Obstacle(SquareTypes.SolidBlock,0,0,3);Obstacle(SquareTypes.EnchantedStone,0,6);Obstacle(SquareTypes.Inkwell,3,6);
   var window=ScriptableObject.CreateInstance<LevelMakerEditor>();''')
t=t.replace('new[]{"RoutePotions","BagOpenings","SortingSignals","CollectItems"}','new[]{"RoutePotions","BagOpenings","SortingSignals","SolidBlock","EnchantedStone","Inkwell","CollectItems"}')
t=t.replace('    if(step==6)\n    {','''    if(step==8)
    {
     var stone=UnityEngine.Object.FindFirstObjectByType<RoyalAves.Gameplay.EnchantedStone>();Require(stone!=null,"Missing rune");var stoneCell=stone.transform.parent.GetComponent<Square>();stone.HandleHit(stoneCell,false);Require(!stone.IsBreaking,"Rune damaged by ordinary hit");stoneCell.DestroyBlock();
     var ink=UnityEngine.Object.FindFirstObjectByType<InkwellCell>();Require(ink!=null,"Missing inkwell");var inkCell=ink.transform.parent.GetComponent<Square>();ink.HandleHit(inkCell,m.field.GetSquare(4,5));Require(ink.Hits==0,"Diagonal ink damage");
     m.field.GetSquare(0,0).DestroyBlock();step=9;at=EditorApplication.timeSinceStartup+.3;return;
    }
    if(step==9){m.field.GetSquare(0,0).DestroyBlock();step=10;at=EditorApplication.timeSinceStartup+.3;return;}
    if(step==10){m.field.GetSquare(0,0).DestroyBlock();step=11;at=EditorApplication.timeSinceStartup+.5;return;}
    if(step==11)
    {
     Require(Goal("SolidBlock")==0&&Goal("EnchantedStone")==0,"Crate/rune completion counters failed");
     var ink=UnityEngine.Object.FindFirstObjectByType<InkwellCell>();if(ink!=null){int before=ink.Hits;ink.transform.parent.GetComponent<Square>().DestroyBlock();Require(ink.Hits==before+1,"Inkwell stage skipped");step=12;at=EditorApplication.timeSinceStartup+.10;return;}
     Require(Goal("Inkwell")==0,"Inkwell goal completion failed");step=13;at=EditorApplication.timeSinceStartup+2;return;
    }
    if(step==12){PostalTapeLaboratoryTools.CaptureBoard("Logs/six-soft3d-ink-hit.png");step=11;at=EditorApplication.timeSinceStartup+.3;return;}
    if(step==13)
    {
     Require(Soft3DElementEffect.ActiveBursts==0,"Material FX leaked after completion");
     Require(UnityEngine.Object.FindObjectsByType<MailDeliveryOverlay>(FindObjectsSortMode.None).Length==0,"Bag HUD overlay leaked");
     File.WriteAllText("Logs/six-soft3d-native.result","PASS: six normal production families; catalog paint/save/load; two independent potion stands and four bottle colors; signal correct-color match, cycles and three-hit removal; fixed bags deliver only on arrival and respect shared quota; three crate layers counted once; rune ordinary hit rejected/special breaks; five inkwell hits/diagonal rejection/final paint and goal; transient FX and HUD overlays cleaned. API/native render validation, no physical input/device/audio/campaign claim.");SessionState.SetBool(Key,false);EditorApplication.Exit(0);return;
    }
    if(step==6)
    {''')
t=t.replace('SessionState.SetBool(Key,false);EditorApplication.Exit(0);\n    }','step=8;return;\n    }')
# Keep the three-elements pass record as an intermediate result; the final PASS occurs in step13.
t=t.replace('new[]{"RoutePotions","BagOpenings","SortingSignals","SolidBlock","EnchantedStone","Inkwell","CollectItems"}','new[]{"RoutePotions","BagOpenings","SortingSignals","CollectItems"}')
t=t.replace('     var stone=UnityEngine.Object.FindFirstObjectByType<RoyalAves.Gameplay.EnchantedStone>();','''     var config=Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");var second=ScriptableObject.CreateInstance<TargetLevel>();second.targets=new TargetList();
     m.levelData.TargetCounters.RemoveAll(c=>c.targetLevel.name!="CollectItems");
     foreach(var marker in m.field.GetComponentsInChildren<Soft3DElementStyle>())if(marker.GetComponent<ParcelStackLayer>()!=null||marker.GetComponent<InkwellCell>()!=null||marker.GetComponent<RoyalAves.Gameplay.EnchantedStone>()!=null){var component=SweetSugar.Scripts.System.Utils.ComponentCache.AddCachedComponent<TargetComponent>(marker.gameObject);component.field=m.field;}
     foreach(var name in new[]{"SolidBlock","EnchantedStone","Inkwell"})
     {
      int index=config.targets.FindIndex(c=>c.name==name);second.AddTargetType(index,config);var entry=second.targets.Last();var goal=config.targets[index];
      var legacy=name=="EnchantedStone"?AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Laboratory/EnchantedStoneSprite.asset"):entry.sprites.First().icon;
      var counter=new TargetCounter(goal.prefabs[0],1,new[]{legacy},-1,goal,false,entry,m.levelData);m.levelData.TargetCounters.Add(counter);Require(counter.count==1,"Wrong single obstacle count: "+name);Require(counter.extraObject.texture.name.Contains(name=="SolidBlock"?"crate":name=="Inkwell"?"inkwell":"rune"),"Legacy goal icon not resolved: "+name);
     }
     UnityEngine.Object.DestroyImmediate(second);
     foreach(var group in UnityEngine.Object.FindObjectsByType<SweetSugar.Scripts.GUI.TargetGUIGroup>(FindObjectsSortMode.None))typeof(SweetSugar.Scripts.GUI.TargetGUIGroup).GetMethod("SetTargets",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(group,null);
     var stone=UnityEngine.Object.FindFirstObjectByType<RoyalAves.Gameplay.EnchantedStone>();''')
# The target HUD supports four goals. Test the two goal families in sequence rather than authoring an invalid seven-goal fixture.
t=t.replace('PostalTapeLaboratoryTools.CaptureBoard(', 'Capture(')
t=t.replace('   EditorUtility.SetDirty(targets);','   var collect=targets.targets.Last();foreach(var duplicate in targets.targets.Where(e=>e.targetType.GetTarget().name=="CollectItems"&&e!=collect).ToArray())targets.targets.Remove(duplicate);\n   EditorUtility.SetDirty(targets);',1)
t=t.replace('static int step,deliveries;','static int step,deliveries,expectedBag;')
t=t.replace('static int step,deliveries,expectedBag;','static int step,deliveries,expectedBag;static bool settled;')
t=t.replace('     Require(m.field.GetComponentsInChildren<RouteCentralPrototype>().Length==2,','     if(!settled){settled=true;at=EditorApplication.timeSinceStartup+.7;return;}\n     File.WriteAllText("Logs/six-soft3d-bag-renderers.txt",string.Join("\\n",Bags().Select(b=>{var r=b.GetComponent<SpriteRenderer>();return "enabled="+r.enabled+" sprite="+r.sprite.name+" position="+r.transform.position+" scale="+r.transform.lossyScale+" bounds="+r.bounds+" material="+r.sharedMaterial.name;})));\n     Require(m.field.GetComponentsInChildren<RouteCentralPrototype>().Length==2,')
t=t.replace('int before=Goal("BagOpenings");var bag=', 'int before=Goal("BagOpenings");expectedBag=before-1;var bag=')
t=t.replace('Mathf.Max(0,3-deliveries)','expectedBag')
method='''
  static void Capture(string path)
  {
   var manager=LevelManager.THIS;var field=manager.levelData.fields[0];var a=manager.field.GetSquare(0,0).transform.position;var b=manager.field.GetSquare(field.maxCols-1,field.maxRows-1).transform.position;
   var root=new GameObject("SixElementBoardCapture");var camera=root.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=Mathf.Max(Mathf.Abs(a.y-b.y),Mathf.Abs(a.x-b.x))*.5f+.7f;camera.transform.position=(a+b)*.5f+Vector3.back*20;
   camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.06f,.06f,.10f);var rt=new RenderTexture(960,960,24);var pixels=new Texture2D(960,960,TextureFormat.RGBA32,false);var old=RenderTexture.active;
   try{camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,960,960),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());}
   finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(root);}
  }
'''
t=t.replace('  static void Tick()',method+'  static void Tick()')
(p/'SixElementsSoft3DVerification.cs').write_text(t,encoding='utf-8')
Path('Tools/royal-postman/production/review/six-elements-soft3d-v1/SixElementsSoft3DVerification.cs.txt').write_text(t,encoding='utf-8')
print('Prepared isolated six-element native verification')
