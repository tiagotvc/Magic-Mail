const fs=require('fs'),path=require('path'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),file=path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab'),text=fs.readFileSync(file,'utf8'),g=graph(text);
const from='Pets/Body/Workshop/Incubator0/Panel/Available/Accelerate',to=from.replace('/Accelerate','/UseItem');
const raw=new Map();for(const b of text.split(/(?=^--- !u!)/m)){const h=b.match(/^--- !u!\d+ &(-?\d+)/m);if(h)raw.set(h[1],b.trimEnd().replace(/\r/g,''));}
const selected=[...g.blocks].filter(([k,b])=>b.path.startsWith(from+'/')||b.path===from&&(b.kind==='224'||b.text.includes('RoyalPostmanActionVisual')||b.text.includes('RoyalPostmanPaintedButton')));
const order=new Map([...raw.keys()].map((id,i)=>[id,i]));selected.sort(([a],[b])=>(order.get(g.ids.get(a.replace(from,to)))??Infinity)-(order.get(g.ids.get(b.replace(from,to)))??Infinity));
const mapping=new Map();let next=8800520000000000000n;
for(const[k,id]of g.ids)if(k.startsWith(from)){const destId=g.ids.get(k.replace(from,to));if(destId)mapping.set(id,destId);}
for(const[k]of selected){const dest=k.replace(from,to),sourceId=g.ids.get(k);let destId=g.ids.get(dest);if(!destId){while(raw.has(String(next)))next++;destId=String(next++);}mapping.set(sourceId,destId);}
const remap=block=>block.replace(/^--- !u!(\d+) &(-?\d+)/m,(m,kind,id)=>'--- !u!'+kind+' &'+(mapping.get(id)||id)).replace(/fileID: (-?\d+)/g,(m,id)=>'fileID: '+(mapping.get(id)||id));
let patch='*** Begin Patch\n*** Update File: '+file.replaceAll('\\','/')+'\n',additions=[],changed=[];
for(const[k,b]of selected){const dest=k.replace(from,to),destId=g.ids.get(dest),old=destId?raw.get(destId):null;let block=remap(raw.get(g.ids.get(k)));
 if(b.path===from&&b.kind==='224')block=block.replace(/m_AnchoredPosition: .+/,old.match(/m_AnchoredPosition: .+/)[0]);
 if(b.path.endsWith('/Label')||b.path.endsWith('/Quantity')){const label=old?.match(/^  m_Text: .+$/m);if(label)block=block.replace(/^  m_Text: .+$/m,label[0]);}
 if(b.path.endsWith('/MovingCenter/Icon')&&b.kind==='114'){const sprite=old?.match(/^  m_Sprite: .+$/m);if(sprite)block=block.replace(/^  m_Sprite: .+$/m,sprite[0]);}
 if(!old){additions.push(block);changed.push(dest);continue;}if(old===block)continue;
 patch+='@@\n'+old.split('\n').map(l=>'-'+l).join('\n')+'\n'+block.split('\n').map(l=>'+'+l).join('\n')+'\n';changed.push(dest);
}
if(additions.length){const last=[...raw.values()].at(-1);patch+='@@\n'+last.split('\n').map(l=>' '+l).join('\n')+'\n'+additions.join('\n').split('\n').map(l=>'+'+l).join('\n')+'\n';}
if(!changed.length)throw Error('No changes');
console.log(JSON.stringify({patch:patch+'*** End Patch',changed,added:additions.length,from,to}));
