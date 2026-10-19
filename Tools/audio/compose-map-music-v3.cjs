// Trilha do mapa v3: a mesma composicao da v2 (aventura energica, Sol maior, 156 bpm, 32 compassos) escrita como MIDI
// e renderizada por FluidSynth com um SoundFont GM (instrumentos sampleados reais), com a cauda do reverb dobrada no
// inicio para o loop fechar sem emenda.
// Uso: node compose-map-music-v3.cjs <fluidsynth.exe> <soundfont.sf2> <saida.wav> [saida.mid]
const fs = require("fs");
const path = require("path");
const { spawnSync } = require("child_process");
const [fluid, sf2, outWav, outMid] = process.argv.slice(2);
if (!fluid || !sf2 || !outWav) { console.error("uso: node compose-map-music-v3.cjs <fluidsynth.exe> <soundfont.sf2> <saida.wav> [saida.mid]"); process.exit(1); }

const BPM = 156, TPQ = 480, S16 = TPQ / 4, BAR = S16 * 16, BARS = 32;
const NOTE = { C: 0, "C#": 1, D: 2, E: 4, F: 5, "F#": 6, G: 7, A: 9, B: 11 };
const midi = s => { const m = /^([A-G]#?)(\d)$/.exec(s); return 12 * (+m[2] + 1) + NOTE[m[1]]; };
const CH = {
  G: { r: "G2", f: "D3", tri: ["G4", "B4", "D5"], low: ["G3", "B3", "D4"] },
  D: { r: "D3", f: "A3", tri: ["F#4", "A4", "D5"], low: ["D3", "F#3", "A3"] },
  Em: { r: "E2", f: "B2", tri: ["E4", "G4", "B4"], low: ["E3", "G3", "B3"] },
  C: { r: "C3", f: "G3", tri: ["E4", "G4", "C5"], low: ["C3", "E3", "G3"] }
};
const PROG = ["G", "D", "Em", "C", "G", "D", "C", "D", "Em", "C", "G", "D", "Em", "C", "D", "D"];
const MEL = [
  [[0, "D5", 2], [3, "G5", 1], [4, "B5", 2], [7, "A5", 1], [8, "G5", 2], [11, "D5", 1], [12, "E5", 2], [14, "G5", 2]],
  [[0, "F#5", 2], [3, "A5", 1], [4, "D6", 2], [7, "C#6", 1], [8, "A5", 2], [11, "F#5", 1], [12, "D5", 2], [14, "A5", 2]],
  [[0, "G5", 2], [3, "B5", 1], [4, "E6", 2], [7, "D6", 1], [8, "B5", 2], [11, "G5", 1], [12, "B5", 2], [14, "D6", 2]],
  [[0, "C6", 2], [3, "B5", 1], [4, "G5", 2], [7, "E5", 1], [8, "G5", 2], [10, "C6", 2], [12, "E6", 4]],
  [[0, "D5", 2], [3, "G5", 1], [4, "B5", 2], [7, "D6", 1], [8, "B5", 2], [11, "G5", 1], [12, "A5", 2], [14, "B5", 2]],
  [[0, "A5", 2], [3, "F#5", 1], [4, "D6", 2], [7, "E6", 1], [8, "F#6", 2], [11, "E6", 1], [12, "D6", 2], [14, "A5", 2]],
  [[0, "E6", 2], [3, "D6", 1], [4, "C6", 2], [7, "B5", 1], [8, "G5", 2], [11, "A5", 1], [12, "B5", 2], [14, "C6", 2]],
  [[0, "D6", 3], [4, "A5", 1], [5, "B5", 1], [6, "C#6", 1], [7, "D6", 1], [8, "F#6", 4], [12, "E6", 2], [14, "D6", 2]],
  [[0, "B5", 1], [1, "E6", 1], [2, "G6", 2], [4, "E6", 1], [5, "B5", 1], [6, "G5", 2], [8, "B5", 1], [9, "E6", 1], [10, "G6", 2], [12, "F#6", 2], [14, "E6", 2]],
  [[0, "C6", 1], [1, "E6", 1], [2, "G6", 2], [4, "E6", 1], [5, "C6", 1], [6, "G5", 2], [8, "C6", 1], [9, "E6", 1], [10, "A6", 2], [12, "G6", 2], [14, "E6", 2]],
  [[0, "D6", 1], [1, "G6", 1], [2, "D6", 2], [4, "B5", 1], [5, "D6", 1], [6, "G6", 2], [8, "A5", 2], [10, "B5", 2], [12, "D6", 4]],
  [[0, "A5", 1], [1, "D6", 1], [2, "F#6", 2], [4, "D6", 1], [5, "A5", 1], [6, "F#5", 2], [8, "A5", 2], [10, "C#6", 2], [12, "D6", 4]],
  [[0, "E6", 2], [3, "E6", 1], [4, "G6", 2], [7, "F#6", 1], [8, "E6", 2], [11, "D6", 1], [12, "B5", 2], [14, "G5", 2]],
  [[0, "C6", 2], [3, "C6", 1], [4, "E6", 2], [7, "D6", 1], [8, "C6", 2], [11, "B5", 1], [12, "G5", 2], [14, "E5", 2]],
  [[0, "A5", 1], [1, "B5", 1], [2, "C#6", 1], [3, "D6", 1], [4, "E6", 1], [5, "F#6", 1], [6, "G6", 1], [7, "F#6", 1], [8, "F#6", 4], [12, "D6", 2], [14, "A5", 2]],
  [[0, "D6", 2], [3, "D6", 1], [4, "D6", 2], [6, "A5", 1], [7, "F#5", 1], [8, "A5", 1], [9, "D6", 1], [10, "A5", 1], [11, "D6", 1], [12, "F#6", 2], [14, "A5", 2]]
];

// ---- eventos ----
const ev = [];
const note = (ch, tick, m, dur, vel) => { ev.push({ t: tick, o: 1, ch, a: m, b: vel }); ev.push({ t: tick + Math.max(10, dur - 8), o: 0, ch, a: m, b: 0 }); };
// programas GM (0-based): marimba 12, glockenspiel 9, nylon guitar 24, finger bass 33, pizzicato strings 45, string ensemble 48, trumpet 56, french horn 60, brass section 61
const PRG = { 0: 12, 1: 56, 2: 24, 3: 33, 4: 48, 5: 9, 6: 61, 7: 45 };
for (let bar = 0; bar < BARS; bar++) {
  const pass = Math.floor(bar / 16), b16 = bar % 16, t0 = bar * BAR, ch = CH[PROG[b16]];
  const hi = pass === 1, bsec = b16 >= 8, fillBar = b16 % 4 === 3;
  const T = s => t0 + s * S16;
  const jit = () => Math.floor(Math.random() * 7) - 3; // humanizacao leve em velocity
  for (const [step, name, d] of MEL[b16]) {
    const m = midi(name);
    note(0, T(step), m, d * S16, 104 + jit());
    if (bsec || hi) note(1, T(step), m, d * S16, hi ? 92 : 74); // trompete dobra na parte B e na 2a volta
  }
  // baixo pulsante (colcheias)
  const pat = [0, 0, 12, 0, 0, 12, 7, 0];
  for (let e = 0; e < 8; e++) note(3, T(e * 2), midi(ch.r) + pat[e], S16 * 2, e % 2 ? 84 : 100);
  // violao nylon em staccato nos contratempos + pizzicato de cordas dobrando
  for (const s of [2, 6, 10, 14]) {
    ch.tri.forEach((n, i) => note(2, T(s) + i * 6, midi(n), S16 * 1.5, 82 + jit()));
    ch.low.forEach(n => note(7, T(s), midi(n) + 12, S16, 74));
  }
  ch.tri.forEach((n, i) => note(2, T(11) + i * 6, midi(n), S16, 62));
  // bateria (canal 10): bumbo 36, caixa 38, palma 39, hat fechado 42, aberto 46, tom 45/47, crash 49
  const D = (tick, n, v, d = 40) => note(9, tick, n, d, v);
  [0, 4, 8, 12].forEach(s => D(T(s), 36, 112));
  D(T(10), 36, 84); if (bsec) D(T(7), 36, 70);
  [4, 12].forEach(s => { D(T(s), 38, 100); D(T(s), 39, 70); });
  for (let s = 0; s < 16; s++) D(T(s), 42, s % 4 === 2 ? 70 : 50 + (s % 2) * 6, 30);
  [2, 6, 10, 14].forEach(s => D(T(s), 46, 62, 60));
  if (b16 === 0 || b16 === 8) D(t0, 49, 100, 400);
  if (fillBar) { [12, 13, 14, 15].forEach((s, i) => D(T(s), 38, 70 + i * 12, 30)); D(T(14), 47, 92); D(T(15), 45, 96); }
  // 2a volta: stabs de metais, cordas longas, brilho de glockenspiel
  if (hi) {
    ch.low.forEach(n => { note(6, T(0), midi(n) + 12, S16 * 2, 100); note(6, T(10), midi(n) + 12, S16 * 1.5, 88); });
    ch.tri.forEach(n => note(4, T(0), midi(n), BAR - 20, 72));
    [3, 7, 11, 15].forEach((s, i) => note(5, T(s), midi(ch.tri[i % 3]) + 12, S16 * 2, 76));
  } else if (bsec) {
    note(5, T(3), midi(ch.tri[2]) + 12, S16 * 2, 70); note(5, T(11), midi(ch.tri[1]) + 12, S16 * 2, 70);
  }
}
const TOTAL = BARS * BAR, TAIL = BAR * 2;

// ---- escritor MIDI (tipo 1, uma trilha) ----
const vlq = n => { const b = [n & 0x7f]; while ((n >>= 7)) b.unshift((n & 0x7f) | 0x80); return b; };
const bytes = [];
const push = (t, arr) => bytes.push({ t, arr });
push(0, [0xff, 0x51, 3, ...[(60000000 / BPM) >> 16 & 255, (60000000 / BPM) >> 8 & 255, (60000000 / BPM) & 255]]);
push(0, [0xff, 0x58, 4, 4, 2, 24, 8]);
for (const c of Object.keys(PRG)) push(0, [0xc0 | +c, PRG[c]]);
push(0, [0xb0, 7, 108]); push(0, [0xb1, 7, 88]); push(0, [0xb2, 7, 96]); push(0, [0xb3, 7, 112]); push(0, [0xb4, 7, 78]);
push(0, [0xb5, 7, 82]); push(0, [0xb6, 7, 100]); push(0, [0xb7, 7, 90]); push(0, [0xb9, 7, 112]);
// panorama
const PAN = { 0: 54, 1: 74, 2: 40, 3: 64, 4: 64, 5: 92, 6: 50, 7: 86 };
for (const c of Object.keys(PAN)) push(0, [0xb0 | +c, 10, PAN[c]]);
ev.sort((a, b) => a.t - b.t || a.o - b.o);
for (const e of ev) push(e.t, e.o ? [0x90 | e.ch, e.a, e.b] : [0x80 | e.ch, e.a, 0]);
bytes.sort((a, b) => a.t - b.t);
let last = 0; const trk = [];
for (const e of bytes) { trk.push(...vlq(e.t - last), ...e.arr); last = e.t; }
trk.push(...vlq(Math.max(0, TOTAL + TAIL - last)), 0xff, 0x2f, 0);
const hdr = Buffer.from([0x4d, 0x54, 0x68, 0x64, 0, 0, 0, 6, 0, 0, 0, 1, TPQ >> 8, TPQ & 255]);
const th = Buffer.alloc(8); th.write("MTrk", 0); th.writeUInt32BE(trk.length, 4);
const midPath = outMid || outWav.replace(/\.wav$/i, ".mid");
fs.writeFileSync(midPath, Buffer.concat([hdr, th, Buffer.from(trk)]));

// ---- renderizacao ----
const rawWav = outWav + ".raw.wav";
const r = spawnSync(fluid, ["-ni", "-g", "0.9", "-r", "44100", "-R", "1", "-C", "0", "-F", rawWav, sf2, midPath], { encoding: "utf8" });
if (r.status !== 0 || !fs.existsSync(rawWav)) { console.error("fluidsynth falhou", r.status, r.stderr || r.stdout); process.exit(2); }

// ---- le o WAV, dobra a cauda no inicio (loop), normaliza e grava ----
const buf = fs.readFileSync(rawWav);
let p = 12, fmt = null, data = null;
while (p < buf.length - 8) {
  const id = buf.toString("ascii", p, p + 4), sz = buf.readUInt32LE(p + 4);
  if (id === "fmt ") fmt = { ch: buf.readUInt16LE(p + 10), sr: buf.readUInt32LE(p + 12), bits: buf.readUInt16LE(p + 22) };
  if (id === "data") { data = buf.subarray(p + 8, p + 8 + Math.min(sz, buf.length - p - 8)); break; }
  p += 8 + sz + (sz & 1);
}
if (!fmt || !data || fmt.bits !== 16 || fmt.ch !== 2) { console.error("formato inesperado", fmt); process.exit(3); }
const SR = fmt.sr, frames = data.length / 4;
const loopN = Math.round(TOTAL / TPQ * 60 / BPM * SR);
const Lc = new Float32Array(loopN), Rc = new Float32Array(loopN);
for (let i = 0; i < frames; i++) {
  const k = i % loopN; // quem passa do fim volta ao comeco
  Lc[k] += data.readInt16LE(i * 4) / 32768; Rc[k] += data.readInt16LE(i * 4 + 2) / 32768;
}
let peak = 0;
for (let i = 0; i < loopN; i++) peak = Math.max(peak, Math.abs(Lc[i]), Math.abs(Rc[i]));
const g = .86 / peak;
const pcm = Buffer.alloc(loopN * 4);
let sum = 0;
for (let i = 0; i < loopN; i++) {
  const l = Lc[i] * g, rr = Rc[i] * g; sum += l * l;
  pcm.writeInt16LE(Math.round(l * 32767), i * 4); pcm.writeInt16LE(Math.round(rr * 32767), i * 4 + 2);
}
const wh = Buffer.alloc(44);
wh.write("RIFF", 0); wh.writeUInt32LE(36 + pcm.length, 4); wh.write("WAVEfmt ", 8); wh.writeUInt32LE(16, 16);
wh.writeUInt16LE(1, 20); wh.writeUInt16LE(2, 22); wh.writeUInt32LE(SR, 24); wh.writeUInt32LE(SR * 4, 28);
wh.writeUInt16LE(4, 32); wh.writeUInt16LE(16, 34); wh.write("data", 36); wh.writeUInt32LE(pcm.length, 40);
fs.writeFileSync(outWav, Buffer.concat([wh, pcm]));
fs.unlinkSync(rawWav);
console.log(`ok ${outWav}: ${(loopN / SR).toFixed(1)} s, RMS ${(10 * Math.log10(sum / loopN)).toFixed(1)} dBFS, ganho ${g.toFixed(2)}, salto na emenda ${Math.abs(Lc[loopN - 1] * g - Lc[0] * g).toFixed(4)}; midi ${midPath}`);
