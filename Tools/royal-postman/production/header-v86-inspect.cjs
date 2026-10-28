const fs=require('fs'),path=require('path'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..');const text=fs.readFileSync(path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab'),'utf8'),g=graph(text);
for(const[k,b]of g.blocks)if(b.path.startsWith('Pets/Header')||/Close|Fechar|rounded_total_close/.test(b.path)){
 const lines=b.text.split('\n').filter(l=>/m_Name:|m_IsActive:|m_Father:|m_Anchor|m_SizeDelta:|m_Pivot:|m_LocalScale:|m_Sprite:|m_Color:|m_Text:|m_Enabled:|m_Interactable:|m_TargetGraphic:|m_Material:|movingCenter:|pressTravel:|pressScale:/.test(l));
 console.log(k+'\n'+lines.join('\n'));
}
console.log('ROOT BINDINGS\n'+[...g.blocks.values()].find(b=>b.path==='Pets'&&b.text.includes('backButton:'))?.text);
console.log('ROOT TRANSFORM\n'+g.blocks.get('Pets#224').text);
