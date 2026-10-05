// Saved progress of the agency: stars to spend, the last level won, the current area, its upgrades done and its chest.
// Levels are won in order, so the lobby always offers the level after the last one won. The first win of a level gives
// AreaProgressionConfig.starsPerFirstWin stars; replays give none.
using System;
using System.Collections.Generic;
using SweetSugar.Scripts.Core;
using UnityEngine;

namespace RoyalAves.Meta
{
    public static class MetaProgress
    {
        const string SaveKey = "royal-aves-meta-v1";

        [Serializable]
        class SaveData
        {
            public int stars;
            public int highestLevelWon;
            public int areaIndex;
            public int tasksDone;
            public bool chestClaimed;
            // Bottom menus (web prototype's features.js): goals claimed, album, records, local teams and profile.
            public List<string> claimedMissions = new List<string>();
            public bool albumClaimed;
            public List<LevelRecord> records = new List<LevelRecord>();
            public List<TeamData> teams = new List<TeamData>();
            public string teamId = "";
            public string profileName = "Mensageiro";
            public int avatar;
        }

        [Serializable]
        public class LevelRecord
        {
            public int level;
            public int score;
            public int wins;
        }

        [Serializable]
        public class TeamData
        {
            public string id;
            public string name;
            public string description;
            public int emblem;
            public int minLevel;
            public bool open = true;
        }

        static SaveData data;
        static SaveData Data => data ??= Load();

        /// Raised after every change that is saved.
        public static event Action Changed;

        public static int Stars => Data.stars;
        public static int NextLevel => Data.highestLevelWon + 1;
        public static int AreaIndex => Data.areaIndex;
        public static int TasksDone => Data.tasksDone;
        public static bool ChestClaimed => Data.chestClaimed;

        public static AreaDefinition CurrentArea(AreaProgressionConfig config) =>
            config.areas.Count == 0 ? null : config.areas[Mathf.Clamp(Data.areaIndex, 0, config.areas.Count - 1)];

        public static bool IsAreaComplete(AreaProgressionConfig config)
        {
            var area = CurrentArea(config);
            return area != null && Data.tasksDone >= area.tasks.Count;
        }

        public static bool HasNextArea(AreaProgressionConfig config) => Data.areaIndex + 1 < config.areas.Count;

        /// Returns the stars given: only the first win of a level counts.
        public static int RecordWin(int level, AreaProgressionConfig config)
        {
            if (level <= Data.highestLevelWon) return 0;
            Data.highestLevelWon = level;
            Data.stars += config.starsPerFirstWin;
            Save();
            return config.starsPerFirstWin;
        }

        /// Testing aid (menu Royal Aves > Teste: +10 estrelas): stars to spend on upgrades, without winning a level.
        public static void AddStars(int amount)
        {
            Data.stars = Mathf.Max(0, Data.stars + amount);
            Save();
        }

        /// Testing aid (menu Royal Aves > Ir para o nível...): jumps straight to any level, as if every one before it
        /// had already been won — so the lobby's "Nível N" button offers exactly this level. Levels are won in order
        /// and there is no per-level save beyond "highest one won", so this cannot single out one level without also
        /// marking every earlier one as won.
        public static void SetNextLevel(int level)
        {
            Data.highestLevelWon = Mathf.Max(0, level - 1);
            Save();
        }

        /// Upgrades are bought in order; returns false without enough stars.
        public static bool BuyNextTask(AreaProgressionConfig config)
        {
            var area = CurrentArea(config);
            if (area == null || Data.tasksDone >= area.tasks.Count) return false;
            var task = area.tasks[Data.tasksDone];
            if (Data.stars < task.starCost) return false;
            Data.stars -= task.starCost;
            Data.tasksDone++;
            Save();
            return true;
        }

        public static bool ClaimChest(AreaProgressionConfig config)
        {
            if (!IsAreaComplete(config) || Data.chestClaimed) return false;
            var reward = CurrentArea(config).reward;
            Data.chestClaimed = true;
            Save();
            if (reward.coins > 0) InitScript.Instance.AddGems(reward.coins);
            foreach (var booster in reward.boosters)
                InitScript.Instance.BuyBoost(booster.type, 0, booster.count);
            return true;
        }

        public static bool GoToNextArea(AreaProgressionConfig config)
        {
            if (!Data.chestClaimed || !HasNextArea(config)) return false;
            Data.areaIndex++;
            Data.tasksDone = 0;
            Data.chestClaimed = false;
            Save();
            return true;
        }

        // ---------- bottom menus ----------

        /// Levels won so far (they are won in order).
        public static int LevelsWon => Data.highestLevelWon;

        public static IReadOnlyList<LevelRecord> Records => Data.records;

        /// Best score and number of wins of a level, for the Recordes screen.
        public static void RecordResult(int level, int score)
        {
            var record = Data.records.Find(r => r.level == level);
            if (record == null) Data.records.Add(record = new LevelRecord { level = level });
            record.score = Mathf.Max(record.score, score);
            record.wins++;
            Save();
        }

        public static bool IsMissionClaimed(string id) => Data.claimedMissions.Contains(id);

        public static bool ClaimMission(LobbyFeaturesConfig.Mission mission)
        {
            if (mission == null || LevelsWon < mission.goal || IsMissionClaimed(mission.id)) return false;
            Data.claimedMissions.Add(mission.id);
            if (mission.coins > 0 && InitScript.Instance != null) InitScript.Instance.AddGems(mission.coins);
            Save();
            return true;
        }

        public static bool AlbumClaimed => Data.albumClaimed;

        public static bool ClaimAlbum(LobbyFeaturesConfig features)
        {
            if (features == null || Data.albumClaimed || LevelsWon < features.album.Count) return false;
            Data.albumClaimed = true;
            if (features.albumReward > 0 && InitScript.Instance != null) InitScript.Instance.AddGems(features.albumReward);
            Save();
            return true;
        }

        public static IReadOnlyList<TeamData> Teams => Data.teams;
        public static string TeamId => Data.teamId;
        public static TeamData CurrentTeam => string.IsNullOrEmpty(Data.teamId) ? null : Data.teams.Find(t => t.id == Data.teamId);

        /// Returns an error message, or null when the team was created (and joined).
        public static string CreateTeam(string name, string description, int emblem, int minLevel, bool open, int cost)
        {
            name = (name ?? "").Trim();
            if (CurrentTeam != null) return "Saia da sua equipe antes de criar outra.";
            if (name.Length < 3) return "Use um nome com pelo menos 3 caracteres.";
            if (minLevel < 0 || minLevel > NextLevel) return "Escolha um nível mínimo até o seu nível atual.";
            if (Data.teams.Exists(t => string.Equals(t.name, name, StringComparison.OrdinalIgnoreCase)))
                return "Este nome já está em uso neste aparelho.";
            if (InitScript.Gems < cost) return $"Você precisa de {CoinFormat.Short(cost)} moedas. Ganhe moedas em Eventos.";
            if (cost > 0) InitScript.Instance.SpendGems(cost);
            var id = "team-" + DateTime.UtcNow.Ticks + "-" + Data.teams.Count;
            Data.teams.Add(new TeamData
            {
                id = id,
                name = name.Length > 30 ? name.Substring(0, 30) : name,
                description = (description ?? "").Trim(),
                emblem = emblem,
                minLevel = minLevel,
                open = open
            });
            Data.teamId = id;
            Save();
            return null;
        }

        public static bool CanJoin(TeamData team) =>
            team != null && team.open && team.minLevel <= NextLevel && CurrentTeam == null;

        public static bool JoinTeam(string id)
        {
            var team = Data.teams.Find(t => t.id == id);
            if (!CanJoin(team)) return false;
            Data.teamId = id;
            Save();
            return true;
        }

        public static void LeaveTeam()
        {
            Data.teamId = "";
            Save();
        }

        public static string ProfileName => string.IsNullOrEmpty(Data.profileName) ? "Mensageiro" : Data.profileName;
        public static int Avatar => Data.avatar;

        public static void SetProfile(string name, int avatar)
        {
            name = (name ?? "").Trim();
            Data.profileName = name.Length == 0 ? "Mensageiro" : name.Length > 30 ? name.Substring(0, 30) : name;
            Data.avatar = Mathf.Max(0, avatar);
            Save();
        }

        public static void ResetAll()
        {
            data = new SaveData();
            Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ListenForWins()
        {
            data = null;
            LevelManager.OnWin -= OnLevelWon;
            LevelManager.OnWin += OnLevelWon;
        }

        static void OnLevelWon()
        {
            var config = AreaProgressionConfig.Instance;
            if (config != null && LevelManager.THIS != null)
                RecordWin(LevelManager.THIS.currentLevel, config);
        }

        static SaveData Load()
        {
            var json = PlayerPrefs.GetString(SaveKey, "");
            return string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json);
        }

        static void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
