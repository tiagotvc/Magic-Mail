using System;
using System.Collections.Generic;
using System.Globalization;

namespace RoyalAves.Meta
{
    // Daily goals count first wins; the season counts each qualifying level once, including replays.
    [Serializable] public sealed class PostalEventState
    {
        public string day="",season="";
        public int dailyWins,lastCreditedLevel,seasonWins;
        public bool dailyClaimed,joined;
        public List<int> seasonClaims=new List<int>();
        public List<int> seasonLevels=new List<int>();
        public bool Observe(PostalEventsConfig config,DateTime now)
        {
            bool changed=false;string today=now.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
            if(day!=today){day=today;dailyWins=0;dailyClaimed=false;changed=true;}
            if(season!=config.seasonId){season=config.seasonId;seasonWins=0;joined=false;seasonClaims=new List<int>();seasonLevels=new List<int>();changed=true;}
            if(seasonClaims==null){seasonClaims=new List<int>();changed=true;}
            if(seasonLevels==null){seasonLevels=new List<int>();changed=true;}return changed;
        }
        public bool RecordWin(int level,bool christmas,PostalEventsConfig config,DateTime now)
        {
            Observe(config,now);if(level<=lastCreditedLevel)return false;
            lastCreditedLevel=level;dailyWins++;
            RecordSeasonWin(level,christmas,config,now);return true;
        }
        public bool RecordSeasonWin(int level,bool christmas,PostalEventsConfig config,DateTime now)
        {
            Observe(config,now);
            if(level<1||!christmas||!joined||!config.IsActive(now)||seasonLevels.Contains(level))return false;
            seasonLevels.Add(level);seasonWins++;return true;
        }
        public bool Join(int nextLevel,PostalEventsConfig config,DateTime now)
        {
            Observe(config,now);if(joined||nextLevel<config.seasonUnlockLevel||!config.IsActive(now))return false;joined=true;return true;
        }
        public bool ClaimDaily(PostalEventsConfig config,DateTime now,Func<int,bool> award)
        {
            Observe(config,now);if(dailyClaimed||dailyWins<Math.Max(1,config.dailyGoal))return false;
            dailyClaimed=true;try{if(award(Math.Max(0,config.dailyCoins)))return true;}catch{dailyClaimed=false;throw;}dailyClaimed=false;return false;
        }
        public bool ClaimSeason(int index,PostalEventsConfig config,DateTime now,Func<int,bool> award)
        {
            Observe(config,now);if(!joined||index<0||index>=config.seasonRewards.Count||seasonClaims.Contains(index))return false;
            var reward=config.seasonRewards[index];if(reward==null||seasonWins<Math.Max(1,reward.goal))return false;
            seasonClaims.Add(index);try{if(award(Math.Max(0,reward.coins)))return true;}catch{seasonClaims.Remove(index);throw;}seasonClaims.Remove(index);return false;
        }
    }
}
