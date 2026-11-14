using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RoyalAves.Effects;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.System;
using UnityEngine;

namespace RoyalAves.Gameplay
{
    public sealed class SortingSignalCell : MonoBehaviour
    {
        public Sprite[] requestedPieces;
        public int[] requestedColors;
        public Material lensTemplate;
        public int Hits {get;private set;}
        public int ColorIndex {get;private set;}
        public bool IsBreaking => Hits>=3;
        public string LastFeedback {get;private set;}="Combine trevos verdes ao lado.";
        public string RequestedName => Names[ColorIndex];
        public static readonly string[] Names={"Verde: trevo","Vermelho: selo","Lilás: pena","Azul: envelope"};
        static readonly Color[] LensColors={new Color(.25f,1,.15f),new Color(1,.12f,.18f),new Color(.65f,.28f,1),new Color(.14f,.55f,1)};
        readonly HashSet<long> matches=new HashSet<long>();
        readonly List<GameObject> pips=new List<GameObject>();
        Square cell;
        LevelManager manager;
        SweetSugar.Scripts.Level.LevelData data;
        SpriteRenderer view,glyph;
        Material lens,pipMaterial;
        Mesh pipMesh;
        AnimateItems barrier;
        int lastMove;
        float nextDamage,shakeUntil;
        Vector3 resting;
        void Start()
        {
            cell=transform.parent!=null?transform.parent.GetComponent<Square>():null;
            if(cell==null)return;
            manager=LevelManager.THIS;data=manager.levelData;lastMove=manager.moveID;
            view=GetComponent<SpriteRenderer>();view.enabled=true;
            view.sortingLayerName=PowerUpEffectOrder.SortingLayer;view.sortingOrder=PowerUpEffectOrder.Value-10;
            lens=new Material(lensTemplate);view.sharedMaterial=lens;resting=transform.localPosition;
            var child=new GameObject("Cor pedida");child.transform.SetParent(transform,false);
            var master=view.sprite;
            child.transform.localPosition=new Vector3((master.texture.width*.496f-master.rect.x-master.pivot.x)/master.pixelsPerUnit,
                (master.texture.height*(Soft3DElementStyle.Has(this)?.55f:.576f)-master.rect.y-master.pivot.y)/master.pixelsPerUnit,-.015f);
            glyph=child.AddComponent<SpriteRenderer>();glyph.sortingLayerName=view.sortingLayerName;glyph.sortingOrder=view.sortingOrder+1;
            BuildPips();ApplyColor();LevelManager.OnTurnEnd+=AfterSettledTurn;
        }
        void BuildPips()
        {
            const int count=16;var points=new Vector3[count+1];var colors=new Color[count+1];var triangles=new int[count*3];
            colors[0]=Color.white;
            for(int i=0;i<count;i++)
            {
                float a=i*Mathf.PI*2/count;points[i+1]=new Vector3(Mathf.Cos(a),Mathf.Sin(a))*.047f;
                colors[i+1]=new Color(1,.88f,.46f);triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%count+1;
            }
            pipMesh=new Mesh{name="SignalResistanceDots",vertices=points,colors=colors,triangles=triangles};
            pipMaterial=new Material(Shader.Find("Sprites/Default")){mainTexture=Texture2D.whiteTexture};
            for(int i=0;i<3;i++)
            {
                var pip=new GameObject("Acerto restante "+(i+1));pip.transform.SetParent(transform,false);
                pip.transform.localPosition=new Vector3((i-1)*.17f,-.70f,-.02f);
                pip.AddComponent<MeshFilter>().sharedMesh=pipMesh;
                var renderer=pip.AddComponent<MeshRenderer>();renderer.sharedMaterial=pipMaterial;
                renderer.sortingLayerName=view.sortingLayerName;renderer.sortingOrder=view.sortingOrder+2;pips.Add(pip);
            }
        }
        void AfterSettledTurn()
        {
            if(manager==null || manager.levelData!=data || manager.moveID==lastMove || IsBreaking)return;
            lastMove=manager.moveID;NextColorForReview();
        }
        public void NextColorForReview()
        {
            if(IsBreaking || view==null)return;
            ColorIndex=(ColorIndex+1)%requestedPieces.Length;matches.Clear();ApplyColor();
            LastFeedback="Cor mudou depois das cascatas: "+RequestedName;
        }
        void ApplyColor()
        {
            lens.SetFloat("_Mode",ColorIndex==0?0:1);lens.SetColor("_LensColor",LensColors[ColorIndex]);
            glyph.enabled=ColorIndex!=0;glyph.sprite=requestedPieces[ColorIndex];
            glyph.transform.localScale=Vector3.one*(.56f/Mathf.Max(glyph.sprite.bounds.size.x,glyph.sprite.bounds.size.y));
        }
        public bool HandleHit(Square origin)
        {
            if(cell==null || IsBreaking || manager==null || manager.levelData!=data)return false;
            bool special=origin==null || origin==cell;
            if(!special)
            {
                if(Mathf.Abs(cell.col-origin.col)+Mathf.Abs(cell.row-origin.row)!=1)return false;
                var item=origin.Item;
                if(item==null || item.currentType!=ItemsTypes.NONE || item.explodedItem!=null)return false;
                var sprite=item.GetSprite();
                if(sprite==null || item.color!=requestedColors[ColorIndex])
                {LastFeedback="Sem dano: precisa de "+RequestedName;shakeUntil=Time.time+.16f;return false;}
                long key=item.combinationID!=-1?(long)item.combinationID<<1:((long)manager.destLoopIterations<<32)|(uint)Time.frameCount;
                if(!matches.Add(key))return false;
            }
            if(Time.time<nextDamage)return false;
            nextDamage=Time.time+.2f;Hits++;shakeUntil=Time.time+.14f;
            pips[3-Hits].SetActive(false);LastFeedback=$"Acerto {(special?"de especial":"da cor correta")}: {Hits}/3";
            if(IsBreaking)
            {
                barrier=gameObject.AddComponent<AnimateItems>();barrier.target=true;manager.animateItems.Add(barrier);
                if(Soft3DElementStyle.Has(this))
                {
                    Soft3DElementEffect.Burst(view,new Color(.80f,.45f,.19f),20);
                    Soft3DElementEffect.Burst(view,LensColors[ColorIndex],8,.8f);
                }
                else SafeDebrisEffect.Metal(view,new Rect(0,0,1,1),18,471);
                view.enabled=false;glyph.enabled=false;
                foreach(var pip in pips)pip.SetActive(false);
                StartCoroutine(Complete());
            }
            return true;
        }
        IEnumerator Complete()
        {
            yield return new WaitForSeconds(.22f);
            if(manager==null || manager.levelData!=data || cell==null)yield break;
            var targets=data.TargetCounters.Where(t=>t.targetLevel.name=="SortingSignals").ToArray();
            RemoveBarrier();
            if(cell.CompleteLayerRemoval(GetComponent<Square>()))foreach(var target in targets)target.changeCount(-1);
        }
        void Update()
        {
            if(view==null)return;
            transform.localPosition=resting+(Time.time<shakeUntil?Vector3.right*(Mathf.Sin(Time.time*100)*.025f):Vector3.zero);
        }
        static void Release(UnityEngine.Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        void RemoveBarrier(){if(manager!=null && barrier!=null)manager.animateItems.Remove(barrier);Release(barrier);barrier=null;}
        void OnDestroy()
        {
            LevelManager.OnTurnEnd-=AfterSettledTurn;RemoveBarrier();
            Release(lens);Release(pipMesh);Release(pipMaterial);
        }
    }
}
