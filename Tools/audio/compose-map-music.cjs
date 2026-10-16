// Trilha original do mapa ("Rota do Correio"): aventura leve em Sol maior, 128 bpm, 16 compassos (~30 s), loop sem emenda.
// Sintetizada em codigo (marimba, pizzicato, glockenspiel, pad, shaker, woodblock) + reverb circular.
// Uso: node compose-map-music.cjs [saida.wav]
const fs = require("fs");
const path = require("path");
const SR = 44100, BPM = 128;
const EIGHTH = 60 / BPM / 2, BAR = EIGHTH * 8, BARS = 16;
const N = Math.round(BAR * BARS * SR);
const L = new Float32Array(N), R = new Float32Array(N);
const hz = m => 440 * Math.pow(2, (m - 69) / 12);

// Mistura um sinal mono (gerado por f(t)) no ponto 't0', com dobra circular para o loop fechar.
function put(t0, dur, gain, pan, f) {
  const n0 = Math.round(t0 * SR), len = Math.round(dur * SR);
  const gl = gain * Math.cos((pan + 1) * Math.PI / 4), gr = gain * Math.sin((pan + 1) * Math.PI / 4);
  for (let i = 0; i < len; i++) {
    const v = f(i / SR);
    const k = (n0 + i) % N;
    L[k] += v * gl; R[k] += v * gr;
  }
}
const sin = (f, t) => Math.sin(2 * Math.PI * f * t);
const env = (t, atk, tau) => (t < atk ? t / atk : Math.exp(-(t - atk) / tau));

function marimba(t0, m, d, g, pan) {
  const f = hz(m), tau = Math.min(.22 + d * .1, .5);
  put(t0, tau * 6 + .1, g, pan, t => env(t, .002, tau) * (sin(f, t) + .45 * sin(f * 4, t) * Math.exp(-t * 18) + .15 * sin(f * 9.2, t) * Math.exp(-t * 40)));
}
function glock(t0, m, g, pan) {
  const f = hz(m);
  put(t0, 1.4, g, pan, t => env(t, .001, .5) * (sin(f, t) + .5 * sin(f * 2.76, t) * Math.exp(-t * 6) + .25 * sin(f * 5.4, t) * Math.exp(-t * 10)));
}
function pizz(t0, m, g, pan) {
  const f = hz(m);
  put(t0, .6, g, pan, t => env(t, .003, .13) * (sin(f, t) + .35 * sin(f * 2, t) * Math.exp(-t * 14) + .12 * sin(f * 3, t) * Math.exp(-t * 25)));
}
function pad(t0, notes, dur, g) {
  notes.forEach((m, i) => {
    const f = hz(m);
    put(t0, dur + .5, g / notes.length, (i - 1) * .35, t => {
      const a = Math.min(1, t / .5) * Math.min(1, Math.max(0, (dur + .5 - t) / .5));
      return a * (sin(f, t) + .5 * sin(f * 2.003, t) * .5 + sin(f * .9985, t) * .6) * .5;
    });
  });
}
let seed = 12345;
const rnd = () => (seed = (seed * 1664525 + 1013904223) >>> 0) / 4294967296;
function shaker(t0, g, pan) {
  let lp = 0;
  put(t0, .06, g, pan, t => { const x = rnd() * 2 - 1; lp += (x - lp) * .85; return (x - lp) * Math.exp(-t * 90); });
}
function wood(t0, g, pan) { put(t0, .12, g, pan, t => env(t, .001, .025) * sin(1250, t)); }

// notacao: nome + oitava ("G4")
const NOTE = { C: 0, "C#": 1, D: 2, E: 4, F: 5, "F#": 6, G: 7, A: 9, B: 11 };
const midi = s => { const m = /^([A-G]#?)(\d)$/.exec(s); return 12 * (+m[2] + 1) + NOTE[m[1]]; };

// acordes por compasso (raiz grave, quinta, tercas para pad/arpejo)
const CH = {
  G: { r: "G2", f: "D3", t: ["G3", "B3", "D4"], arp: ["G5", "B5", "D6", "B5"] },
  Em: { r: "E2", f: "B2", t: ["E3", "G3", "B3"], arp: ["E5", "G5", "B5", "G5"] },
  C: { r: "C3", f: "G3", t: ["C3", "E3", "G3"], arp: ["C5", "E5", "G5", "E5"] },
  D: { r: "D3", f: "A3", t: ["D3", "F#3", "A3"], arp: ["D5", "F#5", "A5", "F#5"] }
};
const PROG = ["G", "G", "C", "D", "G", "Em", "C", "D", "C", "D", "G", "Em", "C", "D", "G", "D"];

// melodia: [passo (0-7 colcheias), nota, duracao em colcheias]
const MEL = [
  [[0, "G4", 1], [1, "B4", 1], [2, "D5", 2], [4, "B4", 1], [5, "D5", 1], [6, "G5", 2]],
  [[0, "F#5", 1], [1, "E5", 1], [2, "D5", 2], [4, "B4", 2], [6, "A4", 1], [7, "G4", 1]],
  [[0, "E5", 1], [1, "G5", 1], [2, "C6", 2], [4, "G5", 1], [5, "E5", 1], [6, "G5", 2]],
  [[0, "F#5", 1], [1, "A5", 1], [2, "D6", 2], [4, "A5", 1], [5, "F#5", 1], [6, "A5", 2]],
  [[0, "B5", 1], [1, "A5", 1], [2, "G5", 2], [4, "D5", 1], [5, "G5", 1], [6, "B5", 2]],
  [[0, "E5", 1], [1, "G5", 1], [2, "B5", 2], [4, "G5", 1], [5, "E5", 1], [6, "B4", 2]],
  [[0, "C5", 1], [1, "E5", 1], [2, "G5", 1], [3, "E5", 1], [4, "C6", 3], [7, "B5", 1]],
  [[0, "A5", 2], [2, "F#5", 2], [4, "D5", 2], [6, "A4", 1], [7, "D5", 1]],
  [[0, "E5", 2], [2, "E5", 1], [3, "G5", 1], [4, "C6", 2], [6, "B5", 1], [7, "A5", 1]],
  [[0, "F#5", 2], [2, "F#5", 1], [3, "A5", 1], [4, "D6", 2], [6, "C#6", 1], [7, "A5", 1]],
  [[0, "B5", 2], [2, "G5", 1], [3, "B5", 1], [4, "D6", 2], [6, "B5", 1], [7, "G5", 1]],
  [[0, "G5", 1], [1, "E5", 1], [2, "G5", 1], [3, "B5", 1], [4, "E6", 3], [7, "D6", 1]],
  [[0, "C6", 1], [1, "B5", 1], [2, "A5", 1], [3, "G5", 1], [4, "E5", 2], [6, "G5", 2]],
  [[0, "F#5", 1], [1, "G5", 1], [2, "A5", 1], [3, "B5", 1], [4, "A5", 2], [6, "F#5", 2]],
  [[0, "G5", 2], [2, "D5", 1], [3, "G5", 1], [4, "B5", 2], [6, "D6", 2]],
  [[0, "D6", 1], [1, "C6", 1], [2, "B5", 1], [3, "A5", 1], [4, "G5", 2], [6, "A5", 1], [7, "B5", 1]]
];
NOTE["C#"] = 1;

for (let bar = 0; bar < BARS; bar++) {
  const t = bar * BAR, ch = CH[PROG[bar]], bsec = bar >= 8;
  // melodia (marimba) + oitava suave na secao B
  for (const [step, name, d] of MEL[bar]) {
    const m = midi(name), tt = t + step * EIGHTH;
    marimba(tt, m, d * EIGHTH, .30, -.12);
    if (bsec) marimba(tt, m - 12, d * EIGHTH, .10, -.2);
  }
  // baixo pizzicato "oom-pah": raiz nos tempos fortes, quinta/acorde nos fracos
  pizz(t + 0 * EIGHTH, midi(ch.r), .32, 0);
  pizz(t + 2 * EIGHTH, midi(ch.f), .22, .05);
  pizz(t + 4 * EIGHTH, midi(ch.r), .30, 0);
  pizz(t + 6 * EIGHTH, midi(ch.f), .22, .05);
  // brilhos de glockenspiel (arpejo nos contratempos) mais presentes na secao B
  const glockGain = bsec ? .11 : .06;
  [1, 3, 5, 7].forEach((s, i) => glock(t + s * EIGHTH, midi(ch.arp[i]) + 12 * (bsec ? 0 : 0), glockGain, .35));
  // pad de cordas suave
  pad(t, ch.t.map(midi).map(m => m + 12), BAR, bsec ? .13 : .09);
  // percussao leve
  for (let s = 0; s < 8; s++) shaker(t + s * EIGHTH + (s % 2 ? EIGHTH * .04 : 0), s % 2 ? .05 : .08, s % 2 ? .3 : -.3);
  wood(t + 2 * EIGHTH, .10, .15);
  wood(t + 6 * EIGHTH, .08, -.15);
  if (bsec) { wood(t + 3 * EIGHTH, .05, .2); wood(t + 7 * EIGHTH, .05, -.2); }
}

// reverb circular (combs + allpass), 2 passes para o rabo da ultima nota entrar no inicio do loop
function reverb(buf, mix) {
  const combs = [[.0297, .78], [.0371, .76], [.0411, .74], [.0437, .72]];
  const wet = new Float32Array(N);
  for (const [dt, g] of combs) {
    const d = Math.round(dt * SR), y = new Float32Array(N);
    for (let pass = 0; pass < 3; pass++)
      for (let i = 0; i < N; i++) y[i] = buf[i] + g * y[(i - d + N) % N];
    for (let i = 0; i < N; i++) wet[i] += y[i] * .25;
  }
  const out = new Float32Array(N);
  for (let i = 0; i < N; i++) out[i] = buf[i] * (1 - mix * .5) + wet[i] * mix;
  return out;
}
const L2 = reverb(L, .22), R2 = reverb(R.map((v, i) => v), .22);
// pequeno atraso estereo no lado direito
const delay = Math.round(.011 * SR), R3 = new Float32Array(N);
for (let i = 0; i < N; i++) R3[i] = R2[(i - delay + N) % N];

// normaliza para -1.5 dBFS
let peak = 0;
for (let i = 0; i < N; i++) peak = Math.max(peak, Math.abs(L2[i]), Math.abs(R3[i]));
const gain = .84 / peak;
const pcm = Buffer.alloc(N * 4);
for (let i = 0; i < N; i++) {
  pcm.writeInt16LE(Math.max(-32768, Math.min(32767, Math.round(L2[i] * gain * 32767))), i * 4);
  pcm.writeInt16LE(Math.max(-32768, Math.min(32767, Math.round(R3[i] * gain * 32767))), i * 4 + 2);
}
const hdr = Buffer.alloc(44);
hdr.write("RIFF", 0); hdr.writeUInt32LE(36 + pcm.length, 4); hdr.write("WAVEfmt ", 8); hdr.writeUInt32LE(16, 16);
hdr.writeUInt16LE(1, 20); hdr.writeUInt16LE(2, 22); hdr.writeUInt32LE(SR, 24); hdr.writeUInt32LE(SR * 4, 28);
hdr.writeUInt16LE(4, 32); hdr.writeUInt16LE(16, 34); hdr.write("data", 36); hdr.writeUInt32LE(pcm.length, 40);
const out = process.argv[2] || path.join(__dirname, "mapa-aventura.wav");
fs.writeFileSync(out, Buffer.concat([hdr, pcm]));
// verificacao de emenda do loop: salto entre o ultimo e o primeiro sample
const jump = Math.abs(L2[N - 1] * gain - L2[0] * gain);
console.log(`ok ${out}: ${(N / SR).toFixed(1)} s, pico antes ${peak.toFixed(2)}, ganho ${gain.toFixed(2)}, salto na emenda ${jump.toFixed(4)}`);
