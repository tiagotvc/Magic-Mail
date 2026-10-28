using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>Yellow lettering, orange lower relief and a strong blue contour.</summary>
    [RequireComponent(typeof(Text))]
    public sealed class RoyalPostmanRewardText : BaseMeshEffect
    {
        public float contour=4,relief=6;
        public override void ModifyMesh(VertexHelper vh)
        {
            if(!IsActive())return;var input=new List<UIVertex>();vh.GetUIVertexStream(input);var output=new List<UIVertex>(input.Count*11);
            void Copy(Vector2 offset,Color tint){foreach(var vertex in input){var v=vertex;v.position+=(Vector3)offset;tint.a=vertex.color.a/255f;v.color=tint;output.Add(v);}}
            var blue=new Color(.015f,.16f,.62f);for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Copy(new Vector2(Mathf.Cos(a)*contour,Mathf.Sin(a)*contour-relief*.35f),blue);}
            Copy(Vector2.down*relief,new Color(1,.38f,.01f));
            var r=((RectTransform)transform).rect;
            foreach(var vertex in input){var v=vertex;float t=Mathf.InverseLerp(r.yMin,r.yMax,v.position.y);var c=Color.Lerp(new Color(1,.65f,.015f),new Color(1,1,.36f),Mathf.Clamp01(t*1.3f));c.a=v.color.a/255f;v.color=c;output.Add(v);}
            vh.Clear();vh.AddUIVertexTriangleStream(output);
        }
    }
}
