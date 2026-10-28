using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>Clips the actual egg sprite into matching irregular shell pieces without editing its pixels.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalPostmanEggShard : MaskableGraphic
    {
        public Sprite sprite;
        public Vector2[] polygon;
        public override Texture mainTexture=>sprite!=null?sprite.texture:Texture2D.whiteTexture;
        public void Set(Sprite source,Vector2[] points){sprite=source;polygon=points;SetAllDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(sprite==null||polygon==null||polygon.Length<3)return;
            var rect=rectTransform.rect;var uv=DataUtility.GetOuterUV(sprite);
            foreach(var p in polygon)vh.AddVert(new Vector2(rect.xMin+p.x*rect.width,rect.yMin+p.y*rect.height),color,new Vector2(Mathf.Lerp(uv.x,uv.z,p.x),Mathf.Lerp(uv.y,uv.w,p.y)));
            for(int i=1;i<polygon.Length-1;i++)vh.AddTriangle(0,i,i+1);
        }
        public static Vector2[][] Pieces()
        {
            var seeds=new[]{new Vector2(.22f,.16f),new Vector2(.62f,.12f),new Vector2(.84f,.35f),new Vector2(.47f,.38f),new Vector2(.14f,.48f),new Vector2(.3f,.75f),new Vector2(.68f,.68f),new Vector2(.48f,.94f)};
            var result=new Vector2[seeds.Length][];
            for(int i=0;i<seeds.Length;i++)
            {
                var points=new List<Vector2>{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};
                for(int j=0;j<seeds.Length;j++)if(i!=j)
                {
                    var normal=seeds[j]-seeds[i];float limit=(seeds[j].sqrMagnitude-seeds[i].sqrMagnitude)*.5f;var next=new List<Vector2>();
                    for(int p=0;p<points.Count;p++){var a=points[p];var b=points[(p+1)%points.Count];float da=Vector2.Dot(a,normal)-limit,db=Vector2.Dot(b,normal)-limit;if(da<=0)next.Add(a);if((da<=0)!=(db<=0))next.Add(Vector2.Lerp(a,b,da/(da-db)));}
                    points=next;
                }
                result[i]=points.ToArray();
            }
            return result;
        }
    }
}
