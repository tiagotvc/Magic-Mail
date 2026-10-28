using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    // Original PNG pixels stay untouched; sprite rectangles trim transparent margins only.
    internal static class RoyalPostmanPetProfileArt
    {
        public static void ImportAll()
        {
            foreach(var id in new[]{"progress-track","progress-blue","progress-green","badge-rare","badge-common","badge-epic","card-rare-bg","card-common-bg","card-epic-bg","collection-panel","card-frame"})Import("Menu","pets-"+id+"-v16");
            foreach(var id in new[]{"status-equipped","status-common","status-epic","sort","feed-button"})Import("Button","pets-"+id+"-v16");
            foreach(var id in new[]{"level-paw","energy-bolt","equipped-check","feed-bag","info-lavender","info-blue","tab-paw","tab-shirt","tab-seal","tab-contract"})Import("Icon","pets-"+id+"-v16");
            Import("Tab","pets-tab-active-v16");Import("Tab","pets-tab-inactive-v16");
        }
        static void Import(string group,string id)
        {
            string path="Assets/RoyalAves/Art/UI/"+group+"/RoyalPostman/Pets/"+id+".png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var read=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(read,File.ReadAllBytes(path));
            var pixels=read.GetPixels32();int w=read.width,h=read.height;
            // Ignore disconnected alpha flecks while retaining all five pieces of standalone paws.
            var visited=new bool[pixels.Length];var queue=new int[pixels.Length];int left=w,right=0,bottom=h,top=0;
            for(int seed=0;seed<pixels.Length;seed++){
                if(visited[seed]||pixels[seed].a<32)continue;
                int head=0,tail=1;queue[0]=seed;visited[seed]=true;int l=w,r=0,b=h,t=0;
                while(head<tail){int at=queue[head++],x=at%w,y=at/w;l=Math.Min(l,x);r=Math.Max(r,x);b=Math.Min(b,y);t=Math.Max(t,y);
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int nx=x+dx,ny=y+dy;if(nx<0||nx>=w||ny<0||ny>=h)continue;int n=ny*w+nx;if(visited[n]||pixels[n].a<32)continue;visited[n]=true;queue[tail++]=n;}
                }
                if(tail<256)continue;left=Math.Min(left,l);right=Math.Max(right,r);bottom=Math.Min(bottom,b);top=Math.Max(top,t);
            }
            if(left>=right)throw new InvalidOperationException("Empty sprite: "+id);
            var rect=Rect.MinMaxRect(Math.Max(0,left-2),Math.Max(0,bottom-2),Math.Min(w,right+3),Math.Min(h,top+3));
            UnityEngine.Object.DestroyImmediate(read);
            bool capsule=id.Contains("progress-");bool tab=id=="pets-tab-active-v16"||id=="pets-tab-inactive-v16";
            var border=capsule?new Vector4(rect.height*.49f,rect.height*.45f,rect.height*.49f,rect.height*.45f):tab?new Vector4(rect.height*.22f,8,rect.height*.22f,rect.height*.22f):Vector4.zero;
            if(id=="pets-collection-panel-v16")border=Vector4.one*110;
            var sprite=Sprite.Create(texture,rect,Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border);sprite.name=id;
            var folder=Path.GetDirectoryName(path).Replace('\\','/')+"/Sprites";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            string destination=folder+"/"+id+".asset";var previous=AssetDatabase.LoadAssetAtPath<Sprite>(destination);
            if(previous==null)AssetDatabase.CreateAsset(sprite,destination);else{EditorUtility.CopySerialized(sprite,previous);EditorUtility.SetDirty(previous);UnityEngine.Object.DestroyImmediate(sprite);}
        }
    }
}
