using System;
using System.IO;
using System.Linq;
using RoyalAves.Effects;
using RoyalAves.Gameplay;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    // Run only in a disposable project copy with a separate PlayerSettings company/product.
    public static class InkwellNativeVerification
    {
        const string Key = "MagicMail.InkwellNativeVerification";
        static double at;
        static int step, hits;
        static Square cell;
        static InkwellCell ink;
        static int goal;
        static float quietSince = -1;
        static bool capturedFlight;
        static StoneShatterEffect porcelainBreak;
        static PorcelainBadgeDebris crownFragment;
        static Vector3 crownOrigin;
        [InitializeOnLoadMethod] static void Init() { EditorApplication.update -= Tick; EditorApplication.update += Tick; }
        public static void Run()
        {
            if (!Application.dataPath.Replace('\\','/').Contains("/Logs/InkwellValidationProject/Assets"))
                throw new InvalidOperationException("Execute somente na cópia isolada InkwellValidationProject.");
            InkwellLaboratoryTools.Prepare();
            InkwellLaboratoryTools.VerifyAuthoring();
            SessionState.SetBool(Key, true);
            SessionState.SetString(Key + ".Deadline", DateTime.UtcNow.AddMinutes(4).ToString("O"));
            InkwellLaboratoryTools.Open();
        }
        static void Require(bool ok, string reason) { if (!ok) throw new Exception(reason); }
        static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            try
            {
                Require(DateTime.UtcNow < DateTime.Parse(SessionState.GetString(Key + ".Deadline", ""), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind), "Timeout da validação nativa.");
                var manager = LevelManager.THIS;
                if (!EditorApplication.isPlaying || manager == null || !InkwellLaboratory.IsActive || manager.gameStatus != GameState.Playing) return;
                if (EditorApplication.timeSinceStartup < at) return;
                if (step == 0)
                {
                    ink = UnityEngine.Object.FindFirstObjectByType<InkwellCell>();
                    Require(ink != null && ink.Hits == 0, "Tinteiro inicial ausente/danificado.");
                    cell = ink.transform.parent.GetComponent<Square>();
                    goal = manager.levelData.TargetCounters.Sum(g=>g.count);
                    Require(goal == 1, "Meta inicial deve ser um tinteiro.");
                    var inkwellGoal=manager.levelData.TargetCounters.Single(g=>g.targetLevel.name=="Inkwell");
                    inkwellGoal.count=2;
                    ink.gameObject.SetActive(false);
                    Require(inkwellGoal.IsTargetReachedSublevel() && !inkwellGoal.IsTotalTargetReached(),"Meta de subnível confundida com a meta total.");
                    ink.gameObject.SetActive(true);inkwellGoal.count=1;
                    InkwellLaboratoryTools.VerifyAssets();
                    var stoneCopy=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Blocks/EnchantedStone"));
                    stoneCopy.transform.position=Vector3.right*100;
                    var defaultStone=StoneShatterEffect.Play(stoneCopy.GetComponent<SpriteRenderer>());
                    Require(!defaultStone.IsPorcelain && !defaultStone.IsChipOnly && defaultStone.ShardCount==30 && defaultStone.gameObject.layer==0,"Modo original da pedra foi alterado.");
                    UnityEngine.Object.Destroy(defaultStone.gameObject);UnityEngine.Object.Destroy(stoneCopy);
                    var registrationRenderer=ink.GetComponent<SpriteRenderer>();
                    Vector3 installedPosition=ink.transform.position,installedScale=ink.transform.localScale,mouth=ink.MouthPosition;
                    for(int state=0;state<5;state++)
                    {
                        registrationRenderer.sprite=ink.stages[state];
                        Require(ink.transform.position==installedPosition && ink.transform.localScale==installedScale && ink.MouthPosition==mouth,
                            "Troca de estágio alterou transformação ou origem do respingo.");
                        InkwellLaboratoryTools.Capture($"Logs/inkwell-registered-state-{state}.png");
                    }
                    registrationRenderer.sprite=ink.stages[0];
                    // Measure visible alpha, not the transparent padding of the six shared sprite frames.
                    var pixels=new Texture2D(2,2);
                    pixels.LoadImage(File.ReadAllBytes("Assets/RoyalAves/Art/Obstacles/Inkwell/InkwellStages.png"));
                    var sprite=ink.GetComponent<SpriteRenderer>().sprite;var rect=sprite.rect;
                    int low=(int)rect.yMax,high=(int)rect.yMin,left=(int)rect.xMax,right=(int)rect.xMin;
                    for(int y=(int)rect.yMin;y<(int)rect.yMax;y++)for(int x=(int)rect.xMin;x<(int)rect.xMax;x++)
                        if(pixels.GetPixel(x,y).a>.5f){low=Math.Min(low,y);high=Math.Max(high,y);left=Math.Min(left,x);right=Math.Max(right,x);}
                    float pitch=Vector3.Distance(cell.field.GetSquare(0,0).transform.position,cell.field.GetSquare(0,1).transform.position);
                    float visibleHeight=(high-low+1)/sprite.pixelsPerUnit*ink.transform.lossyScale.y/pitch;
                    var visibleCenter=new Vector3(((left+right+1)*.5f-rect.x-sprite.pivot.x)/sprite.pixelsPerUnit,
                        ((low+high+1)*.5f-rect.y-sprite.pivot.y)/sprite.pixelsPerUnit,0);
                    var centerOffset=(Vector2)(ink.transform.TransformPoint(visibleCenter)-cell.transform.position);
                    Require(centerOffset.magnitude/pitch<.002f,"Tinteiro fora do centro da célula: "+centerOffset);
                    Require(ink.BodyCenterLocal.sqrMagnitude<.00001f,"Origem dos cacos fora do corpo centralizado.");
                    UnityEngine.Object.Destroy(pixels);
                    Require(visibleHeight>=.8f && visibleHeight<=1.05f,"Tamanho visível fora do padrão da célula: "+visibleHeight);
                    File.WriteAllText("Logs/inkwell-size-verification.result",$"PASS: visible alpha height/cell pitch={visibleHeight:F3}; pixelsPerUnit={sprite.pixelsPerUnit}; visibleCenterOffset={centerOffset}.");
                    InkwellLaboratoryTools.Capture("Logs/inkwell-size-before-hit.png");
                    // A diagonal origin must not masquerade as a neighboring match.
                    var diagonal = cell.field.GetSquare(cell.col+1,cell.row+1);
                    ink.HandleHit(cell,diagonal); Require(ink.Hits==0,"Dano diagonal indevido.");
                    // Exercise the real swap/match resolver, not DestroyBlock on the neighboring square.
                    foreach (var tile in cell.field.GetSquares())
                        if (tile.Item != null && tile.Item.currentType == SweetSugar.Scripts.Items.ItemsTypes.NONE)
                            tile.Item.colorableComponent.SetColor((tile.row + 2*tile.col)%4);
                    cell.field.GetSquare(4,2).Item.colorableComponent.SetColor(0);
                    cell.field.GetSquare(4,3).Item.colorableComponent.SetColor(1);
                    cell.field.GetSquare(4,4).Item.colorableComponent.SetColor(0);
                    cell.field.GetSquare(5,3).Item.colorableComponent.SetColor(0);
                    cell.field.GetSquare(5,2).Item.colorableComponent.SetColor(2);
                    cell.field.GetSquare(5,4).Item.colorableComponent.SetColor(3);
                    // The legacy drag delta is inverse to the board direction: left swaps to the right.
                    cell.field.GetSquare(4,3).Item.SwitchDirection(Vector3.left);
                    step=10; at=0;
                    return;
                }
                if (step == 10)
                {
                    var flight=UnityEngine.Object.FindFirstObjectByType<InkwellSplashEffect>();
                    if (flight != null && flight.Age > .06f && flight.Age < InkwellSplashEffect.TravelTime)
                    {
                        InkwellLaboratoryTools.Capture("Logs/inkwell-liquid-flight.png"); capturedFlight=true;
                    }
                    if (ink.Hits==0) return;
                    Require(InkwellSplashEffect.Emitted>=1,"Match causou dano sem projetar tinta.");
                    if (!capturedFlight) return;
                    Require(ink.Hits==1,"Fixture deve gerar um primeiro impacto.");
                    cell.DestroyBlock(); Require(ink.Hits==1,"Impacto duplicado no mesmo instante.");
                    hits=1;step=1;at=EditorApplication.timeSinceStartup+.32;return;
                }
                if (step == 1)
                {
                    var stain=cell.field.GetComponentsInChildren<InkwellStainedCell>().Single();
                    Require(stain.Floor.Progress>0 && stain.Floor.Progress<1,"Preenchimento sem fase intermediária.");
                    ScreenCapture.CaptureScreenshot("Logs/inkwell-fill-mid.png");
                    step=2;at=EditorApplication.timeSinceStartup+.8;return;
                }
                if (step == 2)
                {
                    var stains=cell.field.GetComponentsInChildren<InkwellStainedCell>();
                    Require(stains.Length==hits,"Quantidade incorreta de células pintadas.");
                    Require(stains.All(s=>s.Floor.Progress==1),"Tinta não chegou ao preenchimento completo.");
                    Require(manager.levelData.TargetCounters.Sum(g=>g.count)==goal,"Meta contou dano parcial/pintura.");
                    if(hits<4){cell.DestroyBlock();hits++;at=EditorApplication.timeSinceStartup+1.1;return;}
                    Require(cell.GetComponent<InkwellStainedCell>()==null,"Célula central pintada antes da quebra.");
                    var stained=stains.First(s=>s.GetComponent<Square>().Item!=null);
                    var source=stained.GetComponent<Square>(); var piece=source.Item;
                    Require(piece.GetComponent<InkwellStainedPiece>()!=null,"Peça não recebeu tinta.");
                    int logicalColor=piece.color; var original=piece.GetSpriteRenderer(); Color rendererColor=original.color;
                    // Isolated transform movement tests ownership, not pointer input or a real swap.
                    Vector3 before=stained.transform.position;
                    piece.transform.position+=Vector3.right*.2f;
                    Require(stained.transform.position==before,"Marca do chão se moveu com a peça.");
                    Require(piece.GetComponentsInChildren<InkTintVisual>().Length>0,"Tinta não acompanha a peça.");
                    Require(piece.color==logicalColor && original.color==rendererColor,"Tinta alterou a cor lógica ou renderer original.");
                    piece.transform.position-=Vector3.right*.2f;
                    ScreenCapture.CaptureScreenshot("Logs/inkwell-four-neighbors.png");
                    cell.DestroyBlock(); Require(ink.Hits==5,"Quinto impacto não quebrou.");
                    Require(!ink.GetComponent<SpriteRenderer>().enabled && ink.GetComponent<SpriteRenderer>().sprite==ink.stages[4],"Quebra ainda mostrou o PNG final estático.");
                    Require(StoneShatterEffect.PorcelainChipBursts==4 && StoneShatterEffect.PorcelainBreakBursts==1,"Número de lascas/quebras incorreto.");
                    porcelainBreak=UnityEngine.Object.FindObjectsByType<StoneShatterEffect>().Single(e=>e.IsPorcelain&&!e.IsChipOnly);
                    crownFragment=UnityEngine.Object.FindFirstObjectByType<PorcelainBadgeDebris>();
                    Require(porcelainBreak.ShardCount==22 && crownFragment!=null,"Cacos sólidos ou fragmento frontal ausentes.");
                    crownOrigin=crownFragment.transform.position;
                    cell.DestroyBlock(); Require(ink.Hits==5,"Quebra aceitou sexto impacto.");
                    Require(cell.GetComponent<InkwellStainedCell>()!=null,"Quinto impacto não pintou a própria célula.");
                    step=30;at=0;return;
                }
                if(step==30)
                {
                    if(porcelainBreak.AnimationTime<.12f)return;
                    Require(porcelainBreak.GetComponent<MeshFilter>().sharedMesh.vertexCount>0,"Animação sem geometria visível.");
                    Require(crownFragment.KeepsOriginalScale && Vector3.Distance(crownOrigin,crownFragment.transform.position)>.01f,"Fragmento da coroa não voa ou mudou de escala.");
                    InkwellLaboratoryTools.Capture("Logs/inkwell-porcelain-burst.png");
                    step=31;return;
                }
                if(step==31)
                {
                    if(porcelainBreak.AnimationTime<.45f)return;
                    Require(crownFragment.KeepsOriginalScale,"Coroa encolheu durante o voo.");
                    InkwellLaboratoryTools.Capture("Logs/inkwell-porcelain-fall.png");
                    File.WriteAllText("Logs/inkwell-porcelain-verification.result","PASS: four intermediate chip bursts; fifth emits 22 thin purple/white/gold solids plus one original-scale textured crown-wall fragment; motion/mesh present; no static rubble sprite displayed; stone default still 30 original-style chunks.");
                    step=3;at=EditorApplication.timeSinceStartup+.8;return;
                }
                if(step==3)
                {
                    if (cell.field.GetItems().Any(p=>p != null && (p.falling || p.destroying)) || manager.StopFall || manager.animateItems.Count>0 ||
                        cell.field.GetComponentsInChildren<InkTintVisual>().Any(v=>v.Progress<1)) { quietSince=-1; return; }
                    if (quietSince<0) quietSince=Time.time;
                    if (Time.time-quietSince<3) return;
                    Require(manager.levelData.TargetCounters.Sum(g=>g.count)==0,"Meta não contou exatamente uma quebra.");
                    Require(cell.GetSubSquare().GetComponent<InkwellCell>()==null,"Camada do tinteiro não removida.");
                    Require(cell.field.GetComponentsInChildren<InkwellStainedCell>().Length==5,"Esperadas cinco células marcadas.");
                    Require(InkwellSplashEffect.Emitted==5 && InkwellSplashEffect.Landed==5,"Cada impacto deve emitir e entregar um respingo.");
                    ScreenCapture.CaptureScreenshot("Logs/inkwell-broken.png");
                    // Trigger the native board capture without invoking gameplay damage.
                    File.WriteAllText("Logs/inkwell-lab.request","capture");
                    step=4;at=EditorApplication.timeSinceStartup+1;return;
                }
                if(step==4)
                {
                    File.WriteAllText("Logs/inkwell-native-verification.result", "PASS: real swap/match resolver damages adjacent jar and emits visible liquid flight; five emitted/landed splashes; intermediate/full wet fill; imported prefab/sprites/material/shader; diagonal/duplicate rejection; four neighbors then center; goal decremented once; independent piece/floor ownership. Swap initiated by API, not physical pointer; remaining four hits diagnostic. No full victory/Continue or device claim.");
                    SessionState.SetBool(Key,false);EditorApplication.Exit(0);
                }
            }
            catch(Exception error)
            {
                File.WriteAllText("Logs/inkwell-native-verification.result","FAIL: "+error);
                SessionState.SetBool(Key,false);Debug.LogException(error);EditorApplication.Exit(1);
            }
        }
    }
}
