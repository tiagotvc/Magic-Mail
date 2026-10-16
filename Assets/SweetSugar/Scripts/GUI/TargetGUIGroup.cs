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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SweetSugar.Scripts.GUI
{
    /// <summary>
    /// Target icons GUI handler. Appears on the top game panel 
    /// </summary>
    public class TargetGUIGroup : MonoBehaviour
    {
        public HorizontalLayoutGroup hg;
        public List<TargetGUI> list = new List<TargetGUI>();
        public TextMeshProUGUI description;
        [Header("HUD: distribuicao centralizada das metas")]
        [Min(1)] public float goalSlotWidth = 150f;
        [Min(0)] public float goalSpacing = 12f;
        [Range(0, .4f)] public float goalSideInset = .12f;
        readonly Dictionary<TargetGUI, Vector3> authoredGoalScales = new Dictionary<TargetGUI, Vector3>();
        HorizontalLayoutGroup group;

        bool IsGameplayHud()
        {
            for (var parent = transform.parent; parent != null; parent = parent.parent)
                if (parent.name == "TopPanel") return true;
            return false;
        }

        void CenterGameplayGoals()
        {
            if (!IsGameplayHud() || hg == null) return;
            // Shrinking layout cells alone does not shrink the fixed-size icon and number inside them.
            // Position the complete items and scale their contents together to the usable cream area.
            hg.enabled = false;
            var area = hg.transform as RectTransform;
            if (area == null) return;
            float usableWidth = Mathf.Max(0, area.rect.width * (1 - 2 * goalSideInset) - 20);
            var active = list.Where(item => item != null && item.gameObject.activeSelf).ToArray();
            float naturalWidth = Mathf.Max(0, active.Length - 1) * goalSpacing;
            foreach (var item in active)
            {
                if (!authoredGoalScales.ContainsKey(item)) authoredGoalScales[item] = item.transform.localScale;
                naturalWidth += goalSlotWidth * Mathf.Abs(authoredGoalScales[item].x);
            }
            float fit = naturalWidth > 0 ? Mathf.Min(1, usableWidth / naturalWidth) : 1;
            float x = -naturalWidth * fit * .5f;
            foreach (var item in active)
            {
                var rect = (RectTransform)item.transform;
                float width = goalSlotWidth * Mathf.Abs(authoredGoalScales[item].x) * fit;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.pivot = new Vector2(.5f, .5f);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, goalSlotWidth);
                rect.localScale = authoredGoalScales[item] * fit;
                rect.anchoredPosition = new Vector2(x + width * .5f, 0);
                x += width + goalSpacing * fit;
            }
            // The hidden description must not reserve horizontal space.
            if (description != null) description.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (IsGameplayHud()) CenterGameplayGoals();
        }

        void OnEnable()
        {
            DisableImages();
            StartCoroutine(WaitForTarget());
            LevelManager.OnLevelLoaded += OnLevelLoaded;
            if (LevelManager.GetGameStatus() > GameState.PrepareGame)
                OnLevelLoaded();
        }

        private void DisableImages()
        {
            ClearTargets();
            description.gameObject.SetActive(false);
            foreach (var item in list)
            {
                item.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {

            LevelManager.OnLevelLoaded -= OnLevelLoaded;

        }

        private void OnLevelLoaded()
        {
            if (IsGameplayHud())
            {
                CenterGameplayGoals();
                return;
            }
            group = GetComponent<HorizontalLayoutGroup>();
            if (group != null)
            {
                if (LevelData.THIS.IsTargetByNameExist("JellyBlock"))
                { group.spacing = 50; /*description.gameObject.SetActive(true);*/ }
                else
                { group.spacing = 0; /*description.gameObject.SetActive(false);*/ }

            }
        }

        IEnumerator WaitForTarget()
        {
            yield return new WaitUntil(() => LevelManager.THIS.levelLoaded);
            yield return new WaitUntil(() => LevelManager.THIS.levelData.GetTargetSprites().Length > 0);

            ClearTargets();
            foreach (var item in list)
                if (item != null) item.gameObject.SetActive(false);
            SetTargets();
        }

        void SetTargets()
        {
            LevelData levelData = LevelManager.THIS.levelData;
            SetDescription(LevelManager.THIS.levelData.GetFirstTarget(true)?.GetDescription());
            var targets = levelData.GetTargetContainersForUI();
            if (transform.parent.parent.parent.name == "PreFailed")
            {
                targets = levelData.GetTargetCounters().Where(i => !i.IsTotalTargetReached()).ToArray();
            }
            for (var i = 0; i < targets.Length; i++)
            {
                var subTargetContainer = targets[i];
                list[i].SetSprite((Sprite) targets[i].extraObject);
                list[i].gameObject.SetActive(true);
                list[i].BindTargetGUI(subTargetContainer);
            }
            CenterGameplayGoals();
        }

        private void SetDescription(string descr)
        {
            description.text = descr;
            if (IsGameplayHud()) return;
            if (descr != "")
            {
                hg.padding.left = 58;
                hg.padding.right = 63;
            }
        }

        void ClearTargets()
        {
            hg.padding.left = 10;
            hg.padding.right = 10;

            description.gameObject.SetActive(false);
        }
    }
}
