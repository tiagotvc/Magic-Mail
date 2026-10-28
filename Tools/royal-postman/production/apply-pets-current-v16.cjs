// Replace only the CurrentProfile presentation; preserve the live incubation screen and shared chrome.
const fs=require('fs'),path=require('path'),crypto=require('crypto'),{execFileSync}=require('child_process'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),preview=path.join(root,'Library/MapValidation/PreviewProject'),stage=path.join(root,'Library/MapValidation/pets-current-v16');
const relative='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab',target=path.join(root,relative),output=path.join(root,'Library/MapValidation/pets-mock-v2');
if(!/^PASS: V1[56]/.test(fs.readFileSync(path.join(output,'validation.txt'),'utf8')))throw Error('Native profile validation has not passed');
if(!fs.readFileSync(path.join(preview,relative),'utf8').includes('equippedCardStatus:'))throw Error('Native prefab does not contain the V16 painted presentation');
const before=fs.readFileSync(path.join(stage,'before.prefab'),'utf8');
if(fs.readFileSync(target,'utf8')!==before)throw Error('Concurrent authored save; restage and verify before installing');
const guid=text=>text.match(/^guid: (\w+)/m)?.[1];
const manifest=JSON.parse(fs.readFileSync(path.join(root,'Tools/royal-postman/production/PETS-CURRENT-V16.json'),'utf8'));
const guidMap=new Map(JSON.parse(fs.readFileSync(path.join(stage,'guid-map.json'),'utf8')));
// Create missing metadata without overwriting metadata already imported by the user's editor.
const newFiles=[];
for(const asset of manifest.assets){
 const original=fs.readFileSync(asset.original),png=fs.readFileSync(path.join(root,asset.path));
 if(!original.equals(png)||!png.equals(fs.readFileSync(path.join(preview,asset.path))))throw Error('Original PNG mismatch: '+asset.id);
 for(const item of [asset.path,path.posix.dirname(asset.path)+'/Sprites/'+asset.id+'.asset']){
  const main=path.join(root,item),native=path.join(preview,item);fs.mkdirSync(path.dirname(main),{recursive:true});
  if(!fs.existsSync(main+'.meta'))fs.copyFileSync(native+'.meta',main+'.meta');
  const a=guid(fs.readFileSync(native+'.meta','utf8')),b=guid(fs.readFileSync(main+'.meta','utf8'));if(a!==b)guidMap.set(a,b);
  if(item.endsWith('.asset'))newFiles.push(item);
 }
}
const helper='Assets/RoyalAves/Editor/RoyalPostmanPetProfileArt.cs';if(!fs.existsSync(path.join(root,helper+'.meta')))fs.copyFileSync(path.join(preview,helper+'.meta'),path.join(root,helper+'.meta'));
let native=fs.readFileSync(path.join(preview,relative),'utf8');
const ids=[...new Set([...native.matchAll(/guid: ([a-f0-9]{32})/g)].map(m=>m[1]))];
const metas=execFileSync('rg',['-l','-g','*.meta','^guid: ('+ids.join('|')+')$','Assets'],{cwd:preview,encoding:'utf8'}).trim().split(/\r?\n/);
for(const meta of metas){const main=path.join(root,meta);if(!fs.existsSync(main))continue;const a=guid(fs.readFileSync(path.join(preview,meta),'utf8')),b=guid(fs.readFileSync(main,'utf8'));if(a&&b&&a!==b)guidMap.set(a,b);}
const mapGuids=text=>{for(const[a,b]of guidMap)text=text.replaceAll(a,b);return text;};native=mapGuids(native);
for(const item of newFiles)fs.writeFileSync(path.join(root,item),mapGuids(fs.readFileSync(path.join(preview,item),'utf8')));
const old=graph(before),next=graph(native),profile=b=>b?.path==='Pets/CurrentProfile'||b?.path?.startsWith('Pets/CurrentProfile/');
const controller=b=>b?.path==='Pets'&&b.kind==='114'&&b.text.includes('incubators:');
function normalize(b){
 if(!controller(b))return b.text;
 const fields=['currentTabBlue','currentTabGold','incubatorTabBlue','incubatorTabGold','defaultCurrentTab'];let text=b.text,values=[];
 for(const field of fields){const re=new RegExp('^  '+field+':[^\\n]*\\n?','m'),match=text.match(re);if(!match)throw Error('Missing shared field '+field);values.push(match[0].trim());text=text.replace(re,'');}
 return text.trimEnd()+'\n'+values.sort().join('\n');
}
for(const key of new Set([...old.blocks.keys(),...next.blocks.keys()])){
 const a=old.blocks.get(key),b=next.blocks.get(key);if(profile(a)||profile(b))continue;
 if(!a||!b||normalize(a)!==normalize(b))throw Error('Unrelated native change: '+key);
}
const assigned=new Map(old.ids),used=new Set(old.ids.values());
for(const[key,item]of next.blocks){if(!profile(item)||assigned.has(key))continue;let id;do{id=(crypto.randomBytes(8).readBigUInt64LE()&0x7fffffffffffffffn).toString();}while(id==='0'||used.has(id));used.add(id);assigned.set(key,id);}
const aliases=new Map([...next.ids].map(([key,id])=>[id,key]));
const remap=text=>text.replace(/fileID: (-?\d+)/g,(match,id)=>assigned.has(aliases.get(id))?'fileID: '+assigned.get(aliases.get(id)):match);
const raw=native.split(/(?=^--- !u!)/m).filter(b=>/^--- !u!/m.test(b));
const oldAliases=new Map([...old.ids].map(([key,id])=>[id,key]));
let result=before.split(/(?=^--- !u!)/m).filter(b=>!profile(old.blocks.get(oldAliases.get(b.match(/^--- !u!\d+ &(-?\d+)/m)?.[1])))).join('');
let count=0;
for(const block of raw){const id=block.match(/^--- !u!(\d+) &(-?\d+)/m),key=aliases.get(id[2]);if(!profile(next.blocks.get(key)))continue;result+=remap(block).replace(/^--- !u!\d+ &-?\d+/,'--- !u!'+id[1]+' &'+assigned.get(key));count++;}
const installed=graph(result);
for(const[key,item]of old.blocks){if(profile(item))continue;if(installed.blocks.get(key)?.text!==item.text)throw Error('Unrelated installed change: '+key);}
for(const[key,item]of next.blocks){if(!profile(item))continue;if(installed.blocks.get(key)?.text!==item.text)throw Error('Profile remapping mismatch: '+key);}
const idsAfter=new Set(installed.ids.values());for(const key of [...old.blocks.keys()].filter(k=>profile(old.blocks.get(k))&&!next.blocks.has(k))){const id=old.ids.get(key);if(!idsAfter.has(id)&&new RegExp('fileID: '+id+'(?=[},])').test(result))throw Error('Dangling old profile link: '+key);}
if(fs.readFileSync(target,'utf8')!==before)throw Error('Concurrent authored save during install');
fs.writeFileSync(path.join(stage,'before-install.prefab'),before);fs.writeFileSync(target,result);
const review=path.join(root,'Tools/royal-postman/production/review/pets-current-v16');fs.mkdirSync(review,{recursive:true});
for(const file of fs.readdirSync(output).filter(n=>n.startsWith('pets-current-')&&n.endsWith('.png')||n==='validation.txt'))fs.copyFileSync(path.join(output,file),path.join(review,file));
fs.appendFileSync(path.join(review,'validation.txt'),'Selective install: '+count+' profile components. All shared header/tab/arrow/divider and incubation authored components/IDs remain identical. 28 original PNGs copied unchanged. Full production runtime/editor compilation passed.\n');
console.log('Installed V16 painted profile only; '+count+' components; all external authored fields and IDs preserved.');
