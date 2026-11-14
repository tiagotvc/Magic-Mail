from pathlib import Path
import re,json,hashlib
base=Path('Backups/franking-soft3d-2026-11-13')
out=Path(__file__).resolve().parent
visual=Path('Assets/RoyalAves/Prefabs/Elements/FrankingMachine.prefab')
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def blocks(p):
 text=p.read_text(encoding='utf-8-sig')
 return {ident:(kind,body) for kind,ident,body in re.findall(r'^--- !u!(\d+) &(-?\d+)\n(.*?)(?=^--- !u!|\Z)',text,re.M|re.S)}
old=blocks(base/visual);new=blocks(visual)
assert set(old)==set(new),'Prefab IDs changed'
goIDs={ident for ident,(kind,body) in old.items() if kind=='1' and re.search(r'^  m_Name: (Press|LeverPivot)$',body,re.M)}
renderers={ident for ident,(kind,body) in old.items() if kind=='212' and any('m_GameObject: {fileID: '+go+'}' in body for go in goIDs)}
assert len(renderers)==2
for ident,(kind,body) in old.items():
 actual=new[ident][1]
 if ident in renderers:
  strip=lambda s:re.sub(r'^  m_Materials:\n(?:  -.*\n)+','  m_Materials: [visual material]\n',re.sub(r'^  m_Sprite: .*$', '  m_Sprite: [visual sprite]',s,flags=re.M),flags=re.M)
  assert strip(body)==strip(actual),('Unexpected renderer property changed',ident)
 elif kind=='114' and '  cabinet:' in body and '  emissionDuration:' in body:
  # Unity persists two previously implicit MailCubbyAnimation defaults on save.
  strip=lambda s:re.sub(r'^  (?:playbackSpeed: 1|useCabinetDelivery: 0)\n','',s,flags=re.M)
  assert strip(body)==strip(actual),('Animation behavior changed',ident)
 else:assert body==actual,('Unrelated component/transform changed',ident)
before=json.loads((base/'before-hashes.json').read_text())
for path,sha in before.items():
 p=Path(path)
 if p.parent==Path('Assets/SweetSugar/Resources/Blocks') or str(p).replace('\\','/').startswith('Assets/SweetSugar/Resources/Levels/'):
  assert digest(p)==sha,('Unrelated prefab/campaign/goal changed',str(p))
report={'prefabIDsPreserved':len(old),'onlyTwoRendererSpriteAndMaterialPairsChanged':True,'implicitDefaultsSerialized':{'playbackSpeed':1,'useCabinetDelivery':False},'letterTransformsSpritesFlightAndLeverMotionPreserved':True,'productionCellAndAllOtherBlocksUnchanged':True,'campaignAndGoalCatalogUnchanged':True,'newArt':{p.name:digest(p) for p in Path('Assets/RoyalAves/Resources/Soft3D').glob('franking-*.png')},'native':(out/'native.result').read_text(),'limits':['Native APIs/camera renders, no physical input/device/audio validation']}
(out/'FRANKING-SOFT3D-V1.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('PASS: same prefab IDs, only body/lever sprite+material changes, original transforms/motion/letters preserved; blocks/campaign/goals untouched')
