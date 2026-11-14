using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using UnityEngine;
using SweetSugar.Scripts.System;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using RoyalAves.Effects;

namespace RoyalAves.Gameplay
{
    // Fixed satchel: one collectible per accepted impact, counted on HUD arrival.
    public sealed class BagOpeningStation : MonoBehaviour
    {
        public Sprite[] frames;
        public Sprite[] scrollFrames;
        TargetCounter deliveryTarget;
        LevelManager manager;
        SweetSugar.Scripts.Level.LevelData data;
        AnimateItems barrier;
        GameObject scroll;
        GameObject openingGlow;
        Material openingGlowMaterial;
        MailDeliveryOverlay overlay;
        static readonly Dictionary<TargetCounter,int> pending = new Dictionary<TargetCounter,int>();
        bool delivered;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPending() => pending.Clear();
        public float framesPerSecond = 20;
        public static float PlaybackSpeed = 1;
        public bool IsAnimating { get; private set; }
        readonly HashSet<int> matches = new HashSet<int>();
        SpriteRenderer body;
        Square cell;

        void Start()
        {
#if UNITY_EDITOR
            // Older laboratory prefabs may survive a script reload with the new array empty.
            if (scrollFrames == null || scrollFrames.Length != 16 || scrollFrames.Any(s => s == null))
                scrollFrames = Enumerable.Range(0,16).Select(i => UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/RoyalAves/Laboratory/ScrollOpeningFrame"+i.ToString("00")+".asset")).ToArray();
#endif
            cell = transform.parent.GetComponent<Square>();
            body = GetComponent<SpriteRenderer>();
            body.sortingLayerName = RoyalAves.Effects.PowerUpEffectOrder.SortingLayer;
            body.sortingOrder = 20;
            body.color = Color.white;
            body.sprite = frames[0];
            var neighbor = cell.field.GetSquare(cell.col+1,cell.row) ?? cell.field.GetSquare(cell.col-1,cell.row);
            float width = neighbor != null ? Vector3.Distance(cell.transform.position,neighbor.transform.position) : 1;
            var vertical = cell.field.GetSquare(cell.col,cell.row+1) ?? cell.field.GetSquare(cell.col,cell.row-1);
            float height = vertical != null ? Vector3.Distance(cell.transform.position,vertical.transform.position) : width;
            var parentScale = transform.parent.lossyScale;
            float scale = width * .88f / frames[0].bounds.size.x;
            transform.localScale = new Vector3(scale / Mathf.Abs(parentScale.x),scale / Mathf.Abs(parentScale.y),1);
            var position = transform.localPosition;
            position.x = 0;
            position.y = -height * .43f / Mathf.Abs(parentScale.y);
            transform.localPosition = position;
        }

        public void HandleHit(Square origin)
        {
            if (cell == null || origin == null) return;
            int id = -1;
            if (origin != cell)
            {
                if (!ItemFlyingLetter.IsAdjacentMatch(cell.row,cell.col,origin.row,origin.col)
                    || origin.Item == null || origin.Item.explodedItem != null) return;
                id = origin.Item.combinationID;
                if (id != -1 && matches.Contains(id)) return;
            }
            if (PlayOpening(true) && id != -1) matches.Add(id);
        }

        public bool PlayOpening(bool count = true)
        {
            if (IsAnimating || body == null) return false;
            if (frames == null || frames.Length != 16 || frames.Any(s => s == null) ||
                scrollFrames == null || scrollFrames.Length != 16 || scrollFrames.Any(s => s == null))
            {
                Debug.LogWarning("Bolsa sem sprites completos. Reprepare o laboratorio da bolsa do carteiro.",this);
                return false;
            }
            manager = LevelManager.THIS;
            data = manager != null ? manager.levelData : null;
            deliveryTarget = data?.TargetCounters.FirstOrDefault(t => t.targetLevel.name == "BagOpenings" && t.count > Reserved(t));
            if (deliveryTarget == null) return false;
            pending[deliveryTarget] = Reserved(deliveryTarget) + 1;
            barrier = gameObject.AddComponent<AnimateItems>();
            barrier.target = true;
            manager.animateItems.Add(barrier);
            IsAnimating = true;
            delivered = false;
            StartCoroutine(Animate());
            return true;
        }

        static int Reserved(TargetCounter target) => pending.TryGetValue(target, out int count) ? count : 0;

        IEnumerator Deliver()
        {
            const float emergenceDuration = .12f;
            const float flightDuration = .6f;
            var mouth = body.bounds.center + Vector3.up * body.bounds.size.y * .2f;
            float width = frames[0].bounds.size.x * Mathf.Abs(transform.lossyScale.x);
            scroll = new GameObject("CollectibleScroll");
            var visual = scroll.AddComponent<SpriteRenderer>();
            visual.sprite = scrollFrames[0];
            visual.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            visual.sortingOrder = PowerUpEffectOrder.Value + 10;
            float size = width * .65f / scrollFrames[0].bounds.size.x;
            scroll.transform.position = mouth;
            scroll.transform.localScale = Vector3.one * size;
            PlayOpeningGlow(mouth,width);
            for (float elapsed=0; elapsed<emergenceDuration; elapsed+=Time.deltaTime * Mathf.Max(.25f,PlaybackSpeed))
            {
                float t=elapsed/emergenceDuration;
                scroll.transform.position=mouth+Vector3.up*width*.65f*Mathf.SmoothStep(0,1,t);
                yield return null;
            }
            var gui = FindObjectsByType<TargetGUI>(FindObjectsSortMode.None).FirstOrDefault(g => g.subTargetContainer == deliveryTarget && g.image != null);
            var start=scroll.transform.position;
            overlay=MailDeliveryOverlay.Create(visual,gui);
            for (float elapsed=0; elapsed<flightDuration; elapsed+=Time.deltaTime*Mathf.Max(.25f,PlaybackSpeed))
            {
                float t=Mathf.Clamp01(elapsed/flightDuration);
                Vector3 end=Destination(gui,start+Vector3.up*width*3);
                Vector3 control=(start+end)*.5f + Vector3.up*width*2 + Vector3.right*(start.x>0?-1:1)*width;
                scroll.transform.position=(1-t)*(1-t)*start+2*(1-t)*t*control+t*t*end;
                visual.sprite=scrollFrames[Mathf.Min(15,Mathf.FloorToInt(t/.36f*16))];
                scroll.transform.rotation=Quaternion.Euler(0,0,360*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.9f,t)));
                // Cropped frames have different widths: preserve the closed roll's
                // world width with uniform scaling, allowing the paper to grow vertically.
                scroll.transform.localScale=Vector3.one*(width*.65f/visual.sprite.bounds.size.x);
                overlay?.Follow();
                yield return null;
            }
            scroll.transform.position=Destination(gui,start+Vector3.up*width*3);
            overlay?.Follow();
            if (manager != null && manager.levelData == data && deliveryTarget.count > 0) deliveryTarget.changeCount(-1);
            delivered=true;
        }

        void PlayOpeningGlow(Vector3 mouth,float width)
        {
            var prefab=Resources.Load<GameObject>("Particles/Casual/Prefabs/Hyper Casual FX/HCFX_Flash_01");
            if (prefab == null) return;
            var glowTexture=Resources.Load<Texture2D>("Effects/BagOpeningGlow");
            if (glowTexture == null || body.sharedMaterial == null) return;
            // The imported FX prefab references a missing material. Use the same
            // supported sprite shader as the bag, with the existing soft glow texture.
            openingGlowMaterial=new Material(body.sharedMaterial);
            openingGlowMaterial.name="BagOpeningGlowRuntime";
            openingGlowMaterial.SetTexture("_MainTex",glowTexture);
            openingGlow=Instantiate(prefab,mouth,Quaternion.identity);
            openingGlow.name="BagOpeningSubtleGlow";
            // Keep the existing prefab's textures and fade curves, with only one
            // small warm flash per layer, fixed to the mouth instead of the scroll.
            foreach(var particles in openingGlow.GetComponentsInChildren<ParticleSystem>(true))
            {
                particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=particles.main;
                main.loop=false;main.playOnAwake=false;main.startDelay=0;
                main.startLifetime=.28f;main.startSpeed=0;
                main.startSize=width*.6f;
                main.startColor=new Color(1f,.88f,.6f,.5f);
                main.simulationSpeed=Mathf.Max(.25f,PlaybackSpeed);
                main.maxParticles=2;
                var shape=particles.shape;shape.enabled=false;
                var emission=particles.emission;
                emission.enabled=true;emission.rateOverTime=0;emission.rateOverDistance=0;
                emission.SetBursts(new[]{new ParticleSystem.Burst(0,1)});
                var sheet=particles.textureSheetAnimation;sheet.enabled=false;
                var renderer=particles.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode=ParticleSystemRenderMode.Billboard;
                renderer.sharedMaterials=new[]{openingGlowMaterial};
                renderer.sortingLayerName=PowerUpEffectOrder.SortingLayer;
                renderer.sortingOrder=PowerUpEffectOrder.Value+9;
            }
            foreach(var particles in openingGlow.GetComponentsInChildren<ParticleSystem>(true)) particles.Play(false);
            Destroy(openingGlow,.4f/Mathf.Max(.25f,PlaybackSpeed));
        }

        static Vector3 Destination(TargetGUI gui, Vector3 fallback)
        {
            if (gui == null || Camera.main == null) return fallback;
            var canvas=gui.GetComponentInParent<Canvas>();
            Vector3 world=gui.image.rectTransform.TransformPoint(gui.image.rectTransform.rect.center);
            if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
            {
                Vector2 screen=RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,world);
                world=Camera.main.ScreenToWorldPoint(new Vector3(screen.x,screen.y,Mathf.Abs(Camera.main.transform.position.z-fallback.z)));
            }
            world.z=fallback.z;
            return world;
        }

        void Release()
        {
            overlay?.Complete(); overlay=null;
            if(scroll != null) Destroy(scroll);
            if(openingGlow != null) Destroy(openingGlow);
            if(openingGlowMaterial != null) Destroy(openingGlowMaterial);
            if(deliveryTarget != null) {int left=Reserved(deliveryTarget)-1;if(left>0)pending[deliveryTarget]=left;else pending.Remove(deliveryTarget);deliveryTarget=null;}
            if(manager != null && barrier != null) manager.animateItems.Remove(barrier);
            if(barrier != null) Destroy(barrier);
            barrier=null;
            IsAnimating=false;
        }

        IEnumerator Animate()
        {
            const float durationScale = .9f / 1.73f;
            // Anchor positions are encoded in each sprite pivot; frame geometry
            // can change without shifting the pouch's lower edge.
            for (int i=0;i<frames.Length;i++)
            {
                body.sprite=frames[i];
                if (i==9) StartCoroutine(Deliver());
                yield return new WaitForSeconds(durationScale/(framesPerSecond*Mathf.Max(.25f,PlaybackSpeed)));
            }
            yield return new WaitForSeconds(.18f*durationScale/Mathf.Max(.25f,PlaybackSpeed));
            for (int i=frames.Length-2;i>=0;i--)
            {
                body.sprite=frames[i];
                yield return new WaitForSeconds(durationScale/(framesPerSecond*Mathf.Max(.25f,PlaybackSpeed)));
            }
            while (!delivered) yield return null;
            Release();
            if(manager != null && manager.levelData == data) manager.CheckWinLose();
        }

        void OnDisable() {StopAllCoroutines();Release(); if(body != null && frames.Length>0) body.sprite=frames[0];}
    }
}

namespace SweetSugar.Scripts.TargetScripts
{
    // Serialized ID retained for compatibility; editor display name: Pergaminhos.
    public sealed class BagOpenings : SolidBlock
    {
        public override void InitTarget(SweetSugar.Scripts.Level.LevelData data) { }
        public override void FulfillTarget<T>(T[] items) { }
        public override int GetDestinationCount() => SweetSugar.Scripts.Core.LevelManager.THIS?.levelData?.TargetCounters?
            .Where(t=>t.targetLevel.name=="BagOpenings").Sum(t=>t.count) ?? 0;
        public override int GetDestinationCountSublevel() => GetDestinationCount();
    }
}
