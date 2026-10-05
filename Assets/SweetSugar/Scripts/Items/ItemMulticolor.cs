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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Effects;
using SweetSugar.Scripts.System;
using SweetSugar.Scripts.System.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SweetSugar.Scripts.Items
{
    /// <summary>
    /// Item multicolor
    /// </summary>
    public class ItemMulticolor : Item, IItemInterface
    {
        // public bool Combinable;
        public bool ActivateByExplosion;
        public bool StaticOnStart;

        public GameObject LightningPrefab;
        private bool jellySpread;

        private bool activated;

        public override void InitItem()
        {
            base.InitItem();
            activated = false;
        }

        public override void Check(Item item1, Item item2)
        {
            if (item1?.square?.type == SquareTypes.JellyBlock || item2?.square?.type == SquareTypes.JellyBlock)
                jellySpread = true;
            if (item2 != null && ((!item2.Combinable && item2.currentType != ItemsTypes.MULTICOLOR)))
                return;
            GetParentItem().destroying = true;
            if (item2.currentType == ItemsTypes.NONE)
            {
                DestroyColor(item2.color);
                item2.DestroyItem();
                activated = true;
            }
            else if (item2.currentType == ItemsTypes.HORIZONTAL_STRIPED || item2.currentType == ItemsTypes.VERTICAL_STRIPED)
            {
                LevelManager.THIS.StartCoroutine(SetTypeByColor(item2));
                activated = true;
            }
            else if (item2.currentType == ItemsTypes.PACKAGE)
            {
                LevelManager.THIS.StartCoroutine(SetTypeByColor(item2));
                activated = true;
            }
            else if (item2.currentType == ItemsTypes.MARMALADE)
            {
                GetParentItem().destroying = false;
                LevelManager.THIS.StartCoroutine(SetTypeByColor(item2));
                activated = true;
            }
            else if (item2.currentType == ItemsTypes.MULTICOLOR)
            {
                item2.SmoothDestroy();
                DestroyDoubleMulticolor(item1.square.col, () =>
                {
                    var list = new[] { item1, item2 };
                    list.First(i => i != GetParentItem()).SmoothDestroy();
                    list.First(i => i == GetParentItem()).SmoothDestroy();
                });
                activated = true;
            }
        }

        public void Destroy(Item item1, Item item2)
        {
            if(activated) return;
            if (item2 == null)
            {
                if (GetParentItem().square.type == SquareTypes.WireBlock)
                {
                    GetParentItem().square.DestroyBlock();
                }

                if (LevelManager.GetGameStatus() == GameState.PreWinAnimations)
                {
                    item2 = LevelManager.THIS.field.squaresArray.First(i => i.item != null && i.item.currentType == ItemsTypes.NONE).item;
                    Check(item1, item2);
                }
                if(explodedItem) DestroyColor(explodedItem.color);
                else DestroyColor(Random.Range(0,LevelManager.THIS.levelData.colorLimit-1));

                return;
            }
            item1?.SmoothDestroy();
        }



        #region ChangeItemTypes

        private IEnumerator SetTypeByColor(Item item2)
        {
            var items = LevelManager.THIS.field.GetItemsByColor(item2.color).Where(i => !i.Equals(GetParentItem()) && i.currentType == ItemsTypes.NONE).ToArray();
            var nextType = item2.currentType;
            bool loopFinished = false;
            GameObject itemMarmaladeTarget = null;
            itemMarmaladeTarget = new GameObject();
            item2.DestroyItem();
            StartCoroutine(IterateItems(items, item =>
            {
                if (nextType == ItemsTypes.HORIZONTAL_STRIPED || nextType == ItemsTypes.VERTICAL_STRIPED)
                    item.GetComponent<Item>().NextType = (ItemsTypes)Random.Range(4, 6);
                else
                    item.GetComponent<Item>().NextType = nextType;
                item.marmaladeTarget = itemMarmaladeTarget;
                item.GetComponent<Item>().ChangeType(null, false);
//            item.Explodable = false;
                CreateLightning(transform.position, item.transform.position);
            }, () => { loopFinished = true;  Destroy(itemMarmaladeTarget);}));
            yield return new WaitUntil(() => loopFinished);
            if(item2.currentType != ItemsTypes.MARMALADE)
                DestroyColor(item2.color);
            else
            {
                LevelManager.THIS.FindMatches();
                DestroyColor(item2.color);
            }
            activated = true;
        }

        #endregion

        // The beam touches the pieces of the colour one by one; each one gets a glow. Only when all of them are selected do
        // they explode together.
        private void DestroyColor(int p)
        {
            SoundBase.Instance.PlayOneShot(SoundBase.Instance.colorBombExpl);

            var items = LevelManager.THIS.field.GetItemsByColor(p).Where(i => !i.Equals(GetParentItem())).ToArray();
            StartCoroutine(SelectThenExplode(items));
        }

        private IEnumerator SelectThenExplode(Item[] items)
        {
            var selected = new List<Item>();
            var highlights = new List<SelectionHighlight>();
            // Flagged as destroying from the start: the normal matcher skips them, so same-colour pieces lined up by the
            // beam can't explode on their own before it reaches them. The flag is cleared right before they explode.
            foreach (var item in items)
                if (item != null) item.destroying = true;

            Coroutine pulse = null;
            foreach (var item in items)
            {
                if (item == null || !item.gameObject.activeSelf) continue;
                CreateLightning(transform.position, item.transform.position);
                highlights.Add(MakeHighlight(item));
                selected.Add(item);
                if (pulse == null) pulse = StartCoroutine(PulseOutlines(highlights));
                yield return new WaitForSeconds(BeamStepDelay);
            }

            // All selected: a short pause so the outline pulses, then everything explodes at once.
            yield return new WaitForSeconds(0.3f);
            if (pulse != null) StopCoroutine(pulse);
            foreach (var highlight in highlights)
                Restore(highlight);
            foreach (var item in selected)
            {
                if (item == null || !item.gameObject.activeSelf) continue;
                item.destroying = false;
                item.DestroyItem(true, true, this, true);
            }

            yield return new WaitForSeconds(0.2f);
            LevelManager.THIS.FindMatches();
            SmoothDestroy();
        }

        const float BeamStepDelay = 0.08f;
        const float PulsePeriod = 0.3f;
        // Neon: a bright inner outline and a wider, softer halo behind it.
        const float OutlineSpread = 1.12f;
        const float HaloSpread = 1.32f;
        const float HaloStrength = 0.5f;
        static readonly Color NeonColor = new Color(0.35f, 0.9f, 1f, 1f);

        // The yellow copies drawn under a selected piece (one per sprite of the piece), and the piece's own orders, so
        // they can be put back when it explodes.
        private class SelectionHighlight
        {
            public readonly List<OutlineCopy> Outlines = new List<OutlineCopy>();
            public readonly List<(SpriteRenderer renderer, int order)> Raised = new List<(SpriteRenderer, int)>();
        }

        private class OutlineCopy
        {
            public SpriteRenderer Renderer;
            public float Spread;
            public float Strength;
        }

        // Each sprite of the piece gets a bigger neon copy under it. Raising the piece one order makes the copy show as an
        // outline that follows the piece's own silhouette.
        private static SelectionHighlight MakeHighlight(Item item)
        {
            var highlight = new SelectionHighlight();
            foreach (var renderer in item.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!renderer.enabled || renderer.sprite == null) continue;
                var order = renderer.sortingOrder;
                highlight.Outlines.Add(MakeCopy(renderer, HaloSpread, HaloStrength, order));
                highlight.Outlines.Add(MakeCopy(renderer, OutlineSpread, 1f, order));
                renderer.sortingOrder = order + 1;
                highlight.Raised.Add((renderer, order));
            }
            return highlight;
        }

        // One shared material: a flat neon silhouette (RoyalAves/NeonSilhouette), so the colour of the piece's art doesn't show.
        static Material neonMaterial;

        private static Material NeonMaterial()
        {
            if (neonMaterial == null)
            {
                var shader = Shader.Find("RoyalAves/NeonSilhouette");
                if (shader == null) Debug.LogWarning("Shader RoyalAves/NeonSilhouette não encontrado; o destaque sai sem neon.");
                neonMaterial = new Material(shader);
                neonMaterial.SetColor("_NeonColor", NeonColor);
            }
            return neonMaterial;
        }

        private static OutlineCopy MakeCopy(SpriteRenderer renderer, float spread, float strength, int order)
        {
            var copy = new GameObject("SelectOutline");
            copy.transform.SetParent(renderer.transform, false);
            var outline = copy.AddComponent<SpriteRenderer>();
            outline.sprite = renderer.sprite;
            outline.flipX = renderer.flipX;
            outline.flipY = renderer.flipY;
            outline.sharedMaterial = NeonMaterial();
            outline.color = Color.white;
            outline.sortingOrder = order;
            return new OutlineCopy { Renderer = outline, Spread = spread, Strength = strength };
        }

        // Bright at the start of each pulse, fading but never gone, then bright again.
        private IEnumerator PulseOutlines(List<SelectionHighlight> highlights)
        {
            while (true)
            {
                var phase = (Time.time % PulsePeriod) / PulsePeriod;
                var intensity = Mathf.Lerp(1f, 0.7f, phase);
                foreach (var highlight in highlights)
                    foreach (var outline in highlight.Outlines)
                    {
                        if (outline.Renderer == null) continue;
                        var color = Color.white;
                        color.a = intensity * outline.Strength;
                        outline.Renderer.color = color;
                        outline.Renderer.transform.localScale = Vector3.one * (outline.Spread + 0.06f * intensity);
                    }
                yield return null;
            }
        }

        private static void Restore(SelectionHighlight highlight)
        {
            foreach (var outline in highlight.Outlines)
                if (outline.Renderer != null) Destroy(outline.Renderer.gameObject);
            foreach (var (renderer, order) in highlight.Raised)
                if (renderer != null) renderer.sortingOrder = order;
        }

        private IEnumerator IterateItems(Item[] items, Action<Item> iterateItem, Action onFinished = null)
        {
            var i = 0;
            foreach (var item in items)
            {
                if (item != null && item.gameObject.activeSelf)
                {
                    i++;
                    if (jellySpread)
                        item.square?.SetType(SquareTypes.JellyBlock, 1, SquareTypes.NONE, 1);
                    iterateItem(item); 
                    if (i % (Mathf.Clamp(items.Length,items.Length,2)) == 0)
                        yield return new WaitForSeconds(0.2f);
                }
            }
            if (onFinished != null)
                yield return new WaitForSeconds(0.2f);
            onFinished();

        }

        private void CreateLightning(Vector3 pos1, Vector3 pos2)
        {
            var go = Instantiate(LightningPrefab, Vector3.zero, Quaternion.identity);
            var lightning = go.GetComponent<Lightning>();
            lightning.SetLight(pos1, pos2);
        }

        #region DoubleMulitcolor
        public void DestroyDoubleMulticolor(int col, Action callback)
        {
            LevelManager.THIS.field.GetItems();
            StartCoroutine(DestroyDoubleBombCor(col, () => { callback(); }));
        }

        private IEnumerator DestroyDoubleBombCor(int col, Action callback)
        {
            for (var i = 0; i < LevelManager.THIS.field.fieldData.maxCols; i++)
            {
            
                var list = LevelManager.THIS.GetColumn(i).Where(a => !a.destroying).ToList();
                foreach (var a in list)
                {
                    a.globalExplEffect = true;
                    if (a.currentType == ItemsTypes.MARMALADE) a.GetCachedComponent<ItemMarmalade>().noMarmaladeLaunch = true;
                    CreateLightning(transform.position, a.transform.position);
                    if(jellySpread)
                        a.square?.SetType(SquareTypes.JellyBlock, 1, SquareTypes.NONE, 1);
                    a.DestroyItem();
                }

                if (LevelManager.THIS.AdditionalSettings.DoubleMulticolorDestroySquaresAndSpreadJelly)
                {
                    var sq = LevelManager.THIS.GetColumnSquare(i).ToList();
                    foreach (var a in sq)
                    {
                        CreateLightning(transform.position, a.transform.position);
                        if(a.type == SquareTypes.SolidBlock)
                            a.DestroyBlock();
                        if(jellySpread)
                            a.SetType(SquareTypes.JellyBlock, 1, SquareTypes.NONE, 1);
                    }
                }

                LevelManager.THIS.levelData.GetTargetObject().CheckItems(list.ToArray());
                if(LevelManager.THIS.AdditionalSettings.MulticolorSpreadJellyOnlyUnder)
                    yield return new WaitWhileDestroyPipeline(list, new Delays());
                yield return new WaitForSeconds(0.1f);
            }
            callback();
        }

        #endregion

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
            var spriteRenderers = GetSpriteRenderers();
            var orderedEnumerable = spriteRenderers.OrderBy(x => x.sortingOrder).ToArray();
            for (int index = 0; index < orderedEnumerable.Length; index++)
            {
                var spr = orderedEnumerable[index];
                spr.sortingOrder = i + index;
            }
        }


    }
}
