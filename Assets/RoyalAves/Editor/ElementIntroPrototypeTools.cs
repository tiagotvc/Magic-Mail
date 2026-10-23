using System.IO;
using RoyalAves.Meta;
using SweetSugar.Scripts.Core;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class ElementIntroPrototypeTools
    {
        const string Request = "Logs/element-intro-preview.request";

        [MenuItem("Royal Aves/Tutoriais/Prévia — Caixas (fases 1 a 3)")]
        public static void Preview()
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText(Request, "preview");
            if (!EditorApplication.isPlaying)
                Debug.Log("Prévia do aviso agendada: entre em Play e abra uma das fases 1 a 3 para visualizar.");
        }

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || !EditorApplication.isPlaying ||
                LevelManager.THIS == null) return;
            var previous = Object.FindFirstObjectByType<ElementIntroPrototype>();
            if (previous != null)
            {
                if (previous.gameObject.name == "Apresentacao das caixas - referencia")
                {
                    File.Delete(Request);
                    return;
                }
                // Replace an already open preview, including the retired modal
                // that may have survived a script reload with the game paused.
                bool oldModal = previous.gameObject.name.StartsWith("Prototipo");
                Object.Destroy(previous.gameObject);
                if (oldModal && LevelManager.THIS.gameStatus == GameState.Pause)
                    LevelManager.THIS.gameStatus = GameState.Playing;
                return;
            }
            if (LevelManager.THIS.gameStatus != GameState.Playing) return;
            int level = LevelManager.THIS.levelData.levelNum;
            if (level < 1 || level > 3) return;
            var crate = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/RoyalAves/Art/Obstacles/ParcelStack/CM_pilha_estado01_00.png");
            if (crate == null)
            {
                File.Delete(Request);
                Debug.LogError("Prévia do aviso: sprite da Caixa de madeira não encontrado.");
                return;
            }
            File.Delete(Request);
            ElementIntroPrototype.Show(crate);
        }
    }
}
