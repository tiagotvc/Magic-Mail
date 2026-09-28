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
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.System;
using TMPro;
using UnityEngine;

namespace SweetSugar.Scripts.GUI
{
    /// <summary>
    /// Daily reward popup
    /// </summary>
    public class DailyReward : MonoBehaviour
    {
        public DayReward[] days;
        public TextMeshProUGUI description;
        int currentDay;
        // Correio Mágico: the day labels show "10k" from 10,000, so their real amounts are read once and kept here.
        int[] amounts;

        void ReadAmounts()
        {
            if (amounts != null) return;
            amounts = new int[days.Length];
            for (var day = 0; day < days.Length; day++)
            {
                amounts[day] = int.Parse(days[day].count.text);
                days[day].count.text = RoyalAves.Meta.CoinFormat.Short(amounts[day]);
            }
        }

        void OnEnable()
        {
            if (ServerTime.THIS.dateReceived)
                CheckDaily();
            else 
                ServerTime.OnDateReceived += CheckDaily;
            ReadAmounts();
            var count = amounts[currentDay];
            description.text = "You got " + RoyalAves.Meta.CoinFormat.Short(count) + " coins";
        }

        private void CheckDaily()
        {
            var previousDay = PlayerPrefs.GetInt("LatestDay", -1);
            if (previousDay == 6)
            {
                previousDay = -1;
            }

            for (var day = 0; day < days.Length; day++)
            {
                if (day <= previousDay)
                    days[day].SetPassedDay();
                if (day == previousDay + 1)
                {
                    days[day].SetCurrentDay();
                    currentDay = day;
                }

                if (day > previousDay + 1)
                    days[day].SetDayAhead();
            }
        }

        public void Ok()
        {
            PlayerPrefs.SetInt("LatestDay", currentDay);
            PlayerPrefs.SetString("DateReward", ServerTime.THIS.serverTime.ToString());
            PlayerPrefs.Save();
            ReadAmounts();
            var count = amounts[currentDay];
            InitScript.Instance.AddGems(count);
            description.text = "You got " + RoyalAves.Meta.CoinFormat.Short(count) + " coins";
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {        
            ServerTime.OnDateReceived -= CheckDaily;

        }
    }
}
