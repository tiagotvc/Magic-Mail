// Cuida do save na nuvem sozinho, sem ninguém chamar:
//  1. ao abrir o jogo entra na conta (automático) e baixa o save da nuvem;
//  2. compara com o do aparelho: o MAIS ADIANTADO (fase mais alta; empate, o mais recente) vence;
//     - nuvem na frente -> aplica no aparelho e recarrega a cena (só no mapa, nunca no meio de uma fase);
//     - aparelho na frente -> envia para a nuvem;
//  3. depois envia sozinho quando algo muda (no máximo a cada 20 s) e ao minimizar/fechar o jogo.
// Sem servidor instalado (ou sem internet) tudo isto é ignorado e o jogo segue com o save local.
using System;
using System.Threading.Tasks;
using SweetSugar.Scripts.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoyalAves.Save
{
    public enum CloudSaveStatus { NotConfigured, Connecting, Synced, Offline, Error }

    public class RoyalCloudSave : MonoBehaviour
    {
        public static RoyalCloudSave Instance { get; private set; }

        public static event Action Changed;

        public CloudSaveStatus Status { get; private set; } = CloudSaveStatus.NotConfigured;
        public DateTime LastSyncUtc { get; private set; }
        public string AccountLabel => provider != null ? provider.AccountLabel : "";

        const float UploadEverySeconds = 20f;

        ICloudSaveProvider provider;
        string lastUploaded;
        float nextCheck;
        bool busy;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("RoyalCloudSave");
            DontDestroyOnLoad(go);
            go.AddComponent<RoyalCloudSave>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
#if ROYAL_CLOUDSAVE
            provider = new UgsCloudProvider();
#else
            provider = new NoCloudProvider();
#endif
        }

        async void Start()
        {
            await ConnectAsync();
        }

        void SetStatus(CloudSaveStatus status)
        {
            Status = status;
            Changed?.Invoke();
        }

        async Task ConnectAsync()
        {
            if (provider is NoCloudProvider) { SetStatus(CloudSaveStatus.NotConfigured); return; }
            SetStatus(CloudSaveStatus.Connecting);
            if (!await provider.SignInAsync()) { SetStatus(CloudSaveStatus.Offline); return; }
            await SyncAsync();
        }

        /// Baixa, compara e decide quem vale. Seguro de chamar de novo (por exemplo, por um botão).
        public async Task SyncAsync()
        {
            if (busy || provider == null || !provider.IsSignedIn) return;
            busy = true;
            try
            {
                var local = SaveGame.Capture();
                var cloud = SaveGame.FromJson(await provider.LoadAsync());
                var order = cloud == null ? 1 : SaveGame.Compare(local, cloud);
                if (order == 0) order = local.savedAtTicks >= cloud.savedAtTicks ? 1 : -1;

                if (order < 0)
                {
                    await RestoreAsync(cloud);
                    lastUploaded = SaveGame.Fingerprint(cloud);
                }
                else
                {
                    await UploadAsync(local);
                }
                LastSyncUtc = DateTime.UtcNow;
                SetStatus(CloudSaveStatus.Synced);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Royal Aves: sincronização do save falhou: " + e.Message);
                SetStatus(CloudSaveStatus.Error);
            }
            finally { busy = false; }
        }

        async Task UploadAsync(SaveSnapshot snapshot)
        {
            await provider.SaveAsync(SaveGame.ToJson(snapshot));
            lastUploaded = SaveGame.Fingerprint(snapshot);
        }

        async Task RestoreAsync(SaveSnapshot cloud)
        {
            // Nunca no meio de uma fase: espera voltar ao mapa.
            while (LevelManager.THIS != null && LevelManager.GetGameStatus() != GameState.Map)
                await Task.Delay(500);

            SaveGame.Apply(cloud);
            SaveGame.RefreshRuntimeState();
            Debug.Log("Royal Aves: progresso restaurado da nuvem (fase " + Math.Max(cloud.TryGetInt("ReachedLevel", out var r) ? r : 0, 0) + ").");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// Envia agora (botão "Salvar progresso"). Devolve se deu certo.
        public async Task<bool> SaveNowAsync()
        {
            if (provider == null || !provider.IsSignedIn) { await ConnectAsync(); }
            if (provider == null || !provider.IsSignedIn) return false;
            try
            {
                await UploadAsync(SaveGame.Capture());
                LastSyncUtc = DateTime.UtcNow;
                SetStatus(CloudSaveStatus.Synced);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Royal Aves: envio do save falhou: " + e.Message);
                SetStatus(CloudSaveStatus.Error);
                return false;
            }
        }

        void Update()
        {
            if (provider == null || !provider.IsSignedIn || busy || Status == CloudSaveStatus.Connecting) return;
            if (Time.realtimeSinceStartup < nextCheck) return;
            nextCheck = Time.realtimeSinceStartup + UploadEverySeconds;
            _ = UploadIfChangedAsync();
        }

        async Task UploadIfChangedAsync()
        {
            if (busy) return;
            var snapshot = SaveGame.Capture();
            if (SaveGame.Fingerprint(snapshot) == lastUploaded) return;
            busy = true;
            try
            {
                await UploadAsync(snapshot);
                LastSyncUtc = DateTime.UtcNow;
                SetStatus(CloudSaveStatus.Synced);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Royal Aves: envio automático do save falhou: " + e.Message);
                SetStatus(CloudSaveStatus.Error);
            }
            finally { busy = false; }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) _ = UploadIfChangedAsync(); // minimizou: melhor momento para enviar
        }

        void OnApplicationQuit()
        {
            _ = UploadIfChangedAsync();
        }
    }
}
