const fs=require('fs'),path=require('path'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),file=path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab');
const text=fs.readFileSync(file,'utf8'),g=graph(text),guid=name=>fs.readFileSync(path.join(root,'Library/MapValidation/user-button-v48/'+name+'-guid.txt'),'utf8').trim();
const border=guid('Border'),center=guid('Center');let count=0,patch='*** Begin Patch\n*** Update File: '+file.replaceAll('\\','/')+'\n';
for(const[k,b]of g.blocks){
 const match=b.path.match(/^Pets\/Body\/Workshop\/Incubator[0-3]\/Panel\/Available\/Accelerate(\/FixedGold|\/MovingCenter\/Face)?$/);if(!match)continue;
 const raw=text.split(/(?=^--- !u!)/m).find(x=>new RegExp('^--- !u!'+b.kind+' &'+g.ids.get(k)+'\\r?\\n').test(x));let next=raw;
 const rimPath=b.path.replace(/\/(FixedGold|MovingCenter\/Face)$/,'')+'/FixedGold';
 const rim=g.blocks.get(rimPath+'#224').text,dimensions=rim.match(/m_SizeDelta: \{x: ([\d.]+), y: ([\d.]+)\}/),width=Number(dimensions[1]),scale=width/1176;
 if(match[1]&&b.kind==='114'){
  next=next.replace(/m_Material: .+/,'m_Material: {fileID: 0}').replace(/m_Sprite: .+/,'m_Sprite: {fileID: 21300000, guid: '+(match[1]==='/FixedGold'?border:center)+', type: 2}').replace(/m_PixelsPerUnitMultiplier: .+/,'m_PixelsPerUnitMultiplier: '+(1/scale));
 }else if(match[1]&&b.kind==='224'){
  const size=match[1]==='/FixedGold'?{x:width,y:415*scale}:{x:1114*scale,y:340*scale};
  next=next.replace(/m_SizeDelta: .+/,'m_SizeDelta: {x: '+size.x+', y: '+size.y+'}');
  if(match[1]==='/MovingCenter/Face')next=next.replace(/m_AnchoredPosition: .+/,'m_AnchoredPosition: '+rim.match(/m_AnchoredPosition: (.+)/)[1]);
 }else if(!match[1]&&b.kind==='224'){
  const children=raw.match(/  m_Children:\r?\n((?:  - .*\r?\n)+)/),lines=children[1].trimEnd().split(/\r?\n/),id=g.ids.get(rimPath+'#224'),first=lines.find(x=>x.includes('fileID: '+id+'}'));
  if(!first)throw Error('Missing rim child');next=next.replace(children[0],'  m_Children:\n'+[first,...lines.filter(x=>x!==first)].join('\n')+'\n');
 }
 if(next===raw)continue;count++;const a=raw.trimEnd().replace(/\r/g,'').split('\n'),n=next.trimEnd().replace(/\r/g,'').split('\n');
 // Child reordering changes multiple adjacent lines; replace each targeted block explicitly.
 patch+='@@\n'+a.map(l=>'-'+l).join('\n')+'\n'+n.map(l=>'+'+l).join('\n')+'\n';
}
if(count!==20)throw Error('Expected20 Acelerar components, got '+count);
console.log(JSON.stringify({patch:patch+'*** End Patch',count}));
