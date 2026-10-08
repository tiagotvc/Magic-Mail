// A "magic wind" rising from the pedestal: small sparkle Images spawn, drift up with a gentle side-to-side
// wobble, and fade out. Plain UI (Image/RectTransform) instead of a real ParticleSystem — a ParticleSystem
// renders through Sorting Layer/Order, which has to compete against this Canvas's sortingOrder (only set
// correctly once the game is actually running, via LobbyController.PlaceBehindSweetSugarPopups). Being
// normal UI content, these sparkles draw in the same batch as everything else and look right everywhere —
// Scene view, Game, Simulator, with or without Play — with no sorting fight to lose.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public class MagicWindEffect : MonoBehaviour
    {
        [Tooltip("Sprite de uma faísca/brilho único (ex.: spark.png).")]
        [SerializeField] Sprite sparkSprite;
        [Tooltip("De onde as faíscas nascem (ex.: a base do pedestal). Usa este objeto se vazio.")]
        [SerializeField] RectTransform spawnPoint;
        [SerializeField] Color color = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] float spawnInterval = 0.15f;
        [SerializeField] float riseHeight = 80f;
        [SerializeField] float lifetime = 1.4f;
        [SerializeField] float wobbleAmplitude = 12f;
        [SerializeField] float wobbleSpeed = 3f;
        [SerializeField] float startSize = 24f;
        [SerializeField] float endSize = 8f;
        [SerializeField] int maxActive = 12;

        readonly List<RectTransform> pool = new List<RectTransform>();
        float timer;

        void OnEnable() => timer = 0f;

        void Update()
        {
            if (sparkSprite == null || !Application.isPlaying) return;
            timer += Time.unscaledDeltaTime;
            if (timer < spawnInterval) return;
            timer = 0f;
            if (CountActive() < maxActive) Spawn();
        }

        int CountActive()
        {
            var n = 0;
            foreach (var p in pool)
                if (p != null && p.gameObject.activeSelf) n++;
            return n;
        }

        void Spawn()
        {
            var origin = spawnPoint != null ? spawnPoint : (RectTransform)transform;
            var spark = GetFromPool();
            spark.SetParent(origin, false);
            var baseX = Random.Range(-10f, 10f);
            spark.anchoredPosition = new Vector2(baseX, 0f);
            spark.gameObject.SetActive(true);
            StartCoroutine(Animate(spark, baseX, Random.Range(0f, Mathf.PI * 2f), Random.Range(0.85f, 1.15f)));
        }

        RectTransform GetFromPool()
        {
            foreach (var p in pool)
                if (p != null && !p.gameObject.activeSelf) return p;
            var go = new GameObject("Spark", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            var image = go.GetComponent<Image>();
            image.sprite = sparkSprite;
            image.raycastTarget = false;
            pool.Add(rect);
            return rect;
        }

        IEnumerator Animate(RectTransform spark, float baseX, float phase, float speedScale)
        {
            var image = spark.GetComponent<Image>();
            var t = 0f;
            while (t < lifetime)
            {
                t += Time.unscaledDeltaTime * speedScale;
                var f = Mathf.Clamp01(t / lifetime);
                var y = f * riseHeight;
                var x = baseX + Mathf.Sin(f * wobbleSpeed * Mathf.PI * 2f + phase) * wobbleAmplitude;
                spark.anchoredPosition = new Vector2(x, y);
                var size = Mathf.Lerp(startSize, endSize, f);
                spark.sizeDelta = new Vector2(size, size);
                var alpha = f < 0.15f ? f / 0.15f : f > 0.7f ? 1f - (f - 0.7f) / 0.3f : 1f;
                image.color = new Color(color.r, color.g, color.b, color.a * alpha);
                yield return null;
            }
            spark.gameObject.SetActive(false);
        }
    }
}
