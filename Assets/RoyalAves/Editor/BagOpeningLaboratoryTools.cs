using System;
using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public sealed class BagOpeningLaboratoryTools : EditorWindow
    {
        const string Menu="Royal Aves/Elementos/Bolsa do carteiro/";
        const string Request="Logs/bag-opening-lab.request";
        const string VerifyRequest="Logs/bag-opening-lab.verify";
        static BagOpeningStation verificationBag;
        static SweetSugar.Scripts.TargetScripts.TargetSystem.TargetCounter verificationTarget;
        static int verificationCount;
        static double verificationStarted;
        const string StartKey="MagicMail.BagOpeningLab.Start";
        const string Art="Assets/RoyalAves/Art/Obstacles/Mailbag/CM_bolsa_carteiro_abrir_sem_alca_v2.png";
        [Serializable] sealed class Frame { public float x=0,y=0,width=0,height=0,anchorX=0,anchorY=0; }
        [Serializable] sealed class FrameSheet { public Frame[] frames=new Frame[0]; public float fps=20; }

        [InitializeOnLoadMethod]
        static void Initialize() {EditorApplication.update-=Poll;EditorApplication.update+=Poll;}

        [MenuItem(Menu+"Laboratório de abertura em U")]
        [MenuItem("Royal Aves/Elementos/Malote/Laboratório de abertura em U")]
        public static void Preview()
        {
            Directory.CreateDirectory("Logs");File.WriteAllText(Request,"preview");
        }

        [MenuItem(Menu+"Preparar laboratório em U")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Pare o Play antes de preparar.");
            var definition=JsonUtility.FromJson<FrameSheet>(File.ReadAllText("Docs/Bolsa_Abrir_Frames.json"));
            if (definition==null || definition.frames.Length!=16 || definition.fps<=0)
                throw new InvalidOperationException("A abertura deve ter 16 quadros e uma velocidade válida.");
            AssetDatabase.ImportAsset(Art);
            var importer=(TextureImporter)AssetImporter.GetAtPath(Art);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.maxTextureSize=2048;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art);
            var frames=new Sprite[16];
            for (int i=0;i<frames.Length;i++)
            {
                var frame=definition.frames[i];
                string path="Assets/RoyalAves/Laboratory/BagOpeningFrame"+i.ToString("00")+".asset";
                frames[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (frames[i]!=null) continue;
                var rect=new Rect(frame.x,texture.height-frame.y-frame.height,frame.width,frame.height);
                var pivot=new Vector2(frame.anchorX/frame.width,1-frame.anchorY/frame.height);
                frames[i]=Sprite.Create(texture,rect,pivot,texture.width/4f/2f,0,SpriteMeshType.FullRect);
                frames[i].name="BagOpeningFrame"+i.ToString("00");AssetDatabase.CreateAsset(frames[i],path);
            }
            const string scrollArt="Assets/RoyalAves/Art/Obstacles/Mailbag/CM_pergaminho_abrir_sheet_v1.png";
            AssetDatabase.ImportAsset(scrollArt);
            var scrollImporter=(TextureImporter)AssetImporter.GetAtPath(scrollArt);
            scrollImporter.textureType=TextureImporterType.Sprite;scrollImporter.spriteImportMode=SpriteImportMode.Single;
            scrollImporter.textureCompression=TextureImporterCompression.Uncompressed;scrollImporter.alphaIsTransparency=true;
            scrollImporter.mipmapEnabled=false;scrollImporter.npotScale=TextureImporterNPOTScale.None;
            scrollImporter.maxTextureSize=2048;scrollImporter.SaveAndReimport();
            var scrollTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(scrollArt);
            var scrollDefinition=JsonUtility.FromJson<FrameSheet>(File.ReadAllText("Docs/Pergaminho_Abrir_Frames.json"));
            var scrollFrames=new Sprite[16];
            for(int i=0;i<16;i++)
            {
                string path="Assets/RoyalAves/Laboratory/ScrollOpeningFrame"+i.ToString("00")+".asset";
                scrollFrames[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(scrollFrames[i]!=null)continue;
                var f=scrollDefinition.frames[i];
                scrollFrames[i]=Sprite.Create(scrollTexture,new Rect(f.x,scrollTexture.height-f.y-f.height,f.width,f.height),new Vector2(.5f,.5f),scrollTexture.width/8f,0,SpriteMeshType.FullRect);
                scrollFrames[i].name="ScrollOpeningFrame"+i.ToString("00");AssetDatabase.CreateAsset(scrollFrames[i],path);
            }
            var source=Resources.Load<GameObject>("Blocks/SolidBlock").GetComponent<LayeredBlock>().layers[0];
            var root=Instantiate(source.gameObject);
            GameObject prefab;
            try
            {
                root.name="BagOpeningCell";
                foreach (var parcel in root.GetComponents<ParcelStackLayer>()) DestroyImmediate(parcel);
                var square=root.GetComponent<Square>();
                square.type=SquareTypes.SolidBlock;square.canMoveIn=square.canMoveOut=false;
                square.undestroyable=square.cannotBeDestroy=false;square.sizeInSquares=Vector2Int.one;
                var renderer=root.GetComponent<SpriteRenderer>();renderer.sprite=frames[0];renderer.color=Color.white;
                var station=root.AddComponent<BagOpeningStation>();station.frames=frames;station.scrollFrames=scrollFrames;station.framesPerSecond=definition.fps;
                prefab=PrefabUtility.SaveAsPrefabAsset(root,MailbagLaboratory.CellPath);
            }
            finally {DestroyImmediate(root);}
            var config=Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");
            var goal=config.targets.FirstOrDefault(t=>t.name=="BagOpenings");
            if (goal==null)
            {
                goal=JsonUtility.FromJson<TargetContainer>(JsonUtility.ToJson(config.targets.First(t=>t.name=="FlyingLetters")));
                goal.name="BagOpenings";config.targets.Add(goal);
            }
            goal.localization.description.text="Colete os pergaminhos";goal.localization.failed.text="Ainda faltam pergaminhos";
            goal.setCount=SetCount.Manually;goal.prefabs=new System.Collections.Generic.List<GameObject>{prefab};
            foreach (var group in goal.defaultSprites) foreach (var sprite in group.sprites) sprite.icon=scrollFrames[15];
            EditorUtility.SetDirty(config);
            var template=AssetDatabase.LoadAssetAtPath<LevelContainer>(FlyingLetterLaboratory.LevelPath);
            var lab=AssetDatabase.LoadAssetAtPath<LevelContainer>(MailbagLaboratory.LevelPath);
            if (lab==null) {lab=CreateInstance<LevelContainer>();AssetDatabase.CreateAsset(lab,MailbagLaboratory.LevelPath);}
            lab.levelData=JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(template.levelData));
            var data=lab.levelData;data.levelNum=81;data.limit=50;data.colorLimit=4;
            data.pieceColors=new System.Collections.Generic.List<int>{0,1,2,6};
            var field=data.fields[0];field.layers=2;
            var normal=Resources.Load<GameObject>("Items/Item");int count=0;
            for (int row=0;row<field.maxRows;row++)
            for (int col=0;col<field.maxCols;col++)
            {
                var cell=field.levelSquares[row*field.maxCols+col];
                bool bag=IsUBorder(col,row,field.maxCols,field.maxRows);
                cell.blocks.Clear();cell.blocks.Add(new SquareTypeLayer{squareType=SquareTypes.EmptySquare});
                cell.block=bag?SquareTypes.SolidBlock:SquareTypes.EmptySquare;cell.blockLayer=1;
                cell.obstacle=SquareTypes.NONE;cell.startWithoutItem=false;
                cell.item=bag?new ItemForEditor():new ItemForEditor{Item=normal,ItemType=ItemsTypes.NONE,Color=data.pieceColors[(row+2*col)%4]};
                if (bag) {cell.blocks.Add(new SquareTypeLayer{squareType=SquareTypes.SolidBlock});count++;}
            }
            void Set(int col,int row,int color) => field.levelSquares[row*field.maxCols+col].item.Color=color;
            Set(1,1,1);Set(2,1,1);Set(3,1,2);Set(3,2,1);
            field.levelSquares[3*field.maxCols+3].item=new ItemForEditor{Item=Resources.Load<GameObject>("Items/HORIZONTAL_STRIPED"),ItemType=ItemsTypes.HORIZONTAL_STRIPED,Color=0};
            MailbagLaboratory.UseBlueEnvelope(data);
            int index=config.targets.FindIndex(t=>t.name=="BagOpenings");data.LoadTargetObject();data.SetTarget(index);
            var targets=AssetDatabase.LoadAssetAtPath<TargetLevel>(MailbagLaboratory.TargetPath);
            if (targets==null) {targets=CreateInstance<TargetLevel>();AssetDatabase.CreateAsset(targets,MailbagLaboratory.TargetPath);}
            int scrollCount=targets.targets != null && targets.targets.Count>0 && targets.targets[0].sprites.Any(s=>s.icon==scrollFrames[0] || s.icon==scrollFrames[15]) ? targets.targets[0].CountDrawer.count : 20;
            targets.targets=new TargetList();targets.targets.Add(new TargetObject{targetType=new TargetType{type=index},
                sprites=new SpriteList{new SpriteObject{icon=scrollFrames[15],uiSprite=true}},
                CountDrawer=new Count{count=scrollCount},ShowTheScoreForStar=new BoolScoreShow()});
            EditorUtility.SetDirty(lab);EditorUtility.SetDirty(targets);AssetDatabase.SaveAssets();
            if (count!=19) throw new InvalidOperationException("O U deve conter 19 bolsas no tabuleiro 7x7.");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/bag-opening-lab.result","READY: 19 bolsas fixas em U, 16 quadros sem alça, base alinhada, tabuleiro 7x7, 50 jogadas; meta manual de pergaminhos.");
        }

        public static bool IsUBorder(int col,int row,int columns,int rows) =>
            col>=0 && row>=0 && col<columns && row<rows && (col==0 || col==columns-1 || row==rows-1);

        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (SessionState.GetBool(StartKey,false) && EditorApplication.isPlaying && LevelManager.THIS!=null
                && AI.THIS!=null && LevelManager.THIS.gameStatus==GameState.Map)
            {
                SessionState.SetBool(StartKey,false);BagOpeningStation.PlaybackSpeed=1;
                LevelManager.THIS.BoostPackage=LevelManager.THIS.BoostColorfullBomb=LevelManager.THIS.BoostStriped=0;
                LevelManager.THIS.gameStatus=GameState.PrepareGame;GetWindow<BagOpeningLaboratoryTools>("Bolsa — abertura");
            }
            VerifyDelivery();
            if (!File.Exists(Request)) return;
            if (EditorApplication.isPlaying) {EditorApplication.isPlaying=false;return;}
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            try
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                Prepare();
                foreach (string key in new[]{RouteCentralLaboratory.EnabledKey,FlyingLetterLaboratory.EnabledKey,
                    MailCubbyLaboratory.EnabledKey,FrankingMachineLaboratory.EnabledKey,InkLaboratory.EnabledKey,ParcelStackLaboratory.EnabledKey,
                    "MagicMail.MailbagLab.Start"}) SessionState.SetBool(key,false);
                SessionState.SetBool(MailbagLaboratory.EnabledKey,true);SessionState.SetBool(StartKey,true);
                EditorSceneManager.OpenScene("Assets/SweetSugar/Scenes/main.unity");EditorApplication.isPlaying=true;
            }
            catch (Exception error) {File.WriteAllText("Logs/bag-opening-lab.result",error.ToString());Debug.LogException(error);}
        }

        static void VerifyDelivery()
        {
            if (verificationBag != null)
            {
                if (!verificationBag.IsAnimating)
                {
                    File.WriteAllText("Logs/bag-opening-lab.verify-result",verificationTarget.count == verificationCount-1
                        ? "PASS: native PlayMode bag opening and complete scroll delivery; goal decremented once on arrival."
                        : "FAIL: delivery finished without expected goal change.");
                    verificationBag=null;
                }
                else if (EditorApplication.timeSinceStartup-verificationStarted>15)
                {
                    File.WriteAllText("Logs/bag-opening-lab.verify-result","FAIL: delivery did not finish within 15 seconds.");
                    verificationBag=null;
                }
                return;
            }
            if (!File.Exists(VerifyRequest) || !EditorApplication.isPlaying || !MailbagLaboratory.IsActive) return;
            var manager=LevelManager.THIS;
            var target=manager?.levelData?.TargetCounters?.FirstOrDefault(t=>t.targetLevel.name=="BagOpenings" && t.count>0);
            var bag=UnityEngine.Object.FindObjectsByType<BagOpeningStation>(FindObjectsSortMode.None).FirstOrDefault(b=>!b.IsAnimating);
            if (target==null || bag==null || !bag.PlayOpening()) return;
            File.Delete(VerifyRequest);
            verificationBag=bag;verificationTarget=target;verificationCount=target.count;
            verificationStarted=EditorApplication.timeSinceStartup;
            File.WriteAllText("Logs/bag-opening-lab.verify-result","RUNNING: native opening accepted; awaiting HUD arrival.");
        }

        void OnInspectorUpdate()=>Repaint();
        void OnGUI()
        {
            GUILayout.Label("Bolsas em U — teste de abertura",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("19 bolsas fixas nas duas laterais e no fundo. Matches adjacentes e especiais abrem a bolsa; ela fecha novamente. Um pergaminho sai fechado, abre e voa em arco até a meta. A quantidade é definida na meta Pergaminhos do Level Editor.",MessageType.Info);
            if (GUILayout.Button("Abrir / reiniciar laboratório em U")) Preview();
            if (GUILayout.Button("Desativar laboratório")) MailbagLaboratoryTools.Disable();
            BagOpeningStation.PlaybackSpeed=EditorGUILayout.Slider("Velocidade",BagOpeningStation.PlaybackSpeed,.25f,2);
            EditorGUILayout.HelpBox("Primeira jogada: trocar (3,1) com (3,2), contando de zero. Foguete horizontal em (3,3) testa as duas laterais.",MessageType.None);
            bool active=EditorApplication.isPlaying && MailbagLaboratory.IsActive;
            using (new EditorGUI.DisabledScope(!active))
            {
                if (GUILayout.Button("Abrir todas as bolsas"))
                    foreach (var bag in UnityEngine.Object.FindObjectsByType<BagOpeningStation>(FindObjectsSortMode.None)) bag.PlayOpening();
                if (GUILayout.Button("Abrir uma bolsa"))
                    UnityEngine.Object.FindObjectsByType<BagOpeningStation>(FindObjectsSortMode.None).FirstOrDefault(b=>!b.IsAnimating)?.PlayOpening();
            }
        }
    }
}
