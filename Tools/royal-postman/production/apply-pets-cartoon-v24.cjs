const fs=require('fs'),path=require('path'),crypto=require('crypto'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),preview=path.join(root,'Library/MapValidation/PreviewProject'),stage=path.join(root,'Library/MapValidation/pets-cartoon-v24');
const rel='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab',target=path.join(root,rel),before=fs.readFileSync(path.join(stage,'before.prefab'),'utf8');
if(fs.readFileSync(target,'utf8')!==before)throw Error('Main prefab changed since staging; preserve and rebase before installing');
const mappings=JSON.parse(fs.readFileSync(path.join(stage,'guid-map.json'),'utf8'));
const remap=text=>{for(const[a,b]of mappings)text=text.replaceAll(b,a);return text;};
let native=remap(fs.readFileSync(path.join(preview,rel),'utf8'));
const old=graph(before),next=graph(native);
if(!native.includes('cartoonLayout: 1'))throw Error('Assembled cartoon UI missing');
for(const[key,a]of old.blocks)if(a.path.startsWith('Pets/CurrentProfile')&&a.text!==next.blocks.get(key)?.text)throw Error('Current profile modified: '+key);
const allowed=['Pets/Background','Pets/Header','Pets/Room','Pets/Body','Pets/Inventory','Pets/Divider','Pets/CurrentTab','Pets/IncubatorTab','Pets/AcceleratorStock','Pets/HelpPanel/Card/Copy'];
for(const key of new Set([...old.blocks.keys(),...next.blocks.keys()])){
 const a=old.blocks.get(key),b=next.blocks.get(key),item=b||a;if(a?.text===b?.text)continue;
 if(item.path==='Pets'||allowed.some(p=>item.path===p||item.path.startsWith(p+'/')))continue;
 throw Error('Unrelated authored component changed: '+key);
}
const assigned=new Map(old.ids),used=new Set(assigned.values());for(const key of next.ids.keys())if(!assigned.has(key)){let id;do{id=(crypto.randomBytes(8).readBigUInt64LE()&0x7fffffffffffffffn).toString();}while(id==='0'||used.has(id));used.add(id);assigned.set(key,id);}
const aliases=new Map([...next.ids].map(([key,id])=>[id,key]));
native=native.replace(/^--- !u!(\d+) &(-?\d+)/gm,(all,kind,id)=>aliases.has(id)?'--- !u!'+kind+' &'+assigned.get(aliases.get(id)):all).replace(/fileID: (-?\d+)/g,(all,id)=>aliases.has(id)?'fileID: '+assigned.get(aliases.get(id)):all);
const final=graph(native);for(const[key,a]of old.blocks)if(a.path.startsWith('Pets/CurrentProfile')&&a.text!==final.blocks.get(key)?.text)throw Error('Current profile changed in remap');
const preserved=JSON.parse(fs.readFileSync(path.join(stage,'preserved.json'),'utf8'));
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
for(const[n,sha]of Object.entries(preserved))if(hash(path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/'+n+'.prefab'))!==sha)throw Error(n+' changed');
const manifest=JSON.parse(fs.readFileSync(path.join(__dirname,'PETS-CARTOON-V24.json'),'utf8'));
const selected=manifest.assets.filter(a=>a.path&&(a.status==='generated'||a.status.startsWith('restored')));
for(const a of selected)if(hash(path.join(root,a.path))!==a.sha256)throw Error('Original PNG changed: '+a.id);
const resources=new Set(selected.map(a=>a.path+'.meta'));
for(const group of ['Menu','Button','Icon']){
 const base='Assets/RoyalAves/Art/UI/'+group+'/RoyalPostman/PetsCartoon';
 for(const name of ['Sprites','Materials']){const dir=path.join(preview,base,name);if(!fs.existsSync(dir))continue;for(const file of fs.readdirSync(dir))if(/\.(asset|mat|meta)$/.test(file))resources.add(base+'/'+name+'/'+file);}
}
for(const resource of resources){const dest=path.join(root,resource);fs.mkdirSync(path.dirname(dest),{recursive:true});fs.writeFileSync(dest,remap(fs.readFileSync(path.join(preview,resource),'utf8')));}
fs.writeFileSync(target,native);
fs.writeFileSync(path.join(stage,'installation.txt'),'Installed Pets cartoon V24 into real prefab. '+selected.length+' original paint sources; '+resources.size+' native import/resource files. CurrentProfile, Home and Navigation preserved.\n');
console.log(fs.readFileSync(path.join(stage,'installation.txt'),'utf8'));
