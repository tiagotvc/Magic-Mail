using RoyalAves.Effects;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    /// <summary>
    /// Menu Royal Aves/VFX/Impact Burst Preview: dispara as explosões (Small / Potion / Chain) sem precisar jogar uma fase.
    /// Fora do Play ela anima a explosão na Scene view (origem ou posição escolhida); no Play usa o pool real.
    /// </summary>
    public sealed class ImpactBurstPreviewWindow : EditorWindow
    {
        ImpactPreset preset = ImpactPreset.Potion;
        float cellSize = 1.2f;
        float radiusCells = 0f;
        Vector3 position = Vector3.zero;
        bool loop;
        float slowMotion = 1f;

        ImpactBurst preview;
        double lastTime, nextLoop;

        [MenuItem("Royal Aves/VFX/Impact Burst Preview")]
        static void Open() => GetWindow<ImpactBurstPreviewWindow>("Impact Burst");

        void OnEnable() { EditorApplication.update += OnUpdate; }

        void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
            Clear();
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Edit mode: anima na Scene view. Play mode: usa o pool do jogo. Ajuste cores/quantidades nos prefabs em " +
                "Assets/RoyalAves/Resources/Effects/Impact.", MessageType.Info);
            preset = (ImpactPreset)EditorGUILayout.EnumPopup("Preset", preset);
            cellSize = EditorGUILayout.FloatField(new GUIContent("Tamanho da casa (mundo)", "squareWidth do tabuleiro (1.2)"), cellSize);
            radiusCells = EditorGUILayout.FloatField(new GUIContent("Alcance em casas", "0 = o do preset; a bomba usa 3, a combinada usa o tabuleiro todo"), radiusCells);
            position = EditorGUILayout.Vector3Field("Posição", position);
            slowMotion = EditorGUILayout.Slider(new GUIContent("Velocidade", "1 = normal; menor = câmera lenta"), slowMotion, 0.05f, 1f);
            loop = EditorGUILayout.Toggle("Repetir", loop);

            EditorGUILayout.Space();
            if (GUILayout.Button("Disparar", GUILayout.Height(32))) Fire();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Small")) { preset = ImpactPreset.Small; Fire(); }
                if (GUILayout.Button("Potion")) { preset = ImpactPreset.Potion; Fire(); }
                if (GUILayout.Button("Chain")) { preset = ImpactPreset.Chain; Fire(); }
            }
            if (GUILayout.Button("Selecionar prefab do preset"))
                Selection.activeObject = Resources.Load<ImpactBurst>(ImpactBurst.ResourceFolder + preset);
        }

        void Fire()
        {
            if (Application.isPlaying)
            {
                ImpactBurst.Play(preset, position, cellSize, radiusCells);
                return;
            }
            Clear();
            var prefab = Resources.Load<ImpactBurst>(ImpactBurst.ResourceFolder + preset);
            if (prefab == null) { Debug.LogError("Prefab do preset não encontrado: " + preset); return; }
            preview = (ImpactBurst)PrefabUtility.InstantiatePrefab(prefab);
            preview.gameObject.hideFlags = HideFlags.DontSave;
            preview.gameObject.SetActive(false);
            preview.Begin(position, cellSize, radiusCells);
            preview.gameObject.SetActive(true);
            lastTime = EditorApplication.timeSinceStartup;
            SceneView.RepaintAll();
        }

        void OnUpdate()
        {
            if (Application.isPlaying) return;
            var now = EditorApplication.timeSinceStartup;
            if (preview != null)
            {
                var dt = (float)(now - lastTime) * slowMotion;
                lastTime = now;
                preview.Tick(Mathf.Min(dt, 0.05f));
                SceneView.RepaintAll();
                if (!preview.Active) { Clear(); nextLoop = now + 0.6; }
            }
            else if (loop && now >= nextLoop && nextLoop > 0) Fire();
        }

        void Clear()
        {
            if (preview != null) DestroyImmediate(preview.gameObject);
            preview = null;
        }
    }
}
