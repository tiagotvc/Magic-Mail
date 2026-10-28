using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using RoyalAves.Meta;
using RoyalAves.EditorTools;
using RoyalAves.UI.RoyalPostman2026;

public static class PetsIncubationChecks
{
    static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
    public static void Run(RoyalPostmanRuntimeKit kit,string output)
    {
        var rules=new PetsIncubationRules();long now=1700000000;
        var state=new PetsIncubationState{commonEggs=1,rareEggs=1,epicEggs=1};
        Check(!state.Incubate(1,1,30,rules,now)&&state.rareEggs==1,"Locked slot consumed an egg");
        for(int i=0;i<3;i++){Check(state.Incubate(i,100,i==0?0:i==1?30:100,rules,now),"Egg could not enter its incubator");Check(state.Remaining(i,now)==new[]{3600,10800,43200}[i],"Wrong egg duration");}
        Check(!state.Incubate(0,100,0,rules,now)&&state.commonEggs==0,"Occupied incubator accepted a second egg");
        int charged=0;var expected=new[]{500,1000,1500,3000,4500,8000,8000,8000};
        for(int i=0;i<expected.Length;i++){int prior=charged;long before=state.Remaining(2,now);Check(state.Accelerate(2,100,100,rules,now,-1,cost=>{charged+=cost;return true;}),"Gold acceleration failed");Check(charged-prior==expected[i]&&before-state.Remaining(2,now)==1800,"Progressive price or 30-minute gold step incorrect");}
        string saved=JsonUtility.ToJson(state);var restored=JsonUtility.FromJson<PetsIncubationState>(saved);restored.Normalize();Check(restored.goldPurchases[2]==8&&restored.finishes[2]==state.finishes[2],"Save lost escalation/timer");
        Check(!state.Accelerate(2,100,100,rules,now,-1,cost=>false)&&JsonUtility.ToJson(state)==saved,"Failed payment changed stock or timer");
        for(int item=0;item<4;item++){
            var stock=new PetsIncubationState{epicEggs=1};stock.accelerationItems[item]=1;stock.Incubate(2,100,100,rules,now);
            Check(stock.Accelerate(2,100,100,rules,now,item,cost=>throw new Exception("Item acceleration spent gold")),"Item acceleration failed");
            Check(stock.accelerationItems[item]==0&&stock.Remaining(2,now)==43200-PetsIncubationState.AccelerationSeconds[item]&&stock.goldPurchases[2]==0,"Wrong item duration/consumption/escalation");
        }
        using(var document=new RoyalPostmanScreenDocument(kit,"pets",720,1280)){
            var view=document.Root.GetComponent<RoyalPostmanPetsScreen>();view.Initialize(()=>{},i=>{});view.ShowCurrent(false);
            var stock=new PetsIncubationState{commonEggs=5};stock.accelerationItems[0]=1;
            int balance=10000,saves=0;string persisted="";
            Action save=()=>{saves++;persisted=JsonUtility.ToJson(stock);};
            Func<int,bool> spend=cost=>{if(balance<cost)return false;balance-=cost;return true;};
            view.BindInventory(stock,save,spend,()=>balance,()=>now);
            var action=view.incubators[0].useItem;var label=action.transform.Find("Label").GetComponent<Text>();
            Check(view.incubators.All(s=>s.accelerate==null)&&action.interactable&&label.text=="Incubar x5","Single incubation button incorrect");
            document.Export(output+"/pets-ready-720.png");
            view.incubators[1].useItem.onClick.Invoke();Check(stock.commonEggs==5&&saves==0,"Locked UI action changed stock");
            view.SelectItem(Array.FindIndex(view.items,i=>i.kind==RoyalPostmanPetsScreen.InventoryItem.Kind.RareEgg));
            action.onClick.Invoke();Check(stock.commonEggs==4&&stock.Occupied(0)&&saves==1&&view.incubators[0].egg.gameObject.activeSelf&&label.text.Contains("30 min"),"Incubate did not consume one COMMON egg or enter acceleration mode");
            Check(view.items.Where(i=>i.kind==RoyalPostmanPetsScreen.InventoryItem.Kind.CommonEgg).All(i=>i.quantity.text=="x4"),"Common egg labels not synchronized");
            document.Export(output+"/pets-incubating-item-720.png");
            action.onClick.Invoke();Check(stock.accelerationItems[0]==0&&stock.Remaining(0,now)==1800&&balance==10000&&saves==2&&label.text.Contains("500 gold"),"Item priority/gold fallback incorrect");
            document.Export(output+"/pets-incubating-gold-720.png");
            balance=0;view.BindInventory(stock,save,spend,()=>balance,()=>now);Check(!action.interactable,"Unaffordable gold action remains enabled");action.onClick.Invoke();Check(saves==2&&stock.Remaining(0,now)==1800,"Unaffordable action mutated state");
            balance=10000;view.BindInventory(stock,save,spend,()=>balance,()=>now);action.onClick.Invoke();Check(balance==9500&&saves==3&&stock.Remaining(0,now)==0&&!action.interactable&&label.text=="Concluída","Gold deduction/completion incorrect");
            action.onClick.Invoke();Check(balance==9500&&saves==3,"Completed incubation charged again");
            var loaded=JsonUtility.FromJson<PetsIncubationState>(persisted);Check(loaded.commonEggs==4&&loaded.goldPurchases[0]==1&&loaded.finishes[0]==now,"UI actions not persisted");
            view.BindInventory(new PetsIncubationState(),()=>{},cost=>false,()=>0,()=>now);Check(!action.interactable&&label.text=="Incubar x0"&&!view.incubators[0].egg.gameObject.activeSelf,"Zero common eggs not disabled/empty");
        }
    }
}
