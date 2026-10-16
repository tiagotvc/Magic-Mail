using System.Collections;
using System.Linq;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.System;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    public sealed class FrankingMachineCell : MonoBehaviour
    {
        public GameObject visualPrefab;
        public MailCubbyAnimation Animation { get; private set; }
        public int Collected { get; private set; }
        public bool IsBreaking => false;
        public bool IsCleared => false;
        public Square[] Cells => new[] { cell };
        Square cell;
        LevelManager manager;
        LevelData data;
        AnimateItems barrier;
        float nextImpact;

        void Start()
        {
            cell = transform.parent != null ? transform.parent.GetComponent<Square>() : null;
            if (cell == null) return;
            manager = LevelManager.THIS;
            data = manager.levelData;
            var root = Instantiate(visualPrefab, cell.transform, false);
            root.transform.localPosition = new Vector3(0, 0, -0.6f);
            Animation = root.GetComponent<MailCubbyAnimation>();
            Animation.letters[0].enabled = false;
            Animation.LetterCollected += Collect;
            var destination = new GameObject("MachineLetterDestination");
            destination.transform.SetParent(root.transform, false);
            Animation.collectionDestination = destination.transform;
        }

        public void HandleHit() => AcceptImpact();
        public bool AcceptImpact(bool manual = false)
        {
            if (Animation == null || Animation.IsAnimating || barrier != null || manager.levelData != data || data.TargetCounters.All(t => t.count <= 0)) return false;
            if (!manual && Time.time < nextImpact) return false;
            nextImpact = Time.time + 0.2f;
            Animation.ResetStock();
            Animation.letters[0].enabled = false;
            var gui = FindObjectsByType<TargetGUI>(FindObjectsSortMode.None).FirstOrDefault(t => t.image != null && t.image.sprite == Animation.letters[0].sprite);
            Vector3 position = gui != null ? gui.image.rectTransform.TransformPoint(gui.image.rectTransform.rect.center) : cell.transform.position + Vector3.up * 2.5f;
            var canvas = gui != null ? gui.GetComponentInParent<Canvas>() : null;
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace && Camera.main != null)
            {
                var screen = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, position);
                position = Camera.main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Abs(Camera.main.transform.position.z - cell.transform.position.z)));
            }
            position.z = Animation.transform.position.z;
            Animation.collectionDestination.position = position;
            barrier = gameObject.AddComponent<AnimateItems>();
            barrier.target = true;
            manager.animateItems.Add(barrier);
            StartCoroutine(Activate());
            return true;
        }

        IEnumerator Activate()
        {
            StartCoroutine(Blob());
            var mechanism = Animation.GetComponent<FrankingMachineLever>();
            if (mechanism != null) yield return mechanism.Pull();
            Animation.letters[0].enabled = true;
            Animation.PlayAcceptedHit();
            // Return the lever after the straight ejection, while the letter continues its flight.
            if (mechanism != null)
            {
                yield return new WaitForSeconds(Animation.launchDuration);
                yield return mechanism.Return();
            }
            while (Animation.IsAnimating) yield return null;
            Release();
            if (manager != null && manager.levelData == data) manager.CheckWinLose();
        }

        IEnumerator Blob()
        {
            var root = Animation.transform;
            var scale = root.localScale;
            var position = root.localPosition;
            // Keep the foot planted while the whole machine absorbs the impact.
            var foot = root.InverseTransformPoint(new Vector3(Animation.cabinet.bounds.center.x,
                Animation.cabinet.bounds.min.y, root.position.z));
            const float compressionTime = 0.055f;
            const float reboundTime = 0.365f;
            float elapsed = 0f;
            try
            {
                while (elapsed < compressionTime + reboundTime)
                {
                    elapsed += Time.deltaTime;
                    float pulse;
                    if (elapsed < compressionTime)
                        pulse = Mathf.Sin(elapsed / compressionTime * Mathf.PI * 0.5f);
                    else
                    {
                        float t = Mathf.Clamp01((elapsed - compressionTime) / reboundTime);
                        // A broad rebound, then a smaller settling bounce instead of a rapid shake.
                        pulse = Mathf.Cos(t * Mathf.PI * 3f) * Mathf.Exp(-2.5f * t) * (1f - t * t);
                    }
                    root.localScale = Vector3.Scale(scale, new Vector3(1f + pulse * 0.28f, 1f - pulse * 0.24f, 1f));
                    root.localPosition = position + root.localRotation * Vector3.Scale(foot, scale - root.localScale);
                    yield return null;
                }
            }
            finally
            {
                if (root != null)
                {
                    root.localScale = scale;
                    root.localPosition = position;
                }
            }
        }

        void Collect()
        {
            if (manager == null || manager.levelData != data) return;
            Collected++;
            foreach (var target in data.TargetCounters)
                if (target.extraObjects.Contains(Animation.letters[0].sprite) && target.count > 0) target.changeCount(-1);
#if UNITY_EDITOR
            System.IO.File.WriteAllText("Logs/franking-machine-collection.result", $"OK: emitidas={Collected}; meta={data.TargetCounters.Sum(t => t.count)}; máquina fixa={cell.IsObstacle()}.");
#endif
        }

        void LateUpdate() { GetComponent<SpriteRenderer>().enabled = false; }
        void Release() { if (manager != null && barrier != null) manager.animateItems.Remove(barrier); if (barrier != null) Destroy(barrier); barrier = null; }
        void OnDestroy() { if (Animation != null) Animation.LetterCollected -= Collect; Release(); }
    }
}
