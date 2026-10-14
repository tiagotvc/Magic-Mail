using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.System;
using UnityEngine;

namespace RoyalAves.Effects
{
    public static class PowerUpFormation
    {
        static readonly Dictionary<Item, List<Item>> groups = new Dictionary<Item, List<Item>>();
#if UNITY_EDITOR
        public static int CreatedCount;
        public static ItemsTypes LastCreatedType;
        public static int LastAttractedCount;
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { groups.Clear(); }
        public static void Register(Item target, IEnumerable<Item> members)
        {
            if (target == null || target.currentType != ItemsTypes.NONE) return;
            var list = members.Where(i => i != null && i.currentType == ItemsTypes.NONE).Distinct().ToList();
            if (list.Contains(target) && list.Count >= 4) groups[target] = list;
        }

        public static IEnumerator Attract(List<Item> batch)
        {
            var targets = batch.Where(i => i != null && i.currentType == ItemsTypes.NONE && i.NextType != ItemsTypes.NONE).Distinct().ToArray();
            foreach (var target in targets)
            {
                if (!groups.TryGetValue(target, out var members)) continue;
                groups.Remove(target);
                members = members.Where(i => i != null && batch.Contains(i) && i.gameObject.activeSelf).ToList();
                if (members.Count < 4 || target.square == null) continue;
#if UNITY_EDITOR
                LastAttractedCount = members.Count;
#endif
                var manager = LevelManager.THIS;
                var data = manager.levelData;
                var root = new GameObject("MatchAttractionToPowerUp");
                root.transform.SetParent(target.field.transform, false);
                var barrier = root.AddComponent<AnimateItems>();
                barrier.target = true;
                manager.animateItems.Add(barrier);
                var sprites = new List<SpriteRenderer>();
                var starts = new List<Vector3>();
                var scales = new List<Vector3>();
                var destination = target.square.transform.position;
                try
                {
                    foreach (var item in members)
                    {
                        item.powerUpFormationMatch = true;
                        foreach (var source in item.GetSpriteRenderers().Where(s => s != null && s.enabled && s.sprite != null))
                        {
                            var node = new GameObject("AttractedMatchIcon");
                            node.transform.SetParent(root.transform, false);
                            node.transform.position = source.transform.position;
                            node.transform.rotation = source.transform.rotation;
                            node.transform.localScale = new Vector3(source.transform.lossyScale.x / root.transform.lossyScale.x,
                                source.transform.lossyScale.y / root.transform.lossyScale.y, 1f);
                            var copy = node.AddComponent<SpriteRenderer>();
                            copy.sprite = source.sprite; copy.sharedMaterial = source.sharedMaterial; copy.color = source.color;
                            copy.sortingLayerName = PowerUpEffectOrder.SortingLayer;
                            copy.sortingOrder = PowerUpEffectOrder.Value - 5;
                            sprites.Add(copy); starts.Add(node.transform.position); scales.Add(node.transform.localScale);
                        }
                        item.HideSprites(true);
                    }
                    float elapsed = 0f;
                    while (elapsed < 0.15f && manager != null && manager.levelData == data)
                    {
                        elapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(elapsed / 0.15f);
                        float pull = t * t * (3f - 2f * t);
                        for (int i = 0; i < sprites.Count; i++)
                        {
                            sprites[i].transform.position = Vector3.Lerp(starts[i], destination, pull);
                            sprites[i].transform.localScale = scales[i] * Mathf.Lerp(1f, 0.35f, t * t);
                        }
                        yield return null;
                    }
                }
                finally
                {
                    if (manager != null) manager.animateItems.Remove(barrier);
                    Object.Destroy(root);
                }
            }
        }

        public static void Born(Item item)
        {
            if (item == null) return;
#if UNITY_EDITOR
            CreatedCount++;
            LastCreatedType = item.currentType;
#endif
            var root = new GameObject("PowerUpGoldenBirthGlow");
            root.transform.SetParent(item.field.transform, false);
            root.transform.position = item.transform.position + Vector3.forward * 0.02f;
            root.AddComponent<PowerUpBirthGlow>().Initialize(item);
        }
    }

    public sealed class PowerUpBirthGlow : MonoBehaviour
    {
        static Sprite glowSprite;
        public void Initialize(Item item)
        {
            if (glowSprite == null)
            {
                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Bilinear;
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                {
                    float radius = new Vector2((x + 0.5f - size / 2f) / (size / 2f), (y + 0.5f - size / 2f) / (size / 2f)).magnitude;
                    pixels[y * size + x] = new Color(1f, 0.83f, 0.12f, Mathf.Pow(Mathf.Clamp01(1f - radius), 1.3f));
                }
                texture.SetPixels(pixels); texture.Apply(false, true);
                glowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
            }
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = glowSprite;
            var source = item.GetSpriteRenderer();
            renderer.sortingLayerName = source != null ? source.sortingLayerName : "Default";
            renderer.sortingOrder = source != null ? source.sortingOrder - 1 : 5;
            StartCoroutine(Flash(renderer, item.field.squareWidth));
        }
        IEnumerator Flash(SpriteRenderer renderer, float cell)
        {
            float elapsed = 0f;
            while (elapsed < 0.28f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.28f);
                transform.localScale = Vector3.one * (cell * Mathf.Lerp(1.5f, 2.1f, t) / Mathf.Abs(transform.parent.lossyScale.x));
                renderer.color = new Color(1, 1, 1, Mathf.Sin(t * Mathf.PI) * 0.9f);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
