// // ©2015 - Present Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

using System;
using System.Collections;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items._Interfaces;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using RoyalAves.Effects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SweetSugar.Scripts.System
{
    /// <summary>
    /// Blocks and ingredients animation, fly up to GUI
    /// </summary>
    public class AnimateItems : MonoBehaviour
    {
        public GameObject linkObject;
        public int linkObjectHash;
        public bool target;
        LevelManager owner;
        MailDeliveryOverlay overlay;
        public bool IsCollectionFlight { get; private set; }
        public float FlightProgress { get; private set; }
        public void InitAnimation(GameObject obj, Vector2 pos, Vector2 scale, Action callBack, Sprite sprite=null, TargetGUI destination=null)
        {
            owner=LevelManager.THIS;
            var colorableComponent = obj.GetComponent<IColorableComponent>();
            SpriteRenderer spr = null;
            if(!sprite)
            {
                if (!colorableComponent) spr = obj.GetComponent<SpriteRenderer>();
                else spr = obj.GetComponent<IColorableComponent>().directSpriteRenderer;
                if (spr == null) spr = obj.GetComponentInChildren<SpriteRenderer>(); 
                sprite = spr.sprite;
            }
            var sprRenderer = gameObject.AddComponent<SpriteRenderer>();
            sprRenderer.sprite = sprite;
            sprRenderer.sortingLayerName = "UI";
            sprRenderer.sortingOrder = 10;
            gameObject.transform.position = target && spr!=null ? spr.transform.position : obj.transform.position;
            gameObject.transform.localScale = target && spr!=null ? spr.transform.lossyScale : (Vector3)scale;
            if(target)
            {
                IsCollectionFlight=true;
                if(spr!=null) { sprRenderer.flipX=spr.flipX;sprRenderer.flipY=spr.flipY; }
                overlay=MailDeliveryOverlay.Create(sprRenderer,destination,false);
                StartCoroutine(FlyToGoal(pos,callBack,destination));
                return;
            }
            StartCoroutine(StartAnimateIngredient(gameObject, pos, () => { if (callBack != null) callBack(); }));
        }

        static Vector3 GoalWorldPosition(TargetGUI gui,Camera camera,Vector3 start,Vector3 fallback)
        {
            if(gui==null || gui.image==null || camera==null)return fallback;
            var canvas=gui.GetComponentInParent<Canvas>();
            if(canvas==null)return fallback;
            var rect=gui.image.rectTransform;
            var screen=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,
                rect.TransformPoint(rect.rect.center));
            return camera.ScreenToWorldPoint(new Vector3(screen.x,screen.y,camera.WorldToScreenPoint(start).z));
        }

        IEnumerator FlyToGoal(Vector3 fallback,Action callback,TargetGUI destination)
        {
            var data=owner!=null?owner.levelData:null;
            var camera=owner!=null?owner.GetComponent<Camera>():Camera.main;
            var start=transform.position;
            var originalScale=transform.localScale;
            float cell=owner?.field!=null?owner.field.squareWidth:1;
            float seed=gameObject.GetHashCode()*2.399963f;
            float side=Mathf.Sin(seed);
            var kick=new Vector3(side*.28f,-.12f+Mathf.Cos(seed)*.09f,0)*cell;
            var end=GoalWorldPosition(destination,camera,start,fallback);
            float screenDistance=camera!=null ? Vector2.Distance(camera.WorldToViewportPoint(start),camera.WorldToViewportPoint(end)) : .6f;
            float travelDuration=Mathf.Lerp(.38f,.58f,Mathf.Clamp01(screenDistance));
            float duration=.1f+travelDuration;
            for(float age=0;age<duration;age+=Time.deltaTime)
            {
                if(owner==null || owner.levelData!=data) {Destroy(gameObject);yield break;}
                FlightProgress=Mathf.Clamp01(age/duration);
                end=GoalWorldPosition(destination,camera,start,fallback);
                float turn;
                if(age<.1f)
                {
                    float t=age/.1f;
                    transform.position=start+kick*Mathf.SmoothStep(0,1,t);
                    transform.localScale=originalScale*Mathf.Lerp(1,1.12f,Mathf.Sin(t*Mathf.PI*.5f));
                    turn=side*14*t;
                }
                else
                {
                    float t=Mathf.Clamp01((age-.1f)/travelDuration),p=t*t,u=1-p;
                    var from=start+kick;
                    var control1=from+new Vector3(side*.4f,.4f,0)*cell;
                    var control2=end+Vector3.down*Mathf.Min(Vector3.Distance(from,end)*.25f,cell*1.2f)+Vector3.right*(side*.2f*cell);
                    transform.position=u*u*u*from+3*u*u*p*control1+3*u*p*p*control2+p*p*p*end;
                    transform.localScale=originalScale*Mathf.Lerp(1.12f,.42f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1,t)));
                    turn=side*14*(1-t)+side*8*Mathf.Sin(t*Mathf.PI);
                }
                transform.rotation=Quaternion.Euler(0,0,turn);
                if(overlay!=null)overlay.Follow();
                yield return null;
            }
            transform.position=end;FlightProgress=1;
            if(overlay!=null)overlay.Follow();
            // Remove the resolution barrier before the arrival callback checks victory.
            if(owner!=null)owner.animateItems.Remove(this);
            if(owner!=null && owner.levelData==data)
            {
                callback?.Invoke();
                if(destination!=null)destination.PlayCollectionArrival();
            }
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if(overlay!=null)overlay.Complete();
            if(owner!=null)owner.animateItems.Remove(this);
        }

        IEnumerator StartAnimateIngredient(GameObject item, Vector2 pos, Action callBack)
        {
            var curveX = new AnimationCurve(new Keyframe(0, item.transform.localPosition.x), new Keyframe(0.4f, pos.x));
            var curveY = new AnimationCurve(new Keyframe(0, item.transform.localPosition.y), new Keyframe(0.5f, pos.y));
            curveY.AddKey(0.2f, item.transform.localPosition.y + Random.Range(-2, 0.5f));
            var startTime = Time.time;
            var speed = Random.Range(0.4f, 0.6f);
            float distCovered = 0;
            var startScale = item.transform.localScale;
            if (linkObject != null && linkObject.GetComponent<Square>()?.sizeInSquares.magnitude >= 2) startScale /= 2;
            while (distCovered < 0.5f)
            {
                distCovered = (Time.time - startTime) * speed;
                item.transform.localPosition = new Vector3(curveX.Evaluate(distCovered), curveY.Evaluate(distCovered), 0);
                item.transform.Rotate(Vector3.back, Time.deltaTime * 1000);
                item.transform.localScale = Vector3.Lerp(startScale, startScale / 2f, distCovered*2);
                yield return new WaitForFixedUpdate();
            }
            //     SoundBase.Instance.audio.PlayOneShot(SoundBase.Instance.getStarIngr);
            LevelManager.Destroy(item);
            if (callBack != null)
                callBack();
            LevelManager.THIS.animateItems.Remove(this);
            Destroy(gameObject);

        }
    }
}
