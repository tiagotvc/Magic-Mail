// // Â©2015 - Present Candy Smith
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
using System.Linq;
using RoyalAves.Effects;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Effects;
using UnityEngine;

namespace SweetSugar.Scripts.Items
{
    /// <summary>
    /// Package destroy animation helper
    /// </summary>
    public class ItemDestroyAnimation : MonoBehaviour
    {
        // Standalone reach stays local; a merged pair hits every playable cell.
        public const int BlastRadius = 2;
        public const float ImpactPause = .28f;
        bool started;
        DynamiteFuseEffect fuse;
        SpriteRenderer mainVisual, incomingVisual;
        Vector3 mainScale, incomingScale, incomingPosition;
        int mainOrder, incomingOrder, mainLayer, incomingLayer;
        Item primary, absorbed;
        static AudioClip explosionSound;
#if UNITY_EDITOR
        public static event global::System.Action<Square,int,bool> BlastApplied;
#endif
        public void DestroyPackage(Item source) => Begin(source,null);
        public void DestroyCombined(Item source,Item other) => Begin(source,other);
        void Begin(Item source,Item other)
        {
            if(started || source==null || source.square==null)return;
            started=true;primary=source;absorbed=other;
            primary.destroying=true;if(absorbed!=null)absorbed.destroying=true;
            if(GetComponent<GameBlocker>()==null)gameObject.AddComponent<GameBlocker>();
            mainVisual=primary.GetSpriteRenderer();mainScale=mainVisual.transform.localScale;mainOrder=mainVisual.sortingOrder;mainLayer=mainVisual.sortingLayerID;
            mainVisual.sortingLayerName=PowerUpEffectOrder.SortingLayer;mainVisual.sortingOrder=PowerUpEffectOrder.Value+8;
            if(primary.anim!=null)primary.anim.enabled=false;
            if(absorbed!=null)
            {
                incomingVisual=absorbed.GetSpriteRenderer();incomingScale=incomingVisual.transform.localScale;
                incomingPosition=incomingVisual.transform.position;incomingOrder=incomingVisual.sortingOrder;incomingLayer=incomingVisual.sortingLayerID;
                incomingVisual.sortingOrder=PowerUpEffectOrder.Value+7;
                if(absorbed.anim!=null)absorbed.anim.enabled=false;
            }
            fuse=DynamiteFuseEffect.Attach(mainVisual,absorbed!=null?.90f:.32f,primary.GetSpriteRenderers());
            StartCoroutine(Sequence());
        }
        IEnumerator Sequence()
        {
            var manager=LevelManager.THIS;var data=manager.levelData;var cell=primary.square;
            bool combined=absorbed!=null;
            float anticipation=combined ? .90f : .32f;
            for(float time=0;time<anticipation;time+=Time.deltaTime)
            {
                if(manager==null || manager.levelData!=data || cell==null)yield break;
                float t=Mathf.Clamp01(time/anticipation);
                if(combined)
                {
                    float pull=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.35f));
                    if(incomingVisual!=null)
                    {
                        incomingVisual.transform.position=Vector3.Lerp(incomingPosition,mainVisual.transform.position,pull);
                        incomingVisual.transform.localScale=incomingScale*Mathf.Lerp(1,.12f,pull);
                        if(pull>=.98f)incomingVisual.enabled=false;
                    }
                    float grow=Mathf.Clamp01((t-.13f)/.5f);
                    float scale=Mathf.Lerp(1,2.12f,1-Mathf.Pow(1-grow,3));
                    float squeeze=Mathf.SmoothStep(.78f,1,t)*.12f;
                    mainVisual.transform.localScale=Vector3.Scale(mainScale,new Vector3(scale*(1+squeeze),scale*(1-squeeze),1));
                }
                else
                {
                    float pulse=Mathf.Sin(t*Mathf.PI);
                    mainVisual.transform.localScale=Vector3.Scale(mainScale,new Vector3(1+.14f*pulse,1-.08f*pulse,1));
                }
                yield return null;
            }
            if(manager==null || manager.levelData!=data || cell==null)yield break;
            int radius=combined?cell.field.squaresArray.Where(s=>s!=null).Max(s=>Mathf.Max(Mathf.Abs(s.col-cell.col),Mathf.Abs(s.row-cell.row))):BlastRadius;
            var barriers=RoyalAves.Gameplay.ImpactBarrierSnapshot.Capture(cell.field);
            var blast=DynamiteBlastEffect.Play(cell.transform.position,cell.field.squareWidth,radius,combined);
            if(explosionSound==null)explosionSound=Resources.Load<AudioClip>("Audio/explosao-som");
            SoundBase.Instance?.PlayLimitSound(explosionSound);
            primary.HideSprites(true);if(absorbed!=null)absorbed.HideSprites(true);
#if UNITY_EDITOR
            BlastApplied?.Invoke(cell,radius,combined);
#endif
            if(fuse!=null)Destroy(fuse.gameObject);
            var affected=combined
                ? cell.field.squaresArray.Where(s=>s!=null && !s.IsNone()).Distinct().ToList()
                : manager.GetSquaresAroundSquare(cell,barriers,radius);
            if(affected.Any(square=>square.type==SquareTypes.JellyBlock))
                manager.levelData.GetTargetObject().CheckSquares(affected.ToArray());
            if(combined)
            {
                // Snapshot before activation: secondary specials may change the board.
                var items=affected.Select(s=>s.Item).Where(i=>i!=null && i!=primary && i!=absorbed).Distinct().ToArray();
                foreach(var item in items)
                    if(item!=null && (item.Combinable || item.Explodable))item.DestroyItem(true,true,primary);
                foreach(var square in affected)if(square!=null)square.DestroyBlock();
            }
            else cell.field.DestroyItemsAround(cell,primary,barriers,radius);
            if(absorbed!=null)manager.levelData.GetTargetObject().CheckItems(new[]{primary,absorbed});
            yield return new WaitForSeconds(ImpactPause);
            while(blast!=null && blast.IsEmphasizing)yield return null;
            if(manager==null || manager.levelData!=data)yield break;
            if(absorbed!=null)absorbed.DestroyBehaviour();
            primary.DestroyBehaviour();
        }
        void OnDisable()
        {
            if(mainVisual!=null){mainVisual.transform.localScale=mainScale;mainVisual.sortingOrder=mainOrder;mainVisual.sortingLayerID=mainLayer;}
            if(incomingVisual!=null)
            {
                incomingVisual.transform.localScale=incomingScale;incomingVisual.transform.position=incomingPosition;
                incomingVisual.sortingOrder=incomingOrder;incomingVisual.sortingLayerID=incomingLayer;
            }
            if(fuse!=null)Destroy(fuse.gameObject);
            started=false;primary=absorbed=null;mainVisual=incomingVisual=null;
        }
    }
}
