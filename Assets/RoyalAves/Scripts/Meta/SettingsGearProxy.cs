// The settings gear as the last round button of the booster bar.
//
// Sweet Sugar's gear is CanvasGlobal/SettingsButton and has to stay there: MenuReference.HideAll keeps it by name as a
// direct child of the canvas, LobbyController finds it the same way, and its click opens a menu that lives inside
// CanvasGlobal — a reference another prefab cannot hold. So the button in the bar is a stand-in: it fires the real
// gear's own click (menu and click sound included) and keeps the original out of sight while the game HUD is up.
// Replace this when the menus stop being Sweet Sugar's; the bar button then calls the new menu directly.
using SweetSugar.Scripts.System;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    [RequireComponent(typeof(Button))]
    public class SettingsGearProxy : MonoBehaviour
    {
        CanvasGroup hidden;

        void Awake() => GetComponent<Button>().onClick.AddListener(OpenSettings);

        void OnEnable() => ShowOriginal(false);

        void OnDisable() => ShowOriginal(true);

        public void OpenSettings()
        {
            var gear = Original();
            if (gear == null)
            {
                Debug.LogWarning("Correio Mágico: CanvasGlobal/SettingsButton não encontrado; o menu de configurações não abriu.");
                return;
            }
            // Invoking the click works even with the original hidden, and carries its menu and its click sound.
            if (gear.GetComponent<Button>() is Button button) button.onClick.Invoke();
        }

        // Hidden with a CanvasGroup, not by turning it off: LobbyController turns the same object on and off around the
        // map, and the two would undo each other.
        void ShowOriginal(bool visible)
        {
            if (hidden == null)
            {
                var gear = Original();
                if (gear == null) return;
                hidden = gear.GetComponent<CanvasGroup>();
                if (hidden == null) hidden = gear.gameObject.AddComponent<CanvasGroup>();
            }
            hidden.alpha = visible ? 1f : 0f;
            hidden.blocksRaycasts = visible;
            hidden.interactable = visible;
        }

        static Transform Original() =>
            MenuReference.THIS != null ? MenuReference.THIS.transform.Find("SettingsButton") : null;
    }
}
