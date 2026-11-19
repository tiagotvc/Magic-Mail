// Liga o botão "Salvar Progresso" da tela de configurações à nuvem. Ponha no botão (ou num pai dele).
// - Clique: envia o save agora e mostra o resultado no texto (PT/EN).
// - Texto de estado (opcional): "Salvo na nuvem · Google Play Games" / "Sem conexão" / "Nuvem não configurada".
using System.Threading.Tasks;
using RoyalAves.Meta;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Save
{
    [AddComponentMenu("Royal Aves/Cloud Save Button")]
    public class CloudSaveButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [Tooltip("Opcional: texto de estado embaixo/ao lado do botão.")]
        [SerializeField] TMP_Text statusText;

        bool saving;
        string flash;
        float flashUntil;

        void Awake()
        {
            if (button == null) button = GetComponentInChildren<Button>(true);
            if (button != null) button.onClick.AddListener(OnClick);
        }

        async void OnClick()
        {
            if (saving || RoyalCloudSave.Instance == null) return;
            saving = true;
            var pt = LocalizedImage.Current == GameLanguage.Portuguese;
            Flash(pt ? "Salvando..." : "Saving...", 30f);
            var ok = await RoyalCloudSave.Instance.SaveNowAsync();
            Flash(ok ? (pt ? "Progresso salvo!" : "Progress saved!")
                     : (pt ? "Não foi possível salvar agora." : "Couldn't save right now."), 3f);
            saving = false;
        }

        void Flash(string text, float seconds) { flash = text; flashUntil = Time.unscaledTime + seconds; }

        void Update()
        {
            if (statusText == null) return;
            var text = Time.unscaledTime < flashUntil ? flash : Describe();
            if (statusText.text != text) statusText.text = text;
        }

        static string Describe()
        {
            var pt = LocalizedImage.Current == GameLanguage.Portuguese;
            var cloud = RoyalCloudSave.Instance;
            if (cloud == null) return "";
            switch (cloud.Status)
            {
                case CloudSaveStatus.Synced:
                    var account = string.IsNullOrEmpty(cloud.AccountLabel) ? "" : " · " + cloud.AccountLabel;
                    return (pt ? "Salvo na nuvem" : "Saved to the cloud") + account;
                case CloudSaveStatus.Connecting: return pt ? "Conectando..." : "Connecting...";
                case CloudSaveStatus.Offline: return pt ? "Sem conexão" : "Offline";
                case CloudSaveStatus.Error: return pt ? "Erro ao sincronizar" : "Sync error";
                default: return pt ? "Nuvem não configurada" : "Cloud not set up";
            }
        }
    }
}
