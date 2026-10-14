"""Original procedural parcel cues; regenerate from the project root with Python 3."""
import array
import math
from pathlib import Path
import random
import sys
import uuid
import wave

RATE = 44100
ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "Assets/RoyalAves/Resources/Audio/Parcels"


def render(name, duration, kind, seed):
    rng = random.Random(seed)
    samples = []
    smooth = previous = phase = filtered = 0.0
    for i in range(round(RATE * duration)):
        t = i / RATE
        x = t / duration
        noise = rng.uniform(-1, 1)
        smooth += 0.18 * (noise - smooth)
        grain = smooth - previous
        previous = smooth
        fade = min(1, t / 0.003) * min(1, (duration - t) / 0.020)
        if kind == "seal":
            phase += 2 * math.pi * (160 + 760 * math.exp(-t * 65)) / RATE
            value = (0.65 * math.sin(phase) * math.exp(-t * 38)
                     + 0.9 * grain * math.exp(-t * 55))
        elif kind == "finish":
            phase += 2 * math.pi * (110 + 230 * math.exp(-t * 50)) / RATE
            body = 0.60 * math.sin(phase) * math.exp(-t * 24)
            paper = 1.1 * grain * math.exp(-t * 27)
            chime = 0.0
            if t > 0.035:
                u = t - 0.035
                chime = (math.sin(2 * math.pi * 1320 * u)
                         + 0.35 * math.sin(2 * math.pi * 1980 * u)) * 0.12 * math.exp(-u * 18) * min(1, u / 0.006)
            value = body + paper + chime
        else:
            # Filtered noise and irregular pulses resemble adhesive peeling, with a soft tail.
            pulses = 0.65 + 0.25 * math.sin(2 * math.pi * (24 * t + 17 * t * t))
            envelope = math.sin(math.pi * x) ** 0.7
            value = (1.5 * grain + 0.32 * smooth) * envelope * pulses
        # Round off the noise's sharpest edges for a soft cartoon sound.
        filtered += 0.25 * (value - filtered)
        samples.append(filtered * fade)
    peak = max(abs(v) for v in samples)
    pcm = array.array("h", (round(v / peak * 0.72 * 32767) for v in samples))
    pcm[0] = pcm[-1] = 0
    if sys.byteorder != "little":
        pcm.byteswap()
    path = OUTPUT / f"{name}.wav"
    with wave.open(str(path), "wb") as wav:
        wav.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        wav.writeframes(pcm.tobytes())
    meta = Path(str(path) + ".meta")
    if not meta.exists():
        meta.write_text(f"""fileFormatVersion: 2
guid: {uuid.uuid4().hex}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 8
  defaultSettings:
    serializedVersion: 2
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 0
    quality: 1
    conversionMode: 0
    preloadAudioData: 1
  platformSettingOverrides: {{}}
  forceToMono: 1
  normalize: 0
  loadInBackground: 0
  ambisonic: 0
  3D: 0
  userData:
  assetBundleName:
  assetBundleVariant:
""", encoding="utf-8")
    rms = math.sqrt(sum((v / peak * 0.72) ** 2 for v in samples) / len(samples))
    print(f"{path.name}: {duration:.2f}s, mono PCM16, peak 0.72, RMS {rms:.3f}")


if __name__ == "__main__":
    OUTPUT.mkdir(parents=True, exist_ok=True)
    render("seal_pop", 0.14, "seal", 401)
    render("tape_vertical", 0.30, "tape", 302)
    render("tape_horizontal", 0.32, "tape", 203)
    render("parcel_complete", 0.32, "finish", 104)
