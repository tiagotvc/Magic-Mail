// // ©2015 - Present Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RoyalAves.Effects;
using SweetSugar.LeanTween.Framework;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.System;
using SweetSugar.Scripts.System.Utils;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEngine;
using Object = System.Object;
using Random = UnityEngine.Random;

namespace SweetSugar.Scripts.Items
{
    /// <summary>
    /// marmalade fly animation
    /// </summary>
    public class MarmaladeFly : UnityEngine.MonoBehaviour
    {
        public GameObject explosionPrefab;
        public GameObject particles;
        private Action callback;
        private bool reachedTarget;
        private int priority = 1;
        private bool canBeStarted;
        public ItemsTypes nextItemType;
        public bool setJelly;
        public Vector2Int[] targets;
        public float animationTime = 1.5f;
        private Vector3 pos, scale;
        private Quaternion rot;
        public int originSortingOrder = 2;
        public Vector3 startDirection;
        private ItemMarmalade thisItem;
        public IMarmaladeTargetable TargetItem;

        // The propeller blur while flying (RoyalAvesPropellerSpinBuilder fills this with the blurred frames of the
        // rotation sheet). Cycling the sprite looks like just the blades spinning; rotating the whole object would
        // spin the body too, which the art isn't drawn for.
        public Sprite[] SpinFrames;
        const float SpinFrameTime = 0.01f;
        // How long it sits still, propeller already spinning, before taking off.
        const float WindUpTime = 0.35f;
        // Flight speed (world units/second) for the direct arc to the target, clamped so a close target isn't instant
        // and a far one doesn't take forever.
        const float FlightSpeed = 5f;
        const float MinFlightTime = 0.5f;
        const float MaxFlightTime = 1.4f;
        Coroutine spinRoutine;
        Vector2 launchDirection;
        int flightTweenId = -1;
        AudioSource flightAudio;
        float flightAudioVolume;
        Coroutine flightAudioFade;
        const float TargetSearchTimeout = 2f;
        const float MaxFlightLifetime = 6f;
        float flightDeadline;
        bool reacquiringTarget;

        public void StartFly()
        {
            flightDeadline = Time.time + MaxFlightLifetime;
            reacquiringTarget = false;
            StartFlightAudio();
            particles.SetActive(true);
            // Correio Mágico: "Item mask" (a camada real cadastrada no projeto tem espaço no nome - "ItemMask" não
            // existia, então a hélice nunca saía da camada "Default" e ficava empatada com os efeitos de match
            // comuns). Ver RoyalAves.Effects.PowerUpEffectOrder pra regra geral dos power-ups.
            GetComponent<SpriteRenderer>().sortingLayerName = PowerUpEffectOrder.SortingLayer;
            GetComponent<SpriteRenderer>().sortingOrder = PowerUpEffectOrder.Value;
            PowerUpEffectOrder.Raise(particles);
            canBeStarted = true;
            if (spinRoutine == null && SpinFrames != null && SpinFrames.Length > 0) spinRoutine = StartCoroutine(SpinCor());
            if (targets != null)
            {
                foreach (var target in targets)
                {
                    var square = LevelManager.THIS.field.GetSquare(target.x, target.y);
                    IMarmaladeTargetable requested = square != null ? square.Item : null;
                    if (requested == null) requested = square;
                    if (IsValidTarget(requested) && requested.GetMarmaladeTarget == null && TargetItem == null)
                    {
                        TargetItem = requested;
                        TargetItem.GetMarmaladeTarget = gameObject;
                    }
                }
            }
        }

        private void Awake()
        {
            thisItem = GetComponentInParent<ItemMarmalade>();
            pos = transform.localPosition;
            rot = transform.localRotation;
            scale = transform.localScale;
        }

        private void OnEnable()
        {
            particles.SetActive(false);
            TargetItem = null;
            setJelly = false;
            transform.localPosition = pos;
            transform.localRotation = rot;
            transform.localScale = scale;
        }

        private void OnDisable()
        {
            StopFlightAudio();
            canBeStarted = false;
            reacquiringTarget = false;
            reachedTarget = false;
            LeanTween.Framework.LeanTween.cancel(gameObject);
            flightTweenId = -1;
            ReleaseTarget();
        
            StopAllCoroutines();
            spinRoutine = null;
            GetComponent<SpriteRenderer>().sortingLayerName = "Default";
            GetComponent<SpriteRenderer>().sortingOrder = originSortingOrder;
            transform.localPosition = pos;
            transform.localRotation = rot;
            transform.localScale = scale;
        }


        private void ReachItem()
        {
            if (!canBeStarted || !isActiveAndEnabled) return;
            if (!IsValidTarget(TargetItem)) { ReacquireTarget(); return; }
            StopFlightAudio();
            try
            {
                if (TargetItem is Item)
                {
                    if (setJelly)
                        TargetItem.GetItem.square.SetType(SquareTypes.JellyBlock, 1, SquareTypes.NONE, 1);
                    reachedTarget = true;
                    if (nextItemType == ItemsTypes.NONE ||
                        (TargetItem.GetItem.currentType != ItemsTypes.NONE && (TargetItem.GetItem.currentType != ItemsTypes.MULTICOLOR ||
                                                                               LevelManager.THIS.AdditionalSettings.MulticolorDestroyByBoostAndMarmalade)) && TargetItem.GetItem.currentType != ItemsTypes.TimeBomb)
                    {
                        callback?.Invoke();
                        TargetItem.GetItem.DestroyItem(true, true, thisItem);
                    }
                    else
                    {
                        TargetItem.GetItem.NextType = nextItemType;
                        TargetItem.GetItem.ChangeType(item=>
                        {
                            callback?.Invoke();
                            if (item != null) item.DestroyItem();
                        },false);
                        TargetItem.GetItem.DestroyItem();
                    }
                }
                else
                {
                    var square = TargetItem.GetGameObject.GetComponent<Square>();
                    var machine = square.GetSubSquare().GetComponent<RoyalAves.Gameplay.FrankingMachineCell>();
                    if (machine != null) machine.AcceptImpact(manual: true);
                    else square.DestroyBlock();
                    if (setJelly)
                        square.SetType(SquareTypes.JellyBlock, 1, SquareTypes.NONE, 1);
                    reachedTarget = true;
                    callback?.Invoke();
                }
            }
            finally { DestroyMarmalade(); }
        }

        private void DestroyMarmalade()
        {
            if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            if (LevelManager.THIS != null) LevelManager.THIS.FindMatches();
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (canBeStarted && Time.time >= flightDeadline) DestroyMarmalade();
        }

        void ReleaseTarget()
        {
            if (TargetItem is UnityEngine.Object owner && owner != null && TargetItem.GetMarmaladeTarget == gameObject)
                TargetItem.GetMarmaladeTarget = null;
            TargetItem = null;
        }

        static bool IsValidTarget(IMarmaladeTargetable target)
        {
            if (!(target is UnityEngine.Object owner) || owner == null) return false;
            var obj = target.GetGameObject;
            if (obj == null || !obj.activeInHierarchy) return false;
            if (target is Item item) return !item.destroying && !item.IsMulticolorReserved;
            if (target is Square square)
            {
                if (square.IsNone()) return false;
                var top = square.GetSubSquare();
                var machine = top.GetComponent<RoyalAves.Gameplay.FrankingMachineCell>();
                if (machine != null) return machine.CanAcceptPowerUpHit;
                var cabinet = top.GetComponent<RoyalAves.Gameplay.MailCubbyCell>();
                if (cabinet != null) return cabinet.Owner != null && !cabinet.Owner.IsBreaking &&
                    !cabinet.Owner.IsCleared && cabinet.Owner.Animation.CanAcceptHit;
                var crate = top.GetComponent<RoyalAves.Gameplay.ParcelStackLayer>();
                return crate == null || !crate.IsFinishing;
            }
            return true;
        }

        internal void SetDirection(Vector2 v)
        {
            Random.InitState(GetHashCode());
            launchDirection = v;
            LeanTween.Framework.LeanTween.scale(gameObject, Vector3.one * 1.5f, animationTime).setDelay(WindUpTime).setEase(LeanTweenType.easeOutBack);
            // A gentle rock, like a helicopter hovering, on top of the propeller blur (SpinCor, already spinning since
            // the moment it was touched). Keeps going for the whole flight; only the move tween gets cancelled below
            // if the target needs to change mid-flight.
            LeanTween.Framework.LeanTween.rotateZ(gameObject, 10f, 0.5f).setDelay(WindUpTime).setEase(LeanTweenType.easeInOutSine).setLoopPingPong();
            StartCoroutine(WindUpThenFindTarget());
        }

        IEnumerator WindUpThenFindTarget()
        {
            yield return new WaitForSeconds(WindUpTime);
            yield return AcquireTargetAndFly();
        }

        // One direct arc from here to the piece it's going to destroy, instead of flying to a vague point first and
        // only then searching for a target.
        private void FlyToTarget()
        {
            var start = transform.position;
            var end = TargetItem.GetGameObject.transform.position;
            var distance = Vector3.Distance(start, end);
            var lift = Mathf.Max(1f, distance * 0.35f);
            // v (the launch side) nudges the apex sideways, so the two duplicates (when there are two) arc apart
            // instead of overlapping, even when they end up going for the same general area.
            var apex = Vector3.Lerp(start, end, 0.5f) + Vector3.up * lift + (Vector3)(launchDirection * 0.6f);
            var flightTime = Mathf.Clamp(distance / FlightSpeed, MinFlightTime, MaxFlightTime);
            HoldFlightAudio();
            if (flightAudio != null && flightAudio.isPlaying)
                flightAudioFade = StartCoroutine(FadeFlightAudio(flightTime));
            // The spline ignores the first and last points as actual path (they only set the tangent), so start and
            // end are each doubled to make sure the piece truly begins and ends exactly there.
            flightTweenId = LeanTween.Framework.LeanTween.moveSpline(gameObject, new[] { start, start, apex, end, end }, flightTime)
                .setEase(LeanTweenType.easeInOutSine)
                .setOnUpdate(CheckTargetUpdate)
                .setOnComplete(ReachItem).id;
        }

        private IEnumerator SpinCor()
        {
            var renderer = GetComponent<SpriteRenderer>();
            var i = 0;
            while (true)
            {
                renderer.sprite = SpinFrames[i % SpinFrames.Length];
                i++;
                yield return new WaitForSeconds(SpinFrameTime);
            }
        }

        // If the target becomes invalid mid-flight (destroyed by something else, falls, etc.), pause just the move
        // (the rock and the propeller blur keep going) and head for a new one once found, from wherever it is now.
        void CheckTargetUpdate(float f)
        {
            if (!IsValidTarget(TargetItem)) ReacquireTarget();
        }

        void ReacquireTarget()
        {
            if (reacquiringTarget || !canBeStarted || !isActiveAndEnabled) return;
            reacquiringTarget = true;
            ReleaseTarget();
            HoldFlightAudio();
            if (flightAudio != null && flightAudio.clip != null && !flightAudio.isPlaying)
                flightAudio.Play();
            if (flightTweenId >= 0) LeanTween.Framework.LeanTween.pause(flightTweenId);
            StartCoroutine(ReacquireTargetThenFly());
        }

        IEnumerator AcquireTargetAndFly()
        {
            float deadline = Mathf.Min(Time.time + TargetSearchTimeout, flightDeadline);
            while (Time.time < deadline)
            {
                if (!IsValidTarget(TargetItem))
                {
                    ReleaseTarget();
                    FindTarget();
                }
                if (IsValidTarget(TargetItem))
                {
                    reacquiringTarget = false;
                    FlyToTarget();
                    yield break;
                }
                yield return null;
            }
            reacquiringTarget = false;
#if UNITY_EDITOR
            Debug.LogWarning($"[Helix target] level={LevelManager.THIS.currentLevel} search expired without a valid unreserved target; state={LevelManager.THIS.gameStatus}", this);
#endif
            DestroyMarmalade();
        }

        IEnumerator ReacquireTargetThenFly()
        {
            if (flightTweenId >= 0) LeanTween.Framework.LeanTween.cancel(flightTweenId);
            yield return AcquireTargetAndFly();
        }

        void StartFlightAudio()
        {
            StopFlightAudio();
            var sound = SoundBase.Instance;
            if (sound == null || sound.helixLaunch == null) return;
            var sharedSource = sound.GetComponent<AudioSource>();
            if (sharedSource == null) return;
            if (flightAudio == null) flightAudio = gameObject.AddComponent<AudioSource>();
            flightAudio.playOnAwake = false;
            flightAudio.clip = sound.helixLaunch;
            flightAudio.outputAudioMixerGroup = sharedSource.outputAudioMixerGroup;
            flightAudioVolume = sharedSource.volume;
            flightAudio.volume = flightAudioVolume;
            flightAudio.mute = sharedSource.mute;
            flightAudio.priority = sharedSource.priority;
            flightAudio.spatialBlend = 0f;
            flightAudio.pitch = 1f;
            // Keep the original timbre, including when a target changes and the flight takes longer.
            flightAudio.loop = true;
            flightAudio.Play();
        }

        void HoldFlightAudio()
        {
            if (flightAudioFade != null) StopCoroutine(flightAudioFade);
            flightAudioFade = null;
            if (flightAudio != null) flightAudio.volume = flightAudioVolume;
        }

        IEnumerator FadeFlightAudio(float flightTime)
        {
            const float fadeDuration = .08f;
            float elapsed = 0f;
            while (elapsed < flightTime)
            {
                elapsed += Time.deltaTime;
                if (flightAudio != null)
                    flightAudio.volume = flightAudioVolume * Mathf.Clamp01((flightTime - elapsed) / fadeDuration);
                yield return null;
            }
            if (flightAudio != null) flightAudio.Stop();
            flightAudioFade = null;
        }

        void StopFlightAudio()
        {
            HoldFlightAudio();
            if (flightAudio != null) flightAudio.Stop();
        }

        // Candies on the board that still count toward one of the level's "collect/match" goals.
        private IEnumerable<IMarmaladeTargetable> FindGoalItems()
        {
            var goalSprites = LevelManager.THIS.levelData.TargetCounters
                .Where(c => c.collectingAction == CollectingTypes.Destroy && !c.IsTargetStars() && c.GetCount() > 0 && c.extraObjects != null)
                .SelectMany(c => c.extraObjects)
                .Where(s => s != null)
                .ToArray();
            if (goalSprites.Length == 0) return Enumerable.Empty<IMarmaladeTargetable>();
            return LevelManager.THIS.field.GetItems()
                .Where(i => ArgItems(i) && goalSprites.Contains(i.GetSprite()))
                .MarmaladeCondition(gameObject, thisItem);
        }

        // Goal names do not necessarily name a SquareTypes value (e.g. MachineLetters).
        public static bool TryGetGoalSquareType(string goalName, out SquareTypes type)
        {
            if (goalName == "MachineLetters") { type = SquareTypes.FrankingMachine; return true; }
            return Enum.TryParse(goalName, out type) && Enum.IsDefined(typeof(SquareTypes), type);
        }

        IEnumerable<IMarmaladeTargetable> FindGoalSquares()
        {
            foreach (var goal in LevelManager.THIS.levelData.TargetCounters)
            {
                if (goal.IsTargetStars() || goal.GetCount() <= 0 ||
                    !TryGetGoalSquareType(goal.targetLevel.name, out var type)) continue;
                foreach (var square in LevelManager.THIS.field.squaresArray)
                {
                    if (square == null || square.IsNone()) continue;
                    var top = square.GetSubSquare();
                    // Campaign furniture uses the blocking flags of the old solid-block prefab.
                    // Its gameplay component, not that prefab's serialized type, identifies the goal.
                    bool matches = type == SquareTypes.FrankingMachine
                        ? top.GetComponent<RoyalAves.Gameplay.FrankingMachineCell>() != null
                        : type == SquareTypes.MailCubby
                            ? top.GetComponent<RoyalAves.Gameplay.MailCubbyCell>() != null
                            : square.type == type || square.subSquares.Any(layer => layer != null && layer.type == type);
                    if (matches && IsValidTarget(square)) yield return square;
                }
            }
        }

        private void FindTarget()
        {
            var goalSquares = FindGoalSquares().Distinct().MarmaladeCondition(gameObject, thisItem)
                .Where(IsValidTarget)
                .OrderByDescending(target => target.GetGameObject.GetComponent<Square>()?.GetSubSquare()
                    .GetComponent<RoyalAves.Gameplay.FrankingMachineCell>() != null)
                .ThenByDescending(target => Vector3.Distance(transform.position, target.GetGameObject.transform.position))
                .ToArray();
            if (goalSquares.Length > 0)
            {
                TargetItem = goalSquares[0];
                TargetItem.GetMarmaladeTarget = gameObject;
                return;
            }
            // The piece it destroys is always one of the level's current objectives, when there's one left on the
            // board. Prefers a farther one, so the flight is actually visible, instead of grabbing whatever's
            // closest. Falls back to the old priority chain (obstacles, ingredients, lonely items...) only when the
            // board has no goal piece left, so it never ends up with nowhere to go.
            var goalItems = FindGoalItems().ToArray();
            if (goalItems.Length > 0)
            {
                TargetItem = goalItems.OrderByDescending(i => Vector3.Distance(transform.position, i.GetGameObject.transform.position)).First();
                if ((Object)TargetItem != thisItem && (TargetItem.GetMarmaladeTarget == null || TargetItem.GetMarmaladeTarget == gameObject))
                    TargetItem.GetMarmaladeTarget = gameObject;
                return;
            }

            IEnumerable<IMarmaladeTargetable> items = Enumerable.Empty<IMarmaladeTargetable>();
            if (!items.Any())
                items = LevelManager.THIS.field.GetLonelyItemsOrCage()
                .Where(i => ArgItems(i));
            //If the marmalade should spread a jelly looking for non jellied squares
            if (setJelly)
                items = LevelManager.THIS.field.squaresArray.Where(i => i.type != SquareTypes.NONE && i.type != SquareTypes.JellyBlock).Select(i => i.Item).Where(i =>
                    i != null && i.Explodable).MarmaladeCondition(gameObject, thisItem);
            items = EligibleTargets(items);
            //trying to find ingredients
            if (!items.Any())
            {
                var list = LevelManager.THIS.field.squaresArray.Where(i => i.type != SquareTypes.NONE).Select(i => i.Item).Where(i =>
                    i != null && i.GetCachedComponent<TargetComponent>() && !i.Combinable);
                if (list.Any())
                    items = list.Where(i=>i.square.nextSquare != null && i.square.nextSquare.Item && i.square.nextSquare.Item.Explodable).Select(i => i.square.nextSquare?.Item).MarmaladeCondition(gameObject, thisItem);
            }
            items = EligibleTargets(items);

 
            
            // //Looking for blocks
            if (items == null || !items.Any())
            {
                items = LevelManager.THIS.field.squaresArray.Where(i => i.IsObstacle() && i.IsHaveDestroybleObstacle() && i.GetSubSquare().type != SquareTypes.ChocoSpread);
            }
            // Unavailable machines and targets reserved by another propeller must not prevent
            // the fallback to ordinary pieces, especially after the final objective is complete.
            items = EligibleTargets(items);
            //Looking through all items
            if (items == null || !items.Any())
            {
                items = LevelManager.THIS.field.GetItems().Where(i => i.Explodable).MarmaladeCondition(gameObject, thisItem);
            }

            TargetItem = items.Where(IsValidTarget).MarmaladeCondition(gameObject, thisItem).OrderBy(i => Vector3.Distance(transform.position, i.GetGameObject.transform.position) - (float)(i.GetItem?.currentType??0)).FirstOrDefault();
            if (TargetItem != null && (Object)TargetItem != thisItem && (TargetItem.GetMarmaladeTarget == null || TargetItem.GetMarmaladeTarget == gameObject))
            {
                TargetItem.GetMarmaladeTarget = gameObject;
            }
        }

        IEnumerable<IMarmaladeTargetable> EligibleTargets(IEnumerable<IMarmaladeTargetable> candidates)
        {
            return candidates.Where(IsValidTarget).MarmaladeCondition(gameObject, thisItem);
        }

        private static bool ArgItems(Item i)
        {
            return i != null && i.Explodable && !i.needFall && !i.falling && !i.destroying && !i.JustCreatedItem;
        }
    }

    public static class MarmaladeUtils
    {
        public static IEnumerable<IMarmaladeTargetable> MarmaladeCondition(this IEnumerable<IMarmaladeTargetable> seq, GameObject gameObject, Item item)
        {
            return seq.WhereNotNull().Where(i => (i.GetMarmaladeTarget == null || i.GetMarmaladeTarget == gameObject)&& (Object)i != item &&
                (!(i is Item targetItem) || !targetItem.destroying));
        }  
    }
}
