// Textos da modal de vidas (LiveShop), ligados sozinhos pelo nome/conteúdo dos objetos (o LifeShop adiciona
// este componente):
// - Title ("Mais vidas!" / "More lives!") e o rótulo "Tempo até a próxima vida:" / "Time until next life:" em PT/EN.
// - "Price (1)" (no coração): vidas atuais.
// - "Timer": só minutos e segundos até a próxima vida (MM:SS).
// - Botão "Ver vídeo" / "Watch video": uma vez a cada 3 horas (LifeAdCooldown); bloqueado mostra o tempo restante.
using SweetSugar.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    [DisallowMultipleComponent]
    public class LifeShopPresenter : MonoBehaviour
    {
        TMP_Text title, heartCount, timer, untilLabel, watchLabel;
        Button watchButton;
        bool found;

        void Find()
        {
            found = true;
            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                var n = text.name;
                var t = (text.text ?? "").ToLowerInvariant();
                if (n == "Title") title = text;
                else if (n == "Price (1)") heartCount = text;
                else if (n == "Timer") timer = text;
                else if (t.StartsWith("tempo at") || t.StartsWith("time until")) untilLabel = text;
                else if (t.StartsWith("ver v") || t.StartsWith("watch")) watchLabel = text;
            }
            if (watchLabel != null) watchButton = watchLabel.GetComponentInParent<Button>(true);
        }

        void LateUpdate()
        {
            if (!found) Find();
            var pt = LocalizedImage.Current == GameLanguage.Portuguese;

            Set(title, pt ? "Mais vidas!" : "More lives!");
            Set(untilLabel, pt ? "Tempo até a próxima vida:" : "Time until next life:");
            if (InitScript.Instance == null) return;

            Set(heartCount, InitScript.lifes.ToString());

            if (timer != null)
            {
                var full = InitScript.lifes >= InitScript.Instance.CapOfLife;
                var seconds = full ? 0 : Mathf.Max(0, Mathf.CeilToInt(InitScript.RestLifeTimer));
                Set(timer, string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60));
            }

            if (watchButton != null)
            {
                var available = LifeAdCooldown.IsAvailable;
                if (watchButton.interactable != available) watchButton.interactable = available;
                if (watchLabel != null)
                {
                    var left = LifeAdCooldown.Remaining;
                    Set(watchLabel, available
                        ? (pt ? "Ver vídeo" : "Watch video")
                        : string.Format("{0:00}:{1:00}:{2:00}", (int)left.TotalHours, left.Minutes, left.Seconds));
                }
            }
        }

        static void Set(TMP_Text text, string value)
        {
            if (text != null && text.text != value) text.text = value;
        }
    }
}
