using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>Regular concentric geometry, independently of small distortions in the painted texture.</summary>
    [AddComponentMenu("UI/Royal Postman/Symmetric painted face")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoyalPostmanSymmetricButtonFace : MaskableGraphic
    {
        public Texture2D paint;
        [Min(0)] public float cornerRadius = 40;
        [Min(0)] public float bevelWidth = 20;
        [Range(8, 48)] public int cornerSegments = 24;
        [Min(0)] public float antialiasWidth = .8f;
        public bool drawCenter = true;
        // Coordinates measured in the unchanged source PNG, from its upper-left corner.
        public Rect sourceOuter = new Rect(96, 110, 1730, 604);
        public Rect sourceFace = new Rect(137, 168, 1648, 456);
        public float sourceOuterRadius = 180;
        public float sourceFaceRadius = 120;
        public override Texture mainTexture => paint != null ? paint : Texture2D.whiteTexture;
        Rect PaintRect
        {
            get { var r=GetPixelAdjustedRect(); return new Rect(Mathf.Round(r.x*1024)/1024,Mathf.Round(r.y*1024)/1024,Mathf.Round(r.width*1024)/1024,Mathf.Round(r.height*1024)/1024); }
        }

        public Vector2 ContourPoint(Rect rectangle, float radius, int corner, float angle)
        {
            radius = Mathf.Clamp(radius, 0, Mathf.Min(rectangle.width, rectangle.height) * .5f);
            float sx = corner == 0 || corner == 3 ? 1 : -1;
            float sy = corner < 2 ? 1 : -1;
            var center = rectangle.center + new Vector2(sx * (rectangle.width * .5f - radius), sy * (rectangle.height * .5f - radius));
            float radians = angle * Mathf.Deg2Rad;
            return center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
        }

        public Rect FaceRect
        {
            get { var r = PaintRect; float b = EffectiveBevel; return new Rect(r.x + b, r.y + b, r.width - b * 2, r.height - b * 2); }
        }
        public float EffectiveRadius => Mathf.Min(cornerRadius, Mathf.Min(PaintRect.width, PaintRect.height) * .5f);
        public float EffectiveBevel => Mathf.Clamp(bevelWidth, 0, Mathf.Min(EffectiveRadius, Mathf.Min(PaintRect.width, PaintRect.height) * .49f));

        Vector2 SourceUV(Rect topLeftRect, float radius, Vector2 point, Rect target)
        {
            var source = new Rect(topLeftRect.x, paint.height - topLeftRect.yMax, topLeftRect.width, topLeftRect.height);
            var direction = new Vector2((point.x-target.center.x)/target.width*source.width,(point.y-target.center.y)/target.height*source.height);
            var half=source.size*.5f;float lo=0,hi=2;
            // A continuous perimeter mapping avoids UV seams where circle quadrants meet.
            for(int i=0;i<22;i++){
                float t=(lo+hi)*.5f;var q=new Vector2(Mathf.Abs(direction.x*t),Mathf.Abs(direction.y*t))-(half-Vector2.one*radius);
                float d=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-radius;
                if(d<=0)lo=t;else hi=t;
            }
            var p=source.center+direction*((lo+hi)*.5f);
            return new Vector2(p.x / paint.width, p.y / paint.height);
        }
        Vector2 FlatFaceUV(Vector2 point,Rect target)
        {
            float x=(point.x-target.xMin)/target.width,y=(point.y-target.yMin)/target.height;
            return new Vector2((sourceFace.x+x*sourceFace.width)/paint.width,1-(sourceFace.yMax-y*sourceFace.height)/paint.height);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (paint == null) return;
            var outer = PaintRect; float radius = EffectiveRadius, bevel = EffectiveBevel;
            var inner = FaceRect;
            var aa = new Rect(outer.x - antialiasWidth, outer.y - antialiasWidth, outer.width + antialiasWidth * 2, outer.height + antialiasWidth * 2);
            int steps = Mathf.Clamp(cornerSegments, 8, 48), count = 4 * (steps + 1);
            for (int ring = 0; ring < 3; ring++)
            for (int corner = 0; corner < 4; corner++)
            for (int j = 0; j <= steps; j++)
            {
                float angle = corner * 90 + j * 90f / steps;
                var p = ContourPoint(ring == 0 ? aa : ring == 1 ? outer : inner, ring == 0 ? radius + antialiasWidth : ring == 1 ? radius : radius - bevel, corner, angle);
                var uv = ring==2&&drawCenter?FlatFaceUV(p,inner):SourceUV(ring < 2 ? sourceOuter : sourceFace, ring < 2 ? sourceOuterRadius : sourceFaceRadius,p,ring==0?aa:ring==1?outer:inner);
                var tint = color; if (ring == 0) tint.a = 0;
                vh.AddVert(p, tint, uv);
            }
            for (int ring = 0; ring < 2; ring++)
            for (int j = 0; j < count; j++)
            {
                int next = (j + 1) % count, a = ring * count + j, b = ring * count + next, c = (ring + 1) * count + j, d = (ring + 1) * count + next;
                vh.AddTriangle(a, c, d); vh.AddTriangle(a, d, b);
            }
            if (!drawCenter) return;
            int middle = vh.currentVertCount;
            vh.AddVert(inner.center, color, new Vector2(sourceFace.center.x / paint.width, 1 - sourceFace.center.y / paint.height));
            for (int j = 0; j < count; j++) vh.AddTriangle(middle, 2 * count + j, 2 * count + (j + 1) % count);
        }
    }
}
