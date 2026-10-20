using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    public sealed class MailCubbyAnimation : MonoBehaviour
    {
        public SpriteRenderer cabinet;
        public Rect cabinetVisibleBounds;
        public SpriteRenderer[] letters;
        public int[] lettersPerHit = { MailCubbyCell.LettersPerHit, MailCubbyCell.LettersPerHit,
            MailCubbyCell.LettersPerHit, MailCubbyCell.LettersPerHit };
        public SpriteRenderer[] doors;
        public bool DoorsAttached { get; private set; }
        public TextMesh counter;
        public Transform collectionDestination;
        [Min(0.1f)] public float emissionDuration = 0.95f;
        [Min(0f)] public float letterStagger = 0.12f;
        public float launchDuration;
        public Vector3 launchDisplacement;
        public float launchStartYScale = 1f;
        public bool drawOverHud;
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
                    StartCoroutine(FlyLetter(letters[firstLetter + i], i));
                    if (i + 1 < count && letterStagger > 0f) yield return new WaitForSeconds(letterStagger);
                }
                while (activeFlights > 0) yield return null;
                if (completedHits == lettersPerHit.Length) Emptied?.Invoke();
            }
            IsAnimating = false;
        }

        IEnumerator ReactToNicheHit()
        {
            RoyalAves.Effects.ParcelSoundEffects.Play(1);
            float elapsed = 0f;
            const float duration = 0.13f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(t * Mathf.PI) * 0.055f;
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
            Vector3 fullScale = scales[Array.IndexOf(letters, letter)] * flyingLetterScale;
            if (launchDuration > 0f) yield return LaunchLetter(letter, fullScale);
            Vector3 start = letter.transform.position;
            Vector3 end = collectionDestination != null
                ? collectionDestination.position
                : transform.TransformPoint(new Vector3(0.9f, 1.35f, 0f));
            Vector3 lift = start + Vector3.up * 0.65f;
            Vector3 bend = Vector3.Lerp(start, end, 0.55f) + Vector3.up * (0.7f + staggerIndex * 0.08f);
            RoyalAves.Effects.MailDeliveryOverlay overlay = null;
            RoyalAves.Effects.MailDeliveryTrail trail = null;
            if (drawOverHud)
            {
                overlay = RoyalAves.Effects.MailDeliveryOverlay.Create(letter);
                if (overlay != null) activeOverlays.Add(overlay);
            }
            if (overlay == null)
            {
                trail = RoyalAves.Effects.MailDeliveryTrail.Create(letter);
                if (trail != null) activeTrails.Add(trail);
            }
            float elapsed = 0f;
            while (elapsed < emissionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / emissionDuration);
                float u = launchDuration > 0f ? Mathf.SmoothStep(0f, 1f, t) : t * t;
                float v = 1f - u;
                letter.transform.position = v * v * v * start + 3 * v * v * u * lift + 3 * v * u * u * bend + u * u * u * end;
                float spinProgress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(launchDuration > 0f ? 0.28f : 0.08f, launchDuration > 0f ? 0.88f : 0.68f, t));
                letter.transform.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(8f, -18f, u) - 360f * spinProgress);
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

        IEnumerator RemoveDoors()
        {
            // Wood breaking, not the parcel seal pop: the doors are torn off the cabinet.
            RoyalAves.Effects.MailCubbySounds.PlayDoors();
            for (int i = 0; i < doors.Length; i++)
            {
                RoyalAves.Effects.FallingBoardDebris.DropDoor(doors[i], i == 0 ? -1f : 1f);
                doors[i].enabled = false;
            }
            float elapsed = 0f;
            const float duration = 0.18f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
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
                elapsed += Time.deltaTime;
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
            IsAnimating = false;
        }
    }
}
