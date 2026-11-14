"""Package native Unity renders as video/GIF; never redraws or edits the source art."""
from pathlib import Path
import subprocess
import imageio_ffmpeg

root = Path(__file__).resolve().parent.parent
folder = root / 'Tools/royal-postman/production/review/safe-v1'
frames = folder / 'frames'
if len(list(frames.glob('*.png'))) != 144:
    raise SystemExit('Expected 144 native Unity captures. Run SafeAnimationPreview.CaptureBatch first.')
ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
common = [ffmpeg, '-y', '-v', 'error', '-framerate', '20', '-i', str(frames / '%03d.png')]
subprocess.run(common + ['-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '19', '-movflags', '+faststart', str(folder / 'cofre-quebra.mp4')], check=True)
subprocess.run(common + ['-filter_complex', '[0:v]split[a][b];[a]palettegen[p];[b][p]paletteuse', '-loop', '0', str(folder / 'cofre-quebra.gif')], check=True)
print(folder / 'cofre-quebra.mp4')
print(folder / 'cofre-quebra.gif')
