"""Gera o impacto de armario de madeira quebrando, por sintese procedural.

Saida: WAV mono 44100 Hz 16 bits, no mesmo formato dos SFX ja usados no jogo.
Uso:   python -I Tools/Audio/gen_wood_break.py <pasta de saida>

O som e montado em quatro camadas:
  1. estalo inicial  - rajada curta de ruido passa-banda, o ataque
  2. modos da tabua  - senoides amortecidas nas frequencias de ressonancia da madeira
  3. lascas          - varios estalinhos espalhados nos primeiros milissegundos
  4. corpo grave     - batida baixa que da peso ao impacto
"""

import math
import os
import random
import struct
import sys
import wave

RATE = 44100


def seconds(duration):
    return int(RATE * duration)


def biquad_bandpass(samples, freq, q):
    """Filtro passa-banda RBJ, aplicado em sequencia sobre a lista."""
    w0 = 2 * math.pi * freq / RATE
    alpha = math.sin(w0) / (2 * q)
    b0, b1, b2 = alpha, 0.0, -alpha
    a0, a1, a2 = 1 + alpha, -2 * math.cos(w0), 1 - alpha
    b0, b1, b2, a1, a2 = b0 / a0, b1 / a0, b2 / a0, a1 / a0, a2 / a0
    x1 = x2 = y1 = y2 = 0.0
    out = []
    for x0 in samples:
        y0 = b0 * x0 + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2
        x2, x1 = x1, x0
        y2, y1 = y1, y0
        out.append(y0)
    return out


def noise_burst(duration, decay, freq, q, rng):
    raw = [rng.uniform(-1.0, 1.0) for _ in range(seconds(duration))]
    band = biquad_bandpass(raw, freq, q)
    return [value * math.exp(-decay * i / RATE) for i, value in enumerate(band)]


def wood_mode(duration, freq, decay, amplitude):
    """Uma senoide amortecida: um modo de vibracao da tabua."""
    return [amplitude * math.sin(2 * math.pi * freq * i / RATE) * math.exp(-decay * i / RATE)
            for i in range(seconds(duration))]


def mix(target, part, start):
    offset = seconds(start)
    for i, value in enumerate(part):
        index = offset + i
        if index < len(target):
            target[index] += value


def soft_clip(samples, drive):
    return [math.tanh(value * drive) / math.tanh(drive) for value in samples]


def finish(samples, peak=0.89, fade=0.02):
    highest = max(abs(value) for value in samples) or 1.0
    gain = peak / highest
    tail = seconds(fade)
    out = []
    for i, value in enumerate(samples):
        value *= gain
        remaining = len(samples) - i
        if remaining < tail:
            value *= remaining / tail
        out.append(max(-1.0, min(1.0, value)))
    return out


def write(path, samples):
    with wave.open(path, "w") as handle:
        handle.setnchannels(1)
        handle.setsampwidth(2)
        handle.setframerate(RATE)
        handle.writeframes(b"".join(struct.pack("<h", int(value * 32000)) for value in samples))
    return path


def splinters(target, rng, count, window, freq_low, freq_high, level):
    for _ in range(count):
        start = rng.uniform(0.012, window)
        freq = rng.uniform(freq_low, freq_high)
        burst = noise_burst(0.05, rng.uniform(70, 150), freq, 7.0, rng)
        mix(target, [value * level * rng.uniform(0.4, 1.0) for value in burst], start)


def dry(seed=11):
    """Seco e curto: a tabua racha e acabou. Bom para impacto intermediario."""
    rng = random.Random(seed)
    track = [0.0] * seconds(0.45)
    mix(track, [v * 1.0 for v in noise_burst(0.12, 55, 1400, 2.2, rng)], 0.0)
    mix(track, wood_mode(0.30, 196, 26, 0.45), 0.0)
    mix(track, wood_mode(0.26, 312, 32, 0.30), 0.004)
    mix(track, wood_mode(0.20, 523, 40, 0.18), 0.008)
    mix(track, wood_mode(0.18, 92, 24, 0.55), 0.0)
    splinters(track, rng, 6, 0.16, 1800, 4200, 0.33)
    return finish(soft_clip(track, 1.6))


def full(seed=23):
    """Quebra inteira, com cauda de lascas caindo. Para o movel se desfazendo."""
    rng = random.Random(seed)
    track = [0.0] * seconds(0.85)
    mix(track, [v * 1.15 for v in noise_burst(0.16, 42, 1100, 1.8, rng)], 0.0)
    mix(track, wood_mode(0.42, 148, 18, 0.52), 0.0)
    mix(track, wood_mode(0.38, 233, 22, 0.38), 0.003)
    mix(track, wood_mode(0.30, 389, 30, 0.24), 0.007)
    mix(track, wood_mode(0.34, 74, 15, 0.70), 0.0)
    # segundo arranque: a estrutura cede logo depois do primeiro golpe
    mix(track, [v * 0.75 for v in noise_burst(0.14, 48, 900, 2.0, rng)], 0.075)
    mix(track, wood_mode(0.30, 170, 20, 0.30), 0.075)
    splinters(track, rng, 9, 0.22, 1600, 3800, 0.38)
    # destrocos espalhados ate o fim
    for _ in range(14):
        start = rng.uniform(0.18, 0.62)
        burst = noise_burst(0.045, rng.uniform(110, 220), rng.uniform(700, 2600), 9.0, rng)
        level = 0.22 * (1.0 - (start - 0.18) / 0.5)
        mix(track, [v * level * rng.uniform(0.5, 1.0) for v in burst], start)
    return finish(soft_clip(track, 1.5))


def cartoon(seed=5):
    """Mais claro e tonal, perto do traco cartoon do jogo. Madeira oca."""
    rng = random.Random(seed)
    track = [0.0] * seconds(0.62)
    mix(track, [v * 0.95 for v in noise_burst(0.10, 60, 1700, 2.6, rng)], 0.0)
    # modos afinados: a madeira oca soa quase como um bloco de percussao
    for freq, decay, amp, delay in ((165, 16, 0.50, 0.0), (330, 20, 0.34, 0.004),
                                    (494, 26, 0.22, 0.010), (247, 18, 0.28, 0.055)):
        mix(track, wood_mode(0.40, freq, decay, amp), delay)
    mix(track, wood_mode(0.22, 88, 26, 0.45), 0.0)
    splinters(track, rng, 7, 0.20, 2200, 5200, 0.30)
    for _ in range(8):
        start = rng.uniform(0.16, 0.44)
        burst = noise_burst(0.04, rng.uniform(140, 260), rng.uniform(1200, 3400), 10.0, rng)
        mix(track, [v * 0.20 * rng.uniform(0.5, 1.0) for v in burst], start)
    return finish(soft_clip(track, 1.7))


VARIANTS = {"seco": dry, "cheio": full, "cartoon": cartoon}


def main():
    folder = sys.argv[1] if len(sys.argv) > 1 else "."
    os.makedirs(folder, exist_ok=True)
    for name, builder in VARIANTS.items():
        path = write(os.path.join(folder, "wood_break_%s.wav" % name), builder())
        print(path)


if __name__ == "__main__":
    main()
