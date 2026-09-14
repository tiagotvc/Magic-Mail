using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Integrations;
using UnityEngine;
#if LEVELPLAY
using Unity.Services.LevelPlay;
#endif

namespace SweetSugar.Scripts.AdsEvents
{
    public class LevelPlayIntegration : MonoBehaviour
    {
        public static LevelPlayIntegration THIS;
        
        private LevelPlayID levelPlaySettings;
        
#if LEVELPLAY
        private LevelPlayInterstitialAd interstitialAd;
        private LevelPlayRewardedAd rewardedAd;
#endif

        private void Awake()
        {
            if (THIS == null)
            {
                THIS = this;
                DontDestroyOnLoad(gameObject);
                levelPlaySettings = Resources.Load<LevelPlayID>("Scriptable/LevelPlayID");
            }
            else if (THIS != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (levelPlaySettings == null || !levelPlaySettings.enable)
                return;

            string appKey = GetAppKey();
            if (string.IsNullOrEmpty(appKey))
                return;

#if LEVELPLAY
            if(levelPlaySettings.testMode)
                LevelPlay.SetMetaData("is_test_suite", "enable");

            LevelPlay.Init(appKey);
            LevelPlay.OnInitSuccess += (_) =>
            {
                Debug.Log("LevelPlay Initialized");
                LevelPlay.ValidateIntegration();
                if(levelPlaySettings.testMode)
                    LevelPlay.LaunchTestSuite();
                interstitialAd = new LevelPlayInterstitialAd(GetInterstitialId());
                interstitialAd.LoadAd();
                rewardedAd = new LevelPlayRewardedAd(GetRewardedId());
                rewardedAd.LoadAd();
            };

            LevelPlay.OnInitFailed += (error) =>
            {
                Debug.LogWarning("LevelPlay Initialization Failed: " + error.ToString());
            };

            interstitialAd.OnAdClosed += OnInterstitialAdClosed;
            

            rewardedAd.OnAdRewarded += OnRewardedAdRewarded;
            rewardedAd.OnAdClosed += OnRewardedAdClosed;
#endif
        }

        private string GetAppKey()
        {
#if UNITY_ANDROID
            return levelPlaySettings?.androidAppKey ?? "";
#elif UNITY_IOS
            return levelPlaySettings?.iOSAppKey ?? "";
#else
            return levelPlaySettings?.androidAppKey ?? "";
#endif
        }

        private string GetInterstitialId()
        {
#if UNITY_ANDROID
            return levelPlaySettings?.androidInterstitialId ?? "DefaultInterstitial";
#elif UNITY_IOS
            return levelPlaySettings?.iOSInterstitialId ?? "DefaultInterstitial";
#else
            return levelPlaySettings?.androidInterstitialId ?? "DefaultInterstitial";
#endif
        }

        private string GetRewardedId()
        {
#if UNITY_ANDROID
            return levelPlaySettings?.androidRewardedId ?? "DefaultRewardedVideo";
#elif UNITY_IOS
            return levelPlaySettings?.iOSRewardedId ?? "DefaultRewardedVideo";
#else
            return levelPlaySettings?.androidRewardedId ?? "DefaultRewardedVideo";
#endif
        }

        public bool IsInterstitialLoaded()
        {
#if LEVELPLAY
            return interstitialAd != null && interstitialAd.IsAdReady();
#else
            return false;
#endif
        }

        public bool IsRewardedLoaded()
        {
#if LEVELPLAY
            return rewardedAd != null && rewardedAd.IsAdReady();
#else
            return false;
#endif
        }

        public void LoadInterstitialAd()
        {
#if LEVELPLAY
            interstitialAd?.LoadAd();
#endif
        }

        public void ShowInterstitial()
        {
#if LEVELPLAY
Debug.Log( "interstitial levelplay is ready " + (interstitialAd != null && interstitialAd.IsAdReady()) );
            if (interstitialAd != null && interstitialAd.IsAdReady())
                interstitialAd.ShowAd();
#endif
        }

        public void ShowRewardedAd()
        {
#if LEVELPLAY
Debug.Log(  "rewarded levelplay is ready " + (rewardedAd != null && rewardedAd.IsAdReady()) );
            if (rewardedAd != null && rewardedAd.IsAdReady())
            {
                rewardedAd.ShowAd();
            }
#endif
        }

        public void LoadRewardedAd()
        {
#if LEVELPLAY
            rewardedAd?.LoadAd();
#endif
        }
        
#if LEVELPLAY
        private void OnInterstitialAdClosed(LevelPlayAdInfo adInfo)
        {
            LoadInterstitialAd();
        }
        
        private void OnRewardedAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward adReward)
        {
            InitScript.Instance.ShowReward();
            AdsManager._OnRewardedShown();
        }
        
        private void OnRewardedAdClosed(LevelPlayAdInfo adInfo)
        {
            LoadRewardedAd();
        }
#endif

        private void OnApplicationPause(bool pauseStatus)
        {
#if LEVELPLAY
#endif
        }
    }
}
