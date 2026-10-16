// Playtest aid in the Unity menu: Royal Aves > Velocidade do jogo. A slider from 0.1x (slow motion, to see the effects) to 3x
// (fast-forward). It only acts while Play mode is running. The speed goes into the DebugSettings that Sweet Sugar's
// LevelManager already reads every frame, and into Time.timeScale for the screens without a LevelManager (lobby, menus).
// The DebugSettings values are only changed in memory; they go back to 1x when Play mode ends, so nothing is saved.
using SweetSugar.Scripts.System;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public class RoyalAvesSpeedWindow : EditorWindow
    {
        const float Min = 0.1f;
        const float Max = 3f;
        float speed = 1f;

        [MenuItem("Royal Aves/Velocidade do jogo")]
        public static void Open()
        {
            var window = GetWindow<RoyalAvesSpeedWindow>("Velocidade do jogo");
            window.minSize = new Vector2(340, 120);
            window.Show();
        }

        void OnEnable()
        {
            EditorApplication.update += ApplySpeed;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        void OnDisable()
        {
            EditorApplication.update -= ApplySpeed;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            SetSpeed(1f);
        }

        void OnGUI()
        {
            EditorGUILayout.Space(8);
            var value = EditorGUILayout.Slider($"Velocidade: {speed:0.0}x", speed, Min, Max);
            // Steps of 0.1x, so the slider always lands on a readable value.
            speed = Mathf.Clamp(Mathf.Round(value * 10f) / 10f, Min, Max);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Voltar para 1x", GUILayout.Width(130))) speed = 1f;
            EditorGUILayout.LabelField(EditorApplication.isPlaying ? "aplicando no Play" : "entre no Play para aplicar",
                EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        void ApplySpeed()
        {
            if (!EditorApplication.isPlaying) return;
            SetSpeed(speed);
        }

        void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) SetSpeed(1f);
        }

        static void SetSpeed(float value)
        {
            Time.timeScale = value;
            var settings = Resources.Load<DebugSettings>("Scriptable/DebugSettings");
            if (settings == null) return;
            settings.TimeScaleItems = value;
            settings.TimeScaleUI = value;
        }
    }
}
