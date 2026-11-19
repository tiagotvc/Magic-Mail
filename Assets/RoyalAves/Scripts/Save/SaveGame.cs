// Camada única de save do jogo. Sabe QUAIS chaves do PlayerPrefs são progresso do jogador (e portanto vão
// para a nuvem) e quais são só do aparelho (limites de vídeo, consentimento de anúncios, "avaliado"...).
//
//   SaveGame.Capture()        -> SaveSnapshot com o progresso atual
//   SaveGame.Apply(snapshot)  -> grava no PlayerPrefs e atualiza o que o jogo já leu na memória
//   SaveGame.Compare(a, b)    -> quem está mais adiantado
//
// Coleção, guild e o que vier depois: chame SaveGame.RegisterKeys(...) (ou RegisterPrefix) no início do jogo
// com as chaves do novo sistema, e ele passa a ir para a nuvem sem mudar mais nada.
using System;
using System.Collections.Generic;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.GUI.Boost;
using UnityEngine;

namespace RoyalAves.Save
{
    public static class SaveGame
    {
        /// Fases consideradas no save (o jogo tem 200; sobra margem para as próximas).
        public const int MaxLevels = 500;

        // ---- Progresso que vai para a nuvem ----
        static readonly string[] FixedInts =
        {
            "Gems", "Lifes", "Lauched",
            "OpenLevel", "ReachedLevel", "LastLevel",
            "LatestDay", "Spinned",
            "Music", "Sound", "Hint", "Vibration", "Notifications",
        };
        static readonly string[] FixedFloats = { "RestLifeTimer" };
        static readonly string[] FixedStrings =
        {
            "DateOfExit", "DateReward", "Spin",
            "royal-aves-meta-v1",     // MetaProgress: áreas, tarefas, baú, histórico de fases
            "magic-mail-pets-v1",     // PetsProgress
            "magic-mail-events-v1",   // PostalEvents
        };

        // ---- Fica só no aparelho (NÃO vai para a nuvem), de propósito ----
        // BoostShop.AdUnlockAt.*, LifeShop.AdUnlockAt (limites de vídeo), npa (consentimento de anúncios),
        // Rated, UserID, OpenLevelTest.

        static readonly List<string> extraInts = new List<string>();
        static readonly List<string> extraStrings = new List<string>();

        /// Registra chaves de PlayerPrefs (texto/JSON) de um sistema novo (coleção, guild...). Chame cedo.
        public static void RegisterKeys(params string[] stringKeys)
        {
            foreach (var k in stringKeys) if (!extraStrings.Contains(k)) extraStrings.Add(k);
        }

        public static void RegisterIntKeys(params string[] intKeys)
        {
            foreach (var k in intKeys) if (!extraInts.Contains(k)) extraInts.Add(k);
        }

        // ---------------------------------------------------------------------------------------------

        static IEnumerable<string> IntKeys()
        {
            foreach (var k in FixedInts) yield return k;
            foreach (var k in extraInts) yield return k;
            foreach (var name in Enum.GetNames(typeof(BoostType))) yield return name; // saldo de power-ups
            for (var n = 1; n <= MaxLevels; n++)
            {
                yield return $"Level.{n:000}.StarsCount";
                yield return $"Level.{n:000}.Score";
                yield return "Score" + n;
            }
        }

        static IEnumerable<string> StringKeys()
        {
            foreach (var k in FixedStrings) yield return k;
            foreach (var k in extraStrings) yield return k;
        }

        /// Tira uma foto do progresso atual.
        public static SaveSnapshot Capture()
        {
            PlayerPrefs.Save();
            var s = new SaveSnapshot { savedAtTicks = DateTime.UtcNow.Ticks };
            foreach (var key in IntKeys())
                if (PlayerPrefs.HasKey(key)) s.ints.Add(new SaveSnapshot.IntEntry { key = key, value = PlayerPrefs.GetInt(key) });
            foreach (var key in FixedFloats)
                if (PlayerPrefs.HasKey(key)) s.floats.Add(new SaveSnapshot.FloatEntry { key = key, value = PlayerPrefs.GetFloat(key) });
            foreach (var key in StringKeys())
                if (PlayerPrefs.HasKey(key)) s.strings.Add(new SaveSnapshot.StringEntry { key = key, value = PlayerPrefs.GetString(key) });
            return s;
        }

        public static string ToJson(SaveSnapshot s) => JsonUtility.ToJson(s);

        public static SaveSnapshot FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<SaveSnapshot>(json); }
            catch (ArgumentException) { return null; }
        }

        /// Conteúdo da foto sem a hora: serve para saber se algo mudou desde o último envio.
        public static string Fingerprint(SaveSnapshot s)
        {
            var copy = new SaveSnapshot { version = s.version, ints = s.ints, floats = s.floats, strings = s.strings };
            return JsonUtility.ToJson(copy);
        }

        /// Grava a foto no aparelho, SUBSTITUINDO o progresso daqui (chaves que a foto não tem são apagadas).
        /// Depois chame RefreshRuntimeState() para o jogo reler o que já estava na memória.
        public static void Apply(SaveSnapshot s)
        {
            if (s == null) return;
            var ints = new Dictionary<string, int>();
            var floats = new Dictionary<string, float>();
            var strings = new Dictionary<string, string>();
            foreach (var e in s.ints) ints[e.key] = e.value;
            foreach (var e in s.floats) floats[e.key] = e.value;
            foreach (var e in s.strings) strings[e.key] = e.value;

            foreach (var key in IntKeys())
            {
                if (ints.TryGetValue(key, out var v)) PlayerPrefs.SetInt(key, v);
                else PlayerPrefs.DeleteKey(key);
            }
            foreach (var key in FixedFloats)
            {
                if (floats.TryGetValue(key, out var v)) PlayerPrefs.SetFloat(key, v);
                else PlayerPrefs.DeleteKey(key);
            }
            foreach (var key in StringKeys())
            {
                if (strings.TryGetValue(key, out var v)) PlayerPrefs.SetString(key, v);
                else PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
        }

        /// Depois de Apply: refaz o que o jogo guarda em variáveis (moedas, vidas, caches dos sistemas).
        public static void RefreshRuntimeState()
        {
            InitScript.Gems = PlayerPrefs.GetInt("Gems");
            InitScript.lifes = PlayerPrefs.GetInt("Lifes");
            InitScript.RestLifeTimer = PlayerPrefs.GetFloat("RestLifeTimer");
            InitScript.DateOfExit = PlayerPrefs.GetString("DateOfExit", "");
            RoyalAves.Meta.MetaProgress.InvalidateCache();
            RoyalAves.Meta.PetsProgress.InvalidateCache();
            RoyalAves.Meta.PostalEvents.InvalidateCache();
        }

        // ---------------------------------------------------------------------------------------------

        /// Medida de "quanto adiantado": fase mais alta e, no empate, estrelas.
        public static long Progress(SaveSnapshot s)
        {
            if (s == null) return -1;
            s.TryGetInt("ReachedLevel", out var reached);
            s.TryGetInt("OpenLevel", out var open);
            long stars = 0;
            foreach (var e in s.ints)
                if (e.key.EndsWith(".StarsCount", StringComparison.Ordinal)) stars += e.value;
            return (long)Math.Max(reached, open) * 100000L + stars;
        }

        /// Maior que zero: a foto A está mais adiantada. Zero: empate (decida pela data).
        public static int Compare(SaveSnapshot a, SaveSnapshot b)
        {
            var pa = Progress(a); var pb = Progress(b);
            return pa == pb ? 0 : pa > pb ? 1 : -1;
        }
    }
}
