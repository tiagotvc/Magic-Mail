using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using RoyalAves.Meta;
using RoyalAves.Meta.WorldMap;
using RoyalAves.EditorTools;

public static class TreeV9ValidationBatch
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/v9";
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void Execute()
    {
        Directory.CreateDirectory(Output);
        var source=RoyalAvesWorldMapSetup.Ensure(true);
        Check(source.postalTrees.Length==3&&source.postalTrees.All(x=>x!=null),"Tree variants missing");
        Check(source.postalTreeDetails.Length==4&&source.postalTreeDetails.All(x=>x!=null)&&source.treeLeafShader!=null,"Tree details missing");
        var view=WorldMapView.Create(null,Resources.Load<LobbyFeaturesConfig>("RoyalAvesFeatures"),source,WorldMapLevelCatalog.Count,0,true);
        try
        {
            view.SetFixedSize(540,1200);view.TimeOverride=0;view.Tick(1,1,false);
            var route=view.Route;var kit=view.CameraModel.Kit;
            Check(route.InsetAmount(route.Pos(1)-1.6f)==0,"Entrance is not grass");
            Check(route.InsetAmount(route.Pos(1))>.99f,"Road does not reach first medal");
            Check(view.Props.Count(x=>x.kind==PropKind.Tree&&x.s<route.Pos(1))>=2,"Entrance trees missing");
            Check(view.GetComponentsInChildren<MeshFilter>().Count(x=>x.sharedMesh==WorldMapCards.Get(source.postalTrees[0],1.6f)||x.sharedMesh==WorldMapCards.Get(source.postalTrees[2],1.48f))>=2,"Foreground trees were culled");
            Check(!view.Props.Any(x=>x.sprite==source.simpleNature[0]),"Old tree with static leaf remains");
            Check(view.CursorForLevel(1)==1,"Game does not open on lawn entrance");
            Check(view.ProjectLevel(1,1,.45f,out var first)&&first.y>.23f&&first.y<.6f,"First medal is too low");
            var old=new WorldMapLayout.Entry{sprite=source.simpleNature[0],keepSprite=true,customSize=true,height=1.93f,lateral=-2.1f,level=4.2f,kind=PropKind.Tree};
            var upgraded=old.Placement(route,kit);
            Check(upgraded.sprite==source.postalTrees[0]&&upgraded.height==1.93f&&upgraded.offset==-2.1f,"Saved tree position/size lost during art upgrade");
            var hanger=view.GetComponentsInChildren<Transform>().FirstOrDefault(x=>x.name=="CartaPendurada_0");
            Check(hanger!=null,"No attached letters");var rotation=hanger.localRotation;var anchor=hanger.localPosition;
            view.TimeOverride=.7f;view.Tick(1,1,false);
            Check(Quaternion.Angle(rotation,hanger.localRotation)>.1f,"Hanging letter does not sway");
            Check(Vector3.Distance(anchor,hanger.localPosition)<.0001f,"Letter detached from branch");
            var poses=Enumerable.Range(0,80).Select(i=>WorldMapTreeDecor.LeafAt(54,0,i*.1f,1.6f,1.65f)).ToArray();
            Check(poses.Any(x=>x.alpha==0)&&poses.Any(x=>x.alpha>.9f)&&poses.Any(x=>x.alpha>.1f&&x.alpha<.8f),"Leaf has no intermittent fade");
            var visible=poses.Where(x=>x.alpha>.1f).ToArray();
            Check(visible.Max(x=>x.position.y)-visible.Min(x=>x.position.y)>.5f,"Leaf does not fall");
            Check(Mathf.Abs(WorldMapTreeDecor.LeafAt(54,0,1,1,1).alpha-WorldMapTreeDecor.LeafAt(55,0,1,1,1).alpha)>.01f,"Trees emit in sync");
            var allowed=source.simpleNature.Concat(source.simplePostal).Concat(source.postalTrees).ToArray();
            Check(view.Props.All(x=>allowed.Contains(x.sprite)),"Unexpected scenery");
            var samples=new[]{1,2,3,4,9,20,21,40,41,80,100}.Where(x=>x<=WorldMapLevelCatalog.Count);
            foreach(var level in samples)
            {
                var cursor=view.CursorForLevel(level);view.Tick(cursor,level,false);
                Check(view.ProjectLevel(level,cursor,.45f,out var uv)&&view.PickViewport(uv)==level,"Picking missed phase "+level);
            }
            view.Tick(1,1,false);
            int count=view.GetComponentsInChildren<Transform>(true).Count(x=>x.name=="EnfeitesDaArvore");
            for(var i=0;i<6;i++){view.Tick(i%2==0?25:1,5,false);}
            Check(view.GetComponentsInChildren<Transform>(true).Count(x=>x.name=="EnfeitesDaArvore")<=count+12,"Tree pool keeps allocating");
        }
        finally{view.Dispose();}
        var window=ScriptableObject.CreateInstance<RoyalAvesLevelMapPreview>();var t=typeof(RoyalAvesLevelMapPreview);
        try
        {
            foreach(var shot in new[]{("entrance",1f,1),("progress",1f,5),("route",8f,8),("bridge",20.8f,21),("grove",25f,25)})
            {
                t.GetField("level",Private).SetValue(window,shot.Item2);
                t.GetMethod("Build",Private).Invoke(window,new object[]{720,1600,shot.Item3});
                var map=(CurvedLevelMap)t.GetField("map",Private).GetValue(window);map.World.TimeOverride=1;
                t.GetMethod("Save",Private).Invoke(window,new object[]{Path.Combine(Output,shot.Item1+".png"),false});
            }
        }
        finally{t.GetMethod("Cleanup",Private).Invoke(window,null);UnityEngine.Object.DestroyImmediate(window);}
        TreeMotion(source);
        File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: three variants and four independent detail sprites; saved-tree upgrade preserves anchors/sizes; two entrance trees; rounded road starts on green lawn; first medal raised; letter cord anchors stay fixed while swaying; leaf falls/spins/fades intermittently with distinct timing; pooled decorations; phase picking; native Unity map and motion captures. Editor/game use the same route, shaders and scenery code. Phone performance not measured.");
    }
    static void TreeMotion(WorldMapKit kit)
    {
        var root=new GameObject("TreeCloseup");int layer=LayerMask.NameToLayer(WorldMapKit.LayerName);root.layer=layer;
        var card=new Material(kit.card);card.SetTexture("_MainTex",kit.postalTrees[0].texture);
        var detail=new Material(kit.card);detail.SetTexture("_MainTex",kit.postalTreeDetails[0].texture);
        var leaf=new Material(kit.treeLeafShader);leaf.SetTexture("_MainTex",kit.postalTreeDetails[0].texture);
        var cameraObject=new GameObject("TreeCamera");var camera=cameraObject.AddComponent<Camera>();
        var target=new RenderTexture(640,640,24,RenderTextureFormat.ARGB32);
        try
        {
            root.AddComponent<MeshFilter>().sharedMesh=WorldMapCards.Get(kit.postalTrees[0],3);
            root.AddComponent<MeshRenderer>().sharedMaterial=card;
            var decor=new WorldMapTreeDecor(root.transform,layer,_=>detail,leaf);
            var placement=new PropPlacement{sprite=kit.postalTrees[0],height=3,kind=PropKind.Tree,seed=54};
            camera.transform.position=new Vector3(0,1.6f,-7);camera.orthographic=true;camera.orthographicSize=2.1f;
            camera.cullingMask=1<<layer;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.allowHDR=false;
            camera.GetUniversalAdditionalCameraData().SetRenderer(kit.rendererIndex);
            target.Create();camera.targetTexture=target;
            for(int i=0;i<32;i++)
            {
                float time=i*7.8f/32;
                root.transform.localScale=new Vector3(1+Mathf.Sin(time*1.7f)*.009f,1+Mathf.Sin(time*1.7f)*.014f,1);
                decor.Tick(kit,placement,kit.postalTrees[0],time,false);camera.Render();
                var previous=RenderTexture.active;RenderTexture.active=target;
                var image=new Texture2D(640,640,TextureFormat.RGBA32,false);
                image.ReadPixels(new Rect(0,0,640,640),0,0);image.Apply();
                Check(image.GetPixels32().Count(x=>x.a>200)>10000,"Empty tree closeup");
                File.WriteAllBytes(Path.Combine(Output,"tree-"+i.ToString("00")+".png"),image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;
            }
        }
        finally
        {
            camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(card);UnityEngine.Object.DestroyImmediate(detail);UnityEngine.Object.DestroyImmediate(leaf);
        }
    }
}
