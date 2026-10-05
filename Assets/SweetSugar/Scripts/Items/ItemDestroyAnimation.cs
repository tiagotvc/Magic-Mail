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
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Effects;
using UnityEngine;

namespace SweetSugar.Scripts.Items
{
    /// <summary>
    /// Package destroy animation helper
    /// </summary>
    public class ItemDestroyAnimation : MonoBehaviour
    {
        private Item item;
        private bool started;

        private void Start()
        {
            item = GetComponent<Item>();
        }

        public void DestroyPackage(Item item1)
        {
            if (started) return;
            started = true;
            var thisItem = GetComponent<Item>();

            var animator = GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger("Explode");

            GameObject go = Instantiate(Resources.Load("Prefabs/Effects/_ExplosionAround") as GameObject);//LevelManager.THIS.GetExplAroundPool();
            if (go != null)
            {
                go.transform.position = transform.position;
                var explosionAround = go.GetComponent<ExplAround>();
                explosionAround.item = thisItem;
                go.SetActive(true);
            }

            var square = item1.square;
            square.Item = item1;

            StartCoroutine(OnPackageAnimationFinished(item1));
        }

        private IEnumerator OnPackageAnimationFinished(Item item1)
        {
            var square = item1.square;
            yield return new WaitForSeconds(.35f);
            DestroyItems(item1, square);
            yield return new WaitForSeconds(0.2f);
            item.HideSprites(true);
            yield return new WaitForSeconds(0.5f);
            item.DestroyBehaviour();
            started = false;
        }

        private void DestroyItems(Item item1, Square square)
        {
            LevelManager.THIS.field.DestroyItemsAround(square, item);
            var sqList = LevelManager.THIS.GetSquaresAroundSquare(square);
            square.DestroyBlock();
            if(square.type == SquareTypes.JellyBlock)
                LevelManager.THIS.levelData.GetTargetObject().CheckSquares(sqList.ToArray());
            item1.destroying = false;
            item.square.Item = null;
        }
    }
}

