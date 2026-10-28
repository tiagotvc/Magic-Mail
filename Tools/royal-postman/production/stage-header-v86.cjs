const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=path.resolve(__dirname,'../../..'),iso=path.join(root,'Library/MapValidation/PreviewProject'),stage=path.join(root,'Library/MapValidation/header-v86');
const art='Assets/RoyalAves/Art/UI/Menu/RoyalPostman/HeaderV86';
fs.mkdirSync(stage,{recursive:true});fs.mkdirSync(path.join(root,art),{recursive:true});
const source=path.join(process.env.USERPROFILE,'.codex/generated_images/01a15af4-fedb-77d3-b750-1054318794bd');
for(const[name,file]of Object.entries({'background':'exec-3e30f2ec-4512-437f-a3e9-f15dd12468de.png','title-pt':'exec-2d670bae-8a6c-4c35-ba09-60f7ea5cf0ea.png','title-en':'exec-b937f5a8-0d9d-47c3-aacf-6a3b07c16e81.png'})){
 const target=path.join(root,art,name+'.png');if(!fs.existsSync(target))fs.copyFileSync(path.join(source,file),target);
 if(!fs.readFileSync(target).equals(fs.readFileSync(path.join(source,file))))throw Error('Original PNG differs: '+name);
}
const pairs=JSON.parse(fs.readFileSync(path.join(root,'Library/MapValidation/pets-cartoon-v24/guid-map.json'),'utf8'));
const translate=s=>pairs.reduce((s,[a,b])=>s.replaceAll(a,b),s);
function meta(rel){const file=path.join(root,rel+'.meta');if(!fs.existsSync(file))fs.writeFileSync(file,'fileFormatVersion: 2\nguid: '+crypto.randomUUID().replaceAll('-','')+'\n');}
for(const name of ['background','title-pt','title-en'])meta(art+'/'+name+'.png');
const runtime='Assets/RoyalAves/Scripts/UI/RoyalPostman2026/RoyalPostmanHeaderTitle.cs';meta(runtime);
function cp(rel){const target=path.join(iso,rel);fs.mkdirSync(path.dirname(target),{recursive:true});fs.copyFileSync(path.join(root,rel),target);if(fs.existsSync(path.join(root,rel+'.meta')))fs.writeFileSync(target+'.meta',translate(fs.readFileSync(path.join(root,rel+'.meta'),'utf8')));if(/\.(prefab|asset|mat)$/.test(rel))fs.writeFileSync(target,translate(fs.readFileSync(path.join(root,rel),'utf8')));}
const metas=new Map();function scan(dir){for(const e of fs.readdirSync(dir,{withFileTypes:true})){const p=path.join(dir,e.name);if(e.isDirectory())scan(p);else if(e.name.endsWith('.meta')){const guid=fs.readFileSync(p,'utf8').match(/guid: (\w{32})/)?.[1];if(guid)metas.set(guid,path.relative(root,p.slice(0,-5)));}}}scan(path.join(root,'Assets'));
const done=new Set();function deps(rel){if(done.has(rel)||!fs.existsSync(path.join(root,rel)))return;done.add(rel);cp(rel);if(/\.(prefab|asset|mat)$/.test(rel))for(const m of fs.readFileSync(path.join(root,rel),'utf8').matchAll(/guid: ([a-f0-9]{32})/g)){const p=metas.get(m[1]);if(p)deps(p);}}
const prefab='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab';
if(!process.argv.includes('--candidate')){fs.writeFileSync(path.join(stage,'before.prefab'),fs.readFileSync(path.join(root,prefab)));deps(prefab);}
else fs.writeFileSync(path.join(iso,prefab),translate(fs.readFileSync(path.join(stage,'candidate.prefab'),'utf8')));
for(const name of ['background','title-pt','title-en'])cp(art+'/'+name+'.png');
for(const rel of [runtime,'Assets/SweetSugar/Scripts/Level/CrosssceneData.cs','Assets/RoyalAves/Editor/RoyalPostmanTextureImportPolicy.cs','Assets/RoyalAves/Editor/RoyalPostmanUserSpriteImportPolicy.cs'])cp(rel);
deps('Assets/RoyalAves/Art/UI/UI assets/seta_voltar_creme_contorno.png');
fs.copyFileSync(path.join(__dirname,'HeaderV86Build.cs'),path.join(iso,'Assets/Editor/HeaderV86Build.cs'));
console.log('Staged header-only work and '+done.size+' dependencies; original PNG bytes retained.');
