// Shows the props of the active season on a screen. Put it on any screen (a canvas) with two groups to hold the props — one
// behind the screen's own elements, one over them — and say which screen it is; the props the season has for that screen
// appear there when the screen opens. A prop's position is in units of the 1080-wide screen from the middle of the bottom
// edge, like every other element of a season, so the screen only has to have its groups anchored to its whole area.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public class SeasonProps : MonoBehaviour
    {
        [SerializeField] SeasonScreen screen = SeasonScreen.Loading;
        [Tooltip("Grupo de adereços que ficam por trás dos elementos da tela (logo acima da arte).")]
        [SerializeField] RectTransform behind;
        [Tooltip("Grupo de adereços que ficam por cima de tudo.")]
        [SerializeField] RectTransform front;

        class Live { public RectTransform rect; public SeasonProp prop; }
        readonly List<Live> live = new List<Live>();

        void Start()
        {
            var config = SeasonConfig.Instance;
            if (config != null) Apply(config.Active);
        }

        /// Replaces the props on screen with the ones `season` has for this screen.
        public void Apply(Season season)
        {
            Clear();
            if (season == null) return;
            foreach (var prop in season.props)
            {
                if (prop == null || prop.screen != screen || !prop.visible || prop.sprite == null) continue;
                var parent = prop.inFront ? front : behind;
                if (parent == null) continue;

                var go = new GameObject(prop.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.layer = parent.gameObject.layer;
                var rect = (RectTransform)go.transform;
                rect.SetParent(parent, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = prop.sprite.rect.size;      // the picture's own size in screen units; scale multiplies it
                var image = go.GetComponent<Image>();
                image.sprite = prop.sprite;
                image.color = prop.tint;
                image.raycastTarget = false;
                live.Add(new Live { rect = rect, prop = prop });
                Pose(rect, prop, 0f);
            }
        }

        void Update()
        {
            foreach (var l in live)
                if (l.rect != null && l.prop.motion != PropMotion.None) Pose(l.rect, l.prop, Time.unscaledTime);
        }

        static void Pose(RectTransform rect, SeasonProp prop, float time)
        {
            SeasonPropMotion.Evaluate(prop, time, out var offset, out var scale, out var rotation);
            rect.anchoredPosition = prop.position + offset;
            rect.localScale = Vector3.one * Mathf.Max(0.05f, prop.scale * scale);
            rect.localEulerAngles = new Vector3(0f, 0f, prop.rotation + rotation);
        }

        void Clear()
        {
            foreach (var l in live)
                if (l.rect != null) Destroy(l.rect.gameObject);
            live.Clear();
        }
    }
}
