// "Magic dust" build-up for an area decoration stage (e.g. Content/areas/<id>/Estagio_X): each piece is
// drawn in from the bottom up with a real dissolve — the project's own AllIn1SpriteShader (Fade effect),
// fed a vertical gradient instead of its usual random noise, so the burning edge sweeps bottom-to-top
// instead of dissolving in a random pattern. Golden sparkles trail right along the rising edge.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public static class AreaStageReveal
    {
        const float Duration = 1.1f;
        const float SparkleInterval = 0.05f;

        // AllIn1SpriteShader's _FadeAmount: pixels below this threshold (per the fade texture's red
        // channel) are cut away. -0.1 shows everything (the shader's own "off" default); 1 hides it all.
        const float FadeAmountHidden = 1f;
        const float FadeAmountShown = -0.1f;

        static Texture2D verticalGradient;
        static Shader fadeShader;

        /// Plays the build-up on every piece directly under "stage". Safe to call on a stage with any
        /// number of children, including zero.
        public static IEnumerator Play(MonoBehaviour host, RectTransform stage, Sprite sparkleSprite, Texture2D burnTexture)
        {
            if (stage == null) yield break;
            var pieces = new RectTransform[stage.childCount];
            for (var i = 0; i < pieces.Length; i++) pieces[i] = (RectTransform)stage.GetChild(i);
            yield return Play(host, stage, pieces, sparkleSprite, burnTexture);
        }

        /// Same build-up, but for an explicit set of pieces instead of everything directly under "stage" —
        /// for stages that swap one specific piece in (e.g. a "broken" clock replaced by a "fixed" one)
        /// rather than building every child at once. "stage" is only used to host spawned sparkles.
        public static IEnumerator Play(MonoBehaviour host, RectTransform stage, IReadOnlyList<RectTransform> pieces, Sprite sparkleSprite, Texture2D burnTexture)
        {
            if (stage == null || pieces == null) yield break;
            stage.gameObject.SetActive(true);

            var images = new Image[pieces.Count];
            var originalMaterials = new Material[pieces.Count];
            var fadeMaterials = new Material[pieces.Count];
            var shader = FadeShader();
            for (var i = 0; i < pieces.Count; i++)
            {
                if (pieces[i] == null) continue;
                pieces[i].gameObject.SetActive(true);
                images[i] = pieces[i].GetComponent<Image>();
                if (images[i] == null || shader == null) continue;
                originalMaterials[i] = images[i].material;
                var mat = new Material(shader);
                mat.EnableKeyword("FADE_ON");
                mat.SetTexture("_FadeTex", VerticalGradient());
                mat.SetTexture("_FadeBurnTex", burnTexture);
                mat.SetColor("_FadeBurnColor", new Color(1f, 0.85f, 0.35f, 1f));
                mat.SetFloat("_FadeBurnWidth", 0.12f);
                mat.SetFloat("_FadeBurnTransition", 0.18f);
                mat.SetFloat("_FadeBurnGlow", 6f);
                mat.SetFloat("_FadeAmount", FadeAmountHidden);
                fadeMaterials[i] = mat;
                images[i].material = mat;
            }

            Coroutine sparkles = null;
            if (sparkleSprite != null && host != null)
                sparkles = host.StartCoroutine(TrailSparkles(host, stage, pieces, fadeMaterials, sparkleSprite));

            var t = 0f;
            while (t < Duration)
            {
                t += Time.unscaledDeltaTime;
                var f = Mathf.Clamp01(t / Duration);
                for (var i = 0; i < pieces.Count; i++)
                {
                    if (fadeMaterials[i] == null) continue;
                    // pieces further right build in slightly later, for an "under construction" left-to-right feel
                    var delay = pieces.Count <= 1 ? 0f : 0.35f * i / (pieces.Count - 1);
                    var pf = Mathf.Clamp01((f - delay) / (1 - delay));
                    fadeMaterials[i].SetFloat("_FadeAmount", Mathf.Lerp(FadeAmountHidden, FadeAmountShown, pf));
                }
                yield return null;
            }
            if (host != null && sparkles != null) host.StopCoroutine(sparkles);
            for (var i = 0; i < pieces.Count; i++)
            {
                if (images[i] == null) continue;
                images[i].material = originalMaterials[i]; // fully shown looks identical without the fade material
                if (fadeMaterials[i] != null) Object.Destroy(fadeMaterials[i]);
            }
        }

        // Spawns sparkles right at each piece's rising edge while it is still filling in.
        static IEnumerator TrailSparkles(MonoBehaviour host, RectTransform stage, IReadOnlyList<RectTransform> pieces, Material[] fadeMaterials, Sprite sprite)
        {
            var corners = new Vector3[4];
            while (true)
            {
                for (var i = 0; i < pieces.Count; i++)
                {
                    if (fadeMaterials[i] == null || pieces[i] == null) continue;
                    var amount = fadeMaterials[i].GetFloat("_FadeAmount");
                    var p = Mathf.InverseLerp(FadeAmountHidden, FadeAmountShown, amount);
                    if (p <= 0f || p >= 1f) continue;
                    pieces[i].GetWorldCorners(corners); // 0 bottom-left, 1 top-left, 2 top-right, 3 bottom-right
                    var edgeY = Mathf.Lerp(corners[0].y, corners[1].y, p);
                    var x = Random.Range(corners[0].x, corners[2].x);
                    host.StartCoroutine(DriftAndFade(SpawnSparkle(stage, sprite, new Vector3(x, edgeY, 0f))));
                }
                yield return new WaitForSecondsRealtime(SparkleInterval);
            }
        }

        static RectTransform SpawnSparkle(RectTransform stage, Sprite sprite, Vector3 worldPos)
        {
            var go = new GameObject("Sparkle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(stage, true);
            rect.position = worldPos;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            var size = Random.Range(14f, 26f);
            rect.sizeDelta = new Vector2(size, size);
            return rect;
        }

        static IEnumerator DriftAndFade(RectTransform sparkle)
        {
            if (sparkle == null) yield break;
            var image = sparkle.GetComponent<Image>();
            var start = sparkle.localPosition;
            var drift = Random.Range(-14f, 14f);
            var travel = Random.Range(20f, 45f);
            var life = Random.Range(0.4f, 0.7f);
            var t = 0f;
            while (t < life)
            {
                if (sparkle == null) yield break;
                t += Time.unscaledDeltaTime;
                var f = Mathf.Clamp01(t / life);
                sparkle.localPosition = start + new Vector3(drift * f, travel * f, 0f);
                var fade = f < 0.25f ? f / 0.25f : 1f - (f - 0.25f) / 0.75f; // quick fade in, gentle fade out
                image.color = new Color(1f, 0.92f, 0.6f, Mathf.Clamp01(fade) * 0.9f);
                yield return null;
            }
            if (sparkle != null) Object.Destroy(sparkle.gameObject);
        }

        static Shader FadeShader() =>
            fadeShader != null ? fadeShader : fadeShader = Shader.Find("AllIn1SpriteShader/AllIn1SpriteShader");

        // White at the bottom, black at the top: fed as AllIn1SpriteShader's _FadeTex (whose dissolve
        // keeps pixels visible longest where this value is highest), this makes the reveal sweep
        // bottom-to-top as _FadeAmount drops, instead of the shader's usual random noise pattern.
        // Unity's SetPixels/UV convention puts row 0 at the texture's bottom (V=0).
        static Texture2D VerticalGradient()
        {
            if (verticalGradient != null) return verticalGradient;
            const int size = 64;
            var tex = new Texture2D(1, size, TextureFormat.R8, false) { wrapMode = TextureWrapMode.Clamp, name = "AreaStageReveal_VerticalGradient" };
            var pixels = new Color[size];
            for (var y = 0; y < size; y++)
            {
                var v = 1f - y / (size - 1f); // row 0 (bottom) = 1 (white), top row = 0 (black)
                pixels[y] = new Color(v, v, v, 1f);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return verticalGradient = tex;
        }
    }
}
