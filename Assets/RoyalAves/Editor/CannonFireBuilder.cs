using System.IO;
using System.Linq;
using RoyalAves.Effects;
using SweetSugar.Scripts.Core;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    /// Prepara o asset de configuração do canhão (Assets/RoyalAves/Resources/Effects/CannonFireEffectData.asset): liga o
    /// material aditivo, confere os quadros da arte e grava os ajustes novos no arquivo. Roda sozinho uma vez por versão;
    /// depois o asset é seu: ajuste tudo no Inspector (o efeito lê os valores a cada uso, inclusive com o jogo rodando).
    public static class CannonFireBuilder
    {
        public const string AssetPath = "Assets/RoyalAves/Resources/Effects/CannonFireEffectData.asset";
        const string ArtFolder = "Assets/RoyalAves/Art/Boosters/";
        const string AdditivePath = "Assets/Royal Postman Art/VFX/Materials/ImpactAdditive.mat";
        const string VersionKey = "RoyalAves.CannonFire.Version";
        const int Version = 1;
        const string Menu = "Royal Aves/Efeitos/Canhão/";

        [InitializeOnLoadMethod]
        static void AutoApply()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                var data = AssetDatabase.LoadAssetAtPath<CannonFireEffectData>(AssetPath);
                if (data != null && data.additiveMaterial != null && EditorPrefs.GetInt(VersionKey, 0) >= Version) return;
                try { Apply(); }
                catch (System.Exception e) { Debug.LogError("Não consegui preparar a configuração do canhão: " + e); }
            };
        }

        [MenuItem(Menu + "Selecionar configurações do efeito")]
        static void Select()
        {
            var data = Apply();
            Selection.activeObject = data;
            EditorGUIUtility.PingObject(data);
        }

        [MenuItem(Menu + "Aplicar configuração do efeito")]
        static void ApplyMenu() => Apply();

        [MenuItem(Menu + "Restaurar valores padrão (apaga ajustes)")]
        static void RestoreDefaults()
        {
            var data = Apply();
            if (data == null) return;
            if (!EditorUtility.DisplayDialog("Restaurar padrão do canhão",
                    "Isto volta todos os tempos, tamanhos e cores do canhão para o padrão (a arte e os sons ligados são mantidos). Continuar?",
                    "Restaurar", "Cancelar")) return;
            var fresh = ScriptableObject.CreateInstance<CannonFireEffectData>();
            try
            {
                fresh.name = data.name;
                fresh.IdleFrames = data.IdleFrames; fresh.FireFrames = data.FireFrames;
                fresh.ballSprite = data.ballSprite; fresh.fuseSparkSprite = data.fuseSparkSprite; fresh.fuseSegmentSprite = data.fuseSegmentSprite;
                fresh.additiveMaterial = data.additiveMaterial;
                fresh.entranceSound = data.entranceSound; fresh.fuseSound = data.fuseSound; fresh.shotSound = data.shotSound;
                Undo.RecordObject(data, "Restaurar padrão do canhão");
                EditorUtility.CopySerialized(fresh, data);
            }
            finally { Object.DestroyImmediate(fresh); }
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
            Debug.Log("Configuração do canhão restaurada para o padrão.");
        }

        // Usa o canhão de graça na coluna do meio, para ver o efeito sem gastar power-up.
        [MenuItem(Menu + "Testar na coluna do meio (jogo rodando)")]
        static void TestInGame()
        {
            var lm = LevelManager.THIS;
            if (!Application.isPlaying || lm == null || lm.field == null || lm.gameStatus != GameState.Playing || lm.DragBlocked)
            {
                Debug.LogWarning("Teste do canhão: entre numa fase e espere o tabuleiro ficar parado.");
                return;
            }
            var items = lm.field.GetItems().Where(i => i != null && i.square != null).ToList();
            if (items.Count == 0) { Debug.LogWarning("Teste do canhão: não achei peças no tabuleiro."); return; }
            var columns = items.Select(i => i.square.col).Distinct().OrderBy(c => c).ToList();
            var middle = columns[columns.Count / 2];
            var target = items.Where(i => i.square.col == middle).OrderBy(i => i.transform.position.y).First();
            lm.DestroyColumnWithCannon(target);
        }

        [MenuItem(Menu + "Testar na coluna do meio (jogo rodando)", true)]
        static bool TestInGameValidate() => Application.isPlaying;

        /// Garante o asset, a arte e o material. Não mexe em nenhum ajuste já feito.
        public static CannonFireEffectData Apply()
        {
            var data = AssetDatabase.LoadAssetAtPath<CannonFireEffectData>(AssetPath);
            if (data == null)
            {
                EnsureFolder(Path.GetDirectoryName(AssetPath).Replace("\\", "/"));
                data = ScriptableObject.CreateInstance<CannonFireEffectData>();
                AssetDatabase.CreateAsset(data, AssetPath);
            }
            if (!HasAny(data.IdleFrames)) data.IdleFrames = LoadFrames("cannon-idle-");
            if (!HasAny(data.FireFrames)) data.FireFrames = LoadFrames("cannon-fire-");
            if (data.additiveMaterial == null) data.additiveMaterial = AssetDatabase.LoadAssetAtPath<Material>(AdditivePath);
            if (data.additiveMaterial == null)
                Debug.LogWarning("Canhão: não achei " + AdditivePath + "; o efeito cria o material aditivo em tempo de execução.");
            if (!HasAny(data.IdleFrames))
                Debug.LogWarning("Canhão: não achei a arte " + ArtFolder + "cannon-idle-N.png; sem ela a coluna é destruída sem animação.");
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
            EditorPrefs.SetInt(VersionKey, Version);
            return data;
        }

        static bool HasAny(Texture2D[] textures) => textures != null && textures.Any(t => t != null);

        static Texture2D[] LoadFrames(string prefix)
        {
            return Enumerable.Range(0, 8)
                .Select(i => AssetDatabase.LoadAssetAtPath<Texture2D>(ArtFolder + prefix + i + ".png"))
                .Where(t => t != null).ToArray();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace("\\", "/");
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
