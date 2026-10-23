using UnityEngine;

namespace RoyalAves.Effects
{
    public sealed class DrawnPotionEffect : MonoBehaviour
    {
        // Alpha-weighted centers and areas measured in the generated sheet.
        // Keep the liquid's center and apparent amount stable between drawings.
        static readonly Vector2[] Centers = {
            new Vector2(.5921f,.4421f), new Vector2(.5456f,.4128f), new Vector2(.492f,.4197f), new Vector2(.458f,.4224f),
            new Vector2(.5996f,.447f), new Vector2(.561f,.432f), new Vector2(.5131f,.4094f), new Vector2(.4759f,.3997f),
            new Vector2(.597f,.415f), new Vector2(.5605f,.424f), new Vector2(.5159f,.4396f), new Vector2(.4862f,.4309f),
            new Vector2(.5955f,.5033f), new Vector2(.5602f,.503f), new Vector2(.5325f,.4955f), new Vector2(.5108f,.4901f) };
        static readonly float[] Areas = {22611,23882,23688,21953,23598,24541,24007,22712,24447,25637,24236,23772,22611,24779,23761,21719};
        readonly Sprite[] frames = new Sprite[16];
        SpriteRenderer ink;
        Vector3 start;
        float elapsed, duration, distance;

        public static void Play(Transform parent, Vector3 origin, Color color, Texture2D sheet)
        {
            var root = new GameObject("PotionSpill");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = origin + Vector3.down * .85f;
            var effect = root.AddComponent<DrawnPotionEffect>();
            effect.start = root.transform.localPosition;
            effect.ink = root.AddComponent<SpriteRenderer>();
            effect.ink.color = color;
            effect.ink.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            effect.ink.sortingOrder = 15;
            float w = sheet.width / 4f, h = sheet.height / 4f;
            for (int i = 0; i < 16; i++)
                effect.frames[i] = Sprite.Create(sheet, new Rect(i % 4 * w, (3-i/4) * h, w, h), Centers[i], w / 1.8f);
            effect.distance = 16;
            if (Camera.main != null)
            {
                var world = parent.TransformPoint(effect.start);
                var bottom = parent.InverseTransformPoint(Camera.main.ViewportToWorldPoint(
                    new Vector3(.5f, 0, Camera.main.WorldToScreenPoint(world).z)));
                effect.distance = Mathf.Max(2, effect.start.y - bottom.y + 2);
            }
            // Same accelerated descent as the approved 2x metaballs clock.
            effect.duration = (-1.4f + Mathf.Sqrt(1.96f + 44.8f * effect.distance)) / 22.4f;
            effect.Sample(0);
        }

        void Sample(float time)
        {
            float t = Mathf.Clamp01(time / duration);
            int frame = Mathf.Min(15, Mathf.FloorToInt(t * 16));
            ink.sprite = frames[frame];
            transform.localScale = Vector3.one * Mathf.Sqrt(23600f / Areas[frame]);
            transform.localPosition = start + new Vector3(Mathf.Sin(t * Mathf.PI) * .1f,
                -(1.4f * time + 11.2f * time * time), 0);
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            Sample(elapsed);
            if (elapsed >= duration) Destroy(gameObject);
        }

        void OnDestroy()
        {
            foreach (var frame in frames) if (frame != null) Destroy(frame);
        }
    }
}
