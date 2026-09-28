// "Nível N" on the level start window (Sweet Sugar's MenuPlay), for the level the lobby is about to open.
using TMPro;
using UnityEngine;

namespace RoyalAves.Meta
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LevelStartTitle : MonoBehaviour
    {
        [SerializeField] string format = "Nível {0}";

        void OnEnable() => GetComponent<TextMeshProUGUI>().text = string.Format(format, PlayerPrefs.GetInt("OpenLevel", 1));
    }
}
