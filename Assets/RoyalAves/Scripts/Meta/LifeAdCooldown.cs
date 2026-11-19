// Limite do botão "Ver vídeo" da modal de vidas: um vídeo a cada 3 horas. O horário de liberação fica em
// PlayerPrefs (UTC).
using System;
using UnityEngine;

namespace RoyalAves.Meta
{
    public static class LifeAdCooldown
    {
        public const int Hours = 3;
        const string Key = "LifeShop.AdUnlockAt";

        public static TimeSpan Remaining
        {
            get
            {
                if (!long.TryParse(PlayerPrefs.GetString(Key, ""), out var ticks)) return TimeSpan.Zero;
                var left = new DateTime(ticks, DateTimeKind.Utc) - DateTime.UtcNow;
                // Relógio do aparelho voltado para trás não pode estender o bloqueio além do limite.
                if (left > TimeSpan.FromHours(Hours)) left = TimeSpan.FromHours(Hours);
                return left > TimeSpan.Zero ? left : TimeSpan.Zero;
            }
        }

        public static bool IsAvailable => Remaining <= TimeSpan.Zero;

        /// Chame quando o vídeo foi assistido e a vida entregue.
        public static void Start()
        {
            PlayerPrefs.SetString(Key, DateTime.UtcNow.AddHours(Hours).Ticks.ToString());
            PlayerPrefs.Save();
        }
    }
}
