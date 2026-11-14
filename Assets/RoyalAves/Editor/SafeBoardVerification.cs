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
    // The reused laboratory fixtures are modified ONLY inside this disposable validation copy.
    public static class SafeBoardVerification
    {
        const string Key="MagicMail.SafeBoardVerification";
        static int step,expected;
        static double at;
        static SafeCell first,second;
        static Square[] footprint;
        [InitializeOnLoadMethod] static void Listen(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;}
        public static void Run()
        {
            if(!Application.dataPath.Replace('\\','/').Contains("/Logs/InkwellValidationProject/Assets"))
                throw new InvalidOperationException("Validação somente na cópia isolada InkwellValidationProject.");
            SafeEditorTools.Prepare();SafeEditorTools.VerifyAuthoring();
            var level=AssetDatabase.LoadAssetAtPath<LevelContainer>(InkwellLaboratory.LevelPath);
            var data=JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(level.levelData));
            data.fields=data.fields.Take(1).ToList();data.currentSublevelIndex=0;data.limit=50;data.selectedTutorial=0;
            var field=data.fields[0];Require(field.maxCols>=6 && field.maxRows>=5,"Fixture pequena demais.");
            for(int row=0;row<field.maxRows;row++)for(int col=0;col<field.maxCols;col++)
            {
                var cell=field.levelSquares[row*field.maxCols+col];
                cell.blocks.Clear();cell.blocks.Add(new SquareTypeLayer{squareType=SquareTypes.EmptySquare});
                cell.block=SquareTypes.EmptySquare;cell.obstacle=SquareTypes.NONE;cell.blockLayer=cell.obstacleLayer=1;
                cell.spawners.Clear();cell.startWithoutItem=false;cell.isEnterTeleport=false;
                cell.teleportCoordinatesLinked=cell.teleportCoordinatesLinkedBack=new Vector2Int(-1,-1);
                cell.separatorIndexes=new bool[4];cell.position=new Vector2Int(col,row);cell.direction=Vector2.down;cell.enterSquare=row==0;
                cell.item=new ItemForEditor {Item=Resources.Load<GameObject>("Items/Item"),ItemType=ItemsTypes.NONE,Color=(row+2*col)%4};
            }
            Require(SafeFootprint.TryPaint(field,2,2,out _),"Primeiro cofre não pintou.");
            Require(SafeFootprint.TryPaint(field,4,2,out _),"Segundo cofre não pintou.");
            field.levelSquares[2*field.maxCols].item=new ItemForEditor {Item=Resources.Load<GameObject>("Items/HORIZONTAL_STRIPED"),ItemType=ItemsTypes.HORIZONTAL_STRIPED,Color=0};
            data.LoadTargetObject();int index=data.GetTargetIndex("Safe");data.SetTarget(index);
            level.levelData=data;EditorUtility.SetDirty(level);AssetDatabase.SaveAssetIfDirty(level);
            var target=AssetDatabase.LoadAssetAtPath<TargetLevel>(InkwellLaboratory.TargetPath);
            target.targets=new TargetList{new TargetObject {targetType=new TargetType{type=index},
                sprites=new SpriteList{new SpriteObject{icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/Obstacles/Safe/SafeState1.asset"),uiSprite=true}},
                CountDrawer=new Count{count=2},ShowTheScoreForStar=new BoolScoreShow()}};
            EditorUtility.SetDirty(target);AssetDatabase.SaveAssetIfDirty(target);
            foreach(string key in LightBallLaboratory.OtherKeys)SessionState.SetBool(key,false);
            LightBallLaboratory.Disable();SessionState.SetBool(InkwellLaboratory.EnabledKey,true);
            SessionState.SetString(Key+".Deadline",DateTime.UtcNow.AddMinutes(4).ToString("O"));SessionState.SetBool(Key,true);
            EditorSceneManager.OpenScene("Assets/SweetSugar/Scenes/game.unity");Application.runInBackground=true;
            step=0;at=0;EditorApplication.isPlaying=true;
        }
        static int Goal(LevelManager manager)=>manager.levelData.TargetCounters.Single(t=>t.targetLevel.name=="Safe").count;
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false) || EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            try
            {
                Require(DateTime.UtcNow<DateTime.Parse(SessionState.GetString(Key+".Deadline",""),System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind),"Timeout da validação.");
                var manager=LevelManager.THIS;
                if(!EditorApplication.isPlaying || manager==null)return;
                if(manager.gameStatus==GameState.Map && AI.THIS!=null){manager.BoostPackage=manager.BoostColorfullBomb=manager.BoostStriped=0;manager.gameStatus=GameState.PrepareGame;}
                if(manager.gameStatus==GameState.Tutorial)manager.gameStatus=GameState.Playing;
                if(!InkwellLaboratory.IsActive || manager.gameStatus!=GameState.Playing || EditorApplication.timeSinceStartup<at)return;
                if(step==0)
                {
                    var safes=manager.field.GetComponentsInChildren<SafeCell>().OrderBy(s=>s.GetComponent<Square>().mainSquare.col).ToArray();
                    Require(safes.Length==2,"Não foram criados exatamente dois objetos cofre.");
                    first=safes[0];second=safes[1];first.EnsureController();second.EnsureController();
                    var layer=first.GetComponent<Square>();
                    footprint=manager.field.GetSquares().Where(s=>s.subSquares.Contains(layer)).ToArray();
                    Require(footprint.Length==4 && footprint.All(s=>!s.IsFree() && s.Item==null),"Cofre não bloqueia quatro células vazias.");
                    Require(Goal(manager)==2,"Meta contou células ou duplicou na instanciação.");
                    float pitch=Vector3.Distance(manager.field.GetSquare(0,0).transform.position,manager.field.GetSquare(1,0).transform.position);
                    var renderer=first.Controller.GetComponent<RoyalAves.Effects.SafeBreakAnimation>().View;
                    Require(renderer.enabled && Mathf.Abs(renderer.bounds.size.x/pitch-1.92f)<.03f,"Tamanho do visual não corresponde ao 2×2.");
                    InkwellLaboratoryTools.Capture("Logs/safe-board-initial.png");
                    manager.field.GetSquare(1,2).DestroyBlock();
                    Require(first.Controller.Hits==0 && second.Controller.Hits==0,"Match vizinho danificou cofre.");
                    step=1;at=EditorApplication.timeSinceStartup+.4;return;
                }
                if(step==1)
                {
                    var rocket=manager.field.GetSquare(0,2).Item;
                    Require(rocket!=null && rocket.currentType==ItemsTypes.HORIZONTAL_STRIPED,"Foguete real ausente.");
                    rocket.DestroyItem();step=2;at=EditorApplication.timeSinceStartup+2;return;
                }
                if(step==2)
                {
                    Require(first.Controller.Hits==1 && second.Controller.Hits==1,"Foguete não aplicou um acerto por cofre: "+first.Controller.Hits+","+second.Controller.Hits);
                    Require(Goal(manager)==2,"Dano intermediário contou meta.");
                    InkwellLaboratoryTools.Capture("Logs/safe-board-after-rocket.png");expected=2;step=3;
                }
                if(step==3)
                {
                    footprint[(expected-2)%4].DestroyBlock();
                    Require(first.Controller.Hits==expected,"Quadrante não compartilha resistência.");
                    footprint[(expected-1)%4].DestroyBlock();
                    Require(first.Controller.Hits==expected,"Dano duplicado entre quadrantes.");
                    Require(Goal(manager)==2,"Meta contou antes da quebra final.");
                    if(expected++<5){at=EditorApplication.timeSinceStartup+.4;return;}
                    step=4;at=EditorApplication.timeSinceStartup+.5;return;
                }
                if(step==4)
                {
                    Require(footprint.All(s=>s.IsFree() && s.GetSubSquare().GetComponent<SafeCell>()==null),"As quatro células não foram liberadas juntas.");
                    Require(Goal(manager)==1 && second.Controller.Hits==1,"Quebra duplicou meta ou danificou vizinho.");
                    InkwellLaboratoryTools.Capture("Logs/safe-board-broken.png");step=5;at=EditorApplication.timeSinceStartup+3;return;
                }
                if(step==5)
                {
                    Require(Goal(manager)==1 && manager.field.GetComponentsInChildren<SafeCell>().Length==1,"Estado final inconsistente.");
                    Require(!manager.animateItems.Any(i=>i!=null && i.gameObject.name=="Safe_2x2"),"Bloqueio de animação não foi limpo.");
                    File.WriteAllText("Logs/safe-board-verification.result","PASS: dois cofres encostados; um objeto e uma resistência por grupo 2×2; quatro células bloqueadas sem peças; visual com 1,92 células; meta inicial 2; match encaminhado sem dano; foguete real atinge cada cofre uma vez; demais acertos diagnósticos por quatro quadrantes; antirrepetição; meta só na quebra; quatro células liberadas juntas; vizinho preservado; bloqueio de animação limpo. Sem teste de ponteiro físico, partida completa, áudio ou dispositivo. Fixture e preferências isoladas.");
                    SessionState.SetBool(Key,false);EditorApplication.Exit(0);
                }
            }
            catch(Exception e)
            {
                File.WriteAllText("Logs/safe-board-verification.result","FAIL: "+e);SessionState.SetBool(Key,false);Debug.LogException(e);EditorApplication.Exit(1);
            }
        }
        static void Require(bool result,string message){if(!result)throw new InvalidOperationException(message);}
    }
}
