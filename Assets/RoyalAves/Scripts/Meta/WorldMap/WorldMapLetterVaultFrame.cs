using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta.WorldMap
{
    // The recessed shield is independent of the seasonal pig artwork.
    public sealed class WorldMapLetterVaultFrame : MaskableGraphic
    {
        static Color C(byte r,byte g,byte b)=>new Color32(r,g,b,255);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            var corners=new[]{new Vector2(-.48f,.36f),new Vector2(-.35f,.50f),new Vector2(.35f,.50f),new Vector2(.48f,.36f),new Vector2(.46f,-.24f),new Vector2(.30f,-.40f),new Vector2(0,-.53f),new Vector2(-.30f,-.40f),new Vector2(-.46f,-.24f)};
            var contour=new List<Vector2>();
            for(int i=0;i<corners.Length;i++)
            {
                var p=corners[i];var enter=Vector2.Lerp(p,corners[(i+corners.Length-1)%corners.Length],.22f);
                var exit=Vector2.Lerp(p,corners[(i+1)%corners.Length],.22f);
                for(int j=0;j<6;j++){float t=j/5f;contour.Add((1-t)*(1-t)*enter+2*(1-t)*t*p+t*t*exit);}
            }
            Color Shade(Vector2 p,Color low,Color high)=>Color.Lerp(low,high,Mathf.Clamp01(p.y+.5f));
            void Fill(float scale,Vector2 offset,Color low,Color high)
            {
                int n=vh.currentVertCount;
                Vector2 Point(Vector2 p)=>r.center+Vector2.Scale(p*scale+offset,r.size);
                vh.AddVert(Point(Vector2.zero),Color.Lerp(low,high,.5f),Vector2.zero);
                foreach(var p in contour)vh.AddVert(Point(p),Shade(p,low,high),Vector2.zero);
                for(int i=0;i<contour.Count;i++)vh.AddTriangle(n,n+1+i,n+1+(i+1)%contour.Count);
            }
            // Short colored support, soft yellow/orange cartoon gold, then a plum inset.
            Fill(1,new Vector2(0,-.018f),C(102,43,109),C(146,65,143));
            Fill(1,Vector2.zero,C(238,135,4),C(255,214,39));
            Fill(.965f,new Vector2(0,.006f),C(255,180,12),C(255,228,71));
            Fill(.925f,Vector2.zero,C(163,82,157),C(194,118,185));
            Fill(.81f,new Vector2(0,.006f),C(135,62,137),C(109,43,123));
            Fill(.775f,new Vector2(0,-.012f),C(174,94,166),C(149,72,151));
        }
    }
}
