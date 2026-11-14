using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    public sealed class MailCubbyAnimation : MonoBehaviour
    {
        public const float DoorFallSpeedMultiplier = 1.25f;
        public SpriteRenderer cabinet;
        public Rect cabinetVisibleBounds;
        public SpriteRenderer[] letters;
        public int[] lettersPerHit = { MailCubbyCell.LettersPerHit, MailCubbyCell.LettersPerHit,
            MailCubbyCell.LettersPerHit, MailCubbyCell.LettersPerHit };
        public SpriteRenderer[] doors;
        public bool DoorsAttached { get; private set; }
        public TextMesh counter;
        public Transform collectionDestination;
        [NonSerialized] public SweetSugar.Scripts.TargetScripts.TargetSystem.TargetGUI collectionTarget;
        [Min(0.1f)] public float emissionDuration = 0.95f;
        [Min(0f)] public float letterStagger = 0.12f;
        [Min(0.1f)] public float playbackSpeed = 1f;
        public float PlaybackSpeed => Mathf.Max(0.1f, playbackSpeed);
        public float launchDuration;
        public Vector3 launchDisplacement;
        public float launchStartYScale = 1f;
        public bool drawOverHud;
        public bool useCabinetDelivery;
        public bool SuppressSounds { get; set; }
        [Min(1f)] public float flyingLetterScale = 1f;
        readonly List<RoyalAves.Effects.MailDeliveryOverlay> activeOverlays = new List<RoyalAves.Effects.MailDeliveryOverlay>();
        readonly List<RoyalAves.Effects.MailDeliveryTrail> activeTrails = new List<RoyalAves.Effects.MailDeliveryTrail>();
        public int Remaining { get; private set; }
        public int NichesRemaining => Mathf.Max(0, (lettersPerHit.Length - completedHits + 1) / 2);
        public bool CanAcceptHit => RemainingNichesAndDoor - pendingHits > 0;
        public bool IsAnimating { get; private set; }
        public int CompletedLetters { get; private set; }
        public event Action LetterCollected;
        public event Action Emptied;
        Vector3[] positions;
        Quaternion[] rotations;
        Vector3[] scales;
        string[] sortingLayers;
        int[] sortingOrders;
        Color[] restingColors;
        Vector3[] doorPositions;
        Quaternion[] doorRotations;
        Vector3[] doorScales;
        Vector3 restScale;
        int pendingHits;
        int completedHits;
        int activeFlights;
        bool[] departed;
        int[] restingSlots;
        bool settling;
        int RemainingNichesAndDoor => lettersPerHit.Length - completedHits + (DoorsAttached ? 1 : 0);

        void Awake()
        {
            restScale = cabinet.transform.localScale;
            doors = doors ?? new SpriteRenderer[0];
            NormalizeNicheCounts();
            positions = new Vector3[letters.Length];
            rotations = new Quaternion[letters.Length];
            scales = new Vector3[letters.Length];
            sortingLayers = new string[letters.Length];
            sortingOrders = new int[letters.Length];
            restingColors = new Color[letters.Length];
            departed = new bool[letters.Length];
            restingSlots = new int[letters.Length];
            doorPositions = new Vector3[doors.Length];
            doorRotations = new Quaternion[doors.Length];
            doorScales = new Vector3[doors.Length];
            for (int i = 0; i < doors.Length; i++)
            {
                doorPositions[i] = doors[i].transform.localPosition;
                doorRotations[i] = doors[i].transform.localRotation;
                doorScales[i] = doors[i].transform.localScale;
            }
            for (int i = 0; i < letters.Length; i++)
            {
                positions[i] = letters[i].transform.localPosition;
                rotations[i] = letters[i].transform.localRotation;
                scales[i] = letters[i].transform.localScale;
                sortingLayers[i] = letters[i].sortingLayerName;
                sortingOrders[i] = letters[i].sortingOrder;
                restingColors[i] = letters[i].color;
            }
            ResetStock();
        }

        void NormalizeNicheCounts()
        {
            if (lettersPerHit != null && lettersPerHit.Length == 4 && Sum(lettersPerHit) == letters.Length) return;
            lettersPerHit = new int[4];
            int baseCount = letters.Length / 4;
            int remainder = letters.Length % 4;
            for (int i = 0; i < lettersPerHit.Length; i++) lettersPerHit[i] = baseCount + (i < remainder ? 1 : 0);
        }

        static int Sum(int[] values)
        {
            int result = 0;
            if (values != null) foreach (int value in values) result += Mathf.Max(0, value);
            return result;
        }

        public void ResetStock()
        {
            StopAllCoroutines();
            ClearDeliveryEffects();
            IsAnimating = false;
            pendingHits = 0;
            activeFlights = 0;
            completedHits = 0;
            settling = false;
            CompletedLetters = 0;
            Remaining = letters.Length;
            DoorsAttached = doors.Length > 0;
            for (int i = 0; i < doors.Length; i++)
            {
                doors[i].enabled = true;
                doors[i].color = Color.white;
                doors[i].transform.localPosition = doorPositions[i];
                doors[i].transform.localRotation = doorRotations[i];
                doors[i].transform.localScale = doorScales[i];
            }
            cabinet.transform.localScale = restScale;
            for (int i = 0; i < letters.Length; i++)
            {
                departed[i] = false;
                restingSlots[i] = i;
                letters[i].enabled = true;
                letters[i].color = restingColors[i];
                letters[i].transform.localPosition = positions[i];
                letters[i].transform.localRotation = rotations[i];
                letters[i].transform.localScale = scales[i];
                letters[i].sortingOrder = sortingOrders[i];
                letters[i].sortingLayerName = sortingLayers[i];
            }
            UpdateCounter();
        }

        void ClearDeliveryEffects()
        {
            foreach (var overlay in activeOverlays) if (overlay != null) Destroy(overlay.gameObject);
            foreach (var trail in activeTrails) if (trail != null) Destroy(trail.gameObject);
            activeOverlays.Clear();
            activeTrails.Clear();
        }

        public bool PlayAcceptedHit()
        {
            if (!CanAcceptHit) return false;
            pendingHits++;
            if (!IsAnimating) StartCoroutine(EmitQueue());
            return true;
        }

        IEnumerator EmitQueue()
        {
            IsAnimating = true;
            while (pendingHits > 0 && RemainingNichesAndDoor > 0)
            {
                pendingHits--;
                if (DoorsAttached)
                {
                    DoorsAttached = false;
                    yield return RemoveDoors();
                    continue;
                }

                int hitStage = completedHits++;
                int firstLetter = 0;
                for (int i = 0; i < hitStage; i++) firstLetter += lettersPerHit[i];
                int count = lettersPerHit[hitStage];
                Remaining -= count;
                UpdateCounter();
                yield return ReactToNicheHit();

                for (int i = 0; i < count; i++)
                {
                    departed[firstLetter + i] = true;
                    StartCoroutine(FlyLetter(letters[firstLetter + i], i));
                    if (useCabinetDelivery) CompactStacks();
                    if (i + 1 < count && letterStagger > 0f) yield return new WaitForSeconds(letterStagger / PlaybackSpeed);
                }
                while (activeFlights > 0 || settling) yield return null;
                if (completedHits == lettersPerHit.Length) Emptied?.Invoke();
            }
            IsAnimating = false;
        }

        IEnumerator ReactToNicheHit()
        {
            if (!SuppressSounds) RoyalAves.Effects.ParcelSoundEffects.Play(1);
            float elapsed = 0f;
            const float duration = 0.13f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime * PlaybackSpeed;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI) * (useCabinetDelivery ? 0.02f : 0.055f);
                cabinet.transform.localScale = Vector3.Scale(restScale, new Vector3(1f + pulse, 1f - pulse * 0.65f, 1f));
                yield return null;
            }
            cabinet.transform.localScale = restScale;
        }

        IEnumerator FlyLetter(SpriteRenderer letter, int staggerIndex)
        {
            activeFlights++;
            letter.enabled = true;
            letter.sortingLayerName = RoyalAves.Effects.PowerUpEffectOrder.SortingLayer;
            letter.sortingOrder = RoyalAves.Effects.PowerUpEffectOrder.Value + 8 + staggerIndex;
            Vector3 fullScale = (useCabinetDelivery ? letter.transform.localScale : scales[Array.IndexOf(letters, letter)]) * flyingLetterScale;
            if (launchDuration > 0f) yield return LaunchLetter(letter, fullScale);
            Vector3 start = letter.transform.position;
            Vector3 end = collectionDestination != null
                ? collectionDestination.position
                : transform.TransformPoint(new Vector3(0.9f, 1.35f, 0f));
            Vector3 lift = start + Vector3.up * 0.65f;
            Vector3 bend = Vector3.Lerp(start, end, 0.55f) + Vector3.up * (0.7f + staggerIndex * 0.08f);
            if (useCabinetDelivery)
            {
                float unit = cabinet.bounds.size.x * .5f;
                float fan = (staggerIndex % 3 - 1) * unit * .12f;
                lift = start + Vector3.up * unit * .45f + Vector3.right * fan;
                bend = Vector3.Lerp(start, end, .68f) + Vector3.up * unit * .25f + Vector3.right * fan;
            }
            RoyalAves.Effects.MailDeliveryOverlay overlay = null;
            RoyalAves.Effects.MailDeliveryTrail trail = null;
            if (drawOverHud)
            {
                overlay = RoyalAves.Effects.MailDeliveryOverlay.Create(letter, collectionTarget, trail: !useCabinetDelivery);
                if (overlay != null) activeOverlays.Add(overlay);
            }
            if (overlay == null)
            {
                trail = RoyalAves.Effects.MailDeliveryTrail.Create(letter);
                if (trail != null) activeTrails.Add(trail);
                if (trail != null && useCabinetDelivery)
                {
                    var ribbon = trail.GetComponent<TrailRenderer>();
                    ribbon.widthMultiplier *= .45f;
                    ribbon.time = .13f;
                }
            }
            float elapsed = 0f;
            while (elapsed < emissionDuration)
            {
                elapsed += Time.deltaTime * PlaybackSpeed;
                float t = Mathf.Clamp01(elapsed / emissionDuration);
                float u = useCabinetDelivery || launchDuration > 0f ? Mathf.SmoothStep(0f, 1f, t) : t * t;
                float v = 1f - u;
                letter.transform.position = v * v * v * start + 3 * v * v * u * lift + 3 * v * u * u * bend + u * u * u * end;
                float spinProgress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(launchDuration > 0f ? 0.28f : 0.08f, launchDuration > 0f ? 0.88f : 0.68f, t));
                letter.transform.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(8f, -18f, u) - 360f * spinProgress);
                if (useCabinetDelivery)
                {
                    // A gentle bank follows the arc; envelopes stay readable throughout delivery.
                    Vector3 tangent = 3f * v * v * (lift - start) + 6f * v * u * (bend - lift) + 3f * u * u * (end - bend);
                    float bank = Mathf.Clamp(-tangent.x / Mathf.Max(.1f, tangent.magnitude) * 22f, -22f, 22f);
                    letter.transform.rotation = Quaternion.Euler(0f, 0f, bank * Mathf.Sin(t * Mathf.PI) + Mathf.Sin(t * Mathf.PI * 2f) * 3f);
                }
                float arrival = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.82f, 1f, t));
                letter.transform.localScale = fullScale * (1f + Mathf.Sin(t * Mathf.PI) * 0.1f) * Mathf.Lerp(1f, 0.55f, arrival);
                if (launchDuration > 0f)
                {
                    float liftProgress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.18f, t));
                    letter.transform.localScale = Vector3.Scale(letter.transform.localScale, new Vector3(1f, Mathf.Lerp(launchStartYScale, 1f, liftProgress), 1f));
                }
                letter.color = Color.white;
                trail?.Follow(letter.transform.position);
                overlay?.Follow();
                yield return null;
            }
            letter.enabled = false;
            trail?.Finish();
            overlay?.Complete();
            if (trail != null) activeTrails.Remove(trail);
            if (overlay != null) activeOverlays.Remove(overlay);
            CompletedLetters++;
            LetterCollected?.Invoke();
            activeFlights--;
        }

        void CompactStacks()
        {
            for (int niche = 0; niche < MailCubbyCell.NichesPerCabinet; niche++)
                for (int column = 0; column < 3; column++)
                {
                    int rank = 0;
                    int first = niche * MailCubbyCell.LettersPerNiche;
                    for (int depth = 0; depth < MailCubbyCell.LettersPerNiche / 3; depth++)
                    {
                        int index = first + depth * 3 + column;
                        if (departed[index]) continue;
                        restingSlots[index] = first + rank++ * 3 + column;
                    }
                }
            settling = true;
        }

        void LateUpdate()
        {
            if (!useCabinetDelivery || !settling || positions == null) return;
            bool moving = false;
            float blend = 1f - Mathf.Exp(-26f * Time.deltaTime * PlaybackSpeed);
            for (int i = 0; i < letters.Length; i++)
            {
                if (departed[i]) continue;
                int slot = restingSlots[i];
                var letter = letters[i];
                letter.transform.localPosition = Vector3.Lerp(letter.transform.localPosition, positions[slot], blend);
                letter.transform.localScale = Vector3.Lerp(letter.transform.localScale, scales[slot], blend);
                letter.color = Color.Lerp(letter.color, restingColors[slot], blend);
                letter.sortingOrder = sortingOrders[slot];
                if ((letter.transform.localPosition - positions[slot]).sqrMagnitude > .000001f ||
                    (letter.transform.localScale - scales[slot]).sqrMagnitude > .000001f) moving = true;
                else
                {
                    letter.transform.localPosition = positions[slot];
                    letter.transform.localScale = scales[slot];
                    letter.color = restingColors[slot];
                }
            }
            settling = moving;
        }

        IEnumerator RemoveDoors()
        {
            // Wood breaking, not the parcel seal pop: the doors are torn off the cabinet.
            if (!SuppressSounds) RoyalAves.Effects.MailCubbySounds.PlayDoors();
            for (int i = 0; i < doors.Length; i++)
            {
                RoyalAves.Effects.FallingBoardDebris.DropDoor(doors[i], i == 0 ? -1f : 1f, PlaybackSpeed * DoorFallSpeedMultiplier);
                doors[i].enabled = false;
            }
            float elapsed = 0f;
            const float duration = 0.18f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime * PlaybackSpeed;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI * 2f) * (1f - t) * 0.025f;
                cabinet.transform.localScale = Vector3.Scale(restScale, new Vector3(1f + pulse, 1f - pulse, 1f));
                yield return null;
            }
            foreach (var door in doors) door.enabled = false;
            cabinet.transform.localScale = restScale;
        }

        IEnumerator LaunchLetter(SpriteRenderer letter, Vector3 fullScale)
        {
            var origin = letter.transform.localPosition;
            letter.transform.localRotation = Quaternion.identity;
            float elapsed = 0f;
            while (elapsed < launchDuration)
            {
                elapsed += Time.deltaTime * PlaybackSpeed;
                float t = Mathf.Clamp01(elapsed / launchDuration);
                float slide = Mathf.SmoothStep(0f, 1f, t);
                letter.transform.localPosition = origin + launchDisplacement * slide;
                letter.transform.localScale = Vector3.Scale(fullScale, new Vector3(1f, launchStartYScale, 1f));
                yield return null;
            }
        }

        void UpdateCounter() { if (counter != null) counter.text = Remaining.ToString(); }

        void OnDisable()
        {
            StopAllCoroutines();
            ClearDeliveryEffects();
            pendingHits = 0;
            activeFlights = 0;
            settling = false;
            IsAnimating = false;
        }
    }
}
