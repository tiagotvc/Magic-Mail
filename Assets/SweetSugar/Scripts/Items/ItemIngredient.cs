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

using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.System;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEngine;

namespace SweetSugar.Scripts.Items
{
    /// <summary>
    /// Item ingredient
    /// </summary>
    public class ItemIngredient : Item, IItemInterface
    {
        public bool ActivateByExplosion;
        public bool StaticOnStart;

        public override void Check(Item item1, Item item2)
        {

        }

        public void Destroy(Item item1, Item item2)
        {
            DestroyBehaviour();
        }

        public Item GetParentItem()
        {
            return transform.GetComponentInParent<Item>();
        }

        public bool IsExplodable()
        {
            return ActivateByExplosion;
        }

        public void SetOrder(int i)
        {
            GetComponent<SpriteRenderer>().sortingOrder = i;
        }

        public override void OnStopFall()
        {
            LevelManager.THIS.levelData.GetTargetsByAction(CollectingTypes.ReachBottom).ForEachY(i => i.CheckBottom());
        }

    }
}
