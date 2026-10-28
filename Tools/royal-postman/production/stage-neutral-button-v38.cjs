const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=path.resolve(__dirname,'../../..'),preview=path.join(root,'Library/MapValidation/PreviewProject'),stage=path.join(root,'Library/MapValidation/neutral-button-v38');
fs.mkdirSync(stage,{recursive:true});
const prefab='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab';
const before=fs.readFileSync(path.join(root,prefab),'utf8');
if(!fs.existsSync(path.join(stage,'before.prefab')))fs.writeFileSync(path.join(stage,'before.prefab'),before);
else if(fs.readFileSync(path.join(stage,'before.prefab'),'utf8')!==before)throw Error('Main prefab changed since staging');
const mappings=JSON.parse(fs.readFileSync(path.join(root,'Library/MapValidation/pets-cartoon-v24/guid-map.json'),'utf8'));
let staged=before;for(const[a,b]of mappings)staged=staged.replaceAll(a,b);fs.writeFileSync(path.join(preview,prefab),staged);
const base='Assets/RoyalAves/Art/UI/Button/RoyalPostman/NeutralV37/';
for(const rel of [base+'button-neutral-rim-v38.png',base+'button-neutral-center-v37.png','Assets/RoyalAves/Scripts/UI/RoyalPostman2026/RoyalPostmanActionVisual.cs']){
 const dest=path.join(preview,rel);fs.mkdirSync(path.dirname(dest),{recursive:true});fs.copyFileSync(path.join(root,rel),dest);
 if(!fs.existsSync(dest+'.meta'))fs.copyFileSync(path.join(root,rel+'.meta'),dest+'.meta');
}
fs.copyFileSync(path.join(__dirname,'review/neutral-button-v38/NeutralButtonV38Validation.cs'),path.join(preview,'Assets/Editor/NeutralButtonV38Validation.cs'));
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
fs.writeFileSync(path.join(stage,'preserved.json'),JSON.stringify(Object.fromEntries(['Home','Navigation'].map(n=>[n,sha(path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/'+n+'.prefab'))])),null,2));
console.log('Staged current authored Pets prefab and two neutral PNGs; no main prefab mutation.');
