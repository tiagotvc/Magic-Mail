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
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.System;
using UnityEngine;

namespace SweetSugar.Scripts.Items
{
    /// <summary>
    /// Striped item
    /// </summary>
    public class ItemStriped : Item, IItemInterface
    {
#if UNITY_EDITOR
        public static event global::System.Action<Square> DynamiteRocketCellHit;
#endif
        private Item itemMain;
        public bool ActivateByExplosion;
        public bool StaticOnStart;

        private new Item GetItem => GetComponentInParent<Item>();

        public void Destroy(Item item1, Item item2)
        {
            if (GetItem.IsMulticolorReserved) return;
            StartCoroutine(DestroyCor(item1, item2));
        }

        private IEnumerator DestroyCor(Item item1, Item item2)
        {
            bool wasLocked = GetItem.square.IsItemLocked;
            GetItem.square.DestroyBlock();
            if (wasLocked)
            {
                GetItem.StopDestroy();
                yield break;
            }

            var list = new[] {item1, item2};
            list = list.OrderBy(i => i != GetItem).ToArray();
            item1 = list.First();
            item2 = list.Last();

            itemMain = item1;
            var square = itemMain.square;
            SoundBase.Instance?.PlayLimitSound(SoundBase.Instance.strippedExplosion);
            var sqL = GetSquaresInRow(square, itemMain.currentType);
            // Correio Mágico: cada peça da linha só estoura quando a metade do foguete passa por ela (ver StripedShowPass).
            // A peça de cada quadrado é capturada já aqui, antes do foguete começar a voar - senão, enquanto uma
            // célula já destruída espera sua vez na fila, a gravidade pode puxar a peça vizinha pra preencher o
            // buraco, e quando o callback daquela célula finalmente dispara "sq.Item" já virou null (a peça
            // escapou pro quadrado vazio em vez de explodir).
            var itemsAtCell = sqL.ToDictionary(sq => sq, sq => sq.Item);
            var wait = LevelManager.THIS.StripedShowPass(gameObject, item1.currentType == ItemsTypes.HORIZONTAL_STRIPED, sqL,
                sq =>
                {
                    if(sq.type==SquareTypes.JellyBlock)
                        LevelManager.THIS.levelData.GetTargetObject().CheckSquares(new[]{sq});
                    if (itemsAtCell[sq] != null)
                        itemsAtCell[sq].DestroyItem(true, GetItem, this);
                    sq.DestroyBlock();
                });
            square.DestroyBlock();
            yield return new WaitForSeconds(0.2f + wait);
            DestroyBehaviour();
        }

        private List<Square> GetSquaresInRow(Square square, ItemsTypes type)
        {
            var cells = type == ItemsTypes.HORIZONTAL_STRIPED
                ? LevelManager.THIS.GetRowSquare(square.row) : LevelManager.THIS.GetColumnSquare(square.col);
            return RoyalAves.Gameplay.ImpactBarrierSnapshot.Capture(square.field).Filter(square, cells);
        }

        public Item GetParentItem()
        {
            return transform.GetComponentInParent<Item>();
        }

        public override void Check(Item item1, Item item2)
        {
            CheckStripes(item1, item2);
            if(gameObject.activeSelf)
                StartCoroutine(CheckStripePackage(item1, item2));

            if (item2.currentType == ItemsTypes.MARMALADE || item2.currentType == ItemsTypes.MULTICOLOR)
            {
                item2.Check(item2, item1);
            }
        }

        /// <summary>
        /// Check if package and striped switched
        /// </summary>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        /// <returns></returns>
        private IEnumerator CheckStripePackage(Item item1, Item item2)
        {
            Item[] list = { item1, item2 };
            var striped = list.Where(i =>
                    i.currentType == ItemsTypes.HORIZONTAL_STRIPED || i.currentType == ItemsTypes.VERTICAL_STRIPED)
                ?.FirstOrDefault();
            var package = list.Where(i => i.currentType == ItemsTypes.PACKAGE)?.FirstOrDefault();

            if (striped != null && package != null)
            {
                if(package.destroying || striped.destroying)yield break;
                package.destroying=striped.destroying=true;
                if(striped.GetComponent<GameBlocker>()==null)striped.gameObject.AddComponent<GameBlocker>();
                var manager = LevelManager.THIS;
                var data=manager.levelData;
                // Correio Mágico: junção (brilho atrás, giro no eixo, um em volta do outro) antes da carga.
                yield return RoyalAves.Effects.PowerUpMerge.Play(package,striped);
                using(var charge=new RoyalAves.Effects.DynamiteCombinationCharge(package,striped))
                {
                yield return charge.Play();
                if(manager==null || manager.levelData!=data || package.square==null)yield break;
                var barriers = RoyalAves.Gameplay.ImpactBarrierSnapshot.Capture(package.square.field);
                // Three rows and three columns, even when a crate occupies a launch cell.
                var origins = package.square.field.squaresArray.Where(cell=>cell!=null && !cell.IsNone() &&
                    Mathf.Abs(cell.col-package.square.col)<=1 && Mathf.Abs(cell.row-package.square.row)<=1).ToList();
                var cells = barriers.Filter(package.square, origins.SelectMany(cell =>
                    GetSquaresInRow(cell, ItemsTypes.HORIZONTAL_STRIPED).Union(GetSquaresInRow(cell, ItemsTypes.VERTICAL_STRIPED))).Distinct().ToList());
                var originals = cells.ToDictionary(cell => cell, cell => cell.Item);
                var passed = new HashSet<Square>();
                var rows = new HashSet<int>(); var columns = new HashSet<int>();
                item1.destroying = item2.destroying = true;
                void Hit(Square cell)
                {
                    if (!originals.ContainsKey(cell) || !passed.Add(cell)) return;
                    if(cell.type==SquareTypes.JellyBlock)
                        manager.levelData.GetTargetObject().CheckSquares(new[]{cell});
                    var target = originals[cell];
                    if (target != null && target != item1 && target != item2)
                    {
#if UNITY_EDITOR
                        DynamiteRocketCellHit?.Invoke(cell);
#endif
                        target.destroyNext = true;
                        target.DestroyItem(true, explodedItem: striped);
                    }
                    cell.DestroyBlock();
                }
                float wait = 0;
                foreach (var cell in origins)
                {
                    if (rows.Add(cell.row))
                        wait = Mathf.Max(wait, manager.StripedShowPass(cell.gameObject, true,
                            GetSquaresInRow(cell, ItemsTypes.HORIZONTAL_STRIPED), Hit));
                    if (columns.Add(cell.col))
                        wait = Mathf.Max(wait, manager.StripedShowPass(cell.gameObject, false,
                            GetSquaresInRow(cell, ItemsTypes.VERTICAL_STRIPED), Hit));
                }
                SoundBase.Instance?.PlayLimitSound(SoundBase.Instance.explosion2);
                yield return new WaitForSeconds(wait + .2f);
                manager.levelData.GetTargetObject().CheckItems(list);
                striped.DestroyBehaviour(); package.DestroyBehaviour();
                manager.FindMatches();
                }
            }
        }


        /// <summary>
        /// is both striped items switched
        /// </summary>
        /// <param name="item1"></param>
        /// <param name="item2"></param>
        private void CheckStripes(Item item1, Item item2)
        {
            var list = new[] { item1, item2 };
            var stripes = list.Where(i =>
                i.currentType == ItemsTypes.HORIZONTAL_STRIPED || i.currentType == ItemsTypes.VERTICAL_STRIPED);
            stripes = stripes.OrderBy(i => i == GetItem).ToList();
            if (stripes.Count() < 2) return;
            if (item1.destroying && item2.destroying) return;
            item1.destroying = item2.destroying = true;
            var manager = LevelManager.THIS;
            var self = GetItem;
            var other = item1 == self ? item2 : item1;
            if(GetComponent<GameBlocker>()==null)gameObject.AddComponent<GameBlocker>();
            manager.StartCoroutine(Run());
            IEnumerator Run()
            {
            // Correio Mágico: junção (brilho atrás, giro no eixo, um em volta do outro) antes de os dois feixes saírem.
            yield return RoyalAves.Effects.PowerUpMerge.Play(self, other);
            if (manager == null || self == null || self.square == null) yield break;
            SoundBase.Instance?.PlayLimitSound(SoundBase.Instance.strippedExplosion);
            var horizontal = GetSquaresInRow(self.square, ItemsTypes.HORIZONTAL_STRIPED);
            var vertical = GetSquaresInRow(self.square, ItemsTypes.VERTICAL_STRIPED);
            var originals = horizontal.Union(vertical).ToDictionary(cell => cell, cell => cell.Item);
            var passed = new HashSet<Square>();
            void Hit(Square cell)
            {
                if (!passed.Add(cell)) return;
                if(cell.type==SquareTypes.JellyBlock)
                    manager.levelData.GetTargetObject().CheckSquares(new[]{cell});
                var target = originals[cell];
                if (target != null && target != item1 && target != item2)
                    target.DestroyItem(true, explodedItem: self);
                cell.DestroyBlock();
            }
            float wait = manager.StripedShowPass(self.gameObject, false, vertical, Hit);
            wait = Mathf.Max(wait, manager.StripedShowPass(self.gameObject, true, horizontal, Hit));
            yield return new WaitForSeconds(wait + .2f);
            manager.levelData.GetTargetObject().CheckItems(list);
            item1.DestroyBehaviour(); item2.DestroyBehaviour();
            manager.FindMatches();
            }
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
