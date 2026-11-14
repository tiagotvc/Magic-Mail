using System.Collections;
using System.Linq;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.System;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    // Independent overlay; never reinterprets the legacy WireBlock or replaces the underlying item.
    public sealed class PostalTapeLayer : MonoBehaviour
    {
        public const float PeelSpeedMultiplier = 1.25f;
        public const float PeelDuration = 1.08f / PeelSpeedMultiplier;
        public bool IsPeeling { get; private set; }
        public bool IsReleased { get; private set; }
        public Square Cell { get; private set; }
        public event System.Action<PostalTapeLayer> Released;
        SpriteRenderer[] strips;
        Mesh[] peelMeshes;
        Material peelMaterial;
        AnimateItems barrier;
        LevelManager manager;
        Square authoredLayer;
        SweetSugar.Scripts.Level.LevelData data;
        public static PostalTapeLayer For(Square square)
        {
            if (square == null) return null;
            return square.GetComponent<PostalTapeLayer>() ?? square.transform.parent?.GetComponent<PostalTapeLayer>();
        }
        public static bool Holds(Square square) { var tape=For(square);return tape!=null && !tape.IsReleased; }
        public void Initialize(Square cell, Sprite sprite, Square placement = null)
        {
            authoredLayer=placement;
            Cell=cell;manager=LevelManager.THIS;data=manager.levelData;
            var neighbor=cell.field.GetSquare(cell.col==0?1:cell.col-1,cell.row);
            float unit=neighbor!=null ? Mathf.Abs(cell.transform.InverseTransformPoint(neighbor.transform.position).x) : 2.2f;
            strips=new SpriteRenderer[2];
            for(int i=0;i<2;i++)
            {
                var child=new GameObject("PostalTapeStrip_"+i);child.transform.SetParent(transform,false);
                child.transform.localPosition=new Vector3(0,0,-.04f-i*.005f);
                child.transform.localRotation=Quaternion.Euler(0,0,i==0?45:-45);
                child.transform.localScale=new Vector3(unit*1.18f/sprite.bounds.size.x,unit*.15f/sprite.bounds.size.y,1);
                var view=child.AddComponent<SpriteRenderer>();view.sprite=sprite;view.color=new Color(1,1,1,.60f);
                view.sortingLayerName=RoyalAves.Effects.PowerUpEffectOrder.SortingLayer;view.sortingOrder=80+i;
                strips[i]=view;
            }
        }
        public bool HandleHit(Square origin)
        {
            if(IsReleased)return false;
            if(IsPeeling)return true;
            if(origin!=null && origin!=Cell && Mathf.Abs(origin.col-Cell.col)+Mathf.Abs(origin.row-Cell.row)!=1)return true;
            if(manager==null || manager.levelData!=data)return true;
            IsPeeling=true;Cell.AbsorbIceImpact();
            barrier=gameObject.AddComponent<AnimateItems>();barrier.target=true;barrier.linkObjectHash=GetHashCode();manager.animateItems.Add(barrier);
            StartCoroutine(Peel());return true;
        }
        IEnumerator Peel()
        {
            // A moving adhesion line, rounded fold and full-length loose tail simulate pulling tape off.
            const int segments=36;
            peelMeshes=new Mesh[strips.Length];
            peelMaterial=new Material(Shader.Find("Sprites/Default")){mainTexture=strips[0].sprite.texture};
            for(int i=0;i<strips.Length;i++)
            {
                var source=strips[i];source.enabled=false;
                var child=new GameObject("CurlingTapeMesh");child.transform.SetParent(source.transform,false);
                var uv=new Vector2[(segments+1)*2];var colors=new Color[uv.Length];var triangles=new int[segments*6];
                var rect=source.sprite.textureRect;var tex=source.sprite.texture;
                for(int j=0;j<=segments;j++)for(int edge=0;edge<2;edge++)
                {int index=j*2+edge;uv[index]=new Vector2(Mathf.Lerp(rect.xMin,rect.xMax,j/(float)segments)/tex.width,(edge==0?rect.yMin:rect.yMax)/tex.height);colors[index]=new Color(1,1,1,.60f);}
                for(int j=0;j<segments;j++){int n=j*2,k=j*6;triangles[k]=n;triangles[k+1]=n+1;triangles[k+2]=n+2;triangles[k+3]=n+1;triangles[k+4]=n+3;triangles[k+5]=n+2;}
                var mesh=new Mesh{name="PostalTapeCurl",vertices=new Vector3[uv.Length],uv=uv,colors=colors,triangles=triangles};mesh.MarkDynamic();peelMeshes[i]=mesh;
                child.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=child.AddComponent<MeshRenderer>();renderer.sharedMaterial=peelMaterial;renderer.sortingLayerName=source.sortingLayerName;renderer.sortingOrder=source.sortingOrder;
            }
            float elapsed=0;
            while(elapsed<PeelDuration)
            {
                elapsed+=Time.deltaTime;
                for(int i=0;i<strips.Length;i++)
                {
                    // The upper diagonal lifts first; the lower one follows as the crossing opens.
                    float age=Mathf.Max(0,elapsed*PeelSpeedMultiplier-(i==1?0:.22f));
                    float peel=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.64f));
                    float detached=Mathf.Clamp01((age-.64f)/.18f);
                    var bounds=strips[i].sprite.bounds;float width=bounds.size.x,front=bounds.max.x-width*peel,radius=width*.09f;
                    var vertices=new Vector3[(segments+1)*2];var colors=new Color[vertices.Length];
                    // Keep the entire strip visible until it is completely off the piece.
                    float alpha=.60f*(1-Mathf.SmoothStep(0,1,detached));
                    for(int j=0;j<=segments;j++)for(int edge=0;edge<2;edge++)
                    {
                        float x=Mathf.Lerp(bounds.min.x,bounds.max.x,j/(float)segments),y=edge==0?bounds.min.y:bounds.max.y;
                        float loose=Mathf.Max(0,x-front);
                        const float pullAngle=150f*Mathf.Deg2Rad;
                        float theta=Mathf.Min(pullAngle,loose/radius),tail=Mathf.Max(0,loose-radius*pullAngle);
                        float rise=radius*(1-Mathf.Cos(theta))+tail*Mathf.Sin(pullAngle);
                        // Preserve loose material as a visible tail, instead of compressing it into a roll.
                        var point=loose>0?new Vector3(front+radius*Mathf.Sin(theta)+tail*Mathf.Cos(pullAngle),y+rise*.90f,-rise*.65f):new Vector3(x,y,0);
                        point+=new Vector3(-width*.18f*detached,width*.25f*detached,-width*.12f*detached);
                        vertices[j*2+edge]=point;
                        float highlight=Mathf.Sin(theta)*.13f;
                        colors[j*2+edge]=new Color(1+highlight,1+highlight*.6f,1-highlight*.12f,alpha);
                    }
                    peelMeshes[i].vertices=vertices;peelMeshes[i].colors=colors;peelMeshes[i].RecalculateBounds();
                }
                yield return null;
            }
            if(manager!=null && manager.levelData==data)
            {
                Cell.AbsorbIceImpact();
            }
            IsReleased=true;
            Released?.Invoke(this);
            if(authoredLayer!=null)Cell.CompleteLayerRemoval(authoredLayer);
            foreach(var strip in strips)if(strip!=null)Destroy(strip.gameObject);
            RemoveBarrier();Destroy(this);
        }
        void RemoveBarrier(){if(manager!=null && barrier!=null)manager.animateItems.Remove(barrier);if(barrier!=null)Destroy(barrier);barrier=null;}
        void OnDestroy(){RemoveBarrier();if(strips!=null)foreach(var strip in strips)if(strip!=null)Destroy(strip.gameObject);if(peelMeshes!=null)foreach(var mesh in peelMeshes)if(mesh!=null)Destroy(mesh);if(peelMaterial!=null)Destroy(peelMaterial);}
    }
}
