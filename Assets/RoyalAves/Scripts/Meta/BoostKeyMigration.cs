// As quantidades de reforços ficam no PlayerPrefs com o NOME do BoostType como chave. Ao renomear os tipos
// (Bomb→Hammer, ExtraMoves→Arrow, ExplodeArea→Cannon, FreeMove→Cap) o saldo guardado ficaria na chave antiga;
// copiamos uma vez para a nova, sem apagar a antiga.
using UnityEngine;

namespace RoyalAves.Meta
{
    public static class BoostKeyMigration
    {
        static readonly string[,] Renames =
        {
            { "Bomb", "Hammer" }, { "ExtraMoves", "Arrow" }, { "ExplodeArea", "Cannon" }, { "FreeMove", "Cap" },
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Migrate()
        {
            for (var i = 0; i < Renames.GetLength(0); i++)
            {
                var old = Renames[i, 0]; var fresh = Renames[i, 1];
                if (PlayerPrefs.HasKey(old) && !PlayerPrefs.HasKey(fresh)) PlayerPrefs.SetInt(fresh, PlayerPrefs.GetInt(old));
            }
        }
    }
}
