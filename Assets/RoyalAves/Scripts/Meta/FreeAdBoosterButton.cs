// "AD GRÁTIS" button on the level-start screen: cycles through the boosters already configured on the
// in-level Boost Shop (SweetSugar.Scripts.GUI.Boost.BoostShop.boostProducts) and, on tap, plays a rewarded
// ad to grant whichever one was showing at the moment of the tap. Reuses the exact same pending-reward
// mechanism as BoostShop.WatchAdForBoost() (InitScript.PrepareBoosterReward + AdsManager.ShowRewardedAds)
// instead of inventing a second one, so the grant/ad-completion plumbing stays in one place.
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SweetSugar.Scripts.AdsEvents;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.GUI.Boost;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public class FreeAdBoosterButton : MonoBehaviour
    {
        [Tooltip("O BoostShop que já lista os boosters disponíveis (Image/Boosters ou o popup de loja) — usado só para ler boostProducts (ícone + tipo), não é aberto nem fechado por este botão.")]
        [SerializeField] BoostShop boostShop;
        [SerializeField] Image icon;
        [SerializeField] Button button;
        [Tooltip("Segundos que cada booster fica em exibição antes de trocar pro próximo.")]
        [SerializeField] float cycleInterval = 1.2f;
        [SerializeField] int rewardedBoosterCount = 1;

        List<BoostProduct> rotation;
        int current;
        float timer;
        bool adPending;
        Vector2 iconSize;
        bool iconSizeCaptured;

        void OnEnable()
        {
            rotation = boostShop != null
                ? boostShop.boostProducts.Where(p => p.boostType != BoostType.None && p.icon != null).ToList()
                : null;
            current = 0;
            timer = 0;
            adPending = false;
            if (button != null) button.interactable = true;
            // Each booster's icon in boostProducts was drawn at its own native size for the full-size Boost
            // Shop popup — swapping the sprite alone lets it blow past this badge's frame. Lock the Image's
            // size to whatever was set up in the Inspector so every booster fits the same small circle.
            if (icon != null && !iconSizeCaptured)
            {
                iconSize = icon.rectTransform.sizeDelta;
                iconSizeCaptured = true;
            }
            ShowCurrent();
        }

        void Update()
        {
            if (adPending || rotation == null || rotation.Count < 2) return;
            timer += Time.deltaTime;
            if (timer < cycleInterval) return;
            timer = 0;
            current = (current + 1) % rotation.Count;
            ShowCurrent();
        }

        void ShowCurrent()
        {
            if (icon == null || rotation == null || rotation.Count == 0) return;
            icon.sprite = rotation[current].icon;
            icon.preserveAspect = true;
            if (iconSizeCaptured) icon.rectTransform.sizeDelta = iconSize;
        }

        // Hooked to the button's OnClick in the Inspector.
        [UsedImplicitly]
        public void WatchAdForFreeBooster()
        {
            if (adPending || rotation == null || rotation.Count == 0 ||
                InitScript.Instance == null || AdsManager.THIS == null ||
                !AdsManager.THIS.GetRewardedUnityAdsReady())
                return;

            adPending = true;
            if (button != null) button.interactable = false;

            InitScript.Instance.PrepareBoosterReward(rotation[current].boostType, rewardedBoosterCount, OnRewardGranted);
            AdsManager.THIS.ShowRewardedAds();
        }

        void OnRewardGranted()
        {
            adPending = false;
            if (button != null) button.interactable = true;
        }
    }
}
