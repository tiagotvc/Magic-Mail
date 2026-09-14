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
using SweetSugar.Scripts.Items._Interfaces;
using UnityEngine;

namespace SweetSugar.Scripts.Items
{
    /// <summary>
    /// Nested color for marmalade
    /// </summary>
    public class NestedColor : MonoBehaviour, IColorChangable/* , IColorEditor */
    {
        public Sprite[] Sprites;

        private void Awake()
        {
            Sprites = GetComponentInParent<IColorableComponent>().GetSprites(LevelManager.THIS.currentLevel);
        }

        public void OnColorChanged(int color)
        {
            GetComponent<SpriteRenderer>().sprite = Sprites[color];
        }
    }
}
