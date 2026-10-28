const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=path.resolve(__dirname,'../../..'),iso=path.join(root,'Library/MapValidation/PreviewProject'),stage=path.join(root,'Library/MapValidation/incubator-fit-v71');
fs.mkdirSync(stage,{recursive:true});
const pairs=JSON.parse(fs.readFileSync(path.join(root,'Library/MapValidation/pets-cartoon-v24/guid-map.json'),'utf8'));
const translate=s=>pairs.reduce((s,[a,b])=>s.replaceAll(a,b),s);
const prefab='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab';
const before=fs.readFileSync(path.join(root,prefab),'utf8');fs.writeFileSync(path.join(stage,'before.prefab'),before);
const art='Assets/RoyalAves/Art/UI/Menu/RoyalPostman/IncubatorControlsV70';
for(const name of fs.readdirSync(path.join(root,art)).filter(n=>n.endsWith('.png'))){
 const rel=path.join(art,name);if(!fs.existsSync(path.join(root,rel+'.meta')))fs.writeFileSync(path.join(root,rel+'.meta'),'fileFormatVersion: 2\nguid: '+crypto.randomUUID().replaceAll('-','')+'\nTextureImporter:\n  serializedVersion: 13\n  externalObjects: {}\n  textureType: 0\n  spriteMode: 0\n  isReadable: 1\n  alphaIsTransparency: 1\n  maxTextureSize: 8192\n');
}
function cp(rel){const target=path.join(iso,rel);fs.mkdirSync(path.dirname(target),{recursive:true});fs.copyFileSync(path.join(root,rel),target);if(fs.existsSync(path.join(root,rel+'.meta')))fs.writeFileSync(target+'.meta',translate(fs.readFileSync(path.join(root,rel+'.meta'),'utf8')));if(/\.(prefab|asset|mat)$/.test(rel))fs.writeFileSync(target,translate(fs.readFileSync(path.join(root,rel),'utf8')));}
const meta=new Map();
function scan(dir){for(const e of fs.readdirSync(dir,{withFileTypes:true})){const p=path.join(dir,e.name);if(e.isDirectory())scan(p);else if(e.name.endsWith('.meta')){const guid=fs.readFileSync(p,'utf8').match(/guid: (\w{32})/)?.[1];if(guid)meta.set(guid,path.relative(root,p.slice(0,-5)));}}}
scan(path.join(root,'Assets'));
const visited=new Set();
function deps(rel){if(visited.has(rel)||!fs.statSync(path.join(root,rel)).isFile())return;visited.add(rel);cp(rel);if(/\.(prefab|asset|mat)$/.test(rel))for(const m of fs.readFileSync(path.join(root,rel),'utf8').matchAll(/guid: ([a-f0-9]{32})/g)){const p=meta.get(m[1]);if(p)deps(p);}}
deps(prefab);
deps('Assets/RoyalAves/Art/UI/Icon/RoyalPostman/Sprites/paw-cream.asset');
deps('Assets/RoyalAves/Art/UI/Menu/RoyalPostman/Pets/Sprites/pets-progress-blue-v16.asset');
for(const name of fs.readdirSync(path.join(root,art)).filter(n=>n.endsWith('.png')))cp(art+'/'+name);
cp('Assets/RoyalAves/Scripts/UI/RoyalPostman2026/RoyalPostmanProgressClip.cs');
deps('Assets/RoyalAves/Art/UI/Button/RoyalPostman/Shaders/RoyalPostmanNeutralPalette.shader');
for(const name of ['IncubatorFitV71Build'])fs.copyFileSync(path.join(__dirname,name+'.cs'),path.join(iso,'Assets/Editor/'+name+'.cs'));
console.log('Staged latest prefab and '+visited.size+' dependency assets.');
