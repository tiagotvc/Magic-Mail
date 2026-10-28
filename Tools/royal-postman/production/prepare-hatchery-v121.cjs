const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'../../..'),review=path.join(__dirname,'review/hatchery-v121');
fs.mkdirSync(review,{recursive:true});
const prefab='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab';
if(!fs.existsSync(path.join(review,'before.prefab')))fs.copyFileSync(path.join(root,prefab),path.join(review,'before.prefab'));
for(const name of ['RoyalPostmanHatchery','RoyalPostmanPetsScreen','RoyalPostmanHatchReveal','RoyalPostmanIncubatorParticles','RoyalPostmanRadialSparkles']){
 const source=path.join(root,'Assets/RoyalAves/Scripts/UI/RoyalPostman2026',name+'.cs'),target=path.join(review,name+'.before.cs');if(!fs.existsSync(target))fs.copyFileSync(source,target);
}
const graph=require('./prefab-graph.cjs')(fs.readFileSync(path.join(root,prefab),'utf8'));
const rects=[];for(const item of graph.blocks.values())if(item.kind==='224'&&((item.path.startsWith('Pets/Hatchery')&&item.path.split('/').length<=4)||item.path.includes('HatcheryTabs'))){
 rects.push({path:item.path,...Object.fromEntries([...item.text.matchAll(/^  (m_AnchorMin|m_AnchorMax|m_Pivot|m_AnchoredPosition|m_SizeDelta|m_LocalScale|m_Father): (.+)$/gm)].map(m=>[m[1],m[2]]))});
}
fs.writeFileSync(path.join(review,'before-rects.json'),JSON.stringify(rects,null,2));
console.log(JSON.stringify(rects));
for(const item of graph.blocks.values())if(item.kind==='114'&&['Pets/Hatchery','Pets/Hatchery/Incubate','Pets/Hatchery/DurationDisplay','Pets/Hatchery/Chamber'].includes(item.path))console.log(item.text);
