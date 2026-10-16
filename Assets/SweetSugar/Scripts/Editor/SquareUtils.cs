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
        static readonly Dictionary<Sprite, Texture2D> icePreviews = new Dictionary<Sprite, Texture2D>();

        static Texture2D IcePreview(SpriteRenderer renderer)
        {
            var sprite = renderer.sprite;
            if (sprite == null) return null;
            if (icePreviews.TryGetValue(sprite, out var cached) && cached != null) return cached;
            // GetSpriteTexture returns the whole source sheet. Render only this state's rectangle,
            // without requiring Read/Write on the imported game texture.
            var rect = sprite.rect;
            var source = sprite.texture;
            var target = RenderTexture.GetTemporary((int)rect.width, (int)rect.height, 0);
            var previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, target, new Vector2(rect.width / source.width, rect.height / source.height),
                    new Vector2(rect.x / source.width, rect.y / source.height));
                RenderTexture.active = target;
                var preview = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false);
                preview.name = sprite.name + " editor preview";
                preview.hideFlags = HideFlags.HideAndDontSave;
                preview.ReadPixels(new Rect(0, 0, rect.width, rect.height), 0, 0);
                var pixels = preview.GetPixels();
                for (int i = 0; i < pixels.Length; i++) pixels[i] *= renderer.color;
                preview.SetPixels(pixels);
                preview.Apply(false, true);
                icePreviews[sprite] = preview;
                return preview;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }

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

        // Magic Mail elements keep the letter sprite on the prefab (it drives the objective count),
        // so the editor icon comes from the element art instead.
        static Texture2D PaletteTexture(SquareTypes sqType, GameObject prefab)
        {
            if (sqType == SquareTypes.IceBlock) return IcePreview(prefab.GetComponent<SpriteRenderer>());
            string path = sqType == SquareTypes.MailCubby ? "Assets/RoyalAves/Art/Obstacles/MailCubby/CM_armario_base_v1.png"
                : sqType == SquareTypes.FrankingMachine ? "Assets/RoyalAves/Art/Obstacles/FrankingMachine/CM_maquina_corpo_v2.png" : null;
            var art = path != null ? UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path) : null;
            return art != null ? art : prefab.GetComponent<SpriteRenderer>()?.sprite?.GetTexture() as Texture2D;
        }

        public static Texture2DSize GetSquareTexture(SquareTypes sqType)
        {
            var blockPrefab = GetBlockPrefab(sqType);
            if(blockPrefab.Any())
            {
                var obj = blockPrefab?.First();
                return new Texture2DSize(PaletteTexture(sqType, obj), obj.GetComponent<Square>().sizeInSquares);
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
                var basePrefabs = GetBlockPrefab(sqBlock.block).Take(Mathf.Max(1, sqBlock.blockLayer));
                if (sqBlock.block == SquareTypes.IceBlock) basePrefabs = basePrefabs.TakeLast(1);
                resultList.AddRange(basePrefabs
                    .Select(i => new Texture2DSize(PaletteTexture(sqBlock.block, i), i.GetComponent<Square>().sizeInSquares, 0)).ToArray());
                if (sqBlock.obstacle != SquareTypes.NONE)
                {
                    var obstaclePrefabs = GetBlockPrefab(sqBlock.obstacle).Take(Mathf.Max(1, sqBlock.obstacleLayer));
                    if (sqBlock.obstacle == SquareTypes.IceBlock) obstaclePrefabs = obstaclePrefabs.TakeLast(1);
                    resultList.AddRange(obstaclePrefabs
                        .Select(i => new Texture2DSize(PaletteTexture(sqBlock.obstacle, i), i.GetComponent<Square>().sizeInSquares, 0)).ToArray());
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
                        if (sqBlockBlock.squareType == SquareTypes.IceBlock)
                        {
                            // A second ice layer replaces the thin preview with the thick state.
                            resultList.RemoveAll(t => t.Texture2D != null && icePreviews.Values.Contains(t.Texture2D));
                        }
                        resultList.Add(new Texture2DSize(PaletteTexture(sqBlockBlock.squareType, blockPrefab[typeCounter]), blockPrefab[typeCounter].GetComponent<Square>().sizeInSquares, index, sqBlockBlock.rotate));
                    }
                }
                if (list1.Any()) listBlocks = list1.ToArray();
            }
            return resultList;
        }
    }
}
