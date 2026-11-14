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
        [System.NonSerialized] public bool metaballsPreview;
        [System.NonSerialized] public Shader metaballsShader;
        [System.NonSerialized] public bool drawnLiquidPreview;
        [System.NonSerialized] public Texture2D drawnLiquidSheet;
        readonly SpriteRenderer[] flasks = new SpriteRenderer[4];
        readonly bool[] breaking = new bool[4];
        readonly Vector3[] intactPositions = new Vector3[4];
        SpriteRenderer standRenderer;
        bool standBroken;
        // Collar x coordinates in each 627px intact quadrant. Their drawings
        // are not centered in the quadrant, unlike the rupture reference.
        static readonly float[] IntactCollarX = { 386.05f, 234.71f, 383.62f, 233.31f };
        readonly List<Sprite> burstFrames = new List<Sprite>();
        readonly List<Object> owned = new List<Object>();
        // Collar centers measured in the authored 1254px sheet, relative to each
        // cell's top-left corner. The drawing shifts inside the cells; a shared
        // center pivot otherwise makes the whole bottle jump during the break.
        // Anchor to the collar, not the changing bounds of glass and splashes.
        static readonly Vector2[] BreakCollarAnchors =
        {
            new Vector2(160.8f, 127.6f), new Vector2(146.8f, 130.1f),
            new Vector2(158.9f, 127.7f), new Vector2(152.9f, 127.8f),
            new Vector2(171.3f, 82.6f), new Vector2(179.7f, 91.9f),
            new Vector2(169.1f, 88.2f), new Vector2(179.5f, 99.8f),
            new Vector2(175.4f, 79.8f), new Vector2(169.9f, 76.9f),
            new Vector2(172f, 68.5f), new Vector2(167.2f, 70.8f)
        };

        static Vector2 BreakFramePivot(int frame)
        {
            // Liquid-only frames inherit the last bottle's correction so the
            // handoff does not undo the alignment or pin the falling droplets.
            var delta = BreakCollarAnchors[Mathf.Min(frame, BreakCollarAnchors.Length - 1)]
                - BreakCollarAnchors[0];
            const float authoredCellSize = 1254f / 4f;
            return new Vector2(.5f + delta.x / authoredCellSize,
                .5f - delta.y / authoredCellSize);
        }
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
                Debug.LogError("Poções: faltam as camadas separadas de estante e frascos. Reimporte os assets antes de testar.", this);
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
                        cellWidth, cellHeight), BreakFramePivot(i), cellWidth / 2.4f);
                    burstFrames.Add(frame);
                    owned.Add(frame);
                }
            }
            if (standArtwork != null)
            {
                var stand = new GameObject("IntactPotionStand");
                stand.transform.SetParent(transform, false);
                var renderer = stand.AddComponent<SpriteRenderer>();
                standRenderer = renderer;
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
                intactPositions[i] = node.transform.localPosition;
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
                "Poções • teste visual 2×2\nClique em um frasco ou use os botões para ver o vidro e a tinta.", title);
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
                standBroken = false;
                if (standRenderer != null) standRenderer.enabled = true;
                foreach (Transform child in transform)
                    if (child.name == "PotionSpill") RemoveSpill(child.gameObject);
                    else if (child.name == "FallingPotionGlass") Destroy(child.gameObject);
                    else if (child.name == "CrateWoodExplosion") Destroy(child.gameObject);
                    else if (child.name == "ParcelSparkFX") Destroy(child.gameObject);
                for (int i = 0; i < flasks.Length; i++)
                {
                    breaking[i] = false;
                    if (flasks[i] == null) continue;
                    flasks[i].sprite = intactSprites[i];
                    flasks[i].transform.localPosition = intactPositions[i];
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
                Debug.LogError("Poções: spritesheet de quebra ausente.", this);
                return false;
            }
            breaking[index] = true;
            StartCoroutine(AnimateBottleBreak(index));
            return true;
        }

        readonly Sprite[] intactSprites = new Sprite[4];
        const int LastRuptureFrame = 7;
        const float BreakFrameDuration = .17f / 8f;

        IEnumerator AnimateBottleBreak(int index)
        {
            var renderer = flasks[index];
            bool useMetaballs = metaballsPreview && metaballsShader != null && metaballsShader.isSupported;
            bool useDrawn = drawnLiquidPreview && drawnLiquidSheet != null;
            if (useDrawn) useMetaballs = false;
            bool useSplash = !useMetaballs && !useDrawn;
            float intactAnchorX = (IntactCollarX[index] / 627f - .5f) * (CanvasSize / 2);
            float breakAnchorX = (BreakCollarAnchors[0].x / (1254f / 4) - .5f) * 2.4f;
            // Keep the full glass rupture. Metaballs take over the liquid while
            // the authored shards/cork continue animating on their own layer.
            int lastFrame = useSplash ? LastRuptureFrame : 11;
            for (int frame = 0; frame <= lastFrame; frame++)
            {
                if (frame >= 2)
                {
                    renderer.transform.localPosition = intactPositions[index]
                        + Vector3.right * (intactAnchorX - breakAnchorX);
                    renderer.color = inks[index];
                    renderer.sprite = burstFrames[frame];
                }
                if (useMetaballs && frame == 4)
                {
                    // First open-glass frame: anchor the top of the flowing
                    // liquid near the bottle base, rather than its center.
                    PotionMetaballsEffect.Play(transform,
                        renderer.transform.localPosition + Vector3.down * .65f,
                        inks[index], metaballsShader);
                }
                if (useDrawn && frame == 4)
                    DrawnPotionEffect.Play(transform, renderer.transform.localPosition + Vector3.down * .65f,
                        inks[index], drawnLiquidSheet);
                if (useSplash && frame == 4)
                    MaterialBurstEffect.Potion(renderer, inks[index]);
                yield return new WaitForSeconds(BreakFrameDuration);
            }
            renderer.enabled = false;
            BreakStandIfEmpty();
            // The painted splash stays local to the rupture. Independent drops
            // and glass continue ballistically; no whole PNG travels downward.
        }

        void BreakStandIfEmpty()
        {
            if (standBroken || standRenderer == null) return;
            // Concurrent hits may finish in any order. Wait until every bottle
            // has actually completed its rupture, and explode the stand once.
            for (int i = 0; i < flasks.Length; i++)
                if (!breaking[i] || flasks[i] == null || flasks[i].enabled) return;
            standBroken = true;
            FallingBoardDebris.ShatterWood(standRenderer, 1);
            ParcelSoundEffects.Play(1);
            ParcelSparkEffect.Play(standRenderer, true);
            standRenderer.enabled = false;
        }

        public float SpillDuration(int index) => metaballsPreview || drawnLiquidPreview ? 4f : 1.6f;

        void RemoveSpill(GameObject root)
        {
            // Each spill component owns and disposes its meshes/materials.
            Destroy(root);
        }

        void OnDestroy()
        {
            foreach (var asset in owned) if (asset != null) Destroy(asset);
        }
    }
}
