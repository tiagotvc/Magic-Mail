using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta.WorldMap
{
    // A permanent map shortcut and honest preview. No fake balance, price or purchase action.
    public sealed class WorldMapLetterVault
    {
        public const string ResourcePath="MapEventsV98/postal-pig-gold-cartoon";
        readonly RectTransform parent;
        readonly Sprite artwork;
        readonly LobbyFeaturesConfig art;
        readonly Action<bool> modalChanged;
        RectTransform dialog;
        public Button Button{get;}
        public bool Visible=>Button.gameObject.activeSelf;
        public bool DialogVisible=>dialog!=null&&dialog.gameObject.activeSelf;
        public WorldMapLetterVault(RectTransform parent,LobbyFeaturesConfig art,Action<bool> modalChanged)
        {
            this.parent=parent;this.art=art;this.modalChanged=modalChanged;artwork=Resources.Load<Sprite>(ResourcePath);
            var r=Rect("LetterVaultShortcut",parent);r.anchorMin=r.anchorMax=Vector2.one;r.pivot=Vector2.one;
            r.sizeDelta=new Vector2(200,220);r.anchoredPosition=new Vector2(-24,-(WorldMapTopHud.TopInset+WorldMapTopHud.Height+18));
            var hit=r.gameObject.AddComponent<Image>();hit.color=Color.clear;
            var frame=Rect("InsetPlumShield",r);frame.anchorMin=frame.anchorMax=new Vector2(.5f,0);frame.pivot=new Vector2(.5f,0);frame.anchoredPosition=Vector2.zero;frame.sizeDelta=new Vector2(200,178);
            var face=frame.gameObject.AddComponent<WorldMapLetterVaultFrame>();face.raycastTarget=false;
            var pig=Rect("SeparateCartoonPig",r);pig.anchoredPosition=new Vector2(0,24);pig.sizeDelta=new Vector2(224,224);
            var image=pig.gameObject.AddComponent<Image>();image.sprite=artwork;image.preserveAspect=true;image.raycastTarget=false;
            Button=r.gameObject.AddComponent<Button>();Button.targetGraphic=face;Button.navigation=new Navigation{mode=Navigation.Mode.None};
            var colors=Button.colors;colors.highlightedColor=Color.white;colors.pressedColor=new Color(.85f,.78f,.93f);Button.colors=colors;
            Button.onClick.AddListener(Open);SetAvailable(false);
        }
        public void SetAvailable(bool visible)
        {visible&=artwork!=null;if(Button.gameObject.activeSelf!=visible)Button.gameObject.SetActive(visible);}
        public void Open()
        {
            if(DialogVisible)return;
            if(dialog==null)BuildDialog();dialog.SetAsLastSibling();dialog.gameObject.SetActive(true);modalChanged?.Invoke(true);
        }
        public void Close()
        {if(!DialogVisible)return;dialog.gameObject.SetActive(false);modalChanged?.Invoke(false);}
        void BuildDialog()
        {
            dialog=Rect("LetterVaultPreview",parent);Stretch(dialog);
            var backdrop=Rect("Dimmer",dialog);Stretch(backdrop);
            var dim=backdrop.gameObject.AddComponent<Image>();dim.color=new Color(.08f,.025f,.14f,.78f);
            var dismiss=backdrop.gameObject.AddComponent<Button>();dismiss.targetGraphic=dim;dismiss.transition=Selectable.Transition.None;dismiss.onClick.AddListener(Close);
            var card=Rect("LetterVaultWindow",dialog);card.sizeDelta=new Vector2(900,1120);
            var face=card.gameObject.AddComponent<PostalEventGraphic>();face.style=PostalEventGraphic.Style.Card;face.raycastTarget=true;
            Label(card,"Porquinho Postal",48,42,804,100,72,new Color32(72,31,83,255));
            Label(card,"EM BREVE",80,154,740,52,34,new Color32(133,64,148,255));
            var picture=Rect("VaultArtwork",card);Top(picture,220,236,460,460);
            var image=picture.gameObject.AddComponent<Image>();image.sprite=artwork;image.preserveAspect=true;image.raycastTarget=false;
            Label(card,"Um cofre permanente para guardar\nmoedas das fases e resgatar\npor uma compra na loja.",70,720,760,170,40,new Color32(87,50,102,255));
            var action=Rect("CloseLetterVault",card);Top(action,160,962,580,108);
            var buttonFace=action.gameObject.AddComponent<PostalEventGraphic>();buttonFace.style=PostalEventGraphic.Style.GreenButton;
            var close=action.gameObject.AddComponent<Button>();close.targetGraphic=buttonFace;close.navigation=new Navigation{mode=Navigation.Mode.None};close.onClick.AddListener(Close);
            Label(action,"Entendi",20,8,540,90,49,new Color32(255,246,216,255));
        }
        TMP_Text Label(Transform parent,string value,float x,float y,float width,float height,float size,Color color)
        {
            var r=Rect("Text",parent);Top(r,x,y,width,height);var text=r.gameObject.AddComponent<TextMeshProUGUI>();
            text.font=WorldMapHudTheme.Current?.font!=null?WorldMapHudTheme.Current.font:art!=null?art.displayFont:null;
            text.text=value;text.fontSize=size;text.enableAutoSizing=true;text.fontSizeMin=size*.8f;text.fontSizeMax=size;text.color=color;
            text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.Normal;
            if(text.font!=null)text.fontSharedMaterial=text.font.material;return text;
        }
        static RectTransform Rect(string name,Transform parent)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer)){layer=parent.gameObject.layer};go.transform.SetParent(parent,false);
            var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;return r;
        }
        static void Top(RectTransform r,float x,float y,float width,float height)
        {r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    }
}
