using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace SweetSugar.Scripts.Editor
{
    public class LayerTransferTool : EditorWindow
    {
        private string exportPath = "";
        private string importPath = "";

        [MenuItem("Tools/Layer Transfer Tool")]
        static void Init()
        {
            LayerTransferTool window = (LayerTransferTool)EditorWindow.GetWindow(typeof(LayerTransferTool));
            window.titleContent = new GUIContent("Layer Transfer");
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("Layer Settings Transfer Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Export section
            GUILayout.Label("Export Layers", EditorStyles.boldLabel);
            if (GUILayout.Button("Export Layer Settings"))
            {
                ExportLayers();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Separator();

            // Import section
            GUILayout.Label("Import Layers", EditorStyles.boldLabel);
            if (GUILayout.Button("Import Layer Settings"))
            {
                ImportLayers();
            }
        }

        void ExportLayers()
        {
            string path = EditorUtility.SaveFilePanel(
                "Export Layer Settings",
                "",
                "LayerSettings.json",
                "json"
            );

            if (string.IsNullOrEmpty(path))
                return;

            var settings = new LayerSettings();
            
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            
            // Export layers
            SerializedProperty layersProp = tagManager.FindProperty("layers");
            for (int i = 0; i < layersProp.arraySize; i++)
            {
                SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(layerProp.stringValue))
                {
                    settings.layers[i] = layerProp.stringValue;
                }
            }

            // Export sorting layers
            SerializedProperty sortingLayersProp = tagManager.FindProperty("m_SortingLayers");
            settings.sortingLayers = new SortingLayerData[sortingLayersProp.arraySize];
            
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                SerializedProperty sortingLayerProp = sortingLayersProp.GetArrayElementAtIndex(i);
                settings.sortingLayers[i] = new SortingLayerData
                {
                    name = sortingLayerProp.FindPropertyRelative("name").stringValue,
                    uniqueID = sortingLayerProp.FindPropertyRelative("uniqueID").intValue,
                    orderInLayer = i
                };
            }

            // Save to file
            File.WriteAllText(path, JsonUtility.ToJson(settings, true));
            Debug.Log("Layer and sorting layer settings exported to: " + path);
        }

        void ImportLayers()
        {
            string path = EditorUtility.OpenFilePanel(
                "Import Layer Settings",
                "",
                "json"
            );

            if (string.IsNullOrEmpty(path))
                return;

            string jsonContent = File.ReadAllText(path);
            var settings = JsonUtility.FromJson<LayerSettings>(jsonContent);

            if (settings == null)
            {
                Debug.LogError("Failed to load layer settings");
                return;
            }

            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            bool changed = false;

            // Apply layers
            SerializedProperty layersProp = tagManager.FindProperty("layers");
            for (int i = 0; i < layersProp.arraySize && i < settings.layers.Length; i++)
            {
                if (!string.IsNullOrEmpty(settings.layers[i]))
                {
                    SerializedProperty sp = layersProp.GetArrayElementAtIndex(i);
                    if (sp.stringValue != settings.layers[i])
                    {
                        sp.stringValue = settings.layers[i];
                        changed = true;
                    }
                }
            }

            // Apply sorting layers
            if (settings.sortingLayers != null && settings.sortingLayers.Length > 0)
            {
                SerializedProperty sortingLayersProp = tagManager.FindProperty("m_SortingLayers");
                sortingLayersProp.ClearArray();

                for (int i = 0; i < settings.sortingLayers.Length; i++)
                {
                    sortingLayersProp.InsertArrayElementAtIndex(i);
                    SerializedProperty sortingLayerProp = sortingLayersProp.GetArrayElementAtIndex(i);
                    
                    sortingLayerProp.FindPropertyRelative("name").stringValue = settings.sortingLayers[i].name;
                    sortingLayerProp.FindPropertyRelative("uniqueID").intValue = settings.sortingLayers[i].uniqueID;
                    changed = true;
                }
            }

            if (changed)
            {
                tagManager.ApplyModifiedProperties();
                Debug.Log("Layer and sorting layer settings imported successfully");
            }
            else
            {
                Debug.Log("No changes were needed in layer settings");
            }
        }
    }

    [Serializable]
    public class LayerSettings
    {
        public string[] layers = new string[32]; // Unity supports 32 layers
        public SortingLayerData[] sortingLayers; // Sorting layers
    }

    [Serializable]
    public class SortingLayerData
    {
        public string name;
        public int uniqueID;
        public int orderInLayer;
    }
}