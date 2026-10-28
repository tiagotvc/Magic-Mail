const fs=require('fs'),path=require('path'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),rel='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab',text=fs.readFileSync(path.join(root,rel),'utf8'),g=graph(text);
const selected=[...g.blocks].filter(([k,b])=>b.kind==='224'&&/^Pets\/Body\/Workshop\/Incubator[0-3]\/Panel\/Available\/(UseItem|Accelerate)$/.test(b.path));
if(selected.length!==8)throw Error('Expected eight action rectangles');
let patch='*** Begin Patch\n*** Update File: '+path.join(root,rel).replaceAll('\\','/')+'\n';
for(const[key]of selected){const id=g.ids.get(key),block=text.split(/(?=^--- !u!)/m).find(b=>b.startsWith('--- !u!224 &'+id+'\n')||b.startsWith('--- !u!224 &'+id+'\r\n'));if(!block)throw Error('Missing rectangle '+key);if(!block.includes('m_LocalScale: {x: 1, y: 1, z: 1}'))throw Error('Action already resized or authored scale changed: '+key);patch+='@@\n'+block.trimEnd().replace(/\r/g,'').split('\n').map(l=>l.includes('m_LocalScale: {x: 1, y: 1, z: 1}')?'-'+l+'\n+'+l.replace('{x: 1, y: 1, z: 1}','{x: 0.85, y: 0.85, z: 1}'):' '+l).join('\n')+'\n';}
patch+='*** End Patch';console.log(JSON.stringify({patch,count:selected.length,paths:selected.map(([k,b])=>b.path)}));
