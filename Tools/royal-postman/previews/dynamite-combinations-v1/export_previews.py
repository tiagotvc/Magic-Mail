from pathlib import Path
import subprocess, json, hashlib, shutil
import imageio_ffmpeg
from PIL import Image, ImageDraw, ImageFont
root=Path(__file__).resolve().parents[4]
output=Path(__file__).resolve().parent
source=root/'Logs/InkwellValidationProject/Logs/dynamite-previews-v1'
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
entries=[(1,'01-duas-dinamites','Dinamite + dinamite','Fus\u00e3o, pavio e explos\u00e3o no tabuleiro inteiro.',[12,38,50,56]),(2,'02-foguete-horizontal','Dinamite + foguete horizontal','Fus\u00e3o seguida de tr\u00eas linhas e tres colunas.',[12,35,49,62]),(5,'03-foguete-vertical','Dinamite + foguete vertical','A mesma cruz de tr\u00eas linhas e tres colunas.',[12,35,49,62]),(3,'04-helice','Dinamite + h\u00e9lice','Transporte da dinamite e explos\u00e3o no destino.',[12,34,45,66]),(4,'05-bola-de-luz','Dinamite + bola de luz','A cor mais comum vira dinamites, que sao ativadas.',[12,55,132,160])]
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',22)
small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16)
sheet=Image.new('RGB',(920,len(entries)*288),(16,24,39));draw=ImageDraw.Draw(sheet)
manifest={'date':'2026-11-10','source':'native Unity isolated Play board camera','fps':24,'audio':False,'capture':'scripted swap, no physical pointer','gameplay_rules_changed':False,'runtime_visual_fix':'Cargo moved below the spinning propeller body to remain visible','entries':[]}
for row,(mode,name,title,description,keys) in enumerate(entries):
 frames=source/('mode-'+str(mode));files=sorted(frames.glob('*.png'))
 if not files: raise RuntimeError('Missing frames: '+str(frames))
 subprocess.run([ffmpeg,'-y','-loglevel','error','-framerate','24','-i',str(frames/'%04d.png'),'-c:v','libx264','-preset','fast','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(output/(name+'.mp4'))],check=True)
 subprocess.run([ffmpeg,'-y','-loglevel','error','-i',str(output/(name+'.mp4')),'-filter_complex','[0:v]fps=12,scale=480:-1:flags=lanczos,split[a][b];[a]palettegen=stats_mode=diff[p];[b][p]paletteuse=dither=sierra2_4a','-loop','0',str(output/(name+'.gif'))],check=True)
 Image.open(files[keys[1]]).save(output/(name+'.png'))
 draw.text((16,row*288+10),title,font=font,fill='#f6eee1')
 for column,key in enumerate(keys):
  image=Image.open(files[min(key,len(files)-1)]).convert('RGB');image.thumbnail((220,220),Image.Resampling.LANCZOS)
  sheet.paste(image,(10+column*230,row*288+42))
  draw.text((16+column*230,row*288+263),f'{key/24:.2f} s',font=small,fill='#b4c9e2')
 manifest['entries'].append({'mode':mode,'name':name,'title':title,'frames':len(files),'seconds':len(files)/24,'storyboard_frames':keys})
sheet.save(output/'todas-combinacoes.png')
html=(output/'index.html').read_text(encoding='utf-8').split('<section>')[0]+'<section>'
for mode,name,title,description,keys in entries:
 html+=f'<article><h2>{title}</h2><video controls playsinline loop muted preload="metadata" poster="{name}.png"><source src="{name}.mp4" type="video/mp4"></video><p>{description}</p><a href="{name}.gif">Abrir GIF</a> &middot; <a href="{name}.mp4" download>Baixar video</a></article>'
html+='</section><footer>Captura do tabuleiro em Play numa copia isolada. Troca iniciada pela API do jogo. Camera dedicada, sem HUD, sem audio e sem alteracoes na campanha. A galeria mostra a implementacao atual para avaliacao visual.</footer></main></html>'
(output/'index.html').write_text(html,encoding='utf-8')
shutil.copy2(source/'result.txt',output/'capture-result.txt')
manifest['sha256']={f.name:hashlib.sha256(f.read_bytes()).hexdigest() for f in output.iterdir() if f.suffix in ['.mp4','.gif','.png']}
(output/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(json.dumps({'entries':len(entries),'output':str(output),'frames':[e['frames'] for e in manifest['entries']]}))
