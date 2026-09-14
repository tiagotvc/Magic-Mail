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
using JetBrains.Annotations;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.System;
using TMPro;
using UnityEngine;

namespace SweetSugar.Scripts.GUI.Boost
{
    /// <summary>
    /// Boost icon handler on GUI
    /// </summary>
    public class BoostIcon : MonoBehaviour
    {
        public TextMeshProUGUI boostCount;
        public BoostType type;
        public GameObject plus;
        public GameObject counter;
        public GameObject check;
        private BoostProduct boostProduct;

        private bool checkOn;
        private GameObject Lock;
        private GameObject Indicator;
        private static BoostShop BoostShop;

        private void Awake()
        {
            Lock = transform.Find("Lock")?.gameObject;
            Indicator = transform.Find("Indicator")?.gameObject;
            if (MenuReference.THIS != null)
            {
                BoostShop = MenuReference.THIS.BoostShop.gameObject.GetComponent<BoostShop>();
            }
        }

        private void OnEnable()
        {
            if (name == "Main Camera") return;
            if (LevelManager.THIS == null) return;
            if (LevelManager.THIS.gameStatus == GameState.Map)
                check.SetActive(false);
            FindBoostProduct();
            ShowPlus(BoostCount() <= 0);
            boostCount.text = "" + PlayerPrefs.GetInt("" + type);

        }

        private void FindBoostProduct()
        {
            if (BoostShop == null && MenuReference.THIS != null)
            {
                BoostShop = MenuReference.THIS.BoostShop?.GetComponent<BoostShop>();
            }

            if (BoostShop != null)
            {
                var productIndex = (int) type;
                if (productIndex >= 0 && productIndex < BoostShop.boostProducts.Count)
                    boostProduct = BoostShop.boostProducts[productIndex];
            }
        }

        [UsedImplicitly]
        public void ActivateBoost()
        {
            if (LevelManager.THIS.tutorialTime) return;
            SoundBase.Instance.PlayOneShot( SoundBase.Instance.click );
            if (checkOn || LevelManager.THIS.ActivatedBoost == this)
            {
                UnCheckBoost();
                return;
            }
            if (IsLocked() || checkOn || (LevelManager.THIS.gameStatus != GameState.Playing && LevelManager.THIS.gameStatus != GameState.Map))
                return;
            if (BoostCount() > 0)
            {
                if (type != BoostType.MulticolorCandy && type != BoostType.Packages && type != BoostType.Stripes && type != BoostType.Marmalade && !LevelManager.THIS.DragBlocked)//for in-game boosts
                    LevelManager.THIS.ActivatedBoost = this;
                else
                    Check(true);

            }
            else
            {
                OpenBoostShop(boostProduct, ActivateBoost);
            }

            if (boostCount != null)
                boostCount.text = "" + BoostCount();
            ShowPlus(BoostCount() <= 0);
        }

        private void UnCheckBoost()
        {
            checkOn = false;
            if (LevelManager.THIS.gameStatus == GameState.Map)
                Check(false);
            else
            {
                LevelManager.THIS.activatedBoost = null;
                LevelManager.THIS.UnLockBoosts();//for in-game boosts
            }
        }

        public void InitBoost()
        {
            check.SetActive(false);
            plus.SetActive(true);
            LevelManager.THIS.BoostColorfullBomb = 0;
            LevelManager.THIS.BoostPackage = 0;
            LevelManager.THIS.BoostStriped = 0;
            if (boostCount != null)
                boostCount.text = "" + PlayerPrefs.GetInt("" + type);
            checkOn = false;
        }

        private void Check(bool checkIt)
        {
            switch (type)
            {
                case BoostType.MulticolorCandy:
                    LevelManager.THIS.BoostColorfullBomb = checkIt ? 1 : 0;
                    break;
                case BoostType.Packages:
                    LevelManager.THIS.BoostPackage = checkIt ? 2 : 0;
                    break;
                case BoostType.Stripes:
                    LevelManager.THIS.BoostStriped = checkIt ? 2 : 0;
                    break;
                case BoostType.Marmalade:
                    LevelManager.THIS.BoostMarmalade = checkIt ? 1 : 0;
                    break;
                case BoostType.ExtraMoves:
                    if (checkIt) checkIt = false;
                    break;
                case BoostType.ExtraTime:
                    break;
                case BoostType.Bomb:
                    break;
                case BoostType.FreeMove:
                    break;
                case BoostType.ExplodeArea:
                    break;
                case BoostType.None:
                    break;
            }
            checkOn = checkIt;
            if (check != null)
            {
                check.SetActive(checkIt);
            }

            if (plus != null)
            {
                plus.SetActive(!checkIt);
            }
        }

        public void LockBoost()
        {
            if (Lock != null)
            {
                Lock.SetActive(true);
            }

            if (Indicator != null)
            {
                Indicator.SetActive(false);
            }
        }

        public void UnLockBoost()
        {
            if (Lock != null)
            {
                Lock.SetActive(false);
            }

            if (Indicator != null)
            {
                Indicator.SetActive(true);
            }

            if (boostCount != null)
                boostCount.text = "" + BoostCount();
            ShowPlus(BoostCount() <= 0);

        }

        private bool IsLocked()
        {
            return Lock.activeSelf;
        }

        private int BoostCount()
        {
            return PlayerPrefs.GetInt("" + type);
        }

        private static void OpenBoostShop(BoostProduct boost, Action callback)
        {
            if (boost == null)
                return;

            if (BoostShop == null && MenuReference.THIS != null)
            {
                BoostShop = MenuReference.THIS.BoostShop?.GetComponent<BoostShop>();
            }

            BoostShop?.SetBoost(boost, callback);
        }

        private void ShowPlus(bool show)
        {
            plus?.gameObject.SetActive(show);
            counter?.gameObject.SetActive(!show);

        }


    }
}
