// // ©2015 - Present Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

#if UNITY_ADS

using SweetSugar.Scripts.Core;
using UnityEngine;
using UnityEngine.Advertisements;

namespace SweetSugar.Scripts.AdsEvents
{
    public class UnityAdsController : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener,
        IUnityAdsShowListener
    {
        #region UnityAdsController

        public static UnityAdsController Instance;

        private bool rewardedLoaded;
        private bool interstitialLoaded;
        private bool rewardedLoadInProgress;
        private bool interstitialLoadInProgress;
        private string pendingShowPlacementId;

        private void Awake()
        {
            Instance = this;
        }

        public void InitAds()
        {
#if UNITY_ANDROID
            Advertisement.Initialize(AdsManager.THIS.androidID, false, this);
#elif UNITY_IOS
            Advertisement.Initialize(AdsManager.THIS.iOSID, false, this);
#endif
        }

        public void ShowAds(string loadAdType)
        {
            if (string.IsNullOrEmpty(loadAdType))
                return;

            if (IsRewardedPlacement(loadAdType))
            {
                if (!rewardedLoaded)
                {
                    pendingShowPlacementId = loadAdType;
                    LoadUnityRewardedAd();
                    return;
                }

                rewardedLoaded = false;
                Advertisement.Show(loadAdType, this);
                return;
            }

            if (IsInterstitialPlacement(loadAdType))
            {
                if (!interstitialLoaded)
                {
                    pendingShowPlacementId = loadAdType;
                    LoadUnityInterstitialAd();
                    return;
                }

                interstitialLoaded = false;
                Advertisement.Show(loadAdType, this);
                return;
            }

            Debug.LogWarning("Unity Ads placement is not configured: " + loadAdType);
        }

        public void LoadUnityRewardedAd()
        {
            var videoAdsZone = GetRewardedPlacementId();
            if (string.IsNullOrEmpty(videoAdsZone) || rewardedLoaded || rewardedLoadInProgress)
                return;

            rewardedLoadInProgress = true;
            Advertisement.Load(videoAdsZone, this);
        }

        public void LoadUnityInterstitialAd()
        {
            var interstitialAdsZone = GetInterstitialPlacementId();
            if (string.IsNullOrEmpty(interstitialAdsZone) || interstitialLoaded || interstitialLoadInProgress)
                return;

            interstitialLoadInProgress = true;
            Advertisement.Load(interstitialAdsZone, this);
        }

        public void OnInitializationComplete()
        {
            Debug.Log("OnInitializationComplete!");
            LoadUnityRewardedAd();
            LoadUnityInterstitialAd();
        }

        public bool isLoaded => rewardedLoaded;

        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            rewardedLoaded = false;
            interstitialLoaded = false;
            rewardedLoadInProgress = false;
            interstitialLoadInProgress = false;
            pendingShowPlacementId = null;
            Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
        }

        public void OnUnityAdsAdLoaded(string placementId)
        {
            if (IsRewardedPlacement(placementId))
            {
                rewardedLoaded = true;
                rewardedLoadInProgress = false;
            }
            else if (IsInterstitialPlacement(placementId))
            {
                interstitialLoaded = true;
                interstitialLoadInProgress = false;
            }

            Debug.Log("OnUnityAdsAdLoaded!  placementId = " + placementId);

            if (pendingShowPlacementId == placementId)
            {
                pendingShowPlacementId = null;
                if (IsRewardedPlacement(placementId))
                    rewardedLoaded = false;
                else if (IsInterstitialPlacement(placementId))
                    interstitialLoaded = false;

                Advertisement.Show(placementId, this);
            }
        }

        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
        {
            if (IsRewardedPlacement(placementId))
            {
                rewardedLoaded = false;
                rewardedLoadInProgress = false;
            }
            else if (IsInterstitialPlacement(placementId))
            {
                interstitialLoaded = false;
                interstitialLoadInProgress = false;
            }

            if (pendingShowPlacementId == placementId)
                pendingShowPlacementId = null;

            Debug.Log($"Unity Ads failed to load {placementId}: {error.ToString()} - {message}");
        }

        public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
        {
            SetPlacementLoaded(placementId, false);
            Debug.Log($"Unity Ads show failed for {placementId}: {error.ToString()} - {message}");
            LoadPlacement(placementId);
        }

        public void OnUnityAdsShowStart(string placementId)
        {
            Debug.Log("OnUnityAdsShowStart!  placementId = " + placementId);
        }

        public void OnUnityAdsShowClick(string placementId)
        {
            Debug.Log("OnUnityAdsShowClick!  placementId = " + placementId);
        }

        public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
        {
            SetPlacementLoaded(placementId, false);
            if (IsRewardedPlacement(placementId) &&
                showCompletionState == UnityAdsShowCompletionState.COMPLETED)
            {
                Debug.Log("OnUnityAdsShowComplete!  placementId = " + placementId);
                AdsManager._OnRewardedShown();
                InitScript.Instance.ShowReward();
            }

            LoadPlacement(placementId);
        }

        private string GetRewardedPlacementId()
        {
            if (AdsManager.THIS == null)
                return null;

            return Application.platform == RuntimePlatform.IPhonePlayer
                ? AdsManager.THIS.unityRewardedIOS
                : AdsManager.THIS.unityRewardedAndroid;
        }

        private string GetInterstitialPlacementId()
        {
            if (AdsManager.THIS == null)
                return null;

            return Application.platform == RuntimePlatform.IPhonePlayer
                ? AdsManager.THIS.unityInterstitialIOS
                : AdsManager.THIS.unityInterstitialAndroid;
        }

        private bool IsRewardedPlacement(string placementId)
        {
            return placementId == GetRewardedPlacementId();
        }

        private bool IsInterstitialPlacement(string placementId)
        {
            return placementId == GetInterstitialPlacementId();
        }

        private void SetPlacementLoaded(string placementId, bool loaded)
        {
            if (IsRewardedPlacement(placementId))
                rewardedLoaded = loaded;
            else if (IsInterstitialPlacement(placementId))
                interstitialLoaded = loaded;
        }

        private void LoadPlacement(string placementId)
        {
            if (IsRewardedPlacement(placementId))
                LoadUnityRewardedAd();
            else if (IsInterstitialPlacement(placementId))
                LoadUnityInterstitialAd();
        }

        #endregion
    }
}
#endif
