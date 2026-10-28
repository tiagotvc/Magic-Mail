// Mount the new Current profile while retaining the latest authored incubation layout.
const fs=require('fs'),path=require('path'),crypto=require('crypto'),{execFileSync}=require('child_process'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),preview=path.join(root,'Library/MapValidation/PreviewProject'),stage=path.join(root,'Library/MapValidation/pets-current-v15');
const prefab='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab',target=path.join(root,prefab),output=path.join(root,'Library/MapValidation/pets-mock-v2');
if(!fs.readFileSync(path.join(output,'validation.txt'),'utf8').startsWith('PASS: V15'))throw Error('Native V15 checks have not passed');
const before=fs.readFileSync(path.join(stage,'before.prefab'),'utf8');let native=fs.readFileSync(path.join(preview,prefab),'utf8');
const guid=text=>text.match(/^guid: (\w+)/m)?.[1],guidMap=new Map(JSON.parse(fs.readFileSync(path.join(stage,'guid-map.json'),'utf8')));
// Newly referenced shared icons may not have appeared in the old prefab. Resolve their actual Main GUIDs too.
const ids=[...new Set([...native.matchAll(/guid: ([a-f0-9]{32})/g)].map(m=>m[1]))];
const metas=execFileSync('rg',['-l','-g','*.meta','^guid: ('+ids.join('|')+')$','Assets'],{cwd:preview,encoding:'utf8'}).trim().split(/\r?\n/);
for(const relative of metas){const main=path.join(root,relative);if(!fs.existsSync(main))continue;const a=guid(fs.readFileSync(path.join(preview,relative),'utf8')),b=guid(fs.readFileSync(main,'utf8'));if(a&&b&&a!==b)guidMap.set(a,b);}
for(const [a,b]of guidMap)native=native.replaceAll(a,b);
const old=graph(before),next=graph(native);
const isAdded=b=>b.path==='Pets/CurrentProfile'||b.path.startsWith('Pets/CurrentProfile/');
const isRemoved=b=>b?.path==='Pets/Body/CurrentPet'||b?.path?.startsWith('Pets/Body/CurrentPet/');
const controller=b=>b.path==='Pets'&&b.kind==='114'&&b.text.includes('incubators:');
const children=/^  m_Children:[\s\S]*?(?=^  m_Father:)/m;
const pattern=field=>new RegExp('^  *'+field+': [^\\r\\n]+','gm');
function fields(b){
 if(controller(b))return ['currentPanel','defaultCurrentTab'];
 if(b.kind==='1'&&['Pets/Room','Pets/Inventory','Pets/Divider','Pets/Body/Workshop','Pets/CurrentTab/Selected','Pets/IncubatorTab/Selected'].includes(b.path))return ['m_IsActive'];
 if(b.kind==='114'&&/^Pets\/(CurrentTab|IncubatorTab)\/Face$/.test(b.path))return ['m_Sprite','m_Color'];
 return [];
}
function strip(text,b){
 if(b.kind==='224'&&['Pets','Pets/Body'].includes(b.path))return text.replace(children,'m_Children: <profile>\n');
 // Unity moves fields previously appended by the V14 installer back into declaration order.
 if(controller(b))text=text.replace(/^  (currentTabBlue|currentTabGold|incubatorTabBlue|incubatorTabGold):[^\n]*\n?/gm,'');
 return fields(b).reduce((s,k)=>controller(b)?s.replace(new RegExp('^  '+k+':[^\\n]*\\n?','gm'),''):s.replace(pattern(k),k+': <requested>'),text).trim();
}
const added=[],removed=[],changed=new Set();
for(const key of old.blocks.keys())if(controller(old.blocks.get(key)))for(const field of ['currentTabBlue','currentTabGold','incubatorTabBlue','incubatorTabGold'])if(old.blocks.get(key).text.match(pattern(field))?.[0]!==next.blocks.get(key).text.match(pattern(field))?.[0])throw Error('Shared tab sprite changed: '+field);
for(const key of new Set([...old.blocks.keys(),...next.blocks.keys()])){
 const a=old.blocks.get(key),b=next.blocks.get(key);if(a?.text===b?.text)continue;
 if(!a&&b&&isAdded(b)){added.push(key);continue;}if(a&&!b&&isRemoved(a)){removed.push(key);continue;}
 if(!a||!b||strip(a.text,b)!==strip(b.text,b))throw Error('Unrelated native change: '+key);changed.add(key);
}
const manifest=JSON.parse(fs.readFileSync(path.join(root,'Tools/royal-postman/production/PETS-CURRENT-V15.json'),'utf8'));
for(const asset of manifest.assets){
 const source=path.join(preview,asset.path),main=path.join(root,asset.path);if(!fs.readFileSync(source).equals(fs.readFileSync(main)))throw Error('PNG pixels differ: '+asset.id);
 const relative=path.posix.dirname(asset.path)+'/Sprites/'+asset.id+'.asset';let data=fs.readFileSync(path.join(preview,relative),'utf8');for(const[a,b]of guidMap)data=data.replaceAll(a,b);
 const normalize=text=>text.replace(/\r/g,'').replace(/m_RenderDataKey:\s*\n\s+[a-f0-9]{32}: \d+/,'m_RenderDataKey: native');
 for(const [suffix,value]of [['',data],['.meta',fs.readFileSync(path.join(preview,relative+'.meta'),'utf8')]]){const destination=path.join(root,relative+suffix);if(fs.existsSync(destination)&&normalize(fs.readFileSync(destination,'utf8'))!==normalize(value))throw Error('Unexpected V15 Sprite collision: '+relative+suffix);fs.writeFileSync(destination,value);}
}
const current=fs.readFileSync(target,'utf8'),live=graph(current),aliases=new Map([...live.ids].map(([key,id])=>[id,key]));
for(const key of added)if(live.ids.has(key))throw Error('Current profile already exists; restage');
// Unity may reuse/reassign IDs when cloning the divider. Keep every existing Main ID and give new nodes fresh IDs.
const assigned=new Map(live.ids),used=new Set(live.ids.values());
for(const key of added){let id;do{id=(crypto.randomBytes(8).readBigUInt64LE()&0x7fffffffffffffffn).toString();}while(id==='0'||used.has(id));used.add(id);assigned.set(key,id);}
const nativeAliases=new Map([...next.ids].map(([key,id])=>[id,key]));
const raw=new Map(native.split(/(?=^--- !u!)/m).map(b=>[nativeAliases.get(b.match(/^--- !u!\d+ &(-?\d+)/m)?.[1]),b]));
const remap=text=>text.replace(/fileID: (-?\d+)/g,(match,id)=>{const mapped=assigned.get(nativeAliases.get(id));return mapped?'fileID: '+mapped:match;});
let patched=current.split(/(?=^--- !u!)/m).map(block=>{
 const id=block.match(/^--- !u!\d+ &(-?\d+)/m)?.[1],key=aliases.get(id),item=live.blocks.get(key);if(!item)return block;if(isRemoved(item))return '';
 if(!changed.has(key))return block;
 if(item.kind==='224'&&item.path==='Pets'){
  const tab=live.ids.get('Pets/CurrentTab#224'),newProfile=assigned.get('Pets/CurrentProfile#224');
  const marker='  - {fileID: '+tab+'}';if(!block.includes(marker))throw Error('Current tab root missing');return block.replace(marker,'  - {fileID: '+newProfile+'}\n'+marker);
 }
 if(item.kind==='224'&&item.path==='Pets/Body'){
  const oldPanel=live.ids.get('Pets/Body/CurrentPet#224');return block.replace(new RegExp('^  - \\{fileID: '+oldPanel+'\\}\\r?\\n','m'),'');
 }
 const source=raw.get(key);
 for(const field of fields(item)){const re=pattern(field),value=source.match(re)?.[0];if(!value)throw Error('Missing profile field '+field);const mapped=remap(value);if(re.test(block))block=block.replace(re,()=>mapped);else if(controller(item))block=block.trimEnd()+'\n'+mapped+'\n';else throw Error('Current field missing '+field);}
 return block;
}).join('');
for(const key of added)patched+=remap(raw.get(key)).replace(/^--- !u!\d+ &-?\d+/,'--- !u!'+next.blocks.get(key).kind+' &'+assigned.get(key));
if(/fileID: Pets\//.test(patched))throw Error('Unresolved semantic reference');
for(const key of removed){const id=live.ids.get(key);if(id&&new RegExp('fileID: '+id+'(?=[},])').test(patched))throw Error('Dangling removed placeholder reference: '+key);}
const installed=graph(patched);
for(const [key,item]of live.blocks){if(isRemoved(item))continue;const result=installed.blocks.get(key);if(!result||strip(item.text,item)!==strip(result.text,result))throw Error('Unrelated authored change: '+key);}
if(fs.readFileSync(target,'utf8')!==current)throw Error('Concurrent save; rerun selective installer');
fs.writeFileSync(path.join(stage,'before-install.prefab'),current);fs.writeFileSync(target,patched);
const review=path.join(root,'Tools/royal-postman/production/review/pets-current-v15');fs.mkdirSync(review,{recursive:true});
for(const name of fs.readdirSync(output).filter(n=>/^(pets-|inventory-row-).+\.png$/.test(n)||n==='validation.txt'))fs.copyFileSync(path.join(output,name),path.join(review,name));
fs.appendFileSync(path.join(review,'validation.txt'),'Selective install: '+added.length+' profile components added; '+removed.length+' old placeholder components removed; '+changed.size+' default-tab/visibility/link components patched. Every other existing authored field and ID preserved. Eight original PNGs installed without pixel edits, shared assets unchanged; full production runtime/editor compile passed. Pet catalogue/equipment/skills are presentation mocks, not economy or combat grants.\n');
console.log('Applied Current profile: '+added.length+' added, '+removed.length+' old placeholder components removed; incubation layout preserved.');
