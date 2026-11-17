// Limite do botão "Assistir vídeo" da modal de compra: cada power-up só pode ser ganho por vídeo uma vez a
// cada 6 horas. O horário de liberação fica em PlayerPrefs (UTC), uma chave por BoostType.
using System;
using SweetSugar.Scripts.GUI.Boost;
using UnityEngine;

namespace RoyalAves.Meta
{
    public static class BoostAdCooldown
    {
        public const int Hours = 6;

        static string Key(BoostType type) => "BoostShop.AdUnlockAt." + type;

        /// Quanto falta para o vídeo deste power-up liberar (zero = já pode assistir).
        public static TimeSpan Remaining(BoostType type)
        {
            var raw = PlayerPrefs.GetString(Key(type), "");
            if (!long.TryParse(raw, out var ticks)) return TimeSpan.Zero;
            var left = new DateTime(ticks, DateTimeKind.Utc) - DateTime.UtcNow;
            // Relógio do aparelho voltado para trás não pode estender o bloqueio além das 6h.
            if (left > TimeSpan.FromHours(Hours)) left = TimeSpan.FromHours(Hours);
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }

        public static bool IsAvailable(BoostType type) => Remaining(type) <= TimeSpan.Zero;

        /// Chame quando o vídeo foi assistido e o prêmio entregue.
        public static void Start(BoostType type)
        {
            PlayerPrefs.SetString(Key(type), DateTime.UtcNow.AddHours(Hours).Ticks.ToString());
            PlayerPrefs.Save();
        }
    }
}
