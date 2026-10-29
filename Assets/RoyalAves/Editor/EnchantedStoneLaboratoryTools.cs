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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public sealed class EnchantedStoneLaboratoryTools : EditorWindow
    {
        const string Menu = "Royal Aves/Elementos/Pedra rúnica/";
        const string Art = "Assets/RoyalAves/Art/Obstacles/EnchantedStone/stone-paw-v1.png";
        const string Request = "Logs/stone-lab.request";
        const string StartKey = "MagicMail.StoneLab.Start";
        const string TestKey = "MagicMail.StoneLab.Test";
        static int testStage;
        static double testTime;
        static double lastStatus;
        static bool breakCaptured;
        static readonly Vector2Int[] Positions = { new Vector2Int(1,2), new Vector2Int(5,3), new Vector2Int(3,5) };

        [InitializeOnLoadMethod]
        static void Initialize() { EditorApplication.update -= Poll; EditorApplication.update += Poll; }

        [MenuItem(Menu + "Laboratório no jogo")]
        public static void Open() { Directory.CreateDirectory("Logs"); File.WriteAllText(Request,"preview"); }

        [MenuItem(Menu + "Preparar laboratório")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Pare o Play antes de preparar.");
            Directory.CreateDirectory("Assets/RoyalAves/Laboratory");
            AssetDatabase.ImportAsset(Art);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Art);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = true; importer.maxTextureSize = 2048; importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Art);
            // Crop transparent canvas, not artwork; the generated PNG itself stays byte-identical.
            int left=texture.width, right=0, bottom=texture.height, top=0;
            var pixels=texture.GetPixels32();
            for(int y=0;y<texture.height;y++) for(int x=0;x<texture.width;x++)
                if(pixels[y*texture.width+x].a>24) {left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
            var rect = new Rect(left,bottom,right-left+1,top-bottom+1);
            var sprite = Sprite.Create(texture,rect,Vector2.one*.5f,Mathf.Max(rect.width,rect.height)/1.85f,0,SpriteMeshType.FullRect);
            sprite.name="StonePaw";
            const string spritePath="Assets/RoyalAves/Laboratory/EnchantedStoneSprite.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if(existing==null) AssetDatabase.CreateAsset(sprite,spritePath);
            else {DestroyImmediate(sprite);sprite=existing;}
            var source=Resources.Load<GameObject>("Blocks/SolidBlock").GetComponent<LayeredBlock>().layers[0];
            var root=Instantiate(source.gameObject);
            try
            {
                root.name="EnchantedStoneCell";
                foreach(var marker in root.GetComponents<ParcelStackLayer>()) DestroyImmediate(marker);
                var square=root.GetComponent<Square>();
                square.type=SquareTypes.SolidBlock;square.canMoveIn=square.canMoveOut=false;
                square.undestroyable=square.cannotBeDestroy=false;square.sizeInSquares=Vector2Int.one;
                root.GetComponent<SpriteRenderer>().sprite=sprite;
                root.GetComponent<SpriteRenderer>().color=Color.white;
                root.AddComponent<EnchantedStone>();
                PrefabUtility.SaveAsPrefabAsset(root,EnchantedStoneLaboratory.PrefabPath);
            }
            finally { DestroyImmediate(root); }
            var template=AssetDatabase.LoadAssetAtPath<LevelContainer>(FlyingLetterLaboratory.LevelPath);
            if(template==null) throw new InvalidOperationException("Falta a fase modelo 7×7 do laboratório de ovo.");
            var lab=AssetDatabase.LoadAssetAtPath<LevelContainer>(EnchantedStoneLaboratory.LevelPath);
            if(lab==null) {lab=CreateInstance<LevelContainer>();AssetDatabase.CreateAsset(lab,EnchantedStoneLaboratory.LevelPath);}
            lab.levelData=JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(template.levelData));
            var data=lab.levelData;data.fields=data.fields.Take(1).ToList();data.currentSublevelIndex=0;
            data.levelNum=101;data.limit=40;data.selectedTutorial=0;
            data.pieceColors=new System.Collections.Generic.List<int>{0,1,2,6};data.colorLimit=4;
            var field=data.fields[0];
            if(field.maxRows!=7 || field.maxCols!=7) throw new InvalidOperationException("O modelo precisa ser 7×7.");
            field.layers=2;
            var normal=Resources.Load<GameObject>("Items/Item");
            for(int row=0;row<7;row++) for(int col=0;col<7;col++)
            {
                var cell=field.levelSquares[row*7+col];
                bool stone=Positions.Contains(new Vector2Int(col,row));
                cell.blocks.Clear();cell.blocks.Add(new SquareTypeLayer{squareType=SquareTypes.EmptySquare});
                cell.block=stone?SquareTypes.SolidBlock:SquareTypes.EmptySquare;cell.blockLayer=1;
                cell.obstacle=SquareTypes.NONE;cell.spawners.Clear();cell.startWithoutItem=false;
                cell.item=stone?new ItemForEditor():new ItemForEditor{Item=normal,ItemType=ItemsTypes.NONE,Color=data.pieceColors[(row+2*col)%4]};
                if(stone) cell.blocks.Add(new SquareTypeLayer{squareType=SquareTypes.SolidBlock});
            }
            void Special(int x,int y,ItemsTypes type) => field.levelSquares[y*7+x].item = new ItemForEditor
                { Item=Resources.Load<GameObject>("Items/"+type),ItemType=type,Color=0 };
            Special(1,4,ItemsTypes.VERTICAL_STRIPED);
            Special(3,3,ItemsTypes.HORIZONTAL_STRIPED);
            Special(3,4,ItemsTypes.PACKAGE);
            // Prepared ordinary match beside the first stone: swap (2,1) with (3,1).
            field.levelSquares[0*7+2].item.Color=1;
            field.levelSquares[1*7+2].item.Color=2;
            field.levelSquares[2*7+2].item.Color=1;
            field.levelSquares[1*7+3].item.Color=1;
            data.LoadTargetObject();int index=data.GetTargetIndex("SolidBlock");data.SetTarget(index);
            var targets=AssetDatabase.LoadAssetAtPath<TargetLevel>(EnchantedStoneLaboratory.TargetPath);
            if(targets==null) {targets=CreateInstance<TargetLevel>();AssetDatabase.CreateAsset(targets,EnchantedStoneLaboratory.TargetPath);}
            targets.targets=new TargetList {new TargetObject {targetType=new TargetType{type=index},
                sprites=new SpriteList{new SpriteObject{icon=sprite,uiSprite=true}},
                CountDrawer=new Count{count=3},ShowTheScoreForStar=new BoolScoreShow()}};
            EditorUtility.SetDirty(lab);EditorUtility.SetDirty(targets);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/stone-lab-prepare.result","READY: 7x7, 3 pedras, meta 3, 40 jogadas, foguetes vertical/horizontal e dinamite; campanha preservada.");
        }

        [MenuItem(Menu + "Desativar laboratório")]
        public static void Disable()
        {
            SessionState.SetBool(EnchantedStoneLaboratory.EnabledKey,false);SessionState.SetBool(StartKey,false);SessionState.SetBool(TestKey,false);
            if(EditorApplication.isPlaying) EditorApplication.isPlaying=false;
        }

        static void Poll()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            // The legacy hand tutorial is disabled globally but PrePlay still enters Tutorial.
            // This bench has no hand tutorial; release it only after PrePlay's finish event.
            if(EditorApplication.isPlaying && EnchantedStoneLaboratory.IsActive && LevelManager.THIS!=null &&
                LevelManager.THIS.gameStatus==GameState.Tutorial)
                LevelManager.THIS.gameStatus=GameState.Playing;
            if(SessionState.GetBool(TestKey,false) && EditorApplication.timeSinceStartup-lastStatus>1)
            {
                lastStatus=EditorApplication.timeSinceStartup;
                File.WriteAllText("Logs/stone-lab-status.result",$"play={EditorApplication.isPlaying}; active={EnchantedStoneLaboratory.IsActive}; enabled={SessionState.GetBool(EnchantedStoneLaboratory.EnabledKey,false)}; start={SessionState.GetBool(StartKey,false)}; manager={LevelManager.THIS}; state={LevelManager.THIS?.gameStatus}; stage={testStage}; scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            }
            if(SessionState.GetBool(TestKey,false) && EditorApplication.isPlaying && EnchantedStoneLaboratory.IsActive)
            {
                try { TestInPlay(); }
                catch(Exception error)
                { SessionState.SetBool(TestKey,false);File.WriteAllText("Logs/stone-lab-native-test.result",error.ToString());Debug.LogException(error); }
            }
            if(SessionState.GetBool(StartKey,false) && EditorApplication.isPlaying && LevelManager.THIS!=null &&
                AI.THIS!=null && LevelManager.THIS.gameStatus==GameState.Map)
            {
                SessionState.SetBool(StartKey,false);
                LevelManager.THIS.BoostPackage=LevelManager.THIS.BoostColorfullBomb=LevelManager.THIS.BoostStriped=0;
                LevelManager.THIS.gameStatus=GameState.PrepareGame;
                GetWindow<EnchantedStoneLaboratoryTools>("Pedra rúnica");
            }
            if(!File.Exists(Request))return;
            if(EditorApplication.isPlaying) {EditorApplication.isPlaying=false;return;}
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            string command=File.ReadAllText(Request).Trim();File.Delete(Request);
            try
            {
                if(command=="prepare") {Prepare();return;}
                if(command=="test") {SessionState.SetBool(TestKey,true);testStage=0;breakCaptured=false;testTime=EditorApplication.timeSinceStartup;}
                if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                Prepare();
                foreach(string key in new[]{MailbagLaboratory.EnabledKey,RouteCentralLaboratory.EnabledKey,
                    FlyingLetterLaboratory.EnabledKey,MailCubbyLaboratory.EnabledKey,FrankingMachineLaboratory.EnabledKey,
                    InkLaboratory.EnabledKey,ParcelStackLaboratory.EnabledKey}) SessionState.SetBool(key,false);
                SessionState.SetBool(EnchantedStoneLaboratory.EnabledKey,true);SessionState.SetBool(StartKey,true);
                EditorSceneManager.OpenScene("Assets/SweetSugar/Scenes/game.unity");EditorApplication.isPlaying=true;
            }
            catch(Exception error) {File.WriteAllText("Logs/stone-lab-prepare.result",error.ToString());Debug.LogException(error);}
        }

        // Diagnostic native smoke, deliberately separate from pointer/playtest acceptance.
        static void TestInPlay()
        {
            var manager=LevelManager.THIS;
            if(manager==null || manager.field==null || manager.gameStatus!=GameState.Playing) return;
            double now=EditorApplication.timeSinceStartup;
            int Count()=>manager.levelData.TargetCounters.Sum(t=>t.count);
            void Require(bool condition,string message) {if(!condition)throw new InvalidOperationException(message);}
            if(testStage==0)
            {
                Require(Count()==3,"Meta inicial precisa ser 3.");
                Require(manager.levelData.TargetCounters[0].extraObject.texture.name=="stone-paw-v1","?cone da meta n?o ? a pedra.");
                var cell=manager.field.GetSquare(1,2,true);
                Require(cell.GetSubSquare().GetComponent<EnchantedStone>()!=null,"Pedra não instanciada.");
                // Same public routing used by an adjacent ordinary match.
                cell.DestroyBlock(impactOrigin:manager.field.GetSquare(2,2,true));
                Require(!cell.GetSubSquare().GetComponent<EnchantedStone>().IsBreaking && Count()==3,"Match causou dano indevido.");
                var rocket=manager.field.GetSquare(1,4,true).Item;
                Require(rocket!=null && rocket.currentType==ItemsTypes.VERTICAL_STRIPED,"Foguete preparado ausente.");
                rocket.ActivateFromSwap();testStage=1;testTime=now;
            }
            else if(testStage==1 && now-testTime>1.8)
            {
                Require(manager.field.GetSquare(1,2,true).GetSubSquare().GetComponent<EnchantedStone>()==null,"Foguete não removeu a pedra.");
                Require(Count()==2 && EnchantedStone.BrokenStones==1,"Contagem após foguete incorreta.");
                var stone=manager.field.GetSquare(5,3,true);
                stone.DestroyBlock();stone.DestroyBlock();
                testStage=2;testTime=now;

            }
            else if(testStage==2 && !breakCaptured && now-testTime>.12)
            {
                breakCaptured=true;ScreenCapture.CaptureScreenshot("Logs/stone-lab-native-break.png");
            }
            else if(testStage==2 && now-testTime>1)
            {
                Require(Count()==1 && EnchantedStone.BrokenStones==2,"Impacto repetido duplicou a coleta.");
                manager.field.GetSquare(3,5,true).DestroyBlock();testStage=3;testTime=now;
            }
            else if(testStage==3 && now-testTime>1)
            {
                Require(Count()==0 && EnchantedStone.BrokenStones==3,"Meta final incorreta.");
                Require(EnchantedStone.RejectedHits>0,"Nenhum match rejeitado registrado.");
                SessionState.SetBool(TestKey,false);
                File.WriteAllText("Logs/stone-lab-native-test.result","PASS: Play nativo, 3 pedras/meta 3, match adjacente rejeitado pela rota DestroyBlock; foguete real ActivateFromSwap removeu uma pedra; duas chamadas repetidas coletaram só uma vez; meta final 0. Sem teste de ponteiro, áudio ou campanha. Reabrindo bancada limpa.");
                Open();
            }
        }

        void OnInspectorUpdate()=>Repaint();
        void OnGUI()
        {
            GUILayout.Label("Pedra rúnica — teste no tabuleiro",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Match adjacente não quebra. Um impacto direto de especial despedaça a pedra. Meta conta uma vez na liberação da célula. Sem áudio próprio nesta versão.",MessageType.Info);
            if(GUILayout.Button("Abrir / reiniciar laboratório"))Open();
            if(GUILayout.Button("Desativar laboratório"))Disable();
            EnchantedStone.EffectSpeed=EditorGUILayout.Slider("Velocidade da quebra",EnchantedStone.EffectSpeed,.25f,2f);
            EnchantedStone.Scatter=EditorGUILayout.Slider("Dispersão",EnchantedStone.Scatter,.3f,1.8f);
            EditorGUILayout.LabelField($"Impactos comuns rejeitados: {EnchantedStone.RejectedHits} | Pedras removidas: {EnchantedStone.BrokenStones}");
            EditorGUILayout.HelpBox("Match preparado: troque (2,1) com (3,1), coordenadas a partir de zero. Ative os foguetes/dinamite no grid. Os botões abaixo são diagnósticos, não simulam entrada de ponteiro.",MessageType.None);
            bool active=EditorApplication.isPlaying && EnchantedStoneLaboratory.IsActive && LevelManager.THIS?.field!=null && LevelManager.GetGameStatus()==GameState.Playing;
            using(new EditorGUI.DisabledScope(!active))
            foreach(var position in Positions)
            {
                var cell=active?LevelManager.THIS.field.GetSquare(position.x,position.y,true):null;
                var stone=cell!=null?cell.GetSubSquare().GetComponent<EnchantedStone>():null;
                GUILayout.BeginHorizontal();GUILayout.Label($"Pedra ({position.x},{position.y})");
                using(new EditorGUI.DisabledScope(stone==null || stone.IsBreaking))
                {
                    if(GUILayout.Button("Match — diagnóstico"))stone.HandleHit(cell,false);
                    if(GUILayout.Button("Especial — diagnóstico"))stone.HandleHit(cell,true);
                }
                GUILayout.EndHorizontal();
            }
        }
    }
}
