using UnityEngine;
using UnityEngine.UI;
using RoyalAves.Meta;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>Presentation only: the hatchery remains the authority for inventory, time and rewards.</summary>
    [ExecuteAlways,DisallowMultipleComponent]
    public sealed class RoyalPostmanIncubatorPresentation : MonoBehaviour
    {
        public RoyalPostmanHatchery hatchery;
        public Button drawerHandle,drawerClose,drawerBackdrop;
        public RectTransform drawerSheet;
        public GameObject drawerRoot,incubateGlow,lamps;
        public Text drawerTitle,handleLabel;
        public Button[] pages;
        public RoyalPostmanGlyph[] dots;
        public RoyalPostmanBevelPanel[] cards;
        public Image primaryFace,primaryIcon;
        public Material greenPaint,redPaint;
        public Sprite paw,cross;
        public RoyalPostmanRadialSparkles actionParticles;
        public bool DrawerOpen {get;private set;}
        bool wired;
        float slide;
        public void Initialize()
        {
            if(wired||drawerHandle==null)return;wired=true;
            drawerHandle.onClick.AddListener(OpenDrawer);drawerClose.onClick.AddListener(CloseDrawer);drawerBackdrop.onClick.AddListener(CloseDrawer);
            for(int i=0;i<pages.Length;i++){int page=i;pages[i].onClick.AddListener(()=>hatchery.SelectSpace(page));}
            CloseDrawer();
        }
        void OnDisable(){CloseDrawer();}
        void Update()
        {
            if(drawerRoot==null)return;
            float target=DrawerOpen?1:0;
            slide=Application.isPlaying?Mathf.MoveTowards(slide,target,Time.unscaledDeltaTime*6):target;
            if(drawerSheet!=null)drawerSheet.anchoredPosition=new Vector2(0,Mathf.Lerp(-drawerSheet.rect.height,0,1-Mathf.Pow(1-slide,3)));
            drawerRoot.SetActive(DrawerOpen||slide>0);
            if(actionParticles!=null&&actionParticles.gameObject.activeInHierarchy)actionParticles.Sample((float)Time.realtimeSinceStartupAsDouble,.65f);
        }
        public void Tick(){Update();}
        public void OpenDrawer(){Initialize();DrawerOpen=true;if(drawerRoot!=null){drawerRoot.SetActive(true);drawerRoot.transform.SetAsLastSibling();}hatchery.Refresh();if(!Application.isPlaying)Tick();}
        public void CloseDrawer(){DrawerOpen=false;slide=0;if(drawerRoot!=null)drawerRoot.SetActive(false);}
        public void Refresh(bool pt,bool running,bool locked,bool hasEgg,int kind,int level,PetsIncubationState state)
        {
            Initialize();
            drawerTitle.text=pt?"Escolha um ovo":"Choose an egg";handleLabel.text=pt?"Ovos":"Eggs";
            hatchery.spaceLabel.gameObject.SetActive(locked||hatchery.SelectedSpace==3);
            bool cancel=running&&!locked;
            if(cancel){hatchery.actionLabel.text=pt?"Cancelar":"Cancel";hatchery.incubate.interactable=true;}
            primaryFace.material=cancel?redPaint:greenPaint;primaryIcon.sprite=cancel?cross:paw;
            primaryFace.color=hatchery.incubate.interactable?Color.white:new Color(.65f,.65f,.7f);
            incubateGlow.SetActive(!cancel&&hatchery.incubate.interactable);lamps.SetActive(running&&!locked);
            hatchery.accelerate.gameObject.SetActive(running&&!locked);if(hatchery.cancel!=null)hatchery.cancel.gameObject.SetActive(false);
            for(int i=0;i<3;i++)
            {
                bool selected=i==hatchery.SelectedSpace,unlocked=level>=hatchery.unlockLevels[i]||(state!=null&&state.Occupied(i));
                dots[i].color=selected?new Color(1,.84f,.19f):unlocked?new Color(.16f,.76f,1):new Color(.13f,.24f,.39f);
                dots[i].rectTransform.localScale=Vector3.one*(selected?1.15f:1);dots[i].SetVerticesDirty();
            }
            for(int i=0;i<4;i++)
            {
                cards[i].rim=hasEgg&&kind==i?new Color(1,.85f,.15f):new Color(.025f,.40f,.86f);cards[i].bevel=hasEgg&&kind==i?9:5;cards[i].SetVerticesDirty();
                var button=cards[i].GetComponentInParent<Button>(true);button.interactable=!locked&&!running&&(state?.EggCount(i)??0)>0;
            }
            if(!Application.isPlaying)Tick();
        }
    }
}
