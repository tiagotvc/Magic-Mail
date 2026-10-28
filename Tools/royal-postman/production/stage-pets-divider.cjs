// Stage the CURRENT authored Pets prefab with native-preview GUIDs, preserving every layout edit.
const fs=require('fs'),path=require('path'),{execFileSync}=require('child_process');
const root=path.resolve(__dirname,'../../..'),preview=path.join(root,'Library/MapValidation/PreviewProject');
const prefab='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab';
const guid=t=>t.match(/^guid: (\w+)/m)?.[1];
let text=fs.readFileSync(path.join(root,prefab),'utf8');
const ids=[...new Set([...text.matchAll(/guid: ([a-f0-9]{32})/g)].map(m=>m[1]))];
const metas=execFileSync('rg',['-l','-g','*.meta','^guid: ('+ids.join('|')+')$','Assets'],{cwd:root,encoding:'utf8'}).trim().split(/\r?\n/);
const restore=[];
for(const meta of metas){
 const source=path.join(root,meta),dest=path.join(preview,meta);
 if(!fs.existsSync(dest))throw Error('Native dependency missing: '+meta);
 const main=guid(fs.readFileSync(source,'utf8')),native=guid(fs.readFileSync(dest,'utf8'));
 if(main&&native&&main!==native){text=text.replaceAll(main,native);restore.push([native,main]);}
}
const stage=path.join(root,'Library/MapValidation',process.argv[2]||'pets-divider-v7');
fs.mkdirSync(stage,{recursive:true});
fs.copyFileSync(path.join(root,prefab),path.join(stage,'before.prefab'));
fs.writeFileSync(path.join(stage,'guid-map.json'),JSON.stringify(restore));
fs.writeFileSync(path.join(preview,prefab),text);
console.log('Staged current Pets layout; '+restore.length+' GUIDs mapped for isolated preview.');
