// Playtest aid: a slider at the bottom of the screen that sets the game speed from 0.1x (slow motion, to see the effects)
// up to 3x (fast-forward for testing). Only in the Editor and in development builds. It creates itself when the first scene
// loads and survives scene changes. The value is not saved: it goes back to 1x when the game starts again.
// Sweet Sugar's LevelManager and menus set Time.timeScale themselves every frame, so the speed is applied in LateUpdate,
// after all of them, to keep the chosen value.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Debugging
{
    public class TimeScaleOverlay : MonoBehaviour
    {
        const float Min = 0.1f;
        const float Max = 3f;
        static TimeScaleOverlay instance;
        float scale = 1f;
        TextMeshProUGUI label;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (instance != null) return;

            var overlay = new GameObject("TimeScaleOverlay");
            Object.DontDestroyOnLoad(overlay);
            instance = overlay.AddComponent<TimeScaleOverlay>();

            var canvas = overlay.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = overlay.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            overlay.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(overlay.transform, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = new Vector2(0f, 140f);
            panelRect.anchoredPosition = new Vector2(0f, 20f);

            instance.label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            instance.label.transform.SetParent(panel.transform, false);
            instance.label.alignment = TextAlignmentOptions.Center;
            instance.label.fontSize = 40;
            instance.label.color = Color.white;
            var labelRect = (RectTransform)instance.label.transform;
            labelRect.anchorMin = new Vector2(0f, 0.55f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var sliderGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderGo.name = "TimeScaleSlider";
            sliderGo.transform.SetParent(panel.transform, false);
            var sliderRect = (RectTransform)sliderGo.transform;
            sliderRect.anchorMin = new Vector2(0.1f, 0f);
            sliderRect.anchorMax = new Vector2(0.9f, 0.5f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            var slider = sliderGo.GetComponent<Slider>();
            slider.minValue = Min;
            slider.maxValue = Max;
            slider.value = 1f;
            slider.onValueChanged.AddListener(v => instance.SetScale(v));
            instance.SetScale(1f);
        }

        void SetScale(float value)
        {
            // Steps of 0.1x, so the slider always lands on a readable value.
            scale = Mathf.Clamp(Mathf.Round(value * 10f) / 10f, Min, Max);
            Time.timeScale = scale;
            if (label != null) label.text = $"Velocidade: {scale:0.0}x";
        }

        void LateUpdate()
        {
            Time.timeScale = scale;
        }
    }
}
#endif
