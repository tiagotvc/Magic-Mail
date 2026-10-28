using System;
using SweetSugar.Scripts.Level;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>Painted title variants share the game's existing language selection.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Image))]
    public sealed class RoyalPostmanHeaderTitle : MonoBehaviour
    {
        public enum EditorLanguage { Portuguese, English, GameLanguage }
        public Sprite portuguese, english;
        public Sprite petsPortuguese, petsEnglish;
        bool petsSection;
        public void SetPetsSection(bool showPets){petsSection=showPets;Refresh();}
        [Tooltip("Preview only. The running game uses its selected language.")]
        public EditorLanguage editorLanguage;
        public float titleHeight = 110;
        public float maximumWidth = 640;
        Image target;

        void OnEnable() => Refresh();
        void OnValidate() => Refresh();
        void Update() => Refresh();
        public void Refresh()
        {
            if (target == null) target = GetComponent<Image>();
            string language = CrosssceneData.selectedLanguage;
            bool pt = string.IsNullOrEmpty(language)
                ? Application.systemLanguage == SystemLanguage.Portuguese
                : language.StartsWith("Portuguese", StringComparison.OrdinalIgnoreCase)
                    || language.StartsWith("pt", StringComparison.OrdinalIgnoreCase);
            if (!Application.isPlaying && editorLanguage != EditorLanguage.GameLanguage)
                pt = editorLanguage == EditorLanguage.Portuguese;
            var sprite = petsSection ? (pt ? petsPortuguese : petsEnglish) : (pt ? portuguese : english);
            if (sprite == null || target == null) return;
            if (target.sprite != sprite) target.sprite = sprite;
            target.preserveAspect = true;
            float height = Mathf.Max(1, titleHeight);
            float width = height * sprite.rect.width / sprite.rect.height;
            if (width > maximumWidth) { height *= maximumWidth / width; width = maximumWidth; }
            var size = new Vector2(width, height);
            if ((target.rectTransform.sizeDelta - size).sqrMagnitude > .001f)
                target.rectTransform.sizeDelta = size;
        }
    }
}
