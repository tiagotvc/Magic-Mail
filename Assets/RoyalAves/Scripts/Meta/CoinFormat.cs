// How coin amounts are written on every screen (lobby, shops, prices, rewards): up to 9999 the full number, from 10,000
// on the whole thousands with "k" (10k, 11k, 125k), so the amounts fit the game's narrow counters and buttons.
namespace RoyalAves.Meta
{
    public static class CoinFormat
    {
        public static string Short(int coins) => coins > 9999 ? coins / 1000 + "k" : coins.ToString();
    }
}
