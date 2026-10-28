using UnityEngine;

namespace RoyalAves.EditorTools
{
    // Pointer coordinates stay in the original local plane while the pivot moves during a drag.
    internal sealed class RoyalPostmanRectResize
    {
        readonly RectTransform target;
        readonly Rect initial;
        readonly Vector2 pivot, pointer;
        readonly Vector2Int handle;
        readonly Matrix4x4 localToWorld, worldToLocal;
        readonly Vector3 position;

        internal RoyalPostmanRectResize(RectTransform target,Vector2Int handle,Vector2 pointer)
        {
            this.target=target;this.handle=handle;this.pointer=pointer;
            initial=target.rect;pivot=target.pivot;position=target.position;
            localToWorld=target.localToWorldMatrix;worldToLocal=target.worldToLocalMatrix;
        }

        internal static Rect DragRect(Rect initial,Vector2Int handle,Vector2 delta,bool proportional)
        {
            float width=handle.x==0?initial.width:Mathf.Max(1,initial.width+delta.x*handle.x);
            float height=handle.y==0?initial.height:Mathf.Max(1,initial.height+delta.y*handle.y);
            if(proportional&&handle.x!=0&&handle.y!=0&&initial.width>0&&initial.height>0)
            {
                float x=width/initial.width,y=height/initial.height;
                float scale=Mathf.Abs(x-1)>Mathf.Abs(y-1)?x:y;
                scale=Mathf.Max(scale,Mathf.Max(1/initial.width,1/initial.height));
                width=initial.width*scale;height=initial.height*scale;
            }
            return new Rect(handle.x<0?initial.xMax-width:initial.xMin,
                handle.y<0?initial.yMax-height:initial.yMin,width,height);
        }

        internal void ApplyLocal(Vector2 point,bool proportional)
        {
            var rect=DragRect(initial,handle,point-pointer,proportional);
            var shift=rect.min+Vector2.Scale(pivot,rect.size);
            target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,rect.width);
            target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,rect.height);
            target.position=localToWorld.MultiplyPoint3x4(new Vector3(shift.x,shift.y,0));
        }

        internal bool ApplyScreen(Vector2 point,Camera camera,bool proportional)
        {
            if(!RectTransformUtility.ScreenPointToWorldPointInRectangle(target,point,camera,out var world))return false;
            var local=worldToLocal.MultiplyPoint3x4(world);ApplyLocal(new Vector2(local.x,local.y),proportional);return true;
        }

        internal void Restore()
        {
            target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,initial.width);
            target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,initial.height);
            target.position=position;
        }
    }
}
