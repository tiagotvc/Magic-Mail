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
using SweetSugar.Scripts.Items;
using UnityEngine;

namespace SweetSugar.Scripts.System
{
    public class WaitWhileDestroyPipeline : CustomYieldInstruction
    {
        private bool currentDestroyFinished;

        public override bool keepWaiting => !currentDestroyFinished;

        public WaitWhileDestroyPipeline(List<Item> destroyItems, Delays delays)
        {
            DestroyingPipeline.THIS.DestroyItems(destroyItems, delays, () =>
            {
                currentDestroyFinished = true;
            });
        }
    }

    public class WaitWhileFall : CustomYieldInstruction
    {
        private List<Item> items;
        public override bool keepWaiting
        {
            get
            {
                var ii = items.WhereNotNull().Where(i=>i.gameObject.activeSelf).Any(i => i.falling);
                return ii;
            }
        }

        public WaitWhileFall(bool generateNewItems = true)
        {
            GenerateAndFall(generateNewItems);
        }

        private void GenerateAndFall(bool generateNewItems)
        {
            items = LevelManager.THIS.field.GetItems(false, null, false);
        }
    }

    public class WaitWhileCollect : CustomYieldInstruction
    {
        private AnimateItems[] items;
        public override bool keepWaiting
        {
            get
            {
                var ii = items.WhereNotNull().Any();
                return ii;
            }
        }

        public WaitWhileCollect()
        {
            items = LevelManager.THIS.animateItems.Where(i=>i.target).ToArray();
        }
    }

    public class WaitForListNull : CustomYieldInstruction
    {
        private List<object> items;
        public override bool keepWaiting => items.AllNull();

        public WaitForListNull(List<object> items_)
        {
            items = items_;
        }
    }

    public class WaitWhileDestroying : CustomYieldInstruction
    {
        private float startTime;
        private List<Item> items;
        public override bool keepWaiting
        {
            get
            {
                if (startTime + 1 < Time.time)
                    items.Where(i => i && i.destroying && i.gameObject.activeSelf).ForEachY(i =>
                    {
                        i.destroying = false;
                        i.DestroyItem();
                    });
                items = items.Where(i => i && i.gameObject.activeSelf).ToList();
                return items.Any(i => i.destroying);
            }
        }

        public WaitWhileDestroying()
        {
            startTime = Time.time;
            items = LevelManager.THIS.field.GetItems(false, null, false);
        }
    }

    public class WaitForNextMove : CustomYieldInstruction
    {
        bool nextMove;
        public override bool keepWaiting
        {
            get
            {
                if (nextMove)
                {
                    LevelManager.OnTurnEnd -= OnTurnEnd;
                    return false;
                }

                return true;
            }
        }
        void OnTurnEnd()
        {
            nextMove = true;
        }

        public WaitForNextMove()
        {
            LevelManager.OnTurnEnd += OnTurnEnd;
        }
    }

    public class WaitForSubLevelChange : CustomYieldInstruction
    {
        bool nextMove;
        public override bool keepWaiting
        {
            get
            {
                if (nextMove)
                {
                    LevelManager.OnSublevelChanged -= OnSublevelChanged;
                    return false;
                }

                return true;
            }
        }
        void OnSublevelChanged()
        {
            nextMove = true;
        }

        public WaitForSubLevelChange()
        {
            LevelManager.OnSublevelChanged += OnSublevelChanged;
        }
    }

    public class WaitForSecCustom : CustomYieldInstruction
    {
        private bool stopWait;
        public float s;

        public override bool keepWaiting => !stopWait;

        public WaitForSecCustom()
        {
            StartCoroutine(WaitForSecCustomCor(s));
        }

        IEnumerator WaitForSecCustomCor(float sec)
        {
            yield return new WaitForSeconds(sec);

        }
        void StartCoroutine(IEnumerator ienumerator)
        {
            LevelManager.THIS.StartCoroutine(ienumerator);
            stopWait = true;
        }
    }
}