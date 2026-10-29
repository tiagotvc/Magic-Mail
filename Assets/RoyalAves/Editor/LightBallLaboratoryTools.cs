using System;
using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Items._Interfaces;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.System;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public sealed class LightBallLaboratoryTools : EditorWindow
    {
        const string Menu="Royal Aves/Efeitos/Light Ball/";
        const string Request="Logs/lightball-lab.request";
        const string StartKey="MagicMail.LightBallLab.Start";
        const string SmokeKey="MagicMail.LightBallLab.Smoke";
        static double deadline,readyAt;
        static bool ready,flowCaptured,collectionCaptured;
        static int smokeStage,initialGoal;
        static ItemMulticolor smokeSource;
        static float speed=1;
        static int viewportWidth,viewportHeight;
        static double nextStatus;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            deadline=EditorApplication.timeSinceStartup+120;
            EditorApplication.update-=Poll;EditorApplication.update+=Poll;
            EditorApplication.playModeStateChanged-=PlayModeChanged;EditorApplication.playModeStateChanged+=PlayModeChanged;
        }
        static void PlayModeChanged(PlayModeStateChange state)
        {if(state==PlayModeStateChange.ExitingPlayMode){Time.timeScale=1;ready=false;}}

        [MenuItem(Menu+"Laboratório no jogo")]
        public static void Open() {Directory.CreateDirectory("Logs");File.WriteAllText(Request,"open");}
        [MenuItem(Menu+"Painel de testes")]
        public static void Panel() => GetWindow<LightBallLaboratoryTools>("Light Ball");
        [MenuItem(Menu+"Desativar laboratório")]
        public static void Disable()
        {
            LightBallLaboratory.Disable();RestoreGameView();SessionState.SetBool(StartKey,false);SessionState.SetBool(SmokeKey,false);
            Time.timeScale=1;if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;
        }
        [MenuItem(Menu+"Preparar laboratório")]
        public static void Prepare()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Pare o Play antes de preparar.");
            Directory.CreateDirectory("Assets/RoyalAves/Laboratory");
            var template=AssetDatabase.LoadAssetAtPath<LevelContainer>(FlyingLetterLaboratory.LevelPath);
            if(template==null)throw new InvalidOperationException("Falta o modelo 7×7 do laboratório.");
            var level=AssetDatabase.LoadAssetAtPath<LevelContainer>(LightBallLaboratory.LevelPath);
            if(level==null){level=CreateInstance<LevelContainer>();AssetDatabase.CreateAsset(level,LightBallLaboratory.LevelPath);}
            level.levelData=JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(template.levelData));
            var data=level.levelData;data.fields=data.fields.Take(1).ToList();data.currentSublevelIndex=0;
            data.levelNum=1;data.selectedTutorial=0;data.limit=40;data.limitType=LIMIT.MOVES;
            data.colorLimit=4;data.pieceColors=new System.Collections.Generic.List<int>{0,1,2,3};
            var field=data.fields[0];
            if(field.maxRows!=7 || field.maxCols!=7)throw new InvalidOperationException("Modelo deve ser 7×7.");
            field.layers=1;field.frankingMachines.Clear();
            var normal=Resources.Load<GameObject>("Items/Item");
            for(int y=0;y<7;y++)for(int x=0;x<7;x++)
            {
                var cell=field.levelSquares[y*7+x];
                cell.blocks.Clear();cell.blocks.Add(new SquareTypeLayer{squareType=SquareTypes.EmptySquare});
                cell.block=SquareTypes.EmptySquare;cell.blockLayer=1;cell.obstacle=SquareTypes.NONE;cell.obstacleLayer=1;
                cell.spawners.Clear();cell.startWithoutItem=false;cell.direction=Vector2.down;cell.enterSquare=y==0;
                cell.isEnterTeleport=false;cell.teleportCoordinatesLinked=cell.teleportCoordinatesLinkedBack=new Vector2Int(-1,-1);
                cell.separatorIndexes=new bool[4];cell.position=new Vector2Int(x,y);
                cell.item=new ItemForEditor{Item=normal,ItemType=ItemsTypes.NONE,Color=(y+2*x)%4};
            }
            field.levelSquares[3*7+3].item=new ItemForEditor{Item=Resources.Load<GameObject>("Items/MULTICOLOR"),ItemType=ItemsTypes.MULTICOLOR,Color=0};
            field.levelSquares[3*7+4].item=new ItemForEditor{Item=normal,ItemType=ItemsTypes.NONE,Color=0};
            data.LoadTargetObject();int targetIndex=data.GetTargetIndex("CollectItems");data.SetTarget(targetIndex);
            foreach(var mode in Enumerable.Range(0,5))
            {
                var type=LightBallLaboratory.PartnerType(mode);
                if(Resources.Load<GameObject>(type==ItemsTypes.NONE?"Items/Item":"Items/"+type)==null)
                    throw new InvalidOperationException("Prefab ausente para "+LightBallLaboratory.Modes[mode]);
            }
            var sprites=normal.GetComponent<IColorableComponent>().GetSprites(1);
            var targets=AssetDatabase.LoadAssetAtPath<TargetLevel>(LightBallLaboratory.TargetPath);
            if(targets==null){targets=CreateInstance<TargetLevel>();AssetDatabase.CreateAsset(targets,LightBallLaboratory.TargetPath);}
            targets.targets=new TargetList();
            foreach(int color in new[]{0,1}) targets.targets.Add(new TargetObject{targetType=new TargetType{type=targetIndex},
                sprites=new SpriteList{new SpriteObject{icon=sprites[color],uiSprite=true}},CountDrawer=new Count{count=20},ShowTheScoreForStar=new BoolScoreShow()});
            EditorUtility.SetDirty(level);EditorUtility.SetDirty(targets);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/lightball-lab-prepare.result","READY: 7x7, 40 movimentos, metas 20+20, Light Ball central e cinco parceiros; assets de campanha preservados.");
        }
        static void Poll()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            try
            {
                if(File.Exists(Request))
                {
                    if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}
                    if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                    string command=File.ReadAllText(Request).Trim();File.Delete(Request);
                    if(File.Exists("Logs/lightball-lab-error.result"))File.Delete("Logs/lightball-lab-error.result");
                    if(command=="prepare"){Prepare();return;}
                    if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                    Prepare();foreach(string key in LightBallLaboratory.OtherKeys)SessionState.SetBool(key,false);
                    if(command=="smoke")SessionState.SetInt(LightBallLaboratory.ModeKey,0);
                    SessionState.SetBool(LightBallLaboratory.EnabledKey,true);SessionState.SetBool(StartKey,true);
                    SessionState.SetBool(SmokeKey,command=="smoke");
                    smokeStage=0;flowCaptured=collectionCaptured=false;ready=false;viewportWidth=viewportHeight=0;speed=1;deadline=EditorApplication.timeSinceStartup+90;
                    EditorSceneManager.OpenScene("Assets/SweetSugar/Scenes/game.unity");Panel();
                    ConfigureGameView();
                    EditorApplication.isPlaying=true;return;
                }
                if(!EditorApplication.isPlaying || !SessionState.GetBool(LightBallLaboratory.EnabledKey,false))return;
                var manager=LevelManager.THIS;if(manager==null)return;
                if(SessionState.GetBool(StartKey,false) && manager.gameStatus==GameState.Map && AI.THIS!=null)
                {
                    Application.runInBackground=true;Time.timeScale=1;readyAt=EditorApplication.timeSinceStartup+1;
                    manager.BoostPackage=manager.BoostColorfullBomb=manager.BoostStriped=0;
                    manager.gameStatus=GameState.PrepareGame;
                }
                if(!LightBallLaboratory.IsActive)return;
                // Keep the designed board, then use the normal camera/PrePlay/HUD route.
                if(SessionState.GetBool(StartKey,false) && manager.gameStatus==GameState.PrepareGame &&
                    EditorApplication.timeSinceStartup>readyAt && manager.field!=null && manager.field.squaresArray.All(c=>c!=null))
                {
                    manager.field.StopAllCoroutines();
                    foreach(var cell in manager.field.GetSquares())
                        if(cell.Item!=null)
                        {
                            cell.Item.Hide(false);
                            if(cell.Item.currentType==ItemsTypes.NONE)cell.Item.colorableComponent.SetColor((cell.row+2*cell.col)%4);
                        }
                    var partner=manager.field.GetSquare(4,3,true).Item;
                    if(partner!=null && partner.currentType==ItemsTypes.NONE)partner.colorableComponent.SetColor(0);
                    SyncGoalSprites(manager);manager.gameStatus=GameState.WaitForPopup;
                }
                if(manager.gameStatus==GameState.Tutorial)manager.gameStatus=GameState.Playing;
                if(SessionState.GetBool(StartKey,false) && manager.gameStatus==GameState.Playing)
                {
                    SessionState.SetBool(StartKey,false);ready=true;readyAt=EditorApplication.timeSinceStartup+.5;
                    foreach(var boost in manager.InGameBoosts)if(boost!=null)boost.gameObject.SetActive(false);
                    File.WriteAllText("Logs/lightball-lab-ready.result","READY: bancada interativa em Playing, coleta real, boosters pagos ocultos, sem premiacao de campanha.");
                }
                if(ready && (viewportWidth!=Screen.width || viewportHeight!=Screen.height))FitViewport(manager);
                if(ready && EditorApplication.timeSinceStartup>nextStatus)
                {
                    nextStatus=EditorApplication.timeSinceStartup+1;
                    var camera=manager.GetComponent<Camera>();
                    var center=manager.field.GetSquare(3,3,true);
                    var report=new System.Text.StringBuilder();
                    report.AppendLine($"state={manager.gameStatus}; cam={camera.transform.position}; size={camera.orthographicSize}; screen={Screen.width}x{Screen.height}; center={center.transform.position}; viewport={camera.WorldToViewportPoint(center.transform.position)}");
                    foreach(var goal in manager.levelData.TargetCounters)report.AppendLine($"goal={goal.count}; sprite={goal.extraObject?.name}; accepted={string.Join(",",goal.extraObjects.Select(x=>x.name))}");
                    foreach(var item in manager.field.GetItems().Take(5))report.AppendLine($"item={item.currentType}; color={item.color}; active={item.gameObject.activeSelf}; sprite={item.GetSpriteRenderer()?.sprite?.name}; renderer={item.GetSpriteRenderer()?.enabled}; target={item.HasActiveCollectionTarget()}");
                    File.WriteAllText("Logs/lightball-lab-status.result",report.ToString());
                }
                if(SessionState.GetBool(SmokeKey,false))Smoke(manager);
            }
            catch(Exception error)
            {
                SessionState.SetBool(SmokeKey,false);SessionState.SetBool(StartKey,false);
                File.WriteAllText("Logs/lightball-lab-error.result",error.ToString());Debug.LogException(error);
            }
        }
        static object SizeGroup()
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;
            var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
            var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance=singleton.GetProperty("instance",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.FlattenHierarchy).GetValue(null);
            var groupType=assembly.GetType("UnityEditor.GameViewSizeGroupType");
            return sizesType.GetMethod("GetGroup").Invoke(instance,new[]{Enum.Parse(groupType,"Standalone")});
        }
        static void ConfigureGameView()
        {
            var assembly=typeof(UnityEditor.Editor).Assembly;
            var view=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            var selection=view.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
            if(!SessionState.GetBool("MagicMail.LightBallLab.ViewSaved",false))
            {
                SessionState.SetInt("MagicMail.LightBallLab.PreviousSize",(int)selection.GetValue(view));
                SessionState.SetBool("MagicMail.LightBallLab.ViewSaved",true);
            }
            var group=SizeGroup();var type=group.GetType();
            int builtin=(int)type.GetMethod("GetBuiltinCount").Invoke(group,null);
            int custom=(int)type.GetMethod("GetCustomCount").Invoke(group,null);
            int index=-1;
            for(int i=0;i<builtin+custom;i++)
            {
                var size=type.GetMethod("GetGameViewSize").Invoke(group,new object[]{i});
                if((int)size.GetType().GetProperty("width").GetValue(size)==1440 && (int)size.GetType().GetProperty("height").GetValue(size)==2960){index=i;break;}
            }
            if(index<0)
            {
                var sizeType=assembly.GetType("UnityEditor.GameViewSize");
                var kind=assembly.GetType("UnityEditor.GameViewSizeType");
                var size=Activator.CreateInstance(sizeType,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic,null,
                    new object[]{Enum.Parse(kind,"FixedResolution"),1440,2960,"Light Ball lab"},null);
                type.GetMethod("AddCustomSize").Invoke(group,new[]{size});index=builtin+custom;
                SessionState.SetInt("MagicMail.LightBallLab.AddedSize",custom);
            }
            selection.SetValue(view,index);view.Focus();view.Repaint();
        }
        static void RestoreGameView()
        {
            if(!SessionState.GetBool("MagicMail.LightBallLab.ViewSaved",false))return;
            var view=EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
            var selection=view.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
            selection.SetValue(view,SessionState.GetInt("MagicMail.LightBallLab.PreviousSize",0));
            int added=SessionState.GetInt("MagicMail.LightBallLab.AddedSize",-1);
            if(added>=0)
            {
                var group=SizeGroup();var groupType=group.GetType();
                int custom=(int)groupType.GetMethod("GetCustomCount").Invoke(group,null);
                if(added<custom)
                {
                    int builtin=(int)groupType.GetMethod("GetBuiltinCount").Invoke(group,null);
                    var size=groupType.GetMethod("GetGameViewSize").Invoke(group,new object[]{builtin+added});
                    if(size.GetType().GetProperty("baseText")?.GetValue(size)?.ToString()=="Light Ball lab")
                        groupType.GetMethod("RemoveCustomSize").Invoke(group,new object[]{added});
                }
                SessionState.EraseInt("MagicMail.LightBallLab.AddedSize");
            }
            SessionState.SetBool("MagicMail.LightBallLab.ViewSaved",false);
        }
        static void SyncGoalSprites(LevelManager manager)
        {
            // Resolve the actual pooled piece artwork, without editing global targets or prefabs.
            for(int color=0;color<2;color++)
            {
                var sample=manager.field.GetItems().First(i=>i.currentType==ItemsTypes.NONE && i.color==color);
                var goal=manager.levelData.TargetCounters[color];
                goal.extraObjects=sample.GetSpriteRenderers().Where(r=>r.sprite!=null).Select(r=>r.sprite).Distinct().ToArray();
                goal.extraObject=goal.extraObjects[0];
                foreach(var view in UnityEngine.Object.FindObjectsByType<TargetGUI>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    if(view.subTargetContainer==goal && view.image!=null)view.SetSprite(goal.extraObject);
            }
        }
        static void FitViewport(LevelManager manager)
        {
            var camera=manager.GetComponent<Camera>();
            foreach(var fitter in UnityEngine.Object.FindObjectsByType<SweetSugar.Scripts.System.Orientation.OrientationGameCameraHandle>(FindObjectsSortMode.None))
                if(fitter.mainCamera==camera){fitter.StopAllCoroutines();fitter.enabled=false;}
            var cells=manager.field.GetSquares().Where(c=>c!=null).ToArray();
            var bounds=new Bounds(cells[0].transform.position,Vector3.zero);
            foreach(var cell in cells)bounds.Encapsulate(cell.transform.position);
            camera.enabled=true;camera.orthographic=true;camera.rect=new Rect(0,0,1,1);
            camera.orthographicSize=Mathf.Max((bounds.size.y+manager.field.squareHeight)/1.12f,
                (bounds.size.x+manager.field.squareWidth)/(camera.aspect*1.9f));
            camera.transform.SetPositionAndRotation(new Vector3(bounds.center.x,bounds.center.y+camera.orthographicSize*.14f,bounds.center.z-20),Quaternion.identity);
            // The orientation HUD prefab has no authored camera. Bind the live laboratory
            // camera so its Canvas follows this Game View instead of stale display dimensions.
            foreach(var hud in UnityEngine.Object.FindObjectsByType<TargetGUI>(FindObjectsSortMode.None)
                .Where(g=>g.image!=null).Select(g=>g.image.GetComponentInParent<Canvas>()?.rootCanvas).Where(c=>c!=null).Distinct())
            {
                hud.renderMode=RenderMode.ScreenSpaceCamera;hud.worldCamera=camera;hud.planeDistance=100;hud.enabled=true;
                var scaler=hud.GetComponent<UnityEngine.UI.CanvasScaler>();
                if(scaler!=null){scaler.enabled=false;scaler.enabled=true;}
            }
            viewportWidth=Screen.width;viewportHeight=Screen.height;Canvas.ForceUpdateCanvases();
        }
        static void Smoke(LevelManager manager)
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout: "+manager.gameStatus);
            if(!ready || manager.field==null)return;
            void Require(bool condition,string message){if(!condition)throw new Exception(message);}
            int Count()=>manager.levelData.TargetCounters.Sum(t=>t.count);
            if(smokeStage==0)
            {
                if(EditorApplication.timeSinceStartup<readyAt)return;
                Canvas.ForceUpdateCanvases();
                Require(manager.field.squaresArray.Length==49 && Count()==40,"Grid/metas inesperados.");
                var goals=UnityEngine.Object.FindObjectsByType<TargetGUI>(FindObjectsSortMode.None).Where(g=>g.isActiveAndEnabled && g.image!=null && g.image.isActiveAndEnabled).ToArray();
                Require(goals.Length>=2 && goals.All(g=>g.image.GetComponentInParent<Canvas>().isActiveAndEnabled),"Metas não estão visíveis no HUD.");
                Require(goals.All(g=>g.image.depth>=0 && g.image.rectTransform.rect.width>0),"Metas sem área renderizada.");
                var screenReport=new System.Text.StringBuilder();
                foreach(var goal in goals)
                {
                    var canvas=goal.image.GetComponentInParent<Canvas>().rootCanvas;
                    var corners=new Vector3[4];goal.image.rectTransform.GetWorldCorners(corners);
                    var a=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,corners[0]);
                    var b=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,corners[2]);
                    screenReport.AppendLine($"{goal.image.sprite.name}: {a} -> {b}; canvas={canvas.name}");
                    File.WriteAllText("Logs/lightball-lab-hud.result",screenReport.ToString());
                    Require(a.x>=0 && b.x<=Screen.width && a.y>=0 && b.y<=Screen.height && Vector2.Distance(a,b)>8,"Meta fora da tela: "+screenReport);
                }
                smokeSource=manager.field.GetSquare(3,3,true).Item as ItemMulticolor;Require(smokeSource!=null,"Light Ball central ausente.");
                initialGoal=Count();Time.timeScale=.6f;ScreenCapture.CaptureScreenshot("Logs/lightball-lab-ready.png");
                var pair=manager.field.GetSquare(4,3,true).Item.SwitchDirection(Vector3.right);
                Require(pair.currentItem!=null && pair.neighborItem!=null,"Troca do parceiro rejeitada.");smokeStage=1;
            }
            else
            {
                if(!flowCaptured && smokeSource.MarkedCount>=4){flowCaptured=true;ScreenCapture.CaptureScreenshot("Logs/lightball-lab-spin.png");}
                var flights=UnityEngine.Object.FindObjectsByType<AnimateItems>(FindObjectsSortMode.None).Where(f=>f.IsCollectionFlight).ToArray();
                if(!collectionCaptured && flights.Any(f=>f.FlightProgress>.75f)){collectionCaptured=true;ScreenCapture.CaptureScreenshot("Logs/lightball-lab-collection.png");}
                if(smokeSource.Phase!=ItemMulticolor.SequencePhase.Finished || flights.Length>0)return;
                Require(Count()<initialGoal && flowCaptured,"Coleta nao concluiu: metas="+Count()+"/"+initialGoal+"; fluxo="+flowCaptured+"; voo="+collectionCaptured+"; alvos="+smokeSource.TargetCount);
                SessionState.SetBool(SmokeKey,false);
                File.WriteAllText("Logs/lightball-lab-smoke.result","PASS: 49 células, duas metas renderizadas, troca real por código, giro/marcação e voo ao HUD; meta diminuiu após coletas. Sem teste de ponteiro. Reabrindo bancada limpa.");Open();
            }
        }
        void OnInspectorUpdate()=>Repaint();
        void OnGUI()
        {
            GUILayout.Label("Light Ball — laboratório interativo",EditorStyles.boldLabel);
            int mode=SessionState.GetInt(LightBallLaboratory.ModeKey,0);
            int selected=EditorGUILayout.Popup("Cenário",mode,LightBallLaboratory.Modes);
            if(selected!=mode)SessionState.SetInt(LightBallLaboratory.ModeKey,selected);
            if(GUILayout.Button("Abrir / reiniciar cenário selecionado"))Open();
            if(GUILayout.Button("Desativar e voltar às fases normais"))Disable();
            EditorGUILayout.HelpBox("Arraste a Light Ball central para a peça à direita. Os cenários testam cor comum, dinamite, foguete, hélice e dupla multicolorida. As duas metas ficam no HUD. Trocar o cenário exige reiniciar.",MessageType.Info);
            bool active=EditorApplication.isPlaying && LightBallLaboratory.IsActive && ready && LevelManager.THIS?.field!=null;
            using(new EditorGUI.DisabledScope(!active))
            {
                float next=EditorGUILayout.Slider("Velocidade da simulação",speed,.2f,1.5f);
                if(!Mathf.Approximately(next,speed)){speed=next;Time.timeScale=speed;}
                if(GUILayout.Button("Restaurar velocidade normal")){speed=1;Time.timeScale=1;}
                var source=active?LevelManager.THIS.field.GetItems().OfType<ItemMulticolor>().FirstOrDefault(i=>i.Phase==ItemMulticolor.SequencePhase.Idle):null;
                using(new EditorGUI.DisabledScope(source==null || LevelManager.GetGameStatus()!=GameState.Playing))
                    if(GUILayout.Button("Ativar combo preparado — diagnóstico"))
                    {
                        var partner=source.square.GetNeighborRight()?.Item;
                        if(partner!=null)source.Check(source,partner);
                    }
                if(GUILayout.Button("Coletar uma peça de objetivo — diagnóstico") && active && LevelManager.GetGameStatus()==GameState.Playing)
                    LevelManager.THIS.field.GetItems().FirstOrDefault(i=>i.currentType==ItemsTypes.NONE && !i.IsMulticolorReserved && !i.destroying && i.HasActiveCollectionTarget())?.DestroyItem(true);
            }
            if(active)
            {
                var manager=LevelManager.THIS;
                EditorGUILayout.LabelField("Metas restantes",string.Join(" / ",manager.levelData.TargetCounters.Select(t=>t.count.ToString())));
                EditorGUILayout.LabelField("Voos ativos",UnityEngine.Object.FindObjectsByType<AnimateItems>(FindObjectsSortMode.None).Count(f=>f.IsCollectionFlight).ToString());
            }
            EditorGUILayout.HelpBox("Botões de diagnóstico não gastam boosters. Os atalhos pagos ficam ocultos nesta bancada. Vitória e derrota não premiam nem avançam a campanha; reinicie para repetir.",MessageType.None);
        }
    }
}
