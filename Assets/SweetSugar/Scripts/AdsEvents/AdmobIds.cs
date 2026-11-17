// Escolhe o ID de bloco do AdMob conforme o modo: no Editor e em Development Build usa sempre os IDs de
// TESTE do Google (clicar em anúncio real durante o desenvolvimento pode suspender a conta); em build de
// release usa o ID real salvo em Resources/Scriptable/AdManagerScriptable.
using UnityEngine;

namespace SweetSugar.Scripts.AdsEvents
{
    public static class AdmobIds
    {
        // IDs de teste oficiais do Google (https://developers.google.com/admob/unity/test-ads).
        public const string TestRewardedAndroid = "ca-app-pub-3940256099942544/5224354917";
        public const string TestRewardedIOS = "ca-app-pub-3940256099942544/1712485313";
        public const string TestInterstitialAndroid = "ca-app-pub-3940256099942544/1033173712";
        public const string TestInterstitialIOS = "ca-app-pub-3940256099942544/4411468910";

        /// Editor ou Development Build.
        public static bool UseTestAds => Debug.isDebugBuild;

        public static string Pick(string realId, string testId) => UseTestAds ? testId : realId;
    }
}
