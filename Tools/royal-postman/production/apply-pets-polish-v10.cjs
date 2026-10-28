// Patch only requested fields onto the latest authored screen, preserving IDs and other edits.
const fs=require('fs'),path=require('path'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),stage=path.join(root,'Library/MapValidation/pets-polish-v10');
const relative='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab',target=path.join(root,relative);
const output=path.join(root,'Library/MapValidation/pets-mock-v2');
if(!fs.readFileSync(path.join(output,'validation.txt'),'utf8').startsWith('PASS: V10'))throw Error('Native V10 validation has not passed');
const before=fs.readFileSync(path.join(stage,'before.prefab'),'utf8');
let native=fs.readFileSync(path.join(root,'Library/MapValidation/PreviewProject',relative),'utf8');
for(const [from,to]of JSON.parse(fs.readFileSync(path.join(stage,'guid-map.json'),'utf8')))native=native.replaceAll(from,to);
const original=graph(before),verified=graph(native);
function fields(b){
 if(b.path==='Pets'&&b.kind==='114'&&b.text.includes('activeDotColor:'))return ['activeDotColor','inactiveDotColor'];
 if(b.path==='Pets/Divider/Ornament'&&b.kind==='224')return ['m_SizeDelta'];
 if(/^Pets\/Divider\/(Left|Right)(Tip|Line)$/.test(b.path)){
  if(b.kind==='224')return ['m_LocalPosition','m_LocalScale','m_AnchoredPosition','m_SizeDelta'];
  if(b.kind==='114'&&b.text.includes('m_PixelsPerUnitMultiplier:'))return ['m_PixelsPerUnitMultiplier'];
 }
 if(/^Pets\/Inventory\/(EggsAndGems|Acceleration)$/.test(b.path)&&b.kind==='224')return ['m_AnchoredPosition'];
 if(/^Pets\/Inventory\/(EggsAndGems|Acceleration)\/PageDots\/Dot\d+\/Face$/.test(b.path)&&b.kind==='114'&&b.text.includes('symbol: 8'))return ['m_Color'];
 return [];
}
const pattern=field=>new RegExp('^  *'+field+': [^\\r\\n]+','gm');
const without=(text,keys)=>keys.reduce((value,key)=>value.replace(pattern(key),key+': <requested>'),text);
let validated=0;
for(const key of new Set([...original.blocks.keys(),...verified.blocks.keys()])){
 const a=original.blocks.get(key),b=verified.blocks.get(key);if(a?.text===b?.text)continue;
 if(!a||!b)throw Error('Unexpected hierarchy change: '+key);
 const keys=fields(b);if(!keys.length||without(a.text,keys)!==without(b.text,keys))throw Error('Unexpected native change: '+key);
 validated++;
}
const current=fs.readFileSync(target,'utf8'),currentGraph=graph(current);let patchedComponents=0;
const patched=current.split(/(?=^--- !u!)/m).map(block=>{
 const header=block.match(/^--- !u!(\d+) &(-?\d+)/m);if(!header)return block;
 const owner=header[1]==='1'?header[2]:block.match(/^  m_GameObject: \{fileID: (-?\d+)\}/m)?.[1];
 const script=block.match(/m_Script: .*guid: (\w+)/)?.[1];
 const entry=[...currentGraph.blocks.entries()].find(([key,b])=>b.go===owner&&b.kind===header[1]&&(!script||key.endsWith(':'+script)));
 if(!entry)return block;const [key,item]=entry,keys=fields(item);if(!keys.length)return block;
 const source=verified.blocks.get(key);if(!source)throw Error('Requested component missing: '+key);
 for(const field of keys){const re=pattern(field),values=source.text.match(re),existing=block.match(re);if(!values||values.length!==existing?.length)throw Error('Field count changed: '+key+'/'+field);let i=0;block=block.replace(re,()=>values[i++]);}
 patchedComponents++;return block;
}).join('');
const installed=graph(patched);
for(const [key,b]of currentGraph.blocks){const result=installed.blocks.get(key);if(!result||without(b.text,fields(b))!==without(result.text,fields(b)))throw Error('Unrelated authored change: '+key);}
if(fs.readFileSync(target,'utf8')!==current)throw Error('Concurrent editor save; rerun the selective installer');
fs.writeFileSync(path.join(stage,'before-install.prefab'),current);fs.writeFileSync(target,patched);
const review=path.join(root,'Tools/royal-postman/production/review/pets-polish-v10');fs.mkdirSync(review,{recursive:true});
for(const name of fs.readdirSync(output).filter(n=>/^(pets-(empty|active|hover)-|inventory-row-).+\.png$/.test(n)||n==='validation.txt'))fs.copyFileSync(path.join(output,name),path.join(review,name));
fs.appendFileSync(path.join(review,'validation.txt'),'Selective install: '+validated+' verified native changes; '+patchedComponents+' requested components patched onto the current authored prefab. Every unrelated field and existing object ID preserved. No PNG assets changed.\n');
console.log('Applied V10: '+validated+' verified changes; current screen preserved outside requested fields.');
