// Contrato de um servidor de save. O resto do jogo só fala com RoyalCloudSave; trocar de servidor
// (Unity Cloud Save, PlayFab, o seu próprio) é escrever outra classe com estes métodos.
using System.Threading.Tasks;

namespace RoyalAves.Save
{
    public interface ICloudSaveProvider
    {
        /// Nome para o log/tela ("Unity Cloud Save").
        string Name { get; }
        /// Entra sozinho (Google Play Games no Android, Game Center no iOS; senão conta anônima).
        /// False = sem nuvem nesta sessão (sem internet, serviço não configurado...).
        Task<bool> SignInAsync();
        bool IsSignedIn { get; }
        /// Texto curto da conta, para mostrar na tela de configurações. Pode ser vazio.
        string AccountLabel { get; }
        /// O save guardado na nuvem, ou null se ainda não existe.
        Task<string> LoadAsync();
        Task SaveAsync(string json);
    }

    /// Usado quando nenhum servidor está instalado: o jogo segue só com o save local.
    public sealed class NoCloudProvider : ICloudSaveProvider
    {
        public string Name => "Sem nuvem";
        public bool IsSignedIn => false;
        public string AccountLabel => "";
        public Task<bool> SignInAsync() => Task.FromResult(false);
        public Task<string> LoadAsync() => Task.FromResult<string>(null);
        public Task SaveAsync(string json) => Task.CompletedTask;
    }
}
