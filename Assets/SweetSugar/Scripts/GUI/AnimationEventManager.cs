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

using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using RoyalAves.Meta;
using SweetSugar.Scripts.AdsEvents;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.GUI.Boost;
using SweetSugar.Scripts.GUI.Purchasing;
using SweetSugar.Scripts.GUI.Utils;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.MapScripts.StaticMap.Editor;
using SweetSugar.Scripts.System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_ADS
using UnityEngine.Advertisements;
#endif

namespace SweetSugar.Scripts.GUI
{
    /// <summary>
    /// Popups animation event manager
    /// </summary>
    public class AnimationEventManager : MonoBehaviour
    {
        [SerializeField] private GameObject canvasGlobal;
        [SerializeField] private GameObject boostInfo;
        [SerializeField] private GameObject canvas;
        [SerializeField] private GameObject menuGameOver;
        
        [SerializeField] private GameObject bannerButtonsVideo;
        [SerializeField] private Button bannerButtonsBuy;
        [SerializeField] private GameObject soundSoundOff;
        [SerializeField] private Image soundSound;
        [SerializeField] private GameObject musicMusicOff;
        [SerializeField] private Image musicMusic;
        [SerializeField] private GameObject imageVideo;
        
        [SerializeField] private BoostIcon boostIcon1;
        [SerializeField] private BoostIcon boostIcon2;
        [SerializeField] private BoostIcon boostIcon3;
        
        [SerializeField] private GameObject[] stars;
        [SerializeField] private TextMeshProUGUI scoreText;
        
        [SerializeField] private GameObject[] targetChecks;
        [SerializeField] private GameObject[] targetUnchecks;
        
        public bool PlayOnEnable = true;
        bool WaitForPickupFriends;

        bool WaitForAksFriends;
        Dictionary<string, string> parameters;

        void Awake()
        {
            // Initialize references if not set in inspector
            if (canvasGlobal == null)
                canvasGlobal = ReferenceRestorer.FindCanvasGlobal();
            if (boostInfo == null && canvasGlobal != null)
                boostInfo = ReferenceRestorer.FindBoostInfo(canvasGlobal);
            if (canvas == null)
                canvas = ReferenceRestorer.FindCanvas();
            if (menuGameOver == null && canvas != null)
                menuGameOver = ReferenceRestorer.FindMenuGameOver(canvas);
        }
        
        // Initialize menu-specific references when the menu becomes active
        private void InitializeReferences()
        {
            if (name == "PreFailed")
            {
                if (bannerButtonsVideo == null)
                    bannerButtonsVideo = ReferenceRestorer.FindBannerButtonsVideo(transform);
                if (bannerButtonsBuy == null)
                    bannerButtonsBuy = ReferenceRestorer.FindBannerButtonsBuy(transform);
            }
            else if (name == "Settings")
            {
                if (soundSoundOff == null)
                    soundSoundOff = ReferenceRestorer.FindSoundSoundOff(transform);
                if (soundSound == null)
                    soundSound = ReferenceRestorer.FindSoundSound(transform);
                if (musicMusicOff == null)
                    musicMusicOff = ReferenceRestorer.FindMusicMusicOff(transform);
                if (musicMusic == null)
                    musicMusic = ReferenceRestorer.FindMusicMusic(transform);
            }
            else if (name == "MenuPlay")
            {
                // Initialize boost icons if not set
                if (boostIcon1 == null)
                    boostIcon1 = ReferenceRestorer.FindBoostIcon(transform, 1);
                if (boostIcon2 == null)
                    boostIcon2 = ReferenceRestorer.FindBoostIcon(transform, 2);
                if (boostIcon3 == null)
                    boostIcon3 = ReferenceRestorer.FindBoostIcon(transform, 3);
            }
            
            // Find image/video reference commonly used in multiple menus
            if (imageVideo == null)
                imageVideo = ReferenceRestorer.FindImageVideo(transform);
                
            if (name == "MenuComplete")
            {
                // Initialize star references
                if (stars == null || stars.Length == 0)
                    stars = ReferenceRestorer.FindStars(transform);
                
                // Initialize score text
                if (scoreText == null)
                    scoreText = ReferenceRestorer.FindScoreText(transform);
            }
            
            if (name == "MenuFailed")
            {
                // Initialize target checks
                if (targetChecks == null || targetChecks.Length == 0)
                    targetChecks = ReferenceRestorer.FindTargetChecks(transform);
                if (targetUnchecks == null || targetUnchecks.Length == 0)
                    targetUnchecks = ReferenceRestorer.FindTargetUnchecks(transform);
            }
        }

        void OnEnable()
        {
            InitializeReferences();
            
            if (name == "PreFailed")
            {
                if (bannerButtonsVideo != null)
                    bannerButtonsVideo.SetActive(false);
                
                if (bannerButtonsBuy != null)
                    bannerButtonsBuy.interactable = true;

                GetComponent<Animation>().Play();
            }
            else if (name == "Settings")
            {
                if (PlayerPrefs.GetInt("Sound") < 1)
                {
                    if (soundSoundOff != null)
                        soundSoundOff.SetActive(true);
                    if (soundSound != null)
                        soundSound.enabled = false;
                }
                else
                {
                    if (soundSoundOff != null)
                        soundSoundOff.SetActive(false);
                    if (soundSound != null)
                        soundSound.enabled = true;
                }

                if (PlayerPrefs.GetInt("Music") < 1)
                {
                    if (musicMusicOff != null)
                        musicMusicOff.SetActive(true);
                    if (musicMusic != null)
                        musicMusic.enabled = false;
                }
                else
                {
                    if (musicMusicOff != null)
                        musicMusicOff.SetActive(false);
                    if (musicMusic != null)
                        musicMusic.enabled = true;
                }

            }
            else if (name == "GemsShop")
            {
                var tr = GetComponent<SweetSugarPacks>().packs;
                for (var i = 0; i < LevelManager.THIS.gemsProducts.Count; i++)
                {
                    var item = tr[i];
                    var countText = ReferenceRestorer.GetTextComponent(item, "Count");
                    var priceText = ReferenceRestorer.GetTextComponent(item, "Buy/Price");
                    
                    if (countText != null)
                        countText.text = RoyalAves.Meta.CoinFormat.Short(LevelManager.THIS.gemsProducts[i].count); // Correio Mágico: 10k from 10,000
                    
                    if (priceText != null)
                        priceText.text = "" + LevelManager.THIS.gemsProducts[i].price;
                }
            }
            else if (name == "MenuComplete")
            {
                for (var i = 0; i < 3; i++)
                {
                    if (stars != null && stars[i] != null)
                        stars[i].SetActive(false);
                }
            }
            else if (name == "MenuFailed" && LobbyController.Instance != null)
                HideScoreRows();

            GameObject videoButton = ReferenceRestorer.FindVideoButton(transform, imageVideo, bannerButtonsVideo);
            
            if (videoButton != null)
            {
#if UNITY_ADS || GOOGLE_MOBILE_ADS || APPODEAL
                AdsManager.THIS.rewardedVideoZone = "rewardedVideo";

                if (!AdsManager.THIS.GetRewardedUnityAdsReady())
                    videoButton.SetActive(false);
                else
                    videoButton.SetActive(true);
#else
                videoButton.SetActive(false);
#endif
            }
        }

        void Update()
        {
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                if (name == "MenuPlay" || name == "Settings" || name == "BoostInfo" || name == "GemsShop" || name == "LiveShop" || name == "BoostShop" || name == "Reward")
                    CloseMenu();
            }
        }

        /// <summary>
        /// show rewarded ads
        /// </summary>
        public void ShowAds()
        {
            if (name == "GemsShop")
                InitScript.Instance.currentReward = RewardsType.GetGems;
            else if (name == "LiveShop")
                InitScript.Instance.currentReward = RewardsType.GetLifes;
            else if (name == "PreFailed")
                InitScript.Instance.currentReward = RewardsType.GetGoOn;
            AdsManager.THIS.ShowRewardedAds();
            CloseMenu();
        }

        /// <summary>
        /// Open rate store
        /// </summary>
        public void GoRate()
        {

#if UNITY_ANDROID
        Application.OpenURL(InitScript.Instance.RateURL);
#elif UNITY_IOS
        Application.OpenURL(InitScript.Instance.RateURLIOS);
#endif
            PlayerPrefs.SetInt("Rated", 1);
            PlayerPrefs.Save();
            CloseMenu();
        }

        void OnDisable()
        {
            if (imageVideo != null)
                imageVideo.SetActive(true);
        }

        /// <summary>
        /// Event on finish animation
        /// </summary>
        public void OnFinished()
        {
            if (name == "MenuComplete")
            {
                StartCoroutine(MenuComplete());
                StartCoroutine(MenuCompleteScoring());
            }
            if (name == "MenuPlay")
            {
                if (boostIcon1 != null)
                    boostIcon1.InitBoost();
                if (boostIcon2 != null)
                    boostIcon2.InitBoost();
                if (boostIcon3 != null)
                    boostIcon3.InitBoost();
            }

            if (name == "MenuFailed")
            {
                if (LobbyController.Instance != null)
                {
                    HideScoreRows();
                }
                else if (LevelManager.Score < LevelManager.THIS.levelData.star1)
                {
                    TargetCheck(false, 2);
                }
                else
                {
                    TargetCheck(true, 2);
                }

            }
            if (name == "PrePlay")
            {
                CloseMenu();
                LevelManager.THIS.gameStatus = GameState.Tutorial;
                if(LevelManager.THIS.levelData.limitType == LIMIT.TIME) SoundBase.Instance.PlayOneShot(SoundBase.Instance.timeOut);

            }
            if (name == "PreFailed")
            {
                if (bannerButtonsVideo != null)
                    bannerButtonsVideo.SetActive(false);
                    
                CloseMenu();
            }

            if (name.Contains("gratzWord"))
                gameObject.SetActive(false);
            if (name == "NoMoreMatches")
                gameObject.SetActive(false);
            if (name == "failed")
                gameObject.transform.parent.gameObject.SetActive(false);

        }

        // Correio Mágico (lobby in the scene): levels are not scored, so the failed popup hides its stars, best score and
        // the "Get one star" goal row. Called on enable and again after the popup animation, which may turn them back on.
        static readonly string[] ScoreRows =
            { "Star1", "Star2", "Star3", "BestScore", "TargetDescription2", "TargetCheck2", "TargetUnCheck2" };

        void HideScoreRows()
        {
            foreach (var row in ScoreRows)
            {
                var child = transform.Find("Image/" + row);
                if (child != null) child.gameObject.SetActive(false);
            }
        }

        void TargetCheck(bool check, int n = 1)
        {
            if (targetChecks != null && targetChecks[n-1] != null && targetUnchecks != null && targetUnchecks[n-1] != null)
            {
                targetChecks[n-1].SetActive(check);
                targetUnchecks[n-1].SetActive(!check);
            }
        }
        /// <summary>
        /// Shows rewarded ad button in Prefailed popup
        /// </summary>
        public void WaitForGiveUp()
        {
            if (name == "PreFailed" && LevelManager.THIS.gameStatus != GameState.Playing)
            {
                GetComponent<Animation>()["bannerFailed"].speed = 0;

                #if UNITY_ADS || GOOGLE_MOBILE_ADS

			if (AdsManager.THIS.enableUnityAds || AdsManager.THIS.enableGoogleMobileAds)
            {
				if (AdsManager.THIS.GetRewardedUnityAdsReady ()) {
					transform.Find ("Banner/Buttons/Video").gameObject.SetActive (true);
				}
			}
                #endif
            }
        }

        /// <summary>
        /// Complete popup animation
        /// </summary>
        IEnumerator MenuComplete()
        {
            for (var i = 1; i <= LevelManager.THIS.stars; i++)
            {
                if (stars != null && stars[i-1] != null)
                    stars[i-1].SetActive(true);
                    
                SoundBase.Instance.PlayOneShot(SoundBase.Instance.star[i - 1]);
                yield return new WaitForSeconds(0.5f);
            }
        }

        /// <summary>
        /// Complete popup animation
        /// </summary>
        IEnumerator MenuCompleteScoring()
        {
            TextMeshProUGUI scores = scoreText;
            
            for (var i = 0; i <= LevelManager.Score; i += 500)
            {
                if (scores != null)
                    scores.text = "" + i;
                yield return new WaitForSeconds(0.00001f);
            }
            
            if (scores != null)
                scores.text = "" + LevelManager.Score;
        }

        /// <summary>
        /// SHows info popup
        /// </summary>
        public void Info()
        {
            MenuReference.THIS.Tutorials.gameObject.SetActive(false);
            MenuReference.THIS.Tutorials.gameObject.SetActive(true);
            OpneMenu(gameObject);
        }



        public void PlaySoundButton()
        {

        }

        public void OpneMenu(GameObject menu)
        {
            if (menu.activeSelf)
                menu.SetActive(false);
            else
                menu.SetActive(true);
        }

        public void CloseMenu()
        {
            if (gameObject.name == "MenuPreGameOver")
            {
                ShowGameOver();
            }
            if (gameObject.name == "MenuComplete")
            {
                PlayerPrefs.SetInt("OpenLevel", LevelManager.THIS.currentLevel + 1);
                CrosssceneData.openNextLevel = true;
                LevelManager.THIS.StartCoroutine(OpenMap());
            }
            if (gameObject.name == "MenuFailed")
            {
                LevelManager.THIS.gameStatus = GameState.Map;
            }

            if (SceneManager.GetActiveScene().name == "game")
            {
                if (LevelManager.THIS.gameStatus == GameState.Pause)
                {
                    LevelManager.THIS.gameStatus = GameState.WaitAfterClose;

                }
            }

            if (gameObject.name == "Settings" && LevelManager.GetGameStatus() != GameState.Map)
            {
                BackToMap();
            }
            else if (gameObject.name == "Settings" && LevelManager.GetGameStatus() == GameState.Map)
                SceneManager.LoadScene("main");

            gameObject.SetActive(false);
        }

        public void SwishSound()
        {
            SoundBase.Instance.PlayOneShot(SoundBase.Instance.swish[1]);

        }

        public void ShowInfo()
        {
            if (boostInfo == null && canvasGlobal != null)
                boostInfo = ReferenceRestorer.FindBoostInfo(canvasGlobal);
            
            if (boostInfo != null)
                boostInfo.SetActive(true);
        }

        public void Play()
        {
            SoundBase.Instance.PlayOneShot(SoundBase.Instance.click);
            if (gameObject.name == "MenuPreGameOver")
            {
                if (InitScript.Gems >= 12)
                {
                    InitScript.Instance.SpendGems(12);
                    LevelManager.THIS.gameStatus = GameState.WaitAfterClose;
                    gameObject.SetActive(false);

                }
                else
                {
                    BuyGems();
                }
            }
            else if (gameObject.name == "MenuFailed")
            {
                LevelManager.THIS.gameStatus = GameState.Map;
            }
            else if (gameObject.name == "MenuPlay")
            {
                GUIUtils.THIS.StartGame();
                CloseMenu();
            }
        }

        public void BackToMap()
        {
            Time.timeScale = 1;
            gameObject.SetActive(false);
            LevelManager.THIS.gameStatus = GameState.Map;
            LevelManager.THIS.StartCoroutine(OpenMap());
        }

        public void Next()
        {
            SoundBase.Instance.PlayOneShot(SoundBase.Instance.click);

            CloseMenu();
        }

        [UsedImplicitly]
        public void Again()
        {
            SoundBase.Instance.PlayOneShot(SoundBase.Instance.click);
            GameObject gm = new GameObject();
            gm.AddComponent<RestartLevel>();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        }

        public void BuyGems()
        {

            SoundBase.Instance.PlayOneShot(SoundBase.Instance.click);
            MenuReference.THIS.GemsShop.gameObject.SetActive(true);
        }

        [UsedImplicitly]
        public void Buy(GameObject pack)
        {
            var i = pack.transform.GetSiblingIndex();
            
            var countText = ReferenceRestorer.GetTextComponent(pack.transform, "Count");
            // Correio Mágico: the pack label shows "10k" from 10,000, so the real amount comes from the product list
            // (same index as the purchase ID below).
            InitScript.waitedPurchaseGems = i < LevelManager.THIS.gemsProducts.Count ? LevelManager.THIS.gemsProducts[i].count : int.Parse(countText != null 
                ? countText.text.Replace("x ", "") 
                : pack.transform.Find("Count").GetComponent<TextMeshProUGUI>().text.Replace("x ", ""));
                
#if UNITY_WEBPLAYER || UNITY_WEBGL
            InitScript.Instance.PurchaseSucceded();
            CloseMenu();
            return;
#endif
#if UNITY_INAPPS
            UnityInAppsIntegration.THIS.BuyProductID(LevelManager.THIS.InAppIDs[i]);
#endif

            CloseMenu();

        }
        
        IEnumerator OpenMap()
        {
            Instantiate(Resources.Load("Loading"), transform.parent);
            yield return new WaitForEndOfFrame();
            SceneManager.LoadScene(Resources.Load<MapSwitcher>("Scriptable/MapSwitcher").GetSceneName());
        }

        public void BuyLifeShop()
        {

            SoundBase.Instance.PlayOneShot(SoundBase.Instance.click);
            if (InitScript.lifes < InitScript.Instance.CapOfLife)
                MenuReference.THIS.LiveShop.gameObject.SetActive(true);

        }

        public void BuyLife(GameObject button)
        {
            SoundBase.Instance.PlayOneShot(SoundBase.Instance.click);
            
            var priceText = ReferenceRestorer.GetTextComponent(button.transform, "Price");
            // Correio Mágico: the price label shows "10k" from 10,000, so the real price comes from the life shop.
            var lifeShop = button.GetComponentInParent<LifeShop>();
            int price = lifeShop != null ? lifeShop.CostIfRefill : int.Parse(priceText != null ? priceText.text : "0");
            
            if (InitScript.Gems >= price)
            {
                InitScript.Instance.SpendGems(price);
                InitScript.Instance.RestoreLifes();
                CloseMenu();
            }
            else
            {
                MenuReference.THIS.GemsShop.gameObject.SetActive(true);
            }

        }

        public void BuyFailed(GameObject button)
        {
            if (InitScript.Gems >= LevelManager.THIS.FailedCost)
            {
                InitScript.Instance.SpendGems(LevelManager.THIS.FailedCost);
                button.GetComponent<Button>().interactable = false;
                GoOnFailed();
                GetComponent<Animation>()["bannerFailed"].speed = 1;
            }
            else
            {
                MenuReference.THIS.GemsShop.gameObject.SetActive(true);
            }
        }

        public void GoOnFailed()
        {
            GetComponent<PreFailed>().Continue();
        }

        [UsedImplicitly]
        public void GiveUp()
        {
            GetComponent<PreFailed>().Close();
        }

        void ShowGameOver()
        {
            SoundBase.Instance.PlayOneShot(SoundBase.Instance.gameOver[1]);

            if (menuGameOver != null)
                menuGameOver.SetActive(true);
                
            gameObject.SetActive(false);
        }

        #region boosts

        public void BuyBoost(BoostType boostType, int price, int count, Action callback)
        {
            SoundBase.Instance.PlayOneShot(SoundBase.Instance.click);
            if (InitScript.Gems >= price)
            {
                InitScript.Instance.SpendGems(price);
                InitScript.Instance.BuyBoost(boostType, price, count);
                callback?.Invoke();
                CloseMenu();
            }
            else
            {
                BuyGems();
            }
        }

        #endregion

        // Update SoundOff method to use cached references
        public void SoundOff(GameObject Off)
        {
            var on = Off.transform.parent;

            if (!Off.activeSelf)
            {
                SoundBase.Instance.audioMixer.SetFloat("SoundVolume", -80);
                
                if (soundSound == null)
                    soundSound = on.GetComponent<Image>();
                    
                if (soundSound != null)
                    soundSound.enabled = false;
                
                Off.SetActive(true);
            }
            else
            {
                SoundBase.Instance.audioMixer.SetFloat("SoundVolume", 1);
                Off.SetActive(false);
                
                if (soundSound != null)
                    soundSound.enabled = true;
            }

            float vol;
            SoundBase.Instance.audioMixer.GetFloat("SoundVolume", out vol);
            PlayerPrefs.SetInt("Sound", (int)vol);
            PlayerPrefs.Save();
        }

        // Update MusicOff method to use cached references
        public void MusicOff(GameObject Off)
        {
            var on = Off.transform.parent;
            if (!Off.activeSelf)
            {
                MusicBase.Instance.audioMixer.SetFloat("MusicVolume", -80);
                
                if (musicMusic == null)
                    musicMusic = on.GetComponent<Image>();
                    
                if (musicMusic != null)
                    musicMusic.enabled = false;

                Off.SetActive(true);
            }
            else
            {
                MusicBase.Instance.audioMixer.SetFloat("MusicVolume", 1);
                Off.SetActive(false);
                
                if (musicMusic != null)
                    musicMusic.enabled = true;
            }
            
            float vol;
            MusicBase.Instance.audioMixer.GetFloat("MusicVolume", out vol);
            PlayerPrefs.SetInt("Music", (int)vol);
            PlayerPrefs.Save();
        }

    }
}
