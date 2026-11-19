// How coin amounts are written on every screen (lobby, shops, prices, rewards): up to 9999 the full number, from 10,000
// on the whole thousands with "k" (10k, 11k, 125k), so the amounts fit the game's narrow counters and buttons.
namespace RoyalAves.Meta
{
    public static class CoinFormat
    {
        public static string Short(int coins) => Short(coins, 10000);

        /// Abrevia em "k" a partir de kFrom (ex.: 100000 -> "100k"); abaixo disso mostra o número inteiro.
        public static string Short(int coins, int kFrom) => coins >= kFrom ? coins / 1000 + "k" : coins.ToString();
    }
}
