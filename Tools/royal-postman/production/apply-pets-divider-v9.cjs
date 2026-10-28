// Install a unified gold divider only after verifying every unrelated authored component.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=path.resolve(__dirname,'../../..'),stage=path.join(root,'Library/MapValidation/pets-divider-v9');
const prefab='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab';
const source=path.join(root,'Library/MapValidation/PreviewProject',prefab),target=path.join(root,prefab);
const before=fs.readFileSync(path.join(stage,'before.prefab'),'utf8');
const current=fs.readFileSync(target,'utf8'); // Preserve concurrent editor saves, including all existing file IDs.
const output=path.join(root,'Library/MapValidation/pets-mock-v2');
if(!fs.readFileSync(path.join(output,'validation.txt'),'utf8').startsWith('PASS: V9'))throw Error('Native V9 validation has not passed');
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
 return{blocks,within(item){return ['LeftTip','RightTip','LeftLine','RightLine'].some(n=>item?.path==='Pets/Divider/'+n);}};
};
const old=graph(before),next=graph(after);
let changed=0;
for(const id of new Set([...old.blocks.keys(),...next.blocks.keys()])){
 const a=old.blocks.get(id),b=next.blocks.get(id);if(a?.text===b?.text)continue;
 if(!old.within(a)&&!next.within(b))throw Error('Unrelated authored component changed: '+id);
 changed++;
}
if(changed!==8)throw Error('Expected exactly four divider transforms and four images, got '+changed);
const art='Assets/RoyalAves/Art/UI/Menu/RoyalPostman/Pets/';
for(const name of ['pets-divider-finial-v9','pets-divider-tip-v9','pets-divider-straight-v9']){
 const rel=art+'Sprites/'+name+'.asset';
 for(const suffix of ['', '.meta']){const src=path.join(root,'Library/MapValidation/PreviewProject',rel+suffix),dst=path.join(root,rel+suffix);if(fs.existsSync(dst)){const normalize=t=>t.replace(/m_RenderDataKey:\s*\n\s+[a-f0-9]{32}: \d+/,'m_RenderDataKey: native');if(normalize(fs.readFileSync(dst,'utf8'))!==normalize(fs.readFileSync(src,'utf8')))throw Error('Unexpected existing V9 asset: '+rel+suffix);}fs.copyFileSync(src,dst);}
}
const png=art+'pets-divider-finial-v9.png';
for(const suffix of ['', '.meta'])if(!fs.readFileSync(path.join(root,png+suffix)).equals(fs.readFileSync(path.join(root,'Library/MapValidation/PreviewProject',png+suffix))))throw Error('Source texture/GUID differ between projects');
const currentGraph=graph(current);let applied=0;
const patched=current.split(/(?=^--- !u!)/m).map(block=>{
 const id=block.match(/^--- !u!(\d+) &(-?\d+)/m);if(!id)return block;
 const item=[...currentGraph.blocks.values()].find(b=>b.text.startsWith('--- '+b.path+'#'+id[1])&&b.go===block.match(/^  m_GameObject: \{fileID: (-?\d+)\}/m)?.[1]&&b.kind===id[1]&&b.text.includes(id[1]==='114'?'m_PixelsPerUnitMultiplier:':'m_AnchoredPosition:'));
 if(!item||!currentGraph.within(item))return block;
 const source=[...next.blocks.values()].find(b=>b.path===item.path&&b.kind===item.kind&&b.text.includes(item.kind==='114'?'m_PixelsPerUnitMultiplier:':'m_AnchoredPosition:'));
 if(!source)return block;
 const fields=item.kind==='224'?['m_LocalPosition','m_LocalScale','m_AnchoredPosition','m_SizeDelta']:['m_Sprite','m_Type','m_PreserveAspect','m_PixelsPerUnitMultiplier'];
 for(const field of fields){const re=new RegExp('^  '+field+': [^\r\n]+','m'),value=source.text.match(re)?.[0];if(!value||!re.test(block))throw Error('Missing divider field '+field);block=block.replace(re,()=>value);}
 applied++;return block;
}).join('');
if(applied!==8)throw Error('Expected 8 current divider components, got '+applied);
const installed=graph(patched);
for(const [key,item] of currentGraph.blocks){if(!currentGraph.within(item)&&item.text!==installed.blocks.get(key)?.text)throw Error('Unrelated current component changed: '+key);}
if(fs.readFileSync(target,'utf8')!==current)throw Error('Concurrent save while applying divider; rerun installer.');
fs.writeFileSync(path.join(stage,'before-install.prefab'),current);fs.writeFileSync(target,patched);
const review=path.join(root,'Tools/royal-postman/production/review/pets-divider-v9');fs.mkdirSync(review,{recursive:true});
for(const name of fs.readdirSync(output).filter(n=>/^(pets-(empty|active|hover)-|inventory-row-).+\.png$/.test(n)||n==='validation.txt'))fs.copyFileSync(path.join(output,name),path.join(review,name));
fs.appendFileSync(path.join(review,'validation.txt'),'Scoped install: '+changed+' divider transform/image blocks changed; central ornament and every other authored component preserved.\n');
console.log('Applied unified gold divider. '+changed+' transform/image blocks updated; center and rest of current authored screen preserved.');
