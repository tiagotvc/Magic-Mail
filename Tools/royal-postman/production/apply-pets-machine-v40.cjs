const fs=require('fs'),path=require('path'),crypto=require('crypto'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),preview=path.join(root,'Library/MapValidation/PreviewProject'),stage=path.join(root,'Library/MapValidation/pets-machine-v40'),prefab='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab';
const before=fs.readFileSync(path.join(stage,'before.prefab'),'utf8');if(fs.readFileSync(path.join(root,prefab),'utf8')!==before)throw Error('Main prefab changed during staging; preserve user edits');
const validation=fs.readFileSync(path.join(stage,'validation.txt'),'utf8');if(!validation.startsWith('PASS:')||!validation.includes('center V42'))throw Error('Native assembly/new-center validation incomplete');
const mappings=JSON.parse(fs.readFileSync(path.join(root,'Library/MapValidation/pets-cartoon-v24/guid-map.json'),'utf8')),remap=t=>{for(const[a,b]of mappings)t=t.replaceAll(b,a);return t;};
const native=remap(fs.readFileSync(path.join(preview,prefab),'utf8')),old=graph(before),next=graph(native);
const affected=p=>p==='Pets/Body/Workshop/ControlPanel'||/^Pets\/Body\/Workshop\/Incubator[0-3]\/Panel\/Available(?:\/|$)/.test(p);
const controller=b=>b?.path==='Pets'&&b?.kind==='114'&&b.text.includes('incubators:');
const strip=t=>t?.replace(/^  enabledTint: \{r: 1, g: 1, b: 1, a: 1\}\n/gm,'').replace(/^  integratedMachineLayout: [01]\n/gm,'');
for(const key of new Set([...old.blocks.keys(),...next.blocks.keys()])){const a=old.blocks.get(key),b=next.blocks.get(key),item=b||a;if(!affected(item.path)&&strip(a?.text)!==strip(b?.text))throw Error('Outside machine actions/controller changed: '+key);}
const assigned=new Map(old.ids),used=new Set(assigned.values());for(const key of next.ids.keys())if(!assigned.has(key)){let id;do{id=(crypto.randomBytes(8).readBigUInt64LE()&0x7fffffffffffffffn).toString();}while(id==='0'||used.has(id));used.add(id);assigned.set(key,id);}
const aliases=new Map([...next.ids].map(([key,id])=>[id,key]));
const nativeIds=t=>t.replace(/^--- !u!(\d+) &(-?\d+)/gm,(s,k,id)=>aliases.has(id)?'--- !u!'+k+' &'+assigned.get(aliases.get(id)):s).replace(/fileID: (-?\d+)/g,(s,id)=>aliases.has(id)?'fileID: '+assigned.get(aliases.get(id)):s);
function chunks(text,g){const byId=new Map([...g.ids].map(([key,id])=>[id,key]));return new Map(text.split(/(?=^--- !u!)/m).flatMap(s=>{const id=s.match(/^--- !u!\d+ &(-?\d+)/m)?.[1];return id?[[byId.get(id),s.trimEnd()+'\n']]:[];}));}
const oldRaw=chunks(before,old),newRaw=chunks(native,next);let result=before.slice(0,before.indexOf('--- !u!'));const seen=new Set();
for(const[key,a]of old.blocks){seen.add(key);if(affected(a.path)||controller(a)){if(next.blocks.has(key))result+=nativeIds(newRaw.get(key));}else result+=oldRaw.get(key);}
for(const[key,b]of next.blocks)if(!seen.has(key)&&affected(b.path))result+=nativeIds(newRaw.get(key));
const final=graph(result);for(const[key,a]of old.blocks)if(!affected(a.path)&&!controller(a)&&a.text!==final.blocks.get(key)?.text)throw Error('Untouched UI component changed: '+key);
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');for(const[n,hash]of Object.entries(JSON.parse(fs.readFileSync(path.join(stage,'preserved.json'),'utf8'))))if(sha(path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/'+n+'.prefab'))!==hash)throw Error(n+' changed');
const housing='Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsMachineV40/',buttons='Assets/RoyalAves/Art/UI/Button/RoyalPostman/NeutralV40/';
const paints=[housing+'pets-machine-housing-v40.png',buttons+'button-reference-rim-v40.png',buttons+'button-reference-center-v40.png',buttons+'button-reference-center-v42.png'];
for(const rel of paints){if(sha(path.join(root,rel))!==sha(path.join(preview,rel)))throw Error('Original paint PNG changed: '+rel);fs.writeFileSync(path.join(root,rel+'.meta'),remap(fs.readFileSync(path.join(preview,rel+'.meta'),'utf8')));}
const resources=[housing+'Sprites/pets-machine-housing-v40.asset',buttons+'Sprites/button-reference-rim-v40.asset',buttons+'Sprites/button-reference-center-v40.asset',buttons+'Sprites/button-reference-center-v42.asset',buttons+'Materials/button-neutral-palette-v40.mat'];
for(const rel of resources)for(const file of [rel,rel+'.meta']){const dest=path.join(root,file);fs.mkdirSync(path.dirname(dest),{recursive:true});fs.writeFileSync(dest,remap(fs.readFileSync(path.join(preview,file),'utf8')));}
fs.writeFileSync(path.join(root,prefab),result);
const review=path.join(__dirname,'review/pets-machine-v40');for(const file of fs.readdirSync(stage).filter(f=>f.endsWith('.png')||f==='validation.txt'))fs.copyFileSync(path.join(stage,file),path.join(review,file));
const report='PASS: integrated machine housing + both larger470x230 actions installed in REAL Pets.prefab; separate neutral rim V40 / corrected center V42 and independent palette tints. Egg inventory/pedestals/rail, apparatus/egg sprites, header, profile, locks, economy, Home and Navigation preserved. Original generated PNG bytes unchanged.\n';fs.writeFileSync(path.join(review,'installation.txt'),report);console.log(report);
