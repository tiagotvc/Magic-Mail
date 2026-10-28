using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace RoyalAves.Meta
{
    [CreateAssetMenu(fileName="RoyalAvesEvents",menuName="Royal Aves/Events")]
    public sealed class PostalEventsConfig : ScriptableObject
    {
        public const string ResourcePath="RoyalAvesEvents";
        [Min(1)] public int dailyGoal=3;
        [Min(0)] public int dailyCoins=150;
        public bool seasonEnabled;
        public string seasonId="christmas-2026";
        public string seasonTitle="The stolen Christmas";
        [TextArea] public string seasonDescription="Recover one gift per Christmas level. Replays count once.";
        [Min(1)] public int seasonUnlockLevel=31;
        [Tooltip("Optional UTC date, for example 2026-12-01T00:00:00Z. Empty means no scheduled boundary.")]
        public string startsUtc="",endsUtc="";
        public List<Reward> seasonRewards=new List<Reward>{new Reward{goal=3,coins=150},new Reward{goal=8,coins=350},new Reward{goal=15,coins=750}};
        [Serializable] public sealed class Reward{[Min(1)]public int goal=3;[Min(0)]public int coins=150;}
        public bool IsActive(DateTime now)
        {
            if(!seasonEnabled||string.IsNullOrWhiteSpace(seasonId))return false;
            return Boundary(startsUtc,out var start)&&Boundary(endsUtc,out var end)&&
                (start==null||now>=start.Value)&&(end==null||now<end.Value);
        }
        public static bool Boundary(string value,out DateTime? result)
        {
            result=null;if(string.IsNullOrWhiteSpace(value))return true;
            if(!DateTime.TryParse(value,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal|DateTimeStyles.AdjustToUniversal,out var parsed))return false;
            result=parsed;return true;
        }
    }
}
