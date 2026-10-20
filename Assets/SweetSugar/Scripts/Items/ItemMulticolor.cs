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
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Effects;
using SweetSugar.Scripts.System;
using SweetSugar.Scripts.System.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SweetSugar.Scripts.Items
{
    /// <summary>
    /// Item multicolor
    /// </summary>
    public class ItemMulticolor : Item, IItemInterface
    {
        // public bool Combinable;
        public bool ActivateByExplosion;
        public bool StaticOnStart;

        public GameObject LightningPrefab;
        private bool jellySpread;

        private bool activated;

        // The fast spin while the bomb is active (RoyalAvesColorBombSpinBuilder fills this with the 12 frames of the
        // rotation sheet). The icon renderer is cached because Item's own renderer field is private.
        public Sprite[] SpinFrames;
        const float SpinFrameTime = 0.018f;
        SpriteRenderer iconRenderer;
        Coroutine spinRoutine;
        bool finishSpinCycle;
        Sprite restingSprite;
        readonly List<Lightning> electricArcs = new List<Lightning>();
        ElectricChargeParticles sourceCharge;
        bool appearing;
        bool sourceVisualExploded;
        float appearanceTime;
        Vector3 appearanceScale;
        const float AppearanceDuration = .28f;

        public void PlayAppearance()
        {
            EndAppearance();
            if (iconRenderer == null) iconRenderer = GetSpriteRenderer();
            if (iconRenderer == null) return;
            appearanceScale = iconRenderer.transform.localScale;
            appearanceTime = 0f;
            appearing = true;
            iconRenderer.transform.localScale = appearanceScale * .12f;
        }

        public static float AppearanceScaleAt(float progress)
        {
            progress = Mathf.Clamp01(progress);
            return progress < .6f
                ? Mathf.Lerp(.12f, 1.13f, 1f - Mathf.Pow(1f - progress / .6f, 3f))
                : Mathf.Lerp(1.13f, 1f, Mathf.SmoothStep(0f, 1f, (progress - .6f) / .4f));
        }

        void LateUpdate()
        {
            if (!appearing) return;
            if (destroying || activated || iconRenderer == null) { EndAppearance(); return; }
            appearanceTime += Time.deltaTime;
            iconRenderer.transform.localScale = appearanceScale * AppearanceScaleAt(appearanceTime / AppearanceDuration);
            if (appearanceTime >= AppearanceDuration) EndAppearance();
        }

        void EndAppearance()
        {
            if (appearing && iconRenderer != null) iconRenderer.transform.localScale = appearanceScale;
            appearing = false;
        }

        public override void InitItem()
        {
            ClearSelection();
            Phase = SequencePhase.Idle;
            TargetCount = MarkedCount = 0;
            sourceVisualExploded = false;
            jellySpread = false;
            EndAppearance();
            base.InitItem();
            EndSpin();
            // The visible sprite is on the child "Sprite", not on this root GameObject.
            if (iconRenderer == null) iconRenderer = GetComponentInChildren<SpriteRenderer>();
            activated = false;
        }

        public enum SequencePhase { Idle, Marking, Charged, Detonating, Finished, Cancelled }
        public SequencePhase Phase { get; private set; }
        public int TargetCount { get; private set; }
        public int MarkedCount { get; private set; }
        public bool IsSpinning => spinRoutine != null;
        public int ActiveArcCount => electricArcs.Count(arc => arc != null && arc.gameObject.activeInHierarchy);
#if UNITY_EDITOR
        public static event Action<ItemMulticolor, string, Item> AuditEvent;
        public static event Action<Item> ActualExplosion;
        internal static void ObserveExplosion(Item item) => ActualExplosion?.Invoke(item);
#endif
        void Trace(string action, Item target = null)
        {
#if UNITY_EDITOR
            AuditEvent?.Invoke(this, action, target);
#endif
        }

        sealed class MarkedTarget
        {
            public Item Item;
            public SelectionHighlight Highlight;
            public ElectricChargeParticles Charge;
        }
        readonly List<Item> reservations = new List<Item>();
        readonly List<MarkedTarget> markedTargets = new List<MarkedTarget>();
        Coroutine selectionPulse;
        readonly List<SelectionHighlight> activeHighlights = new List<SelectionHighlight>();
        Item pairedMulticolor;
        AnimateItems sequenceBarrier;
        LevelManager sequenceManager;

        public override void Check(Item item1, Item item2)
        {
            if (activated || (IsMulticolorReserved && MulticolorOwner != this)) return;
            if (item2 == null) { Destroy(item1, null); return; }
            if (!item2.Combinable && item2.currentType != ItemsTypes.MULTICOLOR) return;
            StartSelection(item2, item2.color);
        }

        public void Destroy(Item item1, Item item2)
        {
            if (activated || (IsMulticolorReserved && MulticolorOwner != this)) return;
            if (square == null) return;
            if (square.IsItemLocked) { square.DestroyBlock(); StopDestroy(); return; }
            int color = explodedItem != null ? explodedItem.color : Random.Range(0, LevelManager.THIS.levelData.colorLimit);
            StartSelection(item2, item2 != null ? item2.color : color);
        }

        void StartSelection(Item partner, int color)
        {
            if (activated || square == null) return;
            if (square.IsItemLocked) { square.DestroyBlock(); StopDestroy(); return; }
            activated = true;
            jellySpread = square.type == SquareTypes.JellyBlock || partner?.square?.type == SquareTypes.JellyBlock;
            Phase = SequencePhase.Marking;
            sequenceManager = LevelManager.THIS;
            var barriers = RoyalAves.Gameplay.ImpactBarrierSnapshot.Capture(square.field);
            bool doubleBomb = partner != null && partner.currentType == ItemsTypes.MULTICOLOR;
            IEnumerable<Item> candidates = doubleBomb ? (IEnumerable<Item>)field.GetItems() : field.GetItemsByColor(color);
            var items = barriers.FilterItems(square, candidates.Where(i => i != this && i != partner &&
                !i.IsMulticolorReserved && !i.destroying && (doubleBomb || i.currentType == ItemsTypes.NONE)))
                .Distinct().ToList();
            if (partner != null && partner != this && !partner.IsMulticolorReserved) items.Add(partner);
            TargetCount = items.Count;
            MarkedCount = 0;
            pairedMulticolor = doubleBomb ? partner : null;
            Claim(this);
            foreach (var item in items) Claim(item);
            sequenceBarrier = gameObject.AddComponent<AnimateItems>();
            sequenceBarrier.target = true;
            sequenceManager.animateItems.Add(sequenceBarrier);
            BeginSpin();
            Trace("Begin");
            var convertTo = partner != null && !doubleBomb ? partner.currentType : ItemsTypes.NONE;
            SoundBase.Instance?.PlayOneShot(SoundBase.Instance.colorBombExpl);
            StartCoroutine(RunSelection(items, partner, convertTo, doubleBomb, barriers));
        }

        void Claim(Item item)
        {
            if (item == null) return;
            item.ReserveForMulticolor(this);
            if (!reservations.Contains(item)) reservations.Add(item);
        }

        void Unclaim(Item item)
        {
            if (item == null) return;
            item.ReleaseFromMulticolor(this);
            reservations.Remove(item);
        }

        IEnumerator RunSelection(List<Item> items, Item partner, ItemsTypes convertTo, bool doubleBomb,
            RoyalAves.Gameplay.ImpactBarrierSnapshot barriers)
        {
            bool completed = false;
            try
            {
                foreach (var original in items)
                {
                    var item = original;
                    if (item == null || !item.gameObject.activeSelf) continue;
                    if (convertTo != ItemsTypes.NONE && item != partner && item.currentType == ItemsTypes.NONE && !item.square.IsItemLocked)
                    {
                        Unclaim(item);
                        item.NextType = convertTo == ItemsTypes.HORIZONTAL_STRIPED || convertTo == ItemsTypes.VERTICAL_STRIPED
                            ? (Random.value < .5f ? ItemsTypes.HORIZONTAL_STRIPED : ItemsTypes.VERTICAL_STRIPED) : convertTo;
                        Item created = null;
                        item.powerUpFormationMatch = false;
                        item.ChangeType(next => { created = next; Claim(next); }, false, false);
                        if (created == null) { Claim(item); }
                        else item = created;
                    }
                    CreateLightning(transform.position, item.transform.position, item.transform, true);
                    yield return new WaitForSeconds(Lightning.TravelTime);
                    var highlight = MakeHighlight(item);
                    activeHighlights.Add(highlight);
                    markedTargets.Add(new MarkedTarget { Item = item, Highlight = highlight,
                        Charge = ElectricChargeParticles.Charge(item.transform, field.squareWidth) });
                    MarkedCount++;
                    Trace("Marked", item);
                    if (selectionPulse == null) selectionPulse = StartCoroutine(PulseOutlines(activeHighlights));
                    yield return new WaitForSeconds(BeamStepDelay - Lightning.TravelTime);
                }
                Phase = SequencePhase.Charged;
                Trace("AllMarked");
                yield return FinishMarkingSpin();

                // Hard boundary: no beam, spinning frame or orbiting charge survives into detonation.
                EndSpin();
                foreach (var target in markedTargets)
                    if (target.Charge != null) { target.Charge.Clear(); target.Charge = null; }
                Phase = SequencePhase.Detonating;
                Trace("RaysOff");
                ExplodeSourceVisual();
                if (pairedMulticolor is ItemMulticolor paired) paired.ExplodeSourceVisual();
                if (doubleBomb && LevelManager.THIS.AdditionalSettings.DoubleMulticolorDestroySquaresAndSpreadJelly)
                {
                    foreach (var cell in barriers.Filter(square, field.squaresArray))
                    {
                        if (cell.type == SquareTypes.SolidBlock) cell.DestroyBlock();
                        if (jellySpread) cell.SetType(SquareTypes.JellyBlock, 1, SquareTypes.NONE, 1);
                    }
                }
                for (int i = 0; i < markedTargets.Count; i++)
                {
                    var target = markedTargets[i];
                    var item = target.Item;
                    Restore(target.Highlight);
                    if (item == null || !item.gameObject.activeSelf || item == pairedMulticolor) continue;
                    Unclaim(item);
                    item.dontDestroyOnThisMove = false;
                    item.dontDestroyForThisCombine = false;
                    item.globalExplEffect = false;
                    item.needFall = false;
                    item.NextType = ItemsTypes.NONE;
                    if (jellySpread) item.square?.SetType(SquareTypes.JellyBlock, 1, SquareTypes.NONE, 1);
                    Trace("Detonate", item);
                    ElectricChargeParticles.Discharge(item.transform.position, field.squareWidth, ElectricPieceColor(item.color));
                    item.DestroyItem(true, true, this, false);
                }
                if (pairedMulticolor is ItemMulticolor other && other.gameObject.activeSelf)
                {
                    Unclaim(other);
                    other.ExplodeMarkedSource();
                }
                Phase = SequencePhase.Finished;
                Trace("SourceDetonate", this);
                completed = true;
                Unclaim(this);
                ClearSelection();
                sequenceManager.FindMatches();
                ExplodeMarkedSource();
            }
            finally
            {
                if (!completed)
                {
                    Phase = SequencePhase.Cancelled;
                    Trace("Cancelled");
                    ClearSelection();
                    if (gameObject.activeSelf) { activated = false; StopDestroy(); }
                }
            }
        }

        void ClearSelection()
        {
            EndSpin();
            if (selectionPulse != null) StopCoroutine(selectionPulse);
            selectionPulse = null;
            foreach (var target in markedTargets)
            {
                Restore(target.Highlight);
                if (target.Charge != null) target.Charge.Clear();
            }
            markedTargets.Clear();
            activeHighlights.Clear();
            foreach (var item in reservations.ToArray()) Unclaim(item);
            reservations.Clear();
            if (sequenceManager != null && sequenceBarrier != null) sequenceManager.animateItems.Remove(sequenceBarrier);
            if (sequenceBarrier != null) UnityEngine.Object.Destroy(sequenceBarrier);
            sequenceBarrier = null;
            pairedMulticolor = null;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (Phase != SequencePhase.Finished && Phase != SequencePhase.Idle)
            {
                Phase = SequencePhase.Cancelled;
                Trace("Cancelled");
            }
            ClearSelection();
        }

        void ExplodeMarkedSource()
        {
            if (IsMulticolorReserved) Unclaim(this);
            ExplodeSourceVisual();
            NextType = ItemsTypes.NONE;
            dontDestroyOnThisMove = false;
            dontDestroyForThisCombine = false;
            DestroyBehaviour();
        }

        void ExplodeSourceVisual()
        {
            if (sourceVisualExploded) return;
            sourceVisualExploded = true;
            ElectricChargeParticles.Flash(transform.position, field.squareWidth * .85f);
            ElectricChargeParticles.Discharge(transform.position, field.squareWidth);
            EndSpin();
            EndAppearance();
            if (anim != null) anim.enabled = false;
            foreach (var renderer in GetSpriteRenderers())
                if (renderer != null && renderer.enabled && renderer.sprite != null) PowerUpEnergyExit.Play(renderer);
            HideSprites(true);
            Trace("SourceVisualDetonate", this);
        }

        const float BeamStepDelay = 0.08f;

        static Color ElectricPieceColor(int color)
        {
            switch (color)
            {
                case 0: return new Color(.7f, .32f, 1f);
                case 1: return new Color(1f, .76f, .16f);
                case 2: return new Color(.35f, 1f, .28f);
                case 3: return new Color(1f, .22f, .25f);
                case 4: return new Color(.2f, .65f, 1f);
                default: return new Color(.4f, .85f, 1f);
            }
        }
        const float PulsePeriod = 0.3f;
        // Neon: a bright inner outline and a wider, softer halo behind it.
        const float OutlineSpread = 1.05f;
        const float HaloSpread = 1.16f;
        const float HaloStrength = 0.3f;
        static readonly Color NeonColor = new Color(0.35f, 0.9f, 1f, 1f);

        // The yellow copies drawn under a selected piece (one per sprite of the piece), and the piece's own orders, so
        // they can be put back when it explodes.
        private class SelectionHighlight
        {
            public readonly List<OutlineCopy> Outlines = new List<OutlineCopy>();
            public readonly List<(SpriteRenderer renderer, int order)> Raised = new List<(SpriteRenderer, int)>();
        }

        private class OutlineCopy
        {
            public SpriteRenderer Renderer;
            public float Spread;
            public float Strength;
        }

        // Each sprite of the piece gets a bigger neon copy under it. Raising the piece one order makes the copy show as an
        // outline that follows the piece's own silhouette.
        private static SelectionHighlight MakeHighlight(Item item)
        {
            var highlight = new SelectionHighlight();
            foreach (var renderer in item.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!renderer.enabled || renderer.sprite == null) continue;
                var order = renderer.sortingOrder;
                highlight.Outlines.Add(MakeCopy(renderer, HaloSpread, HaloStrength, order));
                highlight.Outlines.Add(MakeCopy(renderer, OutlineSpread, 1f, order));
                renderer.sortingOrder = order + 1;
                highlight.Raised.Add((renderer, order));
            }
            return highlight;
        }

        // One shared material: a flat neon silhouette (RoyalAves/NeonSilhouette), so the colour of the piece's art doesn't show.
        static Material neonMaterial;

        private static Material NeonMaterial()
        {
            if (neonMaterial == null)
            {
                var shader = Shader.Find("RoyalAves/NeonSilhouette");
                if (shader == null) Debug.LogWarning("Shader RoyalAves/NeonSilhouette não encontrado; o destaque sai sem neon.");
                neonMaterial = new Material(shader);
                neonMaterial.SetColor("_NeonColor", NeonColor);
            }
            return neonMaterial;
        }

        private static OutlineCopy MakeCopy(SpriteRenderer renderer, float spread, float strength, int order)
        {
            var copy = new GameObject("SelectOutline");
            copy.transform.SetParent(renderer.transform, false);
            var outline = copy.AddComponent<SpriteRenderer>();
            outline.sprite = renderer.sprite;
            outline.flipX = renderer.flipX;
            outline.flipY = renderer.flipY;
            outline.sharedMaterial = NeonMaterial();
            outline.color = Color.white;
            outline.sortingLayerID = renderer.sortingLayerID;
            outline.sortingOrder = order;
            outline.transform.localScale = Vector3.one * spread;
            return new OutlineCopy { Renderer = outline, Spread = spread, Strength = strength };
        }

        // Bright at the start of each pulse, fading but never gone, then bright again.
        private IEnumerator PulseOutlines(List<SelectionHighlight> highlights)
        {
            while (true)
            {
                var phase = (Time.time % PulsePeriod) / PulsePeriod;
                var intensity = Mathf.Lerp(1f, 0.7f, phase);
                foreach (var highlight in highlights)
                    foreach (var outline in highlight.Outlines)
                    {
                        if (outline.Renderer == null) continue;
                        var color = Color.white;
                        color.a = intensity * outline.Strength;
                        outline.Renderer.color = color;
                        outline.Renderer.transform.localScale = Vector3.one * (outline.Spread + 0.06f * intensity);
                    }
                yield return null;
            }
        }

        private static void Restore(SelectionHighlight highlight)
        {
            foreach (var outline in highlight.Outlines)
                if (outline.Renderer != null) Destroy(outline.Renderer.gameObject);
            foreach (var (renderer, order) in highlight.Raised)
                if (renderer != null) renderer.sortingOrder = order;
            highlight.Outlines.Clear();
            highlight.Raised.Clear();
        }

        private Lightning CreateLightning(Vector3 pos1, Vector3 pos2, Transform target = null, bool hold = false)
        {
            var lightning = Lightning.Rent();
            if (lightning == null) return null;
            lightning.SetLight(pos1, pos2, square.field.squareWidth, hold, transform, target);
            electricArcs.Add(lightning);
            return lightning;
        }

        // The bomb spins fast for as long as it's active (from the moment it's touched until it explodes). Started once
        // per activation; EndSpin runs right before the bomb destroys itself, on every path that does so.
        private void BeginSpin()
        {
            EndAppearance();
            ElectricChargeParticles.Flash(transform.position, field.squareWidth);
            sourceCharge = ElectricChargeParticles.Charge(transform, field.squareWidth);
            if (spinRoutine != null || SpinFrames == null || SpinFrames.Length == 0) return;
            finishSpinCycle = false;
            restingSprite = iconRenderer != null ? iconRenderer.sprite : null;
            spinRoutine = StartCoroutine(SpinCor());
        }

        // Marking owns the spin: complete its current revolution before releasing any target.
        private IEnumerator FinishMarkingSpin()
        {
            finishSpinCycle = true;
            while (spinRoutine != null) yield return null;
        }

        private void EndSpin()
        {
            if (sourceCharge != null) { sourceCharge.Clear(); sourceCharge = null; }
            foreach (var arc in electricArcs)
                if (arc != null) arc.Release();
            electricArcs.Clear();
            if (spinRoutine != null) StopCoroutine(spinRoutine);
            spinRoutine = null;
            finishSpinCycle = false;
            if (iconRenderer != null && restingSprite != null) iconRenderer.sprite = restingSprite;
        }

        private IEnumerator SpinCor()
        {
            do
            {
                for (int i = 0; i < SpinFrames.Length; i++)
                {
                    if (iconRenderer != null) iconRenderer.sprite = SpinFrames[i];
                    yield return new WaitForSeconds(SpinFrameTime);
                }
            }
            while (!finishSpinCycle);
            spinRoutine = null;
        }

        public Item GetParentItem()
        {
            return transform.GetComponentInParent<Item>();
        }

        public bool IsExplodable()
        {
            return ActivateByExplosion;
        }

        public void SetOrder(int i)
        {
            var spriteRenderers = GetSpriteRenderers();
            var orderedEnumerable = spriteRenderers.OrderBy(x => x.sortingOrder).ToArray();
            for (int index = 0; index < orderedEnumerable.Length; index++)
            {
                var spr = orderedEnumerable[index];
                spr.sortingOrder = i + index;
            }
        }


    }
}
