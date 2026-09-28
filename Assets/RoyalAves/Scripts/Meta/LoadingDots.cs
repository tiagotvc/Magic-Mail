// "Carregando" with 1 to 3 dots that come and go, for the loading overlay Sweet Sugar shows between scenes.
using TMPro;
using UnityEngine;

namespace RoyalAves.Meta
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LoadingDots : MonoBehaviour
    {
        [SerializeField] string word = "Carregando";
        [Tooltip("Pontos por segundo.")]
        [SerializeField, Min(0.5f)] float speed = 2f;

        TextMeshProUGUI label;
        float shownSince;

        void Awake() => label = GetComponent<TextMeshProUGUI>();

        void OnEnable() => shownSince = Time.unscaledTime;

        void Update() =>
            label.text = word + new string('.', 1 + (int)((Time.unscaledTime - shownSince) * speed) % 3);
    }
}
