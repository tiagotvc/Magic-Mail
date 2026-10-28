const fs=require('fs'),path=require('path'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),rel='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab';
const text=fs.readFileSync(path.join(root,rel),'utf8'),g=graph(text);
const selected=[...g.blocks].filter(([k,b])=>b.kind==='114'&&/^Pets\/Body\/Workshop\/Incubator[0-3]\/Panel\/Available\/(UseItem|Accelerate)\/FixedGold$/.test(b.path));
if(selected.length!==8)throw Error('Expected eight fixed rims');
let patch='*** Begin Patch\n*** Update File: '+path.join(root,rel).replaceAll('\\','/')+'\n';
for(const[key]of selected){
 const id=g.ids.get(key),block=text.split(/(?=^--- !u!)/m).find(b=>new RegExp('^--- !u!114 &'+id+'\\r?\\n').test(b));
 const oldMat='guid: cf73f7898dec7cd468b65a11ae03f1bc',oldColor='m_Color: {r: 1, g: 0.83, b: 0.25, a: 1}';
 if(!block.includes(oldMat)||!block.includes(oldColor))throw Error('Unexpected rim '+key);
 patch+='@@\n'+block.trimEnd().replace(/\r/g,'').split('\n').map(l=>l.includes(oldMat)?'-'+l+'\n+'+l.replace(oldMat,'guid: 05c858dd81054865a3c89d3ea74890bd'):l.includes(oldColor)?'-'+l+'\n+'+l.replace(oldColor,'m_Color: {r: 1, g: 1, b: 1, a: 1}'):' '+l).join('\n')+'\n';
}
console.log(JSON.stringify({patch:patch+'*** End Patch',count:selected.length}));
