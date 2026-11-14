using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Effects
{
    // LateUpdate observes the actual tween result, avoiding timer/tween Update ordering races.
    public sealed class RocketAxisPassObserver : MonoBehaviour
    {
        Vector3 origin;
        bool horizontal;
        Transform halfA, halfB;
        Vector3[] cells;
        bool[] passed;
        Action<int> onPassed;
        public void Initialize(Vector3 source, bool alongRow, Transform a, Transform b, IList<Vector3> targets, Action<int> callback)
        {
            origin=source;horizontal=alongRow;halfA=a;halfB=b;onPassed=callback;
            cells=new Vector3[targets.Count];passed=new bool[targets.Count];
            for(int i=0;i<targets.Count;i++)cells[i]=targets[i];
            Observe();
        }
        void LateUpdate() => Observe();
        float Along(Vector3 p) => horizontal ? p.x-origin.x : p.y-origin.y;
        bool Reached(Transform half, float target)
        {
            if(half==null)return false;
            float head=Along(half.position);
            return target>=0 ? head>=target-.002f : head<=target+.002f;
        }
        void Observe()
        {
            if(cells==null)return;
            for(int i=0;i<cells.Length;i++)
            {
                if(passed[i])continue;
                float cross=horizontal?cells[i].y-origin.y:cells[i].x-origin.x;
                if(Mathf.Abs(cross)>.01f)continue;
                float target=Along(cells[i]);
                if(Mathf.Abs(target)>.002f && !Reached(halfA,target) && !Reached(halfB,target))continue;
                passed[i]=true;onPassed?.Invoke(i);
            }
        }
    }
}
