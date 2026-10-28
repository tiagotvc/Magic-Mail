using System;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.Meta;

namespace RoyalAves.UI.RoyalPostman2026
{
    /// <summary>Authored pet workshop view. Incubation data is supplied separately from its layout.</summary>
    public sealed class RoyalPostmanPetsScreen : MonoBehaviour
    {
        public const string ScreenId = "pets";

        [Serializable]
        public sealed class IncubatorView
        {
            public int unlockLevel;
            public GameObject root;
            public Image apparatus;
            public Image egg;
            public GameObject available, empty, running, locked;
            public Text progress, remaining;
            public Image progressFill;
            public Button useItem, accelerate;
            public Text actionQuantity, accelerationQuantity, accelerationCost;
            public RoyalPostmanIncubatorParticles particles;
        }

        [Serializable]
        public sealed class InventoryItem
        {
            public enum Kind { Example, CommonEgg, Acceleration, RareEgg, EpicEgg, LegendaryEgg }
            public string id, title;
            [TextArea] public string description;
            public Button button;
            public GameObject selection;
            public Kind kind;
            public Text quantity;
            public int accelerationMinutes=30;
            public bool aggregateAcceleration;
        }

        [Serializable]
        public sealed class InventoryRow
        {
            public string id;
            public Button previous, next;
            public Text pagination;
            public Button[] dots = Array.Empty<Button>();
            public Color activeDotColor = new Color(1,.78f,.04f);
            public Color inactiveDotColor = new Color(.5f,.39f,.02f,.38f);
            public GameObject[] pages;
            [NonSerialized] public int currentPage;
        }

        public Button backButton, helpButton, currentTab, incubatorTab, dismissHelp;
        public Sprite currentTabBlue,currentTabGold,incubatorTabBlue,incubatorTabGold;
        public bool defaultCurrentTab;
        public GameObject workshop, currentPanel, helpPanel;
        public Text hintTitle, hintDescription;
        public GameObject itemTooltip;
        public RectTransform tooltipCard;
        public Image tooltipIcon;
        public Button dismissItemTooltip;
        public ScrollRect inventory;
        public IncubatorView[] incubators;
        public Button previousIncubator, nextIncubator;
        public Text incubatorRarity;
        public bool mobileCarousel;
        public bool cartoonLayout;
        public Texture2D cartoonSelectedTabPaint, cartoonInactiveTabPaint;
        public bool integratedMachineLayout;
        public bool layeredIncubatorLayout;
        int selectedIncubator;
        public int SelectedIncubator { get=>hatchery!=null?hatchery.SelectedSpace:selectedIncubator; private set=>selectedIncubator=value; }
        public InventoryItem[] items;
        public InventoryRow[] inventoryRows = Array.Empty<InventoryRow>();
        public PetsIncubationRules incubationRules = new PetsIncubationRules();
        public RoyalPostmanHatchery hatchery;

        // The view never grants pets or spends inventory/currency by itself.
        public Action<int, string> UseItemRequested;
        public Action<int> AccelerateRequested;
        Action back;
        Action<int> navigate;
        int selectedItem = -1;
        int tooltipItemIndex = -1;
        bool initialized;
        readonly bool[] active = new bool[4];
        PetsIncubationState stock;
        Action saveStock;
        Func<int,bool> spendGold;
        Func<int> goldBalance;
        Func<long> clock;
        float nextStockRefresh;
        bool handlingAction;

        public void BindInventory(PetsIncubationState state,Action save,Func<int,bool> spend,Func<int> balance,Func<long> now=null)
        {
            stock=state??throw new ArgumentNullException(nameof(state));stock.Normalize();
            saveStock=save;spendGold=spend;goldBalance=balance;clock=now??(()=>DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            if(hatchery!=null)hatchery.Bind(this,stock,save,spend,balance,clock);
            RefreshStock();
        }
        void Update(){if(stock!=null&&Time.unscaledTime>=nextStockRefresh){nextStockRefresh=Time.unscaledTime+.25f;RefreshStock();}}
        void RefreshStock()
        {
            if(hatchery!=null){hatchery.Refresh();return;}
            if(stock==null)return;long now=clock();
            for(int i=0;i<incubators.Length;i++){
                var slot=incubators[i];active[i]=stock.Occupied(i);slot.egg.gameObject.SetActive(active[i]);
                long left=stock.Remaining(i,now),duration=Math.Max(1,incubationRules.EggSeconds(i));
                float progress=active[i]?Mathf.Clamp01(1-(float)left/duration):0;
                slot.progress.text=layeredIncubatorLayout?(active[i]?(left>0?"Tempo restante · "+FormatIncubationTime(left):"Pronto"):"Duração · "+FormatIncubationTime(duration)):Mathf.RoundToInt(progress*100)+" / 100";slot.progressFill.fillAmount=progress;
                slot.progressFill.GetComponent<RoyalPostmanProgressClip>()?.Refresh();
                var time=TimeSpan.FromSeconds(left);slot.remaining.text=layeredIncubatorLayout?(active[i]?(left>0?"Incubando":"Pronto"):"Selecione um ovo"):left==0?"Pronto":time.TotalHours>=1?((int)time.TotalHours)+"h "+time.Minutes+"m":time.Minutes+"m "+time.Seconds+"s";
            }
            foreach(var item in items)if(item.quantity!=null&&item.kind!=InventoryItem.Kind.Example){
                int stack=Array.IndexOf(PetsIncubationState.AccelerationSeconds,item.accelerationMinutes*60);
                int count=item.kind==InventoryItem.Kind.CommonEgg?stock.commonEggs:item.kind==InventoryItem.Kind.RareEgg?stock.rareEggs:item.kind==InventoryItem.Kind.EpicEgg?stock.epicEggs:item.kind==InventoryItem.Kind.LegendaryEgg?stock.legendaryEggs:stack>=0?stock.accelerationItems[stack]:0;
                if(item.aggregateAcceleration){count=0;foreach(var quantity in stock.accelerationItems)count=(int)Math.Min(int.MaxValue,(long)count+quantity);}
                item.quantity.text="x"+count;
            }
            if(itemTooltip!=null&&itemTooltip.activeSelf&&tooltipItemIndex>=0)UpdateTooltipText(tooltipItemIndex);
            RefreshAvailability(MetaProgress.NextLevel);
        }
        int PreferredAcceleration()
        {
            if(selectedItem<0||selectedItem>=items.Length||items[selectedItem].kind!=InventoryItem.Kind.Acceleration)return -1;
            return Array.IndexOf(PetsIncubationState.AccelerationSeconds,items[selectedItem].accelerationMinutes*60);
        }
        public static string FormatIncubationTime(long seconds)
        {
            seconds=Math.Max(0,seconds);
            return (seconds/3600).ToString("00")+":"+((seconds/60)%60).ToString("00")+":"+(seconds%60).ToString("00");
        }
        void PrimaryAction(int index)
        {
            PerformAction(index,stock!=null&&stock.Occupied(index));
        }
        void PerformAction(int index,bool accelerating)
        {
            if(stock==null||handlingAction)return;handlingAction=true;
            try{
                var slot=incubators[index];
                bool changed=accelerating?stock.Accelerate(index,MetaProgress.NextLevel,slot.unlockLevel,incubationRules,clock(),PreferredAcceleration(),spendGold):stock.Incubate(index,MetaProgress.NextLevel,slot.unlockLevel,incubationRules,clock());
                if(!changed)return;saveStock?.Invoke();RefreshStock();
                if(accelerating)AccelerateRequested?.Invoke(index);else UseItemRequested?.Invoke(index,new[]{"common-egg","rare-egg","epic-egg","legendary-egg"}[index]);
            }finally{handlingAction=false;}
        }

        public bool IsOpen => gameObject.activeSelf;
        public static RoyalPostmanPetsScreen Create(RectTransform parent, Action back, Action<int> navigate)
        {
            var kit = RoyalPostmanRuntimeKit.Load();
            if (kit == null || kit.ScreenPrefab(ScreenId) == null) return null;
            var root = kit.InstantiateScreen(ScreenId, parent);
            var view = root.GetComponent<RoyalPostmanPetsScreen>();
            view.Initialize(back, navigate);
            root.SetActive(false);
            return view;
        }

        public void Initialize(Action onBack, Action<int> onNavigate)
        {
            back = onBack;
            navigate = onNavigate;
            if (initialized) return;
            initialized = true;
            currentPanel.GetComponent<RoyalPostmanCurrentPet>()?.Initialize(this);
            backButton.onClick.AddListener(() => back?.Invoke());
            helpButton.onClick.AddListener(() => {if(layeredIncubatorLayout)back?.Invoke();else helpPanel.SetActive(true);});
            dismissHelp.onClick.AddListener(() => helpPanel.SetActive(false));
            if(dismissItemTooltip!=null)dismissItemTooltip.onClick.AddListener(HideItemTooltip);
            currentTab.onClick.AddListener(() => ShowCurrent(true));
            incubatorTab.onClick.AddListener(() => ShowCurrent(false));
            if(previousIncubator!=null)previousIncubator.onClick.AddListener(()=>StepIncubator(-1));
            if(nextIncubator!=null)nextIncubator.onClick.AddListener(()=>StepIncubator(1));
            for (int i = 0; inventoryRows != null && i < inventoryRows.Length; i++)
            {
                int index = i;
                inventoryRows[i].previous.onClick.AddListener(() => SetInventoryPage(index, inventoryRows[index].currentPage - 1));
                inventoryRows[i].next.onClick.AddListener(() => SetInventoryPage(index, inventoryRows[index].currentPage + 1));
                for(int p=0;inventoryRows[i].dots!=null&&p<inventoryRows[i].dots.Length;p++)
                {
                    int page=p;inventoryRows[i].dots[p].onClick.AddListener(()=>SetInventoryPage(index,page));
                }
                SetInventoryPage(index, 0);
            }
            for (int i = 0; i < items.Length; i++)
            {
                int index = i;
                items[i].button.onClick.AddListener(() => SelectItem(index));
            }
            if(hatchery==null)for (int i = 0; i < incubators.Length; i++)
            {
                int index = i;
                if (incubators[i].useItem != null) incubators[i].useItem.onClick.AddListener(() => {if(mobileCarousel)PerformAction(index,false);else PrimaryAction(index);});
                if (mobileCarousel&&incubators[i].accelerate != null) incubators[i].accelerate.onClick.AddListener(() => PerformAction(index,true));
            }
            var nav = transform.Find("Navigation");
            if (nav != null)
                for (int i = 0; i < 5; i++)
                {
                    int index = i;
                    var node = nav.Find("Tab" + i);
                    var hit = node.GetComponent<Graphic>();
                    if (hit == null) { var image = node.gameObject.AddComponent<Image>(); image.color = Color.clear; hit = image; }
                    hit.raycastTarget = true;
                    var button = node.GetComponent<Button>() ?? node.gameObject.AddComponent<Button>();
                    button.targetGraphic = hit;
                    button.transition = Selectable.Transition.None;
                    button.onClick.AddListener(() => navigate?.Invoke(index));
                }
            RefreshAvailability(RoyalAves.Meta.MetaProgress.NextLevel);
            SelectIncubator(SelectedIncubator);
            ShowCurrent(defaultCurrentTab);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            RefreshStock();
            RefreshAvailability(RoyalAves.Meta.MetaProgress.NextLevel);
            ShowCurrent(defaultCurrentTab);
        }
        public void Close() { HideItemTooltip();helpPanel.SetActive(false); gameObject.SetActive(false); }

        public void RefreshAvailability(int level)
        {
            if(hatchery!=null){hatchery.RefreshAvailability(level);return;}
            for (int i = 0; i < incubators.Length; i++)
            {
                var view = incubators[i];
                bool locked = level < view.unlockLevel;
                view.available.SetActive(!locked);
                view.locked.SetActive(locked);
                view.empty.SetActive(!locked && !active[i]);
                view.running.SetActive(!locked && active[i]);
                view.egg.gameObject.SetActive(!locked && active[i]);
                if(layeredIncubatorLayout){
                    bool ready=stock!=null&&active[i]&&stock.Remaining(i,clock())>0;
                    view.remaining.gameObject.SetActive(!locked);
                    view.useItem.gameObject.SetActive(!locked);
                    int accelerator=stock!=null?stock.FindAccelerationItem(PreferredAcceleration()):-1;
                    int cost=stock!=null?incubationRules.GoldCost(stock.goldPurchases[i],stock.Remaining(i,clock())):0;
                    view.useItem.interactable=!locked&&!active[i]&&stock!=null&&stock.EggCount(i)>0;
                    view.accelerate.gameObject.SetActive(!locked&&ready);
                    view.accelerate.interactable=ready&&(accelerator>=0||(cost>0&&spendGold!=null&&goldBalance!=null&&goldBalance()>=cost));
                    if(view.accelerationCost!=null)view.accelerationCost.text=ready&&accelerator<0&&cost>0?cost+" gold":"";
                    if(view.apparatus!=null)view.apparatus.color=Color.white;
                    if(view.particles!=null)view.particles.SetRunning(!locked&&ready);
                    var group=view.root.GetComponent<CanvasGroup>();if(group!=null)group.alpha=locked?.55f:1;
                    continue;
                }
                if(view.apparatus!=null)view.apparatus.color=locked?new Color(.32f,.34f,.4f,1):active[i]?Color.white:cartoonLayout?new Color(.78f,.80f,.84f,1):new Color(.64f,.66f,.72f,1);
                if(mobileCarousel){
                    int accelerator=stock!=null?stock.FindAccelerationItem(PreferredAcceleration()):-1;
                    int goldCost=stock!=null?incubationRules.GoldCost(stock.goldPurchases[i],stock.Remaining(i,clock())):0;
                    bool ready=stock!=null&&stock.Remaining(i,clock())>0;
                    view.useItem.interactable=!locked&&!active[i]&&stock!=null&&stock.EggCount(i)>0;
                    view.accelerate.interactable=!locked&&active[i]&&ready&&(accelerator>=0||(goldCost>0&&spendGold!=null&&goldBalance!=null&&goldBalance()>=goldCost));
                    if(cartoonLayout){
                        view.useItem.transform.Find("MovingCenter/Label").GetComponent<Text>().text="Incubar";
                        view.accelerate.transform.Find("MovingCenter/Label").GetComponent<Text>().text="Acelerar";
                        if(view.actionQuantity!=null)view.actionQuantity.text="x"+(stock?.EggCount(i)??0);
                        int availableItems=0;if(stock!=null)foreach(int quantity in stock.accelerationItems)availableItems=(int)Math.Min(int.MaxValue,(long)availableItems+quantity);
                        if(view.accelerationQuantity!=null)view.accelerationQuantity.text="x"+availableItems;
                        if(view.accelerationCost!=null)view.accelerationCost.text=!locked&&active[i]&&ready&&accelerator<0&&goldCost>0?goldCost+" gold":"";
                        view.useItem.GetComponent<RoyalPostmanActionVisual>()?.Refresh();view.accelerate.GetComponent<RoyalPostmanActionVisual>()?.Refresh();
                        continue;
                    }
                    view.useItem.GetComponentInChildren<Text>(true).text="Incubar x"+(stock?.EggCount(i)??0);
                    view.accelerate.GetComponentInChildren<Text>(true).text=active[i]&&ready?(accelerator>=0?"Acelerar "+PetsIncubationState.AccelerationSeconds[accelerator]/60+" min":"Acelerar · "+goldCost+" gold"):"Acelerar";
                    view.useItem.GetComponent<RoyalPostmanActionVisual>()?.Refresh();view.accelerate.GetComponent<RoyalPostmanActionVisual>()?.Refresh();
                    continue;
                }
                if(view.accelerate!=null)view.accelerate.gameObject.SetActive(false);
                if(view.useItem!=null){
                    int accelerator=stock!=null?stock.FindAccelerationItem(PreferredAcceleration()):-1;
                    int goldCost=stock!=null?incubationRules.GoldCost(stock.goldPurchases[i],stock.Remaining(i,clock())):0;
                    bool hasItem=accelerator>=0;
                    bool canGold=goldCost>0&&spendGold!=null&&goldBalance!=null&&goldBalance()>=goldCost;
                    bool canAccelerate=stock!=null&&stock.Remaining(i,clock())>0&&(hasItem||canGold);
                    view.useItem.interactable=!locked&&(active[i]?canAccelerate:stock!=null&&stock.EggCount(i)>0&&incubationRules.EggSeconds(i)>0);
                    var label=view.useItem.transform.Find("Label")?.GetComponent<Text>();
                    if(label!=null)label.text=active[i]?(stock!=null&&stock.Remaining(i,clock())==0?"Concluída":hasItem?"Acelerar\n"+(PetsIncubationState.AccelerationSeconds[accelerator]/60)+" min":goldCost>0?"Acelerar 30m\n"+goldCost+" gold":"Acelerar\ncom gold"):"Incubar x"+(stock?.EggCount(i)??0);
                }
            }
        }

        public void SetIncubation(int index, bool isActive, Sprite egg, int progress, int total, string remaining)
        {
            if (index < 0 || index >= incubators.Length) throw new ArgumentOutOfRangeException(nameof(index));
            var view = incubators[index];
            active[index] = isActive && egg != null;
            view.egg.sprite = egg;
            int max = Mathf.Max(1, total), value = Mathf.Clamp(progress, 0, max);
            view.progress.text = layeredIncubatorLayout?(remaining??""):value + " / " + max;
            view.progressFill.fillAmount = (float)value / max;
            view.progressFill.GetComponent<RoyalPostmanProgressClip>()?.Refresh();
            view.remaining.text = layeredIncubatorLayout?(isActive?"Incubando":"Selecione um ovo"):remaining ?? "";
            RefreshAvailability(RoyalAves.Meta.MetaProgress.NextLevel);
        }

        public void SelectItem(int index)
        {
            if(hatchery!=null){if(index>=0&&index<4)hatchery.SelectEgg(index);return;}
            if (index < 0 || index >= items.Length) return;
            selectedItem = index;
            if(mobileCarousel){int slot=items[index].kind==InventoryItem.Kind.CommonEgg?0:items[index].kind==InventoryItem.Kind.RareEgg?1:items[index].kind==InventoryItem.Kind.EpicEgg?2:items[index].kind==InventoryItem.Kind.LegendaryEgg?3:-1;if(slot>=0)SelectIncubator(slot);}
            tooltipItemIndex=index;
            for (int i = 0; i < items.Length; i++) items[i].selection.SetActive(i == index);
            UpdateTooltipText(index);
            if(itemTooltip!=null&&tooltipCard!=null){
                PlaceTooltip(items[index].button.GetComponent<RectTransform>());
            }
            if(stock!=null)RefreshAvailability(MetaProgress.NextLevel);
        }
        public void ShowDetails(string title,string description,Sprite icon,RectTransform anchor)
        {
            if(itemTooltip==null)return;tooltipItemIndex=-1;hintTitle.text=title;hintDescription.text=description;tooltipIcon.sprite=icon;PlaceTooltip(anchor);
        }
        void PlaceTooltip(RectTransform anchor)
        {
            if(itemTooltip!=null&&tooltipCard!=null){
                itemTooltip.SetActive(true);
                var root=(RectTransform)transform;var corners=new Vector3[4];anchor.GetWorldCorners(corners);
                Vector2 lower=root.InverseTransformPoint(corners[0]),upper=root.InverseTransformPoint(corners[2]);
                Vector2 half=tooltipCard.rect.size*.5f;
                float x=Mathf.Clamp((lower.x+upper.x)*.5f,root.rect.xMin+20+half.x,root.rect.xMax-20-half.x);
                float y=upper.y+18+half.y;
                if(y+half.y>root.rect.yMax-20)y=lower.y-18-half.y;
                y=Mathf.Clamp(y,root.rect.yMin+20+half.y,root.rect.yMax-20-half.y);
                tooltipCard.anchoredPosition=new Vector2(x,y);
            }
        }
        void UpdateTooltipText(int index)
        {
            var item=items[index];
            if(hintTitle!=null)hintTitle.text=item.title;
            string details=item.description??"";
            if(item.kind==InventoryItem.Kind.RareEgg)details+="\nIncubadora: nível 30.";
            if(item.kind==InventoryItem.Kind.EpicEgg)details+="\nIncubadora: nível 100.";
            if(item.kind==InventoryItem.Kind.LegendaryEgg&&!details.Contains("48"))details+="\nIncubação: 48 horas.";
            if(item.aggregateAcceleration&&stock!=null)for(int i=0;i<4;i++)details+=(i%2==0?"\n":"   ·   ")+(PetsIncubationState.AccelerationSeconds[i]/60)+"m: x"+stock.accelerationItems[i];
            if(item.quantity!=null)details+="\nQuantidade: "+item.quantity.text.TrimStart('x',' ');
            if(hintDescription!=null)hintDescription.text=details;
            if(tooltipIcon!=null)tooltipIcon.sprite=item.button.transform.Find("Icon")?.GetComponent<Image>()?.sprite;
        }
        public void HideItemTooltip(){tooltipItemIndex=-1;if(itemTooltip!=null)itemTooltip.SetActive(false);}
        public void ResetTooltipPreview()
        {
            HideItemTooltip();if(hintTitle!=null)hintTitle.text="";if(hintDescription!=null)hintDescription.text="";
            if(tooltipIcon!=null)tooltipIcon.sprite=null;
            if(tooltipCard!=null)tooltipCard.anchoredPosition=Vector2.zero;
        }

        public void SetInventoryPage(int rowIndex, int page)
        {
            if (inventoryRows == null || rowIndex < 0 || rowIndex >= inventoryRows.Length) return;
            var row = inventoryRows[rowIndex];
            if (row.pages == null || row.pages.Length == 0) return;
            HideItemTooltip();
            row.currentPage = Mathf.Clamp(page, 0, row.pages.Length - 1);
            for (int i = 0; i < row.pages.Length; i++) row.pages[i].SetActive(i == row.currentPage);
            row.previous.interactable = row.currentPage > 0;
            row.next.interactable = row.currentPage < row.pages.Length - 1;
            if (row.pagination != null) row.pagination.text = (row.currentPage + 1) + " / " + row.pages.Length;
            for(int i=0;row.dots!=null&&i<row.dots.Length;i++)
                row.dots[i].targetGraphic.color=i==row.currentPage?row.activeDotColor:row.inactiveDotColor;
        }
        public void ShowCurrent(bool show)
        {
            HideItemTooltip();
            workshop.SetActive(!show);
            currentPanel.SetActive(show);
            if(hatchery!=null){if(show)currentPanel.GetComponent<RoyalPostmanCurrentPet>()?.Refresh();hatchery.ShowSection(show);return;}
            foreach(string name in new[]{"Room","Inventory","Divider"}){var node=transform.Find(name);if(node!=null)node.gameObject.SetActive(!show);}
            if(show)currentPanel.GetComponent<RoyalPostmanCurrentPet>()?.Refresh();
            currentTab.transform.Find("Selected").gameObject.SetActive(show);
            incubatorTab.transform.Find("Selected").gameObject.SetActive(!show);
            var currentFace=currentTab.transform.Find("Face")?.GetComponent<Image>();
            var incubatorFace=incubatorTab.transform.Find("Face")?.GetComponent<Image>();
            if(currentFace!=null){var sprite=show?currentTabGold:currentTabBlue;if(sprite!=null)currentFace.sprite=sprite;currentFace.color=Color.white;}
            if(incubatorFace!=null){var sprite=show?incubatorTabBlue:incubatorTabGold;if(sprite!=null)incubatorFace.sprite=sprite;incubatorFace.color=Color.white;}
            if(layeredIncubatorLayout){
                currentTab.targetGraphic.color=show?new Color(.8f,.82f,.83f):new Color(.65f,.66f,.68f);
                incubatorTab.targetGraphic.color=show?new Color(.65f,.66f,.68f):new Color(.8f,.82f,.83f);
                return;
            }
            if(cartoonLayout){
                var background=transform.Find("Background")?.GetComponent<Image>();if(background!=null)background.color=show?new Color32(10,23,47,255):new Color32(148,74,28,255);
                var currentPaint=currentTab.transform.Find("MovingCenter/Face")?.GetComponent<RoyalPostmanSymmetricButtonFace>();
                var incubatorPaint=incubatorTab.transform.Find("MovingCenter/Face")?.GetComponent<RoyalPostmanSymmetricButtonFace>();
                if(currentPaint!=null){currentPaint.paint=show?cartoonSelectedTabPaint:cartoonInactiveTabPaint;currentPaint.SetAllDirty();}
                if(incubatorPaint!=null){incubatorPaint.paint=show?cartoonInactiveTabPaint:cartoonSelectedTabPaint;incubatorPaint.SetAllDirty();}
            }
        }
        public void StepIncubator(int delta){if(hatchery!=null){hatchery.Step(delta);return;}SelectIncubator((SelectedIncubator+delta%incubators.Length+incubators.Length)%incubators.Length);}
        public void SelectIncubator(int index)
        {
            if(hatchery!=null){hatchery.SelectSpace(index);return;}
            if(!mobileCarousel||incubators==null||incubators.Length==0)return;
            SelectedIncubator=Mathf.Clamp(index,0,incubators.Length-1);HideItemTooltip();
            for(int i=0;i<incubators.Length;i++)if(incubators[i].root!=null)incubators[i].root.SetActive(i==SelectedIncubator);
            if(cartoonLayout&&items!=null)for(int i=0;i<items.Length;i++){
                int slot=items[i].kind==InventoryItem.Kind.CommonEgg?0:items[i].kind==InventoryItem.Kind.RareEgg?1:items[i].kind==InventoryItem.Kind.EpicEgg?2:items[i].kind==InventoryItem.Kind.LegendaryEgg?3:-1;
                if(items[i].selection!=null)items[i].selection.SetActive(slot==SelectedIncubator);
            }
            if(incubatorRarity!=null){incubatorRarity.text=new[]{"Comum","Rara","Épica","Lendária"}[SelectedIncubator]+(cartoonLayout?"":"   ·   "+(SelectedIncubator+1)+" / "+incubators.Length);incubatorRarity.color=cartoonLayout?new Color(1,.96f,.82f):new[]{new Color(.65f,.88f,1),new Color(.84f,.57f,1),new Color(1,.42f,.38f),new Color(1,.78f,.17f)}[SelectedIncubator];}
            if(layeredIncubatorLayout&&incubatorRarity!=null){incubatorRarity.text=new[]{"Comum","Raro","Épico","Lendário"}[SelectedIncubator]+" · "+(SelectedIncubator+1)+"/4";incubatorRarity.color=new Color32(54,16,69,255);}
        }
    }
}
