using System.Linq;
using System.Text.RegularExpressions;
using RoyalAves.Meta;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    /// Inspector do SpriteNumberText com um botão que preenche os algarismos 0-9 a partir de uma pasta, e o menu que liga o
    /// componente ao contador de jogadas do HUD.
    [CustomEditor(typeof(SpriteNumberText))]
    public sealed class SpriteNumberTextEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var target = (SpriteNumberText)this.target;
            EditorGUILayout.Space();
            if (!target.HasSprites)
                EditorGUILayout.HelpBox("Enquanto os 10 sprites (0-9) não estiverem preenchidos, o texto normal continua aparecendo.", MessageType.Info);
            if (GUILayout.Button("Procurar sprites 0-9 numa pasta..."))
            {
                var path = EditorUtility.OpenFolderPanel("Pasta com os sprites dos algarismos", Application.dataPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    var relative = FileUtil.GetProjectRelativePath(path);
                    if (string.IsNullOrEmpty(relative)) Debug.LogWarning("Escolha uma pasta dentro do projeto (Assets).");
                    else { Undo.RecordObject(target, "Preencher algarismos"); Fill(target, relative); EditorUtility.SetDirty(target); }
                }
            }
        }

        /// Procura sprites cujo nome termine no algarismo (0, num_0, numero-0, digit_0...). ":" pode ser "colon", "dois_pontos" ou ":".
        public static void Fill(SpriteNumberText target, string folder)
        {
            var sprites = AssetDatabase.FindAssets("t:Sprite", new[] { folder })
                .SelectMany(guid => AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)).OfType<Sprite>())
                .GroupBy(s => s.name).Select(g => g.First()).ToList();
            if (target.digits == null || target.digits.Length < 10) target.digits = new Sprite[10];
            var found = 0;
            for (var d = 0; d < 10; d++)
            {
                var match = sprites.FirstOrDefault(s => Regex.IsMatch(s.name, @"(^|[^0-9])" + d + @"$"));
                if (match != null) { target.digits[d] = match; found++; }
            }
            var colon = sprites.FirstOrDefault(s => Regex.IsMatch(s.name.ToLowerInvariant(), "colon|dois|dots|:"));
            if (colon != null) target.colon = colon;
            var minus = sprites.FirstOrDefault(s => Regex.IsMatch(s.name.ToLowerInvariant(), "minus|menos|dash|hifen"));
            if (minus != null) target.minus = minus;
            Debug.Log("SpriteNumberText: " + found + " de 10 algarismos encontrados em " + folder + ".");
            target.Refresh();
        }

        [MenuItem("Royal Aves/Números em sprite/Ligar ao contador de jogadas (HUD)")]
        static void AddToMovesCounter()
        {
            var added = 0;
            const string panelPath = "Assets/SweetSugar/Scripts/System/Orientation/OrientationPanel.prefab";
            if (System.IO.File.Exists(panelPath))
            {
                var contents = PrefabUtility.LoadPrefabContents(panelPath);
                try
                {
                    foreach (var text in contents.GetComponentsInChildren<TMP_Text>(true).Where(t => t.name == "Limit"))
                        if (text.GetComponent<SpriteNumberText>() == null) { text.gameObject.AddComponent<SpriteNumberText>(); added++; }
                    if (added > 0) PrefabUtility.SaveAsPrefabAsset(contents, panelPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            foreach (var text in Resources.FindObjectsOfTypeAll<TMP_Text>().Where(t => t.name == "Limit" && t.gameObject.scene.IsValid()))
                if (text.GetComponent<SpriteNumberText>() == null) { Undo.AddComponent<SpriteNumberText>(text.gameObject); added++; }
            AssetDatabase.SaveAssets();
            Debug.Log("SpriteNumberText ligado em " + added + " contador(es) de jogadas. Preencha os sprites 0-9 em qualquer um deles " +
                      "(ou no prefab OrientationPanel) e o jogo passa a mostrar os números em sprite.");
        }
    }
}
