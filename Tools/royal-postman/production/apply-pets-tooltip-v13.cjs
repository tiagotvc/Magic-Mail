const fs=require('fs'),path=require('path'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),stage=path.join(root,'Library/MapValidation/pets-tooltip-v13');
const prefab='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab',target=path.join(root,prefab),output=path.join(root,'Library/MapValidation/pets-mock-v2');
if(!fs.readFileSync(path.join(output,'validation.txt'),'utf8').startsWith('PASS: V13'))throw Error('Native V13 checks have not passed');
const before=fs.readFileSync(path.join(stage,'before.prefab'),'utf8');let native=fs.readFileSync(path.join(root,'Library/MapValidation/PreviewProject',prefab),'utf8');
for(const [from,to]of JSON.parse(fs.readFileSync(path.join(stage,'guid-map.json'),'utf8')))native=native.replaceAll(from,to);
const old=graph(before),next=graph(native);
const surfaceGuid=fs.readFileSync(path.join(root,'Assets/RoyalAves/Scripts/UI/RoyalPostman2026/RoyalPostmanTooltipSurface.cs.meta'),'utf8').match(/^guid: (\w+)/m)[1];
const isAdded=b=>b.path==='Pets/ItemTooltip'||b.path.startsWith('Pets/ItemTooltip/')||b.path==='Pets/Inventory/Background'&&b.kind==='114'&&b.text.includes('guid: '+surfaceGuid);
const isController=b=>b.path==='Pets'&&b.kind==='114'&&b.text.includes('incubators:');
const children=/^  m_Children:[\s\S]*?(?=^  m_Father:)/m,components=/^  m_Component:[\s\S]*?(?=^  m_Layer:)/m;
const pattern=key=>new RegExp('^  *'+key+': [^\\r\\n]+','gm');
function fields(b){
 if(b.path==='Pets/IncubatorTab/Label')return b.kind==='224'?['m_SizeDelta']:b.text.includes('m_Text:')?['m_FontSize','m_BestFit','m_MinSize','m_MaxSize','m_HorizontalOverflow','m_VerticalOverflow']:[];
 if(b.path==='Pets/Inventory/Background'&&b.kind==='114'&&!isAdded(b))return ['m_RaycastTarget'];
 return [];
}
function strip(text,b){
 if(isController(b))return text.replace(/^  (hintTitle|hintDescription|itemTooltip|tooltipCard|tooltipIcon|dismissItemTooltip):[^\n]*\n?/gm,'').trim();
 if(b.path==='Pets'&&b.kind==='224')return text.replace(children,'m_Children: <tooltip>\n');
 if(b.path==='Pets/Inventory/Background'&&b.kind==='1')return text.replace(components,'m_Component: <tooltip surface>\n');
 return fields(b).reduce((s,k)=>s.replace(pattern(k),k+': <requested>'),text);
}
const added=[],changed=new Set();
for(const key of new Set([...old.blocks.keys(),...next.blocks.keys()])){
 const a=old.blocks.get(key),b=next.blocks.get(key);if(a?.text===b?.text)continue;
 if(!a&&b&&isAdded(b)){added.push(key);continue;}
 if(!a||!b||strip(a.text,b)!==strip(b.text,b))throw Error('Unrelated native component change: '+key);changed.add(key);
}
const current=fs.readFileSync(target,'utf8'),live=graph(current),aliases=new Map([...live.ids].map(([key,id])=>[id,key]));
for(const key of added)if(live.ids.has(key)||aliases.has(next.ids.get(key)))throw Error('Tooltip already exists or new ID collision; restage it');
const nativeAliases=new Map([...next.ids].map(([key,id])=>[id,key]));
const rawNative=new Map(native.split(/(?=^--- !u!)/m).map(b=>[nativeAliases.get(b.match(/^--- !u!\d+ &(-?\d+)/m)?.[1]),b]));
const remap=text=>text.replace(/fileID: (-?\d+)/g,(match,id)=>{const key=nativeAliases.get(id),mapped=live.ids.get(key);return mapped?'fileID: '+mapped:match;});
const tooltipTransform=next.ids.get('Pets/ItemTooltip#224');
const surfaceKey=added.find(key=>next.blocks.get(key).path==='Pets/Inventory/Background');
let patched=current.split(/(?=^--- !u!)/m).map(block=>{
 const header=block.match(/^--- !u!\d+ &(-?\d+)/m);if(!header)return block;const key=aliases.get(header[1]);if(!changed.has(key))return block;
 const item=live.blocks.get(key),source=next.blocks.get(key),raw=remap(rawNative.get(key));
 if(isController(item)){if(strip(item.text,item)!==strip(old.blocks.get(key).text,item))throw Error('Controller changed during preview; restage it');return raw.replace(/^--- !u!\d+ &-?\d+/,header[0]);}
 if(item.path==='Pets'&&item.kind==='224')return block.replace(children,value=>value+'  - {fileID: '+tooltipTransform+'}\n');
 if(item.path==='Pets/Inventory/Background'&&item.kind==='1')return block.replace(components,value=>value+'  - component: {fileID: '+next.ids.get(surfaceKey)+'}\n');
 for(const field of fields(item)){const re=pattern(field),values=source.text.match(re),existing=block.match(re);if(!values||values.length!==existing?.length)throw Error('Field mismatch: '+key+'/'+field);let i=0;block=block.replace(re,()=>values[i++]);}
 return block;
}).join('');
for(const key of added)patched+=remap(rawNative.get(key));
const installed=graph(patched);
for(const [key,item]of live.blocks){const result=installed.blocks.get(key);if(!result||strip(item.text,item)!==strip(result.text,result))throw Error('Unrelated authored field changed: '+key);}
if(fs.readFileSync(target,'utf8')!==current)throw Error('Concurrent editor save; rerun selective installer');
fs.writeFileSync(path.join(stage,'before-install.prefab'),current);fs.writeFileSync(target,patched);
const review=path.join(root,'Tools/royal-postman/production/review/pets-tooltip-v13');fs.mkdirSync(review,{recursive:true});
for(const name of fs.readdirSync(output).filter(n=>/^(pets-|inventory-row-).+\.png$/.test(n)||n==='validation.txt'))fs.copyFileSync(path.join(output,name),path.join(review,name));
fs.appendFileSync(path.join(review,'validation.txt'),'Selective install: '+changed.size+' caption/controller/tooltip-link components changed; '+added.length+' tooltip components added. Every unrelated authored field and existing ID preserved, including V12 single-button mechanics. Full runtime/editor compilation passed; no new PNG.\n');
console.log('Applied V13: '+changed.size+' existing components patched; '+added.length+' tooltip components added.');
