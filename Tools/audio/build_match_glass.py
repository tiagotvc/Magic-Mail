"""Render the supplied glass sound into the two existing match cues, preserving their GUIDs.

Uses numpy and the ffmpeg binary provided by imageio_ffmpeg. Run from any directory.
The original cue backups define the reference loudness, so rebuilding is repeatable.
"""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import wave

import imageio_ffmpeg
import numpy as np

ROOT = Path(__file__).resolve().parents[2]
SOURCE = Path(__file__).with_name('sources') / 'vidro-quebrando.mp3'
TARGETS = [('Jelly_destroy.wav', .183), ('Jelly_destroy_02.wav', .157)]
RATE = 44100
BACKUP = ROOT / 'Backups/Match-before-glass'


def ffmpeg_samples(path, filters=None):
    command = [imageio_ffmpeg.get_ffmpeg_exe(), '-v', 'error', '-i', str(path)]
    if filters:
        command += ['-af', filters]
    command += ['-ar', str(RATE), '-ac', '1', '-f', 'f32le', 'pipe:1']
    return np.frombuffer(subprocess.run(command, check=True, capture_output=True).stdout, dtype='<f4').astype(np.float64)


def rms(samples):
    return float(np.sqrt(np.mean(samples*samples)))


def main():
    original = ffmpeg_samples(SOURCE)
    activity = np.flatnonzero(np.abs(original) > 10**(-45/20))
    assert activity.size > 0, 'The source has no audible signal'
    start = max(0, int(activity[0]) - round(.002*RATE))
    end = min(original.size, int(activity[-1])+round(.005*RATE)+1)
    BACKUP.mkdir(parents=True, exist_ok=True)
    report = []
    for name, duration in TARGETS:
        target = ROOT / 'Assets/SweetSugar/Audio/wav' / name
        meta = target.with_suffix('.wav.meta')
        meta_hash = hashlib.sha256(meta.read_bytes()).hexdigest()
        for path in (target, meta):
            if not (BACKUP/path.name).exists():
                shutil.copy2(path, BACKUP/path.name)
        with wave.open(str(BACKUP/name), 'rb') as old:
            assert old.getsampwidth() == 2
            reference = np.frombuffer(old.readframes(old.getnframes()), dtype='<i2').astype(np.float64)/32768
            reference_rms = rms(reference)
        frames = round(duration*RATE)
        effective_duration = frames/RATE
        # Chain moderate atempo factors to retain pitch and include the full audible source.
        factor = ((end-start)/RATE/effective_duration)**.5
        assert .5 <= factor <= 2
        filters = (f'atrim=start_sample={start}:end_sample={end},asetpts=PTS-STARTPTS,'
                   f'atempo={factor:.12f},atempo={factor:.12f},'
                   f'apad,atrim=end_sample={frames},afade=t=in:d=0.001,'
                   f'afade=t=out:st={effective_duration-.01:.12f}:d=0.01')
        samples = ffmpeg_samples(SOURCE, filters)
        assert samples.size == frames and rms(samples) > 0
        gain = min(reference_rms/rms(samples), .98/float(np.max(np.abs(samples))))
        samples *= gain
        pcm = np.rint(np.clip(samples, -1, 1)*32767).astype('<i2')
        with wave.open(str(target), 'wb') as output:
            output.setnchannels(1)
            output.setsampwidth(2)
            output.setframerate(RATE)
            output.writeframes(pcm.tobytes())
        with wave.open(str(target), 'rb') as output:
            assert output.getnframes() == frames and output.getframerate() == RATE
            assert abs(output.getnframes()/RATE-duration) <= 1/RATE
        assert hashlib.sha256(meta.read_bytes()).hexdigest() == meta_hash
        assert np.max(np.abs(pcm)) < 32767
        assert abs(int(pcm[-1])) <= 1
        report.append(dict(asset=str(target.relative_to(ROOT)),requestedSeconds=duration,
                           seconds=effective_duration,frames=frames,sampleRate=RATE,
                           oldRMS=reference_rms,newRMS=rms(pcm.astype(float)/32768),
                           pitchPreserved=True,guidPreserved=True))
    output = ROOT / 'Logs/match-glass-audio-validation.json'
    output.parent.mkdir(exist_ok=True)
    output.write_text(json.dumps(dict(sourceSHA256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
                                     originalSeconds=original.size/RATE,auditionedInUnity=False,variants=report),indent=2),encoding='utf-8')
    for variant in report:
        print(f'{variant["asset"]}: {variant["seconds"]:.6f}s, {variant["frames"]} frames; GUID preserved.')


if __name__ == '__main__':
    main()
