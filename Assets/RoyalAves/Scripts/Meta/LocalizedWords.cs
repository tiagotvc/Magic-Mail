// Short hand-authored words/labels in PT/ES/EN, for text drawn with the gold sprite font (DialogoLetras) —
// Resources/Localization/*.txt (LocalizationManager) is id-keyed and meant for the Sweet Sugar template's
// own text objects; this is a readable key->3 languages table for OUR dialog labels instead, reading the
// SAME current-language source of truth (CrosssceneData.selectedLanguage) so it never disagrees with
// whatever language the rest of the game is already showing.
using System.Collections.Generic;
using SweetSugar.Scripts.Level;
using UnityEngine;

namespace RoyalAves.Meta
{
    [CreateAssetMenu(fileName = "RoyalAvesWords", menuName = "Royal Aves/Palavras em 3 idiomas")]
    public class LocalizedWords : ScriptableObject
    {
        public const string ResourcePath = "RoyalAvesWords";

        [System.Serializable]
        public class Entry
        {
            public string key;
            [Tooltip("Português")] public string pt;
            [Tooltip("Español")] public string es;
            [Tooltip("English")] public string en;
        }

        public List<Entry> words = new List<Entry>();

        static LocalizedWords instance;
        public static LocalizedWords Instance =>
            instance != null ? instance : instance = Resources.Load<LocalizedWords>(ResourcePath);

        /// The word for "key" in whatever language CrosssceneData.selectedLanguage currently points to
        /// (English if nothing matches). Returns "key" itself if there's no entry for it — missing
        /// translations show up as the key instead of silently going blank.
        public string Get(string key)
        {
            var entry = words.Find(w => w.key == key);
            if (entry == null) return key;
            var language = CrosssceneData.selectedLanguage ?? "";
            if (language.StartsWith("Portuguese")) return entry.pt;
            if (language.StartsWith("Spanish")) return entry.es;
            return entry.en;
        }
    }
}
