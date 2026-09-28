// Serializable shapes of the JSON files in Assets/RoyalAves/Data, exported from the web version by
// scripts/export-unity.js. Load them with JsonUtility, for example:
//   var file = JsonUtility.FromJson<RoyalAves.Data.LevelFile>(levelsJson.text);
using System;
using System.Collections.Generic;

namespace RoyalAves.Data
{
    [Serializable] public class TypeCount { public string type; public int count; }
    [Serializable] public class CellHp { public int cell; public int hp; }
    [Serializable] public class CellPiece { public int cell; public string piece; }
    [Serializable] public class NamedSprite { public string type; public string name; public string sprite; }

    // levels.json
    [Serializable] public class BoardInfo { public int columns; public int rows; public string cellIndex; }
    [Serializable]
    public class Level
    {
        public int id;
        public string name;
        public string subtitle;
        public int moves;
        public int colors;
        public List<TypeCount> goals;
        public List<int> crates;
        public List<int> parcels;
        public List<int> voids;
        public List<int> blockers;
        public List<CellHp> grass;
        public List<CellHp> ice;
        public List<CellPiece> initial;
        public string tip;
    }
    [Serializable]
    public class LevelFile
    {
        public BoardInfo board;
        public List<NamedSprite> pieces;
        public List<NamedSprite> specials;
        public List<Level> levels;
    }

    // progression.json
    [Serializable] public class AreaTask { public string id; public string name; public int cost; public string sprite; }
    [Serializable] public class AreaReward { public int coins; public List<TypeCount> boosters; }
    [Serializable] public class Area { public string id; public string name; public string next; public List<AreaTask> tasks; public AreaReward reward; }
    [Serializable] public class LivesRules { public int max; public int refillMinutes; public int refillAllCost; }
    [Serializable] public class ContinueOffer { public int cost; public int moves; }
    [Serializable] public class BoosterRules { public int freePerAttempt; public int extraUnitCost; public List<NamedSprite> types; }
    [Serializable] public class Powerup { public string type; public string name; public string sprite; public int cost; }
    [Serializable]
    public class ProgressionFile
    {
        public List<Area> areas;
        public LivesRules lives;
        public ContinueOffer continueOffer;
        public BoosterRules boosters;
        public List<Powerup> powerups;
    }

    // events.json
    [Serializable] public class Mission { public string id; public string name; public int goal; public int coins; }
    [Serializable] public class AlbumCard { public string sprite; public string title; }
    [Serializable] public class Album { public int reward; public List<AlbumCard> cards; }
    [Serializable] public class BonusRoutes { public List<int> unlockAfterLevels; public int moves; public int coinsPerPiece; }
    [Serializable] public class TeamRules { public int createCost; public List<string> emblems; }
    [Serializable]
    public class EventsFile
    {
        public List<Mission> missions;
        public Album album;
        public BonusRoutes bonusRoutes;
        public TeamRules teams;
    }

    // sfx-volumes.json
    [Serializable] public class SfxVolume { public string clip; public string webSound; public float volume; }
    [Serializable] public class SfxVolumeFile { public string note; public List<SfxVolume> clips; }
}
