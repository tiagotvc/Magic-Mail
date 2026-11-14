"""Deterministic original campaign recipes. Native Unity builder owns asset serialization.
Design difficulty is an authoring estimate, not a measured player win rate.
"""
from pathlib import Path
import json,random,math,hashlib,csv,html
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Tools/level-map/campaign-200'
DEBUT={'C':1,'g':4,'A':11,'M':21,'I':31,'E':46,'P':61,'B':76,'R':91,'K':106,'S':121,'N':136,'T':151}
TITLES={'C':'Caixas','g':'Grama','A':'Escaninho','M':'Franqueadora','I':'Gelo','E':'Ovos mágicos','P':'Poções','B':'Bolsa do carteiro','R':'Pedra rúnica','K':'Tinteiro','S':'Cofre','N':'Sinalizador','T':'Fita postal'}
GOAL={'C':'SolidBlock','g':'SugarSquare','A':'MailCubby','M':'MachineLetters','I':'IceBlock','E':'FlyingLetters','P':'RoutePotions','B':'BagOpenings','R':'EnchantedStone','K':'Inkwell','S':'Safe','N':'SortingSignals'}
BLOCK=set('CAMPRKSBN')
SHAPES=['aberto','cantos','ombros','cruz','ampulheta','vaso','degraus','ondas','arco','duas-pernas','portais-laterais','janela-central','tridente','escada-inversa','diamante','asas']
PALETTE=[1,3,0,4,2,5]
def shape_grid(n,w,h):
 name=SHAPES[(n-1)%len(SHAPES)] if n>4 else ['aberto','cantos','ombros','aberto'][n-1]
 def active(x,y):
  if name=='aberto':return True
  if name=='cantos':return not (x in (0,w-1) and y in (0,h-1))
  if name=='ombros':return min(x,w-1-x)>=(2 if y in (0,h-1) else 1 if y in (1,h-2) else 0)
  if name=='cruz':return min(x,w-1-x)>=(2 if y<2 or y>=h-2 else 0)
  if name=='ampulheta':return min(x,w-1-x)>=(2 if abs(y-(h-1)/2)<1.2 else 1 if abs(y-(h-1)/2)<2.2 else 0)
  if name=='vaso':return min(x,w-1-x)>=(1 if y<3 else 2 if y==3 else 0)
  if name=='degraus':return x>=max(0,(y-1)//3)
  if name=='escada-inversa':return x<w-max(0,(y-1)//3)
  if name=='ondas':return x>=(y%3==0) and x<w-(y%3==2)
  if name=='arco':return not (y>=h-2 and abs(x-(w-1)/2)<1.4)
  if name=='duas-pernas':return not (y>=h-3 and abs(x-(w-1)/2)<1)
  if name=='portais-laterais':return not (y in (3,4) and x in (0,w-1))
  if name=='janela-central':return not (y in (4,5) and x in (w//2,w//2+1))
  if name=='tridente':return not(y>=h-2 and x in (2,w-3))
  if name=='diamante':return min(x,w-1-x)>=max(0,2-min(y,h-1-y))
  if name=='asas':return not (x in (0,w-1) and y in (0,1,h-2,h-1))
  return True
 grid=[['.' if active(x,y) else '#' for x in range(w)] for y in range(h)]
 for y in range(3):
  for x in range(w//2-1,w//2+2):grid[y][x]='.'
 return name,grid
def component(points):
 if not points:return set()
 seen={next(iter(points))};todo=list(seen)
 while todo:
  x,y=todo.pop()
  for q in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]:
   if q in points and q not in seen:seen.add(q);todo.append(q)
 return seen
def make(n):
 rng=random.Random(640200+n*7919)
 w=7 if n<=10 else 8 if n<=60 else 9
 h=7 if n<=10 else 8 if n<=40 else 9
 if n>80 and n%11==0:w=8
 role='desafio' if n%5==0 else ['respiro','pratica','combinacao','tatico'][(n-1)%5]
 intro=next((k for k,v in DEBUT.items() if v==n),None)
 if n==36:intro='I2'
 if intro:role='estreia'
 name,grid=shape_grid(n,w,h)
 core={(x,y) for x in range(w//2-1,w//2+2) for y in range(3)}
 unlocked=[k for k,v in DEBUT.items() if v<=n]
 newest=unlocked[-1]
 focus=(intro if intro!='I2' else 'I') or (newest if n-DEBUT[newest]<5 or n%3 else unlocked[(n//3)%len(unlocked)])
 families=[focus]
 older=[k for k in unlocked if k!=focus and k!='T']
 if n>=12 and not intro and older:families.append(older[(n*7+n//5)%len(older)])
 if n>=85 and not intro and n%5 in (0,3,4):
  choices=[k for k in older if k not in families]
  if choices:families.append(choices[(n//5)%len(choices)])
 if focus=='T' and len(families)==1:families.append('g')
 # Put bulky groups first; palette and pressure vary independently of silhouettes.
 families=sorted(families,key=lambda k:k not in 'APS')
 placements=[];goals={};cost=0
 pattern=['faixa-inferior','laterais','ilhas','escadaria','moldura','corredores','duas-frentes','diagonal'][(n//len(SHAPES))%8]
 def candidates(k,group=False):
  ps=[]
  for y in range(h-(1 if group else 0)):
   for x in range(w-(1 if group else 0)):
    cells={(xx,yy) for yy in range(y,y+(2 if group else 1)) for xx in range(x,x+(2 if group else 1))}
    if cells&core or any(grid[yy][xx]!='.' for xx,yy in cells):continue
    if k=='A' and any(abs(x-p['x'])<3 and abs(y-p['y'])<3 for p in placements if p['kind']=='A'):continue
    if k=='M' and x not in (0,w-1) and y not in (0,h-1):continue
    score={'faixa-inferior':y,'laterais':abs(x-(w-1)/2),'ilhas':-abs(y-h*.5),'escadaria':-abs(x-y),'moldura':max(abs(x-(w-1)/2),abs(y-(h-1)/2)),'corredores':-(x%3),'duas-frentes':abs(x-(w-1)/2)+y*.2,'diagonal':-abs(x+y-(w+h)*.5)}[pattern]
    ps.append((score+rng.random()*2,x,y))
  return sorted(ps,reverse=True)
 def place(k,x,y,layers=1):
  nonlocal cost
  group=k in 'APS';size=2 if group else 1
  for yy in range(y,y+size):
   for xx in range(x,x+size):grid[yy][xx]=k
  placements.append({'kind':k,'x':x,'y':y,'layers':layers})
  count=36 if k=='A' else 4 if k=='P' else 1
  if k not in 'MTB':goals[GOAL[k]]=goals.get(GOAL[k],0)+count
  cost+= {'C':layers,'g':layers*.6,'A':5,'M':2,'I':layers*.75,'E':.8,'P':5.6,'B':2,'R':2.5,'K':5.5,'S':15,'N':4.5,'T':.7}[k]
 for k in families:
  if k in 'APS':count=1 if intro or n<110 else (2 if n%5==0 and len(families)<3 else 1)
  elif k in 'MB':count=1 if intro else 1+(n%3==0)+(n>120 and n%5==0)
  elif k in 'RKN':count=2 if intro else 2+(n//60)+(n%5==0)
  elif k=='E':count=4 if intro else 5+n%5
  else:count=(6 if k=='C' else 10 if k=='g' else 5 if k=='I' else 5) if intro else (6+n%5+(n//60)*2)
  if len(families)>=3:count=max(1,round(count*.7))
  if role=='desafio' and k not in 'APSMB':count+=3
  if n<=3:count=5+n
  # A first adjacent match demonstrates the new crate/egg, grass, or thin ice.
  if intro in ('C','E') and grid[3][w//2]=='.':place(k,w//2,3);count-=1
  if (intro in ('g','I') and k==intro) or (intro=='I2' and k=='I'):
   x,y=w//2,2;place(k,x,y,1 if n<36 else 2);count-=1
  for _ in range(count):
   options=candidates(k,k in 'APS')
   if not options:break
   _,x,y=options[0]
   layers=(1 if n<25 else rng.randint(1,2 if n<70 else 3)) if k=='C' else (1 if n<36 else rng.randint(1,2)) if k=='I' else (1 if n<26 else rng.randint(1,2)) if k=='g' else 1
   place(k,x,y,layers)
  if k in 'MB':
   quota=(8 if k=='M' else 5) if intro else (10+n%7 if k=='M' else 6+n%6)
   goals[GOAL[k]]=quota;cost+=quota*.65
 # Each blocker must touch free play space; avoid dense inaccessible solid fields.
 open_set={(x,y) for y in range(h) for x in range(w) if grid[y][x] not in BLOCK|{'#'}}
 assert len(open_set)>=22,(n,'not enough playable area')
 palette=PALETTE[:4 if n<81 else 5 if n<161 or role in ('respiro','estreia') else 6]
 specials=[]
 available=[(x,y) for x,y in open_set if grid[y][x]=='.' and (x,y) not in {(w//2,0),(w//2,1),(w//2,2),(w//2-1,1)}]
 rng.shuffle(available)
 special_types=['HORIZONTAL_STRIPED'] if n<3 else ['HORIZONTAL_STRIPED','PACKAGE'] if n<6 else ['HORIZONTAL_STRIPED','PACKAGE','MARMALADE'] if n<8 else ['HORIZONTAL_STRIPED','PACKAGE','MARMALADE','MULTICOLOR']
 count=0 if n==1 else 1 if role=='desafio' else 2 if n<90 else 3
 if intro in ('R','S'):count=4
 for i in range(min(count,len(available))):
  x,y=available[i];typ=special_types[(n+i)%len(special_types)]
  if n in (2,3,6,8):typ={2:'HORIZONTAL_STRIPED',3:'PACKAGE',6:'MARMALADE',8:'MULTICOLOR'}[n]
  if intro in ('R','S'):typ=['MARMALADE','HORIZONTAL_STRIPED','PACKAGE','MARMALADE'][i%4]
  specials.append({'x':x,'y':y,'type':typ})
 special_cells={(s['x'],s['y']) for s in specials}
 normal=open_set-special_cells-{(p['x'],p['y']) for p in placements if p['kind']=='E'}
 pins={(w//2,0):palette[0],(w//2,1):palette[1],(w//2,2):palette[0],(w//2-1,1):palette[0]}
 def no_triple(colors,x,y,value):
  return not ((colors.get((x-1,y))==value==colors.get((x-2,y))) or (colors.get((x,y-1))==value==colors.get((x,y-2))) or (colors.get((x-1,y))==value==colors.get((x,y-1))==colors.get((x-1,y-1))))
 for attempt in range(100):
  colors={};okay=True
  for x,y in sorted(normal|special_cells,key=lambda p:(p[1],p[0])):
   # Authored powerups also carry logical color0 in the legacy combine finder.
   choices=[0] if (x,y) in special_cells else [pins[(x,y)]] if (x,y) in pins else rng.sample(palette,len(palette))
   choices=[c for c in choices if no_triple(colors,x,y,c)]
   if not choices:okay=False;break
   colors[x,y]=choices[0]
  if okay:break
 assert okay,(n,'could not fill fixed opening')
 assert all(colors.get(p)==c for p,c in pins.items()),(n,'opening overwritten')
 colors={p:c for p,c in colors.items() if p in normal}
 moves=max(20,min(38,round(23+cost*.16+({'estreia':5,'respiro':5,'pratica':2,'combinacao':0,'tatico':-1,'desafio':-4}[role])-n/65)))
 active={(x,y) for y in range(h) for x in range(w) if grid[y][x]!='#'}
 assert component(active)==active,(n,'disconnected silhouette')
 assert 1<=len(goals)<=3,(n,goals)
 difficulty=round((cost+len(active)*.12)/moves*(1+(len(palette)-4)*.12),3)
 return {'level':n,'seed':640200+n*7919,'role':role,'shape':name,'pattern':pattern,'focus':focus,'intro':intro or ({2:'rocket',3:'dynamite',6:'propeller',8:'lightball'}.get(n,'')),'moves':moves,'palette':palette,'layout':[''.join(row) for row in grid],'placements':placements,'goals':[{'name':k,'count':v} for k,v in goals.items()],'opening':[{'x':x,'y':y,'color':c} for (x,y),c in colors.items()],'specials':specials,'openingSwap':{'x':w//2-1,'y':1,'toX':w//2,'toY':1},'pressureEstimate':difficulty,'workload':round(cost,2),'intent':f'{TITLES[focus]}: {pattern}; '+('aprender a regra com acesso aberto' if role=='estreia' else 'priorizar acesso e aproveitar especiais' if role=='desafio' else 'combinar a mecânica com objetivos já conhecidos')}
def main():
 OUT.mkdir(parents=True,exist_ok=True)
 recipes=[make(n) for n in range(1,201)]
 signatures=[hashlib.sha256(json.dumps({k:r[k] for k in ['layout','placements','goals','opening','specials']},sort_keys=True).encode()).hexdigest() for r in recipes]
 assert len(set(signatures))==200
 for k,first in DEBUT.items():assert min(r['level'] for r in recipes if any(p['kind']==k for p in r['placements']))==first
 (OUT/'recipes.json').write_text(json.dumps({'levels':recipes},ensure_ascii=False,indent=2),encoding='utf-8')
 with (OUT/'campaign.csv').open('w',newline='',encoding='utf-8-sig') as f:
  writer=csv.writer(f);writer.writerow(['level','role','shape','pattern','focus','intro','moves','palette','goals','pressureEstimate','seed','intent'])
  for r in recipes:writer.writerow([r['level'],r['role'],r['shape'],r['pattern'],TITLES[r['focus']],r['intro'],r['moves'],len(r['palette']),'; '.join(f"{g['name']}={g['count']}" for g in r['goals']),r['pressureEstimate'],r['seed'],r['intent']])
 colors={'#':'#ffffff','.':'#c9e3f0','C':'#c98e48','g':'#65b73b','A':'#2f9993','M':'#efa93d','I':'#91cce9','E':'#f5dfa7','P':'#9d6cce','B':'#e8843d','R':'#8e8b9e','K':'#773ac2','S':'#398b9b','N':'#c26d36','T':'#e8be57'}
 cards=[]
 for r in recipes:
  svg=''.join(f'<rect x="{x*12}" y="{y*12}" width="11" height="11" rx="2" fill="{colors[c]}"/>' for y,row in enumerate(r['layout']) for x,c in enumerate(row) if c!='#')
  goalnames={v:TITLES[k] for k,v in GOAL.items()}
  goals=', '.join(f'{goalnames[g["name"]]}: {g["count"]}' for g in r['goals'])
  badge=' · APRESENTAÇÃO' if r['intro'] else ''
  cards.append(f'<article><b>{r["level"]} · {r["role"]}{badge}</b><svg role="img" aria-label="Mapa esquemático do nível {r["level"]}" viewBox="0 0 {len(r["layout"][0])*12} {len(r["layout"])*12}">{svg}</svg><small>{html.escape(r["intent"])}<br>{r["moves"]} jogadas · {len(r["palette"])} cores<br>{html.escape(goals)}</small></article>')
 legend=' '.join(f'<span><i style="background:{colors[k]}"></i>{title}</span>' for k,title in TITLES.items())
 (OUT/'index.html').write_text('<!doctype html><meta charset="utf-8"><title>Campanha Magic Mail 1–200</title><style>body{font:14px system-ui;background:#e8eef2;color:#243848;padding:24px}main{display:grid;grid-template-columns:repeat(auto-fill,minmax(200px,1fr));gap:16px}article{background:white;border-radius:12px;padding:14px}svg{display:block;height:150px;margin:12px auto}small{line-height:1.5}h1{font-size:26px}aside{display:flex;gap:12px;flex-wrap:wrap;margin:24px 0}i{display:inline-block;width:14px;height:14px;border-radius:3px;margin-right:6px}</style><h1>Magic Mail · Campanha 1–200</h1><p>Mapas esquemáticos de autoria, não capturas do jogo. Casas azuis são peças; cores da legenda representam os elementos. Dificuldade estimada, sem taxa de vitória medida. Fita é apenas bloqueio, nunca meta.</p><aside>'+legend+'</aside><main>'+''.join(cards)+'</main>',encoding='utf-8')
 print('PASS:',len(recipes),'unique recipes;',len(SHAPES),'silhouettes;',len(DEBUT),'element debuts; maximum3goals; fixed legal openings')
if __name__=='__main__':main()
