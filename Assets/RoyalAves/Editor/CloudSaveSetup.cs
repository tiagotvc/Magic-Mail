// Instala os pacotes do save na nuvem pelo Package Manager (o Unity escolhe a versão compatível).
//   Royal Aves > Save na nuvem > Instalar Cloud Save (Unity)           [obrigatório]
//   Royal Aves > Save na nuvem > Instalar Google Play Games (Android)   [login automático no Android]
//   Royal Aves > Save na nuvem > Instalar Apple GameKit (iOS)           [login automático no iOS]
//   Royal Aves > Save na nuvem > Testar salvar/restaurar (Play)         [só envia/mostra o resumo do save local]
// Depois de instalar, os símbolos ROYAL_CLOUDSAVE / ROYAL_PLAYGAMES / ROYAL_GAMEKIT são ligados sozinhos
// (PostImporting). Veja Docs/Save_na_nuvem.md para o passo a passo no painel do Unity, Google e Apple.
using RoyalAves.Save;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class CloudSaveSetup
    {
        [MenuItem("Royal Aves/Save na nuvem/Instalar Cloud Save (Unity)")]
        static void InstallCloudSave() => Add("com.unity.services.cloudsave");

        [MenuItem("Royal Aves/Save na nuvem/Instalar Google Play Games (Android)")]
        static void InstallPlayGames() => Add("com.google.play.games");

        [MenuItem("Royal Aves/Save na nuvem/Instalar Apple GameKit (iOS)")]
        static void InstallGameKit()
        {
            Add("https://github.com/apple/unityplugins.git?path=/plug-ins/Apple.Core/Apple.Core_Unity");
            Add("https://github.com/apple/unityplugins.git?path=/plug-ins/Apple.GameKit/Apple.GameKit_Unity");
        }

        [MenuItem("Royal Aves/Save na nuvem/Mostrar resumo do save local")]
        static void ShowSummary()
        {
            var s = SaveGame.Capture();
            s.TryGetInt("ReachedLevel", out var reached);
            s.TryGetInt("OpenLevel", out var open);
            s.TryGetInt("Gems", out var gems);
            s.TryGetInt("Lifes", out var lifes);
            Debug.Log($"Save local: fase mais alta {reached}, fase atual {open}, moedas {gems}, vidas {lifes}, " +
                      $"{s.ints.Count} números, {s.floats.Count} decimais, {s.strings.Count} textos, " +
                      $"{SaveGame.ToJson(s).Length / 1024f:0.0} KB. Progresso = {SaveGame.Progress(s)}.");
        }

        static void Add(string id)
        {
            Debug.Log("Royal Aves: instalando " + id + " pelo Package Manager...");
            Client.Add(id);
        }
    }
}
