using System.Collections;
using System.Linq;
using RoyalAves.Effects;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.System;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    public sealed class SafeBoardController : MonoBehaviour
    {
        SafeBreakAnimation animation;
        Square layer;
        Square[] cells;
        LevelManager manager;
        LevelData data;
        AnimateItems barrier;
        public bool IsComplete {get;private set;}
        public int Hits => animation!=null?animation.Hits:0;
        public void Initialize(SafeBreakAnimation visual,Square sharedLayer,Square[] footprint)
        {
            animation=visual;layer=sharedLayer;cells=footprint;
            manager=LevelManager.THIS;data=manager?.levelData;
            animation.Finished+=Complete;
        }
        public bool AcceptHit(bool special)
        {
            if(IsComplete || animation==null || manager==null || manager.levelData!=data)return false;
            bool accepted=animation.Hit(special);
            if(accepted && animation.Hits==5)
            {
                barrier=gameObject.AddComponent<AnimateItems>();barrier.target=true;manager.animateItems.Add(barrier);
            }
            return accepted;
        }
        void Complete()
        {
            if(IsComplete || manager==null || manager.levelData!=data || layer==null)return;
            IsComplete=true;
            // The same layer is referenced by all four cells. Remove every reference before
            // its deferred destruction, then count exactly ONE safe. Coins are decorative.
            bool removed=false;
            foreach(var cell in cells)if(cell!=null)removed|=cell.CompleteLayerRemoval(layer);
            if(removed)foreach(var target in data.TargetCounters.Where(t=>t.targetLevel.name=="Safe"))target.changeCount(-1);
            StartCoroutine(FinishEffect());
        }
        IEnumerator FinishEffect()
        {
            yield return new WaitForSeconds(2.65f);
            RemoveBarrier();
            if(manager!=null && manager.levelData==data)manager.CheckWinLose();
            Destroy(gameObject);
        }
        void RemoveBarrier()
        {
            if(manager!=null && barrier!=null)manager.animateItems.Remove(barrier);
            if(barrier!=null)Destroy(barrier);barrier=null;
        }
        void OnDestroy() {if(animation!=null)animation.Finished-=Complete;RemoveBarrier();}
    }
}
