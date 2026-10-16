using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.UI;
using SweetSugar.Scripts.MapScripts;

namespace RoyalAves.Meta.WorldMap
{
    public sealed class WorldMapNodeRef : MonoBehaviour
    {
        public int level;
    }

    // Mundo 3D do mapa: camera de perspectiva -> RenderTexture exibida por um RawImage do lobby.
    // Vive longe da origem, em layer proprio, e e desligado quando o mapa fecha.
    public sealed class WorldMapView : MonoBehaviour
    {
        static readonly Vector3 RootOffset = new Vector3(0, -3000, 0);
        const float SlotSpacing = 500f; // cada mapa vivo ocupa uma faixa propria do espaco (camera enxerga 160 m)
        const int MaxNodes = 14;
        const int MaxSlots = 110;
        const float Back = 10f, PropBack = 4.6f, Front = 50f, LiftNear = 5.5f, LiftFar = 18f, LieAngle = 64f;

        sealed class Node
        {
            public GameObject go;
            public Transform t;
            public Transform decorationRoot;
            public MaterialPropertyBlock block = new MaterialPropertyBlock();
            public MeshRenderer body;
            public TextMeshPro number;
            public Transform numberT;
            public readonly MeshRenderer[] stars = new MeshRenderer[3];
            public MeshRenderer crown;
            public Transform shadowT;
            public MeshRenderer badge;
            public MeshFilter badgeMesh;
            public WorldMapNodeRef reference;
            public int level, stateKey = -1;
        }

        sealed class Slot
        {
            public GameObject go;
            public Transform t, card, shadowT,snowRoot;
            public MeshFilter cardMesh;
            public MeshRenderer cardRenderer, shadowRenderer;
            public MaterialPropertyBlock shadowBlock = new MaterialPropertyBlock();
            public MaterialPropertyBlock cardBlock = new MaterialPropertyBlock();
            public Sprite shown;
            public float shownHeight;
            public PropKind shownKind;
            public bool shownBottomPivot;
            public readonly MeshFilter[] caps = new MeshFilter[3];
            public readonly MeshRenderer[] capRenderers = new MeshRenderer[3];
            public int dressKey = int.MinValue;
            public Transform lightT;
            public MeshRenderer lightRenderer;
            public MaterialPropertyBlock lightBlock=new MaterialPropertyBlock();
            public readonly MeshFilter[] animation=new MeshFilter[3];
            public readonly MeshRenderer[] animationRenderers=new MeshRenderer[3];
            public readonly MaterialPropertyBlock animationBlock=new MaterialPropertyBlock();
            public WorldMapTreeDecor treeDecor;
            public WorldMapPostman postman;
            public WorldMapMailbox mailbox;
            public WorldMapGroundDetail groundDetail;
            public WorldMapGroundDetail groundGlow;
            public WorldMapSeparatedSnow separatedSnow;
            public WorldMapChristmasDecor christmasDecor;
            public WorldMapWindowLight windowLight;
            public WorldMapLightFx lightFx;
            public MeshRenderer lampHalo;
            public TextMeshPro caption;
            public WorldMapAdornments adornments;
            public readonly MaterialPropertyBlock lampHaloBlock=new MaterialPropertyBlock();
            public WorldMapLightSet.Shaft shaft;
        }

        public WorldMapRoute Route { get; private set; }
        public IReadOnlyList<PropPlacement> Props => props;
        public bool MailboxNeedsFrames=>props.Exists(p=>WorldMapMailbox.Supports(p.sprite));
        public bool CharacterNeedsFrames=>props.Exists(p=>p.kind==PropKind.Character&&(p.sprite==kit.postmanSprite||WorldMapMission100.Character(p.sprite)!=null));
        public bool AdornmentNeedsFrames=>props.Exists(p=>p.workMotion!=WorldMapWorkMotion.None||p.customAdornments&&p.adornments!=null&&System.Array.Exists(p.adornments,a=>a.blink||a.motion!=WorldMapAdornmentMotion.None));
        public bool IceNeedsFrames=>kit.Christmas||props.Exists(p=>p.kind==PropKind.GroundDetail&&WorldMapScenery.HasIceTheme(p,false));
        public WorldMapEffectPiece[] EffectPieces(string id)
        {
            foreach(var pair in activeSlots)if(props[pair.Key].id==id)
                return System.Array.FindAll(pair.Value.card.GetComponentsInChildren<WorldMapEffectPiece>(true),p=>p.transform.parent.gameObject.activeInHierarchy);
            return System.Array.Empty<WorldMapEffectPiece>();
        }
        public WorldMapCameraModel CameraModel => camModel;
        public bool ProjectEffectPiece(WorldMapEffectPiece piece,out Rect bounds)
        {
            bounds=default;var drawing=piece.GetComponentInChildren<MeshRenderer>(true);if(drawing==null)return false;
            var b=drawing.bounds;var lo=Vector2.positiveInfinity;var hi=Vector2.negativeInfinity;
            for(int i=0;i<8;i++)
            {
                var p=cam.WorldToViewportPoint(new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,(i&4)==0?b.min.z:b.max.z));
                if(p.z<=0)return false;lo=Vector2.Min(lo,p);hi=Vector2.Max(hi,p);
            }
            bounds=Rect.MinMaxRect(lo.x,lo.y,hi.x,hi.y);return bounds.width>0&&bounds.height>0;
        }
        public Camera Cam => cam;
        public Texture Target => rt;
        public float TimeOverride = -1;
        WorldMapSantaEvent santa;
        public float Clock=>Now;
        public bool SantaNeedsFrames=>santa!=null&&santa.NeedsFrames(Now);
        public bool ChristmasNeedsFrames
        {
            get{foreach(var slot in activeSlots.Values)if(slot.christmasDecor!=null&&slot.christmasDecor.Active)return true;return false;}
        }
        public WorldMapSantaEvent Santa=>santa;
        internal Transform SceneryCard(int index)=>activeSlots.TryGetValue(index,out var slot)?slot.card:null;
        internal bool SceneryOccludes(int index,Ray mapRay,float maxDistance)
        {
            if(!activeSlots.TryGetValue(index,out var slot)||!slot.go.activeInHierarchy||slot.cardMesh.sharedMesh==null)return false;
            var inverse=(transform.worldToLocalMatrix*slot.card.localToWorldMatrix).inverse;
            var origin=inverse.MultiplyPoint3x4(mapRay.origin);var direction=inverse.MultiplyVector(mapRay.direction);
            if(Mathf.Abs(direction.z)<.00001f)return false;
            var distance=-origin.z/direction.z;if(distance<0||distance>=maxDistance)return false;
            var point=(Vector2)(origin+direction*distance);var mesh=slot.cardMesh.sharedMesh;
            var vertices=mesh.vertices;var triangles=mesh.triangles;
            for(var t=0;t<triangles.Length;t+=3)
            {
                var a=(Vector2)vertices[triangles[t]];var b=(Vector2)vertices[triangles[t+1]];var c=(Vector2)vertices[triangles[t+2]];
                var ab=b-a;var bc=c-b;var ca=a-c;var ap=point-a;var bp=point-b;var cp=point-c;
                var x=ab.x*ap.y-ab.y*ap.x;var y=bc.x*bp.y-bc.y*bp.x;var z=ca.x*cp.y-ca.y*cp.x;
                if(x>=0&&y>=0&&z>=0||x<=0&&y<=0&&z<=0)return true;
            }
            return false;
        }
        public bool ShowSantaNow(bool evil)=>santa!=null&&santa.ShowNow(Now,evil);
        public bool CatchSanta(Vector2 viewport,out bool evil,out Vector2 anchor)
        {
            evil=false;anchor=default;return santa!=null&&santa.Catch(viewport,Now,out evil,out anchor);
        }
        public System.Func<int,bool> LevelExists;
        WorldMapKit[] islandKits;
        Material[] islandMaterials;
        public int RenderedLevelCount=>maxShown;
        public WorldMapKit KitAt(float meters)=>islandKits!=null?islandKits[Route.IslandIndexAt(meters)]:kit;
        int SceneryLimit=>!Application.isPlaying?Route.LevelSlots:maxShown;

        public void ReloadScenery()
        {
            santa?.Reset();
            BindGround();
            foreach(var pair in activeSlots){pair.Value.go.SetActive(false);free.Push(pair.Value);}
            activeSlots.Clear();
            props=WorldMapScenery.Build(Route,kit,SceneryLimit,KitAt);
        }

        public bool ProjectProp(int index,float aspect,out Rect bounds,out float depth)
        {
            bounds=default;depth=float.PositiveInfinity;
            if(!activeSlots.TryGetValue(index,out var slot)||!slot.go.activeInHierarchy||slot.cardMesh.sharedMesh==null)return false;
            var b=slot.cardMesh.sharedMesh.bounds;
            if(slot.postman?.Active==true)b=slot.postman.LocalBounds;
            if(slot.groundDetail?.Active==true)b=slot.groundDetail.LocalBounds;
            if(props[index].kind==PropKind.Geyser)b.Encapsulate(new Vector3(0,props[index].height*1.3f,0));
            var corners=new Vector3[8];
            for(int i=0;i<8;i++)corners[i]=new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,(i&4)==0?b.min.z:b.max.z);
            var lo=new Vector2(float.PositiveInfinity,float.PositiveInfinity);
            var hi=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            foreach(var corner in corners)
            {
                var local=transform.InverseTransformPoint(slot.card.TransformPoint(corner));
                if(!camModel.ProjectPoint(local,aspect,out var uv,out var d))return false;
                lo=Vector2.Min(lo,uv);hi=Vector2.Max(hi,uv);depth=Mathf.Min(depth,d);
            }
            bounds=Rect.MinMaxRect(lo.x,lo.y,hi.x,hi.y);
            return true;
        }

        public bool ProjectPropAnchor(int index,float aspect,out Vector2 viewport)
        {
            viewport=default;
            return activeSlots.TryGetValue(index,out var slot) && slot.go.activeInHierarchy &&
                camModel.ProjectPoint(transform.InverseTransformPoint(slot.t.position),aspect,out viewport,out _);
        }

        public bool GroundAtViewport(Vector2 uv,float cursorLevels,float aspect,out float level,out float lateral)
        {
            level=lateral=0;
            var ray=camModel.ViewportRay(uv,aspect);
            var center=new Vector3(0,-kit.radius,0);
            var delta=ray.origin-center;
            var r=kit.radius+WorldMapTerrain.BankH;
            var b=Vector3.Dot(delta,ray.direction);
            var disc=b*b-Vector3.Dot(delta,delta)+r*r;
            if(disc<0)return false;
            var t=-b-Mathf.Sqrt(disc);if(t<0)return false;
            var lateralRadius=kit.radius/Route.Curvature;
            var cursorMeters=Route.MetersAt(cursorLevels);
            void Decode(Vector3 q,out float s,out float side,out float height)
            {
                var rel=Mathf.Atan2(q.z,q.y)*kit.radius;s=cursorMeters+rel;side=0;height=WorldMapTerrain.BankH;
                var scale=Mathf.Lerp(1,.8f,Mathf.InverseLerp(0,14,rel));
                for(int pass=0;pass<3;pass++)
                {
                    var x=Mathf.Asin(Mathf.Clamp(q.x/(lateralRadius+height),-1,1))*lateralRadius;
                    var offset=x-Route.CenterX(s);var a=Mathf.Abs(offset);
                    side=Mathf.Sign(offset)*(a<=WorldMapTerrain.PathHalf*scale?a/scale:a-WorldMapTerrain.PathHalf*scale+WorldMapTerrain.PathHalf);
                    height=Route.InGap(s)?Route.WaterHeight(s):WorldMapTerrain.SurfaceHeight(Route,s,side);
                }
            }
            float SurfaceError(float distance)
            {
                var q=ray.GetPoint(distance)-center;
                Decode(q,out _,out _,out var height);
                return Mathf.Sqrt(q.y*q.y+q.z*q.z)-(kit.radius-lateralRadius+Mathf.Sqrt(Mathf.Max(.001f,(lateralRadius+height)*(lateralRadius+height)-q.x*q.x)));
            }
            for(var i=0;i<10;i++)
            {
                var error=SurfaceError(t);if(Mathf.Abs(error)<.00001f)break;
                var derivative=(SurfaceError(t+.01f)-error)/.01f;if(Mathf.Abs(derivative)<.0001f)return false;
                t-=error/derivative;
            }
            if(t<0||Mathf.Abs(SurfaceError(t))>.002f)return false;
            var point=ray.GetPoint(t)-center;
            Decode(point,out var meters,out lateral,out _);
            level=Route.LevelAt(meters);return true;
        }

        WorldMapKit kit;
        LobbyFeaturesConfig art;
        RectTransform surface;
        RawImage image;
        Camera cam;
        RenderTexture rt;
        int layer, levelCount, maxShown, fixedWidth, fixedHeight;
        WorldMapCameraModel camModel;
        WorldMapTerrain terrain;
        Mesh tokenMesh, shadowMesh,skullMesh,rewardStarMesh;
        Material matSkull,matRewardStar;
        Material matSurface, matTokens, matWater, matShadow,matTreeLeaves;
        Material matGlow, matBeam, matSparkle;
        Material matVapor;
        Material matNumbers,matNumbersActive;
        VolumeProfile postProfile;
        readonly List<Material> hazeMaterials = new List<Material>();
        readonly Dictionary<Texture, Material> cardMaterials = new Dictionary<Texture, Material>();
        readonly Node[] nodes = new Node[MaxNodes];
        readonly List<Slot> slots = new List<Slot>();
        readonly Stack<Slot> free = new Stack<Slot>();
        readonly Dictionary<int, Slot> activeSlots = new Dictionary<int, Slot>();
        readonly List<int> releaseBuffer = new List<int>();
        List<PropPlacement> props;
        float builtCursor = float.NaN;
        bool disposed, christmas,manualRendering;

        float Now => TimeOverride >= 0 ? TimeOverride : Time.realtimeSinceStartup;
        Sprite boundGround;
        Vector2[] boundGroundRows;
        MapSeasonLook boundSeason;
        float boundSnowCoverage;
        void BindGround()
        {
            BindGround(matSurface,kit);
            if(islandMaterials!=null)
                for(var i=0;i<islandMaterials.Length;i++)BindGround(islandMaterials[i],islandKits[i]);
            boundGround=kit.GroundArt;boundGroundRows=kit.GroundBounds;
            boundSeason=kit.SeasonLook;
            boundSnowCoverage=kit.SeasonLook?.snowCoverage??.72f;
            builtCursor=float.NaN;
        }
        void BindGround(Material matSurface,WorldMapKit kit)
        {
            if(matSurface==null||kit.GroundArt==null)return;
            matSurface.SetFloat("_SimpleBase",kit.simpleBase?1:0);
            matSurface.SetFloat("_CentralEntranceGrade",kit.simpleBase&&WorldMapMission100.PolishedEntrance?1:0);
            matSurface.SetFloat("_SimpleGrade",kit.simpleBase&&!kit.customGround&&(kit.seasonPreview==1||kit.SeasonLook?.groundMaterials==null)?1:0);
            matSurface.SetFloat("_PathMint",kit.simpleBase||kit.simplePalette==1?1:0);
            matSurface.SetFloat("_SimpleGroundVariant",kit.simpleGroundVariant?1:0);
            matSurface.SetFloat("_GroundBrushAmount",kit.groundBrushLawn!=null&&kit.groundBrushPath!=null?1:0);
            if(kit.groundBrushLawn!=null)matSurface.SetTexture("_GroundBrushLawn",kit.groundBrushLawn);
            if(kit.groundBrushPath!=null)matSurface.SetTexture("_GroundBrushPath",kit.groundBrushPath);
            matSurface.SetVector("_SimpleLawnColor",(Vector4)kit.simpleLawnColor);
            matSurface.SetVector("_SimplePathColor",(Vector4)kit.simplePathColor);
            matSurface.SetFloat("_GreenIslandGradient",kit.simpleGreenGradient?1:0);
            matSurface.SetFloat("_IndependentPathColor",kit.simplePathOverride?1:0);
            matSurface.SetVector("_IslandSpan",new Vector4(kit.simpleIslandSpan.x,kit.simpleIslandSpan.y,0,0));
            matSurface.SetTexture("_PaintTex",kit.GroundArt.texture);
            matSurface.SetFloat("_GroundMode",1);
            if(kit.GroundBounds!=null&&kit.GroundBounds.Length==64)
            {
                var bounds=new Vector4[64];
                for(var i=0;i<64;i++)bounds[i]=new Vector4(kit.GroundBounds[i].x,kit.GroundBounds[i].y,0,0);
                matSurface.SetVectorArray("_GroundBounds",bounds);
            }
            if(kit.bridgeDeck!=null)
            {
                var r=kit.bridgeDeck.rect;var t=kit.bridgeDeck.texture;
                matSurface.SetTexture("_BridgeTex",t);
                matSurface.SetVector("_BridgeRect",new Vector4(r.x/t.width,r.y/t.height,r.width/t.width,r.height/t.height));
                foreach(var pair in new[]{("_BridgeWoodRect",kit.bridgeTimber),("_BridgeLogsRect",kit.bridgeLogs)})
                    if(pair.Item2!=null){var b=pair.Item2.rect;var texture=pair.Item2.texture;matSurface.SetVector(pair.Item1,new Vector4(b.x/texture.width,b.y/texture.height,b.width/texture.width,b.height/texture.height));if(pair.Item1=="_BridgeWoodRect")matSurface.SetTexture("_BridgeWoodTex",texture);}
            }
            matSurface.SetFloat("_UseTerrainTiles",kit.terrainMaterials!=null?1:0);
            matSurface.SetFloat("_LavaTerrain",kit.Lava?1:0);
            matSurface.SetFloat("_SnowCoverage",kit.Christmas?(kit.SeasonLook?.snowCoverage??.72f):0);
            var winter=kit.seasonPreview!=1&&kit.SeasonLook?.groundMaterials!=null?kit.SeasonLook.groundMaterials:kit.snowTerrainMaterials;
            matSurface.SetFloat("_HasSnowTex",winter!=null?1:0);
            if(winter!=null)
            {
                var r=winter.rect;var t=winter.texture;matSurface.SetTexture("_SnowTex",t);
                matSurface.SetVector("_SnowRect",new Vector4(r.x/t.width,r.y/t.height,r.width/t.width,r.height/t.height));
                matSurface.SetFloat("_SnowRowFlip",winter==kit.snowTerrainMaterials?1:0);
            }
            matSurface.SetVector("_LaneShape",new Vector4(WorldMapTerrain.PathHalf,WorldMapTerrain.SlopeW,WorldMapTerrain.RimW,WorldMapTerrain.BankH));
            var groundMaterial=kit.seasonPreview!=1&&kit.SeasonLook?.groundMaterials!=null?kit.SeasonLook.groundMaterials:kit.terrainMaterials;
            if(groundMaterial!=null)
            {
                matSurface.SetFloat("_UseTerrainTiles",1);
                var r=groundMaterial.rect;var t=groundMaterial.texture;
                matSurface.SetTexture("_TerrainTiles",t);
                matSurface.SetVector("_TerrainRect",new Vector4(r.x/t.width,r.y/t.height,r.width/t.width,r.height/t.height));
            }
            matSurface.SetColor("_GroundRimLight",kit.Lava?new Color(.6f,.36f,.22f):new Color(.36f,.5f,.28f));
            matSurface.SetVector("_GrassGrade",kit.Lava?Vector4.zero:new Vector4(.151f,.462f,.242f,.3f));
        }

        // surface = RectTransform do RawImage que mostra o mundo (null no modo captura do editor).
        public static WorldMapView Create(RectTransform surface, LobbyFeaturesConfig art, WorldMapKit kit,
            int levelCount, int futureShown, bool editorOnly = false,WorldMapIslands islandsOverride=null)
        {
            if (kit == null || !kit.Ready) return null;
            var layer = LayerMask.NameToLayer(WorldMapKit.LayerName);
            if (layer < 0) return null;
            var go = new GameObject("WorldMap3D") { layer = layer };
            // Fora do play mode (capturas, janela de preview) nada pode ser salvo na cena aberta do usuario.
            if (editorOnly || !Application.isPlaying) go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.position = RootOffset + Vector3.right * (SlotSpacing * FreeSlot());
            var view = go.AddComponent<WorldMapView>();
            view.Init(surface, art, kit, layer, levelCount, futureShown, editorOnly,islandsOverride);
            return view;
        }

        // Primeira faixa sem outro WorldMapView (sobrevive a domain reload, ao contrario de um contador estatico).
        static int FreeSlot()
        {
            var views = Resources.FindObjectsOfTypeAll<WorldMapView>();
            for (var slot = 0; slot < 256; slot++)
            {
                var taken = false;
                foreach (var v in views)
                    if (v != null && Mathf.Abs(v.transform.position.x - RootOffset.x - SlotSpacing * slot) < SlotSpacing * 0.5f) { taken = true; break; }
                if (!taken) return slot;
            }
            return 0;
        }

        void Init(RectTransform surface, LobbyFeaturesConfig art, WorldMapKit kit, int layer, int levelCount, int futureShown, bool editorOnly,WorldMapIslands islandsOverride)
        {
            this.surface = surface;
            this.art = art;
            this.kit = kit;
            this.layer = layer;
            this.levelCount = Mathf.Max(0, levelCount);
            manualRendering=editorOnly||!Application.isPlaying;
            maxShown = this.levelCount;
            Route = new WorldMapRoute(islandsOverride??Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath),this.levelCount);
            this.kit=Instantiate(kit);kit=this.kit;kit.hideFlags=HideFlags.HideAndDontSave;kit.simpleBase=Route.Islands?.unifiedBase==true;
            if(kit.simpleBase)
            {
                kit.terrainTheme=WorldMapTerrainTheme.GreenLand;
                kit.bridgeDeck=kit.bridgeLogs=kit.simpleBridge??kit.bridgeDeck;
                kit.levelCrown=kit.simpleCrown??kit.levelCrown;kit.levelStar=kit.simpleStar??kit.levelStar;
                kit.hardBadge=kit.simpleHard??kit.hardBadge;kit.expressBadge=kit.simpleExpress??kit.expressBadge;
            }
            if(Route.Islands!=null)
            {
                islandKits=new WorldMapKit[Route.Islands.islands.Count];
                for(var i=0;i<islandKits.Length;i++)
                {
                    var island=Route.Islands.islands[i];var style=Instantiate(kit);
                    style.hideFlags=HideFlags.HideAndDontSave;style.terrainTheme=kit.simpleBase?(island.groundStyle==WorldMapGroundStyle.Volcanic?WorldMapTerrainTheme.LavaLand:WorldMapTerrainTheme.GreenLand):island.terrain;style.simpleBase=kit.simpleBase;style.simplePalette=island.palette;
                    style.simpleGroundVariant=island.groundStyle!=WorldMapGroundStyle.Green||island.customColors;
                    style.simpleGreenGradient=island.groundStyle==WorldMapGroundStyle.Green&&!island.customColors;
                    style.simplePathOverride=island.customPathColor||island.customColors||island.groundStyle!=WorldMapGroundStyle.Green;
                    style.simpleIslandSpan=new Vector2(Route.MetersAt(Route.Islands.FirstLevel(i)),Route.MetersAt(Route.Islands.FirstLevel(i)+Route.Islands.levelsPerIsland-1));
                    var floor=WorldMapIslands.IllustratedPalette(island);style.simpleLawnColor=floor.outerA;style.simplePathColor=floor.pathA;
                    style.customGround=(island.customColors&&!kit.simpleBase)||island.groundMaterials!=null;
                    if(island.groundMaterials!=null)style.terrainMaterials=island.groundMaterials;
                    islandKits[i]=style;
                }
            }
            camModel = new WorldMapCameraModel(Route, kit);
            terrain = new WorldMapTerrain();
            tokenMesh = WorldMapTokenMesh.Build();
            rewardStarMesh=WorldMapTokenMesh.BuildRewardStar();
            matRewardStar=MakeMaterial(kit.surface,"RewardStars_NoOutline",false);matRewardStar.SetFloat("_NightAmount",0);matRewardStar.SetFloat("_PaintAmount",0);matRewardStar.SetFloat("_TokenMode",0);matRewardStar.SetFloat("_GroundMode",0);
            skullMesh=WorldMapTokenMesh.BuildSkull();matSkull=MakeMaterial(kit.surface,"SeloCaveira",false);matSkull.SetFloat("_NightAmount",0);
            shadowMesh = WorldMapCards.ShadowQuad();
            var flags = editorOnly || !Application.isPlaying ? HideFlags.HideAndDontSave : HideFlags.None;

            matSurface = MakeMaterial(kit.surface, "MapSurface", true);
            if (kit.groundIllustration != null)
            {
                matSurface.SetTexture("_PaintTex", kit.groundIllustration.texture);
                matSurface.SetFloat("_GroundMode", 1);
                if (kit.groundLaneBounds != null && kit.groundLaneBounds.Length == 64)
                {
                    var bounds = new Vector4[64];
                    for (var i = 0; i < bounds.Length; i++) bounds[i] = new Vector4(kit.groundLaneBounds[i].x,kit.groundLaneBounds[i].y,0,0);
                    matSurface.SetVectorArray("_GroundBounds", bounds);
                }
            }
            matWater = MakeMaterial(kit.water, "MapWater", true);
            matWater.SetFloat("_HasWakeSprite",kit.waterRipple!=null?1:0);
            matTokens = MakeMaterial(kit.surface,"MapTokens",true);
            matTokens.SetFloat("_GroundMode",0);
            matTokens.SetFloat("_TokenMode",1);
            matShadow = MakeMaterial(kit.shadow, "MapShadow", false);
            matShadow.SetColor("_Color", new Color(0.21f, 0.10f, 0.35f, 0.32f));
            foreach (var m in new[] { matSurface, matTokens, matWater, matShadow }) m.hideFlags = flags;
            BindGround();
            if(islandKits!=null)
            {
                islandMaterials=new Material[islandKits.Length];
                for(var i=0;i<islandMaterials.Length;i++)
                {
                    var m=MakeMaterial(kit.surface,"MapIsland_"+i,true);m.hideFlags=flags;
                    islandMaterials[i]=m;BindGround(m,islandKits[i]);
                }
            }

            var camGo = new GameObject("Camera") { layer = layer };
            camGo.transform.SetParent(transform, false);
            cam = camGo.AddComponent<Camera>();
            cam.cullingMask = 1 << layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 160;
            cam.fieldOfView = camModel.VerticalFov(cam.aspect);
            cam.orthographic = kit.illustratedProjection;
            cam.orthographicSize = camModel.IllustratedSize(cam.aspect);
            cam.allowHDR = Route.Islands!=null&&Route.Islands.postProcessing;
            cam.allowMSAA = true;
            cam.depth = -50;
            cam.useOcclusionCulling = false;
            var data = cam.GetUniversalAdditionalCameraData();
            data.SetRenderer(kit.rendererIndex);
            data.renderPostProcessing = cam.allowHDR;
            data.renderShadows = false;
            data.requiresDepthTexture = false;
            data.requiresColorTexture = false;
            data.volumeLayerMask = 1<<layer;
            if(data.renderPostProcessing)
            {
                var saved=Resources.Load<VolumeProfile>("RoyalAvesMapPostProfile");
                postProfile=ScriptableObject.CreateInstance<VolumeProfile>();
                if(saved!=null)foreach(var component in saved.components)postProfile.components.Add(Instantiate(component));
                postProfile.hideFlags=HideFlags.HideAndDontSave;
                if(!postProfile.TryGet<Bloom>(out var glow))glow=postProfile.Add<Bloom>(true);
                glow.intensity.Override(Route.Islands.bloom);glow.threshold.Override(.95f);
                if(!postProfile.TryGet<ColorAdjustments>(out var grade))grade=postProfile.Add<ColorAdjustments>(true);
                grade.saturation.Override(Route.Islands.vibrance);grade.contrast.Override(3);
                var volumeGo=new GameObject("LuzDoMapa",typeof(BoxCollider),typeof(Volume)){layer=layer};volumeGo.transform.SetParent(transform,false);
                volumeGo.GetComponent<BoxCollider>().size=Vector3.one*180;volumeGo.GetComponent<BoxCollider>().isTrigger=true;
                var volume=volumeGo.GetComponent<Volume>();volume.isGlobal=false;volume.priority=20;volume.sharedProfile=postProfile;
            }
            if(kit.glow!=null)matGlow=new Material(kit.glow){hideFlags=flags};
            if(kit.beam!=null)matBeam=new Material(kit.beam){hideFlags=flags};
            if(kit.sparkle!=null)matSparkle=new Material(kit.sparkle){hideFlags=flags};

            AddMesh("Chao", terrain.ground, matSurface);
            if(islandMaterials!=null)transform.Find("Chao").GetComponent<MeshRenderer>().sharedMaterials=islandMaterials;
            AddMesh("Agua", terrain.water, matWater);
            for (var i = 0; i < nodes.Length; i++) nodes[i] = CreateNode(i);
            props = WorldMapScenery.Build(Route, kit, SceneryLimit,KitAt);

            if (surface != null)
            {
                image = surface.GetComponent<RawImage>();
                if (image == null) image = surface.gameObject.AddComponent<RawImage>();
                image.raycastTarget = true;
                image.color = Color.white;
            }
            cam.enabled = !manualRendering;
        }

        Material MakeMaterial(Shader shader, string name, bool haze)
        {
            var m = new Material(shader) { name = name };
            if (haze) hazeMaterials.Add(m);
            return m;
        }

        void AddMesh(string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        Material CardMaterial(Texture texture)
        {
            if (cardMaterials.TryGetValue(texture, out var m) && m != null) return m;
            m = MakeMaterial(kit.card, "MapCard_" + texture.name, true);
            if (!Application.isPlaying) m.hideFlags = HideFlags.HideAndDontSave;
            m.SetTexture("_MainTex", texture);
            cardMaterials[texture] = m;
            return m;
        }

        // ---- criacao de objetos ---------------------------------------------------------------------

        readonly Dictionary<Texture,Material> effectMaterials=new Dictionary<Texture,Material>();
        Material EffectMaterial(Texture texture)
        {
            if(effectMaterials.TryGetValue(texture,out var material))return material;
            material=new Material(kit.vapor){hideFlags=HideFlags.HideAndDontSave};material.SetTexture("_MainTex",texture);
            material.SetFloat("_ReflectionInk",kit.waterRipple!=null&&texture==kit.waterRipple.texture?1:0);
            effectMaterials[texture]=material;hazeMaterials.Add(material);return material;
        }
        Node CreateNode(int index)
        {
            var node = new Node();
            node.go = new GameObject("Nivel") { layer = layer };
            node.t = node.go.transform;
            node.t.SetParent(transform, false);
            node.go.AddComponent<MeshFilter>().sharedMesh = tokenMesh;
            node.body = node.go.AddComponent<MeshRenderer>();
            node.body.sharedMaterial = matTokens;
            node.body.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            node.body.receiveShadows = false;
            // O volume de clique deve coincidir com o disco, sobretudo no horizonte.
            // A caixa alta interceptava o raio destinado ao nivel seguinte.
            var collider = node.go.AddComponent<MeshCollider>();
            collider.sharedMesh = tokenMesh;
            node.reference = node.go.AddComponent<WorldMapNodeRef>();
            node.decorationRoot=new GameObject("OrnamentosDoNivel").transform;
            node.decorationRoot.SetParent(transform,false);
            var badgeGo=new GameObject("SeloDeDificuldade",typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};
            badgeGo.transform.SetParent(node.decorationRoot,false);
            node.badge=badgeGo.GetComponent<MeshRenderer>();node.badgeMesh=badgeGo.GetComponent<MeshFilter>();badgeGo.SetActive(false);
            var tokenShadow=new GameObject("SombraDoMedalhao",typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};
            tokenShadow.transform.SetParent(node.t,false);node.shadowT=tokenShadow.transform;
            tokenShadow.GetComponent<MeshFilter>().sharedMesh=shadowMesh;
            tokenShadow.GetComponent<MeshRenderer>().sharedMaterial=matShadow;

            var textGo = new GameObject("Numero", typeof(RectTransform)) { layer = layer };
            textGo.transform.SetParent(node.t, false);
            node.numberT = textGo.transform;
            node.numberT.localRotation = Quaternion.Euler(90, 0, 0);
            node.number = textGo.AddComponent<TextMeshPro>();
            node.number.rectTransform.sizeDelta = new Vector2(0.62f, 0.44f);
            if (art != null && art.displayFont != null) node.number.font = art.displayFont;
            node.number.alignment = TextAlignmentOptions.Center;
            node.number.fontStyle = FontStyles.Normal;
            node.number.color = Color.white;
            node.number.enableAutoSizing = true;
            node.number.fontSizeMin = 1;
            node.number.fontSizeMax = 3.6f;
            node.number.textWrappingMode = TextWrappingModes.NoWrap;
            node.number.overflowMode = TextOverflowModes.Overflow;
            // TMP's outline setters call Renderer.material, cloning a material per node in edit mode.
            if(matNumbers==null)
            {
                matNumbers=new Material(node.number.fontSharedMaterial){name="NumerosDoMapa",hideFlags=HideFlags.HideAndDontSave};
                matNumbers.SetFloat(ShaderUtilities.ID_OutlineWidth,.045f);
                matNumbers.SetColor(ShaderUtilities.ID_OutlineColor,new Color32(69,29,84,255));
                matNumbersActive=new Material(matNumbers){name="NumeroAtualDoMapa",hideFlags=HideFlags.HideAndDontSave};
                matNumbersActive.SetColor(ShaderUtilities.ID_OutlineColor,new Color32(69,29,84,255));
            }
            node.number.fontSharedMaterial=matNumbers;
            node.number.UpdateMeshPadding();

            if (rewardStarMesh != null)
            {
                for (var j = 0; j < 3; j++)
                {
                    var starGo = new GameObject("Estrela_" + (j + 1)) { layer = layer };
                    starGo.transform.SetParent(node.decorationRoot, false);
                    starGo.AddComponent<MeshFilter>().sharedMesh = rewardStarMesh;
                    var star = starGo.AddComponent<MeshRenderer>();
                    star.sharedMaterial = matRewardStar;
                    star.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    star.receiveShadows = false;
                    node.stars[j] = star;
                }
            }
            if (kit.levelCrown != null)
            {
                var crownGo = new GameObject("Coroa") { layer = layer };
                crownGo.transform.SetParent(node.decorationRoot, false);
                crownGo.AddComponent<MeshFilter>().sharedMesh = WorldMapCards.Get(kit.levelCrown, .42f);
                node.crown = crownGo.AddComponent<MeshRenderer>();
                node.crown.sharedMaterial = CardMaterial(kit.levelCrown.texture);
                var crownBlock=new MaterialPropertyBlock();crownBlock.SetFloat("_NightAmount",0);node.crown.SetPropertyBlock(crownBlock);
                node.crown.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                node.crown.receiveShadows = false;
            }
            node.go.SetActive(false);
            node.decorationRoot.gameObject.SetActive(false);
            return node;
        }

        Slot CreateSlot()
        {
            var slot = new Slot { go = new GameObject("Objeto") { layer = layer } };
            slot.t = slot.go.transform;
            slot.t.SetParent(transform, false);
            var cardGo = new GameObject("Cartao") { layer = layer };
            slot.card = cardGo.transform;
            slot.card.SetParent(slot.t, false);
            slot.cardMesh = cardGo.AddComponent<MeshFilter>();
            slot.cardRenderer = cardGo.AddComponent<MeshRenderer>();
            slot.cardRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            slot.cardRenderer.receiveShadows = false;
            slot.snowRoot=new GameObject("NeveDoObjeto"){layer=layer}.transform;slot.snowRoot.SetParent(slot.card,false);
            for (var i = 0; i < slot.caps.Length; i++)
            {
                var cap = new GameObject("CoberturaDeNeve_" + i) { layer = layer };
                cap.transform.SetParent(slot.snowRoot, false);
                slot.caps[i] = cap.AddComponent<MeshFilter>();
                slot.capRenderers[i] = cap.AddComponent<MeshRenderer>();
                slot.capRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                slot.capRenderers[i].receiveShadows = false;
                cap.SetActive(false);
            }
            for(var i=0;i<slot.animation.Length;i++)
            {
                var go=new GameObject(i==0?"JatoIndependente":"VaporIndependente_"+i){layer=layer};go.transform.SetParent(slot.card,false);
                slot.animation[i]=go.AddComponent<MeshFilter>();slot.animationRenderers[i]=go.AddComponent<MeshRenderer>();
                slot.animationRenderers[i].shadowCastingMode=ShadowCastingMode.Off;slot.animationRenderers[i].receiveShadows=false;go.SetActive(false);
            }
            var shadowGo = new GameObject("Sombra") { layer = layer };
            slot.shadowT = shadowGo.transform;
            slot.shadowT.SetParent(slot.t, false);
            shadowGo.AddComponent<MeshFilter>().sharedMesh = shadowMesh;
            slot.shadowRenderer = shadowGo.AddComponent<MeshRenderer>();
            slot.shadowRenderer.sharedMaterial = matShadow;
            slot.shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            slot.shadowRenderer.receiveShadows = false;
            var lightGo=new GameObject("LuzDeContato",typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};lightGo.transform.SetParent(slot.t,false);
            slot.lightT=lightGo.transform;lightGo.GetComponent<MeshFilter>().sharedMesh=shadowMesh;
            slot.lightRenderer=lightGo.GetComponent<MeshRenderer>();slot.lightRenderer.sharedMaterial=matGlow;
            slot.lightRenderer.enabled=false;
            slot.go.SetActive(false);
            slots.Add(slot);
            return slot;
        }

        // ---- ciclo de vida --------------------------------------------------------------------------

        public void SetActive(bool active)
        {
            if(!active)santa?.Reset();
            if (disposed) return;
            gameObject.SetActive(active);
            if (cam != null) cam.enabled = active && image != null&&!manualRendering;
        }

        public void SetFixedSize(int width, int height)
        {
            fixedWidth = width;
            fixedHeight = height;
        }

        void EnsureTarget()
        {
            var width = fixedWidth;
            var height = fixedHeight;
            if (surface != null)
            {
                var canvas = surface.GetComponentInParent<Canvas>();
                var scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1;
                width = Mathf.RoundToInt(surface.rect.width * scale * kit.renderScale);
                height = Mathf.RoundToInt(surface.rect.height * scale * kit.renderScale);
            }
            if (width < 16 || height < 16) return;
            var limit = Mathf.Max(width, height) / 2048f;
            if (limit > 1) { width = Mathf.RoundToInt(width / limit); height = Mathf.RoundToInt(height / limit); }
            if (rt != null && rt.width == width && rt.height == height) return;
            if (rt != null) { cam.targetTexture = null; rt.Release(); Kill(rt); }
            var descriptor=new RenderTextureDescriptor(width,height,cam.allowHDR?RenderTextureFormat.ARGBHalf:RenderTextureFormat.ARGB32,24){msaaSamples=4,useMipMap=false};
            descriptor.msaaSamples=SystemInfo.GetRenderTextureSupportedMSAASampleCount(descriptor);
            rt = new RenderTexture(descriptor)
            {
                name = "WorldMapRT", filterMode = FilterMode.Bilinear, useMipMap = false
            };
            if (Application.isPlaying == false) rt.hideFlags = HideFlags.HideAndDontSave;
            rt.Create();
            cam.targetTexture = rt;
            cam.aspect = (float)width / height;
            if (image != null) image.texture = rt;
        }

        // Chamado todo quadro enquanto o mapa esta aberto.
        public void Tick(float cursorLevels, int nextLevel, bool dragging)
        {
            if (disposed) return;
            EnsureTarget();
            var cursor = Route.MetersAt(cursorLevels);
            matWater.SetFloat("_MapTime",Now);
            matWater.SetFloat("_Frozen",kit.Christmas?1:0);
            if(boundGround!=kit.GroundArt||boundGroundRows!=kit.GroundBounds||boundSeason!=kit.SeasonLook||christmas!=kit.Christmas||boundSnowCoverage!=(kit.SeasonLook?.snowCoverage??.72f))BindGround();
            matSurface.SetFloat("_PaintScroll", (cursor - Route.Pos(1)) / 18f);
            if(islandMaterials!=null)foreach(var m in islandMaterials)m.SetFloat("_PaintScroll",(cursor-Route.Pos(1))/18f);
            christmas = kit.Christmas;
            camModel.Aspect=cam.aspect;
            cam.transform.localPosition = camModel.Position(cursor);
            cam.transform.localRotation = camModel.Rotation;
            cam.fieldOfView = camModel.VerticalFov(cam.aspect);
            cam.orthographic = kit.illustratedProjection;
            cam.orthographicSize = camModel.IllustratedSize(cam.aspect);
            var palette = Route.Islands==null&&kit.Lava?WorldMapRoute.LavaPalette:Route.PaletteAt(cursor + 18);
            UpdateNight(cursor);
            SetHaze(palette.haze);
            if (float.IsNaN(builtCursor) || Mathf.Abs(builtCursor - cursor) > 0.0005f)
            {
                // Only the front hemisphere can be visible. Keep sprite draw range independent so
                // tall illustrated silhouettes can still emerge behind the horizon.
                terrain.IllustratedBridge=kit.bridgeDeck!=null;terrain.Lava=Route.Islands==null&&kit.Lava;terrain.Scenery=props;
                terrain.Rebuild(Route, kit.radius, cursor, 6f, 18f);
                builtCursor = cursor;
            }
            UpdateNodes(cursor, nextLevel, dragging);
            UpdateProps(cursor);
            UpdateIceReflections(cursor);
            UpdateShafts(cursor);
            if(santa==null&&kit.santaEventEnabled&&(kit.santaFriendly!=null||kit.santaVillain!=null))santa=new WorldMapSantaEvent(this,kit,layer,CardMaterial);
            santa?.Tick(Now,dragging);
        }

        // Render sincrono para capturas fora do play mode.
        public void RenderNow()
        {
            EnsureTarget();
            cam.Render();
        }

        public float FocusOffsetLevels => kit.focusAhead / WorldMapRoute.Spacing;
        public float CursorForLevel(float level)
        {
            var cursor=Route.LevelAt(Route.MetersAt(level)-kit.focusAhead);
            return kit.simpleBase&&level<4?Mathf.Lerp(1,cursor,Mathf.SmoothStep(0,1,(level-1)/3)):cursor;
        }
        public float TokenHalfWidth => .46f * kit.tokenScale;

        public string ChapterTitle(float cursorLevels)
        {
            if(Route.Islands!=null)return Route.IslandAt(Route.MetersAt(cursorLevels)+kit.focusAhead).title;
            var level = Mathf.Clamp(Mathf.RoundToInt(cursorLevels + 2), 1, WorldMapRoute.Positions);
            return WorldMapEditorial.ChapterTitles[WorldMapRoute.ChapterOf(level)];
        }

        public int ChapterStart(float cursorLevels) => Route.Islands!=null?
            Route.Islands.FirstLevel(Route.IslandIndexAt(Route.MetersAt(cursorLevels)+kit.focusAhead)):
            WorldMapRoute.ChapterOf(Mathf.Clamp(Mathf.RoundToInt(cursorLevels + 2), 1, WorldMapRoute.Positions)) * WorldMapEditorial.LevelsPerChapter + 1;

        readonly Vector4[] iceLights=new Vector4[12],iceLightColors=new Vector4[12];
        void UpdateIceReflections(float cursor)
        {
            int count=0;
            for(int pass=0;pass<2;pass++)foreach(var p in props)
            {
                if(count==iceLights.Length)break;
                if(p.s<cursor-PropBack||p.s>cursor+18)continue;
                bool lamp=p.kind==PropKind.Lamp,window=p.kind==PropKind.Landmark;
                bool bulbs=WorldMapScenery.HasIceTheme(p,christmas)&&(p.kind==PropKind.Tree||WorldMapScenery.IsShrub(p,kit))&&p.lightsEffect.Visible(true);
                if(pass==0?!(lamp||window):!bulbs)continue;
                iceLights[count]=new Vector4(p.offset,p.s,lamp?1.8f:window?1.1f:.9f,lamp?1.10f:window?.70f:.50f);
                var tint=lamp||window?new Color(1,.57f,.14f):count%2==0?new Color(1,.12f,.05f):new Color(.12f,1,.70f);
                if(lamp)tint=WorldMapLampArt.Tint(p.sprite);
                iceLightColors[count]=QualitySettings.activeColorSpace==ColorSpace.Linear?tint.linear:tint;count++;
            }
            void Bind(Material m)
            {
                if(m==null)return;m.SetFloat("_IceNight",night);m.SetFloat("_IceClock",Now);m.SetFloat("_IceLightCount",count);
                m.SetVectorArray("_IceLights",iceLights);m.SetVectorArray("_IceLightColors",iceLightColors);
            }
            Bind(matSurface);Bind(matWater);if(islandMaterials!=null)foreach(var m in islandMaterials)Bind(m);
            foreach(var m in cardMaterials.Values)Bind(m);
        }
        float night;
        bool nightInit;
        float nightStarted,nightLastTime,nightTarget;
        public float Night => night;
        public bool NightNeedsFrames=>kit.dayNightCycle||night>.01f||Mathf.Abs(night-nightTarget)>.001f;
        public void ConfigureDayNight(bool cycle,float minutes,float amount)
        {
            if(cycle&&!kit.dayNightCycle)nightStarted=Now;
            kit.dayNightCycle=cycle;kit.dayNightCycleMinutes=minutes;kit.night=Mathf.Clamp01(amount);
        }
        GameObject moonPool;
        MeshRenderer moonPoolRenderer;
        MaterialPropertyBlock moonBlock;

        void UpdateNight(float cursor)
        {
            if(moonBlock==null)moonBlock=new MaterialPropertyBlock();
            var target = Mathf.Clamp01(kit.night);
            if(!nightInit)nightStarted=nightLastTime=Now;
            if(kit.dayNightCycle&&TimeOverride<0)target=WorldMapKit.NightAt(Now-nightStarted,kit.dayNightCycleMinutes);
            nightTarget=target;
            night = !nightInit || TimeOverride >= 0 ? target : Mathf.MoveTowards(night, target, Mathf.Clamp(Now-nightLastTime,0,.1f)*.28f);
            nightLastTime=Now;
            nightInit = true;
            if (matGlow == null) return;
            if (moonPool == null)
            {
                moonPool = new GameObject("PocaDeLuar", typeof(MeshFilter), typeof(MeshRenderer)) { layer = layer };
                moonPool.transform.SetParent(transform, false);
                moonPool.GetComponent<MeshFilter>().sharedMesh = shadowMesh;
                moonPoolRenderer = moonPool.GetComponent<MeshRenderer>();
                moonPoolRenderer.sharedMaterial = matGlow;
                moonPoolRenderer.shadowCastingMode = ShadowCastingMode.Off;
                moonPoolRenderer.receiveShadows = false;
            }
            if (moonPoolRenderer == null) moonPoolRenderer = moonPool.GetComponent<MeshRenderer>();
            if (moonPoolRenderer == null) return;
            moonPool.SetActive(night > .02f);
            if (night <= .02f) return;
            // mancha de luar sobre o chao, a frente da camera: a lua reflete no terreno
            const float rel = 12f;
            var s = cursor + rel;
            var x = Route.CenterX(s);
            moonPool.transform.localPosition = MapCurve.Point(rel, x, .04f, kit.radius, Route.Curvature);
            moonPool.transform.localRotation = MapCurve.Orientation(rel, kit.radius, x, Route.Curvature);
            moonPool.transform.localScale = new Vector3(9f, 1, 5.5f);
            moonBlock.SetColor("_Color", new Color(.55f, .72f, 1f, .34f * night));
            moonPoolRenderer.SetPropertyBlock(moonBlock);
        }

        void SetHaze(Color haze)
        {
            haze = Color.Lerp(haze, new Color(.09f, .13f, .30f), night);
            var nightColor = new Color(.42f, .50f, .68f);
            if (islandMaterials != null)
                foreach (var im in islandMaterials)
                    if (im != null) { im.SetFloat("_NightAmount", night); im.SetColor("_NightColor", nightColor); }
            foreach (var m in hazeMaterials)
            {
                if (m == null) continue;
                m.SetColor("_HazeColor", haze);
                m.SetFloat("_NightAmount", night);
                m.SetColor("_NightColor", nightColor);
                m.SetFloat("_HazeNear", 16);
                m.SetFloat("_HazeFar", 56);
            }
        }

        // ---- medalhoes ------------------------------------------------------------------------------

        void UpdateNodes(float cursor, int nextLevel, bool dragging)
        {
            var first = Mathf.Max(1, Mathf.FloorToInt(Route.LevelAt(cursor - 5)));
            var used = 0;
            for (var level = first; level <= maxShown && used < nodes.Length; level++)
            {
                if(LevelExists!=null&&!LevelExists(level))continue;
                var s = Route.Pos(level);
                var rel = s - cursor;
                if (rel > Front - 12) break;
                var node = nodes[used++];
                node.go.SetActive(true);
                Place(node, level, s, rel, nextLevel, dragging);
            }
            for (var i = used; i < nodes.Length; i++){nodes[i].go.SetActive(false);nodes[i].decorationRoot.gameObject.SetActive(false);}
        }

        void Place(Node node, int level, float s, float rel, int nextLevel, bool dragging)
        {
            var radius = kit.radius;
            var playable = level <= levelCount;
            var active = playable && level == nextLevel;
            var done = playable && level < nextLevel;
            var locked = !playable || level > nextLevel;
            var earned = done ? (LevelsMap._mapProgressManager?.LoadLevelStarsCount(level) ?? 0) : 0;
            if (!Application.isPlaying && done && earned == 0) earned = 3; // preview only, never writes scores
            earned = Mathf.Clamp(earned, 0, 3);
            var key = level * 32 + (active ? 1 : 0) + (done ? 2 : 0) + (playable ? 4 : 0) + earned * 8;
            if (node.stateKey != key)
            {
                node.stateKey = key;
                node.level = level;
                node.go.name = "Nivel_" + level;
                node.reference.level = level;
                node.number.text = level.ToString();
                node.number.fontSizeMax = level >= 100 ? 2.6f : 3.6f;
                node.number.color = locked ? new Color(0.93f, 0.93f, 0.97f) : Color.white;
                node.number.ForceMeshUpdate();
                node.numberT.localPosition = new Vector3(0, WorldMapTokenMesh.FaceHeight + .012f, 0);
                node.number.fontSharedMaterial=active?matNumbersActive:matNumbers;
                for (var j = 0; j < 3; j++) if (node.stars[j] != null)
                {
                    var star=node.stars[j];star.gameObject.SetActive(done||active);
                    // Score ornaments remain legible at night; silver slots show missing stars.
                    var starBlock=new MaterialPropertyBlock();
                    starBlock.SetFloat("_NightAmount",0);starBlock.SetFloat("_HazeNear",1000);starBlock.SetFloat("_HazeFar",2000);
                    starBlock.SetFloat("_Desat",j<earned?0:.92f);
                    starBlock.SetColor("_Tint",j<earned?Color.white:new Color(.72f,.77f,.85f,1));
                    star.SetPropertyBlock(starBlock);
                }
                var difficulty=WorldMapEditorial.DifficultyOf(level);
                if(difficulty!='n')node.numberT.localPosition+=new Vector3(0,0,-.07f);
                var emblem=difficulty=='v'?kit.expressBadge:difficulty=='h'?kit.hardBadge:null;
                node.badge.gameObject.SetActive(emblem!=null);
                if(node.crown!=null)node.crown.gameObject.SetActive(earned==3&&emblem==null);
                if(difficulty!='n'){node.badgeMesh.sharedMesh=skullMesh;node.badge.sharedMaterial=matSkull;node.badge.gameObject.SetActive(true);}
                node.block.Clear();
                node.block.SetColor("_Tint", Color.white);
                node.block.SetFloat("_PaintAmount", 0);
                node.block.SetFloat("_GroundMode", 0);
                node.block.SetFloat("_TokenMode", 1);
                var faceSprite=active?kit.levelFaceActive:done?kit.levelFaceCompleted:kit.levelFaceUpcoming;
                if(faceSprite!=null)
                {
                    var rect=faceSprite.rect;var tex=faceSprite.texture;
                    node.block.SetTexture("_TokenFaceTex",tex);
                    node.block.SetVector("_TokenFaceRect",new Vector4(rect.x/tex.width,rect.y/tex.height,rect.width/tex.width,rect.height/tex.height));
                    node.block.SetFloat("_TokenFaceAmount",0);
                }
                Color face = active ? new Color32(164,28,255,255) : done ? new Color32(255,145,8,255) : new Color32(139,20,242,255);
                if(difficulty=='h')face=new Color32(225,5,35,255);
                if(difficulty=='v')face=new Color32(166,0,26,255);
                node.block.SetColor("_FaceColor", QualitySettings.activeColorSpace == ColorSpace.Linear ? face.linear : face);
                node.block.SetFloat("_Desat", 0);
                node.body.SetPropertyBlock(node.block);
            }
            var x = Route.CenterX(s);
            var pulse = active && !dragging ? 1 + 0.05f * Mathf.Sin(Now * 3) : 1;
            if (active)
            {
                node.block.SetFloat("_Glow", 0);
                node.body.SetPropertyBlock(node.block);
            }
            var normal = MapCurve.Normal(rel, radius, x,Route.Curvature);
            var near=1-Mathf.InverseLerp(0,12,rel);
            var faceLift = kit.illustratedProjection ? Mathf.Lerp(-9,-20,near) : 0;
            node.t.localRotation = MapCurve.Orientation(rel, radius, x,Route.Curvature) * Quaternion.Euler(faceLift,0,0);
            // The body follows the ground. Reward silhouettes face the camera without inheriting
            // the body's non-uniform thickness, which used to shear crowns and flatten the stars.
            var rewardRotation=Quaternion.Euler(-faceLift,0,0);
            var apparent = kit.illustratedProjection ? Mathf.Lerp(1,.64f,Mathf.InverseLerp(0,14,rel)) : 1;
            var thickness=kit.illustratedProjection?Mathf.Lerp(1,1.7f,Mathf.InverseLerp(2,14,rel)):1;
            var size=kit.tokenScale*apparent*(active?1.14f*pulse:1);
            // Keep the tilted front edge above the terrain; burying it clipped the cream rim and stars.
            var support=WorldMapTokenMesh.Radius*size*Mathf.Abs(Mathf.Sin(faceLift*Mathf.Deg2Rad))+.008f;
            node.t.localPosition=MapCurve.Point(rel,x,0,radius,Route.Curvature)+normal*support;
            node.t.localScale = new Vector3(size,size*thickness,size);
            void Billboard(Transform decoration,Vector3 anchor)
            {
                decoration.localPosition=node.t.localPosition+node.t.localRotation*Vector3.Scale(anchor,node.t.localScale);
                decoration.localRotation=camModel.Rotation;
                decoration.localScale=Vector3.one*size;
            }
            node.decorationRoot.gameObject.SetActive(true);
            if(node.crown!=null)Billboard(node.crown.transform,new Vector3(0,WorldMapTokenMesh.FaceHeight+.01f,.27f));
            Billboard(node.badge.transform,new Vector3(0,WorldMapTokenMesh.FaceHeight+.10f,.37f));
            for(var j=0;j<node.stars.Length;j++)if(node.stars[j]!=null)
            {
                Billboard(node.stars[j].transform,new Vector3((j-1)*.245f,.11f,j==1?-.65f:-.58f));
                node.stars[j].transform.localScale*=j==1?1.16f:.95f;
            }
            var groundRotation=MapCurve.Orientation(rel,radius,x,Route.Curvature);
            node.shadowT.SetParent(node.decorationRoot,false);
            node.shadowT.localRotation=groundRotation;
            node.shadowT.localPosition=MapCurve.Point(rel,x,.006f,radius,Route.Curvature)+groundRotation*new Vector3(.10f*size,0,-.06f*size);
            node.shadowT.localScale=new Vector3(.48f*size,1,.44f*size);
        }

        // ---- cenografia -----------------------------------------------------------------------------

        // Raios de sol editaveis (WorldMapLightSet): um WorldMapLightFx por luz, posicionado por fase + lateral.
        WorldMapLightSet lightSet;
        bool lightSetLoaded;
        readonly System.Collections.Generic.List<WorldMapLightFx> shaftFx = new System.Collections.Generic.List<WorldMapLightFx>();

        void UpdateShafts(float cursor)
        {
            if (!lightSetLoaded) { lightSet = Resources.Load<WorldMapLightSet>(WorldMapLightSet.ResourcePath); lightSetLoaded = true; }
            if (lightSet == null || matBeam == null || matSparkle == null) return;
            var radius = kit.radius;
            for (var i = 0; i < lightSet.shafts.Count; i++)
            {
                while (shaftFx.Count <= i) shaftFx.Add(new WorldMapLightFx(transform, layer, matBeam, matSparkle));
                var shaft = lightSet.shafts[i];
                if (shaft == null) { shaftFx[i].SetActive(false); continue; }
                var s = Route.MetersAt(shaft.level);
                var rel = s - cursor;
                if (rel < -6 || rel > Front - 8) { shaftFx[i].SetActive(false); continue; }
                var x = Route.CenterX(s) + WorldMapTerrain.Lateral(shaft.lateral, rel);
                var foot = WorldMapTerrain.SurfaceHeight(Route, s, shaft.lateral);
                var root = shaftFx[i].Root;
                root.localPosition = MapCurve.Point(rel, x, foot, radius, Route.Curvature);
                root.localRotation = MapCurve.Orientation(rel, radius, x, Route.Curvature);
                shaftFx[i].TickShaft(shaft, Now, 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(LiftNear, LiftFar, rel)));
            }
            for (var i = lightSet.shafts.Count; i < shaftFx.Count; i++) shaftFx[i].SetActive(false);
        }

        void UpdateProps(float cursor)
        {
            var lo = cursor - PropBack; // a camera fica 5 m atras do cursor; mais que isso e invisivel
            var hi = cursor + Front - 8;
            var index = LowerBound(lo);
            releaseBuffer.Clear();
            var seen = ScratchSeen;
            seen.Clear();
            for (; index < props.Count && props[index].s <= hi; index++)
            {
                var p = props[index];
                if (!activeSlots.TryGetValue(index, out var slot))
                {
                    slot = free.Count > 0 ? free.Pop() : slots.Count < MaxSlots ? CreateSlot() : null;
                    if (slot == null) break;
                    activeSlots[index] = slot;
                    slot.go.SetActive(true);
                    slot.shown = null;
                }
                seen.Add(index);
                Pose(slot, p, cursor, false);
            }
            foreach (var kv in activeSlots)
                if (!seen.Contains(kv.Key)) releaseBuffer.Add(kv.Key);
            foreach (var key in releaseBuffer)
            {
                var slot = activeSlots[key];
                activeSlots.Remove(key);
                slot.go.SetActive(false);
                free.Push(slot);
            }
        }

        readonly HashSet<int> seenScratch = new HashSet<int>();
        HashSet<int> ScratchSeen => seenScratch;

        int LowerBound(float s)
        {
            int lo = 0, hi = props.Count;
            while (lo < hi)
            {
                var mid = (lo + hi) >> 1;
                if (props[mid].s < s) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }

        Sprite PoseSprite(PropPlacement p)
        {
            var t = Now;
            if(p.workMotion==WorldMapWorkMotion.OwlBlink&&p.alt!=null)return Mathf.Repeat(t+p.seed*.37f,4.7f)<.16f?p.alt:p.sprite;
            switch (p.kind)
            {
                case PropKind.Geyser:
                    return kit.geyserBase??p.sprite;
                case PropKind.Mailbox:
                    return p.alt != null && (t + p.seed * 0.37f) % 5f < 0.7f ? p.alt : p.sprite;
                case PropKind.Dog:
                    var phase = (t + p.seed * 1.3f) % 7f;
                    if (phase >= 3.5f && phase < 4.3f && kit.dogWag != null) return kit.dogWag;
                    if (phase >= 5.3f && phase < 6.3f && kit.dogSniff != null) return kit.dogSniff;
                    return p.sprite;
                default:
                    return p.sprite;
            }
        }

        // Posiciona um cartao em pe no chao; ele "se ergue" conforme se aproxima (funcao da distancia, reversivel).
        void Pose(Slot slot, PropPlacement p, float cursor, bool forceStanding)
        {
            var radius = kit.radius;
            var rel = p.s - cursor;
            // The opening is composed as an illustrated landscape. An object whose contact
            // point has crossed behind the crest must not hover in the sky above another prop.
            bool opening=kit.simpleBase&&WorldMapMission100.PolishedEntrance&&cursor<Route.MetersAt(20);
            if(opening&&!forceStanding&&!camModel.SurfaceVisible(cursor,p.s)){slot.go.SetActive(false);return;}
            slot.go.SetActive(true);
            var floating = p.kind == PropKind.FloatingMail;
            var groundDetail=p.kind==PropKind.GroundDetail||p.kind==PropKind.WaterRipple||p.kind==PropKind.WaterRock;
            var visualEffect=p.kind==PropKind.WaterRipple||p.kind==PropKind.Steam||p.kind==PropKind.Meteor;
            var phase=Mathf.Repeat(Now*.23f+p.seed*.37f,1);
            var approvedPostman=p.kind==PropKind.Character&&(p.sprite==kit.postmanSprite&&kit.postmanSprite!=null||WorldMapMission100.Character(p.sprite)!=null);
            var walk=p.kind==PropKind.Character&&!approvedPostman?Mathf.Sin(Now*.85f+p.seed)*.075f:0;
            var x = Route.CenterX(p.s) + WorldMapTerrain.Lateral(p.offset, rel) + walk + (floating ? Mathf.Sin(Now * .32f + p.seed) * .08f : 0);
            var foot = floating||p.kind==PropKind.WaterRipple ? Route.WaterHeight(p.s)+.008f : Route.InGap(p.s)&&groundDetail?Route.WaterHeight(p.s):WorldMapTerrain.SurfaceHeight(Route,p.s,p.offset);
            if(p.kind==PropKind.Steam)foot+=phase*p.height*.65f;
            if(p.kind==PropKind.Meteor){foot+=(1-phase)*p.height*2.5f;x+=(1-phase)*p.height;}
            slot.t.localPosition = MapCurve.Point(rel, x, foot, radius,Route.Curvature);
            slot.t.localRotation = MapCurve.Orientation(rel, radius, x,Route.Curvature);
            var sprite = PoseSprite(p);
            bool animatedMailbox=WorldMapMailbox.Supports(sprite);
            slot.cardRenderer.enabled=!approvedPostman&&!groundDetail&&p.kind!=PropKind.LightShaft;
            if (slot.shown != sprite||!Mathf.Approximately(slot.shownHeight,p.height)||slot.shownKind!=p.kind||slot.shownBottomPivot!=p.bottomPivot)
            {
                slot.shown = sprite;
                slot.shownHeight=p.height;slot.shownKind=p.kind;slot.shownBottomPivot=p.bottomPivot;
                var height = p.height;
                if(p.kind==PropKind.Geyser)height*=.34f;
                if (p.kind == PropKind.Dog && p.sprite != null)
                    height *= WorldMapCards.VisibleBounds(sprite).height / Mathf.Max(0.001f, WorldMapCards.VisibleBounds(p.sprite).height);
                slot.cardMesh.sharedMesh = WorldMapCards.Get(sprite, height, p.bottomPivot);
                slot.cardRenderer.sharedMaterial = visualEffect?EffectMaterial(sprite.texture):CardMaterial(sprite.texture);
                slot.cardBlock.SetFloat("_GroundedFoot",floating?0:1);
                slot.cardRenderer.SetPropertyBlock(slot.cardBlock);
            }
            var raise = forceStanding ? 1 : 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(LiftNear, LiftFar, rel));
            bool workScene=p.workMotion!=WorldMapWorkMotion.None||p.id!=null&&p.id.StartsWith("work52-")||p.customAdornments&&p.adornments!=null&&System.Array.Exists(p.adornments,a=>a.motion!=WorldMapAdornmentMotion.None);
            var sway = !workScene&&(p.kind == PropKind.Tree || p.kind == PropKind.Small) ? Mathf.Sin(Now * 1.1f + p.seed) * 1.3f : 0;
            slot.card.localRotation = floating ? Quaternion.Euler(90, 0, Mathf.Sin(Now * .5f + p.seed) * 12) : Quaternion.Euler((1 - raise) * (kit.simpleBase?35:LieAngle), 0, sway);
            var breathe=kit.simpleBase&&!approvedPostman&&!workScene&&(p.kind==PropKind.Tree||p.kind==PropKind.Small||p.kind==PropKind.Landmark||p.kind==PropKind.Character)?Mathf.Sin(Now*1.7f+p.seed*.7f)*(p.kind==PropKind.Character?.027f:.014f):0;
            var mirror=WorldMapScenery.IsMirrored(p);
            slot.card.localScale=new Vector3((mirror?-1:1)*(1+breathe*.65f),1+breathe,1);
            // Sink only the artwork. The placement anchor and its contact shadow stay on the terrain.
            var pinned=p.groundPinned&&!floating&&!groundDetail&&!visualEffect;
            var sink=pinned?p.height*Mathf.Clamp(p.groundDepth,0,.30f):0;
            slot.card.localPosition = new Vector3(0, -p.height * .018f-sink, 0);
            if(p.workMotion==WorldMapWorkMotion.WateringCan){slot.card.localRotation*=Quaternion.Euler(0,0,-12+Mathf.Sin(Now*1.1f+p.seed)*5);slot.card.localPosition+=Vector3.up*p.height*(.22f+Mathf.Sin(Now*1.1f+p.seed)*.035f);}
            if(p.workMotion==WorldMapWorkMotion.FloatingIslet)slot.card.localPosition+=Vector3.up*p.height*(.30f+Mathf.Sin(Now*.9f+p.seed)*.045f);
            if(approvedPostman)
            {
                if(slot.postman==null||slot.postman.Source!=sprite){slot.postman?.Dispose();slot.postman=new WorldMapPostman(slot.card,layer,kit,CardMaterial,sprite);}
                slot.postman.Tick(p.height,Now,p.seed);
            }
            else slot.postman?.SetActive(false);
            if(!string.IsNullOrEmpty(p.caption))
            {
                if(slot.caption==null)
                {
                    var label=new GameObject("EnglishSignText",typeof(RectTransform)){layer=layer};label.transform.SetParent(slot.card,false);slot.caption=label.AddComponent<TextMeshPro>();
                    if(art!=null&&art.displayFont!=null)slot.caption.font=art.displayFont;
                    slot.caption.alignment=TextAlignmentOptions.Center;slot.caption.enableAutoSizing=true;slot.caption.fontSizeMin=.2f;slot.caption.fontSizeMax=2.2f;slot.caption.fontStyle=FontStyles.Bold;slot.caption.color=new Color32(69,27,96,255);slot.caption.textWrappingMode=TextWrappingModes.Normal;
                }
                slot.caption.gameObject.SetActive(true);slot.caption.text=p.caption;
                slot.caption.rectTransform.sizeDelta=new Vector2(p.height*.82f,p.height*.23f);
                slot.caption.transform.localPosition=WorldMapCards.ImagePoint(sprite,p.height,new Vector2(.5f,.60f))+Vector3.back*.045f;
                slot.caption.transform.localScale=new Vector3(mirror?-1:1,1,1);slot.caption.ForceMeshUpdate();
            }
            else if(slot.caption!=null)slot.caption.gameObject.SetActive(false);
            if(groundDetail)
            {
                if(slot.groundDetail==null)slot.groundDetail=new WorldMapGroundDetail(slot.card,layer,CardMaterial);
                var moving=p;if(p.kind==PropKind.WaterRipple)moving.offset+=(phase-.5f)*.9f;
                slot.groundDetail.Tick(sprite,moving,Route,transform,cursor,radius,p.kind==PropKind.WaterRipple||p.kind==PropKind.WaterRock,
                    visualEffect?EffectMaterial(sprite.texture):null,visualEffect?Mathf.Sin(phase*Mathf.PI)*.75f:1,mirror,
                    WorldMapScenery.HasIceTheme(p,christmas)&&kit.groundDetails!=null&&kit.groundDetails.Length>0&&sprite==kit.groundDetails[0],magicTime:Now);
                // Frozen crossings have a solid face, without animated water wakes above it.
                if(p.kind==PropKind.WaterRipple&&christmas)slot.groundDetail.SetActive(false);
            }
            else slot.groundDetail?.SetActive(false);
            var houseLight=p.kind==PropKind.Landmark&&WorldMapWindowLight.IsBuilding(sprite);
            var luminous=matGlow!=null&&(p.kind==PropKind.Lamp||p.kind==PropKind.Geyser||houseLight&&night>.01f);
            slot.lightRenderer.enabled=false;
            if(!luminous)slot.groundGlow?.SetActive(false);
            if(luminous)
            {
                slot.lightT.localPosition=new Vector3(0,.016f,0);slot.lightT.localScale=new Vector3(.52f,1,.36f)*(1+Mathf.Sin(Now*1.7f+p.seed)*.045f)*(1+night*.9f);
                slot.lightBlock.SetColor("_Color",p.kind==PropKind.Geyser?new Color(.1f,1,.78f,.3f):new Color(1,.68f,.17f,.24f*(1+night*2.2f)));
                if(houseLight){slot.lightT.localScale=new Vector3(1.4f,1,1.0f);slot.lightBlock.SetColor("_Color",new Color(1,.57f,.12f,night*.52f));}
                slot.lightRenderer.SetPropertyBlock(slot.lightBlock);
                if(slot.groundGlow==null)slot.groundGlow=new WorldMapGroundDetail(slot.t,layer,CardMaterial);
                var poolColor=houseLight?new Color(1,.64f,.25f,night*.40f):new Color(1,.68f,.22f,.10f+night*.48f);
                if(p.kind==PropKind.Lamp){var tint=WorldMapLampArt.Tint(sprite);poolColor=new Color(tint.r,tint.g,tint.b,poolColor.a);}
                slot.groundGlow.Glow(p,Route,transform,cursor,radius,matGlow,houseLight?new Vector2(p.height*.95f,p.height*.62f):new Vector2(.90f,.70f),poolColor);
            }
            // Luz dourada: feixe + brilhos nas caixas de correio; brilhos nas luminarias.
            if(p.kind==PropKind.LightShaft&&matBeam!=null&&matSparkle!=null)
            {
                // Raio de sol colocado no editor curvo: tamanho = height, rotacao e intensidade vem do objeto.
                if(slot.lightFx==null)slot.lightFx=new WorldMapLightFx(slot.card,layer,matBeam,matSparkle);
                var sh=slot.shaft??(slot.shaft=new WorldMapLightSet.Shaft());
                sh.enabled=true;sh.height=0;sh.angle=mirror?-p.rotation:p.rotation;sh.scale=1;sh.length=6f*p.height;sh.width=2.4f*p.height*(p.thickness>0?p.thickness:1);
                sh.intensity=p.intensity>0?p.intensity:.35f;sh.tint=WorldMapLightTint.Warm;sh.bands=3;sh.sparkles=3;sh.sparkleSize=.07f*Mathf.Max(.4f,p.height);sh.seed=p.seed==0?7:p.seed;
                slot.lightFx.TickShaft(sh,Now,raise*(1-night)); // raios de sol somem a noite
            }
            else if(matBeam!=null&&matSparkle!=null&&(p.kind==PropKind.Mailbox||p.kind==PropKind.Lamp))
            {
                if(slot.lightFx==null)slot.lightFx=new WorldMapLightFx(slot.card,layer,matBeam,matSparkle);
                slot.lightFx.Tick(p.kind,p.height,Now,p.seed,raise*raise);
            }
            else slot.lightFx?.SetActive(false);
            slot.cardBlock.SetFloat("_Emission",p.kind==PropKind.Lamp?(WorldMapLampArt.HasRegisteredHead(sprite)?0:.45f+night*.9f):p.kind==PropKind.Geyser?.25f:0);
            slot.cardBlock.SetFloat("_LampLight",p.kind==PropKind.Lamp?1:0);
            if(p.kind==PropKind.Lamp)
            {
                var rect=sprite.rect;var texture=sprite.texture;
                var glass=WorldMapLampArt.Glass(sprite);
                slot.cardBlock.SetVector("_LampGlass",glass);
                var lampTint=WorldMapLampArt.Tint(sprite);
                slot.cardBlock.SetColor("_LampTint",QualitySettings.activeColorSpace==ColorSpace.Linear?lampTint.linear:lampTint);
                slot.cardBlock.SetFloat("_LampCold",WorldMapLampArt.Cold(sprite)?1:0);
                slot.cardBlock.SetVector("_LampRect",new Vector4(rect.x/texture.width,rect.y/texture.height,rect.width/texture.width,rect.height/texture.height));
                if(slot.lampHalo==null)
                {
                    var halo=new GameObject("HaloDoPoste",typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};halo.transform.SetParent(slot.card,false);
                    halo.GetComponent<MeshFilter>().sharedMesh=WorldMapWindowLight.Quad;slot.lampHalo=halo.GetComponent<MeshRenderer>();slot.lampHalo.sharedMaterial=matGlow;
                }
                slot.lampHalo.gameObject.SetActive(true);slot.lampHalo.transform.localPosition=WorldMapCards.ImagePoint(sprite,p.height,new Vector2(glass.x,glass.y))+Vector3.back*.03f;
                slot.lampHalo.transform.localScale=new Vector3(.65f,.58f,1)*p.height;
                slot.lampHaloBlock.SetColor("_Color",new Color(lampTint.r,lampTint.g,lampTint.b,.16f+night*.64f));slot.lampHalo.SetPropertyBlock(slot.lampHaloBlock);
            }
            else if(slot.lampHalo!=null)slot.lampHalo.gameObject.SetActive(false);
            if(p.customAdornments&&p.adornments!=null&&p.adornments.Length>0||!p.customAdornments&&WorldMapAdornment.HasDefault(sprite))
            {
                if(slot.adornments==null)slot.adornments=new WorldMapAdornments(slot.card,layer,CardMaterial,matGlow);
                slot.adornments.Tick(sprite,p.height,p.customAdornments,p.adornments,p.effectPieces,kit,Now,night);
            }
            else slot.adornments?.SetActive(false);
              slot.cardBlock.SetColor("_Tint",visualEffect?new Color(1,1,1,Mathf.Sin(phase*Mathf.PI)*.8f):Color.white);
            slot.cardRenderer.SetPropertyBlock(slot.cardBlock);
            AnimateLayers(slot,p);
            if(animatedMailbox)
            {
                if(slot.mailbox==null)slot.mailbox=new WorldMapMailbox(slot.card,layer,CardMaterial);
                slot.mailbox.Tick(p.height,Now,p.seed);slot.cardRenderer.enabled=false;
            }
            else slot.mailbox?.SetActive(false);
            var postalTree=kit.simpleBase&&WorldMapScenery.IsPostalTree(sprite,kit)&&p.kind==PropKind.Tree;
            if(postalTree&&kit.postalTreeDetails.Length==4&&kit.postalTreeDetails[0]!=null&&kit.treeLeafShader!=null)
            {
                if(matTreeLeaves==null)
                {
                    matTreeLeaves=new Material(kit.treeLeafShader){hideFlags=HideFlags.HideAndDontSave};
                    matTreeLeaves.SetTexture("_MainTex",kit.postalTreeDetails[0].texture);hazeMaterials.Add(matTreeLeaves);
                }
                if(slot.treeDecor==null)slot.treeDecor=new WorldMapTreeDecor(slot.card,layer,CardMaterial,matTreeLeaves);
                slot.treeDecor.Tick(kit,p,sprite,Now,WorldMapScenery.HasIceTheme(p,christmas)||p.s<Route.Pos(1));
            }
            else slot.treeDecor?.SetActive(false);
            DressSnow(slot, p);
            slot.separatedSnow?.Apply(p.effectPieces,p.height);
            var shrub=WorldMapScenery.IsShrub(p,kit);
            var festive=WorldMapScenery.HasIceTheme(p,christmas)&&(p.kind==PropKind.Tree||shrub||p.lightsEffect.visibility==WorldMapEffectVisibility.Show||p.iceEffect.visibility==WorldMapEffectVisibility.Show);
            if(festive)
            {
                if(slot.christmasDecor==null)slot.christmasDecor=new WorldMapChristmasDecor(slot.card,layer,CardMaterial);
                slot.christmasDecor.Tick(kit,sprite,p.height,Now,p.lightsEffect,p.iceEffect,p.kind==PropKind.Tree,night,shrub,p.effectPieces);
            }
            else slot.christmasDecor?.SetActive(false);
            if(p.kind==PropKind.Landmark)
            {
                if(slot.windowLight==null)slot.windowLight=new WorldMapWindowLight(slot.card,layer,kit.windowGlow,kit,CardMaterial);
                slot.windowLight.Tick(sprite,p.height,p.lightsEffect.Visible(true)?night:0,slot.cardRenderer,slot.cardBlock);
                p.lightsEffect.Apply(slot.windowLight.Root,p.height,Vector2.zero,true);
            }
            else
            {
                slot.windowLight?.SetActive(false);slot.cardBlock.SetFloat("_WindowLight",0);slot.cardBlock.SetFloat("_WindowCount",0);
                slot.cardRenderer.SetPropertyBlock(slot.cardBlock);
            }
            var width = WorldMapCards.Width(sprite, p.height);
            var shadowAlpha = p.shadow && !floating && !groundDetail && !visualEffect ? Mathf.Lerp(pinned?.26f:.20f,pinned?.40f:.34f,raise) : 0;
            bool entranceContact=WorldMapMission100.PolishedEntrance&&p.s<Route.MetersAt(20)&&p.kind==PropKind.Character;
            if(entranceContact)shadowAlpha*=.82f;
            slot.shadowRenderer.enabled = shadowAlpha > 0.01f;
            if (slot.shadowRenderer.enabled)
            {
                slot.shadowT.localPosition = pinned?new Vector3(0,.008f,0):new Vector3(p.height*.025f, .012f, p.height*.015f);
                slot.shadowT.localScale = new Vector3(width * (pinned?.34f:.38f), 1, Mathf.Max(pinned?.05f:.10f,p.height*(pinned?.09f:.14f)));
                if(entranceContact)slot.shadowT.localScale=new Vector3(width*.56f,1,p.height*.10f);
                var shade=Route.LavaAt(p.s,kit.Lava)?new Color(.19f,.07f,.10f,shadowAlpha):new Color(.18f,.28f,.10f,shadowAlpha);
                slot.shadowBlock.SetColor("_Color",QualitySettings.activeColorSpace==ColorSpace.Linear?shade.linear:shade);
                slot.shadowRenderer.SetPropertyBlock(slot.shadowBlock);
            }
        }

        void DressSnow(Slot slot, PropPlacement p)
        {
            var snow=WorldMapScenery.HasIceTheme(p,christmas);
            p.snowEffect.Apply(slot.snowRoot,p.height,new Vector2(0,.55f),true);
            var key = p.sprite.GetEntityId().GetHashCode() ^ p.height.GetHashCode() ^ (snow ? 123456 : 0) ^ (kit.SeasonLook?.GetHashCode()??0) ^ ((int)p.theme*397) ^ ((int)p.kind*7919);
            if (slot.dressKey == key) return;
            slot.dressKey = key;
            slot.separatedSnow?.Clear();
            for (var i = 0; i < slot.caps.Length; i++){slot.caps[i].gameObject.SetActive(false);slot.capRenderers[i].SetPropertyBlock(null);}
            slot.cardBlock.SetFloat("_SnowDust",0);slot.cardRenderer.SetPropertyBlock(slot.cardBlock);
            if(p.theme==WorldMapObjectTheme.None)return;
            var custom=p.theme==WorldMapObjectTheme.Season&&kit.seasonPreview!=1?kit.SeasonLook?.overlays:null;
            if (!snow&&(custom==null||custom.Count==0)) return;
            void Cap(int i, Sprite snow, float height, float y)
            {
                if (snow == null) return;
                var cap = slot.caps[i];
                cap.gameObject.SetActive(true);
                cap.sharedMesh = WorldMapCards.Get(snow, height);
                slot.capRenderers[i].sharedMaterial = CardMaterial(snow.texture);
                cap.transform.localPosition = new Vector3(0, y-p.height*.55f, -.012f - i * .002f);
            }
            if (snow)
            {
            WorldMapSnowOverlay painted=null;
            if(kit.paintedSnow!=null)foreach(var layer in kit.paintedSnow)
                if(layer!=null&&layer.source==p.sprite&&layer.overlay!=null){painted=layer;break;}
            if(painted!=null)
            {
                if(slot.separatedSnow==null)slot.separatedSnow=new WorldMapSeparatedSnow(slot.snowRoot);
                slot.separatedSnow.Build(p.sprite,painted.overlay,p.height,CardMaterial(painted.overlay.texture),false);
            }
            else if(p.kind==PropKind.Tree||p.kind==PropKind.Small||p.kind==PropKind.Mailbox||p.kind==PropKind.Lamp||p.kind==PropKind.Landmark||p.snowEffect.visibility==WorldMapEffectVisibility.Show)
            {
                if(slot.separatedSnow==null)slot.separatedSnow=new WorldMapSeparatedSnow(slot.snowRoot);
                slot.separatedSnow.Build(p.sprite,null,p.height,CardMaterial(p.sprite.texture),p.kind==PropKind.Lamp);
            }
            }
            if(custom!=null)
            {
                var i=0;
                foreach(var overlay in custom)
                    if(overlay.category==p.kind&&overlay.sprite!=null&&i<slot.caps.Length)
                    {Cap(i,overlay.sprite,p.height*overlay.relativeHeight,p.height*overlay.position.y);slot.caps[i].transform.localPosition+=Vector3.right*p.height*overlay.position.x;i++;}
            }
        }

        void AnimateLayers(Slot slot,PropPlacement p)
        {
            for(var i=0;i<3;i++)slot.animation[i].gameObject.SetActive(false);
            if(p.kind!=PropKind.Geyser)return;
            var phase=Mathf.Repeat(Now+p.seed*.37f,5.5f);
            var jet=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.6f,3.4f,phase))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3.8f,4.9f,phase)));
            for(var i=0;i<3;i++)
            {
                var sprite=i==0?kit.geyserWater:kit.geyserSteam;if(sprite==null)continue;
                var mesh=slot.animation[i];mesh.gameObject.SetActive(true);
                mesh.sharedMesh=WorldMapCards.Get(sprite,p.height*(i==0?.85f:.33f));
                if(i>0&&matVapor==null&&kit.vapor!=null){matVapor=new Material(kit.vapor);matVapor.SetTexture("_MainTex",sprite.texture);}
                slot.animationRenderers[i].sharedMaterial=i>0&&matVapor!=null?matVapor:CardMaterial(sprite.texture);
                if(i==0)
                {
                    mesh.transform.localPosition=new Vector3(0,p.height*.19f,-.016f);
                    mesh.transform.localScale=new Vector3(1+.025f*Mathf.Sin(Now*9),.035f+jet*.96f,1);
                }
                else
                {
                    var rise=Mathf.Repeat(Now*.32f+p.seed*.11f+i*.5f,1);
                    mesh.transform.localPosition=new Vector3(Mathf.Sin(rise*4+i)*p.height*.10f,p.height*(.22f+rise*.9f),-.024f-i*.002f);
                    mesh.transform.localScale=Vector3.one*(.6f+rise*.6f);
                    slot.animationBlock.SetColor("_Tint",new Color(1,1,1,Mathf.Sin(rise*Mathf.PI)*.55f));slot.animationRenderers[i].SetPropertyBlock(slot.animationBlock);
                }
            }
        }

        // ---- cliques --------------------------------------------------------------------------------

        // Converte o ponteiro no RectTransform real do RawImage para um raio da camera do mapa.
        public int Pick(Vector2 screenPoint, Camera uiCamera)
        {
            return ScreenToViewport(screenPoint,uiCamera,out var viewport)?PickViewport(viewport):0;
        }
        public bool ScreenToViewport(Vector2 screenPoint,Camera uiCamera,out Vector2 viewport)
        {
            viewport=default;
            if(disposed||surface==null||cam==null||rt==null||!RectTransformUtility.ScreenPointToLocalPointInRectangle(surface,screenPoint,uiCamera,out var local))return false;
            var rect = surface.rect;
            viewport = new Vector2((local.x - rect.xMin) / rect.width, (local.y - rect.yMin) / rect.height);
            return true;
        }

        // Raio da camera do mapa para uma coordenada 0..1 do RenderTexture; retorna a fase do medalhao atingido (ou 0).
        public int PickViewport(Vector2 viewport)
        {
            if (disposed || cam == null || viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1) return 0;
            // Project and pick in the same local coordinate system. The map is isolated far from
            // the scene origin; a world-space inverse camera matrix loses precision at the horizon.
            var ray = camModel.ViewportRay(viewport, cam.aspect);
            var nearest = float.MaxValue;
            var picked = 0;
            foreach (var node in nodes)
            {
                if (!node.go.activeSelf || !camModel.SurfaceVisible(builtCursor, Route.Pos(node.level))) continue;
                var normal = node.t.localRotation * Vector3.up;
                var denominator = Vector3.Dot(normal, ray.direction);
                if (denominator >= -.000001f) continue;
                var center = node.t.localPosition + node.t.localRotation * new Vector3(0, WorldMapTokenMesh.FaceHeight * node.t.localScale.y, 0);
                var distance = Vector3.Dot(center - ray.origin, normal) / denominator;
                if (distance < 0 || distance >= nearest) continue;
                var local = Quaternion.Inverse(node.t.localRotation) * (ray.GetPoint(distance) - node.t.localPosition) / node.t.localScale.x;
                if (local.x * local.x + local.z * local.z > WorldMapTokenMesh.Radius * WorldMapTokenMesh.Radius) continue;
                nearest = distance;
                picked = node.level;
            }
            if (picked > 0) return picked;
            // The cream side of a distant disc can be visible below its top face.
            Physics.SyncTransforms();
            if (Physics.Raycast(cam.ViewportPointToRay(viewport), out var hit, 250, 1 << layer, QueryTriggerInteraction.Ignore))
            {
                var reference = hit.collider.GetComponentInParent<WorldMapNodeRef>();
                if (reference != null && camModel.SurfaceVisible(builtCursor, Route.Pos(reference.level))) return reference.level;
            }
            return 0;
        }

        // Para ferramentas de editor: projecao de um medalhao na tela do mapa (0..1).
        public bool ProjectLevel(int level, float cursorLevels, float aspect, out Vector2 screen)
        {
            var cursor = Route.MetersAt(cursorLevels);
            var s = Route.Pos(level);
            screen = default;
            if (!camModel.SurfaceVisible(cursor, s)) return false;
            foreach (var node in nodes)
            {
                if (!node.go.activeSelf || node.level != level) continue;
                var point = node.t.localPosition + node.t.localRotation * new Vector3(0, WorldMapTokenMesh.FaceHeight * node.t.localScale.y, 0);
                return camModel.ProjectPoint(point, aspect, out screen, out _);
            }
            return false;
        }

        // Diagnostico para ferramentas: renderers, shaders suportados e posicoes no mundo.
        public string Diagnose()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"camera {cam.transform.position} rot {cam.transform.eulerAngles} fov {cam.fieldOfView} aspect {cam.aspect} mask {cam.cullingMask} layer {layer} rt {(rt != null ? rt.width + "x" + rt.height : "null")}");
            sb.AppendLine($"illustrated={cam.orthographic} size={cam.orthographicSize} ground={matSurface.GetTexture("_PaintTex")?.name} bounds={matSurface.GetVectorArray("_GroundBounds").Length} kitBounds={kit.groundLaneBounds?.Length}");
            var all = GetComponentsInChildren<MeshRenderer>(false);
            sb.AppendLine($"renderers ativos: {all.Length}; nodes ativos: {System.Array.FindAll(nodes, n => n.go.activeSelf).Length}");
            foreach (var n in nodes)
                if (n.go.activeSelf) sb.Append($" {n.go.name}@{n.t.position.x:0.0},{n.t.position.z:0.0}");
            sb.AppendLine();
            foreach (var r in all)
                if (r.name == "Chao" || r.name == "Agua")
                {
                    var f = r.GetComponent<MeshFilter>();
                    sb.AppendLine($"{r.name} mesh={f.sharedMesh.vertexCount}v bounds={r.bounds.center}/{r.bounds.size}");
                }
            return sb.ToString();
        }

        void OnDestroy() => Dispose();

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            santa?.Dispose();santa=null;
            foreach(var slot in slots){slot.adornments?.Dispose();slot.mailbox?.Dispose();slot.postman?.Dispose();slot.groundDetail?.Dispose();slot.groundGlow?.Dispose();slot.separatedSnow?.Dispose();slot.christmasDecor?.Dispose();slot.windowLight?.Dispose();}
            if (cam != null) cam.targetTexture = null;
            if (image != null && image.texture == rt) image.texture = null;
            if (rt != null) { rt.Release(); Kill(rt); }
            foreach (var m in hazeMaterials) Kill(m);
            Kill(matShadow);
            if (shaftFx != null) foreach (var fx in shaftFx) fx?.Dispose();
            Kill(matGlow);Kill(matBeam);Kill(matSparkle);
            Kill(matVapor);
            Kill(matTreeLeaves);
            Kill(matNumbers);Kill(matNumbersActive);
            if(postProfile!=null){foreach(var component in postProfile.components)Kill(component);Kill(postProfile);}
            Kill(tokenMesh);
            Kill(skullMesh);Kill(matSkull);
            Kill(rewardStarMesh);Kill(matRewardStar);
            Kill(shadowMesh);
            if (terrain != null) { Kill(terrain.ground); Kill(terrain.water); }
            if(islandKits!=null)foreach(var style in islandKits)Kill(style);
            Kill(kit);
            if (gameObject != null) Kill(gameObject);
        }

        static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }
    }
}
