using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalAves.Meta.WorldMap
{
    // One transient actor, drawn behind a real scenery card. No authoring layout is modified.
    public sealed class WorldMapSantaEvent : IDisposable
    {
        const float Rise=.55f,Fall=.45f;
        readonly WorldMapView world;
        readonly WorldMapKit kit;
        readonly Func<Texture,Material> material;
        readonly GameObject root;
        readonly MeshFilter mesh;
        readonly MeshRenderer drawing;
        readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
        readonly System.Random random=new System.Random();
        readonly List<int> candidates=new List<int>();
        bool initialized,active,evil,caught;
        float next,start,height,clipBelow;
        int host=-1,lastHost=-1;
        Sprite sprite;
        Rect exposed;
        public bool Enabled=>kit.santaEventEnabled&&(kit.santaFriendly!=null||kit.santaVillain!=null);
        public bool NeedsFrames(float now)=>Enabled&&(active||!initialized||now>=next);
        public int HostIndex=>host;
        public bool Evil=>evil;
        public bool Active=>active&&root.activeInHierarchy;
        public WorldMapSantaEvent(WorldMapView world,WorldMapKit kit,int layer,Func<Texture,Material> material)
        {
            this.world=world;this.kit=kit;this.material=material;
            root=new GameObject("NoelSurpresa",typeof(MeshFilter),typeof(MeshRenderer)){layer=layer,hideFlags=world.gameObject.hideFlags};
            root.transform.SetParent(world.transform,false);mesh=root.GetComponent<MeshFilter>();drawing=root.GetComponent<MeshRenderer>();
            drawing.shadowCastingMode=ShadowCastingMode.Off;drawing.receiveShadows=false;root.SetActive(false);
        }
        public void Reset()
        {
            active=initialized=caught=false;host=-1;root.SetActive(false);root.transform.SetParent(world.transform,false);
        }
        void Wait(float now)
        {
            active=false;host=-1;root.SetActive(false);root.transform.SetParent(world.transform,false);
            next=now+Mathf.Max(2,kit.santaInterval)*(float)(.8+random.NextDouble()*.4);
        }
        bool HostVisible(int index)
        {
            if(index<0||index>=world.Props.Count||world.SceneryCard(index)==null)return false;
            var p=world.Props[index];
            // Small also includes signs, parcels and benches: identify actual shrubs by their art.
            var art=world.SceneryCard(index).GetComponent<MeshRenderer>().sharedMaterial.mainTexture;
            var name=((art!=null?art.name:"")+" "+(p.sprite!=null?p.sprite.name:"")).ToLowerInvariant();
            var shrub=p.sprite!=null&&(p.sprite==kit.shrub||p.sprite==kit.shrubCoral||p.sprite==kit.shrubGold||p.sprite==kit.shrubMint)||
                name.Contains("shrub")||name.Contains("bush")||name.Contains("arbusto");
            if(p.height<.32f||!shrub)return false;
            return world.ProjectProp(index,world.Cam.aspect,out var rect,out _)&&rect.center.x>.08f&&rect.center.x<.92f&&rect.yMax>.24f&&rect.yMax<.75f;
        }
        public bool ShowNow(float now,bool? variant=null,int preferredHost=-1)
        {
            if(!Enabled)return false;
            candidates.Clear();
            for(var i=0;i<world.Props.Count;i++)if(HostVisible(i))candidates.Add(i);
            if(candidates.Count==0){initialized=true;next=now+2;return false;}
            if(preferredHost>=0&&!candidates.Contains(preferredHost))return false;
            if(preferredHost<0&&candidates.Count>1)candidates.Remove(lastHost);
            host=candidates.Contains(preferredHost)?preferredHost:candidates[random.Next(candidates.Count)];lastHost=host;
            evil=variant??random.Next(2)==1;
            sprite=evil?kit.santaVillain:kit.santaFriendly;
            if(sprite==null){sprite=kit.santaFriendly??kit.santaVillain;evil=sprite==kit.santaVillain;}
            var cover=world.SceneryCard(host);var p=world.Props[host];
            height=Mathf.Clamp(p.height*.9f,.60f,.95f);
            root.transform.SetParent(cover,false);mesh.sharedMesh=WorldMapCards.Get(sprite,height,true);
            drawing.sharedMaterial=material(sprite.texture);
            block.Clear();block.SetFloat("_GroundedFoot",0);block.SetFloat("_Emission",.035f);
            drawing.SetPropertyBlock(block);
            initialized=active=true;caught=false;start=now;root.SetActive(true);Pose(now);return true;
        }
        public void Tick(float now,bool suspended)
        {
            if(!Enabled){if(active)Reset();return;}
            if(!initialized){initialized=true;next=now+Mathf.Max(1,kit.santaFirstDelay);}
            if(suspended){if(active)Wait(now);next=Mathf.Max(next,now+1);return;}
            if(!active){if(now>=next)ShowNow(now);return;}
            if(!HostVisible(host)||now-start>Rise+Mathf.Max(1,kit.santaVisibleSeconds)+Fall){Wait(now);return;}
            Pose(now);
        }
        void Pose(float now)
        {
            var t=now-start;var hold=Mathf.Max(1,kit.santaVisibleSeconds);
            var amount=Mathf.SmoothStep(0,1,t/Rise)*(1-Mathf.SmoothStep(0,1,(t-Rise-hold)/Fall));
            if(caught)amount=0;
            var p=world.Props[host];var cover=world.SceneryCard(host);
            world.ProjectProp(host,world.Cam.aspect,out var coverRect,out _);
            var sign=cover.localScale.x<0?-1:1;
            root.transform.localScale=new Vector3(sign,1,1);
            root.transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*4)*2.5f*amount);
            // Front-facing bust rises through the shrub; its bottom edge stays concealed by the crown.
            var y=Mathf.Lerp(-height*.95f,p.height*.65f,amount)+Mathf.Sin(t*5)*height*.008f*amount;
            root.transform.localPosition=new Vector3(0,y,.05f);
            clipBelow=Mathf.Max(0,p.height*.58f-y);block.SetFloat("_ClipBelow",clipBelow);drawing.SetPropertyBlock(block);
            Project(coverRect);
        }
        Matrix4x4 ToMap=>world.transform.worldToLocalMatrix*root.transform.localToWorldMatrix;
        void Project(Rect cover)
        {
            var matrix=ToMap;var b=mesh.sharedMesh.bounds;var lo=Vector2.one*100;var hi=-lo;
            for(var i=0;i<4;i++)
            {
                var local=new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?Mathf.Min(clipBelow,b.max.y):b.max.y,0);
                if(world.CameraModel.ProjectPoint(matrix.MultiplyPoint3x4(local),world.Cam.aspect,out var p,out _)){lo=Vector2.Min(lo,p);hi=Vector2.Max(hi,p);}
            }
            exposed=Rect.MinMaxRect(Mathf.Max(0,lo.x),Mathf.Max(0,lo.y),Mathf.Min(1,hi.x),Mathf.Min(.87f,hi.y));
        }
        public bool VisibleRect(out Rect rect)
        {
            rect=exposed;return Active&&!caught&&rect.width>.005f&&rect.height>.005f;
        }
        public bool Hit(Vector2 viewport,float now)
        {
            if(!VisibleRect(out var rect)||!rect.Contains(viewport)||now-start<Rise*.65f||now-start>Rise+Mathf.Max(1,kit.santaVisibleSeconds))return false;
            var ray=world.CameraModel.ViewportRay(viewport,world.Cam.aspect);var matrix=ToMap;
            var plane=new Plane(matrix.MultiplyVector(Vector3.forward),matrix.MultiplyPoint3x4(Vector3.zero));
            if(!plane.Raycast(ray,out var distance))return false;
            var local=matrix.inverse.MultiplyPoint3x4(ray.GetPoint(distance));if(local.y<clipBelow)return false;
            var bounds=WorldMapCards.VisibleBounds(sprite);var scale=height/Mathf.Max(.001f,bounds.height);
            var sx=local.x/scale+bounds.center.x;var sy=local.y/scale+bounds.yMin;
            var u=(sx*sprite.pixelsPerUnit+sprite.pivot.x+sprite.rect.x)/sprite.texture.width;
            var v=(sy*sprite.pixelsPerUnit+sprite.pivot.y+sprite.rect.y)/sprite.texture.height;
            if(u<0||u>1||v<0||v>1||sprite.texture.isReadable&&sprite.texture.GetPixelBilinear(u,v).a<.5f)return false;
            for(var i=0;i<world.Props.Count;i++)
                if(world.SceneryOccludes(i,ray,distance))return false;
            return true;
        }
        public bool Catch(Vector2 viewport,float now,out bool villain,out Vector2 anchor)
        {
            villain=evil;anchor=exposed.center;
            if(!Hit(viewport,now))return false;
            caught=true;Wait(now);return true;
        }
        public void Dispose(){if(root!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(root);else UnityEngine.Object.DestroyImmediate(root);}}
    }
}
