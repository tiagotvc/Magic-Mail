const fs=require('fs'),path=require('path'),crypto=require('crypto'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),preview=path.join(root,'Library/MapValidation/PreviewProject'),stage=path.join(root,'Library/MapValidation/neutral-button-v38');
const prefab='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab';
const before=fs.readFileSync(path.join(stage,'before.prefab'),'utf8');
if(fs.readFileSync(path.join(root,prefab),'utf8')!==before)throw Error('Main prefab changed since staging; preserve user edits before installation');
if(!fs.readFileSync(path.join(stage,'validation.txt'),'utf8').startsWith('PASS:'))throw Error('Native button checks not passed');
const mappings=JSON.parse(fs.readFileSync(path.join(root,'Library/MapValidation/pets-cartoon-v24/guid-map.json'),'utf8'));
const remap=t=>{for(const[a,b]of mappings)t=t.replaceAll(b,a);return t;};
const native=remap(fs.readFileSync(path.join(preview,prefab),'utf8')),old=graph(before),next=graph(native);
const affected=p=>/^Pets\/Body\/Workshop\/Incubator[0-3]\/Panel\/Available\/UseItem(?:\/|$)/.test(p);
const stripDefault=t=>t?.replace(/^  enabledTint: \{r: 1, g: 1, b: 1, a: 1\}\n/gm,'');
for(const key of new Set([...old.blocks.keys(),...next.blocks.keys()])){
 const a=old.blocks.get(key),b=next.blocks.get(key),item=b||a;
 if(!affected(item.path)&&a?.text!==b?.text&&stripDefault(a?.text)!==stripDefault(b?.text))throw Error('Unrelated authored component changed: '+key);
}
const assigned=new Map(old.ids),used=new Set(assigned.values());
for(const key of next.ids.keys())if(!assigned.has(key)){let id;do{id=(crypto.randomBytes(8).readBigUInt64LE()&0x7fffffffffffffffn).toString();}while(id==='0'||used.has(id));used.add(id);assigned.set(key,id);}
function chunks(text,g){const byId=new Map([...g.ids].map(([key,id])=>[id,key]));return new Map(text.split(/(?=^--- !u!)/m).flatMap(s=>{const id=s.match(/^--- !u!\d+ &(-?\d+)/m)?.[1];return id?[[byId.get(id),s.trimEnd()+'\n']]:[];}));}
const oldRaw=chunks(before,old),newRaw=chunks(native,next),aliases=new Map([...next.ids].map(([key,id])=>[id,key]));
const nativeIds=t=>t.replace(/^--- !u!(\d+) &(-?\d+)/gm,(s,k,id)=>aliases.has(id)?'--- !u!'+k+' &'+assigned.get(aliases.get(id)):s).replace(/fileID: (-?\d+)/g,(s,id)=>aliases.has(id)?'fileID: '+assigned.get(aliases.get(id)):s);
let result=before.slice(0,before.indexOf('--- !u!'));const seen=new Set();
for(const[key,a]of old.blocks){seen.add(key);if(affected(a.path)){if(next.blocks.has(key))result+=nativeIds(newRaw.get(key));}else result+=oldRaw.get(key);}
for(const[key,b]of next.blocks)if(!seen.has(key)&&affected(b.path))result+=nativeIds(newRaw.get(key));
const final=graph(result);for(const[key,a]of old.blocks)if(!affected(a.path)&&a.text!==final.blocks.get(key)?.text)throw Error('Outside Incubar changed in merge: '+key);
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
for(const[n,sha]of Object.entries(JSON.parse(fs.readFileSync(path.join(stage,'preserved.json'),'utf8'))))if(hash(path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/'+n+'.prefab'))!==sha)throw Error(n+' changed');
const base='Assets/RoyalAves/Art/UI/Button/RoyalPostman/NeutralV37/';
for(const name of ['button-neutral-rim-v38','button-neutral-center-v37']){
 if(hash(path.join(root,base+name+'.png'))!==hash(path.join(preview,base+name+'.png')))throw Error('Original PNG differs');
 for(const rel of [base+name+'.png.meta',base+'Sprites/'+name+'.asset',base+'Sprites/'+name+'.asset.meta']){const dest=path.join(root,rel);fs.mkdirSync(path.dirname(dest),{recursive:true});fs.writeFileSync(dest,remap(fs.readFileSync(path.join(preview,rel),'utf8')));}
}
fs.writeFileSync(path.join(root,prefab),result);
const report='PASS: neutral center V37 + rim V38 installed into Incubar on all four carousel pages; enabled/disabled tint and fixed-rim press verified natively. Every authored component outside Incubar unchanged; Home and Navigation hashes intact; original PNG bytes intact.\n';
const review=path.join(__dirname,'review/neutral-button-v38');for(const f of fs.readdirSync(stage).filter(f=>/\.png$/.test(f)||f==='validation.txt'))fs.copyFileSync(path.join(stage,f),path.join(review,f));fs.writeFileSync(path.join(review,'installation.txt'),report);console.log(report);
