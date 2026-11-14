using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.Gameplay;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class SortingSignalNativeVerification
    {
        const string Key="MagicMail.SortingSignalVerification";
        static int Step {get=>SessionState.GetInt(Key+".Step",0);set=>SessionState.SetInt(Key+".Step",value);}
        static double at;
        static SortingSignalCell first;
        static int moves;
        [InitializeOnLoadMethod] static void Listen(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;}
        public static void Run()
        {
            if(!Application.dataPath.Replace('\\','/').Contains("/Logs/InkwellValidationProject/Assets"))throw new InvalidOperationException("Somente cópia isolada.");
            SortingSignalLaboratoryTools.Prepare();
            Require(Resources.Load<GameObject>("Blocks/SolidBlock").GetComponent<SortingSignalCell>()==null,"Caixa foi substituída na produção.");
            SessionState.SetBool(Key,true);Step=0;
            SessionState.SetString(Key+".Deadline",DateTime.UtcNow.AddMinutes(5).ToString("O"));
            SortingSignalLaboratoryTools.Open();
        }
        static int Goal()=>LevelManager.THIS.levelData.TargetCounters.Single(t=>t.targetLevel.name=="SortingSignals").count;
        static SortingSignalCell[] Signals()=>UnityEngine.Object.FindObjectsByType<SortingSignalCell>(FindObjectsSortMode.None);
        static Square Cell(SortingSignalCell signal)=>signal.transform.parent.GetComponent<Square>();
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false) || EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            try
            {
                Require(DateTime.UtcNow<DateTime.Parse(SessionState.GetString(Key+".Deadline",""),System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind),"Timeout da verificação do sinalizador.");
                if(!EditorApplication.isPlaying || !SortingSignalLaboratory.IsActive || LevelManager.THIS==null ||
                    LevelManager.THIS.gameStatus!=GameState.Playing || EditorApplication.timeSinceStartup<at)return;
                var manager=LevelManager.THIS;
                if(Step==0)
                {
                    var signals=Signals();Require(signals.Length==3 && signals.All(s=>s.Hits==0 && s.ColorIndex==0),"Estado inicial incorreto.");
                    Require(Goal()==3,"Meta inicial deve ser três sinalizadores.");
                    first=signals.Single(s=>Cell(s).col==3 && Cell(s).row==2);moves=manager.moveID;
                    var neighbor=manager.field.GetSquare(3,1);int original=neighbor.Item.color;
                    neighbor.Item.colorableComponent.SetColor(first.requestedColors[1]);
                    Cell(first).DestroyBlock(impactOrigin:neighbor);Require(first.Hits==0,"Cor errada causou dano.");
                    neighbor.Item.colorableComponent.SetColor(original);
                    InkwellLaboratoryTools.Capture("Logs/sorting-signal-initial.png");ScreenCapture.CaptureScreenshot("Logs/sorting-signal-full-initial.png");
                    manager.field.GetSquare(1,2).Item.SwitchDirection(Vector3.left);
                    Step=1;at=0;return;
                }
                if(Step==1)
                {
                    if(first.Hits==0)return;
                    Require(first.Hits==1,"Uma combinação danificou mais de uma vez.");
                    if(manager.findMatchesStarted || manager.DragBlocked || first.ColorIndex==0)return;
                    Require(first.ColorIndex==1 && Signals().All(s=>s.ColorIndex==1),"Cor não mudou uma vez no fim da jogada.");
                    Require(manager.moveID==moves+1 && Goal()==3,"Troca/meta incorretas.");
                    InkwellLaboratoryTools.Capture("Logs/sorting-signal-red.png");ScreenCapture.CaptureScreenshot("Logs/sorting-signal-full-red.png");
                    var rocket=manager.field.GetSquare(0,5).Item;
                    Require(rocket!=null && rocket.currentType==SweetSugar.Scripts.Items.ItemsTypes.HORIZONTAL_STRIPED,"Foguete preparado ausente.");
                    typeof(LevelManager).GetMethod("ActivateTappedBonus",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(manager,new object[]{rocket});
                    Step=2;at=EditorApplication.timeSinceStartup+2;return;
                }
                if(Step==2)
                {
                    if(manager.findMatchesStarted || manager.DragBlocked || first.ColorIndex==1)return;
                    Require(Signals().All(s=>s.Hits==1 && s.ColorIndex==2),"Foguete/ciclo não correspondem a um acerto por alvo e uma cor por turno.");
                    Require(Goal()==3,"Dano parcial reduziu meta.");
                    InkwellLaboratoryTools.Capture("Logs/sorting-signal-purple.png");
                    Cell(first).DestroyBlock();Cell(first).DestroyBlock();Require(first.Hits==2,"Antirrepetição do especial falhou.");
                    Step=3;at=EditorApplication.timeSinceStartup+.3;return;
                }
                if(Step==3)
                {
                    Cell(first).DestroyBlock();Require(first.Hits==3 && Goal()==3,"Meta baixou antes da quebra.");
                    Step=4;at=EditorApplication.timeSinceStartup+.5;return;
                }
                if(Step==4)
                {
                    Require(Goal()==2 && Signals().Length==2,"Quebra não contou uma unidade.");
                    InkwellLaboratoryTools.Capture("Logs/sorting-signal-broken.png");
                    foreach(var signal in Signals())Cell(signal).DestroyBlock();Step=5;at=EditorApplication.timeSinceStartup+.3;return;
                }
                if(Step==5)
                {
                    foreach(var signal in Signals())Cell(signal).DestroyBlock();Step=6;at=EditorApplication.timeSinceStartup+.6;return;
                }
                if(Step==6)
                {
                    Require(Goal()==0 && Signals().Length==0,"Meta final/remoção incorretas.");
                    manager.CheckWinLose();Require(manager.gameStatus==GameState.Playing,"Laboratório entrou em resultado de campanha.");
                    Step=51;SortingSignalLaboratoryTools.Open();return;
                }
                if(Step==51)
                {
                    if(Signals().Length!=3)return;
                    Require(Signals().All(s=>s.Hits==0 && s.ColorIndex==0) && Goal()==3,"Reinício não restaurou cor, vida e meta.");
                    ScreenCapture.CaptureScreenshot("Logs/sorting-signal-full-restarted.png");
                    File.WriteAllText("Logs/sorting-signal-native.result","PASS: master original escolhido; três objetos/células; meta própria 3; cor errada rejeitada; troca real via API forma match e causa um dano; ciclo de cor após resolver turno; foguete real pelo ativador de toque via API atinge dois alvos uma vez; três acertos; antirrepetição; quebra/meta unitária; conclusão sem resultado/recompensa; reinício real restaura tudo. Capturas nativas. Não é ponteiro físico, áudio, aparelho ou campanha completa.");
                    SessionState.SetBool(Key,false);EditorApplication.Exit(0);
                }
            }
            catch(Exception e){File.WriteAllText("Logs/sorting-signal-native.result","FAIL: "+e);Debug.LogException(e);SessionState.SetBool(Key,false);EditorApplication.Exit(1);}
        }
        static void Require(bool result,string message){if(!result)throw new InvalidOperationException(message);}
    }
}
