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
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SweetSugar.Scripts.AdsEvents;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SweetSugar.Scripts.GUI.Boost
{
    public enum BoostType
    {
        ExtraMoves,
        Packages,
        Stripes,
        ExtraTime,
        Bomb,
        MulticolorCandy,
        FreeMove,
        ExplodeArea,
        Marmalade,
        None
    }

    /// <summary>
    /// Boost shop popup
    /// </summary>
    public class BoostShop : MonoBehaviour
    {
        public int[] prices;
        public Image icon;
        public TextMeshProUGUI description;
        public TextMeshProUGUI boostName;
        [SerializeField] private Button buyBoostButton;
        [SerializeField] private Button watchAdButton;
        [SerializeField] private int rewardedBoosterCount = 1;
        private Action callback;
        private bool adRewardPending;

        BoostType boostType;

        public List<BoostProduct> boostProducts = new List<BoostProduct>();


        private void OnEnable()
        {
            foreach (var item in boostProducts.Select(i => i.boostIconObject))
            {
                if (item != null)
                    item.SetActive(false);
            }
        }

        public void SetBoost(BoostProduct boost, Action callbackL)
        {
            if (boost == null)
                return;

            boostType = boost.boostType;
            callback = callbackL;
            gameObject.SetActive(true);

            if (boost.boostIconObject != null)
                boost.boostIconObject.SetActive(true);
            if (description != null)
                description.text = boost.GetDescription();
            if (boostName != null)
                boostName.text = boost.GetName();

            if (buyBoostButton != null)
            {
                var countText = buyBoostButton.transform.Find("Count")?.GetComponent<TextMeshProUGUI>();
                var priceText = buyBoostButton.transform.Find("Price")?.GetComponent<TextMeshProUGUI>();
                if (countText != null)
                    countText.text = "x" + boost.count;
                if (priceText != null)
                    priceText.text = "" + boost.GemPrices;
                buyBoostButton.gameObject.SetActive(true);
            }

            if (watchAdButton != null)
                watchAdButton.gameObject.SetActive(true);
        }

        /// <summary>
        /// Purchase boost button function
        /// </summary>
        [UsedImplicitly]
        public void BuyBoost()
        {
            BuyBoost(buyBoostButton?.gameObject);
        }

        [UsedImplicitly]
        public void BuyBoost(GameObject button)
        {
            var buyButton = buyBoostButton?.gameObject ?? button;
            if (buyButton == null)
                return;

            var countText = buyButton.transform.Find("Count")?.GetComponent<TextMeshProUGUI>();
            var priceText = buyButton.transform.Find("Price")?.GetComponent<TextMeshProUGUI>();
            if (countText == null || priceText == null)
                return;

            if (!int.TryParse(countText.text.Replace("x", ""), out var count) ||
                !int.TryParse(priceText.text, out var price))
                return;

            GetComponent<AnimationEventManager>()?.BuyBoost(boostType, price, count, callback);
        }

        [UsedImplicitly]
        public void WatchAdForBoost()
        {
            if (adRewardPending || boostType == BoostType.None ||
                InitScript.Instance == null || AdsManager.THIS == null ||
                !AdsManager.THIS.GetRewardedUnityAdsReady())
                return;

            adRewardPending = true;
            if (watchAdButton != null)
                watchAdButton.interactable = false;

            InitScript.Instance.PrepareBoosterReward(boostType, rewardedBoosterCount, callback);
            AdsManager.THIS.ShowRewardedAds();
        }

        public void CloseAfterReward()
        {
            adRewardPending = false;
            if (watchAdButton != null)
                watchAdButton.interactable = true;
            gameObject.SetActive(false);
        }
    }

    [Serializable]
    public class BoostProduct
    {
        public BoostType boostType;
        public Sprite icon;
        public string description;
        public int descriptionLocalizationRefrence;
        public string name;
        public int nameLocalizationReference;
        public int count;
        public int GemPrices;
        public GameObject boostIconObject;

        public string GetDescription()
        {
            return LocalizationManager.GetText(descriptionLocalizationRefrence, description);
        }

        public string GetName()
        {
            return LocalizationManager.GetText(nameLocalizationReference, name);
        }
    }
}
