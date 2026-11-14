"""Archived V1 audit. For the compact revision use audit_campaign_density_v2.py."""
from pathlib import Path
import hashlib,json,re,datetime
ROOT=Path(__file__).resolve().parents[2]
BACKUP=ROOT/'Backups/campaign-200-2026-11-13'
OUT=ROOT/'Tools/level-map/campaign-200/evidence'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()

def main():
 if (ROOT/'Tools/level-map/campaign-200/evidence-density-v2/CAMPAIGN-DENSITY-V2.json').exists():
  raise SystemExit('Campanha atual V2: execute python Tools/level-map/audit_campaign_density_v2.py')
 OUT.mkdir(parents=True,exist_ok=True)
 protected=json.loads((BACKUP/'protected-hashes.json').read_text(encoding='utf8'))
 changed=[rel for rel,digest in protected.items() if sha(ROOT/rel)!=digest]
 assert not changed,('Unrelated assets changed',changed)
 levels=ROOT/'Assets/SweetSugar/Resources/Levels'
 actual={int(p.stem[6:]) for p in levels.glob('Level_*.asset')}
 assert actual==set(range(1,201)),('Wrong campaign catalog',actual-set(range(1,201)),set(range(1,201))-actual)
 targets={int(p.stem[11:]) for p in (levels/'Targets').glob('TargetLevel*.asset') if p.stem[11:].isdigit()}
 assert targets==set(range(1,201)),('Wrong target assets',len(targets))
 preserved=[]
 for p in (BACKUP/'Assets/SweetSugar/Resources/Levels').rglob('*.meta'):
  rel=p.relative_to(BACKUP);assert p.read_bytes()==(ROOT/rel).read_bytes(),('Existing metadata changed',str(rel));preserved.append(str(rel))
 guids=[]
 for p in list(levels.glob('Level_*.asset.meta'))+list((levels/'Targets').glob('TargetLevel*.asset.meta')):
  guid=re.search(r'^guid: (\w+)',p.read_text(encoding='utf8'),re.M);assert guid,('Missing GUID',str(p));guids.append(guid[1])
 assert len(guids)==400 and len(set(guids))==400,'Duplicate campaign GUID'
 rs=json.loads((ROOT/'Tools/level-map/campaign-200/recipes.json').read_text(encoding='utf8'))['levels']
 intro={r['level']:r['intro'] for r in rs if r['intro']}
 assert len(intro)==18
 for r in rs:
  colors={(p['x'],p['y']):p['color'] for p in r['opening']}
  colors.update({(p['x'],p['y']):0 for p in r['specials']})
  for (x,y),v in colors.items():
   assert not (colors.get((x-1,y))==v==colors.get((x-2,y))),('Initial horizontal match',r['level'],x,y)
   assert not (colors.get((x,y-1))==v==colors.get((x,y-2))),('Initial vertical match',r['level'],x,y)
   assert not (colors.get((x-1,y))==v==colors.get((x,y-1))==colors.get((x-1,y-1))),('Initial square',r['level'],x,y)
  assert all(g['name']!='PostalTapes' for g in r['goals'])
 paths=[p for p in levels.rglob('*.asset') if p.name.startswith(('Level_','TargetLevel'))]
 result={'status':'PASS','utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
  'levels':200,'targets':200,'uniqueCampaignGuids':len(set(guids)),
  'existingMetaFilesPreserved':len(preserved),'protectedFilesUnchanged':len(protected),
  'presentations':intro,'mainStructuralResult':(ROOT/'Logs/campaign-200.result').read_text(encoding='utf8'),
  'isolatedNativeResult':(OUT/'campaign-200-native.result').read_text(encoding='utf8'),
  'currentRuntimeFocusedResult':(OUT/'campaign-200-current.result').read_text(encoding='utf8'),
  'finalCatalogFocusedResult':(OUT/'campaign-200-catalog.result').read_text(encoding='utf8'),
  'runtimeSourceAlignment':json.loads((OUT/'runtime-source-alignment.json').read_text(encoding='utf8')),
  'nativeAssetAlignment':json.loads((OUT/'asset-alignment.json').read_text(encoding='utf8')),
  'assetHashes':{p.relative_to(ROOT).as_posix():sha(p) for p in sorted(paths)},
  'sourceHashes':{rel:sha(ROOT/rel) for rel in ['Tools/level-map/build_campaign_200.py','Tools/level-map/campaign-200/recipes.json','Assets/RoyalAves/Editor/Campaign200Tools.cs','Assets/RoyalAves/Scripts/Meta/CampaignElementIntroductions.cs','Assets/RoyalAves/Scripts/Meta/ElementIntroPrototype.cs']},
  'limits':'Structural checks cover 200. Native representative loading/opening and introduction checks are not 200 full wins, win-rate tuning, physical pointer/device/audio or full campaign progression validation.'}
 assert all(result[k].startswith('PASS') for k in ['mainStructuralResult','isolatedNativeResult','currentRuntimeFocusedResult','finalCatalogFocusedResult'])
 assert result['runtimeSourceAlignment']['allRuntimeSourcesMatch'] and result['nativeAssetAlignment']['allCampaignAssetsMatch']
 (OUT/'CAMPAIGN-200.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf8')
 print('PASS: 200 levels, 200 target assets, 400 unique GUIDs,',len(preserved),'existing metadata files preserved,',len(protected),'protected production files unchanged; 18 presentations')
if __name__=='__main__':main()
