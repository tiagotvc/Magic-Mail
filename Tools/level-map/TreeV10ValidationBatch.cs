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

public static class TreeV10ValidationBatch
{
    const string Output="C:/Users/Tiago Carvalho/Documents/projetos/Magic-Mail/Tools/level-map/previews/v11";
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void Execute()
    {
        Directory.CreateDirectory(Output);var source=RoyalAvesWorldMapSetup.Ensure(true);
        Check(source.entranceTrees.Length==2&&source.entranceTrees.All(x=>x!=null),"Color variants missing");
        Check(WorldMapTreeDecor.LeafRelativeSize>.12f&&WorldMapTreeDecor.LeafRelativeSize<.15f,"Leaf size not gently increased");
        foreach(float side in new[]{-2f,2f})
        {
            var a=WorldMapTreeDecor.LeafAt(0,0,.4f,1.5f,1.6f,side);
            var z=WorldMapTreeDecor.LeafAt(0,0,2.4f,1.5f,1.6f,side);
            Check(side*(z.position.x-a.position.x)<0,"Leaves travel away from lane");
            Check(z.position.y<a.position.y&&a.position.y-z.position.y<.30f,"Leaf arc drops too far or rises");
            Check(Mathf.Abs(z.position.x)<1.0f,"Leaf travels too far from canopy");
            var start=WorldMapTreeDecor.LeafAt(0,0,0,1.5f,1.6f,side);
            Check(Mathf.Abs(start.position.x)<.15f,"Leaf no longer starts in canopy center");
            var middle=WorldMapTreeDecor.LeafAt(0,0,1.4f,1.5f,1.6f,side);
            Check(Mathf.Abs(middle.position.y-(a.position.y+z.position.y)*.5f)>.025f,"Leaf path is straight");
            Check(Mathf.Abs(z.angle-a.angle)>30,"Leaves no longer turn");
        }
        var upper=WorldMapTreeDecor.LeafAt(0,0,0,1,1);
        var lower=WorldMapTreeDecor.LeafAt(0,1,-WorldMapTreeDecor.LeafPeriod/WorldMapTreeDecor.LeafCount,1,1);
        Check(upper.position.y-lower.position.y>.1f,"Two emission lines missing");
        var samples=Enumerable.Range(0,128).Select(i=>Enumerable.Range(0,WorldMapTreeDecor.LeafCount)
            .Count(n=>WorldMapTreeDecor.LeafAt(0,n,i*WorldMapTreeDecor.LeafPeriod/128,1,1,-2).alpha>.1f)).ToArray();
        Check(samples.Average()>2.1&&samples.Max()<=4,"Leaf density is wrong");
        var view=WorldMapView.Create(null,Resources.Load<LobbyFeaturesConfig>("RoyalAvesFeatures"),source,WorldMapLevelCatalog.Count,0,true);
        try
        {
            view.SetFixedSize(540,1200);
            foreach(float time in new[]{0f,.8f,2.1f,4f,6f})
            {
                view.TimeOverride=time;view.Tick(1,1,false);
                foreach(var sprite in source.entranceTrees)
                {
                    var mesh=view.GetComponentsInChildren<MeshFilter>().FirstOrDefault(x=>x.sharedMesh!=null&&x.sharedMesh.name=="Card_"+sprite.name);
                    Check(mesh!=null,"Entrance tree is not visible");
                    var tree=mesh.transform;
                    Check(!tree.GetComponentsInChildren<Transform>().Any(x=>x.name.StartsWith("FolhaSolta_")),"Entrance emits leaves");
                    Check(tree.GetComponentsInChildren<Transform>().Count(x=>x.name.StartsWith("CartaPendurada_"))==2,"Entrance lost hanging letters");
                }
            }
            var entry=new WorldMapLayout.Entry{sourceId="entrance-tree-0",sprite=source.postalTrees[0],keepSprite=true,customSize=true,height=1.77f,lateral=-2.2f,level=-.9f,kind=PropKind.Tree};
            var p=entry.Placement(view.Route,view.CameraModel.Kit);
            Check(p.sprite==source.entranceTrees[0]&&p.height==1.77f&&p.offset==-2.2f,"Saved entrance upgrade changes authored placement");
        }
        finally{view.Dispose();}
        var window=ScriptableObject.CreateInstance<RoyalAvesLevelMapPreview>();var t=typeof(RoyalAvesLevelMapPreview);
        try
        {
            foreach(var shot in new[]{("entrance",1f,1),("route",8f,8)})
            {
                t.GetField("level",Private).SetValue(window,shot.Item2);t.GetMethod("Build",Private).Invoke(window,new object[]{720,1600,shot.Item3});
                var map=(CurvedLevelMap)t.GetField("map",Private).GetValue(window);map.World.TimeOverride=1;
                t.GetMethod("Save",Private).Invoke(window,new object[]{Path.Combine(Output,shot.Item1+".png"),false});
            }
        }
        finally{t.GetMethod("Cleanup",Private).Invoke(window,null);UnityEngine.Object.DestroyImmediate(window);}
        Motion(source);
        File.WriteAllText(Path.Combine(Output,"validation.txt"),"PASS: two short curved leaf streams start in the middle of the canopy; both move toward the lane with a gentle downward arc; reduced reach; density/size retained; entrance trees emit no leaves; hanging letters and saved placements preserved. Native Unity map captures and32 animation frames. Phone performance not measured.");
    }
    static void Motion(WorldMapKit kit)
    {
        var root=new GameObject("LeafMotionProof");int layer=LayerMask.NameToLayer(WorldMapKit.LayerName);
        var card=new Material(kit.card);card.SetTexture("_MainTex",kit.postalTrees[0].texture);
        var detail=new Material(kit.card);detail.SetTexture("_MainTex",kit.postalTreeDetails[0].texture);
        var leaf=new Material(kit.treeLeafShader);leaf.SetTexture("_MainTex",kit.postalTreeDetails[0].texture);
        var cameraObject=new GameObject("TreeCamera");var camera=cameraObject.AddComponent<Camera>();var target=new RenderTexture(960,640,24,RenderTextureFormat.ARGB32);
        try
        {
            var roots=new Transform[2];var effects=new WorldMapTreeDecor[2];var placements=new PropPlacement[2];
            for(int side=0;side<2;side++)
            {
                var tree=new GameObject(side==0?"LeftTree":"RightTree"){layer=layer};tree.transform.SetParent(root.transform,false);
                tree.transform.localPosition=new Vector3(side==0?-2.25f:2.25f,0,0);
                var sprite=kit.postalTrees[side==0?0:2];roots[side]=tree.transform;
                tree.AddComponent<MeshFilter>().sharedMesh=WorldMapCards.Get(sprite,2.5f);tree.AddComponent<MeshRenderer>().sharedMaterial=card;
                effects[side]=new WorldMapTreeDecor(tree.transform,layer,_=>detail,leaf);
                placements[side]=new PropPlacement{sprite=sprite,height=2.5f,kind=PropKind.Tree,seed=54+side,offset=side==0?-2:2};
            }
            camera.transform.position=new Vector3(0,1.4f,-7);camera.orthographic=true;camera.orthographicSize=2.75f;camera.cullingMask=1<<layer;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.allowHDR=false;
            camera.GetUniversalAdditionalCameraData().SetRenderer(kit.rendererIndex);target.Create();camera.targetTexture=target;
            for(int i=0;i<32;i++)
            {
                float time=i*WorldMapTreeDecor.LeafPeriod/32;
                for(int side=0;side<2;side++)effects[side].Tick(kit,placements[side],placements[side].sprite,time,false);
                camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
                var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(Output,"tree-"+i.ToString("00")+".png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;
            }
        }
        finally
        {
            camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(card);UnityEngine.Object.DestroyImmediate(detail);UnityEngine.Object.DestroyImmediate(leaf);
        }
    }
}
