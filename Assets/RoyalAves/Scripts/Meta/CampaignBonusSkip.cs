using SweetSugar.Scripts.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    // The real bonus still calculates every point; skipping hides and fast-forwards its presentation.
    public sealed class CampaignBonusSkip : MonoBehaviour
    {
        public const float RevealDelay=3f;
        public const float FinishSpeed=16f;
        LevelManager owner;
        SweetSugar.Scripts.Level.LevelData data;
        Button button;
        GameObject curtain;
        RectTransform safe;
        Canvas overlay;
        AudioSource effectsAudio;
        bool previousMute;
        float began, previousScale;
        bool completed, restored;
        public bool Requested { get; private set; }
        public Button SkipButton => button;
        public bool ButtonVisible => button!=null && button.gameObject.activeInHierarchy;

        public static CampaignBonusSkip Begin(LevelManager manager)
        {
            var existing=FindFirstObjectByType<CampaignBonusSkip>();
            if(existing!=null)return existing;
            var root=new GameObject("Pular bônus final",typeof(RectTransform),typeof(Canvas),
                typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CampaignBonusSkip));
            var view=root.GetComponent<CampaignBonusSkip>();
            view.owner=manager;view.data=manager.levelData;view.began=Time.unscaledTime;
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32020;
            view.overlay=canvas;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=.5f;
            var shield=Image(root.transform,"Bloqueio de entrada",Color.clear);
            Stretch(shield.rectTransform);shield.raycastTarget=true;
            var cover=Image(root.transform,"Finalização",new Color(.025f,.045f,.1f,1));
            Stretch(cover.rectTransform);cover.raycastTarget=true;view.curtain=cover.gameObject;
            var art=LobbyFeaturesConfig.Instance;
            var label=Label(cover.transform,"Finalizando bônus...",art);
            label.rectTransform.sizeDelta=new Vector2(900,180);label.fontSize=62;label.enableAutoSizing=true;
            label.fontSizeMin=28;label.fontSizeMax=62;
            view.curtain.SetActive(false);
            view.safe=new GameObject("Área segura",typeof(RectTransform)).GetComponent<RectTransform>();
            view.safe.SetParent(root.transform,false);view.FitSafeArea();
            var face=Image(view.safe,"Pular",Color.white);
            face.sprite=art!=null?(art.bonusSkipButton!=null?art.bonusSkipButton:art.buttonBlue):null;
            face.type=UnityEngine.UI.Image.Type.Sliced;face.raycastTarget=true;
            var rect=face.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,0);
            rect.anchoredPosition=new Vector2(-32,96);
            Canvas.ForceUpdateCanvases();
            rect.sizeDelta=new Vector2(360,Mathf.Max(132,44f/Mathf.Max(.01f,canvas.scaleFactor)));
            view.button=face.gameObject.AddComponent<Button>();view.button.targetGraphic=face;
            view.button.navigation=new Navigation{mode=Navigation.Mode.None};
            view.button.onClick.AddListener(view.RequestSkip);
            var title=Label(face.transform,"Pular",art);Stretch(title.rectTransform);
            title.fontSize=56;title.enableAutoSizing=true;title.fontSizeMin=28;title.fontSizeMax=56;
            view.button.gameObject.SetActive(false);
            return view;
        }

        public void RequestSkip()
        {
            if(completed || Requested || owner==null || owner.levelData!=data ||
                owner.gameStatus!=GameState.PreWinAnimations || Time.unscaledTime-began<RevealDelay)return;
            Requested=true;previousScale=Time.timeScale;
            button.interactable=false;button.gameObject.SetActive(false);curtain.SetActive(true);
            effectsAudio=SweetSugar.Scripts.SoundBase.Instance!=null?SweetSugar.Scripts.SoundBase.Instance.GetComponent<AudioSource>():null;
            if(effectsAudio!=null){previousMute=effectsAudio.mute;effectsAudio.Stop();effectsAudio.mute=true;}
            Time.timeScale=FinishSpeed;
        }
        void Update()
        {
            if(owner==null || owner.levelData!=data || owner.gameStatus!=GameState.PreWinAnimations){Complete();return;}
            FitSafeArea();
            if(button!=null && overlay!=null)
                ((RectTransform)button.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                    Mathf.Max(132,44f/Mathf.Max(.01f,overlay.scaleFactor)));
            if(!Requested && Time.unscaledTime-began>=RevealDelay)button.gameObject.SetActive(true);
        }
        void FitSafeArea()
        {
            if(safe==null || Screen.width==0 || Screen.height==0)return;
            var area=Screen.safeArea;
            safe.anchorMin=new Vector2(area.xMin/Screen.width,area.yMin/Screen.height);
            safe.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);
            safe.offsetMin=safe.offsetMax=Vector2.zero;
        }
        void Restore()
        {
            if(restored)return;restored=true;
            if(Requested)Time.timeScale=previousScale;
            if(effectsAudio!=null){effectsAudio.Stop();effectsAudio.mute=previousMute;}
        }
        public void Complete()
        {
            if(completed)return;completed=true;Restore();
            if(button!=null)button.interactable=false;
            gameObject.SetActive(false);Destroy(gameObject);
        }
        void OnDestroy()=>Restore();
        static void Stretch(RectTransform rect)
        {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        static Image Image(Transform parent,string name,Color color)
        {
            var node=new GameObject(name,typeof(RectTransform),typeof(Image));node.transform.SetParent(parent,false);
            var image=node.GetComponent<Image>();image.color=color;image.raycastTarget=false;return image;
        }
        static TextMeshProUGUI Label(Transform parent,string text,LobbyFeaturesConfig art)
        {
            var node=new GameObject(text,typeof(RectTransform),typeof(TextMeshProUGUI));node.transform.SetParent(parent,false);
            var label=node.GetComponent<TextMeshProUGUI>();label.text=text;label.color=new Color(1,.97f,.87f);
            label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
            if(art!=null && art.displayFont!=null)label.font=art.displayFont;
            return label;
        }
    }
}
