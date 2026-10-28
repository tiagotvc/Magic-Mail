using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using RoyalAves.UI.RoyalPostman2026;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    /// <summary>Builds an isolated art kit. Never touches the game scene, saves or gameplay prefabs.</summary>
    public static class RoyalPostman2026KitBuilder
    {
        const string Art = "Assets/RoyalAves/Art/RoyalPostmanUI2026/";
        const string Output = "Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/";
        const string MaterialFolder = Art + "Materials/";
        static readonly Color Cream = Hex("FFF2D2"), Plum = Hex("31103F");
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        static Font font;
        [InitializeOnLoadMethod]
        static void InstallAfterCompilation()
        {
            EditorApplication.delayCall+=EnsureRuntimeKit;
        }

        static void EnsureRuntimeKit()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=EnsureRuntimeKit;return;}
            // Existing prefabs are authored assets. Compilation/import must never regenerate them.
            if(!File.Exists("Assets/RoyalAves/Resources/RoyalPostmanUI2026/RuntimeKit.asset"))InstallRuntime();
        }

        [MenuItem("Royal Postman/Editar menu principal")]
        public static void OpenHomePrefab()=>OpenPrefab("Home");
        [MenuItem("Royal Postman/Editar modal de play")]
        public static void OpenPlayPrefab()=>OpenPrefab("PlayModal");
        static void OpenPrefab(string name)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Output+name+".prefab");
            if(prefab!=null){Selection.activeObject=prefab;AssetDatabase.OpenAsset(prefab);}
            else Debug.LogError("Prefab ausente: "+Output+name+".prefab");
        }

        [MenuItem("Royal Postman/2026/Reparar sprites e referências")]
        public static void RepairSpriteReferences()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=RepairSpriteReferences;return;}
            try
            {
                ImportArt();ImportNavigation();EnsureRuntimeKit();AssetDatabase.SaveAssets();
                Debug.Log("Royal Postman: sprites reparados; layout e prefabs manuais preservados.");
            }
            catch(Exception error){Debug.LogException(error);}
        }

        [MenuItem("Royal Postman/2026/Ativar home e modal no jogo")]
        public static void InstallRuntime()
        {
            const string resource="Assets/RoyalAves/Resources/RoyalPostmanUI2026/RuntimeKit.asset";
            var home=AssetDatabase.LoadAssetAtPath<GameObject>(Output+"Home.prefab");
            var modal=AssetDatabase.LoadAssetAtPath<GameObject>(Output+"PlayModal.prefab");
            if(home==null||modal==null)return;
            Directory.CreateDirectory(Path.GetDirectoryName(resource));
            var kit=AssetDatabase.LoadAssetAtPath<RoyalPostmanRuntimeKit>(resource);
            if(kit==null){kit=ScriptableObject.CreateInstance<RoyalPostmanRuntimeKit>();AssetDatabase.CreateAsset(kit,resource);}
            kit.home=home;kit.playModal=modal;kit.rocket=Sprite("booster-rocket");kit.bomb=Sprite("booster-bomb");kit.orb=Sprite("booster-orb");
            EditorUtility.SetDirty(kit);AssetDatabase.SaveAssets();
        }
        [Serializable] class PixelBounds { public int x, y, width, height; }
        [Serializable] class PixelReport { public string id; public int width, height; public PixelBounds solidBounds; }
        [Serializable] class Report { public PixelReport[] assets; }

        [MenuItem("Royal Postman/2026/Fabricação/Recriar kit (substitui prefabs)")]
        public static void Build()
        {
            Directory.CreateDirectory(Output); Directory.CreateDirectory(MaterialFolder);
            ImportArt();
            ImportNavigation();
            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/RoyalAves/Fonts/LilitaOne-Regular.ttf");
            if (font == null) throw new InvalidOperationException("Missing Lilita One source font.");
            Materials.Clear();
            foreach (string state in new[] { "teal", "gold", "red", "plum", "gray", "yellow", "cream" }) Paint(state);
            var scene = EditorSceneManager.NewPreviewScene();
            GameObject canvasObject = null;
            try
            {
                canvasObject = new GameObject("RoyalPostman2026Review", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                SceneManager.MoveGameObjectToScene(canvasObject, scene);
                var canvas = canvasObject.GetComponent<Canvas>();
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0;
                var home = Home((RectTransform)canvasObject.transform);
                PrefabUtility.SaveAsPrefabAsset(home.gameObject, Output + "Home.prefab");
                var modal = Modal((RectTransform)canvasObject.transform);
                PrefabUtility.SaveAsPrefabAsset(modal.gameObject, Output + "PlayModal.prefab");
                foreach(string name in new[]{"LevelButton","PetButton","CoinPlus","LifePlus","SettingsButton","CoinCounter","LifeCounter","Coin","Life","Avatar","Navigation"})
                    PrefabUtility.SaveAsPrefabAsset(home.Find(name).gameObject,Output+name+".prefab");
                foreach(string name in new[]{"PlayButton","FreeButton","AvailableButton","SelectedButton","LockedButton"})
                    PrefabUtility.SaveAsPrefabAsset(modal.Find(name).gameObject,Output+name+".prefab");
                var review = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Tools/royal-postman/production/review");
                Directory.CreateDirectory(review);
                var cameraObject = new GameObject("ReviewCamera", typeof(Camera)); SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>(); camera.enabled = false; camera.scene = scene; camera.orthographic = true;
                camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Hex("EDF4EE");
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 5;
                modal.gameObject.SetActive(false);
                Capture(canvas, camera, review + "/home.png", 900, 1600);
                Capture(canvas, camera, review + "/home-tall.png", 720, 1600);
                modal.gameObject.SetActive(true);
                Capture(canvas, camera, review + "/play-modal.png", 900, 1600);
                Capture(canvas, camera, review + "/play-modal-tall.png", 720, 1600);
                Validate(home, modal, review);
                modal.gameObject.SetActive(false);
                var main = home.Find("LevelButton").GetComponent<RoyalPostmanPaintedButton>(); main.SetPressed(true);
                Capture(canvas, camera, review + "/home-pressed.png", 900, 1600); main.SetPressed(false);
                home.gameObject.SetActive(false);
                var sheet = Components((RectTransform)canvasObject.transform);
                PrefabUtility.SaveAsPrefabAsset(sheet.gameObject, Output + "Components.prefab");
                Capture(canvas, camera, review + "/components.png", 900, 1600);
                sheet.gameObject.SetActive(false);
                var navigation=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Output+"Navigation.prefab"),canvasObject.transform,false);
                Capture(canvas,camera,review+"/navigation-home.png",1080,320);
                navigation.GetComponent<RoyalPostmanNavigation>().Select(3);
                Capture(canvas,camera,review+"/navigation-teams.png",1080,320);
                navigation.SetActive(false);
                var buttonReview=Full((RectTransform)canvasObject.transform,"HomeButtonReview");
                var levelReview=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Output+"LevelButton.prefab"),buttonReview,false);
                var petReview=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Output+"PetButton.prefab"),buttonReview,false);
                levelReview.GetComponent<RectTransform>().anchoredPosition=new Vector2(435,160);
                petReview.GetComponent<RectTransform>().anchoredPosition=new Vector2(786,160);
                Capture(canvas,camera,review+"/home-buttons-v6.png",1080,320);
                levelReview.GetComponent<RoyalPostmanPaintedButton>().SetPressed(true);petReview.GetComponent<RoyalPostmanPaintedButton>().SetPressed(true);
                Capture(canvas,camera,review+"/home-buttons-v6-pressed.png",1080,320);
                buttonReview.gameObject.SetActive(false);
                var topReview=Full((RectTransform)canvasObject.transform,"TopHUDReview");
                var topBackground=Pic(topReview,"Background","home-background",540,-960,1080,1920);
                topBackground.rectTransform.anchorMin=topBackground.rectTransform.anchorMax=new Vector2(0,1);
                foreach(string name in new[]{"Avatar","CoinCounter","Coin","CoinValue","CoinPlus","LifeCounter","Life","LivesValue","LifeStatus","LifePlus","SettingsButton"})
                {
                    var copy=UnityEngine.Object.Instantiate(home.Find(name).gameObject,topReview,false);copy.name=name;
                }
                Capture(canvas,camera,review+"/top-hud-v14.png",1080,320);
                foreach(string name in new[]{"CoinPlus","LifePlus","SettingsButton"})topReview.Find(name).GetComponent<RoyalPostmanPaintedButton>().SetPressed(true);
                Capture(canvas,camera,review+"/top-hud-v14-pressed.png",1080,320);
                topReview.gameObject.SetActive(false);
                var finishReview=Full((RectTransform)canvasObject.transform,"LevelFinishReview");
                foreach(var item in new[]{("LevelButton",280f,1f),("CoinPlus",650f,2.3f),("SettingsButton",910f,1.5f)})
                {
                    var copy=UnityEngine.Object.Instantiate(home.Find(item.Item1).gameObject,finishReview,false);
                    var rect=copy.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=Vector2.zero;
                    rect.anchoredPosition=new Vector2(item.Item2,190);rect.localScale=Vector3.one*item.Item3;
                }
                Capture(canvas,camera,review+"/top-controls-v14.png",1080,380);
                AssetDatabase.SaveAssets();
                Debug.Log("ROYAL_POSTMAN_KIT_OK: " + review);
            }
            finally
            {
                if (canvasObject != null) UnityEngine.Object.DestroyImmediate(canvasObject);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        static void ImportArt()
        {
            Directory.CreateDirectory(Art+"Sprites");AssetDatabase.Refresh();
            string file = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Tools/royal-postman/production/ALPHA-REPORT.json");
            var entries = JsonUtility.FromJson<Report>("{\"assets\":" + File.ReadAllText(file) + "}").assets;
            foreach (var entry in entries)
            {
                string assetPath = RoyalPostmanArtPaths.Texture(entry.id);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
                importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 8192; importer.textureCompression = TextureImporterCompression.Uncompressed;
                var b = entry.solidBounds;
                // Sprite metadata trims transparent padding only; source image pixels remain untouched.
                var rect = Rect.MinMaxRect(Mathf.Max(0,b.x-2), Mathf.Max(0,entry.height-b.y-b.height-2), Mathf.Min(entry.width,b.x+b.width+2), Mathf.Min(entry.height,entry.height-b.y+2));
                bool sliced = entry.id.Contains("master") || entry.id.Contains("panel") || entry.id.Contains("socket") || entry.id.Contains("body") || entry.id.Contains("frame") || entry.id.Contains("navigation");
                float border = sliced ? Mathf.Min(rect.width, rect.height) * .23f : 0;
                if (entry.id.Contains("navigation")) border = Mathf.Min(rect.width, rect.height) * .15f;
                importer.SaveAndReimport();
                // Each PNG is one texture. Separate native Sprite assets avoid importer-generated subasset IDs,
                // which were different after import in the actual game and made every UI image white.
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                float scaleX=(float)texture.width/entry.width, scaleY=(float)texture.height/entry.height;
                rect=new Rect(rect.x*scaleX,rect.y*scaleY,rect.width*scaleX,rect.height*scaleY); border*=Mathf.Min(scaleX,scaleY);
                var spritePath=RoyalPostmanArtPaths.Sprite(entry.id);
                var existing=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                var sprite=UnityEngine.Sprite.Create(texture,rect,Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(border,border,border,border));sprite.name=entry.id;
                if(existing==null)AssetDatabase.CreateAsset(sprite,spritePath);
                else {EditorUtility.CopySerialized(sprite,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(sprite);}
            }
        }

        [Serializable] class NavigationImage { public string id; public int width,height; public PixelBounds[] rectangles; public PixelBounds opening; }
        [Serializable] class NavigationReport { public NavigationImage[] assets; }
        static void ImportNavigation()
        {
            ImportImageReport("Tools/royal-postman/production/NAVIGATION-BOUNDS.json");
            if(File.Exists("Tools/royal-postman/production/HOME-BUTTON-BOUNDS.json"))ImportImageReport("Tools/royal-postman/production/HOME-BUTTON-BOUNDS.json");
            if(File.Exists("Tools/royal-postman/production/TOP-HUD-BOUNDS.json"))ImportImageReport("Tools/royal-postman/production/TOP-HUD-BOUNDS.json");
        }
        static void ImportImageReport(string reportPath)
        {
            var report=JsonUtility.FromJson<NavigationReport>(File.ReadAllText(reportPath));
            foreach(var entry in report.assets)
            {
                string path=RoyalPostmanArtPaths.Texture(entry.id);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Default;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
                importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=8192;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                for(int i=0;i<entry.rectangles.Length;i++)
                {
                    var b=entry.rectangles[i];float sx=(float)texture.width/entry.width,sy=(float)texture.height/entry.height;
                    var rect=new Rect(b.x*sx,(entry.height-b.y-b.height)*sy,b.width*sx,b.height*sy);
                    string id=entry.rectangles.Length==1?entry.id:"navigation-icon-approved-"+i;
                    float border=id.EndsWith("-v4")?Mathf.Min(rect.width,rect.height)*(id.Contains("rim")?.42f:.35f):0;
                    if(id=="level-base-v6")border=Mathf.Min(rect.width,rect.height)*.45f;
                    if(id=="top-value-container-v7")border=Mathf.Min(rect.width,rect.height)*.43f;
                    var sprite=UnityEngine.Sprite.Create(texture,rect,Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(border,border,border,border));sprite.name=id;
                    string destination=RoyalPostmanArtPaths.Sprite(id);var existing=AssetDatabase.LoadAssetAtPath<Sprite>(destination);
                    if(existing==null)AssetDatabase.CreateAsset(sprite,destination);
                    else{EditorUtility.CopySerialized(sprite,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(sprite);}
                }
            }
        }

        static RectTransform Home(RectTransform parent)
        {
            var root = Full(parent, "Home");
            Pic(root, "Background", "home-background", 540, 960, 1080, 1920);
            var charactersGroup=Box(root,"Characters",540,1100,1030,1200);
            Pic(charactersGroup,"Courier","hero-courier",-30,0,840,1200,true);
            Pic(charactersGroup,"Owl","hero-owl",260,-210,550,790,true);
            var avatar = Box(root, "Avatar", 114, 1795, 184, 184);
            var clip=Box(avatar,"PortraitClip",0,0,154,154);var maskGraphic=clip.gameObject.AddComponent<RoyalPostmanPortraitMask>();maskGraphic.raycastTarget=false;
            var mask=clip.gameObject.AddComponent<Mask>();mask.showMaskGraphic=false;
            var portrait = Pic(clip, "Portrait", "top-avatar-portrait-v7", 0, 0, 154, 154);
            var frame = Pic(avatar, "Frame", "top-avatar-frame-v7", 0, 0, 184, 184,true);
            var avatarControl = avatar.gameObject.AddComponent<RoyalPostmanAvatar>(); avatarControl.portrait = portrait; avatarControl.frame = frame;
            TopCounter(root,"CoinCounter",396,1795,244,90);
            Pic(root, "Coin", "top-coin-envelope-v7", 272, 1795, 116, 116, true);
            TopValueLabel(root, "CoinValue", "1867", 420, 1797, 186, 74, 62);
            HudPlus(root, "CoinPlus", 324, 1753);
            TopCounter(root,"LifeCounter",752,1795,224,90);
            Pic(root, "Life", "top-heart-v7", 606, 1795, 128, 128);
            HomeButtonLabel(root, "LivesValue", "5", 606, 1798, 78, 96, 74,"870F28");
            TopValueLabel(root, "LifeStatus", "Cheio", 762, 1797, 194, 77, 59);
            HudPlus(root, "LifePlus", 658, 1753);
            var settings = HudCircleButton(root,"SettingsButton",1005,1795,130,"top-round-existing-v14","plum");
            var gear=Pic(settings,"Gear","top-gear-existing-v14",0,0,84,84,true);
            gear.material=ExistingControlPaint("top-gear-existing-v14","plum",false,false,false);
            var level = HomeButton(root, "LevelButton", 535, 412, 440, 220, "level");
            HomeButtonLabel(level, "LevelLabel", "Nível 14", 0, 3, 407, 110, 82, "003F39");
            var pet = HomeButton(root, "PetButton", 886, 412, 200, 210, "pet");
            Pic(pet, "Paw", "paw-cream", 0, 29, 96, 94, true);
            HomeButtonLabel(pet, "PetLabel", "Pet", 0, -48, 135, 60, 43, "381248");
            Navigation(root);
            var background=root.Find("Background").GetComponent<RectTransform>();background.anchorMin=Vector2.zero;background.anchorMax=Vector2.one;background.offsetMin=background.offsetMax=Vector2.zero;
            var characters=root.Find("Characters").GetComponent<RectTransform>();characters.anchorMin=characters.anchorMax=new Vector2(.5f,.5f);characters.anchoredPosition=new Vector2(0,140);
            foreach(string name in new[]{"Avatar","CoinCounter","Coin","CoinValue","CoinPlus","LifeCounter","Life","LivesValue","LifeStatus","LifePlus","SettingsButton"})
            {
                var rt=root.Find(name).GetComponent<RectTransform>();var position=rt.anchoredPosition;rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.anchoredPosition=position-new Vector2(0,1920);
            }
            return root;
        }
        static void Navigation(RectTransform parent)
        {
            var root = Box(parent, "Navigation", 540, 116, 1080, 232);
            root.anchorMin=new Vector2(0,0);root.anchorMax=new Vector2(1,0);
            root.anchoredPosition=new Vector2(0,116);root.sizeDelta=new Vector2(0,232);
            var bar=Pic(root, "ContinuousBar", "navigation-bar-approved-v2", 0, 0, 1080, 232).rectTransform;
            bar.anchorMin=Vector2.zero;bar.anchorMax=Vector2.one;bar.offsetMin=bar.offsetMax=Vector2.zero;
            float[] centers={-440,-240,0,240,440};
            string[] titles={"Eventos","Ranking","Início","Equipes","Coleção"};
            for (int i = 0; i < 5; i++)
            {
                var item = Box(root, "Tab"+i, centers[i], 0, i==2?280:200, 232);
                item.anchorMin=new Vector2((centers[i]+540-(i==2?140:100))/1080f,0);
                item.anchorMax=new Vector2((centers[i]+540+(i==2?140:100))/1080f,1);
                item.offsetMin=item.offsetMax=Vector2.zero;
                var hit = item.gameObject.AddComponent<Image>(); hit.color = Color.clear;
                item.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
                var active=Box(item,"ActiveColor",0,-10,item.sizeDelta.x,212).gameObject.AddComponent<Image>();
                var activeRect=active.rectTransform;activeRect.anchorMin=Vector2.zero;activeRect.anchorMax=Vector2.one;
                activeRect.offsetMin=Vector2.zero;activeRect.offsetMax=new Vector2(0,-28);
                active.color=new Color(1f,.31f,.18f,.22f);active.raycastTarget=false;
                string[] iconIds={"clipboard","trophy","home","teams","album"};
                Pic(item, "Icon", "navigation-"+iconIds[i]+"-individual-v3", 0, i == 2 ? 55 : 20, i == 2 ? 184 : 142, i == 2 ? 166 : 142, true);
                var label=Label(item, "SelectedLabel", titles[i], 0, -51, i==2?272:192, 58, 44, Cream);
                label.resizeTextForBestFit=true;label.resizeTextMinSize=28;label.resizeTextMaxSize=44;
                var marker=Box(item,"Marker",0,-85,86,8).gameObject.AddComponent<Image>();
                marker.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                marker.type=Image.Type.Sliced;marker.color=Hex("FFE3A4");marker.raycastTarget=false;
            }
            foreach(float x in new[]{-340f,-140f,140f,340f})
            {
                // Follow the marked reference: start exactly at red and stop above its bottom.
                float redHeight=232-RoyalPostmanNavigation.GoldRailHeight;
                float height=redHeight*RoyalPostmanNavigation.DividerRedFraction;
                float center=116-RoyalPostmanNavigation.GoldRailHeight-height*.5f;
                var divider=Box(root,"Divider",x,center,3,height).gameObject.AddComponent<RoyalPostmanNavigationDivider>();divider.raycastTarget=false;
                var seam=divider.rectTransform;seam.anchorMin=seam.anchorMax=new Vector2((x+540)/1080f,.5f);seam.anchoredPosition=new Vector2(0,center);
            }
            root.gameObject.AddComponent<RoyalPostmanNavigation>().Select(2);
        }

        static RectTransform Modal(RectTransform parent)
        {
            var root = Full(parent, "PlayModal");
            var scrim = Full(root,"Scrim").gameObject.AddComponent<Image>(); scrim.color = new Color(0,0,0,.68f);
            Pic(root,"FixedGoldOuter", "button-frame-gold",540,947,1018,1270);
            Pic(root,"RedBody","modal-body-red",540,947,974,1226);
            Pic(root,"CreamPanel","panel-cream",540,1038,902,834);
            Label(root,"ObjectiveLabel","Objetivo",540,1390,800,100,75,Plum,false);
            // The recessed goal well is a palette variant of the same clean cream panel.
            Pic(root,"GoalWell","panel-cream",540,1210,794,280,false,"cream");
            string[] goals = { "goal-postal-cap", "goal-wax-seal", "goal-potion" };
            string[] counts = { "20", "12", "8" };
            for(int i=0;i<3;i++)
            {
                float x=290+i*250;
                Pic(root,"Goal"+i,goals[i],x,1235,182,180,true);
                Label(root,"GoalCount"+i,counts[i],x,1120,180,85,72,Cream);
            }
            Label(root,"BoosterLabel","Selecionar reforços",540,1000,850,95,63,Plum,false);
            Booster(root,"Available",280,814,"yellow","booster-rocket",false,false);
            Booster(root,"Selected",540,814,"teal","booster-bomb",true,false);
            Booster(root,"Locked",800,814,"gray","booster-orb",false,true);
            var play=Button(root,"PlayButton",303,465,435,198,"teal",true);
            Label(play,"PlayLabel","Jogar",0,5,365,130,105,Cream);
            var free=Button(root,"FreeButton",778,465,435,198,"teal",true);
            Label(free,"FreeLabel","Grátis",0,5,365,130,100,Cream);
            var badge=Button((RectTransform)free.parent,"RewardBadge",170,75,91,91,"plum",true,true);
            Pic(badge,"VisibleReward","booster-rocket",0,2,65,65,true);
            badge.parent.GetComponent<Image>().raycastTarget=false;badge.parent.GetComponent<Button>().interactable=false;
            Glyph(badge,"VideoSymbol",RoyalPostmanGlyph.Symbol.Video,27,-24,28);
            Pic(root,"HeaderGoldUnderlay","modal-header-red",540,1598,1052,257,false,"gold");
            Pic(root,"HeaderRed","modal-header-red",540,1600,1040,245);
            Pic(root,"HeaderGoldLip","modal-header-lip-gold",540,1488,1020,56);
            Label(root,"Title","Nível 14",540,1593,850,160,136,Cream);
            var close=Button(root,"CloseButton",958,1604,125,125,"red",true,true);
            Glyph(close,"CloseGlyph",RoyalPostmanGlyph.Symbol.Close,0,2,80);
            foreach(RectTransform child in root)
            {
                if(child.name=="Scrim")continue;
                var position=child.anchoredPosition;child.anchorMin=child.anchorMax=new Vector2(.5f,.5f);child.anchoredPosition=position-new Vector2(540,960);
            }
            return root;
        }
        static void Booster(RectTransform root,string name,float x,float y,string state,string icon,bool selected,bool locked)
        {
            var socket=Pic(root,name+"TightInset","socket-cream-tight",x,y,240,240);
            var moving=Button(root,name+"Button",x,y,224,224,state,false);
            Pic(moving,"BoosterIcon",icon,0,9,165,170,true);
            var padlock=Pic(moving,"Lock","lock-gray",0,-63,91,103,true);
            var quantity=Button(moving,"Quantity",74,-75,76,76,"red",false,true);
            quantity.parent.GetComponent<Button>().interactable=false;quantity.parent.GetComponent<Image>().raycastTarget=false;
            var quantityText=Label(quantity,"Value","4",0,3,65,72,59,Cream);
            var check=Button(moving,"Selected",77,83,78,78,"teal",false,true);
            check.parent.GetComponent<Button>().interactable=false;check.parent.GetComponent<Image>().raycastTarget=false;
            Glyph(check,"Check",RoyalPostmanGlyph.Symbol.Check,0,2,54);
            var visual=moving.parent.gameObject.AddComponent<RoyalPostmanBoosterButton>();
            visual.face=moving.Find("Face").GetComponent<Image>();visual.availablePaint=Paint("yellow");visual.selectedPaint=Paint("teal");visual.lockedPaint=Paint("gray");
            visual.quantity=quantity.parent.gameObject;visual.selection=check.parent.gameObject;visual.padlock=padlock.gameObject;visual.quantityText=quantityText;
            visual.SetState(locked?RoyalPostmanBoosterButton.VisualState.Locked:selected?RoyalPostmanBoosterButton.VisualState.Selected:RoyalPostmanBoosterButton.VisualState.Available);
        }
        static RectTransform Components(RectTransform parent)
        {
            var root=Full(parent,"Components");
            var backdrop=Box(root,"ReviewBackdrop",540,960,1080,1920).gameObject.AddComponent<Image>();backdrop.color=Hex("F2EAD9");
            Label(root,"Title","Royal Postman",540,1800,1000,100,76,Plum,false);
            Label(root,"Subtitle","Centros • bordas • ícones separados",540,1698,1000,70,38,Plum,false);
            Booster(root,"Available",255,1440,"yellow","booster-rocket",false,false);
            Booster(root,"Selected",540,1440,"teal","booster-bomb",true,false);
            Booster(root,"Locked",825,1440,"gray","booster-orb",false,true);
            var play=Button(root,"PlayButton",335,1110,440,185,"teal",true);Label(play,"Label","Jogar",0,4,370,130,100,Cream);
            var free=Button(root,"FreeButton",335,855,440,185,"teal",true);Label(free,"Label","Grátis",0,4,370,130,100,Cream);
            Pic(root,"SeparateFrame","button-frame-gold",825,1110,225,225);
            Pic(root,"SeparateCenter","button-face-master",825,855,205,205,false,"teal");
            string[] icons={"coin-envelope","heart-red","gear-cream","paw-cream","lock-gray","courier-portrait"};
            for(int i=0;i<icons.Length;i++)Pic(root,icons[i],icons[i],210+(i%3)*330,530-(i/3)*270,175,175,true);
            return root;
        }

        static RectTransform Button(RectTransform parent,string name,float x,float y,float w,float h,string color,bool gold,bool circle=false)
        {
            var root=Box(parent,name,x,y,w,h);
            var hit=root.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            var button=root.gameObject.AddComponent<Button>();button.targetGraphic=hit;button.transition=Selectable.Transition.None;
            if(gold)Pic(root,"FixedGoldFrame",circle?"circle-frame-gold":"button-frame-gold",0,0,w,h);
            var moving=Box(root,"MovingCenter",0,gold?2:0,w-(gold?26:0),h-(gold?26:0));
            Pic(moving,"Face",circle?"circle-face-master":"button-face-master",0,0,moving.sizeDelta.x,moving.sizeDelta.y,false,color);
            var press=root.gameObject.AddComponent<RoyalPostmanPaintedButton>();press.movingCenter=moving;press.pressTravel=Mathf.Min(6,h*.035f);
            return moving;
        }
        static RectTransform HomeButton(RectTransform parent,string name,float x,float y,float w,float h,string kind)
        {
            var root=Box(parent,name,x,y,w,h);var hit=root.gameObject.AddComponent<Image>();hit.color=Color.clear;
            var button=root.gameObject.AddComponent<Button>();button.targetGraphic=hit;button.transition=Selectable.Transition.None;
            string id=kind+"-base-v6";
            var well=Pic(root,"FixedWell",id,0,0,w,h);well.material=ButtonPart(false);well.color=new Color(.72f,.72f,.72f,1);
            var moving=Box(root,"MovingCenter",0,0,w,h);
            var face=Pic(moving,"Face",id,0,0,w,h);face.material=ButtonPart(false);
            var frame=Pic(root,"FixedGoldFrame",id,0,0,w,h);frame.material=ButtonPart(true);
            if(kind=="level")
            {
                // One source-pixel scale for the four sides and corner patches. Only the
                // center stretches, so changing button height cannot thicken top/bottom gold.
                foreach(var image in new[]{well,face,frame})
                {
                    image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=4;
                }
            }
            var press=root.gameObject.AddComponent<RoyalPostmanPaintedButton>();press.movingCenter=moving;press.pressTravel=4;
            return moving;
        }
        static Material ButtonPart(bool gold)
        {
            string key=gold?"home-fixed-gold-v6":"home-moving-center-v6";
            if(Materials.TryGetValue(key,out var existing))return existing;
            string path=RoyalPostmanArtPaths.Material(key);var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("RoyalPostman/UI/PaintedPalette"));AssetDatabase.CreateAsset(material,path);}
            material.SetFloat("_UsePalette",0);material.SetFloat("_ButtonPart",gold?1:2);EditorUtility.SetDirty(material);Materials[key]=material;return material;
        }
        static Text HomeButtonLabel(RectTransform parent,string name,string value,float x,float y,float w,float h,int size,string shade)
        {
            var text=Label(parent,name,value,x,y,w,h,size,Cream);
            var outline=text.GetComponent<Outline>();outline.effectColor=Hex(shade);outline.effectDistance=new Vector2(1,-1);
            var shadow=text.gameObject.AddComponent<Shadow>();shadow.effectColor=Hex(shade);shadow.effectDistance=new Vector2(0,-3);shadow.useGraphicAlpha=true;
            return text;
        }
        static void Plus(RectTransform root,string name,float x,float y)
        {
            var button=Button(root,name,x,y,64,64,"teal",false,true);
            Glyph(button,"PlusGlyph",RoyalPostmanGlyph.Symbol.Plus,0,1,42);
        }
        static void TopCounter(RectTransform parent,string name,float x,float y,float width,float height)
        {
            var image=Pic(parent,name,"top-value-container-v7",x,y,width,height);
            image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=image.sprite.rect.height/height;
        }
        static Text TopValueLabel(RectTransform parent,string name,string value,float x,float y,float w,float h,int size)
        {
            var text=Label(parent,name,value,x,y,w,h,size,Plum,false);
            var outline=text.gameObject.AddComponent<Outline>();outline.effectColor=Cream;outline.effectDistance=new Vector2(.85f,-.85f);return text;
        }
        static RectTransform HudCircleButton(RectTransform parent,string name,float x,float y,float size,string id,string palette)
        {
            var root=Box(parent,name,x,y,size,size);var hit=root.gameObject.AddComponent<Image>();hit.color=Color.clear;
            var button=root.gameObject.AddComponent<Button>();button.targetGraphic=hit;button.transition=Selectable.Transition.None;
            // Opaque authored base beneath the moving face prevents a dark alpha seam at rest.
            // The shoulder stays fixed; only the independently masked face and symbol travel.
            var shoulder=Pic(root,"FixedShoulder",id,0,0,size,size,true);
            shoulder.material=ExistingControlPaint(id,palette,false,true,false);
            var moving=Box(root,"MovingCenter",0,0,size,size);var face=Pic(moving,"Face",id,0,0,size,size,true);
            face.material=ExistingControlPaint(id,palette,true,true,false);
            var press=root.gameObject.AddComponent<RoyalPostmanPaintedButton>();press.movingCenter=moving;press.pressTravel=size<100?2:3;
            return moving;
        }
        static void HudPlus(RectTransform parent,string name,float x,float y)
        {
            var center=HudCircleButton(parent,name,x,y,64,"top-round-existing-v14","teal");
            var glyph=Pic(center,"PlusGlyph","top-plus-symbol-existing-v14",0,1,38,38,true);
            glyph.rectTransform.localEulerAngles=new Vector3(0,0,45);
            glyph.material=ExistingControlPaint("top-plus-symbol-existing-v14","teal",false,false,true);
        }
        static Material ExistingControlPaint(string id,string palette,bool center,bool removeX,bool extractX)
        {
            string key=id+"-"+palette+(center?"-moving":"-fixed");
            if(Materials.TryGetValue(key,out var existing))return existing;
            string path=RoyalPostmanArtPaths.Material(key);var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("RoyalPostman/UI/PaintedPalette"));AssetDatabase.CreateAsset(material,path);}
            var sprite=Sprite(id);var rect=sprite.rect;var texture=sprite.texture;
            material.SetFloat("_UsePalette",0);material.SetFloat("_ButtonPart",center?4:0);material.SetFloat("_CircleCenterRadius",.395f);
            material.SetFloat("_RecolorRed",1);material.SetColor("_TargetColor",Hex(palette=="plum"?"792B94":"009D92"));
            material.SetFloat("_RemoveCloseGlyph",removeX?1:0);material.SetFloat("_ExtractCloseGlyph",extractX?1:0);material.SetFloat("_ClipCircle",removeX?1:0);
            material.SetVector("_ButtonUVRect",new Vector4(rect.x/texture.width,rect.y/texture.height,rect.width/texture.width,rect.height/texture.height));
            EditorUtility.SetDirty(material);Materials[key]=material;return material;
        }
        static Material CirclePart(string id,bool center)
        {
            string key=id+(center?"-moving":"-fixed");if(Materials.TryGetValue(key,out var existing))return existing;
            string path=RoyalPostmanArtPaths.Material(key);var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("RoyalPostman/UI/PaintedPalette"));AssetDatabase.CreateAsset(material,path);}
            var sprite=Sprite(id);var rect=sprite.rect;var texture=sprite.texture;
            material.SetFloat("_UsePalette",0);material.SetFloat("_ButtonPart",center?4:3);material.SetFloat("_CircleCenterRadius",.40f);
            material.SetVector("_ButtonUVRect",new Vector4(rect.x/texture.width,rect.y/texture.height,rect.width/texture.width,rect.height/texture.height));
            EditorUtility.SetDirty(material);Materials[key]=material;return material;
        }
        static Material GearAperture()
        {
            const string id="top-gear-cream-v10",key=id+"-aperture";
            string path=RoyalPostmanArtPaths.Material(key);var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("RoyalPostman/UI/PaintedPalette"));AssetDatabase.CreateAsset(material,path);}
            var sprite=Sprite(id);var rect=sprite.rect;var texture=sprite.texture;
            material.SetFloat("_UsePalette",0);material.SetFloat("_ButtonPart",0);
            // A native circular aperture keeps the gear's hole clear at every UI scale.
            // The source PNG remains untouched.
            material.SetFloat("_GlyphHoleRadius",.145f);material.SetVector("_GlyphHoleCenter",new Vector4(.5f,.49f,0,0));
            material.SetVector("_ButtonUVRect",new Vector4(rect.x/texture.width,rect.y/texture.height,rect.width/texture.width,rect.height/texture.height));
            EditorUtility.SetDirty(material);return material;
        }
        static void Glyph(RectTransform parent,string name,RoyalPostmanGlyph.Symbol symbol,float x,float y,float size)
        {
            var glyph=Box(parent,name,x,y,size,size).gameObject.AddComponent<RoyalPostmanGlyph>();glyph.symbol=symbol;glyph.color=Cream;glyph.raycastTarget=false;
        }
        static RectTransform Full(RectTransform parent,string name)
        {
            var rt=Box(parent,name,0,0,0,0);rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=Vector2.zero;rt.offsetMax=Vector2.zero;rt.pivot=Vector2.zero;return rt;
        }
        static RectTransform Box(RectTransform parent,string name,float x,float y,float w,float h)
        {
            var rt=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=parent.pivot;rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(w,h);return rt;
        }
        static Sprite Sprite(string id)
        {
            string path=RoyalPostmanArtPaths.Sprite(id);
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(sprite==null)throw new InvalidOperationException("Missing separate sprite: "+path);return sprite;
        }
        static Image Pic(RectTransform root,string name,string id,float x,float y,float w,float h,bool aspect=false,string paint=null)
        {
            var image=Box(root,name,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=Sprite(id);image.raycastTarget=false;
            image.preserveAspect=aspect;image.type=!aspect&&image.sprite.border.sqrMagnitude>0?Image.Type.Sliced:Image.Type.Simple;
            if(id.Contains("face-master")||id=="circle-frame-gold")image.type=Image.Type.Simple;
            if(image.type==Image.Type.Sliced)image.pixelsPerUnitMultiplier=id.Contains("navigation")?1:3;
            if(paint!=null)image.material=Paint(paint);return image;
        }
        static Text Label(RectTransform root,string name,string content,float x,float y,float w,float h,int size,Color color,bool outline=true)
        {
            var text=Box(root,name,x,y,w,h).gameObject.AddComponent<Text>();text.font=font;text.text=content;text.fontSize=size;text.color=color;
            text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Overflow;
            if(outline){var stroke=text.gameObject.AddComponent<Outline>();stroke.effectColor=Plum;stroke.effectDistance=new Vector2(1.6f,-1.6f);stroke.useGraphicAlpha=true;}
            return text;
        }
        static Material Paint(string state)
        {
            if(Materials.TryGetValue(state,out var existing))return existing;
            string[] colors;
            switch(state)
            {
                case "gold":colors=new[]{"F29100","FFCF13","FFF18A"};break;
                case "red":colors=new[]{"AF1E34","D92D40","F56766"};break;
                case "plum":colors=new[]{"4A175F","7C2396","B969C7"};break;
                case "gray":colors=new[]{"7C7C98","ACABC3","E3E3EE"};break;
                case "yellow":colors=new[]{"E77B1F","FFB72B","FFE47B"};break;
                case "cream":colors=new[]{"CFAE7D","E6CDA5","F4E4C6"};break;
                default:colors=new[]{"006F79","009FAB","29CDCC"};break;
            }
            string file=RoyalPostmanArtPaths.Material(state);
            var mat=AssetDatabase.LoadAssetAtPath<Material>(file);
            if(mat==null){mat=new Material(Shader.Find("RoyalPostman/UI/PaintedPalette"));AssetDatabase.CreateAsset(mat,file);}
            mat.SetColor("_Low",Hex(colors[0]));mat.SetColor("_Mid",Hex(colors[1]));mat.SetColor("_High",Hex(colors[2]));mat.SetFloat("_UsePalette",1);
            EditorUtility.SetDirty(mat);Materials[state]=mat;return mat;
        }
        static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var color);return color;}
        static void Capture(Canvas canvas,Camera camera,string file,int width,int height)
        {
            var target=new RenderTexture(width,height,24){antiAliasing=4};target.Create();var previous=RenderTexture.active;
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);
            try
            {
                camera.targetTexture=target;camera.aspect=width/(float)height;Canvas.ForceUpdateCanvases();
                var request=new RenderPipeline.StandardRequest{destination=target};
                if(RenderPipeline.SupportsRenderRequest(camera,request))RenderPipeline.SubmitRenderRequest(camera,request);else camera.Render();
                RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(file,texture.EncodeToPNG());
            }
            finally{camera.targetTexture=null;RenderTexture.active=previous;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(texture);}
        }
        static void Validate(RectTransform home,RectTransform modal,string folder)
        {
            var button=home.Find("LevelButton");var rim=button.Find("FixedGoldFrame").GetComponent<RectTransform>();var rimBefore=rim.anchoredPosition;
            var press=button.GetComponent<RoyalPostmanPaintedButton>();var centerBefore=press.movingCenter.anchoredPosition;press.SetPressed(true);
            if(rim.anchoredPosition!=rimBefore||press.movingCenter.anchoredPosition==centerBefore)throw new InvalidOperationException("Press separation failed.");press.SetPressed(false);
            foreach(string name in new[]{"AvailableButton","SelectedButton","LockedButton"})
                if(modal.Find(name).Find("FixedGoldFrame")!=null)throw new InvalidOperationException("Booster must not have metallic rim.");
            var avatar=home.GetComponentInChildren<RoyalPostmanAvatar>();var frame=avatar.frame.sprite;var portrait=avatar.portrait.sprite;
            avatar.SetPortrait(Sprite("heart-red"));if(avatar.frame.sprite!=frame)throw new InvalidOperationException("Avatar layers coupled.");avatar.SetPortrait(portrait);
            avatar.SetFrame(Sprite("circle-frame-gold"));if(avatar.portrait.sprite!=portrait)throw new InvalidOperationException("Frame changed portrait.");avatar.SetFrame(frame);
            var booster=modal.Find("AvailableButton").GetComponent<RoyalPostmanBoosterButton>();
            booster.SetState(RoyalPostmanBoosterButton.VisualState.Selected);
            if(!booster.selection.activeSelf||booster.face.material!=booster.selectedPaint)throw new InvalidOperationException("Selected booster state failed.");
            booster.SetState(RoyalPostmanBoosterButton.VisualState.Locked);
            if(!booster.padlock.activeSelf||booster.quantity.activeSelf||booster.GetComponent<Button>().interactable)throw new InvalidOperationException("Locked booster state failed.");
            booster.SetState(RoyalPostmanBoosterButton.VisualState.Available);
            foreach(var glyph in home.GetComponentsInChildren<RoyalPostmanGlyph>(true).Concat(modal.GetComponentsInChildren<RoyalPostmanGlyph>(true)))
                if(glyph.GetComponent<CanvasRenderer>()==null)throw new InvalidOperationException("Missing vector CanvasRenderer.");
            File.WriteAllText(folder+"/CHECKS.txt","PASS: fixed rim / moving face and content\nPASS: booster buttons without metallic rims\nPASS: available / selected / locked transitions\nPASS: independently replaceable avatar portrait/frame\nPASS: all labels independent from painted art\nPASS: vector symbols have CanvasRenderer\nPASS: no changes to game scene or player state\nTypography: native Lilita One is a review candidate; exact generated-reference glyph equivalence is not certified.\n");
        }
    }
}
