using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta.WorldMap
{
    public sealed class WorldMapSantaBalloon
    {
        readonly RectTransform root;
        readonly TMP_Text label;
        float until;
        public bool Visible=>root!=null&&root.gameObject.activeSelf;
        public WorldMapSantaBalloon(RectTransform parent,LobbyFeaturesConfig art)
        {
            root=Make(parent,"BalaoEventoNoel",new Vector2(390,145));root.anchorMin=root.anchorMax=Vector2.zero;
            var border=root.gameObject.AddComponent<Image>();border.sprite=art!=null?art.pill:null;border.type=Image.Type.Sliced;border.color=new Color(.34f,.08f,.48f);
            var inside=Make(root,"Papel",Vector2.zero);inside.anchorMin=Vector2.zero;inside.anchorMax=Vector2.one;inside.offsetMin=new Vector2(4,4);inside.offsetMax=new Vector2(-4,-4);
            var paper=inside.gameObject.AddComponent<Image>();paper.sprite=border.sprite;paper.type=Image.Type.Sliced;paper.color=new Color(1,.96f,.83f);paper.raycastTarget=false;
            var tail=Make(root,"Ponta",new Vector2(25,23));tail.anchorMin=tail.anchorMax=new Vector2(.5f,0);tail.anchoredPosition=new Vector2(0,-15);
            tail.gameObject.AddComponent<SantaSpeechTail>().color=new Color(.34f,.08f,.48f);
            var text=Make(root,"Texto",Vector2.zero);text.anchorMin=Vector2.zero;text.anchorMax=Vector2.one;text.offsetMin=new Vector2(24,15);text.offsetMax=new Vector2(-24,-15);
            label=text.gameObject.AddComponent<TextMeshProUGUI>();label.font=art!=null?art.displayFont:null;
            label.text="";label.fontSize=29;label.enableAutoSizing=true;label.fontSizeMin=23;label.fontSizeMax=29;
            label.color=new Color(.30f,.08f,.40f);label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
            var button=root.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;button.targetGraphic=border;
            button.onClick.AddListener(Hide);root.gameObject.SetActive(false);
        }
        static RectTransform Make(Transform parent,string name,Vector2 size)
        {
            var r=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;r.gameObject.layer=parent.gameObject.layer;
            r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=size;return r;
        }
        public void Show(string text,Vector2 anchor,float now)
        {
            label.text=text;var parent=(RectTransform)root.parent;var size=parent.rect.size;
            root.anchoredPosition=new Vector2(Mathf.Clamp(anchor.x*size.x,210,size.x-210),Mathf.Clamp(anchor.y*size.y+110,280,size.y-310));
            root.SetAsLastSibling();root.gameObject.SetActive(true);until=now+5;
        }
        public void Tick(float now){if(Visible&&now>=until)Hide();}
        public void Hide(){if(root!=null)root.gameObject.SetActive(false);}
    }
    public sealed class SantaSpeechTail : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;vh.AddVert(new Vector2(r.xMin,r.yMax),color,Vector2.zero);
            vh.AddVert(new Vector2(r.center.x,r.yMin),color,Vector2.zero);vh.AddVert(new Vector2(r.xMax,r.yMax),color,Vector2.zero);vh.AddTriangle(0,1,2);
        }
        protected override void Awake(){base.Awake();raycastTarget=false;}
    }
}
