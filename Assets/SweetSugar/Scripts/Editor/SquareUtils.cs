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
using System.Collections.Generic;
using System.Linq;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Level;
using UnityEngine;

namespace SweetSugar.Scripts.Editor
{
    public static class SquareUtils
    {
        /// <summary>
        /// Methods for the editor
        /// </summary>
        /// <param name="sqType"></param>
        /// <returns></returns>
        public static GameObject[] GetBlockPrefab(SquareTypes sqType, int layer=-1)
        {
            var list = new List<GameObject>();
            var item1 = Resources.Load("Blocks/" + sqType) as GameObject;
            var layeredBlock = item1?.GetComponent<LayeredBlock>();
            if (layeredBlock != null)
            {
                int range = layer == -1 ? layeredBlock.layers.Length : layer+1;
                list.AddRange(layeredBlock.layers.Select(i => i.gameObject).Take(range));
            }
            if (list.Count() == 0 && item1 != null) list.Add(item1);
            return list.ToArray();
        }

        public static int GetLayersCount(SquareTypes sqType)
        {
            var layers = 1;
            var item1 = Resources.Load("Blocks/" + sqType) as GameObject;
            layers = item1?.GetComponent<LayeredBlock>()?.layers?.Length ?? 0;
            if (layers == 0) layers = 1;
            return layers;
        }

        public static Texture2DSize GetSquareTexture(SquareTypes sqType)
        {
            var blockPrefab = GetBlockPrefab(sqType);
            if(blockPrefab.Any())
            {
                var obj = blockPrefab?.First();
                return new Texture2DSize(obj?.GetComponent<SpriteRenderer>()?.sprite?.GetTexture(), obj.GetComponent<Square>().sizeInSquares);
            }

            return new Texture2DSize(null,Vector2Int.zero);
        }

        public static List<Texture2DSize> GetSquareTextures(SquareBlocks sqBlock, int layer)
        {
            var resultList = new List<Texture2DSize>();
            sqBlock.SortMergeBlocks();
            Tuple<GameObject, int>[] listBlocks = new Tuple<GameObject, int>[0];
            if (!sqBlock.blocks.Any())
            {
                resultList.AddRange(GetBlockPrefab(sqBlock.block)
                    .Select(i => new Texture2DSize(i.GetComponent<SpriteRenderer>().sprite.GetTexture(), i.GetComponent<Square>().sizeInSquares, 0)).ToArray());
                if (sqBlock.obstacle != SquareTypes.NONE)
                {
                    resultList.AddRange(GetBlockPrefab(sqBlock.obstacle)
                        .Select(i => new Texture2DSize(i.GetComponent<SpriteRenderer>().sprite.GetTexture(), i.GetComponent<Square>().sizeInSquares, 0)).ToArray());
                }
            }
            else
            {
                List<Tuple<GameObject, int>> list1 = new List<Tuple<GameObject,int>>();
                int typeCounter = 0;
                for (int index = 0; index <= Mathf.Clamp(layer, 0, sqBlock.blocks.Count-1); index++)
                {
                    var sqBlockBlock = sqBlock.blocks[index];
                    if (index > 0 && sqBlockBlock.squareType == sqBlock.blocks[index - 1].squareType) typeCounter++;
                    else typeCounter = 0;
                    var blockPrefab = GetBlockPrefab(sqBlockBlock.squareType);

                    if(typeCounter < blockPrefab.Length && !sqBlockBlock.anotherSquare)
                    {
                        resultList.Add(new Texture2DSize(blockPrefab[typeCounter].GetComponent<SpriteRenderer>().sprite.GetTexture(), blockPrefab[typeCounter].GetComponent<Square>().sizeInSquares, index, sqBlockBlock.rotate));
                    }
                }
                if (list1.Any()) listBlocks = list1.ToArray();
            }
            return resultList;
        }
    }
}