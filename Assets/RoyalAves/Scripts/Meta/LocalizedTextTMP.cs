// Texto (TextMeshPro) com uma versão por idioma, ligado ao idioma global do jogo (a mesma fonte do LocalizedImage:
// CrosssceneData.selectedLanguage). Fica no mesmo objeto de um TextMeshProUGUI / TextMeshPro.
//
// Como usar: adicione "Localized Text" ao objeto e preencha Português / English (Spanish é opcional) na lista.
// Idioma sem texto cai no "Idioma reserva". Para destacar palavras use <hl>palavra</hl>: vira a cor de destaque abaixo.
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace RoyalAves.Meta
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Royal Aves/Localized Text")]
    public class LocalizedTextTMP : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public GameLanguage language = GameLanguage.English;
            [TextArea(2, 6)] public string text;
        }

        [SerializeField] List<Entry> texts = new List<Entry>
        {
            new Entry { language = GameLanguage.Portuguese },
            new Entry { language = GameLanguage.English },
        };

        [Tooltip("Usado quando o idioma ativo não tem texto na lista.")]
        [SerializeField] GameLanguage fallbackLanguage = GameLanguage.English;

        [Tooltip("Cor das palavras entre <hl> e </hl>.")]
        [SerializeField] Color highlightColor = new Color(1f, 0.12f, 0.6f, 1f);

        [Header("Pré-visualização no editor (fora do Play)")]
        [SerializeField] bool previewInEditor;
        [SerializeField] GameLanguage previewLanguage = GameLanguage.Portuguese;

        TMP_Text label;
        GameLanguage? shown;
        int frame;

        /// Reaplica o texto do idioma atual neste objeto.
        public void Refresh() => Apply(true);

        void OnEnable() => Apply(true);

        void Update()
        {
            // O idioma quase nunca muda: confere algumas vezes por segundo, não a cada quadro.
            if ((++frame & 15) != 0) return;
            Apply(false);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () => { if (this != null) Apply(true); };
        }
#endif

        /// Define (ou troca) o texto de um idioma por código. Texto vazio remove o idioma da lista.
        public void SetText(GameLanguage language, string text)
        {
            texts.RemoveAll(e => e == null || e.language == language);
            if (!string.IsNullOrEmpty(text)) texts.Add(new Entry { language = language, text = text });
            Apply(true);
        }

        public void SetTexts(string portuguese, string english, string spanish = null)
        {
            texts.Clear();
            if (!string.IsNullOrEmpty(portuguese)) texts.Add(new Entry { language = GameLanguage.Portuguese, text = portuguese });
            if (!string.IsNullOrEmpty(english)) texts.Add(new Entry { language = GameLanguage.English, text = english });
            if (!string.IsNullOrEmpty(spanish)) texts.Add(new Entry { language = GameLanguage.Spanish, text = spanish });
            Apply(true);
        }

        void Apply(bool force)
        {
            var language = !Application.isPlaying && previewInEditor ? previewLanguage : LocalizedImage.Current;
            if (!force && shown == language) return;
            shown = language;

            var text = Find(language) ?? (language == fallbackLanguage ? null : Find(fallbackLanguage));
            if (text == null) return;   // nada cadastrado: não apaga o texto que já estava no objeto

            if (label == null) label = GetComponent<TMP_Text>();
            if (label == null) return;
            label.text = Expand(text);
        }

        string Find(GameLanguage language)
        {
            foreach (var entry in texts)
                if (entry != null && entry.language == language && !string.IsNullOrEmpty(entry.text)) return entry.text;
            return null;
        }

        string Expand(string text)
        {
            if (text.IndexOf("<hl>", StringComparison.Ordinal) < 0) return text;
            var hex = ColorUtility.ToHtmlStringRGB(highlightColor);
            return text.Replace("<hl>", "<color=#" + hex + ">").Replace("</hl>", "</color>");
        }
    }
}
