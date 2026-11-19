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
using System.Linq;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.System;
using SweetSugar.Scripts.System.Utils;
using UnityEngine;

namespace SweetSugar.Scripts.Items
{
    /// <summary>
    /// Item marmalade
    /// </summary>
    public class ItemMarmalade : Item, IItemInterface
    {
        // public bool Combinable;
        public bool ActivateByExplosion;
        public bool StaticOnStart;
        public bool noMarmaladeLaunch;
        public ItemMarmalade secondItem;
        public MarmaladeFly[] marmalades;
        private bool destroyStarted;
#if UNITY_EDITOR
        public static int LaunchCount;
#endif

        public void Destroy(Item item1, Item item2)
        {
            if (GetParentItem().IsMulticolorReserved) return;
            if (GetParentItem().square?.IsItemLocked == true)
            {
                GetParentItem().square.DestroyBlock();
                GetParentItem().StopDestroy();
                return;
            }

            if(merging)return;                         // a junção com o parceiro está tocando; o lançamento vem logo depois
            item1.destroying = true;
            var switchItemType = item2?.currentType ?? ItemsTypes.NONE;
            if(switchItemType==ItemsTypes.MARMALADE)
            {
                if(destroyStarted || item2.destroying)return;
                destroyStarted=true;item2.destroying=true;
                if(GetComponent<GameBlocker>()==null)gameObject.AddComponent<GameBlocker>();
                StartCoroutine(CombinedPropellers(item2));return;
            }
            bool carryingDynamite=switchItemType==ItemsTypes.PACKAGE;
            bool carryingPayload=carryingDynamite || switchItemType==ItemsTypes.HORIZONTAL_STRIPED || switchItemType==ItemsTypes.VERTICAL_STRIPED;
            // Correio Mágico: antes de levar o parceiro, a junção (brilho atrás, giro no eixo, um em volta do outro).
            if(carryingPayload && !mergeShown && RoyalAves.Effects.PowerUpVfx.IsAvailable)
            {
                if(item2.destroying || destroyStarted)return;
                merging=true;item2.destroying=true;
                if(GetComponent<GameBlocker>()==null)gameObject.AddComponent<GameBlocker>();
                StartCoroutine(MergeThenCarry(item1,item2));return;
            }
            if(carryingPayload)
            {
                if((item2.destroying && !mergeShown) || destroyStarted)return;
                item2.destroying=true;
                if(GetComponent<GameBlocker>()==null)gameObject.AddComponent<GameBlocker>();
            }
            if(!noMarmaladeLaunch && !destroyStarted)
            {
                destroyStarted = true;
                // Each flying propeller owns its sound, so it stops at that propeller's arrival.
                if(carryingPayload)
                    foreach(var flyer in marmalades)flyer.PrepareCargo(item2.GetSpriteRenderer().sprite);
                CreateMarmaladeWithType(switchItemType);
                if(carryingPayload)
                {
                    // Consume the partner once. Its blast belongs at the landing cell, never at launch.
                    LevelManager.THIS.levelData.GetTargetObject().CheckItems(new[]{item2});
                    item2.NextType=ItemsTypes.NONE;item2.dontDestroyOnThisMove=item2.dontDestroyForThisCombine=false;
                    item2.HideSprites(true);item2.DestroyBehaviour();
                    var origin=GetParentItem().square;
                    var neighbors=LevelManager.THIS.GetSquaresAroundSquare(origin).Where(c=>c!=origin &&
                        (c.col==origin.col || c.row==origin.row) && c!=item2.square).ToArray();
                    foreach(var cell in neighbors)
                    {
                        if(cell.type==SquareTypes.JellyBlock)LevelManager.THIS.levelData.GetTargetObject().CheckSquares(new[]{cell});
                        if(cell.Item!=null)cell.Item.DestroyItem(true,true,this);
                        cell.DestroyBlock();
                    }
                }
            }
            if(carryingPayload) { /* Delivered by the flying cargo. */ }
            else if (switchItemType == ItemsTypes.MARMALADE)
                item2?.GetTopItemInterface()?.Destroy(item2, null);
            else if (switchItemType != ItemsTypes.NONE && (item2?.Combinable ?? false))
                item2?.DestroyItem();
            else if( switchItemType == ItemsTypes.MULTICOLOR)
                item2?.DestroyBehaviour();
            else if(noMarmaladeLaunch) DestroyBehaviour();
            GetParentItem().square?.DestroyBlock();
        }

        bool merging, mergeShown;
        IEnumerator MergeThenCarry(Item item1,Item item2)
        {
            yield return RoyalAves.Effects.PowerUpMerge.Play(GetParentItem(),item2);
            merging=false;mergeShown=true;
            // se o parceiro sumiu no meio, o giro decola sozinho
            Destroy(item1,item2!=null && item2.gameObject.activeInHierarchy ? item2 : null);
        }

        IEnumerator CombinedPropellers(Item partner)
        {
            var manager=LevelManager.THIS;var data=manager.levelData;var origin=GetParentItem().square;
            var incoming=partner.GetSpriteRenderer();var main=GetSpriteRenderer();
            var incomingPosition=incoming.transform.position;var incomingScale=incoming.transform.localScale;
            var incomingLocalPosition=incoming.transform.localPosition;
            var mainPosition=main.transform.position;var mainLocalPosition=main.transform.localPosition;
            var mainScale=main.transform.localScale;
            var mergePoint=(origin.transform.position+partner.square.transform.position)*.5f;
            mergePoint.z=mainPosition.z;
            bool animatorWasEnabled=partner.anim!=null && partner.anim.enabled;
            if(partner.anim!=null)partner.anim.enabled=false;
            // Correio Mágico: junção (brilho atrás, giro no eixo, um em volta do outro) antes de os dois se fundirem.
            yield return RoyalAves.Effects.PowerUpMerge.Play(GetParentItem(),partner);
            try
            {
                for(float time=0;time<.12f;time+=Time.deltaTime)
                {
                    if(manager==null || manager.levelData!=data || partner==null)yield break;
                    float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(time/.12f));
                    incoming.transform.position=Vector3.Lerp(incomingPosition,mergePoint,t);
                    main.transform.position=Vector3.Lerp(mainPosition,mergePoint,t);
                    incoming.transform.localScale=incomingScale*Mathf.Lerp(1,.22f,t);
                    main.transform.localScale=mainScale*(1+.18f*Mathf.Sin(t*Mathf.PI));
                    yield return null;
                }
                partner.HideSprites(true);
                incoming.transform.localPosition=incomingLocalPosition;incoming.transform.localScale=incomingScale;
                main.transform.localPosition=mainLocalPosition;main.transform.localScale=mainScale;
                if(partner.anim!=null)partner.anim.enabled=animatorWasEnabled;
                manager.levelData.GetTargetObject().CheckItems(new[]{partner});
                partner.NextType=ItemsTypes.NONE;partner.dontDestroyOnThisMove=partner.dontDestroyForThisCombine=false;
                partner.DestroyBehaviour();
                if(marmalades.Length<3)
                {
                    var extra=Instantiate(marmalades[0].gameObject,marmalades[0].transform.parent);
                    extra.name="Third combined propeller";extra.SetActive(false);
                    marmalades=marmalades.Concat(new[]{extra.GetComponent<MarmaladeFly>()}).ToArray();
                }
                for(int i=0;i<3;i++)
                {
                    var fly=marmalades[i];fly.gameObject.SetActive(true);
                    fly.transform.position=mergePoint;
                    fly.nextItemType=ItemsTypes.NONE;fly.targets=null;
                    fly.ConfigurePropellerGroup(i);
                    fly.SetDirection(new Vector2(i-1,.5f));fly.StartFly();
                }
#if UNITY_EDITOR
                LaunchCount+=3;CombinedPropellersStarted?.Invoke(origin,3);
#endif
                var neighbors=manager.GetSquaresAroundSquare(origin).Where(c=>c==origin || c.col==origin.col || c.row==origin.row).Distinct().ToArray();
                foreach(var cell in neighbors)
                {
                    if(cell.type==SquareTypes.JellyBlock)manager.levelData.GetTargetObject().CheckSquares(new[]{cell});
                    if(cell.Item!=null && cell.Item!=this)cell.Item.DestroyItem(true,true,this);
                    cell.DestroyBlock();
                }
                yield return new WaitWhile(()=>marmalades.Take(3).Any(fly=>fly!=null && fly.gameObject.activeSelf));
                NextType=ItemsTypes.NONE;dontDestroyOnThisMove=dontDestroyForThisCombine=false;
                DestroyBehaviour();
            }
            finally
            {
                if(main!=null){main.transform.localPosition=mainLocalPosition;main.transform.localScale=mainScale;}
                if(incoming!=null){incoming.transform.localPosition=incomingLocalPosition;incoming.transform.localScale=incomingScale;}
                if(partner!=null && partner.anim!=null)partner.anim.enabled=animatorWasEnabled;
            }
        }
#if UNITY_EDITOR
        public static event Action<Square,int> CombinedPropellersStarted;
#endif

        private void CreateMarmaladeWithType(ItemsTypes itemsType)
        {
            CreateMarmalade(itemsType);
 
        }

        private void CreateMarmalade(ItemsTypes itemsType)
        {
#if UNITY_EDITOR
            LaunchCount++;
#endif
            // Only duplicates (both propellers take off) when combined with another paper plane; a simple activation
            // sends just the one piece already shown at rest.
            var launching = itemsType == ItemsTypes.MARMALADE ? marmalades : new[] { marmalades[0] };
            for (var i = 0; i < launching.Length; i++)
            {
                var marmalade = launching[i];
                marmalade.gameObject.SetActive(true);
                marmalade.targets = GetParentItem().itemForEditor.TargetMarmaladePositions;
                if (itemsType != ItemsTypes.MARMALADE && itemsType != ItemsTypes.MULTICOLOR && itemsType != ItemsTypes.INGREDIENT)
                    marmalade.nextItemType = itemsType;
                if (GetParentItem().square?.type == SquareTypes.JellyBlock || LevelManager.THIS.lastSwitchedItem?.square?.type == SquareTypes.JellyBlock)
                    marmalade.setJelly = true;
                // With two, each one diverges to its own side; alone, it picks a random side.
                var toLeft = launching.Length > 1 ? i == 0 : UnityEngine.Random.value >= 0.5f;
                marmalade.SetDirection(toLeft ? Vector2.left : Vector2.right);
                marmalade.StartFly();
            }

            StartCoroutine(WaitForReachTarget());
        }

        IEnumerator WaitForReachTarget()
        {
            yield return new WaitWhile(()=>marmalades.Any(i=>i.gameObject.activeSelf));
            DestroyBehaviour();
        }

        private void OnDisable()
        {
            if(square?.Item == this && LevelManager.THIS.gameStatus != GameState.RegenLevel)
                square.Item = null;
        }

        public override void InitItem()
        {
            destroyStarted = false;
            merging = mergeShown = false;
            noMarmaladeLaunch = false;
            RoyalAves.Effects.SeekerIdleSpin.Attach(this);      // giro rápido de tempos em tempos enquanto está parado
            // Only one piece shows at rest; CreateMarmalade reveals the second one when it duplicates and takes off.
            for (var i = 0; i < marmalades.Length; i++)
                marmalades[i].gameObject.SetActive(i == 0);
            base.InitItem();
        }

        public override void Check(Item item1, Item item2)
        {
            if (item2.currentType == ItemsTypes.MULTICOLOR)
            {
                item2.Check(item2, item1);
            }
            else if (item2.currentType != ItemsTypes.NONE)
                Destroy(item1, item2);
            
            LevelManager.THIS.FindMatches();
        }

        public Item GetParentItem()
        {
            return this.GetCachedComponent<Item>();
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
                var flight=spr.GetComponent<MarmaladeFly>();
                if(flight!=null && flight.IsFlying)flight.KeepFlightInFront();
                else spr.sortingOrder = i + index;
            }
        }
    }
}
