using UnityEngine;

namespace RoyalAves.UI.RoyalPostman2026
{
    // The painted kit uses 1080 logical pixels, independently of the legacy canvas reference size.
    public sealed class RoyalPostmanViewport : MonoBehaviour
    {
        // RenderTexture previews supply their own device coordinates, never the host monitor's safe area.
        public Rect? SafeAreaOverride { get; set; }

        void OnEnable()=>Fit();
        void LateUpdate()=>Fit();
        public void Fit()
        {
            // Shared painted windows own their proportional panel and full-screen input shield.
            // The RuntimeKit/editor must use the same fitter as the live lobby.
            var window=GetComponent<RoyalPostmanSettingsFit>();
            if(window!=null){window.SafeAreaOverride=SafeAreaOverride;window.Fit();return;}
            var rect=(RectTransform)transform;var parent=rect.parent as RectTransform;
            if(parent==null||parent.rect.width<=0)return;
            if(GetComponent<RoyalPostmanPetsScreen>()!=null)
            {
                FitPets(rect,parent);
                return;
            }
            float scale=parent.rect.width/1080f;
            rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
            rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(1080,parent.rect.height/scale);rect.localScale=Vector3.one*scale;
        }

        void FitPets(RectTransform rect,RectTransform parent)
        {
            var safe=parent.rect;
            var canvas=GetComponentInParent<Canvas>();
            var camera=canvas!=null&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
            Rect? pixels=SafeAreaOverride;
            if(!pixels.HasValue&&(camera==null||camera.targetTexture==null))pixels=Screen.safeArea;
            if(pixels.HasValue&&pixels.Value.width>0&&pixels.Value.height>0)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,pixels.Value.min,camera,out var min);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,pixels.Value.max,camera,out var max);
                var intersection=Rect.MinMaxRect(Mathf.Max(safe.xMin,min.x),Mathf.Max(safe.yMin,min.y),
                    Mathf.Min(safe.xMax,max.x),Mathf.Min(safe.yMax,max.y));
                if(intersection.width>0&&intersection.height>0)safe=intersection;
            }

            // Keep all authored artwork proportional. Short displays fit the complete design;
            // taller displays give their extra height to the inventory's authored anchors.
            float scale=Mathf.Min(safe.width/1080f,safe.height/1920f);
            if(scale<=0)return;
            rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
            rect.anchoredPosition=safe.center-parent.rect.center;
            bool layered=GetComponent<RoyalPostmanPetsScreen>().layeredIncubatorLayout;
            bool hatchery=GetComponent<RoyalPostmanPetsScreen>().hatchery!=null;
            rect.sizeDelta=new Vector2(1080,layered&&!hatchery?1920:safe.height/scale);
            rect.localScale=Vector3.one*scale;

            // Paint continues beneath the notch and home indicator; only controls use the safe rectangle.
            Vector2 fullMin=rect.InverseTransformPoint(parent.TransformPoint(parent.rect.min));
            Vector2 fullMax=rect.InverseTransformPoint(parent.TransformPoint(parent.rect.max));
            var background=transform.Find("Background") as RectTransform;
            if(background!=null)
            {
                background.offsetMin=fullMin-rect.rect.min;
                background.offsetMax=fullMax-rect.rect.max;
            }
            var header=transform.Find("Header") as RectTransform;
            var headerPaint=header!=null?header.Find("Background") as RectTransform:null;
            if(headerPaint!=null)
            {
                float inset=Mathf.Max(0,fullMax.y-rect.rect.yMax);
                headerPaint.sizeDelta=new Vector2(fullMax.x-fullMin.x,header.rect.height+inset);
                headerPaint.anchoredPosition=new Vector2((fullMin.x+fullMax.x)*.5f,inset*.5f);
            }
            if(layered)
            {
                var room=transform.Find("Room") as RectTransform;
                if(room!=null){room.offsetMin=fullMin-rect.rect.min;room.offsetMax=fullMax-rect.rect.max;}
                return;
            }
            if(GetComponent<RoyalPostmanPetsScreen>().cartoonLayout)
            {
                var rail=header!=null?header.Find("GoldRail") as RectTransform:null;
                if(rail!=null){rail.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,fullMax.x-fullMin.x);rail.anchoredPosition=new Vector2((fullMin.x+fullMax.x)*.5f,rail.anchoredPosition.y);}
                var shelf=transform.Find("Inventory/Background") as RectTransform;
                if(shelf!=null){shelf.offsetMin=new Vector2(fullMin.x-rect.rect.xMin,fullMin.y-rect.rect.yMin);shelf.offsetMax=new Vector2(fullMax.x-rect.rect.xMax,0);}
                if(GetComponent<RoyalPostmanPetsScreen>().integratedMachineLayout)
                {
                    var housing=transform.Find("Body/Workshop/ControlPanel") as RectTransform;
                    if(housing!=null){housing.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,fullMax.x-fullMin.x+2);housing.anchoredPosition=new Vector2((fullMin.x+fullMax.x)*.5f,housing.anchoredPosition.y);}
                }
                var room=transform.Find("Room") as RectTransform;
                if(room!=null){room.offsetMin=new Vector2(fullMin.x-rect.rect.xMin,room.offsetMin.y);room.offsetMax=new Vector2(fullMax.x-rect.rect.xMax,room.offsetMax.y);room.GetComponent<RoyalPostmanCartoonBackdrop>()?.Fit();}
            }
        }
    }
}
