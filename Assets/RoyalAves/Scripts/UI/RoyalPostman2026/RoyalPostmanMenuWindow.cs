using System;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// Authored menu view. Navigation stays in the lobby's existing handlers.
    public sealed class RoyalPostmanMenuWindow : MonoBehaviour
    {
        public enum Destination { Records, Teams, Collection, Events, Profile, Inventory, Shop, Settings }

        [Serializable] public sealed class Entry
        {
            public Destination destination;
            public Button button;
        }

        [SerializeField] Button closeButton;
        [SerializeField] Entry[] entries = Array.Empty<Entry>();
        Action<Destination> navigate;
        Action tap, visibilityChanged;
        bool wired;

        public bool IsOpen => gameObject.activeSelf;

        void Awake() => Wire();

        public void Bind(Action<Destination> onNavigate, Action onTap, Action onVisibilityChanged)
        {
            navigate = onNavigate;
            tap = onTap;
            visibilityChanged = onVisibilityChanged;
            Wire();
        }

        void Wire()
        {
            if (wired) return;
            wired = true;
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            foreach (var entry in entries)
            {
                if (entry == null || entry.button == null) continue;
                var destination = entry.destination;
                entry.button.onClick.AddListener(() => Choose(destination));
            }
        }

        public void Open()
        {
            if (IsOpen) return;
            Wire();
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            GetComponent<RoyalPostmanSettingsFit>()?.Fit();
            tap?.Invoke();
            visibilityChanged?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen) return;
            Dismiss();
            tap?.Invoke();
        }

        public void Dismiss()
        {
            if (!IsOpen) return;
            gameObject.SetActive(false);
            visibilityChanged?.Invoke();
        }

        void Choose(Destination destination)
        {
            if (!IsOpen || navigate == null) return;
            // Close before navigation so settings/shops/other routes can own the next frame.
            // The existing destination handler already plays its tap sound.
            Dismiss();
            navigate(destination);
        }
    }
}
