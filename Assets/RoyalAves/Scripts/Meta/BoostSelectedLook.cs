// Level start window: a selected booster turns green, as in the mock-up. Sweet Sugar's BoostIcon still does the work
// and shows its check (restyled as a green badge with a check mark); this only swaps the button behind the booster.
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

        BoostIcon boost;
        bool? shown;

        void Awake() => boost = GetComponent<BoostIcon>();

        void OnEnable() => shown = null;

        void LateUpdate()
        {
            var on = boost.check != null && boost.check.activeSelf;
            if (on == shown || background == null) return;
            shown = on;
            background.sprite = on && selected != null ? selected : normal;
        }
    }
}
