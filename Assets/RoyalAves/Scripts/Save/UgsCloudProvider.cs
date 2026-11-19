// Servidor de save: Unity Cloud Save + Unity Authentication. Só compila com o pacote
// com.unity.services.cloudsave instalado (o símbolo ROYAL_CLOUDSAVE é ligado sozinho pelo PostImporting).
//
// Login automático (sem tela, sem digitar nada):
//   Android -> Google Play Games   (símbolo ROYAL_PLAYGAMES, pacote com.google.play.games)
//   iOS     -> Apple Game Center   (símbolo ROYAL_GAMEKIT, pacote com.apple.unityplugin.gamekit)
//   Se a plataforma não estiver configurada ou falhar -> conta anônima (o save segue na nuvem, mas só se
//   recupera no mesmo aparelho; ao configurar a plataforma ele passa a seguir o jogador).
#if ROYAL_CLOUDSAVE
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;
#if UNITY_ANDROID && ROYAL_PLAYGAMES
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif
#if UNITY_IOS && ROYAL_GAMEKIT
using Apple.GameKit;
#endif

namespace RoyalAves.Save
{
    public sealed class UgsCloudProvider : ICloudSaveProvider
    {
        const string DataKey = "royal_save";

        public string Name => "Unity Cloud Save";
        public bool IsSignedIn => UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn;
        public string AccountLabel { get; private set; } = "";

        public async Task<bool> SignInAsync()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                    await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    var platform = await PlatformSignInAsync();
                    if (!platform) await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    AccountLabel = platform ? PlatformName : "";
                }
                return AuthenticationService.Instance.IsSignedIn;
            }
            catch (Exception e)
            {
                // Sem internet, projeto não ligado ao Unity Cloud (Edit > Project Settings > Services) etc.
                Debug.LogWarning("Royal Aves: save na nuvem indisponível (" + e.GetType().Name + "): " + e.Message);
                return false;
            }
        }

        public async Task<string> LoadAsync()
        {
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { DataKey });
            return data.TryGetValue(DataKey, out var item) ? item.Value.GetAsString() : null;
        }

        public Task SaveAsync(string json)
            => CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { DataKey, json } });

        // ---------------------------------------------------------------------------------------------

#if UNITY_ANDROID && ROYAL_PLAYGAMES
        const string PlatformName = "Google Play Games";

        static Task<bool> PlatformSignInAsync()
        {
            var done = new TaskCompletionSource<bool>();
            try
            {
                PlayGamesPlatform.Activate();
                PlayGamesPlatform.Instance.Authenticate(status =>
                {
                    if (status != SignInStatus.Success) { done.TrySetResult(false); return; }
                    PlayGamesPlatform.Instance.RequestServerSideAccess(true, async code =>
                    {
                        try
                        {
                            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(code);
                            done.TrySetResult(true);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning("Royal Aves: login do Google Play Games no Unity Authentication falhou: " + e.Message);
                            done.TrySetResult(false);
                        }
                    });
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning("Royal Aves: Google Play Games indisponível: " + e.Message);
                done.TrySetResult(false);
            }
            return done.Task;
        }
#elif UNITY_IOS && ROYAL_GAMEKIT
        const string PlatformName = "Game Center";

        static async Task<bool> PlatformSignInAsync()
        {
            try
            {
                if (!GKLocalPlayer.Local.IsAuthenticated) await GKLocalPlayer.Authenticate();
                if (!GKLocalPlayer.Local.IsAuthenticated) return false;
                var items = await GKLocalPlayer.Local.FetchItems();
                await AuthenticationService.Instance.SignInWithAppleGameCenterAsync(
                    Convert.ToBase64String(items.GetSignature()),
                    GKLocalPlayer.Local.TeamPlayerId,
                    items.PublicKeyUrl,
                    Convert.ToBase64String(items.GetSalt()),
                    items.Timestamp);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Royal Aves: login do Game Center falhou: " + e.Message);
                return false;
            }
        }
#else
        const string PlatformName = "";
        static Task<bool> PlatformSignInAsync() => Task.FromResult(false); // editor e plataformas sem login nativo
#endif
    }
}
#endif
