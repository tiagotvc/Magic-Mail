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
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.System.Utils;
using UnityEngine;

namespace SweetSugar.Scripts.System.Pool
{
    /// <summary>
    /// Enhanced PoolBehaviour to cache component references for pooled objects
    /// </summary>
    public class PoolBehaviour : MonoBehaviour
    {
        public new string name;
        
        // Item component cache for quick access
        [NonSerialized] public Item itemComponent;
        
        private void Awake()
        {
            // Cache the Item component using the new utility
            itemComponent = this.GetCachedComponent<Item>();
        }
        
        // Reset cache when object is enabled (in case components change)
        private void OnEnable()
        {
            // Re-cache key components when enabled
            if (itemComponent == null)
            {
                itemComponent = this.GetCachedComponent<Item>();
            }
        }
    }
}