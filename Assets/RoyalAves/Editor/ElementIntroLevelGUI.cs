using System.Collections.Generic;
using System.Linq;
using RoyalAves.Meta;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Level;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    /// <summary>
    /// Bloco do editor de fases: mostra se a fase tem tela de introdução, detecta obstáculos que aparecem pela primeira vez
    /// nela e abre o editor de introduções (nome do elemento em imagem, texto do retângulo em português e inglês).
    /// </summary>
    public static class ElementIntroLevelGUI
    {
        static bool open = true;
        // Primeira fase em que cada tipo de bloco aparece (calculado sob demanda, 1 passada pelas fases).
        static Dictionary<SquareTypes, int> firstSeen;

        static readonly HashSet<SquareTypes> Ignored = new HashSet<SquareTypes> { SquareTypes.NONE, SquareTypes.EmptySquare };

        public static void Draw(int level, LevelData data)
        {
            if (data == null) return;
            EditorGUILayout.BeginVertical("box");
            open = EditorGUILayout.Foldout(open, "Introdução de elemento (tela \"Novo item desbloqueado!\")", true);
            if (open)
            {
                var catalog = ElementIntroCatalog.Instance ?? ElementIntroEditorWindow.LoadOrCreateCatalog();
                var current = ElementIntroCatalog.FindByLevel(level);
                if (current != null)
                {
                    EditorGUILayout.LabelField("Esta fase abre a introdução: " + current.DisplayName, EditorStyles.boldLabel);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Editar introdução")) ElementIntroEditorWindow.Open(current);
                        if (GUILayout.Button("Abrir prefab da tela"))
                        {
                            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ElementIntroScreenBuilder.PrefabPath);
                            if (prefab != null) { Selection.activeObject = prefab; AssetDatabase.OpenAsset(prefab); }
                        }
                    }
                }
                else EditorGUILayout.LabelField("Nenhuma introdução definida para a fase " + level + ".");

                DrawDetected(level, data, catalog);

                if (current == null && GUILayout.Button("Criar introdução manual para esta fase (ex.: powerup)"))
                    ElementIntroEditorWindow.Open(ElementIntroEditorWindow.CreateEntry(level, "", "fase-" + level));
                if (GUILayout.Button("Abrir editor de todas as introduções")) ElementIntroEditorWindow.OpenWindow();
            }
            EditorGUILayout.EndVertical();
        }

        static void DrawDetected(int level, LevelData data, ElementIntroCatalog catalog)
        {
            if (firstSeen == null)
            {
                if (GUILayout.Button("Detectar elementos novos desta fase")) firstSeen = ScanFirstAppearances();
                return;
            }
            var introduced = TypesIn(data).Where(t => firstSeen.TryGetValue(t, out var first) && first == level).ToList();
            if (introduced.Count == 0)
            {
                EditorGUILayout.LabelField("Nenhum obstáculo novo nesta fase.", EditorStyles.miniLabel);
            }
            foreach (var type in introduced)
            {
                var resource = "Blocks/" + type;
                var entry = catalog != null ? catalog.entries.Find(e => e != null && e.iconResource == resource) : null;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (entry == null)
                    {
                        EditorGUILayout.LabelField("Novo: " + type + " (sem introdução)");
                        if (GUILayout.Button("Criar", GUILayout.Width(70)))
                            ElementIntroEditorWindow.Open(ElementIntroEditorWindow.CreateEntry(level, resource, type.ToString().ToLowerInvariant()));
                    }
                    else if (entry.level == level)
                    {
                        EditorGUILayout.LabelField("Novo: " + type + "  ✔ " + entry.DisplayName);
                        if (GUILayout.Button("Editar", GUILayout.Width(70))) ElementIntroEditorWindow.Open(entry);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Novo: " + type + " (a introdução está na fase " + entry.level + ")");
                        if (GUILayout.Button("Mover p/ cá", GUILayout.Width(90)))
                        {
                            Undo.RecordObject(catalog, "Mover introdução");
                            entry.level = level;
                            EditorUtility.SetDirty(catalog);
                            AssetDatabase.SaveAssets();
                        }
                    }
                }
            }
            if (GUILayout.Button("Recalcular detecção", EditorStyles.miniButton)) firstSeen = ScanFirstAppearances();
        }

        static IEnumerable<SquareTypes> TypesIn(LevelData data)
        {
            var found = new HashSet<SquareTypes>();
            foreach (var field in data.fields)
            {
                if (field?.levelSquares == null) continue;
                foreach (var square in field.levelSquares)
                {
                    if (square == null) continue;
                    found.Add(square.block);
                    found.Add(square.obstacle);
                    if (square.blocks != null) foreach (var layer in square.blocks) found.Add(layer.squareType);
                }
            }
            found.ExceptWith(Ignored);
            return found;
        }

        static Dictionary<SquareTypes, int> ScanFirstAppearances()
        {
            var result = new Dictionary<SquareTypes, int>();
            var misses = 0;
            try
            {
                for (var n = 1; n <= 1000 && misses < 5; n++)
                {
                    if (n % 20 == 0) EditorUtility.DisplayProgressBar("Detectando elementos novos", "Fase " + n, n / 200f);
                    var data = LoadingManager.LoadlLevel(n, "Levels/Level_");
                    if (data == null || data.fields == null || data.fields.Count == 0) { misses++; continue; }
                    misses = 0;
                    foreach (var type in TypesIn(data)) if (!result.ContainsKey(type)) result[type] = n;
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            return result;
        }
    }
}
