// An original adventure theme for Magic Mail, composed and played by code, in three parts like the brief:
//   A  adventure and action  — A minor, 132 BPM: marimba eighths, bass and timpani on the short-short-long motif, shaker,
//                              a glockenspiel melody that comes back an octave higher the second time
//   B  calm                  — C major, 88 BPM: soft pad, harp arpeggio, a singing line
//   C  serious               — D minor, 100 BPM: low pad, slow bass pulses, timpani, a lead line that ends on a long chord
// It makes a stereo WAV (a small synth: pluck, bell, pad, bass, timpani and shaker, with a little reverb) and a MIDI file
// with the same notes, to be opened in any DAW and played with real instruments. Pure C# (no Unity types), so it can run
// anywhere; MagicMusicMenu is the Unity menu that writes the files into the project.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MagicMail.Audio
{
    public static class MagicMusicComposer
    {
        public const int SampleRate = 44100;

        public enum Inst { Marimba, Glock, Harp, Pad, Bass, Lead, Timpani, Shaker }

        class Section { public double Bpm, StartBeat; public int Bars; }
        class Note { public Inst Inst; public int Midi; public double Beat, Beats; public float Velocity = 0.8f; }

        // ---- the composition -------------------------------------------------------------------------------------------

        static readonly List<Section> sections = new List<Section>();
        static readonly List<Note> notes = new List<Note>();

        // MIDI note number of a name like "C#5" or "Bb4" (C4 = 60).
        static int M(string name)
        {
            var map = new Dictionary<char, int> { ['C'] = 0, ['D'] = 2, ['E'] = 4, ['F'] = 5, ['G'] = 7, ['A'] = 9, ['B'] = 11 };
            var semitone = map[name[0]];
            var i = 1;
            if (name[i] == '#') { semitone++; i++; }
            else if (name[i] == 'b') { semitone--; i++; }
            return (int.Parse(name.Substring(i)) + 1) * 12 + semitone;
        }

        // Notes one after the other from `beat`: (name, length in beats); "-" is a rest.
        static void Line(Inst inst, double beat, float velocity, params (string note, double beats)[] items)
        {
            foreach (var (name, beats) in items)
            {
                if (name != "-") notes.Add(new Note { Inst = inst, Midi = M(name), Beat = beat, Beats = beats, Velocity = velocity });
                beat += beats;
            }
        }

        static void Add(Inst inst, int midi, double beat, double beats, float velocity) =>
            notes.Add(new Note { Inst = inst, Midi = midi, Beat = beat, Beats = beats, Velocity = velocity });

        static (string, double)[] Bar(params (string, double)[] items) => items;

        static void Compose()
        {
            sections.Clear();
            notes.Clear();
            double beat = 0;

            // ---- A: adventure and action (A minor, 132 BPM): 8 bars, played twice ----
            sections.Add(new Section { Bpm = 132, StartBeat = beat, Bars = 16 });
            // root (the chord's own octave), is it minor? — Am Am F G | Am C F E
            var chordsA = new[] { (57, true), (57, true), (53, false), (55, false), (57, true), (60, false), (53, false), (52, false) };
            // The motif, short-short-long ("tam tam taram"), bar by bar.
            var melodyA = new[]
            {
                Bar(("E5", .5), ("E5", .5), ("A5", 1), ("G5", .5), ("G5", .5), ("E5", 1)),
                Bar(("E5", .5), ("E5", .5), ("A5", 1), ("C6", .5), ("B5", .5), ("A5", 1)),
                Bar(("F5", .5), ("F5", .5), ("A5", 1), ("G5", .5), ("F5", .5), ("C5", 1)),
                Bar(("G5", .5), ("G5", .5), ("B5", 1), ("A5", .5), ("G5", .5), ("D5", 1)),
                Bar(("E5", .5), ("E5", .5), ("A5", 1), ("C6", .5), ("C6", .5), ("E6", 1)),
                Bar(("E5", .5), ("E5", .5), ("G5", 1), ("C6", .5), ("B5", .5), ("G5", 1)),
                Bar(("F5", .5), ("F5", .5), ("A5", 1), ("C6", .5), ("A5", .5), ("F5", 1)),
                Bar(("E5", .5), ("E5", .5), ("G#5", 1), ("B5", .5), ("G#5", .5), ("E5", 1)),
            };
            for (var pass = 0; pass < 2; pass++)
            for (var bar = 0; bar < 8; bar++)
            {
                var b = beat + (pass * 8 + bar) * 4;
                var (root, minor) = chordsA[bar];
                var fifth = root + 7;
                // Marimba eighths: root, fifth, octave, fifth, twice per bar.
                int[] pattern = { root, fifth, root + 12, fifth, root, fifth, root + 12, fifth };
                for (var i = 0; i < 8; i++) Add(Inst.Marimba, pattern[i], b + i * .5, .45, i % 2 == 0 ? .7f : .5f);
                // The bass answers the motif: short, short, long, short, short, long.
                var low = root - 12;
                Add(Inst.Bass, low, b, .45, .9f); Add(Inst.Bass, low, b + .5, .45, .7f); Add(Inst.Bass, low, b + 1, .9, .9f);
                Add(Inst.Bass, low, b + 2, .45, .9f); Add(Inst.Bass, low, b + 2.5, .45, .7f); Add(Inst.Bass, low + 7, b + 3, .9, .85f);
                // Timpani on 1 and 3, shaker on every eighth with the off-beats louder.
                Add(Inst.Timpani, 45, b, .5, 1f); Add(Inst.Timpani, 45, b + 2, .5, .85f);
                for (var i = 0; i < 8; i++) Add(Inst.Shaker, 0, b + i * .5, .25, i % 2 == 0 ? .35f : .6f);
                // The melody, with the second pass doubled an octave up for lift.
                Line(Inst.Glock, b, .8f, melodyA[bar]);
                if (pass == 1) Line(Inst.Glock, b, .45f, melodyA[bar].Select(x => (x.Item1 == "-" ? "-" : Up(x.Item1), x.Item2)).ToArray());
                _ = minor;
            }
            beat += 16 * 4;

            // ---- B: calm (C major, 88 BPM): 8 bars ----
            sections.Add(new Section { Bpm = 88, StartBeat = beat, Bars = 8 });
            var chordsB = new[] { (48, false), (45, true), (53, false), (55, false), (48, false), (52, true), (53, false), (55, false) };
            var melodyB = new[]
            {
                Bar(("E5", 2), ("G5", 1), ("E5", 1)),
                Bar(("E5", 2), ("C5", 1), ("A4", 1)),
                Bar(("A5", 2), ("G5", 1), ("F5", 1)),
                Bar(("D5", 2), ("B4", 1), ("D5", 1)),
                Bar(("E5", 2), ("G5", 1), ("C6", 1)),
                Bar(("B5", 2), ("G5", 1), ("E5", 1)),
                Bar(("A5", 2), ("C6", 1), ("A5", 1)),
                Bar(("B5", 3), ("D6", 1)),
            };
            for (var bar = 0; bar < 8; bar++)
            {
                var b = beat + bar * 4;
                var (root, minor) = chordsB[bar];
                var third = root + (minor ? 3 : 4);
                var fifth = root + 7;
                foreach (var n in new[] { root, third, fifth }) Add(Inst.Pad, n, b, 4.2, .5f);
                int[] arpeggio = { root + 12, third + 12, fifth + 12, root + 24, fifth + 12, third + 12, fifth + 12, third + 12 };
                for (var i = 0; i < 8; i++) Add(Inst.Harp, arpeggio[i], b + i * .5, .5, .55f);
                Add(Inst.Bass, root - 12, b, 3.5, .6f);
                Line(Inst.Glock, b, .55f, melodyB[bar]);
            }
            beat += 8 * 4;

            // ---- C: serious (D minor, 100 BPM): 8 bars and a long last chord ----
            sections.Add(new Section { Bpm = 100, StartBeat = beat, Bars = 10 });
            var chordsC = new[] { (50, true), (50, true), (46, false), (45, false), (50, true), (43, true), (45, false), (50, true) };
            var melodyC = new[]
            {
                Bar(("D5", 1), ("F5", 1), ("A5", 2)),
                Bar(("G5", 1.5), ("F5", .5), ("E5", 1), ("D5", 1)),
                Bar(("D5", 1), ("F5", 1), ("Bb5", 2)),
                Bar(("C#5", 1), ("E5", 1), ("A5", 1), ("G5", 1)),
                Bar(("D5", 1), ("F5", 1), ("A5", 1), ("D6", 1)),
                Bar(("Bb5", 1.5), ("A5", .5), ("G5", 1), ("D5", 1)),
                Bar(("E5", 1), ("G5", 1), ("C#6", 2)),
                Bar(("D6", 4)),
            };
            for (var bar = 0; bar < 8; bar++)
            {
                var b = beat + bar * 4;
                var (root, minor) = chordsC[bar];
                var third = root + (minor ? 3 : 4);
                var fifth = root + 7;
                foreach (var n in new[] { root - 12, root, third, fifth }) Add(Inst.Pad, n, b, 4.3, .55f);
                Add(Inst.Bass, root - 12, b, 1.8, .85f); Add(Inst.Bass, root - 12, b + 2, 1.8, .75f);
                int[] ostinato = { root, fifth, root + 12, fifth };
                for (var i = 0; i < 4; i++) Add(Inst.Marimba, ostinato[i], b + i, .9, .45f);
                Add(Inst.Timpani, 38, b, .5, .95f);
                if (bar % 2 == 1) Add(Inst.Timpani, 38, b + 3, .5, .7f);
                Line(Inst.Lead, b, .7f, melodyC[bar]);
            }
            // The last chord, left to ring.
            var end = beat + 8 * 4;
            foreach (var n in new[] { 38, 50, 53, 57, 62 }) Add(Inst.Pad, n, end, 7.5, .6f);
            Add(Inst.Bass, 38, end, 6, .8f);
            Add(Inst.Timpani, 38, end, .5, 1f);
        }

        static string Up(string name)
        {
            var digits = new string(name.Where(char.IsDigit).ToArray());
            return name.Substring(0, name.Length - digits.Length) + (int.Parse(digits) + 1);
        }

        // Beats to seconds, with each section at its own tempo.
        static double Seconds(double beat)
        {
            double seconds = 0;
            foreach (var s in sections)
            {
                var end = s.StartBeat + s.Bars * 4;
                var inside = Math.Min(beat, end) - s.StartBeat;
                if (inside <= 0) break;
                seconds += inside * 60.0 / s.Bpm;
                if (beat <= end) break;
            }
            return seconds;
        }

        // ---- WAV --------------------------------------------------------------------------------------------------------

        /// Plays the composition. Returns 16-bit stereo samples at SampleRate and its length in seconds.
        public static (short[] stereo, double seconds) RenderWav()
        {
            Compose();
            var last = notes.Max(n => Seconds(n.Beat + n.Beats));
            var total = last + 3.0;                                      // room for the reverb and the last bell to ring out
            var frames = (int)(total * SampleRate);
            var left = new float[frames];
            var right = new float[frames];
            var rng = new Random(11);

            foreach (var n in notes) Play(n, left, right, rng);
            Reverb(left, right);

            // Normalise a little under full scale, and fade the very end.
            var peak = 0f;
            for (var i = 0; i < frames; i++) peak = Math.Max(peak, Math.Max(Math.Abs(left[i]), Math.Abs(right[i])));
            var gain = peak > 0f ? 0.89f / peak : 1f;
            var fade = 2 * SampleRate;
            var pcm = new short[frames * 2];
            for (var i = 0; i < frames; i++)
            {
                var f = i > frames - fade ? (frames - i) / (float)fade : 1f;
                pcm[i * 2] = (short)Math.Round(Math.Max(-1f, Math.Min(1f, left[i] * gain * f)) * 32767f);
                pcm[i * 2 + 1] = (short)Math.Round(Math.Max(-1f, Math.Min(1f, right[i] * gain * f)) * 32767f);
            }
            return (pcm, total);
        }

        public static void WriteWav(string path, short[] stereo)
        {
            using (var w = new BinaryWriter(File.Create(path)))
            {
                var bytes = stereo.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + bytes); w.Write(new[] { 'W', 'A', 'V', 'E' });
                w.Write(new[] { 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)2);
                w.Write(SampleRate); w.Write(SampleRate * 4); w.Write((short)4); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(bytes);
                foreach (var s in stereo) w.Write(s);
            }
        }

        static double Hz(int midi) => 440.0 * Math.Pow(2.0, (midi - 69) / 12.0);

        // One note into the mix: each instrument has its own shape, level and place in the stereo field.
        static void Play(Note n, float[] left, float[] right, Random rng)
        {
            var start = Seconds(n.Beat);
            var held = Seconds(n.Beat + n.Beats) - start;
            double level, pan, tail;
            switch (n.Inst)
            {
                case Inst.Marimba: level = .30; pan = -.25; tail = Math.Min(held + .25, 1.1); break;
                case Inst.Glock: level = .20; pan = .30; tail = 1.6; break;
                case Inst.Harp: level = .26; pan = .15; tail = 1.3; break;
                case Inst.Pad: level = .12; pan = 0; tail = held + .9; break;
                case Inst.Bass: level = .40; pan = 0; tail = held + .2; break;
                case Inst.Lead: level = .24; pan = .1; tail = held + .35; break;
                case Inst.Timpani: level = .55; pan = -.1; tail = 1.4; break;
                default: level = .10; pan = .35; tail = .14; break;      // shaker
            }
            var first = (int)(start * SampleRate);
            var count = (int)(tail * SampleRate);
            var f0 = n.Inst == Inst.Shaker || n.Inst == Inst.Timpani ? 0 : Hz(n.Midi);
            var theta = (pan + 1) * Math.PI / 4;
            double lg = Math.Cos(theta) * level * n.Velocity, rg = Math.Sin(theta) * level * n.Velocity;
            double phase = 0, hp = 0;

            for (var i = 0; i < count && first + i < left.Length; i++)
            {
                var t = i / (double)SampleRate;
                var attack = Math.Min(1.0, t / 0.004);
                double s;
                switch (n.Inst)
                {
                    case Inst.Marimba:
                        s = (Math.Sin(2 * Math.PI * f0 * t) * Math.Exp(-t * 5.5) + .35 * Math.Sin(2 * Math.PI * f0 * 3.9 * t) * Math.Exp(-t * 18)) * attack;
                        break;
                    case Inst.Glock:
                        s = (Math.Sin(2 * Math.PI * f0 * t) * Math.Exp(-t * 3.2) + .45 * Math.Sin(2 * Math.PI * f0 * 2.76 * t) * Math.Exp(-t * 5)
                             + .2 * Math.Sin(2 * Math.PI * f0 * 5.4 * t) * Math.Exp(-t * 9)) * attack;
                        break;
                    case Inst.Harp:
                        s = (Math.Sin(2 * Math.PI * f0 * t) * Math.Exp(-t * 3) + .3 * Math.Sin(4 * Math.PI * f0 * t) * Math.Exp(-t * 6)) * attack;
                        break;
                    case Inst.Pad:
                    {
                        var env = Math.Min(1.0, t / .45) * Math.Min(1.0, Math.Max(0.0, (held + .9 - t) / .9));
                        s = 0;
                        for (var k = 1; k <= 6; k++)
                            s += (Math.Sin(2 * Math.PI * f0 * 1.003 * k * t) + Math.Sin(2 * Math.PI * f0 * .997 * k * t)) / (2.0 * k);
                        s *= env;
                        break;
                    }
                    case Inst.Bass:
                        s = (Math.Sin(2 * Math.PI * f0 * t) + .4 * Math.Sin(4 * Math.PI * f0 * t)) * Math.Exp(-t * 2.5) * attack;
                        break;
                    case Inst.Lead:
                    {
                        var vibrato = 1 + .005 * Math.Sin(2 * Math.PI * 5.5 * t) * Math.Min(1.0, t / .4);
                        var env = Math.Min(1.0, t / .08) * Math.Min(1.0, Math.Max(0.0, (held + .35 - t) / .35));
                        s = (Math.Sin(2 * Math.PI * f0 * vibrato * t) + .3 * Math.Sin(4 * Math.PI * f0 * vibrato * t)) * env;
                        break;
                    }
                    case Inst.Timpani:
                    {
                        var freq = 60 * (1 + .6 * Math.Exp(-t * 12));
                        phase += 2 * Math.PI * freq / SampleRate;
                        s = Math.Sin(phase) * Math.Exp(-t * 4.5) + (rng.NextDouble() * 2 - 1) * Math.Exp(-t * 60) * .3;
                        break;
                    }
                    default: // shaker: bright noise
                    {
                        var noise = rng.NextDouble() * 2 - 1;
                        s = (noise - hp) * Math.Exp(-t * 45);
                        hp = noise;
                        break;
                    }
                }
                left[first + i] += (float)(s * lg);
                right[first + i] += (float)(s * rg);
            }
        }

        // A small room: four combs and two all-passes on the sum of both channels, added back at a low level.
        static void Reverb(float[] left, float[] right)
        {
            var n = left.Length;
            var wet = new float[n];
            int[] combs = { 1557, 1617, 1491, 1422 };
            foreach (var d in combs)
            {
                var buffer = new float[d];
                for (var i = 0; i < n; i++)
                {
                    var input = (left[i] + right[i]) * .5f;
                    var y = buffer[i % d];
                    buffer[i % d] = input + y * .78f;
                    wet[i] += y * .25f;
                }
            }
            foreach (var d in new[] { 556, 441 })
            {
                var buffer = new float[d];
                for (var i = 0; i < n; i++)
                {
                    var delayed = buffer[i % d];
                    var input = wet[i];
                    wet[i] = -input + delayed;
                    buffer[i % d] = input + delayed * .5f;
                }
            }
            for (var i = 0; i < n; i++) { left[i] += wet[i] * .35f; right[i] += wet[i] * .35f; }
        }

        // ---- MIDI -------------------------------------------------------------------------------------------------------

        // The same notes as a Standard MIDI File (type 1): a tempo track and one track per instrument, drums on channel 10.
        public static void WriteMidi(string path)
        {
            if (notes.Count == 0) Compose();
            const int ppq = 480;
            var tracks = new List<byte[]>();

            var tempo = new List<byte>();
            foreach (var s in sections)
            {
                var micro = (int)(60000000.0 / s.Bpm);
                Delta(tempo, (long)(s.StartBeat * ppq) - tempoTick);
                tempoTick = (long)(s.StartBeat * ppq);
                tempo.AddRange(new byte[] { 0xFF, 0x51, 0x03, (byte)(micro >> 16), (byte)(micro >> 8), (byte)micro });
            }
            tempo.AddRange(new byte[] { 0x00, 0xFF, 0x2F, 0x00 });
            tempoTick = 0;
            tracks.Add(tempo.ToArray());

            var program = new Dictionary<Inst, byte>
            {
                [Inst.Marimba] = 12, [Inst.Glock] = 9, [Inst.Harp] = 46, [Inst.Pad] = 89, [Inst.Bass] = 33, [Inst.Lead] = 82, [Inst.Timpani] = 47,
            };
            var channel = 0;
            foreach (var inst in Enum.GetValues(typeof(Inst)).Cast<Inst>())
            {
                var mine = notes.Where(n => n.Inst == inst).ToList();
                if (mine.Count == 0) continue;
                var isDrum = inst == Inst.Shaker;
                var ch = isDrum ? 9 : channel++;
                if (channel == 9) channel++;
                var events = new List<(long tick, int order, byte[] data)>();
                foreach (var n in mine)
                {
                    var key = (byte)(isDrum ? 70 : n.Midi);                 // 70 = maracas
                    var vel = (byte)Math.Max(1, Math.Min(127, (int)(n.Velocity * 110)));
                    events.Add(((long)(n.Beat * ppq), 1, new[] { (byte)(0x90 | ch), key, vel }));
                    events.Add(((long)((n.Beat + n.Beats) * ppq), 0, new[] { (byte)(0x80 | ch), key, (byte)0 }));
                }
                var data = new List<byte>();
                if (!isDrum) { Delta(data, 0); data.AddRange(new[] { (byte)(0xC0 | ch), program[inst] }); }
                long previous = 0;
                foreach (var e in events.OrderBy(e => e.tick).ThenBy(e => e.order))
                {
                    Delta(data, e.tick - previous);
                    previous = e.tick;
                    data.AddRange(e.data);
                }
                data.AddRange(new byte[] { 0x00, 0xFF, 0x2F, 0x00 });
                tracks.Add(data.ToArray());
            }

            using (var w = new BinaryWriter(File.Create(path)))
            {
                w.Write(new[] { 'M', 'T', 'h', 'd' }); BigEndian(w, 6); BigEndian16(w, 1); BigEndian16(w, (short)tracks.Count); BigEndian16(w, ppq);
                foreach (var t in tracks) { w.Write(new[] { 'M', 'T', 'r', 'k' }); BigEndian(w, t.Length); w.Write(t); }
            }
        }

        static long tempoTick;

        static void Delta(List<byte> into, long value)
        {
            var buffer = new Stack<byte>();
            buffer.Push((byte)(value & 0x7F));
            while ((value >>= 7) > 0) buffer.Push((byte)((value & 0x7F) | 0x80));
            into.AddRange(buffer);
        }

        static void BigEndian(BinaryWriter w, int v) { w.Write((byte)(v >> 24)); w.Write((byte)(v >> 16)); w.Write((byte)(v >> 8)); w.Write((byte)v); }
        static void BigEndian16(BinaryWriter w, short v) { w.Write((byte)(v >> 8)); w.Write((byte)v); }
    }
}
