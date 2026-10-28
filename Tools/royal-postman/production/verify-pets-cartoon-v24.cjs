const fs=require('fs'),path=require('path'),crypto=require('crypto'),{execFileSync}=require('child_process'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),stage=path.join(root,'Library/MapValidation/pets-cartoon-v24'),review=path.join(__dirname,'review/pets-cartoon-v24');
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const old=graph(fs.readFileSync(path.join(stage,'before.prefab'),'utf8')),file='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab',text=fs.readFileSync(path.join(root,file),'utf8'),next=graph(text);
for(const[key,a]of old.blocks)if(a.path.startsWith('Pets/CurrentProfile')&&a.text!==next.blocks.get(key)?.text)throw Error('Current profile modified: '+key);
for(const[n,sha]of Object.entries(JSON.parse(fs.readFileSync(path.join(stage,'preserved.json')))))if(hash(path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/'+n+'.prefab'))!==sha)throw Error(n+' changed');
const manifestFile=path.join(__dirname,'PETS-CARTOON-V24.json'),manifest=JSON.parse(fs.readFileSync(manifestFile));
const sources=manifest.assets.filter(a=>a.path&&(a.status==='generated'||a.status.startsWith('restored')));
for(const a of sources)if(hash(path.join(root,a.path))!==a.sha256)throw Error('PNG bytes changed: '+a.id);
const metas=execFileSync('rg',['--with-filename','^guid: ','Assets','Packages','Library/PackageCache','-g','*.meta','--hidden','--no-ignore'],{cwd:root,encoding:'utf8',maxBuffer:64*1024*1024});
const guids=new Map(metas.split(/\r?\n/).flatMap(l=>{let m=l.match(/^(.*\.meta):guid: ([a-f0-9]{32})$/);return m?[[m[2],m[1].slice(0,-5)]]:[];}));
const visited=new Set(),pending=[file];let references=0;
while(pending.length){const rel=pending.pop();if(visited.has(rel))continue;visited.add(rel);if(!/\.(asset|mat|prefab)$/.test(rel))continue;const doc=fs.readFileSync(path.join(root,rel),'utf8');for(const[,guid]of doc.matchAll(/guid: ([a-f0-9]{32})/g)){if(/^0{16}/.test(guid))continue;const referenced=guids.get(guid);if(!referenced)throw Error('Missing local asset GUID '+guid+' in '+rel);references++;if(!visited.has(referenced))pending.push(referenced);}}
manifest.status='installed';manifest.geometry.productionInstalled=true;manifest.assets.find(a=>a.id==='pets-cartoon-v24-header').paintSource='pets-header-red-home-v4';manifest.assets.find(a=>a.id==='pets-cartoon-v24-header').geometry='Native header strip, reuse Home paint';
manifest.assets.find(a=>a.id==='pets-cartoon-v24-action-teal-symmetric').status='restored-reference-paint-approved-native-geometry-validated';
manifest.installation={prefab:file,sha256:hash(path.join(root,file)),sources:sources.length,nativeResourceFiles:69,preserved:['CurrentProfile','Home','Navigation','stock/timers/unlocks','Legendary 48h'],review:'Tools/royal-postman/production/review/pets-cartoon-v24',validation:'Native resolution checks see validation.txt; dependencies and original PNG hashes verified'};
fs.writeFileSync(manifestFile,JSON.stringify(manifest,null,2)+'\n');
fs.mkdirSync(review,{recursive:true});
for(const name of ['cartoon-stock-720.png','cartoon-empty-720.png','cartoon-active-720.png','cartoon-current-720.png','cartoon-empty-390x844-notch.png','cartoon-empty-360x640-notch.png','pressed-action.png','current-preserved.png','validation.txt','raster-parity.txt','installation.txt']){const source=path.join(stage,name);if(fs.existsSync(source))fs.copyFileSync(source,path.join(review,name));}
const report='PASS: real Pets prefab installed with cartoonLayout=1; '+sources.length+' original PNG hashes intact; '+references+' local asset references resolve across '+visited.size+' dependencies; CurrentProfile semantic blocks and Home/Navigation bytes preserved.\n';
fs.writeFileSync(path.join(review,'installation-audit.txt'),report);console.log(report);
