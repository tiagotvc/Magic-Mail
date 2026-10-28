const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=path.resolve(__dirname,'../../..'),preview=path.join(root,'Library/MapValidation/PreviewProject'),stage=path.join(root,'Library/MapValidation/pets-machine-v40');fs.mkdirSync(stage,{recursive:true});
const prefab='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab',before=fs.readFileSync(path.join(root,prefab),'utf8');
if(!fs.existsSync(path.join(stage,'before.prefab')))fs.writeFileSync(path.join(stage,'before.prefab'),before);else if(fs.readFileSync(path.join(stage,'before.prefab'),'utf8')!==before)throw Error('Main prefab changed since staging');
const map=JSON.parse(fs.readFileSync(path.join(root,'Library/MapValidation/pets-cartoon-v24/guid-map.json'),'utf8'));
let staged=before;for(const[a,b]of map)staged=staged.replaceAll(a,b);fs.writeFileSync(path.join(preview,prefab),staged);
const files=['Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsMachineV40/pets-machine-housing-v40.png',...['rim','center'].map(n=>'Assets/RoyalAves/Art/UI/Button/RoyalPostman/NeutralV40/button-reference-'+n+'-v40.png'),...['RoyalPostmanPetsScreen','RoyalPostmanViewport','RoyalPostmanActionVisual'].map(n=>'Assets/RoyalAves/Scripts/UI/RoyalPostman2026/'+n+'.cs'),'Assets/RoyalAves/Editor/RoyalPostmanScreenDocument.cs'];
for(const rel of files){const dest=path.join(preview,rel);fs.mkdirSync(path.dirname(dest),{recursive:true});fs.copyFileSync(path.join(root,rel),dest);if(!fs.existsSync(dest+'.meta'))fs.copyFileSync(path.join(root,rel+'.meta'),dest+'.meta');}
fs.copyFileSync(path.join(__dirname,'review/pets-machine-v40/PetsMachineV40Validation.cs'),path.join(preview,'Assets/Editor/PetsMachineV40Validation.cs'));
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
fs.writeFileSync(path.join(stage,'preserved.json'),JSON.stringify(Object.fromEntries(['Home','Navigation'].map(n=>[n,hash(path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/'+n+'.prefab'))])),null,2));
console.log('Staged current authored Pets screen, three original paint PNGs and updated viewport/tints.');
