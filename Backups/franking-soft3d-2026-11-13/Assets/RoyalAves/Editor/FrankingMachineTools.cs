using System.IO;
using RoyalAves.Gameplay;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class FrankingMachineTools
    {
        public static void Build()
        {
            const string art = "Assets/RoyalAves/Art/Obstacles/FrankingMachine/CM_maquina_corpo_v2.png";
            AssetDatabase.ImportAsset(art);
            var importer = (TextureImporter)AssetImporter.GetAtPath(art);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
            var root = new GameObject("FrankingMachine_1x1");
            try
            {
                var animation = root.AddComponent<MailCubbyAnimation>();
                var body = new GameObject("Press");
                body.transform.SetParent(root.transform, false);
                animation.cabinet = body.AddComponent<SpriteRenderer>();
                animation.cabinet.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(art);
                animation.cabinet.sortingLayerName = "Item mask";
                animation.cabinet.sortingOrder = 30;
                const string leverPath = "Assets/RoyalAves/Art/Obstacles/FrankingMachine/CM_maquina_alavanca_v2.png";
                AssetDatabase.ImportAsset(leverPath);
                var leverImporter = (TextureImporter)AssetImporter.GetAtPath(leverPath);
                var settings = new TextureImporterSettings();
                leverImporter.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(0.49f, 0.14f);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                leverImporter.SetTextureSettings(settings);
                leverImporter.SaveAndReimport();
                var lever = new GameObject("LeverPivot");
                lever.transform.SetParent(root.transform, false);
                lever.transform.localPosition = new Vector3(0.55f, 0.23f, -0.03f);
                lever.transform.localScale = Vector3.one * 0.47f;
                lever.transform.localRotation = Quaternion.Euler(0, 0, -28f);
                var leverRenderer = lever.AddComponent<SpriteRenderer>();
                leverRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(leverPath);
                leverRenderer.sortingLayerName = "Item mask";
                leverRenderer.sortingOrder = 32;
                root.AddComponent<FrankingMachineLever>().lever = lever.transform;
                var letter = new GameObject("GeneratedLetter");
                letter.transform.SetParent(root.transform, false);
                letter.transform.localPosition = new Vector3(0, -0.43f, -0.04f);
                var renderer = letter.AddComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/Obstacles/MailCubby/CM_escaninho_carta_cartoon_v2.png");
                renderer.sortingLayerName = "Item mask";
                renderer.sortingOrder = 31;
                animation.letters = new[] { renderer };
                animation.launchDuration = 0.14f;
                animation.launchDisplacement = new Vector3(0, -0.25f, -0.03f);
                animation.launchStartYScale = 0.18f;
                animation.emissionDuration = 0.65f;
                animation.drawOverHud = true;
                Directory.CreateDirectory("Assets/RoyalAves/Prefabs/Elements");
                PrefabUtility.SaveAsPrefabAsset(root, "Assets/RoyalAves/Prefabs/Elements/FrankingMachine.prefab");
            }
            finally { Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
        }
    }
}
