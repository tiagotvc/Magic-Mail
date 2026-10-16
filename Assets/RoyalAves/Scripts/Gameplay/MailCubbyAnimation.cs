using System;
using System.Collections;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    // Visual controller only. The board must forward one accepted impact per activation.
    public sealed class MailCubbyAnimation : MonoBehaviour
    {
        public SpriteRenderer cabinet;
        public Rect cabinetVisibleBounds;
        public SpriteRenderer[] letters;
        public SpriteRenderer[] doors;
        public bool DoorsAttached { get; private set; }
        public TextMesh counter;
        public Transform collectionDestination;
        [Min(0.1f)] public float emissionDuration = 0.95f;
        public float launchDuration;
        public Vector3 launchDisplacement;
        public float launchStartYScale = 1f;
        public bool drawOverHud;
        RoyalAves.Effects.MailDeliveryOverlay activeOverlay;
        public int Remaining { get; private set; }
        public bool IsAnimating { get; private set; }
        public event Action LetterCollected;
        public event Action Emptied;
        Vector3[] positions;
        Quaternion[] rotations;
        Vector3[] scales;
        string[] sortingLayers;
        Vector3[] doorPositions;
        Quaternion[] doorRotations;
        Vector3[] doorScales;
        RoyalAves.Effects.MailDeliveryTrail activeTrail;
        Vector3 restScale;
        int pendingHits;

        void Awake()
        {
            restScale = cabinet.transform.localScale;
            positions = new Vector3[letters.Length];
            rotations = new Quaternion[letters.Length];
            scales = new Vector3[letters.Length];
            sortingLayers = new string[letters.Length];
            doors = doors ?? new SpriteRenderer[0];
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
            }
            ResetStock();
        }

        public void ResetStock()
        {
            StopAllCoroutines();
            if (activeTrail != null) Destroy(activeTrail.gameObject);
            if (activeOverlay != null) Destroy(activeOverlay.gameObject);
            IsAnimating = false;
            pendingHits = 0;
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
                letters[i].color = Color.white;
                letters[i].transform.localPosition = positions[i];
                letters[i].transform.localRotation = rotations[i];
                letters[i].transform.localScale = scales[i];
                letters[i].sortingOrder = cabinet.sortingOrder + 1;
                letters[i].sortingLayerName = sortingLayers[i];
            }
            UpdateCounter();
        }

        public bool PlayAcceptedHit()
        {
            if (Remaining + (DoorsAttached ? 1 : 0) - pendingHits <= 0) return false;
            pendingHits++;
            if (!IsAnimating) StartCoroutine(EmitQueue());
            return true;
        }

        IEnumerator EmitQueue()
        {
            IsAnimating = true;
            while (pendingHits > 0 && Remaining > 0)
            {
                pendingHits--;
                if (DoorsAttached)
                {
                    // This hit is entirely absorbed by the doors. No letter or objective is collected.
                    DoorsAttached = false;
                    yield return RemoveDoors();
                    continue;
                }
                var letter = letters[Remaining - 1];
                Remaining--;
                UpdateCounter();
                letter.sortingOrder = cabinet.sortingOrder + 3;
                letter.sortingLayerName = RoyalAves.Effects.PowerUpEffectOrder.SortingLayer;
                letter.sortingOrder = RoyalAves.Effects.PowerUpEffectOrder.Value + 8;
                if (launchDuration > 0f) yield return LaunchLetter(letter, scales[Remaining]);
                Vector3 start = letter.transform.position;
                Vector3 end = collectionDestination != null
                    ? collectionDestination.position
                    : transform.TransformPoint(new Vector3(0.9f, 1.35f, 0f));
                Vector3 lift = start + Vector3.up * 0.65f;
                Vector3 bend = Vector3.Lerp(start, end, 0.55f) + Vector3.up * 0.7f;
                Vector3 letterScale = scales[Remaining];
                if (drawOverHud) activeOverlay = RoyalAves.Effects.MailDeliveryOverlay.Create(letter);
                if (activeOverlay == null) activeTrail = RoyalAves.Effects.MailDeliveryTrail.Create(letter);
                float elapsed = 0f;
                while (elapsed < emissionDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / emissionDuration);
                    float u = launchDuration > 0f ? Mathf.SmoothStep(0f, 1f, t) : t * t;
                    float v = 1f - u;
                    letter.transform.position = v*v*v*start + 3*v*v*u*lift + 3*v*u*u*bend + u*u*u*end;
                    // One playful turn during takeoff, then settle into the delivery tilt.
                    float spinProgress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(launchDuration > 0f ? 0.28f : 0.08f, launchDuration > 0f ? 0.88f : 0.68f, t));
                    letter.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(8f, -18f, u) - 360f * spinProgress);
                    float arrival = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.82f, 1f, t));
                    letter.transform.localScale = letterScale * (1f + Mathf.Sin(t * Mathf.PI) * 0.1f) * Mathf.Lerp(1f, 0.55f, arrival);
                    if (launchDuration > 0f)
                    {
                        float liftProgress = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.18f, t));
                        letter.transform.localScale = Vector3.Scale(letter.transform.localScale, new Vector3(1f, Mathf.Lerp(launchStartYScale, 1f, liftProgress), 1f));
                    }
                    letter.color = Color.white;
                    activeTrail?.Follow(letter.transform.position);
                    activeOverlay?.Follow();
                    float pulse = Mathf.Sin(Mathf.Min(t / 0.3f, 1f) * Mathf.PI) * 0.025f;
                    cabinet.transform.localScale = Vector3.Scale(restScale, new Vector3(1+pulse, 1-pulse, 1));
                    yield return null;
                }
                letter.enabled = false;
                activeTrail?.Finish();
                activeTrail = null;
                activeOverlay?.Complete();
                activeOverlay = null;
                cabinet.transform.localScale = restScale;
                LetterCollected?.Invoke();
                if (Remaining == 0) Emptied?.Invoke();
            }
            IsAnimating = false;
        }

        IEnumerator RemoveDoors()
        {
            RoyalAves.Effects.ParcelSoundEffects.Play(4);
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
                cabinet.transform.localScale = Vector3.Scale(restScale, new Vector3(1f + pulse, 1f - pulse, 1));
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
                // The envelope stays level with the mouth until it has slid completely out.
                letter.transform.localScale = Vector3.Scale(fullScale, new Vector3(1f, launchStartYScale, 1f));
                yield return null;
            }
        }

        void UpdateCounter() { if (counter != null) counter.text = Remaining.ToString(); }

        void OnDisable()
        {
            StopAllCoroutines();
            if (activeTrail != null) Destroy(activeTrail.gameObject);
            if (activeOverlay != null) Destroy(activeOverlay.gameObject);
            pendingHits = 0;
            IsAnimating = false;
        }
    }
}
