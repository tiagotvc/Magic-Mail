from pathlib import Path
import subprocess,json,hashlib,shutil
import imageio_ffmpeg
from PIL import Image,ImageDraw
p=Path(__file__).resolve().parent
ff=imageio_ffmpeg.get_ffmpeg_exe()
subprocess.run([ff,'-y','-framerate','24','-i',str(p/'native/frame-%04d.png'),'-c:v','libx264','-crf','19','-pix_fmt','yuv420p','-movflags','+faststart',str(p/'previa-seis-elementos.mp4')],check=True,capture_output=True)
subprocess.run([ff,'-y','-i',str(p/'previa-seis-elementos.mp4'),'-filter_complex','fps=16,scale=900:-1:flags=lanczos,split[a][b];[a]palettegen=stats_mode=diff[p];[b][p]paletteuse=dither=sierra2_4a','-loop','0',str(p/'previa-seis-elementos.gif')],check=True,capture_output=True)
shutil.copyfile(p/'native/frame-0000.png',p/'previa.png')
sheet=Image.new('RGB',(1200,1200),'#d4e5ed')
for i,frame in enumerate([0,27,48,63,89,105]):
 im=Image.open(p/f'native/frame-{frame:04d}.png');im.thumbnail((600,400));sheet.paste(im,(i%2*600,i//2*400))
sheet.save(p/'sequencia.png')
frames=list((p/'native').glob('frame-*.png'))
assert len(frames)==168
assert (p/'native/frame-0000.png').read_bytes()==(p/'native/frame-0167.png').read_bytes(), 'Repeat must restore identical intact frame'
manifest={'scope':'Six-element isolated animation style preview; no production replacement','nativeFrames':len(frames),'fps':24,'size':[1200,800],'duration':7,'reset':'PASS: first and last native frame byte-identical','assets':{f.name:hashlib.sha256(f.read_bytes()).hexdigest() for f in p.glob('*.png')},'sources':{str(f):hashlib.sha256(f.read_bytes()).hexdigest() for f in [Path('Assets/RoyalAves/Editor/Soft3DAnimationLaboratory.cs'),Path('Assets/RoyalAves/Shaders/Soft3DPreviewLens.shader'),Path('Assets/RoyalAves/Shaders/Soft3DPreviewSolid.shader')]},'limits':['Art preview only','No physical pointer, device, audio or campaign validation','Existing production prefabs unchanged']}
(p/'ANIMATION-PREVIEW-V1.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('PASS: MP4, GIF, six-frame contact sheet, 168 native frames, identical loop reset')
