// Apply feedback only after verifying the rest of the current authored screen is unchanged.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=path.resolve(__dirname,'../../..'),stage=path.join(root,'Library/MapValidation/pets-feedback-v8');
const prefab='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab';
const source=path.join(root,'Library/MapValidation/PreviewProject',prefab),target=path.join(root,prefab);
const before=fs.readFileSync(path.join(stage,'before.prefab'),'utf8');
if(fs.readFileSync(target,'utf8')!==before)throw Error('Current authored screen changed during preview; restage it before applying.');
const output=path.join(root,'Library/MapValidation/pets-mock-v2');
if(!fs.readFileSync(path.join(output,'validation.txt'),'utf8').startsWith('PASS: V8'))throw Error('Native V8 validation has not passed');
let after=fs.readFileSync(source,'utf8');
for(const [native,main]of JSON.parse(fs.readFileSync(path.join(stage,'guid-map.json'),'utf8')))after=after.replaceAll(native,main);
const graph=text=>{
 const raw=new Map(),names=new Map(),transforms=new Map(),owners=new Map();
 for(const b of text.split(/(?=^--- !u!)/m)){
  const h=b.match(/^--- !u!(\d+) &(-?\d+)/m);if(!h)continue;
  const go=h[1]==='1'?h[2]:b.match(/^  m_GameObject: \{fileID: (-?\d+)\}/m)?.[1];
  const item={text:b.replace(/\r/g,'').trim(),go,kind:h[1]};raw.set(h[2],item);
  if(h[1]==='1')names.set(go,b.match(/^  m_Name: (.+)$/m)?.[1]?.trim());
  if(h[1]==='224'){transforms.set(go,b.match(/^  m_Father: \{fileID: (-?\d+)\}/m)?.[1]);owners.set(h[2],go);}
 }
 const objectPath=go=>{const parts=[];for(let n=0;go&&n<32;n++){parts.unshift(names.get(go)||go);go=owners.get(transforms.get(go));}return parts.join('/');};
 // Unity can reassign IDs to identically named labels on save. Compare semantic paths and references.
 const aliases=new Map();
 for(const[id,item]of raw){const script=item.text.match(/m_Script: .*guid: (\w+)/)?.[1];aliases.set(id,objectPath(item.go)+'#'+item.kind+(script?':'+script:''));}
 const blocks=new Map();
 for(const[id,item]of raw){const key=aliases.get(id);if(blocks.has(key))throw Error('Ambiguous authored component path: '+key);
  const normalized=item.text.replace(/^--- !u!\d+ &-?\d+/,'--- '+key).replace(/fileID: (-?\d+)/g,(match,ref)=>aliases.has(ref)?'fileID: '+aliases.get(ref):match);
  blocks.set(key,{...item,text:normalized,path:objectPath(item.go)});
 }
 return{blocks,within(item){return item?.path==='Pets/Inventory'||item?.path?.startsWith('Pets/Inventory/');}};
};
const old=graph(before),next=graph(after),script=fs.readFileSync(path.join(root,'Assets/RoyalAves/Scripts/UI/RoyalPostman2026/RoyalPostmanPetsScreen.cs.meta'),'utf8').match(/^guid: (\w+)/m)[1];
let changed=0;
for(const id of new Set([...old.blocks.keys(),...next.blocks.keys()])){
 const a=old.blocks.get(id),b=next.blocks.get(id);if(a?.text===b?.text)continue;
 const controller=(b||a).kind==='114'&&(b||a).text.includes('guid: '+script);
 if(!controller&&!old.within(a)&&!next.within(b))throw Error('Unrelated authored component changed: '+id);
 changed++;
}
if(!after.includes('RoyalPostmanInventoryHover')&&changed===0)throw Error('No feedback refinements found');
fs.writeFileSync(target,after);
const review=path.join(root,'Tools/royal-postman/production/review/pets-feedback-v8');fs.mkdirSync(review,{recursive:true});
for(const name of fs.readdirSync(output).filter(n=>/^(pets-(empty|active|hover)-|inventory-row-).+\.png$/.test(n)||n==='validation.txt'))fs.copyFileSync(path.join(output,name),path.join(review,name));
fs.appendFileSync(path.join(review,'validation.txt'),'Scoped install: '+changed+' inventory/controller blocks changed; every other authored screen component preserved.\n');
console.log('Applied inventory feedback. '+changed+' inventory/controller blocks updated; rest of current authored layout preserved.');
