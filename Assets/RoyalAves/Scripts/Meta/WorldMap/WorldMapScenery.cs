using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Meta.WorldMap
{
    // Pose e projecao da camera do mapa como funcao pura do cursor. Usada pela view e pelo gerador de cenografia
    // (que testa limites projetados de props contra medalhoes antes de aceitar o objeto).
    public sealed class WorldMapCameraModel
    {
        readonly WorldMapRoute route;
        readonly WorldMapKit kit;
        public WorldMapKit Kit => kit;
        public float Aspect { get; set; } = 9f/16f;

        void Framing(float aspect,out float pitch,out float height)
        {
            pitch=kit.pitch*Mathf.Deg2Rad;height=kit.cameraHeight;
            if(!kit.illustratedProjection||aspect>=9f/16f)return;
            // Preserve the screen height of both the crest and the current level on taller phones.
            // Horizontal world scale is unchanged, so the lane and both margins still fit the frame.
            var scale=IllustratedSize(aspect)/kit.illustratedSize;
            var focus=Mathf.Max(0,-kit.focusAhead)/kit.radius;
            var angle=Mathf.Acos(Mathf.Clamp(1-(1-Mathf.Cos(pitch+focus))*scale,-1,1))-focus;
            var crest=kit.radius*(1-Mathf.Cos(pitch))-height*Mathf.Cos(pitch)+kit.cameraBack*Mathf.Sin(pitch);
            height=(kit.radius*(1-Mathf.Cos(angle))+kit.cameraBack*Mathf.Sin(angle)-crest*scale)/Mathf.Cos(angle);
            pitch=angle;
        }
        public float PitchAt(float aspect){Framing(aspect,out var pitch,out _);return pitch*Mathf.Rad2Deg;}
        public Quaternion RotationAt(float aspect)=>Quaternion.Euler(PitchAt(aspect),0,0);
        public Vector3 PositionAt(float aspect){Framing(aspect,out _,out var height);return new Vector3(0,height,-kit.cameraBack);}

        public WorldMapCameraModel(WorldMapRoute route, WorldMapKit kit)
        {
            this.route = route;
            this.kit = kit;
        }

        // A camera conserva eixo, posicao lateral e inclinacao. Quem avanca e o mundo.
        public Vector3 Position(float cursorMeters) =>
            PositionAt(Aspect);

        public Quaternion Rotation => RotationAt(Aspect);
        float TanHalfFov(float aspect) => Mathf.Tan(kit.fov * .5f * Mathf.Deg2Rad) * Mathf.Max(1, (9f / 16f) / aspect);
        public float VerticalFov(float aspect) => Mathf.Atan(TanHalfFov(aspect)) * 2 * Mathf.Rad2Deg;
        public float IllustratedSize(float aspect) => kit.illustratedSize * Mathf.Max(1,(9f/16f)/aspect);

        public bool SurfaceVisible(float cursor, float s, float h = 0)
        {
            var rel = s - cursor;
            var sight = kit.illustratedProjection ? -(Rotation * Vector3.forward) : Position(cursor) - MapCurve.Point(rel,0,h,kit.radius);
            return Vector3.Dot(MapCurve.Normal(rel, kit.radius), sight) > 0;
        }

        // Retorna false se o ponto estiver atras da camera. screen em 0..1 (origem embaixo-esquerda).
        public bool Project(float cursorMeters, float s, float lateral, float height, float aspect, out Vector2 screen, out float depth)
        {
            var world = MapCurve.Point(s - cursorMeters, lateral, height, kit.radius,route.Curvature);
            return ProjectPoint(world, aspect, out screen, out depth);
        }

        public bool ProjectPoint(Vector3 point, float aspect, out Vector2 screen, out float depth)
        {
            var local = Quaternion.Inverse(RotationAt(aspect)) * (point - PositionAt(aspect));
            depth = local.z;
            screen = default;
            if (local.z < 0.3f) return false;
            var t = kit.illustratedProjection ? IllustratedSize(aspect) / local.z : TanHalfFov(aspect);
            screen = new Vector2(local.x / (local.z * t * aspect) * 0.5f + 0.5f, local.y / (local.z * t) * 0.5f + 0.5f);
            return true;
        }

        public Ray ViewportRay(Vector2 viewport, float aspect)
        {
            if(kit.illustratedProjection)
            {
                var size=IllustratedSize(aspect);
                var rotation=RotationAt(aspect);
                return new Ray(PositionAt(aspect)+rotation*new Vector3((viewport.x-.5f)*2*size*aspect,(viewport.y-.5f)*2*size,0),rotation*Vector3.forward);
            }
            var t = TanHalfFov(aspect);
            return new Ray(PositionAt(aspect), RotationAt(aspect) * new Vector3((viewport.x - .5f) * 2 * t * aspect, (viewport.y - .5f) * 2 * t, 1).normalized);
        }
    }

    public enum PropKind { Tree, Small, Landmark, Lamp, Mailbox, Dog, FloatingMail, Geyser, Character, GroundDetail, WaterRipple, Steam, Meteor, WaterRock, LightShaft }
    public enum WorldMapObjectTheme { Season, None, Ice }

    public enum WorldMapEffectVisibility { Automatic, Show, Hide }
    [System.Serializable]
    public struct WorldMapEffectPose
    {
        public WorldMapEffectVisibility visibility;
        public Vector2 offset,scale;
        public float rotation;
        // Old layouts have zero scale; interpret that as the original unit scale.
        public Vector2 EffectiveScale=>new Vector2(scale.x>0?scale.x:1,scale.y>0?scale.y:1);
        public bool Visible(bool automatic)=>visibility==WorldMapEffectVisibility.Show||visibility==WorldMapEffectVisibility.Automatic&&automatic;
        public void Apply(Transform target,float height,Vector2 pivot,bool automatic)
        {
            target.localPosition=new Vector3((pivot.x+offset.x)*height,(pivot.y+offset.y)*height,0);
            target.localRotation=Quaternion.Euler(0,0,rotation);
            var size=EffectiveScale;target.localScale=new Vector3(size.x,size.y,1);
            target.gameObject.SetActive(Visible(automatic));
        }
    }

    public struct PropPlacement
    {
        public string id;          // stable editor-authored placement, null for generated defaults
        public float s;            // metros na rota
        public float offset;       // lateral relativa ao centro da rota
        public float height;       // altura visivel em metros
        public PropKind kind;
        public Sprite sprite;
        public Sprite alt;         // segunda pose (caixa aberta, cachorro...)
        public string caption;
        public WorldMapWorkMotion workMotion;
        public int seed;
        public bool shadow;
        public bool fixedPlacement; // Explicit editor choices keep their exact anchors.
        public bool bottomPivot;   // retratos: base no chao em vez do pivo do sprite
        public bool flipX;
        public WorldMapObjectTheme theme;
        public WorldMapEffectPose snowEffect,lightsEffect,iceEffect;
        public WorldMapPiecePose[] effectPieces;
        public bool customAdornments;
        public WorldMapAdornment[] adornments;
        public bool groundPinned;
        public float groundDepth;
        public float rotation, intensity, thickness; // LightShaft: graus (+ vem da esquerda) e opacidade; o tamanho e o height
    }

    // Cenografia autorada por capitulo: receitas de elenco, espacamentos e lados definidos a mao, com semente por
    // capitulo (deterministica). Cada objeto passa por um teste de oclusao projetada contra os medalhoes.
    public static class WorldMapScenery
    {
        public static bool IsDog(Sprite sprite,PropKind kind,WorldMapKit kit) => kind==PropKind.Dog ||
            (sprite!=null&&(sprite==kit.dogIdle||sprite==kit.dogWag||sprite==kit.dogSniff||sprite==kit.dogCarry));
        public static bool IsPostalTree(Sprite sprite,WorldMapKit kit)=>sprite!=null&&(System.Array.IndexOf(kit.postalTrees,sprite)>=0||System.Array.IndexOf(kit.entranceTrees,sprite)>=0||System.Array.IndexOf(kit.expressiveTrees,sprite)>=0);
        static Sprite PostalTree(WorldMapKit kit,int variant)=>UpgradeStyle(kit.postalTrees.Length==3&&kit.postalTrees[Mathf.Abs(variant)%3]!=null?kit.postalTrees[Mathf.Abs(variant)%3]:kit.simpleNature[0],kit);
        public static List<PropPlacement> Build(WorldMapRoute route, WorldMapKit kit, int levelLimit,Func<float,WorldMapKit> kitAt=null)
        {
            if(kit.simpleBase&&kit.simpleNature.Length==6&&kit.simplePostal.Length==6)return SimpleBuild(route,kit,levelLimit);
            var list = new List<PropPlacement>(512);
            var camera = new WorldMapCameraModel(route, kit);
            var levels = Mathf.Min(levelLimit, route.LevelSlots);
            var chapters = route.Islands!=null?route.Islands.islands.Count:(levels + WorldMapEditorial.LevelsPerChapter - 1) / WorldMapEditorial.LevelsPerChapter;
            for (var chapter = 0; chapter < chapters; chapter++)
            {
                var first = route.Islands!=null?route.Islands.FirstLevel(chapter):chapter * WorldMapEditorial.LevelsPerChapter + 1;
                if(first>levels)break;
                var last = Mathf.Min(levels,route.Islands!=null&&chapter+1<chapters?route.Islands.FirstLevel(chapter+1)-1:first + WorldMapEditorial.LevelsPerChapter - 1);
                if(route.Islands!=null&&chapter==chapters-1)last=levels;
                var rng = new System.Random(7000 + chapter * 131);
                var from = chapter == 0 ? 2.5f : route.Pos(first) - WorldMapRoute.Spacing * 0.5f;
                var to = route.Pos(last) + WorldMapRoute.Spacing * 0.5f + (last == levels ? 9 : 0);
                var style=kitAt?.Invoke(route.Pos(first))??kit;
                var cast = CastOf(route.Islands!=null?route.Islands.islands[chapter].palette:chapter, style);
                Lamps(list, route, style, camera, rng, from, to, cast);
                Trees(list, route, style, camera, rng, from, to, cast);
                Small(list, route, style, camera, rng, from, to, cast);
                Landmarks(list, route, style, camera, rng, first, last, cast);
                Characters(list, route, style, camera, rng, first, last, cast);
            }
            list.Sort((a, b) => a.s.CompareTo(b.s));
            foreach (var level in new[] { 9, 38, 62 })
                if (level <= levels && kit.fountain != null)
                    TryAdd(list, route, camera, new PropPlacement
                    {
                        s = route.Pos(level) + .4f, offset = .74f, height = .4f,
                        kind = PropKind.Landmark, sprite = kit.fountain, shadow = true
                    }, 0);
            list.Sort((a, b) => a.s.CompareTo(b.s));
            WorldMapLayout.For(kit)?.Apply(list,route,kit,kitAt);
            for(var i=0;i<list.Count;i++)
            {
                var p=list[i];var beat=Mathf.RoundToInt(route.LevelAt(p.s));
                var style=kitAt?.Invoke(p.s)??kit;
                if(string.IsNullOrEmpty(p.id))p.sprite=VariedSprite(p.sprite,p.kind,beat,style);
                if(style.Lava&&p.sprite==style.lavaMailbox)p.alt=style.lavaMailboxOpen;
                if(style.Family!=null&&style.Family.Length==6&&p.sprite==style.Family[3])p.alt=null;
                // Existing manually authored locations and size overrides remain authoritative.
                if(!p.fixedPlacement||p.height<.01f)p.height=WorldMapPropSizing.Height(p.sprite,p.kind,style);
                list[i]=p;
            }
            list.RemoveAll(p=>IsDog(p.sprite,p.kind,kit)||p.kind==PropKind.FloatingMail);
            if(route.Islands!=null&&kit.geyserBase!=null)
                for(var island=0;island<route.Islands.islands.Count;island++)
                    if(route.Islands.islands[island].terrain==WorldMapTerrainTheme.LavaLand)
                        foreach(var offset in new[]{4,11,17})
                        {
                            var phase=route.Islands.FirstLevel(island)+offset;if(phase>levels)continue;
                            var id="thermal-"+route.Islands.islands[island].id+"-"+offset;
                            if(Resources.Load<WorldMapLayout>(WorldMapLayout.ResourcePath)?.ExtraClaimed(id)==true)continue;
                            var origin=route.Pos(phase)+.55f;var chosen=origin;var side=offset%2==0?-1f:1f;var chosenSide=side;var best=float.NegativeInfinity;
                            for(var attempt=0;attempt<20;attempt++)
                            {
                                var s=origin+(attempt/4)*.65f*(attempt%4<2?1:-1);var x=(attempt%2==0?side:-side)*1.9f;
                                if(route.InGap(s,.8f)||route.IslandIndexAt(s)!=island)continue;
                                var clearance=10f;
                                foreach(var other in list)if(Mathf.Sign(other.offset)==Mathf.Sign(x)&&Mathf.Abs(other.offset-x)<1)
                                    clearance=Mathf.Min(clearance,Mathf.Abs(other.s-s)-(other.height+1.12f)*.65f);
                                if(clearance>best){best=clearance;chosen=s;chosenSide=Mathf.Sign(x);}
                            }
                            list.Add(new PropPlacement{id=id,s=chosen,offset=chosenSide*1.9f,height=1.12f,kind=PropKind.Geyser,sprite=kit.geyserBase,seed=phase,shadow=true,fixedPlacement=true});
                        }
            ComposeMargins(list,route,kit);
            return list;
        }

        static List<PropPlacement> SimpleBuild(WorldMapRoute route,WorldMapKit kit,int limit)
        {
            var list=new List<PropPlacement>();var n=kit.simpleNature;var p=kit.simplePostal;
            var count=route.Islands?.islands.Count??Mathf.CeilToInt(limit/20f);
            for(var chapter=0;chapter<count;chapter++)
            {
                var first=route.Islands?.FirstLevel(chapter)??chapter*20+1;if(first>limit)break;
                var grove=chapter%3==1;
                void Add(float phase,float side,Sprite sprite,PropKind kind,float height,int seed)
                {
                    var level=first+phase;var s=route.MetersAt(level);
                    if(sprite==null||level>limit+.1f||route.InGap(s,.5f))return;
                    list.Add(new PropPlacement{s=s,offset=side,sprite=UpgradeStyle(sprite,kit),kind=kind,height=height,seed=chapter*71+seed,shadow=true,fixedPlacement=true});
                }
                for(var i=0;i<(grove?5:4);i++)
                    Add(2+i*(grove?3.6f:5),i%2==0?-2.02f:2.06f,grove&&i%3==1?n[1]:PostalTree(kit,chapter+i),PropKind.Tree,1.45f+(i%2)*.13f,i);
                for(var i=0;i<4;i++)Add(.7f+i*5.1f,i%2==0?1.69f:-1.7f,n[2],PropKind.Small,.58f,i+10);
                Add(8.1f,-1.72f,p[2],PropKind.Lamp,1.06f,20);
                Add(6.8f,1.55f,n[4],PropKind.Small,.22f,21);
                Add(13.5f,-1.55f,n[3],PropKind.Small,.27f,22);
                if(!grove)
                {
                    Add(4.6f,2.25f,p[0],PropKind.Landmark,1.42f,30);
                    Add(16.4f,-2.25f,p[1],PropKind.Landmark,1.42f,31);
                    if(chapter%2==0)Add(13.8f,1.8f,p[5],PropKind.Mailbox,.67f,32);
                }
                Add(18.6f,-2.05f,n[5],PropKind.Small,.34f,40);
                Add(18.8f,2.05f,n[5],PropKind.Small,.34f,41);
                if(chapter==0)
                {
                    Add(.3f,-1.72f,kit.postmanSprite??p[3],PropKind.Character,kit.postmanSprite!=null?1.6f:1.05f,50);
                    Add(10.1f,1.83f,p[4],PropKind.Character,.90f,51);
                    Add(-.7f,1.0f,n[2],PropKind.Small,.55f,52);
                }
                else if(chapter%3==2)Add(9.4f,1.85f,chapter%2==0?(kit.postmanSprite??p[3]):p[4],PropKind.Character,chapter%2==0&&kit.postmanSprite!=null?1.6f:.82f,53);
            }
            WorldMapLayout.For(kit)?.Apply(list,route,kit);
            var layout=WorldMapLayout.For(kit);
            foreach(var gap in route.Gaps)
            {
                var settings=route.CrossingAt(gap);var center=(gap.start+gap.end)*.5f;
                void Extra(string suffix,Sprite sprite,PropKind kind,float s,float x,float size,int seed)
                {
                    var id="river-"+gap.afterLevel+"-"+suffix;
                    if(sprite==null||layout?.ExtraClaimed(id)==true)return;
                    list.Add(new PropPlacement{id=id,s=s,offset=x,sprite=sprite,kind=kind,height=size,seed=seed,shadow=false,fixedPlacement=true});
                }
                if(kit.waterRipple!=null&&(settings?.waterMotion??WorldMapWaterMotion.Wakes)==WorldMapWaterMotion.Wakes)
                    for(int i=0;i<2;i++)Extra("wake-"+i,kit.waterRipple,PropKind.WaterRipple,center+(i==0?-.12f:.12f),i==0?-2.1f:2.1f,.30f,gap.afterLevel+i);
                if(settings?.channelRocks==true||settings?.crossing==WorldMapCrossing.Stones)
                {
                    var stepping=settings.crossing==WorldMapCrossing.Stones;
                    for(int i=0;i<(stepping?4:2);i++)Extra("rocks-"+i,kit.waterRocks??kit.simpleNature[4],PropKind.WaterRock,
                        stepping?Mathf.Lerp(gap.start+.15f,gap.end-.15f,i/3f):center+(i==0?-.25f:.25f),stepping?(i%2==0?-.13f:.13f):i==0?-1.25f:1.5f,stepping?.42f:.32f,i);
                }
            }
            for(var side=0;side<2;side++)
            {
                var id="entrance-tree-"+side;
                if(layout?.ExtraClaimed(id)==true)continue;
                var entrance=kit.entranceTrees.Length==2&&kit.entranceTrees[side]!=null?kit.entranceTrees[side]:PostalTree(kit,side*2);
                list.Add(new PropPlacement{id=id,s=route.MetersAt(side==0?-1.3f:-1.05f),offset=side==0?-1.95f:1.95f,sprite=UpgradeStyle(entrance,kit),
                    kind=PropKind.Tree,height=side==0?1.6f:1.48f,seed=54+side,shadow=true,fixedPlacement=true});
            }
            list.Sort((a,b)=>a.s.CompareTo(b.s));return list;
        }

        public static Sprite UpgradeStyle(Sprite sprite,WorldMapKit k)
        {
            if(sprite==null||k==null)return sprite;
            if(k.simpleBase&&k.expressiveTrees.Length==4)
            {
                if(k.simpleNature.Length==6)
                {
                    if(sprite==k.simpleNature[0])return k.expressiveTrees[1]??sprite;
                    if(sprite==k.simpleNature[1])return k.expressiveTrees[2]??sprite;
                }
                var oldTree=System.Array.IndexOf(k.postalTrees,sprite);
                if(oldTree>=0)return k.expressiveTrees[oldTree==0?1:oldTree==1?0:3]??sprite;
                var entrance=System.Array.IndexOf(k.entranceTrees,sprite);
                if(entrance>=0)return k.expressiveTrees[entrance==0?1:2]??sprite;
                if(k.simplePostal.Length==6)
                {
                    if(sprite==k.simplePostal[0])return k.expressivePostOffice??sprite;
                    if(sprite==k.simplePostal[1])return k.expressiveWorkshop??sprite;
                }
            }
            if(k.simpleBase&&k.postmanSprite!=null&&k.simplePostal.Length==6&&sprite==k.simplePostal[3])return k.postmanSprite;
            // Old manually placed round trees keep their anchors while losing the baked-on leaf.
            if(k.simpleBase&&k.simpleNature.Length==6&&sprite==k.simpleNature[0])return PostalTree(k,0);
            var family=k.Family;
            if(family!=null&&family.Length==6)
            {
                if(sprite==k.tree||sprite==k.pine||sprite==k.postalPine||sprite==k.autumnTree)return family[0]??sprite;
                if(sprite==k.postOffice||sprite==k.legacyPostOffice||sprite==k.mailKiosk)return family[1]??sprite;
                if(sprite==k.lamp||sprite==k.curvedLantern)return family[2]??sprite;
                if(sprite==k.mailboxClosed||sprite==k.mailboxOpen)return family[3]??sprite;
                if(sprite==k.shrub||sprite==k.shrubCoral||sprite==k.shrubGold||sprite==k.shrubMint)return family[4]??sprite;
                if(sprite==k.flowers||sprite==k.bellFlowers||sprite==k.detailBells||sprite==k.detailTulips)return family[5]??sprite;
            }
            switch(sprite.name)
            {
                case "green-post-office-polished":case "green_post_office_polished_house":return k.postOffice??sprite;
                case "postal_scenery_variety_windmill":return k.mailWindmill??sprite;
                case "postal_scenery_variety_lantern":return k.curvedLantern??sprite;
                case "postal_scenery_variety_autumn_tree":return k.autumnTree??sprite;
                case "postal_scenery_variety_pine":return k.postalPine??sprite;
                case "postal_scenery_variety_kiosk":return k.mailKiosk??sprite;
                case "postal_scenery_variety_flowers":return k.bellFlowers??sprite;
                default:return sprite;
            }
        }

        public static Sprite VariedSprite(Sprite sprite,PropKind kind,int beat,WorldMapKit k)
        {
            if(sprite==null)return null;
            sprite=UpgradeStyle(sprite,k);
            if(k.Lava)
            {
                if(kind==PropKind.Tree)return (beat%3==0?k.lavaCrystal:k.lavaTree)??sprite;
                if(kind==PropKind.Lamp)return k.lavaLamp??sprite;
                if(kind==PropKind.Mailbox)return k.lavaMailbox??sprite;
                if(kind==PropKind.Landmark)return (beat%3==0?k.lavaForge:beat%3==1?k.lavaOffice:k.lavaObservatory)??sprite;
                if(kind==PropKind.Small)return (beat%3==0?k.lavaShrub:beat%3==1?k.lavaFlower:k.lavaBoulder)??sprite;
            }
            var family=k.Family;
            if(family!=null&&family.Length==6)
            {
                if(kind==PropKind.Tree)return family[0]??sprite;
                if(kind==PropKind.Lamp)return family[2]??sprite;
                if(kind==PropKind.Mailbox)return family[3]??sprite;
                if(kind==PropKind.Landmark&&(k.terrainTheme!=WorldMapTerrainTheme.GreenLand||sprite==k.postOffice||sprite==k.hut||sprite==k.agency))return family[1]??sprite;
                if(kind==PropKind.Small&&sprite!=k.parcels&&sprite!=k.sign)return family[beat%2==0?4:5]??sprite;
            }
            var chapter=WorldMapRoute.ChapterOf(Mathf.Max(1,beat));
            if(sprite==k.tree||sprite==k.postalPine||sprite==k.autumnTree)
            {
                var main=chapter%4==0?k.tree:chapter%4==1?k.autumnTree:chapter%4==2?k.postalPine:k.tree;
                var accent=chapter%4==0?k.autumnTree:chapter%4==1?k.tree:chapter%4==2?k.tree:k.postalPine;
                return (beat%5==1?accent:main)??sprite;
            }
            if(sprite==k.shrub||sprite==k.shrubCoral||sprite==k.shrubGold||sprite==k.shrubMint)
                return (chapter%3==0?k.shrubCoral:chapter%3==1?k.shrubGold:k.shrubMint)??sprite;
            if(sprite==k.flowers)return (chapter%2==0?k.detailBells:k.detailTulips)??sprite;
            
            if(sprite==k.lamp&&beat%2==0)return k.curvedLantern??sprite;
            return sprite;
        }

        sealed class Cast
        {
            public Sprite[] trees, small;
            public Sprite landmarkA, landmarkB;
            public float lampEvery;
            public float treeDensity;
        }

        // A legacy saved layout can contain overlapping defaults. Reflow those defaults only;
        // explicit edits (custom size or chosen sprite) retain their exact phase and lateral.
        static void ComposeMargins(List<PropPlacement> list,WorldMapRoute route,WorldMapKit kit)
        {
            var accepted=new List<PropPlacement>();
            list.Sort((a,b)=>Priority(a).CompareTo(Priority(b)));
            foreach(var source in list)
            {
                var p=source;
                if(!p.fixedPlacement)
                {
                    var width=WorldMapCards.Width(p.sprite,p.height);
                    var clearance=WorldMapTerrain.PathHalf+.10f+width*.43f;
                    p.offset=Mathf.Sign(p.offset==0?1:p.offset)*Mathf.Max(Mathf.Abs(p.offset),clearance);
                    var origin=p.s;
                    var chapter=route.ChapterAtMeters(origin);
                    var first=chapter*WorldMapEditorial.LevelsPerChapter+1;
                    var last=Mathf.Min(route.LevelSlots,first+WorldMapEditorial.LevelsPerChapter-1);
                    if(route.Islands!=null)
                    {
                        var island=route.IslandIndexAt(origin);first=route.Islands.FirstLevel(island);
                        last=island+1<route.Islands.islands.Count?route.Islands.FirstLevel(island+1)-1:route.LevelSlots;
                    }
                    var min=first==1?1:(route.Pos(first-1)+route.Pos(first))*.5f;
                    var max=last==route.LevelSlots?route.Length-1:(route.Pos(last)+route.Pos(last+1))*.5f-.01f;
                    var placed=false;
                    for(var attempt=0;attempt<16;attempt++)
                    {
                        var blocked=false;
                        foreach(var q in accepted)
                        {
                            if(Mathf.Sign(p.offset)!=Mathf.Sign(q.offset))continue;
                            var tall=p.kind!=PropKind.Small&&q.kind!=PropKind.Small;
                            var gap=tall?Mathf.Max(2.25f,(p.height+q.height)*.72f):.8f;
                            if(Mathf.Abs(p.s-q.s)<gap&&Mathf.Abs(p.offset-q.offset)<.95f){blocked=true;break;}
                        }
                        if(!blocked&&!route.InGap(p.s,.6f)){placed=true;break;}
                        var step=(attempt/2+1)*.5f*(attempt%2==0?1:-1);
                        p.s=Mathf.Clamp(origin+step,min,max);
                    }
                    if(!placed){p.s=origin;p.offset+=Mathf.Sign(p.offset)*.75f;}
                }
                accepted.Add(p);
            }
            list.Clear();list.AddRange(accepted);list.Sort((a,b)=>a.s.CompareTo(b.s));
        }
        static int Priority(PropPlacement p)=>p.fixedPlacement?0:p.kind==PropKind.Lamp?1:p.kind==PropKind.Mailbox?2:p.kind==PropKind.Landmark?3:p.kind==PropKind.Tree?4:5;

        // Both source lamps have their head on the left. Mailbox doors also point left.
        public static bool MirrorTowardLane(PropPlacement p)=>
            (p.kind==PropKind.Lamp||p.kind==PropKind.Mailbox)&&p.offset<0;
        public static bool IsMirrored(PropPlacement p)=>(MirrorTowardLane(p)||p.kind==PropKind.Character&&((p.offset>0)^(WorldMapMission100.Character(p.sprite)?.facingLeft??false)))^p.flipX;
        public static bool HasIceTheme(PropPlacement p,bool seasonalSnow)=>p.theme==WorldMapObjectTheme.Ice||p.theme==WorldMapObjectTheme.Season&&seasonalSnow;
        public static bool IsShrub(PropPlacement p,WorldMapKit kit)
        {
            if(p.kind!=PropKind.Small||p.sprite==null)return false;
            var name=p.sprite.name.ToLowerInvariant();
            return p.sprite==kit.shrub||p.sprite==kit.shrubCoral||p.sprite==kit.shrubGold||p.sprite==kit.shrubMint||name.Contains("shrub")||name.Contains("bush")||name.Contains("arbusto");
        }

        static Cast CastOf(int chapter, WorldMapKit k)
        {
            Sprite[] S(params Sprite[] sprites) => Array.FindAll(sprites, x => x != null);
            switch (chapter)
            {
                case 0: return new Cast { trees = S(k.tree,k.postalPine), small = S(k.flowers, k.shrub, k.parcels, k.sign, k.bench), landmarkA = k.postOffice, landmarkB = k.mailWindmill??k.postOffice, lampEvery = 8, treeDensity = 0.7f };
                case 1: return new Cast { trees = S(k.tree,k.autumnTree), small = S(k.flowers, k.sign, k.parcels), landmarkA = k.mailKiosk??k.postOffice, landmarkB = k.mailWindmill??k.clock, lampEvery = 7.5f, treeDensity = 0.6f };
                case 2: return new Cast { trees = S(k.pine, k.tree), small = S(k.shrub, k.flowers), landmarkA = k.hut, landmarkB = k.pine, lampEvery = 0, treeDensity = 1.3f };
                case 3: return new Cast { trees = S(k.tree), small = S(k.parcels, k.shrub, k.sign), landmarkA = k.boat, landmarkB = k.boat, lampEvery = 9, treeDensity = 0.5f };
                case 4: return new Cast { trees = S(k.shrub), small = S(k.parcels, k.sign, k.parcelCart), landmarkA = k.factoryBody ?? k.factory, landmarkB = k.factoryBody ?? k.factory, lampEvery = 8, treeDensity = 0.35f };
                case 5: return new Cast { trees = S(k.tree), small = S(k.flowers, k.shrub), landmarkA = k.windmill, landmarkB = k.kite, lampEvery = 0, treeDensity = 0.7f };
                case 6: return new Cast { trees = S(k.shrub), small = S(k.parcels, k.sign), landmarkA = k.lighthouse, landmarkB = k.boat, lampEvery = 9, treeDensity = 0.3f };
                case 7: return new Cast { trees = S(k.pine), small = S(k.shrub), landmarkA = k.hut, landmarkB = k.tunnel, lampEvery = 0, treeDensity = 0.8f };
                case 8: return new Cast { trees = S(k.tree, k.topiary), small = S(k.flowers, k.shrub, k.bench), landmarkA = k.fountain ?? k.clock, landmarkB = k.clock, lampEvery = 8.5f, treeDensity = 0.7f };
                default: return new Cast { trees = S(k.tree), small = S(k.flowers, k.sign), landmarkA = k.agency, landmarkB = k.agency, lampEvery = 6.5f, treeDensity = 0.5f };
            }
        }

        static void Lamps(List<PropPlacement> list, WorldMapRoute route, WorldMapKit kit, WorldMapCameraModel camera,
            System.Random rng, float from, float to, Cast cast)
        {
            if (cast.lampEvery <= 0 || kit.lamp == null) return;
            var side = rng.Next(2) == 0 ? -1 : 1;
            for (var s = from + 3; s < to; s += cast.lampEvery)
            {
                TryAdd(list, route, camera, new PropPlacement
                {
                    s = s, offset = side * .94f, height = .85f, kind = PropKind.Lamp, sprite = kit.lamp, shadow = true
                }, 0);
                side = -side;
            }
        }

        static void Trees(List<PropPlacement> list, WorldMapRoute route, WorldMapKit kit, WorldMapCameraModel camera,
            System.Random rng, float from, float to, Cast cast)
        {
            if (cast.trees.Length == 0) return;
            var side = rng.Next(2) == 0 ? -1 : 1;
            for (var s = from + 1.5f; s < to; s += (3.4f + (float)rng.NextDouble() * 2.2f) / cast.treeDensity)
            {
                var sprite = cast.trees[rng.Next(cast.trees.Length)];
                var height = .9f + (float)rng.NextDouble() * .3f;
                var offset = side * (1.08f + (float)rng.NextDouble() * .25f);
                TryAdd(list, route, camera, new PropPlacement
                {
                    s = s, offset = offset, height = height, kind = PropKind.Tree, sprite = sprite, shadow = true, seed = rng.Next()
                }, 0);
                side = -side;
            }
        }

        static void Small(List<PropPlacement> list, WorldMapRoute route, WorldMapKit kit, WorldMapCameraModel camera,
            System.Random rng, float from, float to, Cast cast)
        {
            if (cast.small.Length == 0) return;
            var side = rng.Next(2) == 0 ? -1 : 1;
            for (var s = from + 0.8f; s < to; s += 2.8f + (float)rng.NextDouble() * 1.8f)
            {
                var sprite = cast.small[rng.Next(cast.small.Length)];
                TryAdd(list, route, camera, new PropPlacement
                {
                    s = s, offset = side * (.82f + (float)rng.NextDouble() * .18f), height = .22f + (float)rng.NextDouble() * .1f,
                    kind = PropKind.Small, sprite = sprite, shadow = true, seed = rng.Next()
                }, 0);
                if (rng.NextDouble() < 0.55) side = -side;
            }
        }

        static void Landmarks(List<PropPlacement> list, WorldMapRoute route, WorldMapKit kit, WorldMapCameraModel camera,
            System.Random rng, int first, int last, Cast cast)
        {
            // Dois marcos grandes por capitulo, longe do caminho dos medalhoes (um no 3o e outro no 15o nivel).
            var spots = new[] { (first + 2, cast.landmarkA), (first + 7, cast.landmarkB), (first + 12, cast.landmarkA), (first + 17, cast.landmarkB) };
            var side = rng.Next(2) == 0 ? -1 : 1;
            foreach (var (level, sprite) in spots)
            {
                if (sprite == null || level > last) continue;
                TryAdd(list, route, camera, new PropPlacement
                {
                    s = route.Pos(level) + 1.2f, offset = side * 1.10f, height = .9f, kind = PropKind.Landmark, sprite = sprite, shadow = true
                }, 0);
                side = -side;
            }
        }

        static void Characters(List<PropPlacement> list, WorldMapRoute route, WorldMapKit kit, WorldMapCameraModel camera,
            System.Random rng, int first, int last, Cast cast)
        {
            if (kit.mailboxClosed == null) return;
            // Uma caixa de correio perto do inicio de cada trecho de cinco fases.
            for (var beat = 0; beat < 4; beat++)
            {
                var level = first + beat * 5;
                if (level > last) break;
                TryAdd(list, route, camera, new PropPlacement
                {
                    s = route.Pos(level) + 0.9f, offset = ((level + beat) & 1) == 0 ? .94f : -.94f, height = .52f,
                    kind = PropKind.Mailbox, sprite = kit.mailboxClosed, alt = kit.mailboxOpen, seed = level, shadow = true
                }, 0);
            }
        }

        // Aceita o objeto se (1) nao estiver sobre um canal, (2) dentro da rota e (3) nao cobrir medalhao
        // mais distante em nenhum avanco plausivel da camera. Se cobrir, afasta lateralmente algumas vezes.
        static void TryAdd(List<PropPlacement> list, WorldMapRoute route, WorldMapCameraModel camera, PropPlacement p, int depth)
        {
            p.height=WorldMapPropSizing.Height(p.sprite,p.kind,camera.Kit);
            if(p.kind != PropKind.FloatingMail) p.offset += Mathf.Sign(p.offset) * .65f;
            if (p.sprite == null || p.s < 1 || p.s > route.Length - 1 || route.InGap(p.s, .6f)) return;
            for (var attempt = 0; attempt < 5; attempt++)
            {
                if (!CoversToken(route, camera, p))
                {
                    list.Add(p);
                    return;
                }
                p.offset += Mathf.Sign(p.offset) * 0.25f;
            }
        }

        static bool CoversToken(WorldMapRoute route, WorldMapCameraModel camera, PropPlacement p)
        {
            const float aspect = 9f / 16f;
            var width = WorldMapCards.Width(p.sprite, p.height);
            var foot = WorldMapTerrain.SurfaceHeight(route,p.s,p.offset);
            for (var back = 2f; back <= 17f; back += 3f)
            {
                var cursor = p.s - back;
                var cx = route.CenterX(p.s) + WorldMapTerrain.Lateral(p.offset, back);
                if (!camera.Project(cursor, p.s, cx - width * 0.5f, foot, aspect, out var a, out var da)) continue;
                camera.Project(cursor, p.s, cx + width * 0.5f, foot + p.height, aspect, out var b, out _);
                var propRect = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
                var firstLevel = Mathf.Max(1, Mathf.CeilToInt(route.LevelAt(p.s + 0.2f)));
                var lastLevel = Mathf.Min(route.LevelSlots, Mathf.FloorToInt(route.LevelAt(cursor + 18)));
                for (var level = firstLevel; level <= lastLevel; level++)
                {
                    var s = route.Pos(level);
                    if (!camera.SurfaceVisible(cursor, s)) continue;
                    var tx = route.CenterX(s);
                    // Medalhao (raio ~0.6 m x 1.25 de escala), com folga para o numero e a estrela.
                    if (!camera.Project(cursor, s, tx - 0.34f, 0, aspect, out var t0, out var dt)) continue;
                    if (dt <= da) continue; // medalhao mais perto que o prop nunca e coberto por ele
                    camera.Project(cursor, s, tx + 0.34f, 0.13f, aspect, out var t1, out _);
                    var tokenRect = Rect.MinMaxRect(Mathf.Min(t0.x, t1.x), Mathf.Min(t0.y, t1.y) - 0.01f, Mathf.Max(t0.x, t1.x), Mathf.Max(t0.y, t1.y) + 0.01f);
                    if (propRect.Overlaps(tokenRect)) return true;
                }
            }
            return false;
        }
    }
}
