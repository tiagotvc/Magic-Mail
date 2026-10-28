using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Meta
{
    [Serializable]
    public sealed class PetsIncubationRules
    {
        [Min(1)] public int commonEggSeconds=3600, rareEggSeconds=10800, epicEggSeconds=43200, legendaryEggSeconds=172800;
        public int[] goldPrices={500,1000,1500,3000,4500,8000};
        public int EggSeconds(int slot)=>slot==0?commonEggSeconds:slot==1?rareEggSeconds:slot==2?epicEggSeconds:slot==3?legendaryEggSeconds:0;
        public int GoldCost(int purchases,long remaining)
        {
            if(goldPrices==null||goldPrices.Length==0)return 0;
            return Math.Max(0,goldPrices[Math.Max(0,Math.Min(goldPrices.Length-1,purchases))]);
        }
    }

    [Serializable]
    public sealed class PetsIncubationState
    {
        public static readonly int[] AccelerationSeconds={1800,3600,5400,7200};
        public int commonEggs, rareEggs, epicEggs, legendaryEggs;
        public int[] accelerationItems=new int[4], goldPurchases=new int[4];
        public long[] started = new long[4], finishes = new long[4];
        // The fourth saved timer remains intact for pre-redesign games. New starts use three spaces.
        public int[] eggKinds = {0,1,2,3};
        public List<HatchedPetStack> hatchedPets=new List<HatchedPetStack>();
        public List<PetHatchReceipt> pendingHatches=new List<PetHatchReceipt>();

        public void Normalize()
        {
            commonEggs=Math.Max(0,commonEggs);rareEggs=Math.Max(0,rareEggs);epicEggs=Math.Max(0,epicEggs);legendaryEggs=Math.Max(0,legendaryEggs);
            if(accelerationItems==null)accelerationItems=new int[4];else if(accelerationItems.Length!=4)Array.Resize(ref accelerationItems,4);
            if(goldPurchases==null)goldPurchases=new int[4];else if(goldPurchases.Length!=4)Array.Resize(ref goldPurchases,4);
            for(int i=0;i<4;i++)accelerationItems[i]=Math.Max(0,accelerationItems[i]);
            for(int i=0;i<4;i++)goldPurchases[i]=Math.Max(0,goldPurchases[i]);
            if(started==null)started=new long[4];else if(started.Length!=4)Array.Resize(ref started,4);
            if(finishes==null)finishes=new long[4];else if(finishes.Length!=4)Array.Resize(ref finishes,4);
            if(eggKinds==null)eggKinds=new[]{0,1,2,3};
            else if(eggKinds.Length!=4){int old=eggKinds.Length;Array.Resize(ref eggKinds,4);for(int i=old;i<4;i++)eggKinds[i]=i;}
            for(int i=0;i<4;i++)if(eggKinds[i]<0||eggKinds[i]>3)eggKinds[i]=i;
            if(hatchedPets==null)hatchedPets=new List<HatchedPetStack>();
            if(pendingHatches==null)pendingHatches=new List<PetHatchReceipt>();
        }
        public bool Occupied(int index)=>index>=0&&finishes!=null&&index<finishes.Length&&finishes[index]>0;
        public long Remaining(int index,long now)=>Occupied(index)?Math.Max(0,finishes[index]-now):0;
        public int EggCount(int slot)=>slot==0?commonEggs:slot==1?rareEggs:slot==2?epicEggs:slot==3?legendaryEggs:0;
        public int FindAccelerationItem(int preferred)
        {
            if(preferred>=0&&preferred<4&&accelerationItems[preferred]>0)return preferred;
            for(int i=0;i<4;i++)if(accelerationItems[i]>0)return i;return -1;
        }
        public bool Incubate(int index,int level,int unlockLevel,PetsIncubationRules rules,long now)
        {
            Normalize();
            if(index<0||index>=4||level<unlockLevel||Occupied(index)||EggCount(index)<=0||rules==null||rules.EggSeconds(index)<=0)return false;
            if(index==0)commonEggs--;else if(index==1)rareEggs--;else if(index==2)epicEggs--;else legendaryEggs--;
            eggKinds[index]=index;started[index]=now;finishes[index]=now+rules.EggSeconds(index);goldPurchases[index]=0;return true;
        }
        public bool IncubateSlot(int slot,int eggKind,int level,int unlockLevel,PetsIncubationRules rules,long now)
        {
            Normalize();
            if(slot<0||slot>=3||eggKind<0||eggKind>3||level<unlockLevel||Occupied(slot)||EggCount(eggKind)<=0||rules==null||rules.EggSeconds(eggKind)<=0)return false;
            if(eggKind==0)commonEggs--;else if(eggKind==1)rareEggs--;else if(eggKind==2)epicEggs--;else legendaryEggs--;
            eggKinds[slot]=eggKind;started[slot]=now;finishes[slot]=now+rules.EggSeconds(eggKind);goldPurchases[slot]=0;return true;
        }
        // Cancel only an unfinished job. Return its egg; spent boosts/currency are not restored.
        public bool CancelIncubation(int index,long now)
        {
            Normalize();
            if(Remaining(index,now)<=0)return false;
            int kind=eggKinds[index];
            if(EggCount(kind)==int.MaxValue)return false;
            if(kind==0)commonEggs++;else if(kind==1)rareEggs++;else if(kind==2)epicEggs++;else legendaryEggs++;
            started[index]=0;finishes[index]=0;goldPurchases[index]=0;return true;
        }
        public bool Accelerate(int index,int level,int unlockLevel,PetsIncubationRules rules,long now,int preferredItem,Func<int,bool> spendGold)
        {
            Normalize();
            if(level<unlockLevel||Remaining(index,now)<=0||rules==null)return false;
            int item=FindAccelerationItem(preferredItem),seconds;
            if(item>=0){accelerationItems[item]--;seconds=AccelerationSeconds[item];}
            else{
                int cost=rules.GoldCost(goldPurchases[index],Remaining(index,now));
                if(cost<=0||spendGold==null||!spendGold(cost))return false;
                goldPurchases[index]++;seconds=1800;
            }
            finishes[index]=Math.Max(now,finishes[index]-seconds);return true;
        }
        public bool TryHatch(int index,long now,PetHatchCatalog.Entry reward,out PetHatchReceipt receipt)
        {
            receipt=null;Normalize();
            if(!Occupied(index)||Remaining(index,now)>0||reward==null||reward.eggKind!=eggKinds[index]||string.IsNullOrWhiteSpace(reward.petId))return false;
            var owned=hatchedPets.Find(p=>p!=null&&p.petId==reward.petId);
            if(owned!=null&&owned.copies==int.MaxValue)return false;
            bool isNew=owned==null||owned.copies<=0;
            if(owned==null){owned=new HatchedPetStack{petId=reward.petId};hatchedPets.Add(owned);}
            receipt=new PetHatchReceipt{receiptId=Guid.NewGuid().ToString("N"),petId=reward.petId,portugueseName=reward.portugueseName,englishName=reward.englishName,rarity=Math.Max(0,Math.Min(3,reward.rarity)),eggKind=eggKinds[index],isNew=isNew};
            owned.copies=Math.Max(0,owned.copies)+1;pendingHatches.Add(receipt);
            started[index]=0;finishes[index]=0;goldPurchases[index]=0;return true;
        }
        public bool AcknowledgeHatch(string receiptId)
        {
            if(pendingHatches==null||string.IsNullOrEmpty(receiptId))return false;
            int index=pendingHatches.FindIndex(r=>r!=null&&r.receiptId==receiptId);
            if(index<0)return false;pendingHatches.RemoveAt(index);return true;
        }
    }
}
