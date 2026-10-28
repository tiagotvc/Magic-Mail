using System;
using UnityEngine;

namespace RoyalAves.Meta
{
    [CreateAssetMenu(menuName="Royal Postman/Pets/Hatch rewards")]
    public sealed class PetHatchCatalog : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            [Range(0,3)] public int eggKind;
            public string petId,portugueseName,englishName;
            [Range(0,3)] public int rarity;
            public Sprite portrait;
        }
        // Empty until real mascots are authored. Editor simulations supply their own temporary catalogue.
        public Entry[] entries=Array.Empty<Entry>();
        public bool TryGet(int eggKind,out Entry reward)
        {
            if(entries==null){reward=null;return false;}
            foreach(var item in entries)
                if(item!=null&&item.eggKind==eggKind&&!string.IsNullOrWhiteSpace(item.petId)&&!string.IsNullOrWhiteSpace(item.portugueseName)&&item.portrait!=null){reward=item;return true;}
            reward=null;return false;
        }
        public Sprite Portrait(string id)
        {
            if(entries==null)return null;
            foreach(var item in entries)if(item!=null&&item.petId==id)return item.portrait;return null;
        }
    }

    [Serializable] public sealed class HatchedPetStack
    {
        public string petId;
        public int copies;
    }
    [Serializable] public sealed class PetHatchReceipt
    {
        public string receiptId,petId,portugueseName,englishName;
        public int eggKind,rarity;
        public bool isNew;
    }
}
