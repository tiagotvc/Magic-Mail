const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=path.resolve(__dirname,'../../..'),iso=path.join(root,'Library/MapValidation/PreviewProject'),stage=path.join(root,'Library/MapValidation/pets-layered-v63');
const pairs=JSON.parse(fs.readFileSync(path.join(root,'Library/MapValidation/pets-cartoon-v24/guid-map.json'),'utf8'));
const translate=s=>pairs.reduce((s,[a,b])=>s.replaceAll(a,b),s);
const file='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab';
const before=fs.readFileSync(path.join(stage,'before.prefab'),'utf8');if(fs.readFileSync(path.join(root,file),'utf8')!==before)throw Error('Main prefab changed during production; preserve new edits before staging.');
const art='Assets/RoyalAves/Art/UI/Menu/RoyalPostman/PetsLayeredV63';
for(const n of fs.readdirSync(path.join(root,art)).filter(n=>n.endsWith('.png'))){const f=path.join(root,art,n);if(!fs.existsSync(f+'.meta'))fs.writeFileSync(f+'.meta','fileFormatVersion: 2\nguid: '+crypto.randomUUID().replaceAll('-','')+'\nTextureImporter:\n  externalObjects: {}\n  serializedVersion: 13\n  isReadable: 1\n  alphaIsTransparency: 1\n  textureType: 0\n  spriteMode: 0\n  maxTextureSize: 8192\n');}
const particle='Assets/RoyalAves/Scripts/UI/RoyalPostman2026/RoyalPostmanIncubatorParticles.cs';
if(!fs.existsSync(path.join(root,particle+'.meta')))fs.writeFileSync(path.join(root,particle+'.meta'),'fileFormatVersion: 2\nguid: '+crypto.randomUUID().replaceAll('-','')+'\n');
function cp(rel){const target=path.join(iso,rel);fs.mkdirSync(path.dirname(target),{recursive:true});fs.copyFileSync(path.join(root,rel),target);if(fs.existsSync(path.join(root,rel+'.meta')))fs.writeFileSync(target+'.meta',translate(fs.readFileSync(path.join(root,rel+'.meta'),'utf8')));}
const metaMap=new Map();
function scan(dir){for(const e of fs.readdirSync(dir,{withFileTypes:true})){const p=path.join(dir,e.name);if(e.isDirectory())scan(p);else if(e.name.endsWith('.meta')){const guid=fs.readFileSync(p,'utf8').match(/guid: (\w{32})/)?.[1];if(guid)metaMap.set(guid,path.relative(root,p.slice(0,-5)).replaceAll('\\','/'));}}}
scan(path.join(root,'Assets'));
const visited=new Set();
function deps(content){for(const match of content.matchAll(/guid: ([0-9a-f]{32})/g)){const guid=match[1];if(visited.has(guid))continue;visited.add(guid);const rel=metaMap.get(guid);if(!rel||!fs.statSync(path.join(root,rel)).isFile())continue;cp(rel);if(/\.(asset|prefab|mat|controller)$/.test(rel)){let s=fs.readFileSync(path.join(root,rel),'utf8');fs.writeFileSync(path.join(iso,rel),translate(s));deps(s);}}}
deps(before);
fs.writeFileSync(path.join(iso,file),translate(before));
for(const n of ['RoyalPostmanPetsScreen','RoyalPostmanViewport','RoyalPostmanIncubatorParticles'])cp('Assets/RoyalAves/Scripts/UI/RoyalPostman2026/'+n+'.cs');
for(const n of fs.readdirSync(path.join(root,art)).filter(n=>n.endsWith('.png')))cp(art+'/'+n);
cp('Assets/RoyalAves/Editor/RoyalPostmanScreenDocument.cs');
fs.copyFileSync(path.join(__dirname,'PetsLayeredV63Build.cs'),path.join(iso,'Assets/Editor/PetsLayeredV63Build.cs'));
console.log('Staged current prefab, '+visited.size+' dependency GUIDs, new code and nine original PNGs.');

