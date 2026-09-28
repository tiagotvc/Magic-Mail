// First screen of the app: the Magic Mail art while the game scene (with the agency lobby) loads in the background.
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoyalAves.Meta
{
    public class LoadingScreen : MonoBehaviour
    {
        [SerializeField] string sceneName = "game";
        [Tooltip("Tempo mínimo na tela, para a arte não piscar em aparelhos rápidos.")]
        [SerializeField, Min(0)] float minimumSeconds = 2.5f;
        [Tooltip("Opcional: barra de progresso.")]
        [SerializeField] RectTransform barFill;
        [SerializeField] TextMeshProUGUI label;

        IEnumerator Start()
        {
            var load = SceneManager.LoadSceneAsync(sceneName);
            load.allowSceneActivation = false;
            var start = Time.unscaledTime;
            while (true)
            {
                var elapsed = Time.unscaledTime - start;
                // Unity stops reporting at 0.9 until the scene is allowed to activate.
                var progress = Mathf.Min(Mathf.Clamp01(load.progress / 0.9f), elapsed / Mathf.Max(0.01f, minimumSeconds));
                if (barFill != null) barFill.anchorMax = new Vector2(progress, 1);
                if (label != null) label.text = "Carregando" + new string('.', 1 + (int)(elapsed * 2) % 3);
                if (load.progress >= 0.9f && elapsed >= minimumSeconds) break;
                yield return null;
            }
            if (barFill != null) barFill.anchorMax = Vector2.one;
            load.allowSceneActivation = true;
        }
    }
}
