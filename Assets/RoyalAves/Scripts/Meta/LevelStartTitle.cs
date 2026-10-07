// "Nível N" on the level start window (Sweet Sugar's MenuPlay), for the level the lobby is about to open.
// Drawn with the gold sprite font — set this object's own TextMeshProUGUI "Sprite Asset" field to
// DialogoLetras in the Inspector. The word comes from LocalizedWords (so it follows PT/ES/EN like the rest
// of the dialog text); the gap before the number uses TMP's <space> tag since the sprite sheets have no
// space glyph of their own.
using TMPro;
using UnityEngine;

namespace RoyalAves.Meta
{
    // ExecuteAlways: redraws in Edit Mode too, so a tweak to wordKey/gap or a sprite font rebuild shows up
    // in the Scene view right away instead of needing a Play to see it.
    [ExecuteAlways]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LevelStartTitle : MonoBehaviour
    {
        [Tooltip("Chave em RoyalAvesWords (ex.: \"nivel\" -> NÍVEL / NIVEL / LEVEL).")]
        [SerializeField] string wordKey = "nivel";
        [Tooltip("Espaço entre a palavra e o número (tag <space=N> do TMP, em unidades da fonte).")]
        [SerializeField] float gap = 20f;

        void OnEnable() => Refresh();
        void OnValidate() => Refresh();

        void Refresh()
        {
            var level = PlayerPrefs.GetInt("OpenLevel", 1);
            var word = LocalizedWords.Instance != null ? LocalizedWords.Instance.Get(wordKey) : wordKey;
            var plain = $"{word.ToUpperInvariant()} {level}";
            var label = GetComponent<TextMeshProUGUI>();
            // Only emit <sprite> tags when a sprite font is actually assigned — otherwise the tags have
            // nothing to resolve against and TMP falls back to its project-wide default Sprite Asset
            // (whatever that happens to be, e.g. a stock emoji set), showing garbage instead of the word.
            label.text = label.spriteAsset != null ? SpriteFontText.ToSpriteTags(plain, gap) : plain;
        }
    }
}
