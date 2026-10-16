// Trilha original do mapa v2 "Rota do Correio - acao": aventura energica em Sol maior, 156 bpm, 32 compassos (~49 s), loop sem emenda.
// Bateria (bumbo, caixa/palma, hi-hat em 16avos), baixo pulsante, acordes em staccato, lead brilhante + marimba,
// segunda volta mais intensa (stabs de metais, oitava no lead, cordas). Sintetizada em codigo.
// Uso: node compose-map-music-v2.cjs [saida.wav]
const fs = require("fs");
const path = require("path");
const SR = 44100, BPM = 156;
const S16 = 60 / BPM / 4, BAR = S16 * 16, BARS = 32;
const N = Math.round(BAR * BARS * SR);
const L = new Float32Array(N), R = new Float32Array(N);
const hz = m => 440 * Math.pow(2, (m - 69) / 12);
const TAU = Math.PI * 2;

function put(t0, dur, gain, pan, f) {
  const n0 = Math.round(t0 * SR), len = Math.round(dur * SR);
  const gl = gain * Math.cos((pan + 1) * Math.PI / 4), gr = gain * Math.sin((pan + 1) * Math.PI / 4);
  for (let i = 0; i < len; i++) {
    const v = f(i / SR);
    const k = (n0 + i) % N;
    L[k] += v * gl; R[k] += v * gr;
  }
}
const sin = (f, t) => Math.sin(TAU * f * t);
const env = (t, atk, tau) => (t < atk ? t / atk : Math.exp(-(t - atk) / tau));
let seed = 987654;
const rnd = () => (seed = (seed * 1664525 + 1013904223) >>> 0) / 4294967296;

// ---------- instrumentos ----------
function marimba(t0, m, g, pan) {
  const f = hz(m);
  put(t0, .6, g, pan, t => env(t, .002, .16) * (sin(f, t) + .5 * sin(f * 4, t) * Math.exp(-t * 22) + .15 * sin(f * 9.2, t) * Math.exp(-t * 50)));
}
// lead tipo "pluck de metais": serra aditiva com ataque rapido e corpo curto
function lead(t0, m, dur, g, pan) {
  const f = hz(m), d = Math.max(.07, dur * .92);
  put(t0, d + .12, g, pan, t => {
    const a = env(t, .004, d * .9) * (t < d ? 1 : Math.exp(-(t - d) * 40));
    let v = 0;
    for (let h = 1; h <= 6; h++) v += sin(f * h, t) / h * (h <= 3 ? 1 : .6);
    const vib = 1 + .0025 * Math.sin(TAU * 5.5 * t);
    return a * v * .42 * vib;
  });
}
// "violao/ukulele" para acordes em staccato nos contratempos
function strum(t0, notes, g, pan) {
  notes.forEach((m, i) => {
    const f = hz(m), d = i * .006;
    put(t0 + d, .22, g / notes.length, pan, t => env(t, .002, .07) * (sin(f, t) + .45 * sin(f * 2, t) + .25 * sin(f * 3, t) * Math.exp(-t * 18)));
  });
}
function bass(t0, m, dur, g) {
  const f = hz(m);
  put(t0, dur + .05, g, 0, t => {
    const a = env(t, .004, dur * .8) * (t < dur ? 1 : Math.exp(-(t - dur) * 60));
    return a * (sin(f, t) + .55 * sin(f * 2, t) + .25 * sin(f * 3, t) + .12 * Math.sign(sin(f, t)));
  });
}
function stab(t0, notes, dur, g, pan) {
  notes.forEach(m => {
    const f = hz(m);
    put(t0, dur + .1, g / notes.length, pan, t => {
      const a = env(t, .006, dur * .6) * (t < dur ? 1 : Math.exp(-(t - dur) * 50));
      let v = 0;
      for (let h = 1; h <= 8; h++) v += sin(f * h * (1 + .0006 * (h % 2 ? 1 : -1)), t) / h;
      return a * v * .5;
    });
  });
}
function strings(t0, notes, dur, g) {
  notes.forEach((m, i) => {
    const f = hz(m);
    put(t0, dur + .3, g / notes.length, (i - 1) * .4, t => {
      const a = Math.min(1, t / .12) * Math.min(1, Math.max(0, (dur + .3 - t) / .3));
      let v = 0;
      for (let h = 1; h <= 5; h++) v += (sin(f * h, t) + sin(f * h * 1.004, t)) / h;
      return a * v * .35;
    });
  });
}
function glock(t0, m, g, pan) {
  const f = hz(m);
  put(t0, .8, g, pan, t => env(t, .001, .3) * (sin(f, t) + .5 * sin(f * 2.76, t) * Math.exp(-t * 8)));
}
// ---------- bateria ----------
function kick(t0, g) {
  put(t0, .28, g, 0, t => { const f = 50 + 110 * Math.exp(-t * 28); return Math.tanh(1.6 * Math.sin(TAU * f * t)) * Math.exp(-t * 11); });
}
function snare(t0, g) {
  let lp = 0;
  put(t0, .22, g, .05, t => {
    const x = rnd() * 2 - 1; lp += (x - lp) * .35;
    return ((x - lp) * .8 + .5 * sin(190, t) * Math.exp(-t * 28)) * Math.exp(-t * 17);
  });
}
function clap(t0, g) {
  [0, .011, .022].forEach(d => put(t0 + d, .12, g / 2, -.1, t => (rnd() * 2 - 1) * Math.exp(-t * 38)));
}
function hat(t0, g, pan, open) {
  let lp = 0;
  const k = open ? 14 : 75;
  put(t0, open ? .2 : .06, g, pan, t => { const x = rnd() * 2 - 1; lp += (x - lp) * .9; return (x - lp) * Math.exp(-t * k); });
}
function tom(t0, f0, g, pan) { put(t0, .3, g, pan, t => sin(f0 * (1 + .8 * Math.exp(-t * 14)), t) * Math.exp(-t * 11)); }
function crash(t0, g) {
  let lp = 0;
  put(t0, 1.6, g, 0, t => { const x = rnd() * 2 - 1; lp += (x - lp) * .8; return (x - lp) * Math.exp(-t * 2.4); });
}

// ---------- notacao ----------
const NOTE = { C: 0, "C#": 1, D: 2, E: 4, F: 5, "F#": 6, G: 7, A: 9, B: 11 };
const midi = s => { const m = /^([A-G]#?)(\d)$/.exec(s); return 12 * (+m[2] + 1) + NOTE[m[1]]; };
const CH = {
  G: { r: "G2", f: "D3", tri: ["G4", "B4", "D5"], low: ["G3", "B3", "D4"] },
  D: { r: "D3", f: "A3", tri: ["F#4", "A4", "D5"], low: ["D3", "F#3", "A3"] },
  Em: { r: "E2", f: "B2", tri: ["E4", "G4", "B4"], low: ["E3", "G3", "B3"] },
  C: { r: "C3", f: "G3", tri: ["E4", "G4", "C5"], low: ["C3", "E3", "G3"] }
};
const PROG = ["G", "D", "Em", "C", "G", "D", "C", "D", "Em", "C", "G", "D", "Em", "C", "D", "D"];
// melodia em 16avos: [passo, nota, duracao]
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

for (let bar = 0; bar < BARS; bar++) {
  const pass = Math.floor(bar / 16), b16 = bar % 16, t = bar * BAR, ch = CH[PROG[b16]];
  const hi = pass === 1, bsec = b16 >= 8, last = b16 === 15, fillBar = b16 % 4 === 3;
  const T = s => t + s * S16;

  // melodia: lead + marimba (ataque percussivo); 2a volta com oitava acima
  for (const [step, name, d] of MEL[b16]) {
    const m = midi(name), tt = T(step), dur = d * S16;
    lead(tt, m, dur, hi ? .26 : .30, -.1);
    marimba(tt, m, .20, .1);
    if (hi) lead(tt, m + 12 > 96 ? m : m + 12, dur, .09, .25);
  }
  // baixo pulsante em colcheias (raiz / oitava / quinta), com sincopa
  const pat = [0, 0, 12, 0, 0, 12, 7, 0]; // semitons acima da raiz por colcheia
  for (let e = 0; e < 8; e++) bass(T(e * 2), midi(ch.r) + pat[e], S16 * 1.6, .34);
  // acordes em staccato nos contratempos (colcheias fracas) e uma batida extra
  for (const s of [2, 6, 10, 14]) strum(T(s), ch.tri.map(midi), .30, .3);
  strum(T(11), ch.tri.map(midi), .16, .25);
  // bateria
  [0, 4, 8, 12].forEach(s => kick(T(s), .62));
  kick(T(10), .38);
  if (bsec) kick(T(7), .30);
  [4, 12].forEach(s => { snare(T(s), .38); clap(T(s), .22); });
  for (let s = 0; s < 16; s++) hat(T(s) + (s % 2 ? S16 * .05 : 0), s % 4 === 2 ? .10 : .055, s % 2 ? .3 : -.3, false);
  [2, 6, 10, 14].forEach(s => hat(T(s), .09, .35, true));
  if (b16 === 0) crash(t, .22);
  if (b16 === 8) crash(t, .26);
  if (fillBar) { // virada de bateria no fim de cada 4 compassos
    [12, 13, 14, 15].forEach((s, i) => snare(T(s) , .20 + i * .06));
    tom(T(14), 140, .30, -.3); tom(T(15), 100, .30, .3);
  }
  if (last) crash(t + BAR - S16 * 0, .0); // (crash do compasso 1 da volta seguinte entra pelo fim do buffer)
  // segunda volta: metais em stab, cordas e brilho de glock
  if (hi) {
    stab(T(0), ch.low.map(midi).map(m => m + 12), S16 * 1.5, .46, -.2);
    stab(T(10), ch.low.map(midi).map(m => m + 12), S16 * 1.2, .34, .2);
    strings(t, ch.tri.map(midi), BAR, .16);
    [3, 7, 11, 15].forEach((s, i) => glock(T(s), midi(ch.tri[i % 3]) + 12, .08, .4));
    for (let s = 1; s < 16; s += 2) hat(T(s), .04, -.4, false);
  } else if (bsec) {
    glock(T(3), midi(ch.tri[2]) + 12, .07, .4); glock(T(11), midi(ch.tri[1]) + 12, .07, .4);
  }
}

// reverb curto e circular
function reverb(buf, mix) {
  const combs = [[.0229, .66], [.0293, .64], [.0347, .62], [.0397, .60]];
  const wet = new Float32Array(N);
  for (const [dt, g] of combs) {
    const d = Math.round(dt * SR), y = new Float32Array(N);
    for (let pass = 0; pass < 3; pass++) for (let i = 0; i < N; i++) y[i] = buf[i] + g * y[(i - d + N) % N];
    for (let i = 0; i < N; i++) wet[i] += y[i] * .25;
  }
  const out = new Float32Array(N);
  for (let i = 0; i < N; i++) out[i] = buf[i] * (1 - mix * .4) + wet[i] * mix;
  return out;
}
const L2 = reverb(L, .14), R2 = reverb(R, .14);
const delay = Math.round(.009 * SR), R3 = new Float32Array(N);
for (let i = 0; i < N; i++) R3[i] = R2[(i - delay + N) % N];

// limitador suave + normalizacao para ~-1.5 dBFS
let peak = 0;
for (let i = 0; i < N; i++) peak = Math.max(peak, Math.abs(L2[i]), Math.abs(R3[i]));
const soft = x => Math.tanh(x * 1.15) / Math.tanh(1.15);
let peak2 = 0;
for (let i = 0; i < N; i++) { L2[i] = soft(L2[i] / peak * 1.25); R3[i] = soft(R3[i] / peak * 1.25); peak2 = Math.max(peak2, Math.abs(L2[i]), Math.abs(R3[i])); }
const gain = .86 / peak2;
const pcm = Buffer.alloc(N * 4);
const q = v => Math.max(-32768, Math.min(32767, Math.round(v * gain * 32767)));
for (let i = 0; i < N; i++) { pcm.writeInt16LE(q(L2[i]), i * 4); pcm.writeInt16LE(q(R3[i]), i * 4 + 2); }
const hdr = Buffer.alloc(44);
hdr.write("RIFF", 0); hdr.writeUInt32LE(36 + pcm.length, 4); hdr.write("WAVEfmt ", 8); hdr.writeUInt32LE(16, 16);
hdr.writeUInt16LE(1, 20); hdr.writeUInt16LE(2, 22); hdr.writeUInt32LE(SR, 24); hdr.writeUInt32LE(SR * 4, 28);
hdr.writeUInt16LE(4, 32); hdr.writeUInt16LE(16, 34); hdr.write("data", 36); hdr.writeUInt32LE(pcm.length, 40);
const out = process.argv[2] || path.join(__dirname, "mapa-aventura-v2.wav");
fs.writeFileSync(out, Buffer.concat([hdr, pcm]));
let s = 0;
for (let i = 0; i < N; i++) s += L2[i] * L2[i] * gain * gain;
console.log(`ok ${out}: ${(N / SR).toFixed(1)} s, ${BPM} bpm, RMS ${(10 * Math.log10(s / N)).toFixed(1)} dBFS, salto na emenda ${Math.abs(L2[N - 1] * gain - L2[0] * gain).toFixed(4)}`);
