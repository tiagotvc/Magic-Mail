const fs=require('fs'),path=require('path'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),file=path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab'),text=fs.readFileSync(file,'utf8'),g=graph(text),raw=new Map();
for(const b of text.split(/(?=^--- !u!)/m)){const h=b.match(/^--- !u!\d+ &(-?\d+)/m);if(h)raw.set(h[1],b.trimEnd().replace(/\r/g,''));}
const edits=[];
for(const [layer,guid]of [['FixedGold','15a74fa071482c240a6a0ba4f58c1f71'],['MovingCenter/Face','ac812d5054c7edd45b3bcf51543e3f9b']]){
 const base='Pets/Header/Back/'+layer;
 for(const[k,b]of g.blocks){if(b.path!==base||!['224','114'].includes(b.kind))continue;const id=g.ids.get(k),old=raw.get(id);let next=old;
  if(b.kind==='114'){
   if(!old.includes('RoyalPostmanSymmetricButtonFace'))throw Error('Unexpected existing graphic '+k);
   next=old.slice(0,old.indexOf('  paint:')).replace('guid: 2080b8a62ddad302f1a2587e3df387d2','guid: fe87c0e1cc204ed48ad3b37840f39efc').replace(/m_EditorClassIdentifier: .+/,'m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image').replace(/m_Material: .+/,'m_Material: {fileID: 0}');
   next+='  m_Sprite: {fileID: 21300000, guid: '+guid+', type: 3}\n  m_Type: 0\n  m_PreserveAspect: 1\n  m_FillCenter: 1\n  m_FillMethod: 4\n  m_FillAmount: 1\n  m_FillClockwise: 1\n  m_FillOrigin: 0\n  m_UseSpriteMesh: 0\n  m_PixelsPerUnitMultiplier: 1';
  }else{
   const scale=138/1350;
   next=next.replace(/m_AnchoredPosition: .+/,'m_AnchoredPosition: {x: 0, y: '+(layer==='FixedGold'?(-12*scale).toFixed(6):'0')+'}').replace(/m_SizeDelta: .+/,'m_SizeDelta: {x: '+(layer==='FixedGold'?'138':(1160*scale).toFixed(6))+', y: '+((layer==='FixedGold'?1374:1160)*scale).toFixed(6)+'}');
  }
  edits.push({id,old,next,order:text.indexOf(old.split('\n')[0])});
 }
}
if(edits.length!==4)throw Error('Expected4 layer edits');edits.sort((a,b)=>a.order-b.order);
let patch='*** Begin Patch\n*** Update File: '+file.replaceAll('\\','/')+'\n';for(const{old,next}of edits)patch+='@@\n'+old.split('\n').map(l=>'-'+l).join('\n')+'\n'+next.split('\n').map(l=>'+'+l).join('\n')+'\n';
console.log(JSON.stringify({patch:patch+'*** End Patch',ids:edits.map(e=>e.id)}));
