const fs=require('fs'),path=require('path'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),file=path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab');
const before=fs.readFileSync(file,'utf8'),g=graph(before),ids=new Map([...g.ids].map(([k,v])=>[v,k]));
const color='{r: 0.039215688, g: 0.09019608, b: 0.18431373, a: 1}';
const expected=new Set(['Pets/Inventory/EggsAndGems#224','Pets/Inventory/Acceleration#224','Pets/Inventory/Background#114:50edde7aa58390e45b414ec54a988a52']);
const after=before.split(/(?=^--- !u!)/m).map(block=>{
 const key=ids.get(block.match(/^--- !u!\d+ &(-?\d+)/m)?.[1]);if(!expected.has(key))return block;
 if(key.endsWith('#224')){
  const first=key.includes('EggsAndGems');
  return block.replace(/^  m_Anchor(Min|Max):[^\r\n]*/gm,(_,which)=>'  m_Anchor'+which+': {x: 0.5, y: '+(first?'0.5':'0')+'}')
   .replace(/^  m_AnchoredPosition:[^\r\n]*/m,'  m_AnchoredPosition: {x: 0, y: '+(first?'113':'196')+'}');
 }
 return block.replace(/^  (top|bottom):[^\r\n]*/gm,(_,field)=>'  '+field+': '+color);
}).join('');
const next=graph(after);let changed=0;for(const [key,b]of g.blocks)if(b.text!==next.blocks.get(key)?.text){if(!expected.has(key))throw Error('Unrelated change: '+key);changed++;}
if(changed){const backup=path.join(root,'Library/MapValidation/pets-device-fit-v16');fs.mkdirSync(backup,{recursive:true});if(!fs.existsSync(path.join(backup,'before.prefab')))fs.writeFileSync(path.join(backup,'before.prefab'),before);fs.writeFileSync(file,after);}
console.log('Applied responsive inventory anchors / solid navy. Changed components: '+changed);
