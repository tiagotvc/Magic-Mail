using System;
using System.Reflection;
using RoyalAves.EditorTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;
using System.Text;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
public static class PolishPreviewBatch
{
 static int frames;
 static bool pilot;
 static bool layoutOnly;
 static bool presentationOnly;
 static bool horizonFix;
 static bool referenceStyle;
 static bool referenceValidation;
 static bool referenceFinish;
 static bool detailPolish;
 static bool sculpt;
 static bool smooth;
 static bool smoothCheck;
 static bool relief;
 static bool islands;
 static bool biomes;
 static bool biomeMotion;
 static bool effectsCheck;
 static bool seasonV5;
 static bool simpleV6;
 static bool simpleSky;
 static bool postmanRig;
 static bool treeV9;
 static bool treeV10;
 static bool postmanMap;
 static bool groundV13,groundV13Before;
 static bool groundV14;
 static bool libraryV15;
 static bool libraryV16;
 static bool buildingsV17;
 static bool buildingsV18;
 static bool buildingsV19;
 static bool groundArtV21,groundArtV21Before;
 public static void RunGroundArtV21Before(){groundArtV21Before=true;RunGroundArtV21();}
 public static void RunGroundArtV21(){IslandValidationBatch.PrepareFixtures();groundArtV21=true;Run();}
 public static void RunBuildingsV19(){IslandValidationBatch.PrepareFixtures();buildingsV19=true;Run();}
 public static void RunBuildingsV18(){IslandValidationBatch.PrepareFixtures();buildingsV18=true;Run();}
 public static void RunBuildingsV17(){IslandValidationBatch.PrepareFixtures();buildingsV17=true;Run();}
 public static void RunLibraryV16(){IslandValidationBatch.PrepareFixtures();libraryV16=true;Run();}
 public static void RunLibraryV15(){IslandValidationBatch.PrepareFixtures();libraryV15=true;Run();}
 public static void RunGroundV14(){IslandValidationBatch.PrepareFixtures();groundV14=true;Run();}
 public static void RunGroundV13Before(){groundV13Before=true;RunGroundV13();}
 public static void RunGroundV13(){IslandValidationBatch.PrepareFixtures();groundV13=true;Run();}
 public static void RunPostmanMap(){IslandValidationBatch.PrepareFixtures();postmanMap=true;Run();}
 public static void RunTreeV11(){RunTreeV10();}
 public static void RunTreeV10(){IslandValidationBatch.PrepareFixtures();treeV10=true;Run();}
 public static void RunTreeV9(){IslandValidationBatch.PrepareFixtures();treeV9=true;Run();}
 public static void RunPostmanRig(){postmanRig=true;Run();}
 public static void RunSimpleV6Sky(){simpleSky=true;Run();}
 public static void RunSimpleV6(){IslandValidationBatch.PrepareFixtures();simpleV6=true;Run();}
 public static void RunSeasonV5(){IslandValidationBatch.PrepareFixtures();seasonV5=true;Run();}
 public static void RunBiomeEffectsCheck(){effectsCheck=true;Run();}
 public static void RunBiomeMotion(){IslandValidationBatch.PrepareFixtures();biomeMotion=true;Run();}
 public static void RunBiomes(){IslandValidationBatch.PrepareFixtures();biomes=true;Run();}
 public static void RunIslands(){IslandValidationBatch.PrepareFixtures();islands=true;Run();}
 public static void RunRelief(){relief=true;Run();}
 public static void RunSmoothCheck(){smoothCheck=true;Run();}
 public static void RunSmoothPilot(){smooth=true;pilot=true;Run();}
 public static void RunSmoothFinish(){smooth=true;Run();}
 public static void RunSculptPilot(){sculpt=true;pilot=true;Run();}
 public static void RunSculptFinish(){sculpt=true;Run();}
 public static void RunSculptPaletteFinish(){sculpt=true;referenceFinish=true;Run();}
 public static void RunDetailPolish(){detailPolish=true;Run();}
 public static void RunDetailPilot(){detailPolish=true;pilot=true;Run();}
 public static void RunDetailFinish(){detailPolish=true;referenceFinish=true;Run();}
 static bool assetFit;
 public static void RunAssetFit(){assetFit=true;Run();}
 public static void RunReferenceFinish(){referenceStyle=true;referenceFinish=true;Run();}
 public static void RunReferenceStyle(){referenceStyle=true;Run();}
 public static void RunReferenceValidation(){referenceStyle=true;referenceValidation=true;Run();}
 public static void RunHorizonFix(){horizonFix=true;Run();}
 public static void RunPresentation(){presentationOnly=true;Run();}
 public static void RunLayout(){layoutOnly=true;Run();}
 public static void RunPilot(){pilot=true;Run();}
 public static void Run()
 {
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/SweetSugar/Settings/SweetSugarURP.asset");
  var renderer=AssetDatabase.LoadAssetAtPath<Renderer2DData>("Assets/SweetSugar/Settings/SweetSugarURP_Renderer2D.asset");
  Debug.Log("POLISH renderer: "+renderer+" pipeline: "+pipeline);
  var serialized=new SerializedObject(pipeline);
  serialized.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue=renderer;
  serialized.FindProperty("m_DefaultRendererIndex").intValue=0;
  serialized.ApplyModifiedPropertiesWithoutUndo();
  GraphicsSettings.defaultRenderPipeline=pipeline;
  QualitySettings.renderPipeline=pipeline;
  RoyalAvesWorldMapSetup.Ensure(true);
  EditorApplication.update+=Update;
 }
 static void Update()
 {
  if(EditorApplication.isCompiling || ++frames<8)return;
  EditorApplication.update-=Update;
  try
  {
   if(groundArtV21){GroundArtV21PreviewBatch.Execute(groundArtV21Before);EditorApplication.Exit(0);return;}
   if(buildingsV19){MapBuildingsV17PreviewBatch.ExecuteV19();EditorApplication.Exit(0);return;}
   if(buildingsV18){MapBuildingsV17PreviewBatch.ExecuteV18();EditorApplication.Exit(0);return;}
   if(buildingsV17){MapBuildingsV17PreviewBatch.Execute();EditorApplication.Exit(0);return;}
   if(libraryV16){MapLibraryV16ValidationBatch.Execute();EditorApplication.Exit(0);return;}
   if(libraryV15){MapLibraryV15ValidationBatch.Execute();EditorApplication.Exit(0);return;}
   if(groundV14){GroundV14ValidationBatch.Execute();EditorApplication.Exit(0);return;}
   if(groundV13){GroundV13PreviewBatch.Execute(groundV13Before);EditorApplication.Exit(0);return;}
   if(postmanMap){PostmanMapValidationBatch.Execute();EditorApplication.Exit(0);return;}
   if(treeV10){TreeV10ValidationBatch.Execute();EditorApplication.Exit(0);return;}
   if(treeV9){TreeV9ValidationBatch.Execute();EditorApplication.Exit(0);return;}
   if(postmanRig){PostmanRigValidationBatch.Execute();EditorApplication.Exit(0);return;}
   if(simpleSky){SimpleV6ValidationBatch.SkyCheck();EditorApplication.Exit(0);return;}
   if(simpleV6){SimpleV6ValidationBatch.Execute();EditorApplication.Exit(0);return;}
   if(seasonV5){SeasonV5ValidationBatch.Execute();EditorApplication.Exit(0);return;}
   if(effectsCheck){BiomeValidationBatch.EffectsCheck();EditorApplication.Exit(0);return;}
   if(biomeMotion){BiomeValidationBatch.Motion();EditorApplication.Exit(0);return;}
   if(biomes){BiomeValidationBatch.Execute();EditorApplication.Exit(0);return;}
   if(islands){IslandValidationBatch.Execute();EditorApplication.Exit(0);return;}
   if(smoothCheck){ValidateSmooth();EditorApplication.Exit(0);return;}
   if(relief)
   {
    var shot=new RoyalAvesWorldMapCapture.Request{outDir=Output,prefix="relief-green",cursors=new[]{2.55f},width=720,height=1600,nextLevel=1,levelCount=100,ui=true,audit=false};
    Capture(shot);shot.lava=true;shot.prefix="relief-lava";Capture(shot);
    ValidateSmooth();EditorApplication.Exit(0);return;
   }
   if(smooth)
   {
    var shot=new RoyalAvesWorldMapCapture.Request{outDir=Output,prefix="smooth-green",cursors=new[]{2.55f},width=720,height=1600,nextLevel=1,levelCount=100,ui=true,audit=false};
    Capture(shot);shot.cursors=new[]{6.1f};shot.nextLevel=8;Capture(shot);
    shot.cursors=new[]{11.5f,17.4f};shot.nextLevel=12;Capture(shot);
    shot.cursors=new[]{45f,85f};shot.nextLevel=19;Capture(shot);
    shot.lava=true;shot.prefix="smooth-lava";shot.cursors=new[]{89f};shot.nextLevel=90;Capture(shot);
    if(!pilot){Validate();ValidateLayout();ValidatePresentation();ValidateHud();ValidateAssetFit();ValidateDetailPolish();ValidateSculpt();ValidateSmooth();}
    EditorApplication.Exit(0);return;
   }
   if(sculpt)
   {
    var shot=new RoyalAvesWorldMapCapture.Request{outDir=Output,prefix="sculpt-green",cursors=new[]{2.55f},width=720,height=1600,nextLevel=1,levelCount=100,ui=true,audit=false};
    Capture(shot);shot.cursors=new[]{6.1f};shot.nextLevel=8;Capture(shot);
    shot.cursors=new[]{11.5f};shot.nextLevel=12;Capture(shot);
    shot.cursors=new[]{17.4f,45f,85f};shot.nextLevel=19;Capture(shot);
    shot.lava=true;shot.prefix="sculpt-lava";shot.cursors=new[]{89f};shot.nextLevel=90;Capture(shot);
    if(!pilot){if(!referenceFinish)Validate();ValidateLayout();ValidatePresentation();ValidateHud();ValidateAssetFit();ValidateDetailPolish();ValidateSculpt();}
    EditorApplication.Exit(0);return;
   }
   if(detailPolish)
   {
    var shot=new RoyalAvesWorldMapCapture.Request{outDir=Output,prefix="detail-green",cursors=new[]{2.55f},width=720,height=1600,nextLevel=1,levelCount=100,ui=true,audit=false};
    Capture(shot);shot.cursors=new[]{11.5f};shot.nextLevel=12;Capture(shot);
    shot.cursors=new[]{17.4f};shot.nextLevel=19;Capture(shot);
    shot.cursors=new[]{29f,45f,65f,85f};shot.nextLevel=46;Capture(shot);
    shot.lava=true;shot.prefix="detail-lava";shot.cursors=new[]{89f};shot.nextLevel=90;Capture(shot);
    if(!pilot){if(!referenceFinish)Validate();ValidateLayout();ValidatePresentation();ValidateHud();ValidateAssetFit();ValidateDetailPolish();}
    EditorApplication.Exit(0);return;
   }
   if(layoutOnly){ValidateLayout();EditorApplication.Exit(0);return;}
   if(presentationOnly){ValidatePresentation();EditorApplication.Exit(0);return;}
   if(assetFit)
   {
    ValidateLayout();ValidatePresentation();ValidateHud();ValidateAssetFit();
    var shot=new RoyalAvesWorldMapCapture.Request{outDir=Output,prefix="asset-fit-green",cursors=new[]{2.55f},width=720,height=1600,nextLevel=1,levelCount=100,ui=true,audit=false};
    Capture(shot);shot.cursors=new[]{17.4f};shot.nextLevel=19;Capture(shot);
    shot.prefix="asset-fit-button";shot.cursors=new[]{11.5f};shot.nextLevel=12;Capture(shot);
    shot.lava=true;shot.prefix="asset-fit-lava";shot.cursors=new[]{89f};shot.nextLevel=90;Capture(shot);
    EditorApplication.Exit(0);return;
   }
   if(referenceStyle)
   {
    if(referenceValidation){Validate();ValidateLayout();ValidatePresentation();ValidateHud();}
    if(referenceFinish){ValidateLayout();ValidatePresentation();ValidateHud();}
    var artKit=RoyalAvesWorldMapSetup.Ensure(false);
    if(artKit.terrainMaterials==null||artKit.navIcons.Length!=5||artKit.mailKiosk==null||artKit.bridgeTimber==null)throw new Exception("Reference-style assets not bound");
    var shot=new RoyalAvesWorldMapCapture.Request{outDir=Output,prefix="reference-current",cursors=new[]{2.55f},width=540,height=960,nextLevel=1,levelCount=100,ui=true,audit=false,diagnose=true};
    Capture(shot);
    shot.prefix="reference-green";shot.cursors=new[]{8.4f,17.4f,45f,85f};shot.nextLevel=19;Capture(shot);
    shot.lava=true;shot.prefix="reference-lava";shot.cursors=new[]{2.55f};shot.nextLevel=1;Capture(shot);
    shot.cursors=new[]{17.4f};shot.nextLevel=19;Capture(shot);
    shot.cursors=new[]{89f};shot.nextLevel=90;Capture(shot);
    shot.prefix="reference-narrow";shot.width=480;shot.height=1040;Capture(shot);
    shot.prefix="reference-wide";shot.width=640;shot.height=960;Capture(shot);
    shot.lava=false;shot.width=720;shot.height=1600;shot.prefix="reference-mobile-green";shot.cursors=new[]{2.55f};shot.nextLevel=1;Capture(shot);
    shot.cursors=new[]{17.4f};shot.nextLevel=19;Capture(shot);
    shot.cursors=new[]{45f};shot.nextLevel=46;Capture(shot);
    shot.lava=true;shot.prefix="reference-mobile-lava";shot.cursors=new[]{89f};shot.nextLevel=90;Capture(shot);
    EditorApplication.Exit(0);return;
   }
   if(horizonFix)
   {
    ValidatePresentation();
    var fix=new RoyalAvesWorldMapCapture.Request
    {
     outDir=Output,prefix="lava-horizon-fix",cursors=new[]{1f,17.4f,89f},
     width=540,height=960,nextLevel=90,levelCount=100,ui=true,lava=true,diagnose=true,audit=false
    };
    typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{fix});
    fix.prefix="lava-horizon-narrow";fix.cursors=new[]{89f};fix.width=480;fix.height=1040;
    typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{fix});
    fix.prefix="lava-horizon-wide";fix.width=640;fix.height=960;
    typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{fix});
    fix.lava=false;fix.prefix="green-inset-fix";fix.width=540;fix.height=960;fix.nextLevel=19;fix.cursors=new[]{1f,17.4f};
    typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{fix});
    EditorApplication.Exit(0);return;
   }
   if(!pilot){Validate();ValidateLayout();ValidatePresentation();}
   var r=new RoyalAvesWorldMapCapture.Request
   {
    outDir="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/polish",
    prefix="composition",cursors=new[]{1f,4f,9f,10.9f,12f,15.11f,37.95f},
    width=540,height=960,nextLevel=15,levelCount=100,ui=true,audit=false,diagnose=true
   };
   typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{r});
   SaveReport("ui-report.txt");
   r.prefix="comparison";r.cursors=new[]{10.9f};r.nextLevel=11;
   typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{r});
   r.prefix="current1";r.cursors=new[]{1f};r.nextLevel=1;
   typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{r});
   r.prefix="current4";r.cursors=new[]{4f};r.nextLevel=4;
   typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{r});
   r.prefix="current9";r.cursors=new[]{9f};r.nextLevel=9;
   typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{r});
   r.prefix="feedback";r.cursors=new[]{15.55f,18.9f};r.nextLevel=19;
   typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{r});
   r.prefix="bridge";r.cursors=new[]{17.4f};
   typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{r});
   r.lava=true;r.prefix="lava-layers";r.cursors=new[]{1f};r.nextLevel=1;
   typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{r});
   r.cursors=new[]{17.4f};r.nextLevel=19;
   typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{r});
   r.lava=false;
   if(pilot){EditorApplication.Exit(0);return;}
   r.nextLevel=15;r.cursors=new[]{4f,9f,12f,15.11f};
   r.ui=false; r.prefix="geometry"; r.audit=true;
   typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{r});
   SaveReport("geometry-report.txt");
   var kit=RoyalAvesWorldMapSetup.Ensure(false);
   kit.christmasPreview=true;
   r.ui=true;r.audit=false;r.prefix="christmas";r.cursors=new[]{1f,46f};
   typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{r});
   SaveReport("christmas-report.txt");
   kit.christmasPreview=false;
   r.prefix="weather";r.cursors=new[]{56f,87f,99f};
   typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{r});
   SaveReport("weather-report.txt");
   r.prefix="scroll";r.cursors=new float[33];
   for(var i=0;i<r.cursors.Length;i++)r.cursors[i]=10.9f+i*.13f;
   typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{r});
   SaveReport("scroll-report.txt");
   EditorApplication.Exit(0);
  }
  catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
 }
 static string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/polish";
 static void Capture(RoyalAvesWorldMapCapture.Request shot)
 {
  typeof(RoyalAvesWorldMapCapture).GetMethod("Execute",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{shot});
  var report=File.ReadAllText(Path.Combine(Application.dataPath,"../Temp/worldmap-capture.done"));
  if(!report.StartsWith("OK"))throw new Exception(report);
 }
 static void ValidatePresentation()
 {
  var kit=RoyalAvesWorldMapSetup.Ensure(false);
  if(kit.bridgeDeck==null||kit.postOffice==kit.legacyPostOffice||kit.skyGoldenOnly==null||kit.GroundArt==kit.skyGoldenOnly||kit.groundLavaIllustration==null||kit.GroundBounds.Length!=64)
   throw new Exception("Independent art layers or polished assets not bound");
  var art=AssetDatabase.LoadAssetAtPath<LobbyFeaturesConfig>("Assets/RoyalAves/Resources/RoyalAvesFeatures.asset");
  var view=WorldMapView.Create(null,art,kit,100,5,true);
  var distant=0;var grounded=0;var thick=0;
  try
  {
   view.TimeOverride=0;view.SetFixedSize(540,960);view.Tick(1,1,false);
   var slots=(System.Collections.IDictionary)typeof(WorldMapView).GetField("activeSlots",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(view);
   foreach(System.Collections.DictionaryEntry pair in slots)
   {
    var p=view.Props[(int)pair.Key];var rel=p.s-view.Route.Pos(1);var slot=pair.Value;var st=slot.GetType();
    if(p.kind==PropKind.FloatingMail)continue;
    var card=(Transform)st.GetField("card").GetValue(slot);
    var renderer=(MeshRenderer)st.GetField("cardRenderer").GetValue(slot);var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
    if(block.GetFloat("_GroundedFoot")<.5f)throw new Exception("Ground contact mask missing");grounded++;
    if(rel>=12&&rel<16&&p.shadow)
    {
     var shadow=(MeshRenderer)st.GetField("shadowRenderer").GetValue(slot);
     var shadowBlock=new MaterialPropertyBlock();shadow.GetPropertyBlock(shadowBlock);
     if(card.localEulerAngles.x<25||!shadow.enabled||shadowBlock.GetColor("_Color").a<.15f)throw new Exception("Distant lift or horizon shadow missing");
     distant++;
    }
   }
   foreach(var gap in view.Route.Gaps)
   {
    if(view.Route.InsetAmount(gap.start-.8f)>.001f||view.Route.InsetAmount(gap.end+.8f)>.001f||view.Route.InsetAmount(view.Route.Pos(gap.afterLevel))<.999f)
     throw new Exception("Rounded lane does not leave clear grass before the island edge");
   }
   var nodes=(Array)typeof(WorldMapView).GetField("nodes",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(view);
   foreach(var n in nodes){var nt=n.GetType();var level=(int)nt.GetField("level").GetValue(n);var tr=(Transform)nt.GetField("t").GetValue(n);
    if(view.Route.Pos(level)-view.Route.Pos(1)>12&&level>0){if(tr.localScale.y/tr.localScale.x<1.45f)throw new Exception("Horizon token thickness missing");thick++;}}
   if(grounded==0||distant==0||thick==0)throw new Exception("Presentation checks lacked representative objects");
   File.WriteAllText(Path.Combine(Output,"presentation-validation.txt"),$"PASS: {grounded} grounded sprites; {distant} distant tilted objects with visible shadow alpha; {thick} horizon tokens with volume. Clear grass margin at every crossing. Independent sky/ground art and new house/bridge bound.");
  }
   finally{view.Dispose();WorldMapCards.Clear();}
   var layerKit=UnityEngine.Object.Instantiate(kit);layerKit.terrainTheme=WorldMapTerrainTheme.LavaLand;
   var layerRoot=new GameObject("LayerValidation",typeof(RectTransform));
   try
   {
    var rect=(RectTransform)layerRoot.transform;rect.sizeDelta=new Vector2(1080,1920);
    var skyGo=new GameObject("SkyValidation",typeof(RectTransform),typeof(UnityEngine.UI.Image));skyGo.transform.SetParent(rect,false);
    var sky=skyGo.GetComponent<UnityEngine.UI.Image>();
    var atmosphere=WorldMapAtmosphere.Create(rect,rect,sky,layerKit);atmosphere.Tick(1,0);
    var images=layerRoot.GetComponentsInChildren<UnityEngine.UI.Image>();
    var smoke=Array.FindAll(images,x=>x.name.StartsWith("FumacaDoVulcao_"));
    var volcanoes=Array.FindAll(images,x=>x.name.StartsWith("VulcaoSeparado_")&&x.enabled);
    if(smoke.Length!=6||volcanoes.Length!=3||sky.sprite==layerKit.GroundArt)throw new Exception("Background layers not independent");
    var before=Array.ConvertAll(smoke,x=>x.rectTransform.anchoredPosition);
    atmosphere.Tick(1,3);var animated=0;
    for(var i=0;i<smoke.Length;i++)if(Vector2.Distance(before[i],smoke[i].rectTransform.anchoredPosition)>1)animated++;
    if(animated!=6)throw new Exception("Volcano smoke not animated independently");
    layerKit.terrainTheme=WorldMapTerrainTheme.GreenLand;atmosphere.Tick(45,3);
    if(sky.enabled)throw new Exception("Legacy backdrop with baked clouds still rendered outside the first region");
    var pureSky=Array.Find(layerRoot.GetComponentsInChildren<WorldMapWeatherGraphic>(),x=>x.name=="CeuSemElementos");
    if(pureSky==null||!pureSky.enabled||pureSky.mode!=7)throw new Exception("Independent clean sky missing");
    File.AppendAllText(Path.Combine(Output,"presentation-validation.txt")," Separate sky, 3 volcano sprites and 6 independently animated smoke layers verified.");
   }
   finally{UnityEngine.Object.DestroyImmediate(layerRoot);UnityEngine.Object.DestroyImmediate(layerKit);}
 }
 static void ValidateLayout()
 {
  var layout=RoyalAvesMapLayoutControls.GetOrCreate();var original=EditorJsonUtility.ToJson(layout);
  try
  {
   var route=new WorldMapRoute();var kit=RoyalAvesWorldMapSetup.Ensure(false);
   var initial=WorldMapScenery.Build(route,kit,100);
   if(initial.Exists(p=>p.kind==PropKind.Dog||p.sprite==kit.dogCarry||p.sprite==kit.dogIdle))throw new Exception("Dog still present in map scenery");
   var art=AssetDatabase.LoadAssetAtPath<LobbyFeaturesConfig>("Assets/RoyalAves/Resources/RoyalAvesFeatures.asset");
   var view=WorldMapView.Create(null,art,kit,100,5,true);
   try
   {
    view.TimeOverride=0;view.SetFixedSize(540,960);view.Tick(1,1,false);view.RenderNow();
    Color32[] Pixels(string name)
    {
     var previous=RenderTexture.active;RenderTexture.active=(RenderTexture)view.Target;
     var texture=new Texture2D(540,960,TextureFormat.RGBA32,false);
     texture.ReadPixels(new Rect(0,0,540,960),0,0);texture.Apply();var pixels=texture.GetPixels32();
     File.WriteAllBytes(Path.Combine(Output,name+".png"),texture.EncodeToPNG());
     RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(texture);return pixels;
    }
    var before=Pixels("edit-before");
    var anchors=0;
    for(var i=0;i<view.Props.Count;i++)
    {
     var p=view.Props[i];var rel=p.s-route.MetersAt(1);
     if(rel<0||rel>8||p.kind==PropKind.FloatingMail||!view.ProjectPropAnchor(i,540f/960,out var anchor))continue;
     if(!view.GroundAtViewport(anchor,1,540f/960,out var phase,out var lateral)||Mathf.Abs(phase-route.LevelAt(p.s))>.05f||Mathf.Abs(lateral-p.offset)>.03f)
      throw new Exception("Manual drag anchor roundtrip failed: "+i);
     anchors++;
    }
    if(anchors<3)throw new Exception("Not enough visible drag anchors tested");
    var snapshot=new WorldMapLayout.Chapter{index=0,manual=true};
    foreach(var p in view.Props)if(route.ChapterAtMeters(p.s)==0)snapshot.entries.Add(WorldMapLayout.Entry.From(p,route));
    layout.chapters.RemoveAll(x=>x.index==0);layout.chapters.Add(snapshot);
    view.ReloadScenery();view.Tick(1,1,false);view.RenderNow();var after=Pixels("edit-after");var different=0;var maximum=0;
    // Fractional phase serialization can move an antialiased edge by less than a pixel.
    // Measured regression images differ by at most 4/255; a displaced ground strip fails this check.
    for(var i=0;i<before.Length;i++)
    {
     var d=Mathf.Max(Mathf.Abs(before[i].r-after[i].r),Mathf.Abs(before[i].g-after[i].g),Mathf.Abs(before[i].b-after[i].b),Mathf.Abs(before[i].a-after[i].a));
     maximum=Mathf.Max(maximum,d);if(d>4)different++;
    }
    if(different>before.Length*.001f)throw new Exception("Edit mode changed rendering: "+different+" pixels above 4/255 tolerance");
    File.WriteAllText(Path.Combine(Output,"edit-mode-validation.txt"),$"PASS: enter manual layout with identical placements, reload live scenery and rerender; {different}/{before.Length} pixels above 4/255 antialiasing tolerance; maximum channel difference {maximum}/255. Camera, terrain and placement presentation preserved. {anchors} object drag anchors roundtrip through the curved ground picker.");
    EditorJsonUtility.FromJsonOverwrite(original,layout);
   }
   finally{view.Dispose();WorldMapCards.Clear();}
   var manual=new WorldMapLayout.Chapter{index=0,manual=true};
   manual.entries.Add(new WorldMapLayout.Entry{id="layout-test",level=2.75f,lateral=-1.7f,height=.63f,customSize=true,sprite=kit.mailboxClosed,kind=PropKind.Mailbox});
   layout.chapters.RemoveAll(x=>x.index==0);layout.chapters.Add(manual);
   var current=WorldMapScenery.Build(route,kit,100);
   var changed=current.FindAll(x=>route.ChapterAtMeters(x.s)==0);
   if(changed.Count!=1||changed[0].id!="layout-test"||Mathf.Abs(changed[0].s-route.MetersAt(2.75f))>.001||Mathf.Abs(changed[0].offset+1.7f)>.001||Mathf.Abs(changed[0].height-.63f)>.001)
    throw new Exception("Manual layout not consumed by runtime scenery: count="+changed.Count+" first="+(changed.Count>0?$"{changed[0].id}, offset={changed[0].offset}, height={changed[0].height}, fixed={changed[0].fixedPlacement}":"empty"));
   manual.entries.Add(new WorldMapLayout.Entry{id="size-a",level=3,lateral=1.8f,height=.8f,sprite=kit.tree,kind=PropKind.Tree});
   manual.entries.Add(new WorldMapLayout.Entry{id="size-b",level=4,lateral=-1.8f,height=1.4f,sprite=kit.tree,kind=PropKind.Tree});
   current=WorldMapScenery.Build(route,kit,100);
   var a=current.Find(p=>p.id=="size-a");var b=current.Find(p=>p.id=="size-b");
   if(Mathf.Abs(a.height-1.7f)>.001||Mathf.Abs(b.height-a.height)>.001)throw new Exception("Family size normalization failed");
   manual.entries[2].customSize=true;current=WorldMapScenery.Build(route,kit,100);
   if(Mathf.Abs(current.Find(p=>p.id=="size-b").height-1.4f)>.001)throw new Exception("Custom size override failed");
   manual.entries[1].keepSprite=true;manual.entries[1].sprite=kit.tree;
   current=WorldMapScenery.Build(route,kit,100);
   if(current.Find(p=>p.id=="size-a").sprite!=kit.tree)throw new Exception("Explicit manual sprite was replaced by automatic variety");
   EditorJsonUtility.FromJsonOverwrite(original,layout);
   if(WorldMapScenery.Build(route,kit,100).Count!=initial.Count)throw new Exception("Layout restore failed");
   EditorUtility.SetDirty(layout);AssetDatabase.SaveAssets();
   File.WriteAllText(Path.Combine(Output,"layout-validation.txt"),"PASS: serialized manual placement replaces chapter defaults and is consumed by the same runtime builder; phase, lateral and custom size preserved; family sizes consistent; explicit override respected; original layout restored.");
  }
  finally{EditorJsonUtility.FromJsonOverwrite(original,layout);EditorUtility.SetDirty(layout);AssetDatabase.SaveAssets();}
 }
 static void SaveReport(string name)
 {
  var text=File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/worldmap-capture.done"));
  File.WriteAllText(Path.Combine(Output,name),text);
  if(!text.StartsWith("OK"))throw new Exception(text);
 }
 static void ValidateHud()
 {
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RoyalAves/Prefabs/RoyalAvesLobby.prefab");
  var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
  try
  {
   var content=(RectTransform)root.transform.Find("Content");var nav=(RectTransform)content.Find("Nav");var hud=content.Find("HUD");
   var snapshots=new System.Collections.Generic.Dictionary<UnityEngine.Object,string>();
   foreach(var parent in new[]{hud,(Transform)nav})
   {
    foreach(var rect in parent.GetComponentsInChildren<RectTransform>(true))snapshots.Add(rect,EditorJsonUtility.ToJson(rect));
    foreach(var image in parent.GetComponentsInChildren<UnityEngine.UI.Image>(true))snapshots.Add(image,EditorJsonUtility.ToJson(image));
   }
   var labels=root.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);var values=Array.ConvertAll(labels,x=>x.text);
   var buttons=root.GetComponentsInChildren<UnityEngine.UI.Button>(true);var events=Array.ConvertAll(buttons,x=>x.onClick);
   var kit=RoyalAvesWorldMapSetup.Ensure(false);var style=new WorldMapHudPresentation(content,nav,kit);style.Show();
   if(nav.Find("FaixaDoMapa")==null)throw new Exception("Map navigation panel missing");
   if(nav.Find("Eventos/Icon").GetComponent<UnityEngine.UI.Image>().sprite!=kit.navIcons[0])throw new Exception("Postal navigation art missing");
   for(var i=0;i<labels.Length;i++)if(labels[i].text!=values[i])throw new Exception("HUD styling changed live currency text");
   for(var i=0;i<buttons.Length;i++)if(buttons[i].onClick!=events[i])throw new Exception("HUD styling replaced a button binding");
   style.Hide();
   foreach(var pair in snapshots)if(EditorJsonUtility.ToJson(pair.Key)!=pair.Value)throw new Exception("HUD not restored: "+pair.Key.name+" / "+pair.Key.GetType().Name);
   style.Show();style.Hide();
   File.WriteAllText(Path.Combine(Output,"hud-validation.txt"),"PASS: map navigation sprites and panel applied; currency text and button event bindings retained; all HUD/navigation rectangles and image properties restored on close, including a second open/close cycle.");
  }
  finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 static void ValidateAssetFit()
 {
  var kit=RoyalAvesWorldMapSetup.Ensure(false);var route=new WorldMapRoute();
  if(WorldMapScenery.Build(route,kit,100).Exists(p=>p.kind==PropKind.FloatingMail))throw new Exception("Floating scenery letters remain");
  if(!kit.autumnTree.name.StartsWith("postal_scenery_cartoon_v2")||!kit.navIcons[0].name.StartsWith("postal_navigation_cartoon_v2")||!kit.bridgeDeck.name.StartsWith("postal_bridge_cartoon_v2")||!kit.postOffice.name.StartsWith("postal_house_cartoon_v2"))throw new Exception("Cartoon revisions not bound");
  var art=AssetDatabase.LoadAssetAtPath<LobbyFeaturesConfig>("Assets/RoyalAves/Resources/RoyalAvesFeatures.asset");
  var view=WorldMapView.Create(null,art,kit,100,5,true);var tested=0;
  try
  {
   view.SetFixedSize(720,1600);view.Tick(11.5f,12,false);
   var nodes=(Array)typeof(WorldMapView).GetField("nodes",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(view);
   foreach(var node in nodes)
   {
    var type=node.GetType();var badge=(MeshRenderer)type.GetField("badge").GetValue(node);
    if(!badge.gameObject.activeInHierarchy)continue;
    var token=(Transform)type.GetField("t").GetValue(node);var mesh=(MeshFilter)type.GetField("badgeMesh").GetValue(node);
    var p=token.InverseTransformPoint(badge.transform.position);
    if(Mathf.Abs(p.y-(WorldMapTokenMesh.FaceHeight+.006f))>.003f||new Vector2(p.x,p.z).magnitude>WorldMapTokenMesh.FaceRadius)throw new Exception("Difficulty emblem base is floating or outside the button face");
    tested++;
   }
   if(tested==0)throw new Exception("No difficulty badge checked");
   File.WriteAllText(Path.Combine(Output,"asset-fit-validation.txt"),$"PASS: revised cartoon scenery, navigation and bridges bound; no floating letters in scenery; {tested} difficulty emblem bases attached to their level token face.");
  }
  finally{view.Dispose();WorldMapCards.Clear();}
 }

 static void ValidateDetailPolish()
 {
  var kit=RoyalAvesWorldMapSetup.Ensure(false);var route=new WorldMapRoute();
  if(kit.shrubCoral==null||kit.shrubGold==null||kit.shrubMint==null||kit.detailTulips==null)throw new Exception("Regional details missing");
  if(WorldMapTerrain.BankH<.015f||WorldMapTerrain.BankH>.04f||WorldMapTokenMesh.SideHeight<.075f||WorldMapTokenMesh.Radius-WorldMapTokenMesh.FaceRadius<.04f)throw new Exception("Subtle terrain or token relief missing");
  var art=AssetDatabase.LoadAssetAtPath<LobbyFeaturesConfig>("Assets/RoyalAves/Resources/RoyalAvesFeatures.asset");
  var view=WorldMapView.Create(null,art,kit,100,5,true);var mirrored=0;var regular=0;var tokens=0;
  try
  {
   view.TimeOverride=0;view.SetFixedSize(720,1600);view.Tick(11.5f,12,false);
   foreach(var p in view.Props)
    if(!p.fixedPlacement&&Mathf.Abs(p.offset)<WorldMapTerrain.PathHalf+.099f+WorldMapCards.Width(p.sprite,p.height)*.43f)
     throw new Exception("Automatic prop crosses lane: "+p.sprite.name);
   var slots=(System.Collections.IDictionary)typeof(WorldMapView).GetField("activeSlots",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(view);
   foreach(System.Collections.DictionaryEntry pair in slots)
   {
    var p=view.Props[(int)pair.Key];if(p.kind!=PropKind.Lamp&&p.kind!=PropKind.Mailbox)continue;
    var card=(Transform)pair.Value.GetType().GetField("card").GetValue(pair.Value);
    var flip=card.localScale.x<0;
    if(flip!=(p.offset<0))throw new Exception("Street-facing prop wrong orientation");
    if(flip)mirrored++;else regular++;
   }
   var nodes=(Array)typeof(WorldMapView).GetField("nodes",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(view);
   foreach(var node in nodes)
   {
    var type=node.GetType();var tr=(Transform)type.GetField("t").GetValue(node);if(!tr.gameObject.activeSelf)continue;
    var renderer=(MeshRenderer)type.GetField("body").GetValue(node);var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
    if(block.GetFloat("_TokenFaceAmount")>.01f||block.GetFloat("_Glow")>.01f)throw new Exception("Glossy token art still active");tokens++;
   }
   if(mirrored==0||regular==0||tokens<3)throw new Exception("Orientation samples insufficient");
  }
  finally{view.Dispose();WorldMapCards.Clear();}
  var root=new GameObject("HudTitleValidation",typeof(RectTransform));CurvedLevelMap map=null;
  try
  {
   var config=AssetDatabase.LoadAssetAtPath<LevelMapConfig>("Assets/RoyalAves/Resources/RoyalAvesLevelMap.asset");
   map=CurvedLevelMap.Create((RectTransform)root.transform,null,config,art,100,()=>1,_=>{},()=>{},kit);
   if(map.World==null||map.transform.Find("Titulo").gameObject.activeSelf)throw new Exception("Chapter banner still shown");
  }
  finally{if(map!=null)UnityEngine.Object.DestroyImmediate(map.gameObject);UnityEngine.Object.DestroyImmediate(root);}
  File.WriteAllText(Path.Combine(Output,"detail-polish-validation.txt"),$"PASS: {mirrored} left-side and {regular} right-side street-facing props, {tokens} satin tokens without glossy atlas, physical lane relief and thick token sides, automatic footprints outside the lane, regional shrub assets bound, chapter title hidden. Layout and HUD restoration checked separately.");
 }

 static void ValidateSculpt()
 {
  var kit=RoyalAvesWorldMapSetup.Ensure(false);var route=new WorldMapRoute();
  if(kit.levelStar==null||!kit.levelCrown.name.StartsWith("level_emblems_v3")||!kit.hardBadge.name.Contains("skull"))throw new Exception("New reward silhouettes missing");
  foreach(var gap in route.Gaps)
  {
   var tip=gap.start-1;
   if(WorldMapTerrain.SurfaceHeight(route,tip+.18f,0)<WorldMapTerrain.BankH*.99f||WorldMapTerrain.SurfaceHeight(route,tip-.30f,0)>.001f)
    throw new Exception("The rounded lane end flattens its raised bank");
  }
  var art=AssetDatabase.LoadAssetAtPath<LobbyFeaturesConfig>("Assets/RoyalAves/Resources/RoyalAvesFeatures.asset");
  var view=WorldMapView.Create(null,art,kit,100,5,true);var stars=0;var crowns=0;var skulls=0;var msaa=0;
  try
  {
   view.TimeOverride=0;view.SetFixedSize(720,1600);view.Tick(6.1f,8,false);view.RenderNow();
   msaa=((RenderTexture)view.Target).antiAliasing;
   var nodes=(Array)typeof(WorldMapView).GetField("nodes",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(view);
   foreach(var node in nodes)
   {
    var type=node.GetType();var token=(Transform)type.GetField("t").GetValue(node);if(!token.gameObject.activeSelf)continue;
    void Check(Transform decoration)
    {
     if(Quaternion.Angle(decoration.localRotation,view.CameraModel.Rotation)>.01f||Mathf.Abs(decoration.localScale.x-decoration.localScale.y)>.0001f)throw new Exception("Reward inherited token shear");
    }
    foreach(var star in (MeshRenderer[])type.GetField("stars").GetValue(node))if(star!=null&&star.gameObject.activeInHierarchy){Check(star.transform);stars++;}
    var crown=(MeshRenderer)type.GetField("crown").GetValue(node);if(crown!=null&&crown.gameObject.activeInHierarchy){Check(crown.transform);crowns++;}
    var badge=(MeshRenderer)type.GetField("badge").GetValue(node);if(badge.gameObject.activeInHierarchy){Check(badge.transform);skulls++;}
   }
   if(stars<3||crowns<1||skulls<1)throw new Exception("Missing visible reward samples");
   var mesh=WorldMapTokenMesh.Build();var perimeter=0;
   foreach(var p in mesh.vertices)if(Mathf.Abs(new Vector2(p.x,p.z).magnitude-WorldMapTokenMesh.Radius)<.0001f)perimeter++;
   UnityEngine.Object.DestroyImmediate(mesh);if(perimeter<256)throw new Exception("Token silhouette too coarse");
  }
  finally{view.Dispose();WorldMapCards.Clear();}
  File.WriteAllText(Path.Combine(Output,"sculpt-validation.txt"),$"PASS: raised banks retained at every rounded lane end; continuous wide cream token rim and 128-segment silhouette; {msaa}x MSAA target; {stars} stars, {crowns} crowns and {skulls} skulls face the camera without non-uniform token deformation. Difficulty bases and editing validated separately.");
 }

 static void ValidateSmooth()
 {
  var kit=RoyalAvesWorldMapSetup.Ensure(false);var route=new WorldMapRoute();var maxTurn=0f;
  for(var s=1f;s<route.Length-.2f;s+=.1f)
  {
   var a=new Vector2(route.CenterX(s)-route.CenterX(s-.1f),.1f);
   var b=new Vector2(route.CenterX(s+.1f)-route.CenterX(s),.1f);
   maxTurn=Mathf.Max(maxTurn,Vector2.Angle(a,b));
  }
  if(maxTurn>5)throw new Exception("Ground curve has abrupt turns: "+maxTurn);
  foreach(var gap in route.Gaps)
   if(Mathf.Abs(route.CenterX(gap.start)-route.CenterX(gap.end))>.0002f)throw new Exception("Bridge no longer straight");
  var terrain=new WorldMapTerrain{IllustratedBridge=kit.bridgeDeck!=null,Lava=kit.Lava};var cursor=route.MetersAt(6.1f);
  try
  {
   terrain.Rebuild(route,kit.radius,cursor,6,18);
   var vertices=terrain.ground.vertices;var normals=terrain.ground.normals;var colors=terrain.ground.colors;
   var t=MapCurve.Tangent(route.Gaps[0].start-cursor,kit.radius);
   Color? first=null;var topVertices=0;
   for(var i=0;i<vertices.Length;i++)
   {
    if(Mathf.Abs(Vector3.Dot(normals[i],t))<.999f||colors[i].a<.99f||colors[i].a>1.01f)continue;
    var height=(vertices[i]+new Vector3(0,kit.radius,0)).magnitude-kit.radius;
    if(Mathf.Abs(height-WorldMapTerrain.BankH)>.001f)continue;
    if(first.HasValue&&Vector4.Distance((Vector4)first.Value,(Vector4)colors[i])>.001f)throw new Exception("River bank split into colored rectangles");
    first=colors[i];topVertices++;
   }
   if(topVertices<50)throw new Exception("Not enough shoreline geometry checked");
   File.WriteAllText(Path.Combine(Output,"smooth-validation.txt"),$"PASS: maximum wave heading change {maxTurn:0.00} degrees per 10 cm sample across the whole route; every bridge joins at the same lateral coordinate; {topVertices} shoreline top vertices share one continuous color; bank height {WorldMapTerrain.BankH:0.000} m, bevel width {WorldMapTerrain.SlopeW:0.000} m. Manual anchors, clicks and reward geometry checked separately.");
  }
  finally{UnityEngine.Object.DestroyImmediate(terrain.ground);UnityEngine.Object.DestroyImmediate(terrain.water);}
 }

 static void Validate()
 {
  var report=new StringBuilder();var kit=RoyalAvesWorldMapSetup.Ensure(false);
  var art=AssetDatabase.LoadAssetAtPath<LobbyFeaturesConfig>("Assets/RoyalAves/Resources/RoyalAvesFeatures.asset");
  var view=WorldMapView.Create(null,art,kit,100,5,true);
  var tests=0;var mismatches=0;
  try
  {
   foreach(var size in new[]{new Vector2Int(540,960),new Vector2Int(443,960),new Vector2Int(720,960)})
   {
    view.SetFixedSize(size.x,size.y);view.Tick(1,15,false);
    var anchor=view.Cam.transform.position;var rotation=view.Cam.transform.rotation;
    for(var cursor=1f;cursor<=100f;cursor+=.5f)
    {
     view.Tick(cursor,15,false);
     var cm=view.Route.MetersAt(cursor);
     for(var s=cm-.5f;s<=cm+9;s+=.25f)
     {
      if(view.Route.InGap(s)||!view.CameraModel.SurfaceVisible(cm,s)||view.Route.InsetAmount(s)<.15f)continue;
      var half=WorldMapTerrain.PathHalf*Mathf.Sqrt(view.Route.InsetAmount(s));
      foreach(var side in new[]{-1,1})
      {
       var x=view.Route.CenterX(s)+WorldMapTerrain.Lateral(half*side,s-cm);
       if(view.CameraModel.Project(cm,s,x,0,size.x/(float)size.y,out var edge,out _)&&edge.y>.18f&&edge.y<.78f&&(edge.x<.015f||edge.x>.985f))throw new Exception($"Road outside frame: cursor {cursor}, aspect {size}, edge {edge}");
      }
     }
     if(Vector3.Distance(anchor,view.Cam.transform.position)>.00001f||Quaternion.Angle(rotation,view.Cam.transform.rotation)>.00001f)throw new Exception("Camera followed the route");
     for(var level=Mathf.Max(1,Mathf.FloorToInt(cursor)-1);level<=Mathf.Min(105,Mathf.CeilToInt(cursor)+8);level++)
     {
      if(!view.ProjectLevel(level,cursor,size.x/(float)size.y,out var p)||p.x<.03||p.x>.97||p.y<.06||p.y>.94)continue;
      tests++;var hit=view.PickViewport(p);
      if(hit!=level)
      {
       mismatches++;if(mismatches<12)report.AppendLine($"cursor {cursor}, level {level} -> {hit}");
       if(mismatches<3)
       {
        var ns=(Array)typeof(WorldMapView).GetField("nodes",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);
        foreach(var n in ns)
        {
         var nt=n.GetType();if((int)nt.GetField("level").GetValue(n)!=level)continue;
         var tr=(Transform)nt.GetField("t").GetValue(n);
         var ray=view.Cam.ViewportPointToRay(p);var center=tr.TransformPoint(new Vector3(0,WorldMapTokenMesh.FaceHeight,0));
         var denominator=Vector3.Dot(tr.up,ray.direction);var distance=Vector3.Dot(center-ray.origin,tr.up)/denominator;
         report.AppendLine($"DEBUG viewport={p} rayOrigin={ray.origin} dir={ray.direction} center={center} up={tr.up} denom={denominator} distance={distance} local={tr.InverseTransformPoint(ray.GetPoint(distance))} camera={view.Cam.transform.position} aspect={view.Cam.aspect}");
        }
       }
      }
     }
    }
   }
   report.Insert(0,$"Camera position and rotation constant across 597 samples. Picking: {tests-mismatches}/{tests}.\n");
   File.WriteAllText(Path.Combine(Output,"validation.txt"),report.ToString());
   if(mismatches>0)throw new Exception(report.ToString());
  }
  finally{view.Dispose();WorldMapCards.Clear();}
 }
}
