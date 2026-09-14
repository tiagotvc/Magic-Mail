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

using UnityEngine;

namespace SweetSugar.Scripts.System
{
    public class AdditionalSettings : ScriptableObject
    {
        [Header("Multicolor shouldn't spread a jelly if no jelly under")]
        public bool MulticolorSpreadJellyOnlyUnder;
        
        [Header("Striped should stop on undestroyable")]
        public bool StripedStopByUndestroyable;
        
        [Header("Double multicolor item should destroy squares and spread jelly")]
        public bool DoubleMulticolorDestroySquaresAndSpreadJelly;
        
        [Header("Boost can destroy SolidBlocks directly")]
        public bool SelectableSolidBlock;
        
        [Header("Multicolor can be destroyed by boost bomb and marmalade")]
        public bool MulticolorDestroyByBoostAndMarmalade;
    }
}