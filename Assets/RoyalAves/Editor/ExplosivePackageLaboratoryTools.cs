using System;
using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RoyalAves.EditorTools
{
    public sealed class ExplosivePackageLaboratoryTools : EditorWindow
    {
        const string Menu="Royal Aves/Efeitos/Pacote explosivo/",Request="Logs/explosive-package-lab.request";
        const string Key=ExplosivePackageLaboratory.EnabledKey;
        static bool seenImported,seenParticles;
        static int coreSamples;
        static bool ready;static double began,preparedAt;static int width,height;
        static float speed=1,impact;static int blasts,hits;static bool testing;static string failure;
        [InitializeOnLoadMethod]static void Init()
        {
            began=EditorApplication.timeSinceStartup;
            EditorApplication.update-=Poll;EditorApplication.update+=Poll;
            EditorApplication.playModeStateChanged-=Changed;EditorApplication.playModeStateChanged+=Changed;
        }
        static void Changed(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.ExitingPlayMode){Time.timeScale=1;if(SessionState.GetBool(Key+".Sampling",false)){Time.maximumDeltaTime=SessionState.GetFloat(Key+".PreviousDelta",.3333333f);SessionState.SetBool(Key+".Sampling",false);}}
            if(state!=PlayModeStateChange.EnteredEditMode)return;
            ready=testing=false;ExplosivePackageLaboratory.ResetActive();
            if(SessionState.GetBool(Key+".View",false)){LightBallLaboratoryTools.RestoreGameView();SessionState.SetBool(Key+".View",false);}
        }
        [MenuItem(Menu+"Abrir laboratório")]
        public static void Open(){Directory.CreateDirectory("Logs");File.WriteAllText(Request,"open");}
        [MenuItem("Royal Aves/Efeitos/Foguete + helice/Abrir laboratorio horizontal")]
        public static void OpenRocketHorizontal(){SessionState.SetInt(ExplosivePackageLaboratory.ModeKey,6);Open();}
        [MenuItem("Royal Aves/Efeitos/Foguete + helice/Abrir laboratorio vertical")]
        public static void OpenRocketVertical(){SessionState.SetInt(ExplosivePackageLaboratory.ModeKey,7);Open();}
        [MenuItem("Royal Aves/Efeitos/Helices combinadas/Abrir laboratorio")]
        public static void OpenPropellerPair(){SessionState.SetInt(ExplosivePackageLaboratory.ModeKey,8);Open();}
        [MenuItem("Royal Aves/Efeitos/Helice + dinamite/Abrir laboratorio")]
        public static void OpenDynamitePropeller(){SessionState.SetInt(ExplosivePackageLaboratory.ModeKey,3);Open();}
        [MenuItem(Menu+"Painel de testes")]
        public static void Panel()=>GetWindow<ExplosivePackageLaboratoryTools>("Pacote explosivo");
        [MenuItem(Menu+"Desativar laboratório")]
        public static void Disable()
        {
            ExplosivePackageLaboratory.Disable();SessionState.SetBool(Key+".Start",false);SessionState.SetBool(Key+".Smoke",false);
            Time.timeScale=1;if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;
        }
        static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        static Item Source()=>LevelManager.THIS?.field?.GetSquare(SessionState.GetInt(ExplosivePackageLaboratory.ModeKey,0)>0?5:4,4,true)?.Item;
        static bool CanActivate()=>ready && LevelManager.THIS!=null && LevelManager.GetGameStatus()==GameState.Playing && !LevelManager.THIS.StopFall && !LevelManager.THIS.DragBlocked && Source()?.currentType==ExplosivePackageLaboratory.PartnerType(SessionState.GetInt(ExplosivePackageLaboratory.ModeKey,0)) && !Source().destroying;
        static void Activate()
        {
            if(!CanActivate())return;
            if(SessionState.GetInt(ExplosivePackageLaboratory.ModeKey,0)>0)Source().SwitchDirection(Vector3.right);
            else Source().ActivateFromSwap();
        }
        static void Blast(Square cell,int radius,bool combined){if(testing){blasts++;impact=Time.time;ScreenCapture.CaptureScreenshot("Logs/explosive-package-imported-start.png");if(radius!=(combined?cell.field.squaresArray.Where(s=>s!=null).Max(s=>Mathf.Max(Mathf.Abs(s.col-cell.col),Mathf.Abs(s.row-cell.row))):ItemDestroyAnimation.BlastRadius)||combined!=(SessionState.GetInt(ExplosivePackageLaboratory.ModeKey,0)==1))failure="Alcance incorreto.";}}
        static void Hit(Item item){if(testing&&item.currentType==ItemsTypes.NONE&&(blasts==0||LevelManager.THIS.StopFall)){if(blasts==0)failure="Dano antes do estouro.";hits++;}}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            try
            {
                if(File.Exists(Request))
                {
                    if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}
                    if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                    string command;try{command=File.ReadAllText(Request).Trim();}catch(IOException){return;}
                    File.Delete(Request);
                    if(command=="dynamite-propeller")SessionState.SetInt(ExplosivePackageLaboratory.ModeKey,3);
                    if(command=="propeller-pair")SessionState.SetInt(ExplosivePackageLaboratory.ModeKey,8);
                    if(command=="rocket-horizontal" || command=="rocket-vertical")
                        SessionState.SetInt(ExplosivePackageLaboratory.ModeKey,command=="rocket-horizontal"?6:7);
                    File.WriteAllText("Logs/rocket-propeller-request.result","command="+command+"; mode="+SessionState.GetInt(ExplosivePackageLaboratory.ModeKey,0));
                    const string scene="Assets/SweetSugar/Scenes/game.unity";
                    if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=scene)
                    {if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene(scene);}
                    Require(AssetDatabase.LoadAssetAtPath<SweetSugar.Scripts.Level.LevelContainer>(LightBallLaboratory.LevelPath)!=null,"Modelo do laboratório ausente.");
                    LightBallLaboratory.Disable();foreach(var key in LightBallLaboratory.OtherKeys)SessionState.SetBool(key,false);
                    if(command=="smoke"){SessionState.SetBool(Key+".Smoke",true);SessionState.SetInt(ExplosivePackageLaboratory.ModeKey,0);File.WriteAllText("Logs/explosive-package-lab-smoke.result","RUNNING\n");}
                    SessionState.SetBool(Key,true);SessionState.SetBool(Key+".Start",true);ready=false;speed=1;width=height=0;
                    began=EditorApplication.timeSinceStartup;LightBallLaboratoryTools.ConfigureGameView();SessionState.SetBool(Key+".View",true);Panel();
                    EditorApplication.isPlaying=true;return;
                }
                if(!EditorApplication.isPlaying||!SessionState.GetBool(Key,false))return;
                EditorApplication.QueuePlayerLoopUpdate();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                var manager=LevelManager.THIS;if(manager==null)return;
                bool starting=SessionState.GetBool(Key+".Start",false);
                if(starting&&manager.gameStatus==GameState.Map&&AI.THIS!=null)
                {Application.runInBackground=true;Time.timeScale=1;manager.BoostPackage=manager.BoostColorfullBomb=manager.BoostStriped=0;preparedAt=EditorApplication.timeSinceStartup+1;manager.gameStatus=GameState.PrepareGame;}
                if(!ExplosivePackageLaboratory.IsActive)return;
                if(starting&&manager.gameStatus==GameState.PrepareGame&&EditorApplication.timeSinceStartup>preparedAt&&manager.field!=null&&manager.field.squaresArray.All(c=>c!=null))
                {
                    manager.field.StopAllCoroutines();foreach(var item in manager.field.GetItems()){item.Hide(false);if(item.currentType==ItemsTypes.NONE)item.color=(item.square.row+2*item.square.col)%4;}
                    LightBallLaboratoryTools.SyncGoalSprites(manager);manager.gameStatus=GameState.WaitForPopup;
                }
                if(manager.gameStatus==GameState.Tutorial)manager.gameStatus=GameState.Playing;
                if(starting&&manager.gameStatus==GameState.Playing)
                {
                    SessionState.SetBool(Key+".Start",false);ready=true;began=EditorApplication.timeSinceStartup;
                    foreach(var boost in manager.InGameBoosts)if(boost!=null)boost.gameObject.SetActive(false);
                    File.WriteAllText("Logs/explosive-package-lab-ready.result","READY: "+ExplosivePackageLaboratory.Modes[SessionState.GetInt(ExplosivePackageLaboratory.ModeKey,0)]+"; 9x9; real Playing; paid boosts hidden; campaign results disabled.");
                }
                if(ready&&(width!=Screen.width||height!=Screen.height)){LightBallLaboratoryTools.FitViewport(manager);width=Screen.width;height=Screen.height;}
                if(SessionState.GetBool(Key+".Smoke",false))Smoke(manager);
                if(starting&&EditorApplication.timeSinceStartup-began>100)throw new Exception("Timeout ao abrir laboratório: "+manager.gameStatus);
            }
            catch(Exception error){SessionState.SetBool(Key+".Smoke",false);SessionState.SetBool(Key+".Start",false);File.WriteAllText("Logs/explosive-package-lab-error.result",error.ToString());Debug.LogException(error);}
        }
        static void Smoke(LevelManager manager)
        {
            int mode=SessionState.GetInt(ExplosivePackageLaboratory.ModeKey,0);
            Require(mode<=1,"Smoke antigo cobre apenas simples e dupla; use a verificacao de combinacoes para os outros modos.");
            if(!testing)
            {
                if(!CanActivate()||EditorApplication.timeSinceStartup-began<.5)return;
                Require(manager.field.squaresArray.Length==81,"Tabuleiro não é 9x9.");
                Require(manager.field.GetItems().Count(i=>i.currentType==ItemsTypes.PACKAGE)==(mode==0?1:2),"Pacotes iniciais incorretos.");
                Require(manager.InGameBoosts.All(b=>b==null||!b.gameObject.activeInHierarchy),"Boosters pagos visíveis.");
                Require(UnityEngine.Object.FindObjectsByType<SweetSugar.Scripts.TargetScripts.TargetSystem.TargetGUI>().Any(g=>g.isActiveAndEnabled&&g.image!=null&&g.image.isActiveAndEnabled&&g.image.depth>=0),"HUD de metas invisível.");
                ItemDestroyAnimation.BlastApplied-=Blast;ItemDestroyAnimation.BlastApplied+=Blast;ItemMulticolor.ActualExplosion-=Hit;ItemMulticolor.ActualExplosion+=Hit;
                coreSamples=0;seenImported=seenParticles=false;SessionState.SetFloat(Key+".PreviousDelta",Time.maximumDeltaTime);SessionState.SetBool(Key+".Sampling",true);blasts=hits=0;impact=0;failure=null;testing=true;began=EditorApplication.timeSinceStartup;
                ScreenCapture.CaptureScreenshot("Logs/explosive-package-lab-mode-"+mode+".png");Activate();return;
            }
            Require(failure==null,failure);
            if(blasts>0&&GameObject.Find("ExplosivePackageBlast(Clone)") is GameObject liveEffect)
            {
                if(liveEffect.GetComponentsInChildren<ParticleSystem>().Any(p=>p.name=="FireBall"&&p.particleCount>0))coreSamples++;
                seenImported=true;seenParticles|=liveEffect.GetComponentsInChildren<ParticleSystem>().Any(p=>p.particleCount>0);
                var system=liveEffect.GetComponentInChildren<ParticleSystem>();
                if(system!=null && system.time>.25f && system.time<.4f)
                {
                    ScreenCapture.CaptureScreenshot("Logs/explosion-tif-peak-"+mode+".png");
                    var mesh=new Mesh();system.GetComponent<ParticleSystemRenderer>().BakeMesh(mesh,manager.GetComponent<Camera>(),true);
                    var uv0=new System.Collections.Generic.List<Vector4>();var uv1=new System.Collections.Generic.List<Vector4>();mesh.GetUVs(0,uv0);mesh.GetUVs(1,uv1);
                    File.WriteAllText("Logs/explosion-flipbook-uv.result","uv0="+string.Join(";",uv0.Take(4))+"\nuv1="+string.Join(";",uv1.Take(4)));
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
                if(system!=null && system.time>.65f && system.time<.8f)ScreenCapture.CaptureScreenshot("Logs/explosion-tif-smoke-"+mode+".png");
                if(Time.time-impact<.4f)File.AppendAllText("Logs/package-explosion-live.result",$"t={Time.time-impact:F3},dt={Time.deltaTime:F3},maxDelta={Time.maximumDeltaTime:F3} "+string.Join("; ",liveEffect.GetComponentsInChildren<ParticleSystem>().Select(p=>$"{p.name}:n={p.particleCount},em={p.emission.enabled},bursts={p.emission.burstCount},size={p.GetComponent<ParticleSystemRenderer>().bounds.size},active={p.isPlaying}"))+"\n");
            }
            if(blasts>0&&Time.time-impact>0&&Time.time-impact<.15f)
            {
                var effect=GameObject.Find("ExplosivePackageBlast(Clone)");
                if(effect!=null)
                {
                    Require(effect.GetComponentsInChildren<ParticleSystemRenderer>().Where(r=>r.enabled).All(r=>r.sharedMaterial!=null&&r.sharedMaterial.shader.isSupported),"Material de explos?o inv?lido.");
                    ScreenCapture.CaptureScreenshot("Logs/explosive-package-imported-effect-"+mode+".png");
                }
            }
            if(blasts>0&&Time.time-impact<.20f)Require(manager.StopFall,"Gravidade liberada antes da ênfase.");
            if(blasts>0&&Time.time-impact>1.8f&&!manager.StopFall&&GameObject.Find("ExplosivePackageBlast(Clone)")==null)
            {
                Require(coreSamples>=3,"O n?cleo n?o persistiu por tr?s quadros de observa??o.");
                Require(seenImported && seenParticles,"O novo efeito importado n?o emitiu part?culas.");
                Require(GameObject.Find("ExplosivePackageBlast(Clone)")==null,"Efeito importado n?o foi limpo.");
                Require(blasts==1&&hits==(mode==0?24:79),$"Esperado um estouro e {(mode==0?24:79)} peças: {blasts}/{hits}.");
                File.AppendAllText("Logs/explosive-package-lab-smoke.result",$"PASS mode {mode}: 81 cells, visible HUD, one blast, core visible in {coreSamples} observed frames, maxDelta={Time.maximumDeltaTime:F3}, {hits} pieces hit, impact hold, paid boosts hidden.\n");
                Time.maximumDeltaTime=SessionState.GetFloat(Key+".PreviousDelta",.3333333f);SessionState.SetBool(Key+".Sampling",false);testing=false;ItemDestroyAnimation.BlastApplied-=Blast;ItemMulticolor.ActualExplosion-=Hit;
                if(mode==0){SessionState.SetInt(ExplosivePackageLaboratory.ModeKey,1);Open();}
                else{SessionState.SetBool(Key+".Smoke",false);File.AppendAllText("Logs/explosive-package-lab-smoke.result","PASS: both scenarios; combo uses actual scripted swap. No physical pointer/device/audio/full victory claim. Reopening clean combo bench.\n");Open();}
            }
            else if(EditorApplication.timeSinceStartup-began>30)throw new Exception($"Timeout do cenário {mode}: blasts={blasts}, hits={hits}");
        }
        void OnInspectorUpdate()=>Repaint();
        void OnGUI()
        {
            GUILayout.Label("Combinacoes de especiais — laboratório",EditorStyles.boldLabel);
            int mode=SessionState.GetInt(ExplosivePackageLaboratory.ModeKey,0);
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Foguete H + helice"))OpenRocketHorizontal();
            if(GUILayout.Button("Foguete V + helice"))OpenRocketVertical();
            EditorGUILayout.EndHorizontal();
            if(GUILayout.Button("Helice + dinamite"))OpenDynamitePropeller();
            if(GUILayout.Button("Duas helices"))OpenPropellerPair();
            int selected=EditorGUILayout.Popup("Cenário",mode,ExplosivePackageLaboratory.Modes);
            if(selected!=mode){SessionState.SetInt(ExplosivePackageLaboratory.ModeKey,selected);Open();}
            if(GUILayout.Button("Abrir / reiniciar cenário"))Open();
            EditorGUILayout.HelpBox(mode==8?"Arraste uma helice sobre a outra. Observe a fusao, a formacao de tres e a separacao para os alvos.":mode>=6?"Arraste a helice da direita sobre o foguete da esquerda. O foguete viaja com ela e dispara no destino.":mode==0?"Toque na dinamite central.":"Arraste o especial da direita sobre a dinamite da esquerda. Foguete: tres linhas/colunas; helice: transporte; bola de luz: cor mais comum.",MessageType.Info);
            using(new EditorGUI.DisabledScope(!CanActivate()))if(GUILayout.Button(mode==0?"Ativar pacote explosivo":"Combinar especiais (diagnostico)"))Activate();
            using(new EditorGUI.DisabledScope(!ready||!EditorApplication.isPlaying))
            {
                float next=EditorGUILayout.Slider("Velocidade",speed,.2f,1.5f);if(!Mathf.Approximately(speed,next)){speed=next;Time.timeScale=speed;}
                if(GUILayout.Button("Velocidade normal")){speed=1;Time.timeScale=1;}
            }
            if(GUILayout.Button("Desativar e voltar às fases normais"))Disable();
            EditorGUILayout.HelpBox("Bancada isolada: não gasta boosters, não entrega recompensas e não avança a campanha. Reinicie para repetir.",MessageType.None);
        }
    }
}
