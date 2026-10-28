const fs=require('fs'),path=require('path'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),file=path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab'),source=fs.readFileSync(file,'utf8'),g=graph(source);
const guid=fs.readFileSync(path.join(root,'Library/MapValidation/pets-counter-v64/sprite-guid.txt'),'utf8').trim(),y=Number(process.argv[2]||1045);
if(!/^[a-f0-9]{32}$/.test(guid))throw Error('Missing native sprite GUID');
const raw=new Map();for(const b of source.split(/(?=^--- !u!)/m)){const h=b.match(/^--- !u!\d+ &(-?\d+)/m);if(h)raw.set(h[1],b.replace(/\r/g,'').trimEnd());}
let edits=[];
for(const[k,b]of g.blocks){if(b.path!=='Pets/Body/Workshop/Counter'||!['224','114'].includes(b.kind))continue;let old=raw.get(g.ids.get(k)),next=old;
if(b.kind==='224')next=next.replace(/m_AnchoredPosition: .+/,'m_AnchoredPosition: {x: 0, y: -'+y+'}').replace(/m_SizeDelta: .+/,'m_SizeDelta: {x: 1650, y: 235}');
else{if(!old.includes('UnityEngine.UI.Image'))throw Error('Counter image changed type');next=next.replace(/m_Sprite: .+/,'m_Sprite: {fileID: 21300000, guid: '+guid+', type: 2}');}
edits.push({old,next,order:source.indexOf(old.split('\n')[0])});}
if(edits.length!==2)throw Error('Expected two counter blocks');edits.sort((a,b)=>a.order-b.order);
const patch='*** Begin Patch\n*** Update File: '+file.replaceAll('\\','/')+'\n'+edits.map(e=>'@@\n'+e.old.split('\n').map(l=>'-'+l).join('\n')+'\n'+e.next.split('\n').map(l=>'+'+l).join('\n')+'\n').join('')+'*** End Patch';
fs.writeFileSync(path.join(root,'Library/MapValidation/pets-counter-v64/install-before.prefab'),source);
console.log(JSON.stringify({patch,y}));

