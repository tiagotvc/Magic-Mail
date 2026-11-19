// Foto do progresso do jogador: tudo que precisa ir para a nuvem (e voltar) num pacote só.
// Formato: JSON simples (JsonUtility). Só chaves que existem no aparelho entram; valor zero também entra.
using System;
using System.Collections.Generic;

namespace RoyalAves.Save
{
    [Serializable]
    public class SaveSnapshot
    {
        [Serializable] public class IntEntry { public string key; public int value; }
        [Serializable] public class FloatEntry { public string key; public float value; }
        [Serializable] public class StringEntry { public string key; public string value; }

        /// Versão do formato. Suba quando mudar o jeito de guardar; Apply ignora o que não conhece.
        public int version = 1;
        /// Quando a foto foi tirada (UTC, ticks).
        public long savedAtTicks;
        public List<IntEntry> ints = new List<IntEntry>();
        public List<FloatEntry> floats = new List<FloatEntry>();
        public List<StringEntry> strings = new List<StringEntry>();

        public DateTime SavedAtUtc => new DateTime(savedAtTicks, DateTimeKind.Utc);

        public bool TryGetInt(string key, out int value)
        {
            foreach (var e in ints) if (e.key == key) { value = e.value; return true; }
            value = 0; return false;
        }
    }
}
