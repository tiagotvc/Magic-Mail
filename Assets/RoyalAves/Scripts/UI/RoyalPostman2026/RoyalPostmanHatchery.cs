using System;
using SweetSugar.Scripts.Level;
using RoyalAves.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.UI.RoyalPostman2026
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class RoyalPostmanHatchery : MonoBehaviour
    {
        public Image egg,lockIcon;
        public Sprite[] eggs;
        public Button incubate,accelerate,cancel,legacy;
        public Text cancelLabel;
        public Text actionLabel,timeLabel,accelerateLabel,accelerateQuantity,spaceLabel,legacyLabel;
        public bool separateAcceleratorStock;
        public Text accelerateCost;
        public Text[] itemNames,quantities;
        public GameObject[] selections;
        public RoyalPostmanDigitalClock display;
        public RoyalPostmanIncubatorParticles particles;
        public RoyalPostmanIncubatorPresentation presentation;
        public RoyalPostmanHeaderTitle headerTitle;
        public Material selectedTabMaterial,unselectedTabMaterial;
        public PetHatchCatalog rewards;
        public int[] unlockLevels={30,50,100};
        public int previewPlayerLevel=100;
        public int selectedEgg;
        [SerializeField] int selectedSpace;
        public int SelectedSpace=>selectedSpace;
        public int SelectedEgg=>selectedEgg;
        [HideInInspector] public RoyalPostmanPetsScreen owner;
        PetsIncubationState stock;
        Action save;
        Func<int,bool> spend;
        Func<int> balance;
        Func<long> clock;
        int availableLevel=-1;
        bool wired,busy;
        readonly int[] stagedEggs={-1,-1,-1};
        Button eggButton;
        RoyalPostmanHatchReveal reveal;
        PetHatchCatalog simulatedRewards;
        double nextRefresh;
        static readonly string[] PT={"Comum","Raro","Épico","Lendário"},EN={"Common","Rare","Epic","Legendary"};
        public void Bind(RoyalPostmanPetsScreen owner,PetsIncubationState state,Action save,Func<int,bool> spend,Func<int> balance,Func<long> clock)
        {
            if(!ReferenceEquals(stock,state)){selectedEgg=-1;for(int i=0;i<3;i++)stagedEggs[i]=-1;reveal?.Hide();}
            this.owner=owner;stock=state;stock.Normalize();this.save=save;this.spend=spend;this.balance=balance;this.clock=clock;
            if(rewards==null)rewards=Resources.Load<PetHatchCatalog>("Pets/PetHatchCatalog");
            if(!wired){wired=true;incubate.onClick.AddListener(PrimaryAction);accelerate.onClick.AddListener(Accelerate);if(cancel!=null)cancel.onClick.AddListener(CancelIncubation);legacy.onClick.AddListener(()=>{selectedSpace=3;Refresh();});}
            presentation?.Initialize();
            if(eggButton==null){egg.raycastTarget=true;eggButton=egg.GetComponent<Button>()??egg.gameObject.AddComponent<Button>();eggButton.targetGraphic=egg;eggButton.transition=Selectable.Transition.None;eggButton.onClick.AddListener(()=>RemoveSelectedEgg());}
            if(particles!=null)particles.EnableWarmth();
            Refresh();
        }
        void OnEnable(){Refresh();}
        void OnDisable(){reveal?.Hide();presentation?.CloseDrawer();}
        void Update(){if(Time.realtimeSinceStartupAsDouble>=nextRefresh){nextRefresh=Time.realtimeSinceStartupAsDouble+.25;Refresh();}}
        public void SelectEgg(int index){if(index<0||index>3||selectedSpace>=3||Occupied||Locked||stock==null||stock.EggCount(index)<=0)return;selectedEgg=index;stagedEggs[selectedSpace]=index;presentation?.CloseDrawer();Refresh();}
        public bool RemoveSelectedEgg(){if(Occupied||selectedSpace>=3||selectedEgg<0)return false;selectedEgg=-1;stagedEggs[selectedSpace]=-1;Refresh();return true;}
        public void SelectSpace(int index){selectedSpace=Mathf.Clamp(index,0,2);selectedEgg=stagedEggs[selectedSpace];Refresh();}
        public void ConfigureSimulation(PetHatchCatalog catalog){simulatedRewards=catalog;}
        public RoyalPostmanHatchReveal Reveal=>reveal;
        public void Step(int delta){SelectSpace((Math.Min(selectedSpace,2)+delta%3+3)%3);}
        public void RefreshAvailability(int level){availableLevel=level;Refresh();}
        public void ShowSection(bool pets)
        {
            headerTitle.SetPetsSection(pets);
            if(owner!=null){SetTab(owner.currentTab,pets);SetTab(owner.incubatorTab,!pets);}
            if(presentation!=null&&owner!=null)owner.currentTab.transform.parent.gameObject.SetActive(pets);
        }
        void SetTab(Button tab,bool selected)
        {
            if(tab==null)return;
            tab.interactable=true;
            var image=tab.targetGraphic as Image??tab.GetComponent<Image>();
            if(image==null)return;
            image.color=Color.white;
            var paint=selected?selectedTabMaterial:unselectedTabMaterial;
            if(paint!=null)image.material=paint;
        }
        bool Portuguese()
        {
            if(!Application.isPlaying&&headerTitle!=null&&headerTitle.editorLanguage!=RoyalPostmanHeaderTitle.EditorLanguage.GameLanguage)return headerTitle.editorLanguage==RoyalPostmanHeaderTitle.EditorLanguage.Portuguese;
            var language=CrosssceneData.selectedLanguage;
            return string.IsNullOrEmpty(language)?Application.systemLanguage==SystemLanguage.Portuguese:language.StartsWith("Portuguese",StringComparison.OrdinalIgnoreCase)||language.StartsWith("pt",StringComparison.OrdinalIgnoreCase);
        }
        int Level=>Application.isPlaying?MetaProgress.NextLevel:availableLevel>=0?availableLevel:previewPlayerLevel;
        bool Occupied=>stock!=null&&stock.Occupied(selectedSpace);
        int Unlock=>selectedSpace<3?unlockLevels[selectedSpace]:0;
        bool Locked=>Level<Unlock&&!Occupied;
        long Now=>clock!=null?clock():DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        bool Reward(int kind,out PetHatchCatalog.Entry reward){var catalog=simulatedRewards!=null?simulatedRewards:rewards;if(catalog!=null)return catalog.TryGet(kind,out reward);reward=null;return false;}
        public void Refresh()
        {
            if(egg==null||display==null||incubate==null||eggs==null||eggs.Length!=4)return;
            bool pt=Portuguese(),occupied=Occupied,locked=Locked;
            if(owner!=null){owner.currentTab.GetComponentInChildren<Text>(true).text=pt?"Mascotes":"Pets";owner.incubatorTab.GetComponentInChildren<Text>(true).text=pt?"Incubadora":"Hatchery";}
            if(!occupied&&selectedEgg>=0&&stock!=null&&stock.EggCount(selectedEgg)<=0){selectedEgg=-1;if(selectedSpace<3)stagedEggs[selectedSpace]=-1;}
            int kind=occupied?stock.eggKinds[selectedSpace]:selectedEgg;bool hasEgg=occupied||selectedEgg>=0;
            long remaining=occupied?stock.Remaining(selectedSpace,Now):0;
            bool running=occupied&&remaining>0;
            egg.sprite=hasEgg?eggs[Mathf.Clamp(kind,0,3)]:null;egg.gameObject.SetActive(!locked&&hasEgg);egg.color=Color.white;if(eggButton!=null)eggButton.interactable=!occupied&&!locked&&hasEgg;
            lockIcon.gameObject.SetActive(locked);
            spaceLabel.text=locked?(pt?"Desbloqueia no nível ":"Unlocks at level ")+Unlock:selectedSpace==3?(pt?"Incubação anterior":"Previous incubation"):(pt?"Espaço ":"Space ")+(selectedSpace+1);
            actionLabel.text=locked?(pt?"Bloqueado":"Locked"):occupied?(running?(pt?"Incubando":"Incubating"):(pt?"Pronto":"Ready")):(pt?"Incubar":"Incubate");
            timeLabel.text=occupied?(pt?"Tempo restante":"Time remaining"):(pt?"Duração":"Duration");
            var rules=owner!=null?owner.incubationRules:new PetsIncubationRules();
            display.Value=locked||!hasEgg?"--:--:--":RoyalPostmanPetsScreen.FormatIncubationTime(occupied?remaining:rules.EggSeconds(selectedEgg));
            bool configured=hasEgg&&Reward(kind,out _);
            if(!occupied&&!locked&&hasEgg&&!configured)actionLabel.text=pt?"Em breve":"Coming soon";
            incubate.interactable=!locked&&hasEgg&&configured&&stock!=null&&(!occupied?selectedSpace<3&&stock.EggCount(selectedEgg)>0:!running);
            int count=0;if(stock!=null)foreach(int n in stock.accelerationItems)count=(int)Math.Min(int.MaxValue,(long)count+n);
            int cost=stock!=null&&running?rules.GoldCost(stock.goldPurchases[selectedSpace],remaining):0;
            accelerateLabel.text=pt?"Acelerar":"Speed up";
            accelerateQuantity.text=separateAcceleratorStock?"x"+count:count>0||!running?"x"+count:cost>0?cost+(pt?" ouro":" gold"):"x0";
            if(accelerateCost!=null){bool show=running&&!locked&&count==0&&cost>0;accelerateCost.text=show?cost+(pt?" ouro":" gold"):string.Empty;accelerateCost.gameObject.SetActive(show);}
            accelerate.interactable=!locked&&running&&(count>0||(cost>0&&spend!=null&&balance!=null&&balance()>=cost));
            if(cancel!=null)cancel.interactable=!locked&&running;
            if(cancelLabel!=null)cancelLabel.text=pt?"Cancelar":"Cancel";
            if(particles!=null)particles.SetRunning(running&&!locked);
            for(int i=0;i<4;i++)
            {
                itemNames[i].text=(pt?PT:EN)[i];quantities[i].text="x"+(stock?.EggCount(i)??0);
                selections[i].SetActive(hasEgg&&i==kind);
            }
            legacy.gameObject.SetActive(stock!=null&&stock.Occupied(3));
            legacyLabel.text=pt?"Incubação anterior preservada":"Previous incubation preserved";
            presentation?.Refresh(pt,running,locked,hasEgg,kind,Level,stock);
            TryPresentCompleted();
        }
        void PrimaryAction()
        {
            if(presentation!=null&&Occupied&&stock.Remaining(selectedSpace,Now)>0){CancelIncubation();return;}
            StartIncubation();
        }
        void StartIncubation()
        {
            if(busy||stock==null||owner==null)return;if(Occupied){TryPresentCompleted();return;}if(selectedEgg<0||!Reward(selectedEgg,out _))return;busy=true;
            try{if(!stock.IncubateSlot(selectedSpace,selectedEgg,Level,Unlock,owner.incubationRules,Now))return;stagedEggs[selectedSpace]=-1;save?.Invoke();owner.UseItemRequested?.Invoke(selectedSpace,new[]{"common-egg","rare-egg","epic-egg","legendary-egg"}[selectedEgg]);Refresh();}
            finally{busy=false;}TryPresentCompleted();
        }
        void Accelerate()
        {
            if(busy||stock==null||owner==null||Locked)return;busy=true;
            try{if(!stock.Accelerate(selectedSpace,Math.Max(Level,Occupied?Unlock:0),Unlock,owner.incubationRules,Now,-1,spend))return;save?.Invoke();owner.AccelerateRequested?.Invoke(selectedSpace);Refresh();}
            finally{busy=false;}TryPresentCompleted();
        }
        void CancelIncubation()
        {
            if(busy||stock==null||Locked)return;busy=true;
            try{if(!stock.CancelIncubation(selectedSpace,Now))return;selectedEgg=-1;if(selectedSpace<3)stagedEggs[selectedSpace]=-1;save?.Invoke();Refresh();}
            finally{busy=false;}
        }
        public void TryPresentCompleted()
        {
            if(busy||stock==null||owner==null||!gameObject.activeInHierarchy||!owner.workshop.activeInHierarchy||(reveal!=null&&reveal.IsOpen))return;
            busy=true;
            try
            {
                PetHatchReceipt receipt=stock.pendingHatches.Find(r=>r!=null);
                if(receipt==null)for(int slot=0;slot<4;slot++)
                {
                    if(!stock.Occupied(slot)||stock.Remaining(slot,Now)>0||!Reward(stock.eggKinds[slot],out var reward))continue;
                    string snapshot=JsonUtility.ToJson(stock);if(!stock.TryHatch(slot,Now,reward,out receipt))continue;
                    try{save?.Invoke();}catch{JsonUtility.FromJsonOverwrite(snapshot,stock);throw;}
                    if(slot<3)stagedEggs[slot]=-1;if(slot==selectedSpace)selectedEgg=-1;break;
                }
                if(receipt==null)return;
                presentation?.CloseDrawer();
                var catalog=simulatedRewards!=null?simulatedRewards:rewards;var portrait=catalog!=null?catalog.Portrait(receipt.petId):null;
                if(portrait==null)portrait=owner.currentPanel.GetComponent<RoyalPostmanCurrentPet>()?.hero.sprite;
                if(reveal==null)reveal=owner.GetComponent<RoyalPostmanHatchReveal>()??owner.gameObject.AddComponent<RoyalPostmanHatchReveal>();
                var pending=receipt;
                reveal.Show(owner,receipt,eggs[Mathf.Clamp(receipt.eggKind,0,3)],portrait,Portuguese(),()=>
                {
                    string snapshot=JsonUtility.ToJson(stock);if(stock.AcknowledgeHatch(pending.receiptId))try{save?.Invoke();}catch{JsonUtility.FromJsonOverwrite(snapshot,stock);throw;}
                    Refresh();
                });
            }
            finally{busy=false;}
        }
    }
}
