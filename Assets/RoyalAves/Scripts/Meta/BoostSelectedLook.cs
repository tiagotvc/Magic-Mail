// Level start window: a selected booster turns green, as in the mock-up. Sweet Sugar's BoostIcon still does the work
// and shows its check (restyled as a green badge with a check mark); this swaps the button behind the booster and,
// on "Center" (the icon itself, via the AllIn1SpriteShader already on it), grayscale when not selected, hue-shifted
// toward green when selected — tune hueShift/selectedSaturation in the Inspector to land on the exact green wanted.
using SweetSugar.Scripts.GUI.Boost;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    [RequireComponent(typeof(BoostIcon))]
    public class BoostSelectedLook : MonoBehaviour
    {
        [SerializeField] Image background;
        [SerializeField] Sprite normal;
        [SerializeField] Sprite selected;

        [Header("Ícone (Center) via AllIn1SpriteShader")]
        [SerializeField] Image center;
        [SerializeField, Range(0, 360)] float selectedHueShift = 100f;
        [SerializeField, Range(0, 2)] float selectedSaturation = 1.3f;
        [SerializeField, Range(0, 2)] float inactiveSaturation = 0f;

        BoostIcon boost;
        Material centerMaterial;
        bool? shown;

        void Awake()
        {
            boost = GetComponent<BoostIcon>();
            if (center != null)
            {
                centerMaterial = center.material = new Material(center.material);
                centerMaterial.EnableKeyword("HSV_ON");
            }
        }

        void OnEnable() => shown = null;

        void LateUpdate()
        {
            var on = boost.check != null && boost.check.activeSelf;
            if (on == shown) return;
            shown = on;
            if (background != null) background.sprite = on && selected != null ? selected : normal;
            if (centerMaterial != null)
            {
                centerMaterial.SetFloat("_HsvSaturation", on ? selectedSaturation : inactiveSaturation);
                centerMaterial.SetFloat("_HsvShift", on ? selectedHueShift : 0f);
            }
        }
    }
}
