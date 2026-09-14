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

using SweetSugar.Scripts.MapScripts;
using TMPro;
using UnityEngine;

namespace SweetSugar.Scripts.GUI
{
    /// <summary>
    /// Level number handler on the map
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class MapLevelNumber : MonoBehaviour
    {
        private void Start()
        {
            MapLevel mapLevel = GetComponentInParent<MapLevel>();
            TextMeshPro text = GetComponent<TextMeshPro>();
            if (mapLevel != null && text != null)
                text.text = mapLevel.Number.ToString();

            LevelsMap.InvalidateLevelNumbersBatch();
        }
    }
}
