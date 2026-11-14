from pathlib import Path
import subprocess,json,shutil,hashlib
import imageio_ffmpeg
from PIL import Image,ImageDraw,ImageFont
out=Path(__file__).resolve().parent
root=out.parents[3]
src=root/'Logs/InkwellValidationProject/Logs/propeller-pair-v1'
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
entries=[(8,'01-helices-combinadas','Duas h\u00e9lices combinadas')]
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',23)
small=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16)
sheet=Image.new('RGB',(920,288),'#101827');draw=ImageDraw.Draw(sheet)
manifest={'date':'2026-11-11','revision':'Merge, triangular three-propeller formation and fan-out','reference':'references/helice-combinado.webp','fps':24,'audio':False,'source':'Real isolated Unity Play, board-only camera, scripted swap','entries':[]}
html='<!doctype html><html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>H\u00e9lices combinadas - Magic Mail</title><style>body{background:#101827;color:#f4ecdf;font:17px system-ui;margin:0}main{max-width:1050px;margin:auto;padding:28px}section{display:grid;grid-template-columns:repeat(auto-fit,minmax(290px,1fr));gap:24px}article{padding:18px;border-radius:15px;background:#1b293c}video{width:100%;border-radius:10px}a{color:#9ed9ff}p{line-height:1.6}</style><main><h1>H\u00e9lices combinadas</h1><p>Duas helices se unem, formam tres e se separam para seus alvos. Capturas reais do Unity, sem \u00e1udio.</p><section>'
for row,(mode,name,title) in enumerate(entries):
 frames=src/('mode-'+str(mode))
 subprocess.run([ffmpeg,'-y','-loglevel','error','-framerate','24','-i',str(frames/'%04d.png'),'-c:v','libx264','-preset','fast','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart',str(out/(name+'.mp4'))],check=True)
 subprocess.run([ffmpeg,'-y','-loglevel','error','-i',str(out/(name+'.mp4')),'-filter_complex','[0:v]fps=12,scale=480:-1:flags=lanczos,split[a][b];[a]palettegen=stats_mode=diff[p];[b][p]paletteuse=dither=sierra2_4a','-loop','0',str(out/(name+'.gif'))],check=True)
 Image.open(frames/'0045.png').save(out/(name+'.png'))
 draw.text((14,row*288+8),title,font=font,fill='#f5eddf')
 for col,key in enumerate([20,34,45,60]):
  im=Image.open(frames/(str(key).zfill(4)+'.png')).convert('RGB');im.thumbnail((220,220),Image.Resampling.LANCZOS);sheet.paste(im,(10+col*230,row*288+40));draw.text((14+col*230,row*288+262),f'{key/24:.2f} s',font=small,fill='#bed5eb')
 html+=f'<article><h2>{title}</h2><video controls loop muted playsinline preload="metadata" poster="{name}.png"><source src="{name}.mp4" type="video/mp4"></video><p><a href="{name}.gif">Abrir GIF</a> &middot; <a href="{name}.mp4" download>Baixar v\u00eddeo</a></p></article>'
 manifest['entries'].append({'name':name,'mode':mode,'frames':len(list(frames.glob('*.png')))})
sheet.save(out/'comparativo.png')
html+='</section><p>C\u00e2mera focada no campo; troca iniciada pela API do jogo. Sem teste de ponteiro f\u00edsico ou dispositivo. Arte original do Magic Mail preservada.</p></main></html>'
(out/'index.html').write_text(html,encoding='utf-8')
for f in ['result.txt','behavior.result']:shutil.copy2(src/f,out/f)
manifest['sha256']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in out.iterdir() if p.suffix in ['.gif','.mp4','.png']}
(out/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('Exported both previews')
