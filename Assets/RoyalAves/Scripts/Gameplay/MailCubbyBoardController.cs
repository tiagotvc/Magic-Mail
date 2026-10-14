using System.Collections;
using System.Linq;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Level;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    public sealed class MailCubbyBoardController : MonoBehaviour
    {
        public MailCubbyAnimation Animation { get; private set; }
        public int Collected { get; private set; }
        public Square[] Cells { get; private set; }
        public bool IsBreaking { get; private set; }
        public bool IsCleared { get; private set; }
        LevelManager manager;
        LevelData data;
        float nextImpactTime;
        SweetSugar.Scripts.System.AnimateItems barrier;

        public void Initialize(MailCubbyAnimation animation, Square[] cells)
        {
            Animation = animation;
            Cells = cells;
            manager = LevelManager.THIS;
            data = manager.levelData;
            animation.LetterCollected += OnCollected;
            animation.Emptied += BreakEmptyCabinet;
            var destination = new GameObject("LetterCollectionDestination");
            destination.transform.SetParent(transform, false);
            animation.collectionDestination = destination.transform;
            UpdateDeliveryDestination();
        }

        void UpdateDeliveryDestination()
        {
            var sprite = Animation.letters[0].sprite;
            var gui = FindObjectsByType<SweetSugar.Scripts.TargetScripts.TargetSystem.TargetGUI>(FindObjectsSortMode.None)
                .FirstOrDefault(item => item.image != null && (item.image.sprite == sprite ||
                    (item.subTargetContainer?.extraObjects != null && item.subTargetContainer.extraObjects.Contains(sprite))));
            if (gui == null)
            {
                Animation.collectionDestination.position = transform.position + Vector3.up * 2.5f;
                return;
            }
            var image = gui.image.rectTransform;
            var position = image.TransformPoint(image.rect.center);
            var canvas = image.GetComponentInParent<Canvas>();
            var camera = Camera.main;
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace && camera != null)
            {
                var screen = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, position);
                position = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Abs(camera.transform.position.z - transform.position.z)));
            }
            position.z = transform.position.z;
            Animation.collectionDestination.position = position;
        }

        public bool AcceptImpact(bool manual = false)
        {
            if (Animation == null || Animation.Remaining == 0 || manager == null || manager.levelData != data) return false;
            // Mirrors the board's short damage window: hitting several parts of this 2x2
            // in the same burst emits one letter. Activation IDs remain a production task.
            if (!manual && Time.time < nextImpactTime) return false;
            UpdateDeliveryDestination();
            if (!Animation.PlayAcceptedHit()) return false;
            nextImpactTime = Time.time + 0.2f;
            if (barrier == null)
            {
                barrier = gameObject.AddComponent<SweetSugar.Scripts.System.AnimateItems>();
                barrier.target = true;
                barrier.linkObjectHash = gameObject.GetHashCode();
                manager.animateItems.Add(barrier);
                StartCoroutine(WaitForEmission());
            }
            return true;
        }

        IEnumerator WaitForEmission()
        {
            while (Animation != null && (Animation.IsAnimating || IsBreaking)) yield return null;
            ReleaseBarrier();
            if (manager != null && manager.levelData == data) manager.CheckWinLose();
        }

        void OnCollected()
        {
            if (manager == null || manager.levelData != data) return;
            Collected++;
            var collectedCell = Cells[Collected - 1].GetSubSquare().GetComponent<MailCubbyCell>();
            if (collectedCell != null) collectedCell.CountsAsObjective = false;
            foreach (var target in data.TargetCounters)
                if (target.extraObjects != null && target.extraObjects.Contains(Animation.letters[0].sprite))
                    target.changeCount(-1);
        }

        void BreakEmptyCabinet()
        {
            if (IsBreaking || IsCleared || manager == null || manager.levelData != data) return;
            IsBreaking = true;
            Animation.counter.gameObject.SetActive(false);
            RoyalAves.Effects.ParcelTearEffect.Play(Animation.cabinet, manager, () =>
            {
                if (manager == null || manager.levelData != data || this == null) return;
                foreach (var cell in Cells)
                {
                    if (cell == null) continue;
                    var layer = cell.GetSubSquare();
                    if (layer.GetComponent<MailCubbyCell>() != null) cell.CompleteLayerRemoval(layer);
                }
                IsBreaking = false;
                IsCleared = true;
#if UNITY_EDITOR
                System.IO.File.WriteAllText("Logs/mail-cubby-completion.result", $"OK: cartas={Collected}; meta={data.TargetCounters.Sum(t => t.count)}; células livres={Cells.Count(cell => cell != null && cell.IsFree())}; quebra concluída.");
#endif
            });
            Animation.cabinet.enabled = false;
        }

        void ReleaseBarrier()
        {
            if (manager != null && barrier != null) manager.animateItems.Remove(barrier);
            if (barrier != null) Destroy(barrier);
            barrier = null;
        }

        void OnDestroy()
        {
            if (Animation != null) Animation.LetterCollected -= OnCollected;
            if (Animation != null) Animation.Emptied -= BreakEmptyCabinet;
            ReleaseBarrier();
        }
    }
}
