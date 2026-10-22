using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Effects
{
    // Visual prototype only. BreakFlask is also the entry point for board integration.
    public sealed class RouteCentralPrototype : MonoBehaviour
    {
        public const float CanvasSize = 4f;
        public Sprite artwork;
        public Sprite standArtwork;
        public Sprite flaskArtwork;
        public Texture2D breakSheet;
        public bool interactivePreview;
        readonly SpriteRenderer[] flasks = new SpriteRenderer[4];
        readonly bool[] breaking = new bool[4];
        readonly List<Sprite> burstFrames = new List<Sprite>();
        readonly List<Object> owned = new List<Object>();
        readonly Color[] inks = { new Color(.3f, .9f, .08f), new Color(.65f, .1f, .95f),
            new Color(.95f, .06f, .2f), new Color(.02f, .65f, 1f) };

        void Start()
        {
            if (artwork == null) return;
            // Older lab prefabs were saved by a still-loaded editor assembly without
            // the new layer fields. Resolve the real layers even for those prefabs.
            if (standArtwork == null) standArtwork = Resources.Load<Sprite>("RouteCentral/CM_central_estante");
            if (flaskArtwork == null) flaskArtwork = Resources.Load<Sprite>("RouteCentral/CM_central_frascos");
            if (standArtwork == null || flaskArtwork == null)
            {
                Debug.LogError("Central de Rotas: faltam as camadas separadas de estante e frascos. Reimporte os assets antes de testar.", this);
                enabled = false;
                return;
            }
            var preview = GetComponent<SpriteRenderer>();
            if (preview != null) preview.enabled = false;
            var sheet = breakSheet != null ? breakSheet : Resources.Load<Texture2D>("RouteCentral/CM_frasco_quebra_sheet");
            if (sheet != null)
            {
                float cellWidth = sheet.width / 4f, cellHeight = sheet.height / 4f;
                for (int i = 0; i < 16; i++)
                {
                    var frame = Sprite.Create(sheet, new Rect(i % 4 * cellWidth, (3 - i / 4) * cellHeight,
                        cellWidth, cellHeight), new Vector2(.5f, .5f), cellWidth / 2.4f);
                    burstFrames.Add(frame);
                    owned.Add(frame);
                }
            }
            if (standArtwork != null)
            {
                var stand = new GameObject("IntactPotionStand");
                stand.transform.SetParent(transform, false);
                var renderer = stand.AddComponent<SpriteRenderer>();
                // Normalize each layer to the same canvas, regardless of importer
                // PPU/pivot settings or the old combined artwork's import rules.
                var standSprite = Sprite.Create(standArtwork.texture, standArtwork.rect,
                    Vector2.one * .5f, standArtwork.rect.width / CanvasSize);
                owned.Add(standSprite);
                renderer.sprite = standSprite;
                renderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
                renderer.sortingOrder = 9;
            }
            var flaskSheet = flaskArtwork;
            var rect = flaskSheet.rect;
            for (int i = 0; i < 4; i++)
            {
                int x = i % 2, y = 1 - i / 2;
                var sprite = Sprite.Create(flaskSheet.texture,
                    new Rect(rect.x + x * rect.width / 2, rect.y + y * rect.height / 2,
                        rect.width / 2, rect.height / 2), Vector2.one * .5f, rect.width / CanvasSize);
                owned.Add(sprite);
                var node = new GameObject("Potion" + i);
                node.transform.SetParent(transform, false);
                node.transform.localPosition = new Vector3((x - .5f) * CanvasSize / 2,
                    (y - .5f) * CanvasSize / 2, 0);
                var renderer = node.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
                renderer.sortingOrder = 10;
                flasks[i] = renderer;
                intactSprites[i] = sprite;
            }
        }

        void Update()
        {
            if (interactivePreview && Camera.main != null)
                Camera.main.orthographicSize = Mathf.Max(5.4f, 4.4f / Camera.main.aspect);
            if (!interactivePreview || !Input.GetMouseButtonDown(0) || Camera.main == null) return;
            if (Input.mousePosition.y < 125 || Input.mousePosition.y > Screen.height - 85) return;
            var p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            for (int i = 0; i < flasks.Length; i++)
                if (flasks[i] != null && flasks[i].enabled)
                {
                    var bounds = flasks[i].bounds;
                    p.z = bounds.center.z;
                    if (bounds.Contains(p)) { BreakFlask(i); break; }
                }
        }

        void OnGUI()
        {
            if (!interactivePreview) return;
            var title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18, wordWrap = true };
            GUI.Label(new Rect(12, 10, Screen.width - 24, 62),
                "Central de Rotas • teste visual 2×2\nClique em um frasco ou use os botões para ver o vidro e a tinta.", title);
            string[] labels = { "Verde", "Roxa", "Vermelha", "Azul" };
            float width = (Screen.width - 30f) / 4;
            for (int i = 0; i < 4; i++)
            {
                GUI.enabled = flasks[i] != null && flasks[i].enabled;
                if (GUI.Button(new Rect(6 + i * (width + 6), Screen.height - 112, width, 42), labels[i])) BreakFlask(i);
            }
            GUI.enabled = true;
            if (GUI.Button(new Rect(12, Screen.height - 58, Screen.width - 24, 42), "Repetir teste"))
            {
                StopAllCoroutines();
                foreach (Transform child in transform)
                    if (child.name == "PotionSpill") RemoveSpill(child.gameObject);
                    else if (child.name == "FallingPotionGlass") Destroy(child.gameObject);
                for (int i = 0; i < flasks.Length; i++)
                {
                    breaking[i] = false;
                    if (flasks[i] == null) continue;
                    flasks[i].sprite = intactSprites[i];
                    flasks[i].color = Color.white;
                    flasks[i].enabled = true;
                }
            }
        }

        public bool BreakFlask(int index)
        {
            if (index < 0 || index >= 4 || flasks[index] == null || !flasks[index].enabled || breaking[index]) return false;
            if (burstFrames.Count != 16)
            {
                Debug.LogError("Central de Rotas: spritesheet de quebra ausente.", this);
                return false;
            }
            breaking[index] = true;
            StartCoroutine(AnimateBottleBreak(index));
            return true;
        }

        readonly Sprite[] intactSprites = new Sprite[4];

        IEnumerator AnimateBottleBreak(int index)
        {
            var renderer = flasks[index];
            for (int frame = 0; frame < 12; frame++)
            {
                if (frame >= 2)
                {
                    renderer.color = inks[index];
                    renderer.sprite = burstFrames[frame];
                }
                yield return new WaitForSeconds(.04f);
            }
            renderer.enabled = false;
            yield return SpriteInkFall(renderer.transform.localPosition, inks[index]);
        }

        IEnumerator SpriteInkFall(Vector3 origin, Color color)
        {
            var node = new GameObject("PotionSpill");
            node.transform.SetParent(transform, false);
            node.transform.localPosition = origin;
            var ink = node.AddComponent<SpriteRenderer>();
            ink.color = color;
            ink.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            ink.sortingOrder = 15;
            float distance = SpillDistance(origin) + 2f;
            float elapsed = 0;
            const float duration = .72f;
            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                // Continue with the sheet's liquid-only frames, using the same
                // size, pivot and starting position as the breaking bottle.
                int frame = t < .25f ? 12 : t < .55f ? 13 : t < .88f ? 14 : 15;
                ink.sprite = burstFrames[frame];
                node.transform.localPosition = origin + new Vector3(
                    Mathf.Sin(t * Mathf.PI) * .16f, -distance * t * t, 0);
                node.transform.localScale = new Vector3(1f + .2f * t, 1f, 1f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            Destroy(node);
        }

        float SpillDistance(Vector3 origin)
        {
            var camera = Camera.main;
            if (camera == null) return 20f;
            var world = transform.TransformPoint(origin);
            float depth = camera.WorldToScreenPoint(world).z;
            var bottom = camera.ViewportToWorldPoint(new Vector3(.5f, 0, depth));
            return Mathf.Max(1f, origin.y - transform.InverseTransformPoint(bottom).y + 1f);
        }

        float SpillDuration(Vector3 origin) => 1.05f;
        public float SpillDuration(int index) => flasks[index] != null ? SpillDuration(flasks[index].transform.localPosition) + .65f : 4f;

        void RemoveSpill(GameObject root)
        {
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                owned.Remove(mesh);
                Destroy(mesh);
            }
            Destroy(root);
        }

        void OnDestroy()
        {
            foreach (var asset in owned) if (asset != null) Destroy(asset);
        }
    }
}
