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
using System.Linq;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.System;
using SweetSugar.Scripts.System.Utils;
using UnityEngine;

namespace SweetSugar.Scripts.Items
{
    /// <summary>
    /// Item marmalade
    /// </summary>
    public class ItemMarmalade : Item, IItemInterface
    {
        // public bool Combinable;
        public bool ActivateByExplosion;
        public bool StaticOnStart;
        public bool noMarmaladeLaunch;
        public ItemMarmalade secondItem;
        public MarmaladeFly[] marmalades;
        private bool destroyStarted;

        public void Destroy(Item item1, Item item2)
        {
            if (GetParentItem().square?.type == SquareTypes.WireBlock)
            {
                GetParentItem().square.DestroyBlock();
                return;
            }

            item1.destroying = true;
            var switchItemType = item2?.currentType ?? ItemsTypes.NONE;
            if(!noMarmaladeLaunch && !destroyStarted)
            {
                destroyStarted = true;
                CreateMarmaladeWithType(switchItemType);
            }
            if (switchItemType == ItemsTypes.MARMALADE)
                item2?.GetTopItemInterface()?.Destroy(item2, null);
            else if (switchItemType != ItemsTypes.NONE && (item2?.Combinable ?? false))
                item2?.DestroyItem();
            else if( switchItemType == ItemsTypes.MULTICOLOR)
                item2?.DestroyBehaviour();
            else if(noMarmaladeLaunch) DestroyBehaviour();
            GetParentItem().square?.DestroyBlock();
        }

        private void CreateMarmaladeWithType(ItemsTypes itemsType)
        {
            CreateMarmalade(itemsType);
 
        }

        private void CreateMarmalade(ItemsTypes itemsType)
        {
            // Only duplicates (both propellers take off) when combined with another paper plane; a simple activation
            // sends just the one piece already shown at rest.
            var launching = itemsType == ItemsTypes.MARMALADE ? marmalades : new[] { marmalades[0] };
            for (var i = 0; i < launching.Length; i++)
            {
                var marmalade = launching[i];
                marmalade.gameObject.SetActive(true);
                marmalade.targets = GetParentItem().itemForEditor.TargetMarmaladePositions;
                if (itemsType != ItemsTypes.MARMALADE && itemsType != ItemsTypes.MULTICOLOR && itemsType != ItemsTypes.INGREDIENT)
                    marmalade.nextItemType = itemsType;
                if (GetParentItem().square?.type == SquareTypes.JellyBlock || LevelManager.THIS.lastSwitchedItem?.square?.type == SquareTypes.JellyBlock)
                    marmalade.setJelly = true;
                // With two, each one diverges to its own side; alone, it picks a random side.
                var toLeft = launching.Length > 1 ? i == 0 : UnityEngine.Random.value >= 0.5f;
                marmalade.SetDirection(toLeft ? Vector2.left : Vector2.right);
                marmalade.StartFly();
            }

            StartCoroutine(WaitForReachTarget());
        }

        IEnumerator WaitForReachTarget()
        {
            yield return new WaitWhile(()=>marmalades.Any(i=>i.gameObject.activeSelf));
            DestroyBehaviour();
        }

        private void OnDisable()
        {
            if(square?.Item == this && LevelManager.THIS.gameStatus != GameState.RegenLevel)
                square.Item = null;
        }

        public override void InitItem()
        {
            destroyStarted = false;
            noMarmaladeLaunch = false;
            // Only one piece shows at rest; CreateMarmalade reveals the second one when it duplicates and takes off.
            for (var i = 0; i < marmalades.Length; i++)
                marmalades[i].gameObject.SetActive(i == 0);
            base.InitItem();
        }

        public override void Check(Item item1, Item item2)
        {
            if (item2.currentType == ItemsTypes.MULTICOLOR)
            {
                item2.Check(item2, item1);
            }
            else if (item2.currentType != ItemsTypes.NONE)
                Destroy(item1, item2);
            
            LevelManager.THIS.FindMatches();
        }

        public Item GetParentItem()
        {
            return this.GetCachedComponent<Item>();
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
