using System;
using UnityEngine;

namespace RoyalAves.Meta
{
    public static class PetsProgress
    {
        const string SaveKey="magic-mail-pets-v1";
        static PetsIncubationState state;
        public static event Action Changed;
        public static PetsIncubationState State
        {
            get{
                if(state==null){try{state=JsonUtility.FromJson<PetsIncubationState>(PlayerPrefs.GetString(SaveKey,""));}catch(ArgumentException){state=null;}state??=new PetsIncubationState();state.Normalize();}
                return state;
            }
        }
        public static void Save(){PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(State));PlayerPrefs.Save();Changed?.Invoke();}
        public static void GrantCommonEggs(int count){if(count<=0)return;State.commonEggs=(int)Math.Min(int.MaxValue,(long)State.commonEggs+count);Save();}
        public static void GrantRareEggs(int count){if(count<=0)return;State.rareEggs=(int)Math.Min(int.MaxValue,(long)State.rareEggs+count);Save();}
        public static void GrantEpicEggs(int count){if(count<=0)return;State.epicEggs=(int)Math.Min(int.MaxValue,(long)State.epicEggs+count);Save();}
        public static void GrantLegendaryEggs(int count){if(count<=0)return;State.legendaryEggs=(int)Math.Min(int.MaxValue,(long)State.legendaryEggs+count);Save();}
        public static void GrantAccelerationItems(int minutes,int count){int i=Array.IndexOf(PetsIncubationState.AccelerationSeconds,minutes*60);if(count<=0)return;if(i<0)throw new ArgumentOutOfRangeException(nameof(minutes));State.accelerationItems[i]=(int)Math.Min(int.MaxValue,(long)State.accelerationItems[i]+count);Save();}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetCache(){state=null;Changed=null;}
    }
}
