using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    [DisallowMultipleComponent]
    public sealed class SeasonalHudLights : MonoBehaviour
    {
        static readonly Vector4[] ChristmasBulbs =
        {
            Bulb(240, 93), Bulb(637, 118), Bulb(875, 114), Bulb(990, 123),
            Bulb(1375, 115), Bulb(1606, 121), Bulb(1816, 96), Bulb(1964, 140),
            Bulb(88, 263, 32, 58), Bulb(2080, 263, 32, 58)
        };

        Image image;
        Material originalMaterial;
        Material animatedMaterial;
        readonly float[] pulses = new float[10];
        public float CurrentPulse => pulses[0];
        [SerializeField, Min(0.1f)] float cycleSeconds = 3.6f;
        [SerializeField, Range(0f, 1f)] float intensity = 0.65f;

        static Vector4 Bulb(float x, float y, float rx = 26f, float ry = 26f) =>
            new Vector4(x / 2172f, 1f - y / 724f, rx / 2172f, ry / 724f);

        public static void SetChristmas(Image target, bool active)
        {
            var lights = target.GetComponent<SeasonalHudLights>();
            if (active && lights == null) lights = target.gameObject.AddComponent<SeasonalHudLights>();
            if (lights != null) lights.enabled = active;
        }

        void OnEnable()
        {
            image = GetComponent<Image>();
            var shader = Resources.Load<Shader>("UI/Board/SeasonalHudLights");
            if (image == null || shader == null) return;
            originalMaterial = image.material;
            animatedMaterial = new Material(shader) { name = "Christmas HUD lights (runtime)" };
            animatedMaterial.SetVectorArray("_Bulbs", ChristmasBulbs);
            animatedMaterial.SetFloat("_BulbCount", ChristmasBulbs.Length);
            image.material = animatedMaterial;
            Update();
        }

        void Update()
        {
            if (animatedMaterial == null || image.overrideSprite == null) return;
            var sprite = image.overrideSprite;
            var rect = sprite.textureRect;
            var uv = new Vector4(rect.x / sprite.texture.width, rect.y / sprite.texture.height,
                rect.width / sprite.texture.width, rect.height / sprite.texture.height);
            float phase = Time.unscaledTime * Mathf.PI * 2f / cycleSeconds;
            for (int bulb = 0; bulb < pulses.Length; bulb++)
                pulses[bulb] = 0.5f + 0.5f * Mathf.Sin(phase + bulb * 2.4f);
            UpdateMaterial(animatedMaterial, uv);
            // UI masks use a derived stencil material; keep its animation uniforms in sync.
            var rendering = image.materialForRendering;
            if (rendering != null && rendering != animatedMaterial) UpdateMaterial(rendering, uv);
        }

        void UpdateMaterial(Material material, Vector4 uv)
        {
            material.SetVector("_SpriteUV", uv);
            material.SetFloatArray("_BulbPulse", pulses);
            material.SetFloat("_LightIntensity", intensity);
        }

        void OnDisable()
        {
            if (image != null && image.material == animatedMaterial) image.material = originalMaterial;
            if (animatedMaterial != null) Destroy(animatedMaterial);
            animatedMaterial = null;
        }
    }
}
