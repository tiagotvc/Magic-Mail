"""Audit the compact campaign revision without overwriting V1 evidence."""
from pathlib import Path
import json,hashlib,re,collections,datetime
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Tools/level-map/campaign-200/evidence-density-v2'
BACKUP=ROOT/'Backups/campaign-density-v2-2026-11-13'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()

def main():
 OUT.mkdir(exist_ok=True)
 recipes=json.loads((ROOT/'Tools/level-map/campaign-200/recipes.json').read_text(encoding='utf8'))['levels']
 assert len(recipes)==200
 distribution=collections.Counter();bag_counts=[]
 for r in recipes:
  kinds={p['kind'] for p in r['placements']};assert len(kinds)<=2
  distribution[len(kinds)]+=1;assert 1<=len(r['goals'])<=2
  assert all(g['name']!='PostalTapes' for g in r['goals'])
  bags=sum(p['kind']=='B' for p in r['placements'])
  if bags:bag_counts.append(bags);assert bags>=3
  c={(p['x'],p['y']):p['color'] for p in r['opening']};c.update({(p['x'],p['y']):0 for p in r['specials']})
  for (x,y),v in c.items():
   assert not(c.get((x-1,y))==v==c.get((x-2,y)))
   assert not(c.get((x,y-1))==v==c.get((x,y-2)))
   assert not(c.get((x-1,y))==v==c.get((x,y-1))==c.get((x-1,y-1)))
  grid=r['layout'];h=len(grid);w=len(grid[0])
  for p in r['placements']:
   if p['kind'] in 'MB':
    x,y=p['x'],p['y'];assert any(0<=xx<w and 0<=yy<h and grid[yy][xx] not in '#MB' for xx,yy in [(x-1,y),(x+1,y),(x,y-1),(x,y+1)])
 r103=recipes[102];assert {p['kind'] for p in r103['placements']}=={'B'} and len(r103['placements'])==6
 assert r103['goals']==[{'name':'BagOpenings','count':18}]
 points={(p['x'],p['y']) for p in r103['placements']};assert len({y for x,y in points})==1 and max(x for x,y in points)-min(x for x,y in points)==5
 levels=ROOT/'Assets/SweetSugar/Resources/Levels'
 assert {int(p.stem[6:]) for p in levels.glob('Level_*.asset')}==set(range(1,201))
 metas=list((BACKUP/'Assets/SweetSugar/Resources/Levels').rglob('*.meta'))
 for p in metas:assert p.read_bytes()==(ROOT/p.relative_to(BACKUP)).read_bytes(),str(p)
 protected=json.loads((ROOT/'Backups/campaign-200-2026-11-13/protected-hashes.json').read_text())
 assert not [p for p,h in protected.items() if sha(ROOT/p)!=h]
 clone=ROOT/'Logs/InkwellValidationProject';assets=[p for p in levels.rglob('*.asset') if p.name.startswith(('Level_','TargetLevel'))]
 assert len(assets)==400
 assert not [str(p.relative_to(ROOT)) for p in assets if p.read_bytes()!=(clone/p.relative_to(ROOT)).read_bytes()]
 for directory in ['Assets/SweetSugar/Scripts','Assets/RoyalAves/Scripts']:
  for p in (ROOT/directory).rglob('*.cs'):assert p.read_bytes()==(clone/p.relative_to(ROOT)).read_bytes()
 native=(OUT/'native.result').read_text(encoding='utf8');installed=(ROOT/'Logs/campaign-200.result').read_text(encoding='utf8')
 assert native.startswith('PASS: 15 ') and installed.startswith('PASS: 200 ')
 assert '214 goal groups' in installed
 assert all('error CS' not in (OUT/name).read_text(encoding='utf8',errors='replace') for name in ['native.log','install.log'])
 result={'status':'PASS','utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'levels':200,
  'familyDistribution':dict(distribution),'goalGroups':sum(len(r['goals']) for r in recipes),
  'minimumBagsWhenPresent':min(bag_counts),'level103':{'bags':6,'families':1,'goal':18,'moves':r103['moves'],'contiguousRow':True},
  'existingMetadataFilesPreserved':len(metas),'protectedProductionFilesUnchanged':len(protected),
  'mainAndCloneCampaignAssetsIdentical':400,'allComparedRuntimeSourcesIdentical':True,
  'nativeResult':native,'mainStructuralResult':installed,
  'recipesSHA256':sha(ROOT/'Tools/level-map/campaign-200/recipes.json'),
  'assetHashes':{p.relative_to(ROOT).as_posix():sha(p) for p in sorted(assets)},
  'limits':'15 representative runtime boards; not 200 full wins, win-rate tuning, physical input, device or audio validation.'}
 (OUT/'CAMPAIGN-DENSITY-V2.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
 print('PASS: 200 levels,150 single-family/50 two-family;103six contiguous bags;214goal groups;400identical native assets;',len(metas),'metadata and',len(protected),'production files preserved')
if __name__=='__main__':main()
