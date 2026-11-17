// Mostra um número (ex.: jogadas restantes) com SPRITES de algarismos em vez de fonte. Fica no mesmo objeto de um TextMeshPro:
// o jogo continua escrevendo o texto nele (Counter_ etc.) e este componente espelha esse texto nos sprites, então não mexe em
// nenhuma lógica. Enquanto os sprites 0-9 não estiverem preenchidos, o texto normal continua aparecendo (nada quebra).
//
// Como usar: arraste os 10 sprites (0 a 9) no campo "Digits" (e, se quiser, ":" para o tempo e "-" para negativos). Dá para
// preencher de uma vez pelo botão "Procurar sprites 0-9 numa pasta" do Inspector.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("Royal Aves/Sprite Number Text")]
    public class SpriteNumberText : MonoBehaviour
    {
        [Header("Sprites dos algarismos")]
        [Tooltip("Posição 0 = sprite do 0, ... posição 9 = sprite do 9.")]
        public Sprite[] digits = new Sprite[10];
        [Tooltip("Opcional: para o tempo (mm:ss).")]
        public Sprite colon;
        [Tooltip("Opcional: para números negativos.")]
        public Sprite minus;

        [Header("Aparência")]
        public Color tint = Color.white;
        [Tooltip("Altura dos algarismos em relação à altura da caixa do texto.")]
        [Range(0.2f, 1.5f)] public float heightFill = 0.9f;
        [Tooltip("Espaço entre algarismos, em múltiplos da altura.")]
        [Range(-0.3f, 0.6f)] public float spacing = 0.02f;
        [Tooltip("Reduz tudo se o número não couber na largura da caixa.")]
        public bool shrinkToFit = true;
        [Tooltip("Quanto o número dá um \"pulinho\" quando o valor muda (0 = sem).")]
        [Range(0f, 0.5f)] public float punchOnChange = 0.12f;
        [Tooltip("Esconde o texto original quando os sprites estão preenchidos.")]
        public bool hideTextWhenSprites = true;
        [Tooltip("Pré-visualiza no editor (fora do Play) o texto atual.")]
        public bool previewInEditor = true;

        TMP_Text label;
        RectTransform container;
        readonly List<Image> images = new List<Image>();
        string shown;
        float punchAge = 1f;
        Vector3 containerScale = Vector3.one;

        public bool HasSprites
        {
            get
            {
                if (digits == null || digits.Length < 10) return false;
                for (var i = 0; i < 10; i++) if (digits[i] == null) return false;
                return true;
            }
        }

        void OnEnable()
        {
            label = GetComponent<TMP_Text>();
            shown = null;
            Refresh();
        }

        void OnDisable()
        {
            if (label != null && hideTextWhenSprites) label.enabled = true;
            if (container != null)
            {
                if (Application.isPlaying) Destroy(container.gameObject); else DestroyImmediate(container.gameObject);
            }
            container = null; images.Clear();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () => { if (this != null && isActiveAndEnabled) { shown = null; Refresh(); } };
        }
#endif

        void LateUpdate()
        {
            if (!Application.isPlaying && !previewInEditor) return;
            Refresh();
            if (container != null && punchAge < 1f)
            {
                punchAge += Time.unscaledDeltaTime / 0.22f;
                var k = Mathf.Clamp01(punchAge);
                container.localScale = containerScale * (1f + punchOnChange * Mathf.Sin(k * Mathf.PI));
            }
        }

        /// Reconstrói os sprites se o texto mudou.
        public void Refresh()
        {
            if (label == null) label = GetComponent<TMP_Text>();
            if (label == null) return;
            if (!HasSprites)
            {
                if (hideTextWhenSprites && !label.enabled) label.enabled = true;
                if (container != null) container.gameObject.SetActive(false);
                return;
            }
            var text = label.text ?? "";
            var rect = (RectTransform)transform;
            var key = text + "|" + rect.rect.width + "x" + rect.rect.height + "|" + heightFill + "|" + spacing + "|" + tint;
            if (key == shown && container != null) return;
            var changedValue = shown != null && container != null && !key.StartsWith(text + "|", System.StringComparison.Ordinal);
            shown = key;
            Build(text, rect);
            if (hideTextWhenSprites) label.enabled = false;
            if (changedValue && Application.isPlaying && punchOnChange > 0f) punchAge = 0f;
        }

        Sprite SpriteFor(char c)
        {
            if (c >= '0' && c <= '9') return digits[c - '0'];
            if (c == ':') return colon;
            if (c == '-') return minus;
            return null;
        }

        void Build(string text, RectTransform rect)
        {
            if (container == null)
            {
                var go = new GameObject("Algarismos (gerado)", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
                container = (RectTransform)go.transform;
                container.SetParent(transform, false);
                container.anchorMin = container.anchorMax = container.pivot = Vector2.one * 0.5f;
                container.anchoredPosition = Vector2.zero;
                container.sizeDelta = Vector2.zero;
            }
            container.gameObject.SetActive(true);
            containerScale = Vector3.one;
            container.localScale = Vector3.one;

            // Só caracteres que têm sprite
            var chars = new List<char>();
            foreach (var c in text) if (SpriteFor(c) != null) chars.Add(c);

            var height = rect.rect.height * heightFill;
            if (height < 1f) height = 100f * heightFill;
            var widths = new float[chars.Count];
            var total = 0f;
            for (var i = 0; i < chars.Count; i++)
            {
                var s = SpriteFor(chars[i]);
                widths[i] = height * s.rect.width / Mathf.Max(1f, s.rect.height);
                total += widths[i] + (i > 0 ? spacing * height : 0f);
            }
            var scale = shrinkToFit && rect.rect.width > 1f && total > rect.rect.width ? rect.rect.width / total : 1f;

            while (images.Count < chars.Count)
            {
                var go = new GameObject("Algarismo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)) { hideFlags = HideFlags.HideAndDontSave };
                go.transform.SetParent(container, false);
                var image = go.GetComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = true;
                images.Add(image);
            }

            var x = -total * scale * 0.5f;
            for (var i = 0; i < images.Count; i++)
            {
                var image = images[i];
                if (i >= chars.Count) { image.gameObject.SetActive(false); continue; }
                image.gameObject.SetActive(true);
                var r = image.rectTransform;
                r.anchorMin = r.anchorMax = Vector2.one * 0.5f;
                r.pivot = new Vector2(0f, 0.5f);
                var w = widths[i] * scale;
                r.sizeDelta = new Vector2(w, height * scale);
                r.anchoredPosition = new Vector2(x, 0f);
                x += w + spacing * height * scale;
                image.sprite = SpriteFor(chars[i]);
                image.color = tint;
            }
        }
    }
}
