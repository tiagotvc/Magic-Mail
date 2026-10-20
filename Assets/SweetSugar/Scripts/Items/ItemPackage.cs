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
using System.Linq;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.System.Utils;
using UnityEngine;

namespace SweetSugar.Scripts.Items
{
    /// <summary>
    /// Item package
    /// </summary>
    public class ItemPackage : Item, IItemInterface
    {
        public bool ActivateByExplosion;

        private Action callbackDestroy;
        private int priority = 0;
        private bool canBeStarted;

        SpriteRenderer appearanceRenderer;
        Vector3 appearanceScale;
        Color appearanceColor;
        float appearanceTime;
        bool appearing;
        const float AppearanceDuration = .24f;
        public bool IsAppearing => appearing;

        public void PlayAppearance()
        {
            EndAppearance();
            appearanceRenderer = GetSpriteRenderer();
            if (appearanceRenderer == null) return;
            appearanceScale = appearanceRenderer.transform.localScale;
            appearanceColor = appearanceRenderer.color;
            appearanceTime = 0;
            appearing = true;
            ApplyAppearance(0);
        }

        void ApplyAppearance(float t)
        {
            float scale = t < .65f
                ? Mathf.Lerp(.16f, 1.12f, 1 - Mathf.Pow(1 - t / .65f, 3))
                : Mathf.Lerp(1.12f, 1, Mathf.SmoothStep(0, 1, (t - .65f) / .35f));
            appearanceRenderer.transform.localScale = appearanceScale * scale;
            var color = appearanceColor;
            color.a *= Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .25f));
            appearanceRenderer.color = color;
        }

        void LateUpdate()
        {
            if (!appearing) return;
            // Reserved combo pieces still finish entering; reservation also sets destroying.
            if (appearanceRenderer == null || (destroying && !IsMulticolorReserved)) { EndAppearance(); return; }
            appearanceTime += Time.deltaTime;
            ApplyAppearance(Mathf.Clamp01(appearanceTime / AppearanceDuration));
            if (appearanceTime >= AppearanceDuration) EndAppearance();
        }

        void EndAppearance()
        {
            if (appearing && appearanceRenderer != null)
            {
                appearanceRenderer.transform.localScale = appearanceScale;
                appearanceRenderer.color = appearanceColor;
            }
            appearing = false;
        }

        public override void InitItem() { EndAppearance(); base.InitItem(); }
        protected override void OnDisable() { EndAppearance(); base.OnDisable(); }

        private new Item GetItem => GetComponentInParent<Item>();

        public void Destroy(Item item1, Item item2)
        {
            if (GetItem.IsMulticolorReserved) return;
            EndAppearance();
            if (GetItem.square.IsItemLocked)
            {
                GetItem.square.DestroyBlock();
                GetItem.StopDestroy();

                return;
            }

            gameObject.AddComponent<GameBlocker>();
            item1.destroying = true;
            if (LevelManager.THIS.DebugSettings.DestroyLog)
                DebugLogKeeper.Log(" pre destroy " + " type " + GetItem.currentType + GetItem.GetObjectId() + " " + item1?.GetObjectId() + " : " + item2?.GetObjectId(), DebugLogKeeper.LogType.Destroying);
            GetParentItem().GetCachedComponent<ItemDestroyAnimation>().DestroyPackage(item1);
        }


        public Item GetParentItem()
        {
            return transform.GetComponentInParent<Item>();
        }

        public override void Check(Item item1, Item item2)
        {
            if ((item2.currentType == ItemsTypes.MULTICOLOR))
            {
                item2.Check(item2, item1);
            }

            if ((item2.currentType == ItemsTypes.HORIZONTAL_STRIPED || item2.currentType == ItemsTypes.VERTICAL_STRIPED))
            {
                item2.Check(item2, item1);
            }

            if (item2.currentType == ItemsTypes.MARMALADE)
            {
                item2.Check(item2, item1);
            }

            if (item1.currentType == ItemsTypes.PACKAGE && item2.currentType == ItemsTypes.PACKAGE)
            {
                item1.GetTopItemInterface().Destroy(item1, item2);
                item2.GetTopItemInterface().Destroy(item2, item1);
            }
        }

        public bool IsExplodable()
        {
            return ActivateByExplosion;
        }

        public void SetOrder(int i)
        {
            var spriteRenderers = GetSpriteRenderers();
            var orderedEnumerable = spriteRenderers.OrderBy(x => x.sortingOrder).ToArray();
            for (int index = 0; index < orderedEnumerable.Length; index++)
            {
                var spr = orderedEnumerable[index];
                spr.sortingOrder = i + index;
            }
        }
    }
}
