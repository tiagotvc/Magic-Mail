const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'../../..'),iso=path.join(root,'Library/MapValidation/PreviewProject'),stage=path.join(root,'Library/MapValidation/hatchery-v88');
fs.mkdirSync(stage,{recursive:true});
const pairs=JSON.parse(fs.readFileSync(path.join(root,'Library/MapValidation/pets-cartoon-v24/guid-map.json'),'utf8'));
const map=s=>pairs.reduce((s,[a,b])=>s.replaceAll(a,b),s);
const prefab='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab';
if(fs.existsSync(path.join(stage,'before.prefab')))throw Error('Snapshot exists; do not overwrite');
fs.copyFileSync(path.join(root,prefab),path.join(stage,'before.prefab'));
for(const rel of [prefab,'Assets/RoyalAves/Scripts/UI/RoyalPostman2026/RoyalPostmanHatchery.cs','Assets/RoyalAves/Scripts/Meta/PetsIncubationState.cs'])
{
 fs.writeFileSync(path.join(iso,rel),map(fs.readFileSync(path.join(root,rel),'utf8')));
 if(fs.existsSync(path.join(root,rel+'.meta')))fs.writeFileSync(path.join(iso,rel+'.meta'),map(fs.readFileSync(path.join(root,rel+'.meta'),'utf8')));
}
for(const name of ['HatcheryV88Build','HatcheryV88Review'])fs.copyFileSync(path.join(__dirname,name+'.cs'),path.join(iso,'Assets/Editor/'+name+'.cs'));
console.log('Staged current authored prefab and two runtime files; no image changes.');
