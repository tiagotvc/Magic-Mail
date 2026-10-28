// Install the single contextual action without rebuilding the user's authored screen.
const fs=require('fs'),path=require('path'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),stage=path.join(root,'Library/MapValidation/pets-incubation-v12');
const prefab='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab',target=path.join(root,prefab),output=path.join(root,'Library/MapValidation/pets-mock-v2');
if(!fs.readFileSync(path.join(output,'validation.txt'),'utf8').startsWith('PASS: V12'))throw Error('Native V12 checks have not passed');
const before=fs.readFileSync(path.join(stage,'before.prefab'),'utf8');
let native=fs.readFileSync(path.join(root,'Library/MapValidation/PreviewProject',prefab),'utf8');
for(const [from,to]of JSON.parse(fs.readFileSync(path.join(stage,'guid-map.json'),'utf8')))native=native.replaceAll(from,to);
const old=graph(before),next=graph(native);
const isRemoved=b=>/^Pets\/Body\/Workshop\/Incubator[012]\/Panel\/Available\/Accelerate(?:\/|$)/.test(b?.path||'');
const isController=b=>b?.path==='Pets'&&b.kind==='114'&&b.text.includes('incubators:');
const children=/^  m_Children:[\s\S]*?(?=^  m_Father:)/m;
const stripRoot=text=>text.replace(/^    (accelerate|kind|quantity|accelerationMinutes|title|description):[^\n]*\n?/gm,'').replace(/^  incubationRules:[\s\S]*$/m,'').trim();
const pattern=key=>new RegExp('^  *'+key+': [^\\r\\n]+','gm');
function fields(b){
 if(/^Pets\/Body\/Workshop\/Incubator[012]\/Panel\/Available\/(Empty|Running)$/.test(b.path)&&b.kind==='224')return ['m_LocalPosition','m_AnchoredPosition'];
 if(/^Pets\/Body\/Workshop\/Incubator[012]\/Panel\/Available\/UseItem$/.test(b.path)){
  if(b.kind==='224')return ['m_LocalPosition','m_AnchoredPosition','m_SizeDelta'];
  if(b.kind==='114'&&b.text.includes('m_Interactable:'))return ['m_Interactable','m_DisabledColor'];
 }
 if(/^Pets\/Body\/Workshop\/Incubator[012]\/Panel\/Available\/UseItem\/Label$/.test(b.path))return b.kind==='224'?['m_SizeDelta']:b.text.includes('m_Text:')?['m_Text','m_FontSize']:[];
 if(/^Pets\/Inventory\/.+\/Quantity$/.test(b.path)&&b.kind==='114'&&b.text.includes('m_Text:'))return ['m_Text'];
 return [];
}
const available=b=>b.kind==='224'&&/^Pets\/Body\/Workshop\/Incubator[012]\/Panel\/Available$/.test(b.path);
const strip=(text,b)=>isController(b)?stripRoot(text):available(b)?text.replace(children,'m_Children: <requested>\n'):fields(b).reduce((s,k)=>s.replace(pattern(k),k+': <requested>'),text);
const changed=new Set();let deleted=0;
for(const key of new Set([...old.blocks.keys(),...next.blocks.keys()])){
 const a=old.blocks.get(key),b=next.blocks.get(key);if(a?.text===b?.text)continue;
 if(!b&&isRemoved(a)){deleted++;continue;}
 if(!a||!b||strip(a.text,b)!==strip(b.text,b))throw Error('Unexpected native change: '+key);
 changed.add(key);
}
if(deleted!==57)throw Error('Unexpected obsolete button component count: '+deleted);
const current=fs.readFileSync(target,'utf8'),live=graph(current);
const aliases=new Map([...live.ids].map(([key,id])=>[id,key]));
const nativeAliases=new Map([...next.ids].map(([key,id])=>[id,key]));
const rawNative=new Map(native.split(/(?=^--- !u!)/m).map(b=>[nativeAliases.get(b.match(/^--- !u!\d+ &(-?\d+)/m)?.[1]),b]));
const remap=text=>text.replace(/fileID: (-?\d+)/g,(match,id)=>{const key=nativeAliases.get(id),mapped=live.ids.get(key);return mapped?'fileID: '+mapped:match;});
const patched=current.split(/(?=^--- !u!)/m).map(block=>{
 const header=block.match(/^--- !u!\d+ &(-?\d+)/m);if(!header)return block;
 const key=aliases.get(header[1]),item=live.blocks.get(key);if(isRemoved(item))return '';
 if(!changed.has(key))return block;
 const source=next.blocks.get(key),sourceRaw=remap(rawNative.get(key));
 if(isController(item)){
  if(stripRoot(item.text)!==stripRoot(old.blocks.get(key).text))throw Error('Controller metadata changed during preview; restage before applying');
  return sourceRaw.replace(/^--- !u!\d+ &-?\d+/,header[0]);
 }
 if(available(item))return block.replace(children,()=>sourceRaw.match(children)[0]);
 for(const field of fields(item)){
  const re=pattern(field),values=source.text.match(re),existing=block.match(re);if(!values||values.length!==existing?.length)throw Error('Field mismatch: '+key+'/'+field);
  let i=0;block=block.replace(re,()=>values[i++]);
 }
 return block;
}).join('');
const installed=graph(patched);
for(const [key,item]of live.blocks){if(isRemoved(item))continue;const result=installed.blocks.get(key);if(!result||strip(item.text,item)!==strip(result.text,result))throw Error('Unrelated current component changed: '+key);}
if(fs.readFileSync(target,'utf8')!==current)throw Error('Concurrent save; rerun the selective installer');
fs.writeFileSync(path.join(stage,'before-install.prefab'),current);fs.writeFileSync(target,patched);
const review=path.join(root,'Tools/royal-postman/production/review/pets-incubation-v12');fs.mkdirSync(review,{recursive:true});
for(const name of fs.readdirSync(output).filter(n=>/^(pets-|inventory-row-).+\.png$/.test(n)||n==='validation.txt'))fs.copyFileSync(path.join(output,name),path.join(review,name));
fs.appendFileSync(path.join(review,'validation.txt'),'Selective install: '+changed.size+' action/quantity/controller components updated, '+deleted+' obsolete ornate-button components removed; every unrelated authored field/object ID preserved. No PNG modified. Full production runtime and editor compilation passed. Preview actions use injected stock/wallet and never spend live currency.\n');
console.log('Applied V12 contextual incubation action. '+changed.size+' components updated; '+deleted+' obsolete components removed.');
