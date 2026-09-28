// One upgrade in the area window, as in the web prototype: icon, name and a "Restaurar ★ n" button for the next
// upgrade, or a faded "Depois" preview of the one after it. LobbyController fills it in and listens to its button.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public class LobbyTaskRow : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI title;
        [SerializeField] Button button;
        [SerializeField] TextMeshProUGUI caption;
        [SerializeField] TextMeshProUGUI cost;
        [SerializeField] CanvasGroup group;

        public Button Button => button;

        public void Show(AreaTask task, bool isNext, bool affordable)
        {
            icon.sprite = task.icon;
            icon.enabled = task.icon != null;
            title.text = task.title;
            caption.text = isNext ? "Restaurar" : "Depois";
            cost.text = task.starCost.ToString();
            button.interactable = isNext && affordable;
            group.alpha = isNext ? 1 : 0.55f;
        }
    }
}
