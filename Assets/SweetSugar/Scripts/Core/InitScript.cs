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

#if GOOGLE_MOBILE_ADS
using SweetSugar.Scripts.AdsEvents.GoogleRewardedAds;
#endif
using System;
using SweetSugar.Scripts.GUI;
using SweetSugar.Scripts.GUI.BonusSpin;
using SweetSugar.Scripts.GUI.Boost;
using SweetSugar.Scripts.GUI.Utils;
using SweetSugar.Scripts.Integrations.Network;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.MapScripts;
using SweetSugar.Scripts.System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SweetSugar.Scripts.Core
{
    /// <summary>
    /// class for main system variables, ads control and in-app purchasing
    /// </summary>
    public class InitScript : MonoBehaviour
    {
        // Reference fields to prevent GameObject.Find calls
        private GameObject menuPlay;
        private GameObject gemsShop;
        private GameObject liveShop;
        private LIFESAddCounter lifeAddCounter;
        private GameObject rate;

        public static InitScript Instance;

        ///life gaining timer
        public static float RestLifeTimer;

        ///date of exit for life timer
        public static string DateOfExit;

        //reward which can be receive after watching rewarded ads
        public RewardsType currentReward;

        ///amount of life
        public static int lifes { get; set; }

        //EDITOR: max amount of life
        public int CapOfLife = 5;

        //EDITOR: time for rest life
        public float TotalTimeForRestLifeHours;

        //EDITOR: time for rest life
        public float TotalTimeForRestLifeMin = 15;

        //EDITOR: time for rest life
        public float TotalTimeForRestLifeSec = 60;

        //EDITOR: coins gifted in start
        public int FirstGems = 20;

        //amount of coins
        public static int Gems;

        //wait for purchasing of coins succeed
        public static int waitedPurchaseGems;

        //EDITOR: how often to show the "Rate us on the store" popup
        public int ShowRateEvery;

        //EDITOR: rate url
        public string RateURL;

        public string RateURLIOS;

        //EDITOR: amount for rewarded ads
        public int rewardedGems = 5;

        //EDITOR: should player lose a life for every passed level
        public bool losingLifeEveryGame;

        //daily reward popup reference
        public GameObject DailyMenu;

        // Pending rewarded-ad booster purchase. The callback is used to continue
        // the original action (for example, activate the booster in the level)
        // only after the ad network confirms a completed view.
        private BoostType pendingBoosterType = BoostType.None;
        private int pendingBoosterCount;
        private Action pendingBoosterCallback;


        void Awake()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            Instance = this;
            RestLifeTimer = PlayerPrefs.GetFloat("RestLifeTimer");
            DateOfExit = PlayerPrefs.GetString("DateOfExit", "");
            DebugLogKeeper.Init();
            Gems = PlayerPrefs.GetInt("Gems");
            lifes = PlayerPrefs.GetInt("Lifes");
            if (PlayerPrefs.GetInt("Lauched") == 0)
            {
                //First lauching
                lifes = CapOfLife;
                PlayerPrefs.SetInt("Lifes", lifes);
                Gems = FirstGems;
                PlayerPrefs.SetInt("Gems", Gems);
                PlayerPrefs.SetInt("Music", 1);
                PlayerPrefs.SetInt("Sound", 1);

                PlayerPrefs.SetInt("Lauched", 1);
                PlayerPrefs.Save();
            }

            // Initialize references using ReferenceRestorer
            menuPlay = ReferenceRestorer.FindMenuPlay();
            gemsShop = ReferenceRestorer.FindGemsShop();
            liveShop = ReferenceRestorer.FindLiveShop();
            lifeAddCounter = ReferenceRestorer.FindLifeAddCounter();
            
            rate = ReferenceRestorer.CreateRateObject(MenuReference.THIS.transform);
            
            var g = ReferenceRestorer.FindRewardObject();
            if (g != null)
            {
                g.SetActive(true);
                g.SetActive(false);
            }
            
            if (CrosssceneData.totalLevels == 0)
                CrosssceneData.totalLevels = LoadingManager.GetLastLevelNum();
#if GOOGLE_MOBILE_ADS
            var obj = FindObjectOfType<RewAdmobManager>();
            if (obj == null)
            {
                GameObject gm = new GameObject("AdmobRewarded");
                gm.AddComponent<RewAdmobManager>();
            }
#endif

            currentReward = RewardsType.NONE;
        }


        public void ShowRate()
        {
            rate.SetActive(true);
        }


        public void ShowReward()
        {
            if (currentReward == RewardsType.GetBooster)
            {
                GrantPendingBoosterReward();
                return;
            }

            var reward = ReferenceRestorer.FindRewardIcon();
            if (currentReward == RewardsType.GetGems)
            {
                ShowGemsReward(rewardedGems);
                if (gemsShop != null)
                    gemsShop.GetComponent<AnimationEventManager>().CloseMenu();
            }
            else if (currentReward == RewardsType.GetLifes)
            {
                if (reward != null)
                {
                    reward.SetIconSprite(1);
                    reward.gameObject.SetActive(true);
                }
                RestoreLifes();
                if (liveShop != null)
                    liveShop.GetComponent<AnimationEventManager>().CloseMenu();
            }
            else if (currentReward == RewardsType.GetGoOn)
            {
                var preFailed = ReferenceRestorer.FindPreFailed();
                preFailed?.GetComponent<AnimationEventManager>()?.GoOnFailed();
            }
            else if(currentReward == RewardsType.FreeAction)
            {
                var bonusSpin = ReferenceRestorer.GetBonusSpin();
                bonusSpin?.StartSpin();
            }

            currentReward = RewardsType.NONE;
        }

        /// <summary>
        /// Stores the booster reward that should be granted when the next
        /// rewarded ad is completed.
        /// </summary>
        public void PrepareBoosterReward(BoostType boostType, int count, Action completedCallback)
        {
            if (boostType == BoostType.None || count <= 0)
                return;

            pendingBoosterType = boostType;
            pendingBoosterCount = count;
            pendingBoosterCallback = completedCallback;
            currentReward = RewardsType.GetBooster;
        }

        private void GrantPendingBoosterReward()
        {
            if (pendingBoosterType == BoostType.None || pendingBoosterCount <= 0)
                return;

            var boostType = pendingBoosterType;
            var count = pendingBoosterCount;
            var callback = pendingBoosterCallback;

            // Clear the pending state before invoking user code so an accidental
            // duplicate ad callback cannot grant the same reward twice.
            pendingBoosterType = BoostType.None;
            pendingBoosterCount = 0;
            pendingBoosterCallback = null;

            BuyBoost(boostType, 0, count);

            var boostShop = MenuReference.THIS?.BoostShop?.GetComponent<BoostShop>();
            boostShop?.CloseAfterReward();
            callback?.Invoke();
        }

        public void ShowGemsReward(int amount)
        {
            var reward = ReferenceRestorer.FindRewardIcon();
            if (reward != null)
            {
                reward.SetIconSprite(0);
                reward.gameObject.SetActive(true);
            }
            AddGems(amount);
        }

        // used by network manager, can be disabled
        public void SetGems(int count)
        {
            Gems = count;
            PlayerPrefs.SetInt("Gems", Gems);
            PlayerPrefs.Save();
        }


        public void AddGems(int count)
        {
            Gems += count;
            PlayerPrefs.SetInt("Gems", Gems);
            PlayerPrefs.Save();
#if PLAYFAB || GAMESPARKS || EPSILON
            NetworkManager.currencyManager.IncBalance(count);
#endif

        }

        public void SpendGems(int count)
        {
            SoundBase.Instance.PlayOneShot(SoundBase.Instance.cash);
            Gems -= count;
            PlayerPrefs.SetInt("Gems", Gems);
            PlayerPrefs.Save();
#if PLAYFAB || GAMESPARKS || EPSILON
            NetworkManager.currencyManager.DecBalance(count);
#endif

        }


        public void RestoreLifes()
        {
            lifes = CapOfLife;
            PlayerPrefs.SetInt("Lifes", lifes);
            PlayerPrefs.Save();
            
            // Use the cached reference with ReferenceRestorer fallback
            if (lifeAddCounter == null)
                lifeAddCounter = ReferenceRestorer.FindLifeAddCounter();
            if (lifeAddCounter != null)
                lifeAddCounter.ResetTimer();
        }

        public void AddLife(int count)
        {
            lifes += count;
            if (lifes > CapOfLife)
                lifes = CapOfLife;
            PlayerPrefs.SetInt("Lifes", lifes);
            PlayerPrefs.Save();
        }

        public int GetLife()
        {
            if (lifes > CapOfLife)
            {
                lifes = CapOfLife;
                PlayerPrefs.SetInt("Lifes", lifes);
                PlayerPrefs.Save();
            }

            return lifes;
        }

        public void PurchaseSucceded()
        {
            SoundBase.Instance.PlayOneShot(SoundBase.Instance.cash);
            AddGems(waitedPurchaseGems);
            waitedPurchaseGems = 0;
        }

        public void SpendLife(int count)
        {
            if (lifes > 0)
            {
                lifes -= count;
                PlayerPrefs.SetInt("Lifes", lifes);
                PlayerPrefs.Save();
            }
        }

        public void BuyBoost(BoostType boostType, int price, int count)
        {
            PlayerPrefs.SetInt("" + boostType, PlayerPrefs.GetInt("" + boostType) + count);
            PlayerPrefs.Save();
#if PLAYFAB || GAMESPARKS
            //NetworkManager.dataManager.SetBoosterData();
#endif
        }

        public void SpendBoost(BoostType boostType)
        {
            PlayerPrefs.SetInt("" + boostType, PlayerPrefs.GetInt("" + boostType) - 1);
            PlayerPrefs.Save();
#if PLAYFAB || GAMESPARKS
            //NetworkManager.dataManager.SetBoosterData();
#endif
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                if (RestLifeTimer > 0)
                {
                    PlayerPrefs.SetFloat("RestLifeTimer", RestLifeTimer);
                }

                PlayerPrefs.SetInt("Lifes", lifes);
                PlayerPrefs.SetString("DateOfExit", ServerTime.THIS.serverTime.ToString());
                PlayerPrefs.Save();
            }
        }

        void OnApplicationQuit()
        {
            if (RestLifeTimer > 0)
            {
                PlayerPrefs.SetFloat("RestLifeTimer", RestLifeTimer);
            }

            PlayerPrefs.SetInt("Lifes", lifes);
            PlayerPrefs.SetString("DateOfExit", ServerTime.THIS.serverTime.ToString());
            PlayerPrefs.Save();
        }

        public void OnLevelClicked(object sender, LevelReachedEventArgs args)
        {
            if (EventSystem.current.IsPointerOverGameObject(-1))
                return;
            
            // Use ReferenceRestorer to check if any menus are active
            bool menusActive = 
                (menuPlay != null && menuPlay.activeSelf) || 
                (gemsShop != null && gemsShop.activeSelf) || 
                (liveShop != null && liveShop.activeSelf);
                
            if (!menusActive)
            {
                SoundBase.Instance.PlayOneShot(SoundBase.Instance.click);
                OpenMenuPlay(args.Number);
                ShowLeadboard(args.Number);
            }
        }

        public static void OpenMenuPlay(int num)
        {
            PlayerPrefs.SetInt("OpenLevel", num);
            PlayerPrefs.Save();
            LevelManager.THIS.MenuPlayEvent();
            LevelManager.THIS.LoadLevel();
            CrosssceneData.openNextLevel = false;
            
            // Use ReferenceRestorer to find MenuPlay
            var menuPlay = ReferenceRestorer.FindMenuPlay();
            if (menuPlay != null)
                menuPlay.SetActive(true);
        }

        static void ShowLeadboard(int levelNumber)
        {
#if EPSILON
            var leadboardList = FindObjectsOfType<LeadboardManager>();
            foreach (var obj in leadboardList)
            {
                obj.levelNumber = levelNumber;
            }
#endif
        }
        
        void OnEnable()
        {
            LevelsMap.LevelSelected += OnLevelClicked;
            LevelsMap.OnLevelReached += OnLevelReached;

        }

        void OnDisable()
        {
            LevelsMap.LevelSelected -= OnLevelClicked;
            LevelsMap.OnLevelReached -= OnLevelReached;

            PlayerPrefs.SetFloat("RestLifeTimer", RestLifeTimer);
            PlayerPrefs.SetInt("Lifes", lifes);
            PlayerPrefs.SetString("DateOfExit", ServerTime.THIS.serverTime.ToString());
            PlayerPrefs.Save();

        }

        void OnLevelReached()
        {
            var num = PlayerPrefs.GetInt("OpenLevel");
            if (CrosssceneData.openNextLevel && CrosssceneData.totalLevels >= num)
            {
                OpenMenuPlay(num);
            }
        }
    }

    /// moves or time is level limit type
    public enum LIMIT
    {
        MOVES,
        TIME
    }

    /// reward type for rewarded ads watching
    public enum RewardsType
    {
        GetLifes,
        GetGems,
        GetGoOn,
        FreeAction,
        NONE,
        GetBooster
    }
}
