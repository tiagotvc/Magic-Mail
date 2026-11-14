using System;
using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using RoyalAves.Meta;
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
    // Preview loads the real campaign assets, never a laboratory override.
    public static class StoneCampaign101Tools
    {
        const string Request = "Logs/stone-101.request";
        const string Active = "MagicMail.Stone101.Preview";
        const string Stage = "MagicMail.Stone101.Stage";
        static double at;
        static double lastStatus;
        static readonly System.Collections.Generic.Dictionary<Item,Vector3> fallPositions=new System.Collections.Generic.Dictionary<Item,Vector3>();
        static bool sawFallFromTop, sawDownwardMovement, sawInteriorDescent, capturedFall;
        [InitializeOnLoadMethod]
        static void Init()
        {
            EditorApplication.update -= Poll; EditorApplication.update += Poll;
            EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed;
            at = EditorApplication.timeSinceStartup;
        }
        static void Changed(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Active,false)) return;
            if (SessionState.GetBool(Active+".HadOpen",false)) PlayerPrefs.SetInt("OpenLevel",SessionState.GetInt(Active+".Open",1));
            else PlayerPrefs.DeleteKey("OpenLevel");
            LightBallLaboratoryTools.RestoreGameView();
            SessionState.SetBool(Active,false);
        }
        [MenuItem("Royal Aves/Elementos/Pedra rúnica/Abrir fase 101 da campanha")]
        public static void Open() { Directory.CreateDirectory("Logs"); File.WriteAllText(Request,"open"); }

        static void Build()
        {
            var asset = AssetDatabase.LoadAssetAtPath<LevelContainer>("Assets/SweetSugar/Resources/Levels/Level_101.asset");
            var data = asset.levelData;
            var template = AssetDatabase.LoadAssetAtPath<LevelContainer>(EnchantedStoneLaboratory.LevelPath).levelData;
            data.fields = JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(template)).fields;
            data.fields = data.fields.Take(1).ToList();
            data.levelNum=101; data.limit=30; data.colorLimit=4;
            data.pieceColors=new System.Collections.Generic.List<int>{0,1,2,6};
            data.currentSublevelIndex=0; data.selectedTutorial=0;
            var field=data.fields[0]; field.maxRows=10; field.maxCols=9; field.layers=2;
            var sample=template.fields[0].levelSquares[0];
            field.levelSquares=new SquareBlocks[90];
            for(int row=0;row<10;row++) for(int col=0;col<9;col++)
            {
                bool rocket=(row==0 && (col==0 || col==8)) || (row==9 && (col==1 || col==7));
                bool bomb=row==2 && col==4;
                var cell=JsonUtility.FromJson<SquareBlocks>(JsonUtility.ToJson(sample));
                cell.blocks.Clear();cell.blocks.Add(new SquareTypeLayer{squareType=SquareTypes.EmptySquare});
                cell.block=rocket || bomb?SquareTypes.EmptySquare:SquareTypes.EnchantedStone;
                if(cell.block==SquareTypes.EnchantedStone)cell.blocks.Add(new SquareTypeLayer{squareType=SquareTypes.EnchantedStone});
                cell.blockLayer=1;cell.obstacle=SquareTypes.NONE;cell.spawners.Clear();cell.startWithoutItem=false;
                cell.item=new ItemForEditor();cell.position=new Vector2Int(col,row);
                cell.direction=Vector2.down;cell.enterSquare=row==0;
                if(rocket || bomb)
                {
                    var type=bomb?ItemsTypes.PACKAGE:ItemsTypes.VERTICAL_STRIPED;
                    cell.item=new ItemForEditor{Item=Resources.Load<GameObject>("Items/"+type),ItemType=type,Color=0};
                }
                field.levelSquares[row*9+col]=cell;
            }
            var goal=AssetDatabase.LoadAssetAtPath<TargetLevel>("Assets/SweetSugar/Resources/Levels/Targets/TargetLevel101.asset");
            data.LoadTargetObject();
            int targetIndex=data.GetTargetIndex("EnchantedStone");
            data.SetTarget(targetIndex);
            goal.targets[0].targetType.type=targetIndex;
            goal.targets[0].CountDrawer.count=85;
            EditorUtility.SetDirty(asset);EditorUtility.SetDirty(goal);AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/stone-101-build.result","READY: real level 101, 9x10, 85 stones, 4 vertical rockets, 1 bomb, 30 moves.");
        }
        static void Require(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
        static void InspectStone()
        {
            var report=new System.Text.StringBuilder();
            var prefab=Resources.Load<GameObject>("Blocks/EnchantedStone");
            void Describe(SpriteRenderer renderer)
            {
                if(renderer==null){report.AppendLine("NO RENDERER");return;}
                var sprite=renderer.sprite;
                report.AppendLine($"{renderer.name}: enabled={renderer.enabled}, active={renderer.gameObject.activeInHierarchy}, sprite={sprite}, vertices={sprite?.vertices.Length}, triangles={sprite?.triangles.Length}, bounds={renderer.bounds}, color={renderer.color}, layer={renderer.sortingLayerName}/{renderer.sortingOrder}, shader={renderer.sharedMaterial?.shader?.name}");
            }
            Describe(prefab.GetComponent<SpriteRenderer>());
            report.AppendLine("Component script="+MonoScript.FromMonoBehaviour(prefab.GetComponent<EnchantedStone>())?.GetClass());
            var manager=LevelManager.THIS;
            if(manager?.field!=null)
            {
                report.AppendLine($"Level={manager.levelData.levelNum}; state={manager.gameStatus}; stones={manager.field.GetComponentsInChildren<EnchantedStone>().Length}");
                foreach(var cell in manager.field.GetSquares().Take(12))
                {
                    report.AppendLine($"Cell {cell.col},{cell.row}: type={cell.type}, entry={cell.isEnterPoint}, direction={cell.direction}, item={cell.Item}, subSquares={cell.subSquares.Count}");
                    foreach(var sub in cell.subSquares)Describe(sub.GetComponent<SpriteRenderer>());
                }
            }
            File.WriteAllText("Logs/rune-visibility-diagnostic.result",report.ToString());
        }
        static void VerifyEditor()
        {
            var config=Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");
            int goalIndex=config.targets.FindIndex(t=>t.name=="EnchantedStone");
            Require((int)SquareTypes.EnchantedStone==15 && goalIndex==14,"Existing enum/target indexes shifted.");
            var stone=SweetSugar.Scripts.Editor.SquareUtils.GetBlockPrefab(SquareTypes.EnchantedStone).Single();
            Require(stone.GetComponent<BindLayer>()!=null,"Rune has no layer binding and can sort behind its cell.");
            Require(stone.GetComponent<SpriteRenderer>().sprite.uv.Distinct().Count()>1,"Rune sprite has collapsed UV coordinates.");
            var crate=SweetSugar.Scripts.Editor.SquareUtils.GetBlockPrefab(SquareTypes.SolidBlock).First();
            Require(stone.GetComponent<Square>().type==SquareTypes.EnchantedStone && stone.GetComponent<EnchantedStone>()!=null,"Rune prefab missing or wrong type.");
            Require(crate.GetComponent<EnchantedStone>()==null,"Crate resolved as rune.");
            var runeIcon=SweetSugar.Scripts.Editor.SquareUtils.GetSquareTexture(SquareTypes.EnchantedStone);
            var crateIcon=SweetSugar.Scripts.Editor.SquareUtils.GetSquareTexture(SquareTypes.SolidBlock);
            Require(runeIcon.Texture2D!=null && runeIcon.Texture2D!=crateIcon.Texture2D,"Rune/box editor icons are not distinct.");
            var source=Resources.Load<LevelContainer>("Levels/Level_101").levelData;
            var copy=JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(source));copy.levelNum=102;
            var view=ScriptableObject.CreateInstance<SweetSugar.Scripts.Editor.LevelMakerEditor>();
            var goals=ScriptableObject.CreateInstance<TargetLevel>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
            var type=view.GetType();
            void Set(string key,object value)=>type.GetField(key,flags).SetValue(view,value);
            try
            {
                Set("levelData",copy);Set("levelNumber",102);Set("subLevelNumber",1);Set("layer",0);
                Set("squareType",SquareTypes.EnchantedStone);
                type.GetMethod("SetSquareType",flags).Invoke(view,new object[]{0,0});
                Require((bool)type.GetField("pendingStoneTarget",flags).GetValue(view),"Rune brush did not request its goal.");
                Set("squareType",SquareTypes.SolidBlock);Set("layer",0);
                type.GetMethod("SetSquareType",flags).Invoke(view,new object[]{8,0});
                var roundtrip=JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(copy));
                Require(roundtrip.GetBlock(0,0).blocks.Any(b=>b.squareType==SquareTypes.EnchantedStone),"Rune paint/save/load failed outside level 101.");
                Require(roundtrip.GetBlock(0,8).blocks.Any(b=>b.squareType==SquareTypes.SolidBlock),"Mixed crate paint/save/load failed.");
                goals.AddTargetType(goalIndex,config);
                Require(goals.targets.Single().sprites.Any(s=>s.icon==stone.GetComponent<SpriteRenderer>().sprite),"Rune goal art is wrong.");
                File.WriteAllText("Logs/rune-editor-native.result","PASS: independent enum/prefab/target, distinct palette images, actual editor brush on an isolated level-102 copy, mixed rune/crate serialization round-trip, automatic goal request and matching goal art. No physical pointer or production level-102 asset modified.");
            }
            finally { UnityEngine.Object.DestroyImmediate(view);UnityEngine.Object.DestroyImmediate(goals); }
        }
        static void VerifyPowerUpMoves(LevelManager manager)
        {
            var items=manager.field.GetItems().ToArray();
            var types=items.Select(i=>i.currentType).ToArray();
            var cells=items.Select(i=>i.square).ToArray();
            var cellTypes=cells.Select(c=>c.type).ToArray();
            try
            {
                foreach(var cell in cells)cell.type=SquareTypes.WireBlock;
                Require(!AI.HasUsablePowerUp(),"Locked powerups counted as usable.");
                cells[0].type=cellTypes[0];
                foreach(var type in new[]{ItemsTypes.HORIZONTAL_STRIPED,ItemsTypes.VERTICAL_STRIPED,
                    ItemsTypes.PACKAGE,ItemsTypes.MARMALADE,ItemsTypes.MULTICOLOR})
                {
                    items[0].currentType=type;
                    Require(AI.HasUsablePowerUp(),"Usable powerup ignored: "+type);
                }
                items[0].falling=true;
                Require(!AI.HasUsablePowerUp(),"Falling powerup counted as usable.");
                items[0].falling=false;items[0].currentType=ItemsTypes.NONE;
                Require(!AI.HasUsablePowerUp(),"Ordinary piece counted as a powerup.");
            }
            finally
            {
                items[0].falling=false;
                for(int i=0;i<items.Length;i++){items[i].currentType=types[i];cells[i].type=cellTypes[i];}
            }
            Require(AI.HasUsablePowerUp(),"Level 101 powerups are not usable.");
            manager.NoMatches();
            Require(!manager.NoMoreMatches.activeSelf && !manager.DragBlocked,"NoMatches interrupted a playable board.");
        }
        static void Poll()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            try
            {
                if(File.Exists(Request))
                {
                    string pendingCommand;
                    try{pendingCommand=File.ReadAllText(Request).Trim();}catch(IOException){return;}
                    if(pendingCommand=="inspect")
                    {File.Delete(Request);InspectStone();return;}
                    if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}
                    if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                    var command=pendingCommand;File.Delete(Request);
                    if(File.Exists("Logs/stone-101-error.result"))File.Delete("Logs/stone-101-error.result");
                    if(command=="stop")return;
                    if(command=="verify-editor"){VerifyEditor();return;}
                    if(command=="prepare"){Build();return;}
                    const string gameScene="Assets/SweetSugar/Scenes/game.unity";
                    bool changeScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=gameScene;
                    if(changeScene && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                    LightBallLaboratory.Disable();
                    foreach(var key in LightBallLaboratory.OtherKeys)SessionState.SetBool(key,false);
                    SessionState.SetBool(Active+".HadOpen",PlayerPrefs.HasKey("OpenLevel"));
                    SessionState.SetInt(Active+".Open",PlayerPrefs.GetInt("OpenLevel",1));
                    PlayerPrefs.SetInt("OpenLevel",101);
                    SessionState.SetBool(Active,true);SessionState.SetInt(Stage,0);
                    SessionState.SetBool(Active+".Captured",false);
                    fallPositions.Clear();sawFallFromTop=sawDownwardMovement=sawInteriorDescent=capturedFall=false;
                    SessionState.SetBool(Active+".Test",command=="test" || command=="refilltest");
                    SessionState.SetBool(Active+".RefillOnly",command=="refilltest");
                    EnchantedStone.RejectedHits=EnchantedStone.BrokenStones=0;
                    if(changeScene)EditorSceneManager.OpenScene(gameScene);
                    LightBallLaboratoryTools.ConfigureGameView();
                    EditorApplication.isPlaying=true;return;
                }
                if(!SessionState.GetBool(Active,false) || !EditorApplication.isPlaying)return;
                var manager=LevelManager.THIS; if(manager==null)return;
                int stage=SessionState.GetInt(Stage,0);
                if(stage==4)
                {
                    float top=manager.field.GetSquare(0,0,true).transform.position.y;
                    foreach(var piece in manager.field.GetItems().Where(i=>i.currentType==ItemsTypes.NONE && i.falling))
                    {
                        var position=piece.transform.position;
                        if(position.y>top+.08f)sawFallFromTop=true;
                        if(position.y<top-manager.field.squareHeight*.5f)sawInteriorDescent=true;
                        if(fallPositions.TryGetValue(piece,out var previous) && previous.y-position.y>.003f)sawDownwardMovement=true;
                        fallPositions[piece]=position;
                        if(!capturedFall && position.y<top+.3f && position.y>manager.field.GetSquare(0,9,true).transform.position.y+.4f)
                        {
                            capturedFall=true;ScreenCapture.CaptureScreenshot("Logs/rune-refill-falling.png");
                        }
                    }
                }
                if(stage!=3)
                {
                    EditorApplication.QueuePlayerLoopUpdate();
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                }
                if(EditorApplication.timeSinceStartup-lastStatus>1)
                {
                    lastStatus=EditorApplication.timeSinceStartup;
                    File.WriteAllText("Logs/stone-101-status.result",$"stage={stage}, state={manager.gameStatus}, level={manager.levelData?.levelNum}, broken={EnchantedStone.BrokenStones}, goal={manager.levelData?.TargetCounters?.Sum(t=>t.count)}, timeScale={Time.timeScale}, time={Time.time}\n"+
                        (manager.field==null?"":string.Join("\n",manager.field.GetItems().Select(i=>$"{i.currentType}: fall={i.falling}, locked={i.square.IsItemLocked}, active={i.gameObject.activeSelf}, pos={i.transform.position}"))));
                }
                if(stage==0 && manager.gameStatus==GameState.Map && AI.THIS!=null)
                {
                    Application.runInBackground=true;
                    var gameView=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
                    if(gameView!=null)EditorWindow.GetWindow(gameView).Focus();
                    manager.BoostPackage=manager.BoostColorfullBomb=manager.BoostStriped=0;
                    manager.gameStatus=GameState.PrepareGame;SessionState.SetInt(Stage,1);at=EditorApplication.timeSinceStartup;
                }
                else if(stage==1 && UnityEngine.Object.FindFirstObjectByType<ElementIntroPrototype>()!=null)
                {
                    Require(manager.levelData.levelNum==101 && !EnchantedStoneLaboratory.IsActive && !LightBallLaboratory.IsActive,"Not the real campaign load.");
                    Require(manager.field.GetComponentsInChildren<EnchantedStone>().Length==85,"Expected 85 stones.");
                    Require(manager.field.GetComponentsInChildren<EnchantedStone>().All(stone=>
                        stone.GetComponent<SpriteRenderer>().sortingOrder > stone.GetComponent<Square>().mainSquare.GetComponent<SpriteRenderer>().sortingOrder),
                        "Rune is behind its background cell.");
                    Require(manager.levelData.TargetCounters.Sum(t=>t.count)==85,"Expected goal 85.");
                    Require(manager.levelData.TargetCounters[0].extraObject.texture.name=="stone-paw-v1","Incorrect HUD goal artwork.");
                    Require(manager.gameStatus==GameState.Pause,"Introduction did not block gameplay.");
                    SessionState.SetInt(Stage,2);at=EditorApplication.timeSinceStartup;
                }
                else if(stage==2 && !SessionState.GetBool(Active+".Captured",false) &&
                    manager.gameStatus==GameState.Pause && EditorApplication.timeSinceStartup-at>.7)
                {
                    ScreenCapture.CaptureScreenshot("Logs/stone-101-intro.png");
                    SessionState.SetBool(Active+".Captured",true);
                }
                else if(stage==2 && manager.gameStatus==GameState.Playing && EditorApplication.timeSinceStartup-at>1)
                {
                    ScreenCapture.CaptureScreenshot("Logs/stone-101-board.png");
                    File.WriteAllText("Logs/stone-101-preview.result","PASS: actual campaign assets, 85 stones/goal, intro paused and resumed, correct goal art. No physical-pointer/full-victory test.");
                    SessionState.SetInt(Stage,3);
                    if(SessionState.GetBool(Active+".Test",false))
                    {
                        VerifyPowerUpMoves(manager);
                        at=EditorApplication.timeSinceStartup;SessionState.SetInt(Stage,5);
                    }
                }
                else if(stage==5)
                {
                    Require(manager.gameStatus==GameState.Playing && !manager.DragBlocked && !manager.NoMoreMatches.activeSelf,
                        "No-moves loop interrupted level 101 while powerups were available.");
                    if(SessionState.GetBool(Active+".RefillOnly",false) || EditorApplication.timeSinceStartup-at>6)
                    {
                        if(!SessionState.GetBool(Active+".RefillOnly",false))
                            File.WriteAllText("Logs/no-moves-powerup-native.result","PASS: five powerup types accepted; locked/falling/non-powerup cases rejected; direct NoMatches suppressed; real level 101 stayed playable for six seconds without a no-moves popup or shuffle. No audible-audio/physical-pointer claim.");
                        var cell=manager.field.GetSquare(1,0,true);
                        var renderer=cell.GetSubSquare().GetComponent<SpriteRenderer>();var color=renderer.color;
                        cell.DestroyBlock(impactOrigin:manager.field.GetSquare(2,0,true));
                        Require(renderer.color==color && !cell.GetSubSquare().GetComponent<EnchantedStone>().IsBreaking,"Ordinary hit changed stone.");
                        manager.field.GetSquare(0,0,true).Item.ActivateFromSwap();
                        at=EditorApplication.timeSinceStartup;SessionState.SetInt(Stage,4);
                    }
                }
                else if(stage==4 && EditorApplication.timeSinceStartup-at>4)
                {
                    Require(sawFallFromTop && sawDownwardMovement && sawInteriorDescent,"Refill did not travel down into the board from the top entrance.");
                    Require(EnchantedStone.BrokenStones>0 && manager.levelData.TargetCounters.Sum(t=>t.count)==85-EnchantedStone.BrokenStones,
                        $"Rocket break/goal mismatch: broken={EnchantedStone.BrokenStones}, goal={manager.levelData.TargetCounters.Sum(t=>t.count)}, state={manager.gameStatus}.");
                    File.AppendAllText("Logs/stone-101-preview.result","\nPASS: ordinary hit has no response; real rocket broke stones and updated goal exactly. No victory/rewards triggered.");
                    ScreenCapture.CaptureScreenshot("Logs/rune-refill-settled.png");
                    File.WriteAllText("Logs/rune-refill-native.result","PASS: top-only entrances; new ordinary pieces observed above the board and descending across frames after the vertical rocket; rune break and goal accounting retained. Captures: stone-101-intro.png, rune-refill-falling.png. No physical-pointer/full-victory claim.");
                    SessionState.SetInt(Stage,3);
                }
                if(stage<3 && EditorApplication.timeSinceStartup-at>90)throw new TimeoutException("Campaign 101 preview timed out at stage "+stage+", state "+manager.gameStatus);
            }
            catch(Exception ex){SessionState.SetInt(Stage,3);File.WriteAllText("Logs/stone-101-error.result",ex.ToString());Debug.LogException(ex);}
        }
    }
}
