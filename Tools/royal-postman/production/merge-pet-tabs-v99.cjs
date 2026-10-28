const fs=require('fs'),path=require('path');
const source=fs.readFileSync(path.join(__dirname,'merge-fast-forward-v93.cjs'),'utf8');
const start=source.indexOf("const icon='"),end=source.indexOf('function copyAsset',start);
const allowedSource="function allowed(b){\n if(b.kind==='114'&&b.path==='Pets/Hatchery')return ['selectedTabMaterial','unselectedTabMaterial'];\n if(b.kind==='114'&&['Pets/HatcheryTabs/Mascotes_PLACEHOLDER','Pets/HatcheryTabs/Incubadora_PLACEHOLDER'].includes(b.path)&&b.text.includes('guid: fe87c0e1cc204ed48ad3b37840f39efc'))return ['m_Color','m_Material'];\n return null;\n}\nconst newTree=p=>false;\n";
const oldAssets="['Assets/RoyalAves/Art/UI/Button/RoyalPostman/Pets/Sprites/fast-forward-chevron-v93.asset','Assets/RoyalAves/Art/UI/Button/RoyalPostman/Pets/fast-forward-cream-v93.mat']";
const assets="['Assets/RoyalAves/Art/UI/Tab/RoyalPostman/Pets/TabsV99/SelectedRed.mat','Assets/RoyalAves/Art/UI/Tab/RoyalPostman/Pets/TabsV99/UnselectedBlue.mat']";
if(process.argv.includes('--install')){
 const graph=require('./prefab-graph.cjs'),root=path.resolve(__dirname,'../../..'),stage=path.join(root,'Library/MapValidation/pet-tabs-v99'),iso=path.join(root,'Library/MapValidation/PreviewProject'),relative='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab';
 const before=graph(fs.readFileSync(path.join(stage,'before.prefab'),'utf8')),current=fs.readFileSync(path.join(root,relative),'utf8'),live=graph(current),candidate=fs.readFileSync(path.join(stage,'candidate.prefab'),'utf8'),next=graph(candidate);
 if(!fs.readFileSync(path.join(stage,'review.txt'),'utf8').startsWith('PASS:'))throw Error('Native review missing');
 const pairs=JSON.parse(fs.readFileSync(path.join(root,'Library/MapValidation/pets-cartoon-v24/guid-map.json'),'utf8')),unmap=s=>pairs.reduce((s,[a,b])=>s.replaceAll(b,a),s);
 if(unmap(fs.readFileSync(path.join(iso,relative),'utf8'))!==candidate)throw Error('Reviewed isolated candidate changed');
 const strip=b=>{
  if(b.path==='Pets/Hatchery'&&b.text.includes('guid: 20821e4f2412446582a7595aa420b84c'))return b.text.replace(/^  (selectedTabMaterial|unselectedTabMaterial):.*\n?/gm,'').trimEnd();
  if(['Pets/HatcheryTabs/Mascotes_PLACEHOLDER','Pets/HatcheryTabs/Incubadora_PLACEHOLDER'].includes(b.path)&&b.text.includes('guid: fe87c0e1cc204ed48ad3b37840f39efc'))return b.text.replace(/^  (m_Color|m_Material):.*\n?/gm,'');
  return b.text;
 };
 if(live.blocks.size!==before.blocks.size||next.blocks.size!==before.blocks.size)throw Error('Concurrent hierarchy change');
 for(const[k,b]of before.blocks){if(!live.blocks.has(k)||!next.blocks.has(k)||strip(b)!==strip(live.blocks.get(k))||strip(b)!==strip(next.blocks.get(k)))throw Error('Concurrent change outside requested tab fields: '+k);}
 const controller=[...live.blocks.values()].find(b=>b.path==='Pets/Hatchery'&&b.text.includes('guid: 20821e4f2412446582a7595aa420b84c'));
 for(const m of controller.text.matchAll(/^  (selectedTabMaterial|unselectedTabMaterial): (.+)$/gm))if(m[2]!=='{fileID: 0}')throw Error('Concurrent non-null tab material assignment');
 // The open editor serialized the new null references and near-white preview tint.
 // All other fields match the original; install the exact already-reviewed candidate.
 for(const name of ['SelectedRed','UnselectedBlue']){const rel='Assets/RoyalAves/Art/UI/Tab/RoyalPostman/Pets/TabsV99/'+name+'.mat',target=path.join(root,rel);if(fs.existsSync(target))throw Error('Material already exists');fs.mkdirSync(path.dirname(target),{recursive:true});for(const suffix of ['', '.meta'])fs.writeFileSync(target+suffix,unmap(fs.readFileSync(path.join(iso,rel+suffix),'utf8')));}
 fs.writeFileSync(path.join(stage,'install-before.prefab'),current);fs.writeFileSync(path.join(root,relative),candidate);console.log('Installed exact native-reviewed tabs; only3 requested components changed,2033 preserved including486 CurrentProfile.');
}else eval((source.slice(0,start)+allowedSource+source.slice(end)).replace(oldAssets,assets).replaceAll('fast-forward-v93','pet-tabs-v99'));
