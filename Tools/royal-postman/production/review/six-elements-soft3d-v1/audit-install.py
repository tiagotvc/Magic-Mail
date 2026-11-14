from pathlib import Path
import re,json,hashlib

root=Path('.')
evidence=Path(__file__).resolve().parent
backup=Path('Backups/six-elements-soft3d-2026-11-13')
names=['SolidBlock','EnchantedStone','Inkwell','RoutePotions','BagOpenings','SortingSignals']
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def ids(p):return set(re.findall(r'^--- !u!\d+ &(-?\d+)',p.read_text(encoding='utf-8-sig'),re.M))
def targets(p):
 text=p.read_text(encoding='utf-8-sig')
 return re.findall(r'^  - name: ([^\n]+)\n(.*?)(?=^  - name: |\Z)',text,re.M|re.S)
report={'scope':names,'prefabIDs':{},'campaignUnchanged':True,'unrelatedPrefabsUnchanged':True,'targetCatalog':{},'nativeTest':(evidence/'six-soft3d-native.result').read_text(),'limits':['API/native validation; no physical pointer/device/audio/full campaign validation']}
for name in names:
 p=Path('Assets/SweetSugar/Resources/Blocks')/(name+'.prefab')
 old=ids(backup/p);new=ids(p)
 assert old<=new,(name,'Existing prefab IDs removed',old-new)
 report['prefabIDs'][name]={'preserved':len(old),'added':len(new-old),'sha256':digest(p)}
before=json.loads((backup/'before-hashes.json').read_text())
for name,sha in before.items():
 p=Path(name)
 if p.parent==Path('Assets/SweetSugar/Resources/Blocks') and p.stem not in names:assert digest(p)==sha,('Unrelated prefab changed',str(p))
for name,sha in json.loads((evidence/'campaign-before-install.json').read_text()).items():assert digest(Path(name))==sha,('Campaign asset changed',name)
target=Path('Assets/SweetSugar/Resources/Levels/TargetEditorScriptable.asset')
old=targets(backup/target);new=targets(target)
assert len(old)==len(new)
for a,b in zip(old,new):
 assert a[0]==b[0],'Goal order changed'
 if a[0] in names:
  strip=lambda value:re.sub(r'^    defaultSprites:\n.*?(?=^    prefabs:)', '',value,flags=re.M|re.S)
  assert strip(a[1])==strip(b[1]),('Goal behavior changed',a[0])
 else:assert a==b,('Unrelated goal changed',a[0])
report['targetCatalog']={'count':len(new),'orderPreserved':True,'onlySixDefaultIconSetsChanged':True}
assets=Path('Assets/RoyalAves/Resources/Soft3D')
report['artHashes']={p.name:digest(p) for p in assets.glob('*.png')}
preview=Path('Tools/royal-postman/previews/elementos-soft3d-animacao-v1')
for name in ['crate-states','inkwell-states','potion-rack','potion-bottles','bag-opening','scroll-opening']:assert digest(assets/(name+'.png'))==digest(preview/(name+'.png'))
for path in assets.glob('Sprites/*.asset'):
 text=path.read_text();width=float(re.search(r'^    width: ([\d.]+)',text,re.M)[1]);height=float(re.search(r'^    height: ([\d.]+)',text,re.M)[1]);ppu=float(re.search(r'^  m_PixelsToUnits: ([\d.]+)',text,re.M)[1]);assert min(width,height,ppu)>0
report['sprites']=len(list(assets.glob('Sprites/*.asset')))
(evidence/'SIX-ELEMENTS-SOFT3D-V1.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('PASS: preserved prefab IDs, unrelated prefabs, campaign assets, target order/count/behavior; only six icon sets changed; approved animation PNG bytes unchanged')
