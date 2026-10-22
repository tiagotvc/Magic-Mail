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

using System.Collections.Generic;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using UnityEngine;

namespace SweetSugar.Scripts.Items
{
    /// <summary>
    /// Color generator, checks available colors
    /// </summary>
    public class ColorGenerator : IColorGenerator
    {
        public int GenColor(Square square, int maxColors = 6, int exceptColor = -1, bool onlyNONEType = false)
        {
        
            var allowed = LevelManager.THIS.levelData.GetPieceColors();
            var candidates = new List<int>();
            foreach (int color in allowed)
                if (color != exceptColor && (square == null || square.GetMatchColorAround(color) <= 1))
                    candidates.Add(color);
            // If every choice makes a match, keep the palette and prefer a different color.
            if (candidates.Count == 0)
                foreach (int color in allowed)
                    if (color != exceptColor) candidates.Add(color);
            if (candidates.Count == 0) candidates.AddRange(allowed);
            return candidates[Random.Range(0, candidates.Count)];
        }
    }
}
