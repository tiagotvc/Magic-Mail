using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RoyalAves.EditorTools;
using RoyalAves.Meta;
using RoyalAves.UI.RoyalPostman2026;

public static class AcceleratorBadgeV98Review
{
    static void Check(bool ok,string message)=>AcceleratorBadgeV98Build.Check(ok,message);
    static Color32[] Pixels(RoyalPostmanScreenDocument d,Rect area)
    {
        d.Render();int x=Mathf.FloorToInt(area.xMin),y=Mathf.FloorToInt(area.yMin),w=Mathf.CeilToInt(area.xMax)-x,h=Mathf.CeilToInt(area.yMax)-y;var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
        try{RenderTexture.active=d.Texture;texture.ReadPixels(new Rect(x,y,w,h),0,0);texture.Apply();return texture.GetPixels32();}finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(texture);}
    }
    public static void Run()
    {
        const string output=AcceleratorBadgeV98Build.Output;
        try
        {
            var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/RoyalAves/Art/UI/Button/RoyalPostman/Shaders/RoyalPostmanBadgeParts.shader");Check(shader!=null&&!ShaderUtil.ShaderHasError(shader),"Badge shader compile error");
            using(var d=new RoyalPostmanScreenDocument(RoyalPostmanRuntimeKit.Load(),"pets",720,1280))
            {
                var v=d.Root.GetComponent<RoyalPostmanPetsScreen>();v.Initialize(()=>{},i=>{});var h=v.hatchery;var badge=h.accelerate.transform.Find("ResourceBadge");Check(badge!=null&&h.separateAcceleratorStock&&h.accelerateQuantity.transform.parent==badge,"Live badge references missing");
                var gold=badge.Find("GoldBorder").GetComponent<Image>();var red=badge.Find("RedFace").GetComponent<Image>();Check(gold.sprite==red.sprite&&gold.rectTransform.anchoredPosition==red.rectTransform.anchoredPosition&&gold.rectTransform.sizeDelta==red.rectTransform.sizeDelta,"Gold/red paint not aligned");
                Check(gold.material.GetFloat("_Part")==1&&red.material.GetFloat("_Part")==2,"Layers not independently selectable");
                Check(!h.accelerate.transform.Find("Quantity").gameObject.activeSelf&&!h.accelerate.transform.Find("Image").gameObject.activeSelf,"Detached resource remains visible");
                int saves=0,boosts=0,spent=0;long now=1700000000;var stock=new PetsIncubationState{legendaryEggs=1,accelerationItems=new[]{2,0,0,0}};
                v.BindInventory(stock,()=>saves++,n=>{spent+=n;return true;},()=>10000,()=>now);v.RefreshAvailability(100);v.ShowCurrent(false);v.AccelerateRequested+=i=>boosts++;
                Check(h.accelerateQuantity.text=="x2"&&!h.accelerateCost.gameObject.activeSelf,"Idle stock/cost state incorrect");v.items[3].button.onClick.Invoke();h.incubate.onClick.Invoke();h.accelerate.onClick.Invoke();Check(h.accelerateQuantity.text=="x1"&&spent==0&&saves==2,"Badge item debit/save mismatch");
                h.accelerate.onClick.Invoke();Check(h.accelerateQuantity.text=="x0"&&h.accelerateCost.gameObject.activeSelf&&h.accelerateCost.text.Contains("500")&&boosts==2&&saves==3,"Badge hides zero count or first gold price");h.accelerate.onClick.Invoke();Check(spent==500&&h.accelerateCost.text.Contains("1000")&&saves==4,"Gold pricing pipeline changed");
                v.BindInventory(stock,()=>saves++,n=>false,()=>0,()=>now);string rejected=JsonUtility.ToJson(stock);h.accelerate.onClick.Invoke();Check(JsonUtility.ToJson(stock)==rejected&&saves==4&&!h.accelerate.interactable,"Rejected gold action mutates stock");
                h.headerTitle.editorLanguage=RoyalPostmanHeaderTitle.EditorLanguage.English;h.Refresh();Check(h.accelerateCost.text=="1000 gold"&&h.accelerateQuantity.text=="x0","English price/count mismatch");h.headerTitle.editorLanguage=RoyalPostmanHeaderTitle.EditorLanguage.Portuguese;h.Refresh();Check(h.accelerateCost.text=="1000 ouro","Portuguese price mismatch");
                d.Render();var area=d.Bounds((RectTransform)badge);var split=Pixels(d,area);var original=gold.material;var whole=new Material(original);whole.SetFloat("_Part",0);
                try{gold.material=whole;red.gameObject.SetActive(false);var combined=Pixels(d,area);int changed=0;for(int i=0;i<split.Length;i++){int delta=Math.Max(Math.Abs(split[i].r-combined[i].r),Math.Max(Math.Abs(split[i].g-combined[i].g),Math.Abs(split[i].b-combined[i].b)));if(delta>3)changed++;}Check(changed<split.Length*.01f,"Split materials do not reproduce the original painted badge");}finally{gold.material=original;red.gameObject.SetActive(true);UnityEngine.Object.DestroyImmediate(whole);}
                var testStock=new PetsIncubationState{commonEggs=1,accelerationItems=new[]{1,0,0,0}};v.BindInventory(testStock,()=>saves++,n=>false,()=>0,()=>now);h.SelectSpace(1);h.SelectEgg(0);v.RefreshAvailability(100);h.incubate.onClick.Invoke();d.Render();
                var events=new GameObject("BadgeV98Events",typeof(EventSystem));
                try
                {
                    var point=d.Camera.WorldToScreenPoint(badge.position);var data=new PointerEventData(events.GetComponent<EventSystem>()){button=PointerEventData.InputButton.Left,position=point};var hits=new System.Collections.Generic.List<RaycastResult>();d.Root.GetComponentInParent<GraphicRaycaster>().Raycast(data,hits);Check(hits.Count>0&&hits[0].gameObject==badge.gameObject,"Badge attachment cannot be clicked");
                    var press=h.accelerate.GetComponent<RoyalPostmanPaintedButton>();var center=press.movingCenter.anchoredPosition;var badgeRest=badge.position;var rim=h.accelerate.transform.Find("FixedGold");var rimRest=rim.position;
                    ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerDownHandler);Check(press.movingCenter.anchoredPosition!=center&&badge.position==badgeRest&&rim.position==rimRest,"Badge/fixed rim drift while pressing");ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerUpHandler);Check(press.movingCenter.anchoredPosition==center,"Button does not release");
                    int before=saves;ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);Check(testStock.accelerationItems[0]==0&&saves==before+1&&h.accelerateQuantity.text=="x0","Badge click does not accelerate exactly once");
                }
                finally{UnityEngine.Object.DestroyImmediate(events);}
                foreach(var size in new[]{new Vector2Int(360,640),new Vector2Int(390,844),new Vector2Int(720,1280)})
                {
                    d.Resize(size.x,size.y);d.Root.GetComponent<RoyalPostmanViewport>().SafeAreaOverride=new Rect(0,24,size.x,size.y-48);d.Render();var b=d.Bounds((RectTransform)badge);Check(b.xMin>=0&&b.xMax<=size.x&&b.yMin>=24&&b.yMax<=size.y-24,"Badge outside safe area");
                    foreach(var arrow in h.accelerate.transform.Find("PaintedCenter/FastForwardIcon").GetComponentsInChildren<Image>())Check(!b.Overlaps(d.Bounds(arrow.rectTransform)),"Badge covers user's thick arrows");
                    Check(!b.Overlaps(d.Bounds(h.accelerateLabel.rectTransform)),"Badge covers Acelerar caption");Check(d.Bounds(h.accelerateCost.rectTransform).yMax<d.Bounds(h.accelerateLabel.rectTransform).yMin,"Gold cost overlaps label");
                }
                testStock.accelerationItems[0]=int.MaxValue;h.Refresh();d.Render();Check(h.accelerateQuantity.text=="x2147483647"&&!h.accelerateCost.gameObject.activeSelf,"Large count lost or stale gold cost");
            }
            File.WriteAllText(output+"/review.txt","PASS: native shader/compile/render; original paint reproduced by complementary gold/red layers (<1% pixels differ>3); independent hourglass/live count; old detached icon/count hidden; stock/item/gold/rejected payment/save-once and PT/EN; attached badge click routes to accelerator and fixed badge/rim survive press; arrows/caption clear, safe360/390/720; large count retained.\n");EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(output+"/review.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
