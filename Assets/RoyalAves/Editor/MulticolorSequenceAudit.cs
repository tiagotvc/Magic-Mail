using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Effects;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Items._Interfaces;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.System;
using SweetSugar.Scripts.System.Pool;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class MulticolorSequenceAudit
    {
        const string Key = "MagicMail.MulticolorSequenceAudit";
        static readonly ItemsTypes[] Partners = { ItemsTypes.NONE, ItemsTypes.NONE, ItemsTypes.NONE,
            ItemsTypes.PACKAGE, ItemsTypes.HORIZONTAL_STRIPED, ItemsTypes.MARMALADE, ItemsTypes.MULTICOLOR, ItemsTypes.NONE };
        static readonly string[] Names = { "empty", "single", "many-with-external-damage", "dynamite", "rocket", "helix", "double-multicolor", "cancel-and-reuse" };
        static int stage, caseIndex, actualCount, releasedCount, originalColorLimit;
        static double deadline, nextAt;
        static double nextStatus;
        static ItemMulticolor source;
        static readonly HashSet<Item> known = new HashSet<Item>();
        static readonly HashSet<Item> detonated = new HashSet<Item>();
        static readonly HashSet<Item> actualObserved = new HashSet<Item>();
        static readonly List<string> timeline = new List<string>();
        static bool raysOff, attacked, cancelled;
        static string failure;
        static int detonationFrame;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            deadline = EditorApplication.timeSinceStartup + 180;
            EditorApplication.update -= Poll; EditorApplication.update += Poll;
            ItemMulticolor.AuditEvent -= Observe; ItemMulticolor.AuditEvent += Observe;
            ItemMulticolor.ActualExplosion -= Actual; ItemMulticolor.ActualExplosion += Actual;
        }
        [MenuItem("Tools/Magic Mail/Audit Multicolor Sequence in Play")]
        public static void Request() => File.WriteAllText("Logs/multicolor-audit.request", "run");
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void Observe(ItemMulticolor owner, string action, Item target)
        {
            if (!SessionState.GetBool(Key, false) || owner != source) return;
            timeline.Add($"{Names[caseIndex]},{Time.frameCount},{Time.time.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)},{owner.Phase},{action},{owner.MarkedCount}/{owner.TargetCount}");
            try
            {
                if (action == "Marked")
                {
                    if (caseIndex == 3 && owner.MarkedCount == 1)
                        Check(target is ItemPackage package && package.IsAppearing,
                            "Converted dynamite did not animate its entrance while reserved.");
                    known.Add(target);
                    Check(target.IsMulticolorReserved, "Marked target was not reserved.");
                    if (caseIndex == 2 && !attacked)
                    {
                        attacked = true;
                        target.StopDestroy(); target.DestroyItem(playerActivated:true);
                        target.SmoothDestroy(); target.DestroyBehaviour();
                        target.NextType = ItemsTypes.PACKAGE; target.ChangeType();
                        Check(target.gameObject.activeSelf && target.IsMulticolorReserved, "External destruction escaped reservation.");
                    }
                }
                if (action == "RaysOff")
                {
                    detonationFrame = Time.frameCount;
                    raysOff = true;
                    Check(owner.MarkedCount == owner.TargetCount, "Detonation started before every target was marked.");
                    Check(!owner.IsSpinning && owner.ActiveArcCount == 0, "Spin or arcs still active at detonation boundary.");
                    Check(!LiveArcs(), "An electric renderer remained visible at detonation boundary.");
                    Check(!LiveChargeParticles(), "Charging particles remained during detonation.");
                    if (caseIndex == 6) Check(owner.TargetCount >= 40, "Double-multicolor case did not select the whole board.");
                    if (caseIndex == 2) ScreenCapture.CaptureScreenshot("Logs/multicolor-audit-detonating.png");
                }
                if (action == "AllMarked" && caseIndex == 2) ScreenCapture.CaptureScreenshot("Logs/multicolor-audit-marked.png");
                if (action == "Detonate")
                {
                    Check(Time.frameCount == detonationFrame, "Marked targets did not detonate together.");
                    releasedCount++; detonated.Add(target);
                    Check(raysOff && owner.Phase == ItemMulticolor.SequencePhase.Detonating, "Target released before phase barrier.");
                    Check(owner.MarkedCount == owner.TargetCount && !owner.IsSpinning && !LiveArcs(), "Explosion overlaps marking/spin/rays.");
                }
                if (action == "SourceDetonate" || action == "SourceVisualDetonate") Check(raysOff && !LiveArcs(), "Source exploded with live rays.");
                if (action == "SourceDetonate" && (caseIndex == 2 || caseIndex == 6))
                {
                    int ordinary = known.Count(item => item != null && item.currentType == ItemsTypes.NONE);
                    int collecting = known.Count(item => item != null && item.currentType == ItemsTypes.NONE && item.HasActiveCollectionTarget());
                    int flights = LevelManager.THIS.animateItems.Count(effect => effect != null && effect.target && effect.GetComponent<SpriteRenderer>() != null);
                    Check(collecting > 0 && flights == collecting, $"Objective flights mismatch: expected {collecting}, got {flights}.");
                    if (caseIndex == 6) Check(collecting < ordinary, "Double fixture must include non-objective pieces that break locally.");
                }
                if (action == "SourceDetonate") Check(actualCount == releasedCount, "Some marked targets did not begin destruction in the batch.");
            }
            catch (Exception e) { failure = e.ToString(); }
        }
        static bool LiveArcs() => UnityEngine.Object.FindObjectsByType<Lightning>(FindObjectsSortMode.None)
            .Any(arc => arc.GetComponentsInChildren<LineRenderer>().Any(line => line.enabled && line.gameObject.activeInHierarchy));
        static bool LiveChargeParticles() => UnityEngine.Object.FindObjectsByType<ElectricChargeParticles>(FindObjectsSortMode.None)
            .Any(effect => effect.name == "MarkedPieceElectricSparks" && effect.GetComponent<ParticleSystem>().particleCount > 0);
        static void Actual(Item item)
        {
            if (!SessionState.GetBool(Key, false) || !known.Contains(item)) return;
            // Later cascades may reuse the same pooled instance. Validate its first marked destruction only.
            if (!actualObserved.Add(item)) return;
            actualCount++;
            if (Time.frameCount != detonationFrame) failure = "An actual target DestroyCor started in a different frame.";
            if (!raysOff || source.MarkedCount != source.TargetCount || LiveArcs()) failure = "An actual DestroyCor ran before all marks/rays-off.";
        }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            try
            {
                if (File.Exists("Logs/multicolor-audit.request"))
                {
                    File.Delete("Logs/multicolor-audit.request");
                    Check(!EditorApplication.isPlaying, "Start audit from Edit Mode.");
                    File.WriteAllText("Logs/multicolor-audit.result", "RUNNING");
                    timeline.Clear(); failure = null; stage = caseIndex = 0;
                    SessionState.SetBool(Key, true); deadline = EditorApplication.timeSinceStartup + 150;
                    EditorApplication.isPlaying = true;
                }
                if (SessionState.GetBool(Key, false) && EditorApplication.timeSinceStartup >= nextStatus)
                {
                    nextStatus = EditorApplication.timeSinceStartup + 1;
                    File.WriteAllText("Logs/multicolor-audit.status", $"playing={EditorApplication.isPlaying}, stage={stage}, case={caseIndex}, manager={LevelManager.THIS != null}, AI={AI.THIS != null}, game={LevelManager.THIS?.gameStatus}, source={source?.Phase}, marks={source?.MarkedCount}/{source?.TargetCount}");
                }
                if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
                Check(failure == null, failure);
                Check(EditorApplication.timeSinceStartup < deadline, "Audit timed out at " + Names[caseIndex]);
                var manager = LevelManager.THIS;
                if (manager == null) return;
                if (stage == 0)
                {
                    if (manager.gameStatus == GameState.Map) manager.gameStatus = GameState.PrepareGame;
                    if (manager.field == null || manager.field.squaresArray == null || manager.field.squaresArray.Length < 25) return;
                    if (manager.gameStatus != GameState.Playing && manager.gameStatus != GameState.WaitForPopup) return;
                    originalColorLimit = manager.levelData.colorLimit;
                    manager.gameStatus = GameState.Playing;
                    if (AI.THIS != null) AI.THIS.enabled = false;
                    foreach (var goal in manager.levelData.TargetCounters)
                    { goal.targetLevel = goal.targetLevel.DeepCopy(); goal.targetLevel.setCount = SetCount.Manually; goal.count = 999; }
                    manager.levelData.limit = 999;
                    stage = 1;
                }
                if (EditorApplication.timeSinceStartup < nextAt) return;
                if (stage == 1) { Prepare(manager); stage = 2; nextAt = EditorApplication.timeSinceStartup + .2; return; }
                if (stage == 2)
                {
                    var partner = caseIndex == 0 ? null : manager.field.GetSquare(4,3).Item;
                    if (caseIndex > 0) Check(partner != null && partner.currentType == Partners[caseIndex], $"Fixture partner changed: expected {Partners[caseIndex]}, got {partner?.currentType}.");
                    if (caseIndex == 2 || caseIndex == 4)
                    {
                        var pair = caseIndex == 2 ? partner.SwitchDirection(Vector3.right) : source.SwitchDirection(Vector3.left);
                        Check(pair.currentItem != null && pair.neighborItem != null, "Actual swap was rejected.");
                    }
                    else if (partner == null) source.Destroy(source, null); else source.Check(source, partner);
                    stage = 3; return;
                }
                if (stage == 3)
                {
                    if (caseIndex == 7 && !cancelled && source.MarkedCount >= 3)
                    {
                        cancelled = true;
                        PoolForFixture(source);
                        Check(known.All(i => i == null || !i.IsMulticolorReserved), "Cancellation left reservations.");
                        Check(!LiveArcs(), "Cancellation left live arcs.");
                        Check(!LiveChargeParticles(), "Cancellation left charging particles.");
                        var cell = manager.field.GetSquare(3,3);
                        cell.Item = null;
                        var reused = cell.GenItem(false, ItemsTypes.MULTICOLOR, noAnim:true) as ItemMulticolor;
                        Check(reused.Phase == ItemMulticolor.SequencePhase.Idle && reused.ActiveArcCount == 0 && !reused.IsSpinning, "Pooled multicolor retained a sequence.");
                        CompleteCase(); return;
                    }
                    if (source.Phase == ItemMulticolor.SequencePhase.Detonating) Check(!LiveArcs(), "Ray visible during explosion frames.");
                    if (source.Phase != ItemMulticolor.SequencePhase.Finished) return;
                    Check(raysOff, "Missing phase boundary.");
                    Check(source.TargetCount == source.MarkedCount, "Incomplete marking.");
                    Check(known.All(i => i == null || !i.IsMulticolorReserved), "Finished sequence left reservations.");
                    Check(caseIndex == 0 || releasedCount > 0, "No marked target detonated.");
                    if (caseIndex == 2) Check(attacked, "External damage stress did not run.");
                    CompleteCase();
                }
            }
            catch (Exception e) { Finish("FAIL: " + e); }
        }
        static void CompleteCase()
        {
            timeline.Add($"PASS,{Names[caseIndex]},actual-explosions={actualCount},released={releasedCount}");
            caseIndex++;
            if (caseIndex == Names.Length) { Finish("PASS: 8 real Play scenarios: empty, single, many with external destruction attempts, dynamite, rocket, helix, double multicolor, cancellation/pool reuse. Every detonation follows all marks, completed spin and zero visible arcs."); return; }
            stage = 1; nextAt = EditorApplication.timeSinceStartup + 3;
        }
        static void Prepare(LevelManager manager)
        {
            manager.StopAllCoroutines(); manager.findMatchesStarted = true;
            manager.DragBlocked = false;
            manager.levelData.colorLimit = caseIndex == 0 ? 1 : originalColorLimit;
            known.Clear(); detonated.Clear(); actualObserved.Clear(); actualCount = releasedCount = 0;
            raysOff = attacked = cancelled = false;
            var field = manager.field;
            // Launched specials can remain active after leaving their cell. End every old item lifecycle
            // before constructing the next fixture, so a previous helix cannot attack the new partner.
            foreach (var previous in UnityEngine.Object.FindObjectsByType<Item>()) PoolForFixture(previous);
            foreach (var cell in field.GetSquares())
            {
                if (cell == null) continue;
                cell.StopAllCoroutines();
                if (cell.Item != null) PoolForFixture(cell.Item);
                foreach (var layer in cell.subSquares.ToArray()) UnityEngine.Object.Destroy(layer.gameObject);
                cell.subSquares.Clear(); cell.type = SquareTypes.EmptySquare; cell.canMoveIn = cell.canMoveOut = true;
                cell.undestroyable = cell.cannotBeDestroy = false; cell.item = null;
            }
            foreach (var cell in field.GetSquares())
            {
                if (cell == null) continue;
                int color = 1 + (cell.col + cell.row) % 3;
                if (caseIndex > 1 && (cell.col + cell.row * field.fieldData.maxCols) % 4 == 0) color = 0;
                cell.Item = cell.GenItem(false, ItemsTypes.NONE, color, noAnim:true);
            }
            Check(field.GetSquares().Where(cell => cell != null).All(cell => cell.Item != null), "Fixture has an empty cell.");
            Check(field.GetSquares().Where(cell => cell != null).Select(cell => cell.Item).Distinct().Count() == field.GetSquares().Count(cell => cell != null), "Fixture reused a live item in two cells.");
            var sourceCell = field.GetSquare(3,3);
            PoolForFixture(sourceCell.Item); sourceCell.item = null;
            source = sourceCell.GenItem(false, ItemsTypes.MULTICOLOR, noAnim:true) as ItemMulticolor;
            sourceCell.Item = source;
            if (caseIndex != 0)
            {
                var partnerCell = field.GetSquare(4,3);
                PoolForFixture(partnerCell.Item); partnerCell.item = null;
                partnerCell.Item = partnerCell.GenItem(false, Partners[caseIndex], 0, noAnim:true);
            }
            // The audit bypasses the start modal, so position the game camera on the visible board.
            var bounds = new Bounds(field.squaresArray.First(cell => cell != null).transform.position, Vector3.zero);
            foreach (var cell in field.squaresArray.Where(cell => cell != null)) bounds.Encapsulate(cell.transform.position);
            var camera = manager.GetComponent<Camera>();
            camera.enabled = true; camera.orthographic = true;
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z - 10f);
            camera.orthographicSize = Mathf.Max((bounds.size.y + field.squareHeight) * .5f,
                (bounds.size.x + field.squareWidth) / (2f * camera.aspect)) * 1.1f;
        }
        static void Finish(string result)
        {
            File.WriteAllLines("Logs/multicolor-audit-timeline.csv", timeline);
            File.WriteAllText("Logs/multicolor-audit.result", result);
            SessionState.SetBool(Key, false);
            if (LevelManager.THIS != null && originalColorLimit > 0) LevelManager.THIS.levelData.colorLimit = originalColorLimit;
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            if (result.StartsWith("FAIL")) Debug.LogError(result); else Debug.Log(result);
        }
        static void PoolForFixture(Item item)
        {
            if (item == null || !item.gameObject.activeSelf) return;
            item.StopAllCoroutines();
            item.CancelInvoke();
            // Mirror normal pool cleanup before OnEnable can run checks against an old cell.
            item.square = null;
            item.previousSquare = null;
            ObjectPooler.Instance.PutBack(item.gameObject);
        }
    }
}
