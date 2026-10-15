using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    // Cria/abre o asset de luzes do mapa 3D. A janela "Mapa de niveis" redesenha ao vivo ao editar a lista.
    public static class RoyalAvesWorldMapLights
    {
        const string Path = "Assets/RoyalAves/Resources/RoyalAvesWorldMapLights.asset";

        [InitializeOnLoadMethod]
        static void Auto() => EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling) Ensure();
        };

        [MenuItem("Royal Aves/Mapa 3D/Luzes do mapa (editar)")]
        static void Open() => Selection.activeObject = Ensure();

        static WorldMapLightSet Ensure()
        {
            var set = AssetDatabase.LoadAssetAtPath<WorldMapLightSet>(Path);
            if (set != null) return set;
            set = ScriptableObject.CreateInstance<WorldMapLightSet>();
            AssetDatabase.CreateAsset(set, Path);
            AssetDatabase.SaveAssets();
            return set;
        }
    }

    [CustomEditor(typeof(WorldMapLightSet))]
    public sealed class WorldMapLightSetEditor : UnityEditor.Editor
    {
        static void Adjust(WorldMapLightSet set, System.Action<WorldMapLightSet.Shaft> change)
        {
            Undo.RecordObject(set, "Ajustar luzes");
            foreach (var s in set.shafts) if (s != null) change(s);
            EditorUtility.SetDirty(set);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Cada item e uma luz: ponto de contato (fase + lateral + altura), inclinacao, comprimento, largura, faixas, cor e brilhos. " +
                "A janela Royal Aves > Mapa de niveis mostra as mudancas na hora.", MessageType.Info);
            DrawDefaultInspector();
            var set = (WorldMapLightSet)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Ajuste rapido (todas as luzes)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Girar -5°")) Adjust(set, s => s.angle = Mathf.Clamp(s.angle - 5, -75, 75));
                if (GUILayout.Button("Girar +5°")) Adjust(set, s => s.angle = Mathf.Clamp(s.angle + 5, -75, 75));
                if (GUILayout.Button("Tamanho -10%")) Adjust(set, s => s.scale = Mathf.Clamp(s.scale * .9f, .2f, 4));
                if (GUILayout.Button("Tamanho +10%")) Adjust(set, s => s.scale = Mathf.Clamp(s.scale * 1.1f, .2f, 4));
            }
            if (GUILayout.Button("Adicionar luz (copia da ultima)"))
            {
                Undo.RecordObject(set, "Adicionar luz");
                var last = set.shafts.Count > 0 ? set.shafts[set.shafts.Count - 1] : new WorldMapLightSet.Shaft();
                var copy = JsonUtility.FromJson<WorldMapLightSet.Shaft>(JsonUtility.ToJson(last));
                copy.name = "Luz " + (set.shafts.Count + 1);
                copy.level += 2;
                copy.seed = Random.Range(1, 9999);
                set.shafts.Add(copy);
                EditorUtility.SetDirty(set);
            }
        }
    }
}
