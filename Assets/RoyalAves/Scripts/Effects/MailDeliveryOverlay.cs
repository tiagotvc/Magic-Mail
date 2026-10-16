using System.Collections.Generic;
using System.Linq;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Effects
{
    // Delivery sprites share the HUD's coordinate system and draw above its panels.
    public sealed class MailDeliveryOverlay : MonoBehaviour
    {
        Image letter;
        SpriteRenderer source;
        Canvas hud;
        RectTransform area;
        Vector2 previous;
        bool hasPrevious;
        readonly List<Image> wake = new List<Image>();
        readonly List<float> born = new List<float>();

        public static MailDeliveryOverlay Create(SpriteRenderer source)
        {
            var target = FindObjectsByType<TargetGUI>(FindObjectsSortMode.None).FirstOrDefault(t => t.image != null && t.image.sprite == source.sprite);
            var hud = target != null ? target.GetComponentInParent<Canvas>()?.rootCanvas : null;
            if (hud == null || Camera.main == null) return null;
            var root = new GameObject("LetterDeliveryAboveHUD", typeof(RectTransform), typeof(Canvas));
            root.transform.SetParent(hud.transform, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var canvas = root.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32700;
            canvas.sortingLayerID = hud.sortingLayerID;
            var effect = root.AddComponent<MailDeliveryOverlay>();
            effect.source = source; effect.hud = hud; effect.area = rect;
            var imageRoot = new GameObject("DeliveryLetter", typeof(RectTransform), typeof(Image));
            imageRoot.transform.SetParent(root.transform, false);
            effect.letter = imageRoot.GetComponent<Image>();
            effect.letter.sprite = source.sprite;
            effect.letter.raycastTarget = false;
            source.enabled = false;
            effect.Follow();
            return effect;
        }

        public void Follow()
        {
            var camera = Camera.main;
            if (source == null || camera == null) return;
            Vector2 screen = camera.WorldToScreenPoint(source.transform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen,
                hud.renderMode == RenderMode.ScreenSpaceOverlay ? null : hud.worldCamera, out var point);
            var rect = letter.rectTransform;
            rect.anchoredPosition = point;
            rect.localRotation = Quaternion.Euler(0, 0, source.transform.eulerAngles.z);
            var worldSize = Vector3.Scale(source.sprite.bounds.size, source.transform.lossyScale);
            var right = camera.WorldToScreenPoint(source.transform.position + Vector3.right * Mathf.Abs(worldSize.x));
            var top = camera.WorldToScreenPoint(source.transform.position + Vector3.up * Mathf.Abs(worldSize.y));
            rect.sizeDelta = new Vector2(Mathf.Abs(right.x - screen.x), Mathf.Abs(top.y - screen.y)) / hud.scaleFactor;
            if (hasPrevious && Vector2.Distance(previous, point) > 1f)
            {
                var node = new GameObject("DeliveryWake", typeof(RectTransform), typeof(Image));
                node.transform.SetParent(transform, false);
                node.transform.SetAsFirstSibling();
                var image = node.GetComponent<Image>(); image.raycastTarget = false;
                image.color = new Color(1f, 0.88f, 0.48f, 0.65f);
                var delta = point - previous;
                image.rectTransform.anchoredPosition = (point + previous) * 0.5f;
                image.rectTransform.sizeDelta = new Vector2(delta.magnitude + 2f, 3f);
                image.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                wake.Add(image); born.Add(Time.time);
            }
            previous = point; hasPrevious = true;
        }

        void Update()
        {
            for (int i = wake.Count - 1; i >= 0; i--)
            {
                float t = (Time.time - born[i]) / 0.18f;
                if (t >= 1f) { Destroy(wake[i].gameObject); wake.RemoveAt(i); born.RemoveAt(i); }
                else wake[i].color = new Color(1f, 0.88f, 0.48f, 0.65f * (1f - t));
            }
        }
        public void Complete() => Destroy(gameObject);
    }
}
