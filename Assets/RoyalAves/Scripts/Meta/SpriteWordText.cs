// Puts a LocalizedWords entry onto a TextMeshProUGUI using the gold sprite font — set "Sprite Asset" on the
// TextMeshProUGUI itself (Royal Aves > Criar fonte de diálogo builds it) and set "key" here to a row in the
// RoyalAvesWords asset. Uppercased because the sprite sheet only has capital letters.
using TMPro;
using UnityEngine;

namespace RoyalAves.Meta
{
    // ExecuteAlways: redraws in Edit Mode too, so a key change or a sprite font rebuild shows up in the
    // Scene view right away instead of needing a Play to see it.
    [ExecuteAlways]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class SpriteWordText : MonoBehaviour
    {
        [SerializeField] string key;

        TextMeshProUGUI label;

        void Awake() => label = GetComponent<TextMeshProUGUI>();

        void OnEnable() => Refresh();
        void OnValidate() => Refresh();

        /// Switches to a different word (e.g. the level title changing level) and redraws it right away.
        public void SetKey(string newKey)
        {
            key = newKey;
            Refresh();
        }

        void Refresh()
        {
            if (string.IsNullOrEmpty(key)) return;
            var words = LocalizedWords.Instance;
            if (words == null)
            {
                Debug.LogWarning("Royal Aves: falta Resources/RoyalAvesWords.asset (menu Royal Aves > Palavras em 3 idiomas).", this);
                return;
            }
            if (label == null) label = GetComponent<TextMeshProUGUI>();
            label.text = SpriteFontText.ToSpriteTags(words.Get(key).ToUpperInvariant());
        }
    }
}
