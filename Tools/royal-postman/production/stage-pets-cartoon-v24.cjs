const fs=require('fs'),path=require('path'),crypto=require('crypto'),{execFileSync}=require('child_process');
const root=path.resolve(__dirname,'../../..'),preview=path.join(root,'Library/MapValidation/PreviewProject'),stage=path.join(root,'Library/MapValidation/pets-cartoon-v24');fs.mkdirSync(stage,{recursive:true});
const prefab='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab';
const code=['RoyalPostmanSymmetricButtonFace','RoyalPostmanEggGlow','RoyalPostmanCartoonBackdrop','RoyalPostmanProgressClip','RoyalPostmanPaintedButton','RoyalPostmanActionVisual','RoyalPostmanPetsScreen','RoyalPostmanViewport','RoyalPostmanCurrentPet'].map(n=>'Assets/RoyalAves/Scripts/UI/RoyalPostman2026/'+n+'.cs');
code.push(...['RoyalPostmanPetsCartoonBuilder','RoyalPostmanPetsCartoonLayoutBuilder','RoyalPostmanPetsBuilder','RoyalPostmanArtPaths','RoyalPostmanScreenDocument'].map(n=>'Assets/RoyalAves/Editor/'+n+'.cs'),'Assets/RoyalAves/Art/UI/Button/RoyalPostman/Shaders/RoyalPostmanSymmetricPaint.shader');
const manifestFile=path.join(__dirname,'PETS-CARTOON-V24.json'),manifest=JSON.parse(fs.readFileSync(manifestFile,'utf8'));
const extra=manifest.assets.filter(a=>a.path&&(a.status==='generated'||a.status.startsWith('restored'))).map(a=>a.path);
const current=fs.readFileSync(path.join(root,prefab),'utf8');if(!fs.existsSync(path.join(stage,'before.prefab')))fs.writeFileSync(path.join(stage,'before.prefab'),current);else if(fs.readFileSync(path.join(stage,'before.prefab'),'utf8')!==current)throw Error('Main prefab changed since first staging; rebase explicitly');
const metadata=execFileSync('rg',['--with-filename','^guid: ','Assets','-g','*.meta'],{cwd:root,encoding:'utf8',maxBuffer:64*1024*1024});const byGuid=new Map(metadata.split(/\r?\n/).flatMap(l=>{let m=l.match(/^(.*\.meta):guid: ([a-f0-9]{32})$/);return m?[[m[2],m[1].slice(0,-5)]]:[]}));
const mappings=new Map(),docs=new Map(),seen=new Set(),pending=[prefab,...code,...extra];
const guid=t=>t.match(/^guid: ([a-f0-9]{32})/m)?.[1];
while(pending.length){const rel=pending.pop();if(seen.has(rel))continue;seen.add(rel);const src=path.join(root,rel),dst=path.join(preview,rel);fs.mkdirSync(path.dirname(dst),{recursive:true});if(!fs.existsSync(src+'.meta'))fs.writeFileSync(src+'.meta','fileFormatVersion: 2\nguid: '+crypto.createHash('sha256').update(rel).digest('hex').slice(0,32)+'\n');if(!fs.existsSync(dst+'.meta'))fs.copyFileSync(src+'.meta',dst+'.meta');const a=guid(fs.readFileSync(src+'.meta','utf8')),b=guid(fs.readFileSync(dst+'.meta','utf8'));if(a&&b&&a!==b)mappings.set(a,b);
 if(/\.(asset|mat|prefab)$/.test(rel)){const text=fs.readFileSync(src,'utf8');docs.set(rel,text);for(const[,id]of text.matchAll(/guid: ([a-f0-9]{32})/g)){const next=byGuid.get(id);if(next&&!seen.has(next))pending.push(next);}}
 else if(!/\.cs$/.test(rel)||code.includes(rel))fs.copyFileSync(src,dst);
}
for(const[rel,original]of docs){let text=original;for(const[a,b]of mappings)text=text.replaceAll(a,b);fs.writeFileSync(path.join(preview,rel),text);}
const targetManifest=path.join(preview,'Tools/royal-postman/production/PETS-CARTOON-V24.json');fs.mkdirSync(path.dirname(targetManifest),{recursive:true});fs.copyFileSync(manifestFile,targetManifest);
for(const name of ['PetsCartoonValidation','PetsCartoonValidationChecks'])fs.copyFileSync(path.join(__dirname,'review/pets-cartoon-v24',name+'.cs'),path.join(preview,'Assets/Editor',name+'.cs'));
fs.writeFileSync(path.join(stage,'guid-map.json'),JSON.stringify([...mappings],null,2));
fs.writeFileSync(path.join(stage,'preserved.json'),JSON.stringify(Object.fromEntries(['Home','Navigation'].map(n=>[n,crypto.createHash('sha256').update(fs.readFileSync(path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/'+n+'.prefab'))).digest('hex')])),null,2));
console.log('Staged '+extra.length+' painted sources and '+code.length+' scripts/shader; '+seen.size+' dependencies, '+mappings.size+' GUID mappings.');
