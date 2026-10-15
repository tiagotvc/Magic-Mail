// Trilha do mapa v4 "Rota do Correio - misterio e aventura": Mi menor/dorico, 118 bpm, 24 compassos (~49 s), loop sem emenda.
// Frase-gancho original no estilo "notas curtas repetidas -> batidas firmes -> nota longa -> corridinha", com harpa, celesta,
// flauta, coral, cordas em tremolo, sinos tubulares, tímpano e bateria leve. MIDI + FluidSynth + SoundFont GM.
// Uso: node compose-map-music-v4.cjs <fluidsynth.exe> <soundfont.sf2> <saida.wav> [saida.mid]
const fs = require("fs");
const { spawnSync } = require("child_process");
const [fluid, sf2, outWav, outMid] = process.argv.slice(2);
if (!fluid || !sf2 || !outWav) { console.error("uso: node compose-map-music-v4.cjs <fluidsynth.exe> <soundfont.sf2> <saida.wav> [saida.mid]"); process.exit(1); }

const BPM = 118, TPQ = 480, S8 = TPQ / 2, S16 = TPQ / 4, BAR = TPQ * 4, BARS = 24;
const NOTE = { C: 0, "C#": 1, D: 2, "D#": 3, E: 4, F: 5, "F#": 6, G: 7, A: 9, B: 11 };
const midi = s => { const m = /^([A-G]#?)(\d)$/.exec(s); return 12 * (+m[2] + 1) + NOTE[m[1]]; };
// acordes (raiz grave, tercas para harpa/cordas/coral)
const CH = {
  Em: { r: "E2", f: "B2", tri: ["E4", "G4", "B4"], arp: ["E3", "B3", "E4", "G4", "B4", "G4", "E4", "B3"] },
  C: { r: "C3", f: "G3", tri: ["E4", "G4", "C5"], arp: ["C3", "G3", "C4", "E4", "G4", "E4", "C4", "G3"] },
  D: { r: "D3", f: "A3", tri: ["F#4", "A4", "D5"], arp: ["D3", "A3", "D4", "F#4", "A4", "F#4", "D4", "A3"] },
  G: { r: "G2", f: "D3", tri: ["D4", "G4", "B4"], arp: ["G2", "D3", "G3", "B3", "D4", "B3", "G3", "D3"] },
  Am: { r: "A2", f: "E3", tri: ["E4", "A4", "C5"], arp: ["A2", "E3", "A3", "C4", "E4", "C4", "A3", "E3"] },
  B: { r: "B2", f: "F#3", tri: ["D#4", "F#4", "B4"], arp: ["B2", "F#3", "B3", "D#4", "F#4", "D#4", "B3", "F#3"] }
};
const PROG = ["Em", "Em", "C", "D", "Em", "C", "D", "B", // A  (gancho)
  "Am", "Em", "C", "G", "Am", "Em", "B", "B", // B  (misterio)
  "Em", "Em", "C", "D", "Em", "C", "D", "B"]; // A' (cheia)
// melodia por compasso: [passo em colcheias (0-7, fracao = 16avos), nota, duracao em colcheias]
const A1 = [ // ta ta ta ... tam tam
  [[0, "E5", 1], [1, "E5", 1], [2, "E5", 1], [4, "B5", 2], [6, "A5", 2]],
  [[0, "G5", 2], [2, "F#5", 2], [4, "E5", 4]],
  [[0, "G5", 1], [1, "G5", 1], [2, "G5", 1], [4, "E6", 2], [6, "D6", 2]],
  [[0, "C6", 2], [2, "B5", 2], [4, "A5", 4]],
  [[0, "E5", 1], [1, "E5", 1], [2, "E5", 1], [4, "B5", 2], [6, "D6", 2]],
  [[0, "E6", 4], [4, "D6", 2], [6, "B5", 2]],
  [[0, "A5", 1], [1, "B5", 1], [2, "C6", 1], [3, "D6", 1], [4, "F#6", 4]], // tatataTAAM (corrida em colcheias, sem pressa)
  [[0, "F#6", 4], [4, "D#6", 2], [6, "B5", 2]]
];
const Bs = [ // parte misteriosa: notas longas, respiro, corridinha suave
  [[0, "A5", 4], [4, "C6", 2], [6, "B5", 2]],
  [[0, "G5", 3], [3, "B5", 1], [4, "E6", 4]],
  [[0, "E6", 2], [2, "D6", 2], [4, "C6", 4]],
  [[0, "B5", 4], [4, "D6", 2], [6, "G6", 2]],
  [[0, "A5", 1], [1, "A5", 1], [2, "A5", 1], [4, "C6", 2], [6, "E6", 2]],
  [[0, "E6", 3], [3, "D6", 1], [4, "B5", 4]],
  [[0, "D#6", 2], [2, "F#6", 2], [4, "B6", 4]],
  [[0, "B6", 4], [4, "A6", 1], [5, "F#6", 1], [6, "D#6", 1], [7, "B5", 1]]
];
const MEL = [...A1, ...Bs, ...A1];

// ---- eventos ----
const ev = [];
const note = (ch, tick, m, dur, vel) => { ev.push({ t: Math.round(tick), o: 1, ch, a: m, b: Math.min(127, Math.max(1, Math.round(vel))) }); ev.push({ t: Math.round(tick + Math.max(10, dur - 8)), o: 0, ch, a: m, b: 0 }); };
// programas GM: 0 flauta 73, 1 celesta 8, 2 harpa 46, 3 contrabaixo pizz (acoustic bass 32), 4 coral aahs 52, 5 cordas tremolo 44, 6 sinos tubulares 14, 7 marimba 12, 8 tímpano 47
const PRG = { 0: 73, 1: 8, 2: 46, 3: 32, 4: 52, 5: 44, 6: 14, 7: 12, 8: 47 };
const VOL = { 0: 108, 1: 80, 2: 92, 3: 104, 4: 84, 5: 74, 6: 76, 7: 78, 8: 96, 9: 100 };
const PAN = { 0: 64, 1: 88, 2: 40, 3: 64, 4: 64, 5: 76, 6: 56, 7: 50, 8: 64 };
for (let bar = 0; bar < BARS; bar++) {
  const t0 = bar * BAR, b8 = bar % 8, sect = Math.floor(bar / 8), ch = CH[PROG[bar]];
  const T = s => t0 + s * S8;
  const full = sect === 2, mystic = sect === 1;
  const j = () => Math.floor(Math.random() * 7) - 3;
  // melodia: flauta + celesta (oitava) ; marimba reforca o ataque no gancho
  for (const [step, name, d] of MEL[bar]) {
    const m = midi(name);
    note(0, T(step), m, d * S8 * .96, 100 + j());
    note(1, T(step), m + 12 > 100 ? m : m + 12, d * S8, mystic ? 70 : 52);
    if (!mystic) note(7, T(step), m, S8, 64);
    if (full) note(6, T(step), m - 12, d * S8, 60); // sinos dobram uma oitava abaixo na volta final
  }
  // harpa: arpejo em colcheias (some um pouco no gancho para dar ar; continuo na parte misteriosa)
  ch.arp.forEach((n, i) => note(2, T(i), midi(n), S8 * 2.2, (mystic ? 78 : full ? 74 : 66) + (i % 4 === 0 ? 8 : 0)));
  // baixo pizzicato: raiz e quinta, meio-tempo (nao corrido)
  note(3, T(0), midi(ch.r), S8 * 2, 100);
  note(3, T(4), midi(ch.f), S8 * 2, 82);
  if (!mystic) { note(3, T(2), midi(ch.r) + 12, S8, 70); note(3, T(6), midi(ch.r), S8, 74); }
  // coral (misticismo) e cordas em tremolo sustentados
  if (mystic || full || b8 >= 4) ch.tri.forEach(n => note(4, t0 + 8, midi(n), BAR - 40, mystic ? 78 : 62));
  if (mystic || full) ch.tri.forEach(n => note(5, t0, midi(n) - 12, BAR - 20, mystic ? 60 : 56));
  // sinos tubulares: toque no primeiro tempo de cada 2 compassos (efeito "mistico")
  if (bar % 2 === 0) note(6, t0, midi(ch.tri[2]) + 12, BAR, mystic ? 80 : 58);
  // percussao (canal 10): bumbo suave 1 e 3, caixa 2 e 4 (so no gancho e na volta cheia), tamborim/pandeiro em colcheias
  const D = (tick, n, v, d = 40) => note(9, tick, n, d, v);
  if (!mystic) {
    D(T(0), 36, 96); D(T(4), 36, 84);
    D(T(2), 38, 62); D(T(6), 38, 70); D(T(6), 39, 50);
    for (let s = 0; s < 8; s++) D(T(s), 54, s % 2 ? 40 : 56, 30); // pandeiro
  } else {
    D(T(0), 36, 70); D(T(4), 36, 60); // so pulso leve no meio
    for (let s = 0; s < 8; s += 2) D(T(s), 53, 34, 30); // sino de prato suave
  }
  if (bar === 0 || bar === 16) D(t0, 49, 96, 300); // crash
  // timpano: final de cada frase (compassos 8, 16, 24) e viradas nos compassos 7/15/23
  if (b8 === 7) { [4, 5, 6, 7].forEach((s, i) => note(8, T(s), midi("E2") + (i === 3 ? 7 : 0), S8, 80 + i * 8)); }
  if (b8 === 0 && bar > 0) note(8, t0, midi("E2"), S8 * 3, 96);
}
const TOTAL = BARS * BAR, TAIL = BAR * 2;

// ---- MIDI ----
const vlq = n => { const b = [n & 0x7f]; while ((n >>= 7)) b.unshift((n & 0x7f) | 0x80); return b; };
const meta = [];
const push = (t, arr) => meta.push({ t, arr });
const us = Math.round(60000000 / BPM);
push(0, [0xff, 0x51, 3, (us >> 16) & 255, (us >> 8) & 255, us & 255]);
push(0, [0xff, 0x58, 4, 4, 2, 24, 8]);
for (const c of Object.keys(PRG)) { push(0, [0xc0 | +c, PRG[c]]); push(0, [0xb0 | +c, 7, VOL[c]]); push(0, [0xb0 | +c, 10, PAN[c]]); }
push(0, [0xb9, 7, VOL[9]]);
ev.sort((a, b) => a.t - b.t || a.o - b.o);
for (const e of ev) push(e.t, e.o ? [0x90 | e.ch, e.a, e.b] : [0x80 | e.ch, e.a, 0]);
meta.sort((a, b) => a.t - b.t);
let last = 0; const trk = [];
for (const e of meta) { trk.push(...vlq(e.t - last), ...e.arr); last = e.t; }
trk.push(...vlq(Math.max(0, TOTAL + TAIL - last)), 0xff, 0x2f, 0);
const hdr = Buffer.from([0x4d, 0x54, 0x68, 0x64, 0, 0, 0, 6, 0, 0, 0, 1, TPQ >> 8, TPQ & 255]);
const th = Buffer.alloc(8); th.write("MTrk", 0); th.writeUInt32BE(trk.length, 4);
const midPath = outMid || outWav.replace(/\.wav$/i, ".mid");
fs.writeFileSync(midPath, Buffer.concat([hdr, th, Buffer.from(trk)]));

// ---- render + loop ----
const rawWav = outWav + ".raw.wav";
const r = spawnSync(fluid, ["-ni", "-g", "0.9", "-r", "44100", "-R", "1", "-C", "1", "-F", rawWav, sf2, midPath], { encoding: "utf8" });
if (r.status !== 0 || !fs.existsSync(rawWav)) { console.error("fluidsynth falhou", r.status, r.stderr || r.stdout); process.exit(2); }
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
for (let i = 0; i < frames; i++) { const k = i % loopN; Lc[k] += data.readInt16LE(i * 4) / 32768; Rc[k] += data.readInt16LE(i * 4 + 2) / 32768; }
let peak = 0;
for (let i = 0; i < loopN; i++) peak = Math.max(peak, Math.abs(Lc[i]), Math.abs(Rc[i]));
const g = .86 / peak, pcm = Buffer.alloc(loopN * 4);
let sum = 0;
for (let i = 0; i < loopN; i++) { const l = Lc[i] * g, rr = Rc[i] * g; sum += l * l; pcm.writeInt16LE(Math.round(l * 32767), i * 4); pcm.writeInt16LE(Math.round(rr * 32767), i * 4 + 2); }
const wh = Buffer.alloc(44);
wh.write("RIFF", 0); wh.writeUInt32LE(36 + pcm.length, 4); wh.write("WAVEfmt ", 8); wh.writeUInt32LE(16, 16);
wh.writeUInt16LE(1, 20); wh.writeUInt16LE(2, 22); wh.writeUInt32LE(SR, 24); wh.writeUInt32LE(SR * 4, 28);
wh.writeUInt16LE(4, 32); wh.writeUInt16LE(16, 34); wh.write("data", 36); wh.writeUInt32LE(pcm.length, 40);
fs.writeFileSync(outWav, Buffer.concat([wh, pcm]));
fs.unlinkSync(rawWav);
console.log(`ok ${outWav}: ${(loopN / SR).toFixed(1)} s, ${BPM} bpm, RMS ${(10 * Math.log10(sum / loopN)).toFixed(1)} dBFS, salto na emenda ${Math.abs(Lc[loopN - 1] * g - Lc[0] * g).toFixed(4)}`);
