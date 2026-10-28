const fs=require('fs'),path=require('path'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),file=path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab');
const text=fs.readFileSync(file,'utf8'),g=graph(text),sprite=fs.readFileSync(path.join(root,'Library/MapValidation/pets-machine-v46/sprite-guid.txt'),'utf8').trim();
const rimSprite=fs.readFileSync(path.join(root,'Library/MapValidation/pets-machine-v46/rim-guid.txt'),'utf8').trim();
let patch='*** Begin Patch\n*** Update File: '+file.replaceAll('\\','/')+'\n',count=0;
for(const[key,b]of g.blocks){
 const m=b.path.match(/^Pets\/Body\/Workshop\/Incubator[0-3]\/Panel\/Available\/(UseItem|Accelerate)(\/MovingCenter\/Face|\/FixedGold)?$/);if(!m)continue;
 const raw=text.split(/(?=^--- !u!)/m).find(x=>new RegExp('^--- !u!'+b.kind+' &'+g.ids.get(key)+'\\r?\\n').test(x));let next=raw;
 if(m[2]==='/FixedGold'&&b.kind==='114'){
  next=next.replace(/m_Sprite: .+/,'m_Sprite: {fileID: 21300000, guid: '+rimSprite+', type: 2}');
 }else if(m[2]==='/MovingCenter/Face'&&b.kind==='224'){
  const rim=g.blocks.get(b.path.replace('/MovingCenter/Face','/FixedGold')+'#224').text;
  const position=rim.match(/m_AnchoredPosition: (.+)/)[1],size=rim.match(/m_SizeDelta: \{x: ([\d.]+), y: ([\d.]+)\}/);
  next=next.replace(/m_AnchoredPosition: .+/, 'm_AnchoredPosition: '+position).replace(/m_SizeDelta: .+/,'m_SizeDelta: {x: '+(Number(size[1])-20)+', y: '+(Number(size[2])-20)+'}');
 }else if(m[2]==='/MovingCenter/Face'&&b.kind==='114'){
  const material=m[1]==='UseItem'?'b9bfd91fc97e438c8b45ad6a2bed06ee':'531809f3528646b5be952b19d8e02c93';
  next=next.replace(/m_Material: .+/,'m_Material: {fileID: 2100000, guid: '+material+', type: 2}').replace(/m_Color: .+/,'m_Color: {r: 0.64, g: 0.64, b: 0.64, a: 1}').replace(/m_Sprite: .+/,'m_Sprite: {fileID: 21300000, guid: '+sprite+', type: 2}');
 }else if(!m[2]&&b.kind==='114'&&raw.includes('RoyalPostmanActionVisual')){
  next=next.replace(/enabledTint: .+/,'enabledTint: {r: 1, g: 1, b: 1, a: 1}').replace(/disabledTint: .+/,'disabledTint: {r: 0.64, g: 0.64, b: 0.64, a: 1}');
 }
 if(next===raw)continue;count++;
 const a=raw.trimEnd().replace(/\r/g,'').split('\n'),n=next.trimEnd().replace(/\r/g,'').split('\n');
 patch+='@@\n'+a.map((l,i)=>l===n[i]?' '+l:'-'+l+'\n+'+n[i]).join('\n')+'\n';
}
if(count!==32)throw Error('Expected 32 face/rim components, got '+count);
console.log(JSON.stringify({patch:patch+'*** End Patch',count,sprite}));
