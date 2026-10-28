using System;
using RoyalAves.Meta.WorldMap;
using SweetSugar.Scripts.Core;
using UnityEngine;

namespace RoyalAves.Meta
{
    public static class PostalEvents
    {
        const string SaveKey="magic-mail-events-v1";
        static PostalEventState state;static PostalEventsConfig config;static bool claiming;
        public static event Action Changed;
        public static PostalEventsConfig Config=>config!=null?config:config=Resources.Load<PostalEventsConfig>(PostalEventsConfig.ResourcePath);
        public static PostalEventState State
        {
            get{
                if(state==null){try{state=JsonUtility.FromJson<PostalEventState>(PlayerPrefs.GetString(SaveKey,""));}catch(ArgumentException){state=null;}state??=new PostalEventState();}
                if(Config!=null&&state.Observe(Config,DateTime.UtcNow))Save();return state;
            }
        }
        public static bool IsChristmasLevel(int level)
        {
            var islands=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);
            if(islands==null||level<1||level>islands.Capacity)return false;
            return islands.Recipe(islands.AtLevel(level))?.christmas==true;
        }
        // Called from MetaProgress's first-win guard; no OnWin duplicate subscription.
        public static void RecordFirstWin(int level)
        {
            if(Config!=null&&State.RecordWin(level,IsChristmasLevel(level),Config,DateTime.UtcNow))Save();
        }
        public static void RecordLevelWin(int level)
        {
            if(Config!=null&&State.RecordSeasonWin(level,IsChristmasLevel(level),Config,DateTime.UtcNow))Save();
        }
        public static int NextSeasonLevel
        {
            get{
                var islands=Resources.Load<WorldMapIslands>(WorldMapIslands.ResourcePath);if(islands==null)return 0;
                int last=Math.Min(MetaProgress.NextLevel,islands.Capacity);var progress=State;
                bool Eligible(int level)=>WorldMapLevelCatalog.Exists(level)&&islands.Recipe(islands.AtLevel(level))?.christmas==true&&!progress.seasonLevels.Contains(level);
                if(last==MetaProgress.NextLevel&&Eligible(last))return last;
                for(int level=1;level<=last;level++)if(Eligible(level))return level;return 0;
            }
        }
        public static void ResetProgress(){state=new PostalEventState();PlayerPrefs.DeleteKey(SaveKey);PlayerPrefs.Save();Changed?.Invoke();}
        public static bool Join(){if(Config==null||!State.Join(MetaProgress.NextLevel,Config,DateTime.UtcNow))return false;Save();return true;}
        public static bool ClaimDaily()=>Claim(s=>s.ClaimDaily(Config,DateTime.UtcNow,Award));
        public static bool ClaimSeason(int index)=>Claim(s=>s.ClaimSeason(index,Config,DateTime.UtcNow,Award));
        static bool Claim(Func<PostalEventState,bool> claim)
        {
            if(claiming||Config==null)return false;claiming=true;
            try{if(!claim(State))return false;Save();return true;}finally{claiming=false;}
        }
        static bool Award(int coins){if(coins<=0)return true;if(InitScript.Instance==null)return false;InitScript.Instance.AddGems(coins);return true;}
        static void Save(){PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(state));PlayerPrefs.Save();Changed?.Invoke();}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetCache(){state=null;config=null;claiming=false;}
    }
}
