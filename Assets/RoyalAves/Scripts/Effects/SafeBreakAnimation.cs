using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Effects
{
    // Visual controller only. Call Hit(true) from a valid special; Hit(false) only shakes.
    // Five static states, never a frame-by-frame sprite animation. No campaign/economy writes.
    public sealed class SafeBreakAnimation : MonoBehaviour
    {
        [SerializeField] Sprite[] stages=new Sprite[5];
        [SerializeField] Material paint;
        [SerializeField] float bodyWidth=1.8f;
        struct TimedBurst {public SafeDebrisEffect effect;public float start;}
        readonly List<TimedBurst> bursts=new List<TimedBurst>();
        [SerializeField] SpriteRenderer view;
        int hits;
        float clock, lastHit=-10, lastDamage=-10, openedAt=-1;
        bool completed;
        public bool Automatic {get;set;}=true;
        public int Hits => hits;
        public bool Completed => completed;
        public int BurstCount => bursts.Count;
        public event Action Finished;
        public SpriteRenderer View => view;
        public void Configure(Sprite[] images,Material material)
        {
            if(images==null || images.Length!=5 || Array.Exists(images,s=>s==null))
                throw new ArgumentException("O cofre precisa de cinco estados estáticos.");
            stages=images;paint=material;EnsureView();ResetAnimation();
        }
        void EnsureView()
        {
            if(view==null)
            {
                var child=new GameObject("SafeVisual");child.layer=gameObject.layer;child.transform.SetParent(transform,false);
                view=child.AddComponent<SpriteRenderer>();view.sharedMaterial=paint;
            }
            // Obstacles and their debris stay below flying power-ups. Apply this to the
            // serialized renderer too, so an existing prefab cannot retain the old tie.
            view.sortingLayerName=PowerUpEffectOrder.SortingLayer;
            view.sortingOrder=PowerUpEffectOrder.Value-10;
        }
        void Awake() {if(stages.Length==5 && Array.TrueForAll(stages,s=>s!=null)){EnsureView();ResetAnimation();}}
        public void ResetAnimation()
        {
            foreach(var b in bursts)if(b.effect!=null)Release(b.effect.gameObject);
            bursts.Clear();hits=0;clock=0;lastHit=lastDamage=-10;openedAt=-1;completed=false;
            EnsureView();SetStage(0);view.enabled=true;
        }
        void SetStage(int stage)
        {
            view.sprite=stages[stage];view.sharedMaterial=paint;
            view.transform.localScale=Vector3.one*(bodyWidth/view.sprite.bounds.size.x);
            view.transform.localPosition=Vector3.zero;view.transform.localRotation=Quaternion.identity;
        }
        void Burst(Rect region,int count,int seed)
        {
            var effect=SafeDebrisEffect.Metal(view,region,count,seed);effect.Automatic=false;
            bursts.Add(new TimedBurst{effect=effect,start=clock});
        }
        public bool Hit(bool special)
        {
            if(completed || openedAt>=0 || (special && clock-lastDamage<.2f))return false;
            lastHit=clock;
            if(!special)return false;
            lastDamage=clock;
            switch(hits)
            {
                case 0:
                    Burst(new Rect(.1f,.4f,.28f,.23f),6,11);
                    Burst(new Rect(.62f,.4f,.28f,.23f),6,12);break;
                case 1:
                    Burst(new Rect(.39f,.57f,.06f,.05f),3,21);
                    Burst(new Rect(.55f,.4f,.05f,.05f),3,22);break;
                case 2: Burst(new Rect(.37f,.37f,.26f,.27f),9,31);break;
                case 3: Burst(new Rect(.19f,.19f,.62f,.63f),16,41);break;
            }
            hits++;
            if(hits<5)SetStage(hits);
            else openedAt=clock;
            return true;
        }
        void Shatter()
        {
            // Only the four frame rails: the illustrated pile is replaced by independently flying coins.
            // Sample the clean closed-body texture for metal so coins painted over the bottom
            // rail in the concept never become stuck to a flying turquoise fragment.
            var openedSprite=view.sprite;
            view.sprite=stages[3];
            Burst(new Rect(0,0,.19f,1),6,51);
            Burst(new Rect(.81f,0,.19f,1),6,52);
            Burst(new Rect(.19f,0,.62f,.21f),6,53);
            Burst(new Rect(.19f,.81f,.62f,.19f),6,54);
            view.sprite=openedSprite;
            var coins=SafeDebrisEffect.Coins(view);coins.Automatic=false;
            bursts.Add(new TimedBurst{effect=coins,start=clock});
            view.enabled=false;completed=true;Finished?.Invoke();
        }
        public void Advance(float delta)
        {
            if(delta<0)throw new ArgumentOutOfRangeException(nameof(delta));
            clock+=delta;
            if(openedAt>=0 && !completed && clock-openedAt>=.16f)Shatter();
            if(view!=null && view.enabled)
            {
                float t=clock-lastHit;
                float shake=t<.18f?Mathf.Sin(t*100)*.035f*(1-t/.18f):0;
                view.transform.localPosition=new Vector3(shake,0);
            }
            for(int i=bursts.Count-1;i>=0;i--)
            {
                var b=bursts[i];float age=clock-b.start;
                if(age>=b.effect.Lifetime){Release(b.effect.gameObject);bursts.RemoveAt(i);}
                else b.effect.Sample(age);
            }
        }
        void Update() {if(Automatic && view!=null)Advance(Time.deltaTime);}
        static void Release(UnityEngine.Object value)
        {
            if(value is GameObject node)node.SetActive(false);
            if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);
        }
        void OnDestroy() {foreach(var b in bursts)if(b.effect!=null)Release(b.effect.gameObject);bursts.Clear();}
    }
}
