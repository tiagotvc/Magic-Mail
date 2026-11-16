// Texto desenhado como imagem, um sprite por idioma. Fica no mesmo objeto de uma Image (UI) ou de um SpriteRenderer
// e troca o sprite sozinho conforme o idioma global do jogo (CrosssceneData.selectedLanguage, a mesma fonte que o
// resto do jogo usa); se o jogador mudar de idioma com a tela aberta, a imagem acompanha.
//
// Como usar: adicione "Localized Image" ao objeto, clique em "+" na lista e escolha idioma + sprite.
// Idioma sem imagem cai no "Idioma reserva" (English por padrão); sem nenhum, mantém o sprite que já estava.
using System;
using System.Collections.Generic;
using SweetSugar.Scripts.Level;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public enum GameLanguage { English, Portuguese, Spanish, Russian }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Royal Aves/Localized Image")]
    public class LocalizedImage : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public GameLanguage language = GameLanguage.English;
            public Sprite sprite;
        }

        [SerializeField] List<Entry> images = new List<Entry>
        {
            new Entry { language = GameLanguage.Portuguese },
            new Entry { language = GameLanguage.English },
            new Entry { language = GameLanguage.Spanish },
        };

        [Tooltip("Usado quando o idioma ativo não tem imagem na lista.")]
        [SerializeField] GameLanguage fallbackLanguage = GameLanguage.English;

        [Tooltip("Ajusta o tamanho do objeto ao tamanho da imagem de cada idioma (só para Image de UI).")]
        [SerializeField] bool setNativeSize;

        [Header("Pré-visualização no editor (fora do Play)")]
        [SerializeField] bool previewInEditor;
        [SerializeField] GameLanguage previewLanguage = GameLanguage.Portuguese;

        Image image;
        SpriteRenderer spriteRenderer;
        string lastLanguageName;
        GameLanguage? shown;

        /// Idioma ativo do jogo. Sem escolha do jogador, vale o do sistema (ou o de teste, no editor).
        /// Só para o preview do editor: força um idioma sem mudar o do jogo.
        public static GameLanguage? OverrideLanguage;

        public static GameLanguage Current => OverrideLanguage ?? FromName(CurrentName());

        /// Reaplica o sprite do idioma atual neste objeto.
        public void Refresh() => Apply(true);

        /// Reaplica em todas as imagens, útil logo depois de mudar CrosssceneData.selectedLanguage.
        public static void RefreshAll()
        {
            foreach (var item in FindObjectsByType<LocalizedImage>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                item.Apply(true);
        }

        /// Define (ou troca) o sprite de um idioma por código, por exemplo quando a imagem vem de um catálogo.
        /// Sprite nulo remove o idioma da lista.
        public void SetSprite(GameLanguage language, Sprite sprite)
        {
            images.RemoveAll(e => e == null || e.language == language);
            if (sprite != null) images.Add(new Entry { language = language, sprite = sprite });
            Apply(true);
        }

        /// Tem imagem cadastrada em algum idioma?
        public bool HasAnySprite()
        {
            foreach (var entry in images) if (entry != null && entry.sprite != null) return true;
            return false;
        }

        void OnEnable() => Apply(true);

        void Update()
        {
            // Compara só o texto do idioma global: barato o bastante para rodar em todo quadro.
            var name = Application.isPlaying || !previewInEditor ? CurrentName() : previewLanguage.ToString();
            if (!ReferenceEquals(name, lastLanguageName) && name != lastLanguageName) Apply(false);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Mexer na lista ou no idioma de teste atualiza na hora, sem esperar o próximo quadro.
            UnityEditor.EditorApplication.delayCall += () => { if (this != null) Apply(true); };
        }
#endif

        void Apply(bool force)
        {
            var language = !Application.isPlaying && previewInEditor ? previewLanguage : Current;
            lastLanguageName = language.ToString();
            if (!force && shown == language) return;
            shown = language;

            var sprite = Find(language) ?? (language == fallbackLanguage ? null : Find(fallbackLanguage));
            if (sprite == null) return;   // nada cadastrado: não apaga o sprite que já estava no objeto

            if (image == null) image = GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprite;
                if (setNativeSize) image.SetNativeSize();
                return;
            }
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) spriteRenderer.sprite = sprite;
        }

        Sprite Find(GameLanguage language)
        {
            foreach (var entry in images)
                if (entry != null && entry.language == language && entry.sprite != null) return entry.sprite;
            return null;
        }

        static string CurrentName()
        {
            var selected = CrosssceneData.selectedLanguage;
            if (!string.IsNullOrEmpty(selected)) return Normalize(selected);
#if UNITY_EDITOR
            // Igual ao LocalizationManager: no editor, vale o idioma de teste das configurações de debug.
            var debug = Resources.Load<SweetSugar.Scripts.System.DebugSettings>("Scriptable/DebugSettings");
            if (debug != null) return Normalize(debug.TestLanguage.ToString());
#endif
            return Normalize(Application.systemLanguage.ToString());
        }

        // "Portuguese (Brazil)", "Portuguese" e afins viram o nome do enum.
        static string Normalize(string language)
        {
            foreach (GameLanguage candidate in Enum.GetValues(typeof(GameLanguage)))
                if (language.StartsWith(candidate.ToString(), StringComparison.OrdinalIgnoreCase)) return candidate.ToString();
            return GameLanguage.English.ToString();
        }

        static GameLanguage FromName(string name) =>
            Enum.TryParse(name, out GameLanguage parsed) ? parsed : GameLanguage.English;
    }
}
