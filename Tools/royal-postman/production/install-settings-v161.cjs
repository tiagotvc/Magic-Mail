// Install only the native-built settings subtree. Every unrelated YAML block is byte-preserved.
const fs=require('fs'),path=require('path'),crypto=require('crypto'),cp=require('child_process');
const root=path.resolve(__dirname,'../../..'),iso=path.join(root,'Library/MapValidation/SettingsV161Validation');
const out=path.join(root,'Library/MapValidation/settings-v161'),review=path.join(__dirname,'review/settings-v161');
const manifestPath=path.join(__dirname,'SETTINGS-V161.json'),manifest=JSON.parse(fs.readFileSync(manifestPath,'utf8'));
const checks=fs.readFileSync(path.join(out,'checks.txt'),'utf8');if(!checks.startsWith('PASS:'))throw Error('Native checks have not passed');
const mainPath=path.join(root,'Assets/RoyalAves/Prefabs/RoyalAvesLobby.prefab'),live=fs.readFileSync(mainPath,'utf8');
const alreadyInstalled=fs.existsSync(path.join(review,'candidate.prefab'))&&live===fs.readFileSync(path.join(review,'candidate.prefab'),'utf8');
const original=alreadyInstalled?fs.readFileSync(path.join(review,'install-before.prefab'),'utf8'):live;
function parse(text){const blocks=new Map();for(const b of text.split(/(?=^--- !u!)/m)){const h=b.match(/^--- !u!(\d+) &(-?\d+)/m);if(h)blocks.set(h[2],{kind:h[1],body:b,id:h[2],go:h[1]==='1'?h[2]:b.match(/^  m_GameObject: \{fileID: (-?\d+)\}/m)?.[1]});}return blocks;}
function subtree(blocks){const go=[...blocks.values()].find(b=>b.kind==='1'&&/^  m_Name: (TelaConfiguracoes|SettingsWindowV161)$/m.test(b.body));if(!go)throw Error('Settings root absent');const transform=[...blocks.values()].find(b=>b.go===go.id&&b.kind==='224');const transforms=new Set([transform.id]),gos=new Set([go.id]);let count;do{count=gos.size;for(const b of blocks.values())if(b.kind==='224'&&transforms.has(b.body.match(/^  m_Father: \{fileID: (-?\d+)\}/m)?.[1])){transforms.add(b.id);gos.add(b.go);}}while(gos.size!==count);return{go,transform,ids:new Set([...blocks.values()].filter(b=>gos.has(b.go)).map(b=>b.id))};}
const old=parse(original),oldTree=subtree(old),back=parse(fs.readFileSync(path.join(review,'before.prefab'),'utf8'));
for(const id of oldTree.ids)if(old.get(id).body!==back.get(id)?.body)throw Error('Concurrent authored settings changes: '+id);
for(const [id,b]of old)if(!oldTree.ids.has(id))for(const m of b.body.matchAll(/fileID: (-?\d+)/g))if(oldTree.ids.has(m[1])&&m[1]!==oldTree.transform.id)throw Error('External reference to old settings child: '+id+' -> '+m[1]);
const source=fs.readFileSync(path.join(iso,manifest.prefab),'utf8'),fresh=parse(source),newTree=subtree(fresh),mapping=new Map();
const fitGuid=fs.readFileSync(path.join(root,'Assets/RoyalAves/Scripts/UI/RoyalPostman2026/RoyalPostmanSettingsFit.cs.meta'),'utf8').match(/guid: (\w+)/)[1];
for(const b of fresh.values())if(b.go===newTree.go.id){const same=[...old.values()].find(o=>o.go===oldTree.go.id&&o.kind===b.kind&&(b.kind!=='114'||o.body.match(/m_Script:.*guid: (\w+)/)?.[1]===b.body.match(/m_Script:.*guid: (\w+)/)?.[1]));if(same)mapping.set(b.id,same.id);}
let next=901610000000000000n;const used=new Set(old.keys());for(const id of fresh.keys())if(!mapping.has(id)){while(used.has(String(next)))next++;mapping.set(id,String(next));used.add(String(next));next++;}
const oldParent=oldTree.transform.body.match(/^  m_Father: (.+)$/m)[1];
const additions=[];for(const b of fresh.values()){let text=b.body.replace(/^(--- !u!\d+ &)(-?\d+)/,'$1'+mapping.get(b.id)).replace(/fileID: (-?\d+)/g,(m,id)=>mapping.has(id)?'fileID: '+mapping.get(id):m);if(b.id===newTree.transform.id)text=text.replace(/^  m_Father: .+$/m,'  m_Father: '+oldParent);if(b.id===newTree.go.id)text=text.replace(/^  m_Name: .+$/m,'  m_Name: TelaConfiguracoes');additions.push(text.trimEnd());}
const unchanged=[...old].filter(([id])=>!oldTree.ids.has(id));
const result='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+unchanged.map(([,b])=>b.body.trimEnd()).concat(additions).join('\n')+'\n';
const after=parse(result);for(const [id,b] of unchanged)if(after.get(id)?.body.replace(/\r/g,'').trimEnd()!==b.body.replace(/\r/g,'').trimEnd())throw Error('Unrelated block changed: '+id);
const ids=new Set(after.keys());for(const b of after.values())for(const line of b.body.split('\n'))if(!line.includes('guid:'))for(const m of line.matchAll(/fileID: (-?\d+)/g))if(m[1]!=='0'&&!ids.has(m[1]))throw Error('Dangling local reference '+m[1]+' in '+b.id);
for(const a of manifest.assets){const from=path.join(iso,manifest.art,a.file+'.meta'),to=path.join(root,manifest.art,a.file+'.meta');fs.copyFileSync(from,to);if(crypto.createHash('sha256').update(fs.readFileSync(path.join(root,manifest.art,a.file))).digest('hex')!==a.sha256)throw Error('Image bytes changed');}
for(const rel of [manifest.art+'/Sprites',manifest.prefab]){const from=path.join(iso,rel),to=path.join(root,rel);if(fs.statSync(from).isDirectory()){fs.mkdirSync(to,{recursive:true});for(const f of fs.readdirSync(from))fs.copyFileSync(path.join(from,f),path.join(to,f));}else{fs.mkdirSync(path.dirname(to),{recursive:true});fs.copyFileSync(from,to);fs.copyFileSync(from+'.meta',to+'.meta');}}
if(fs.readFileSync(mainPath,'utf8')!==original&&fs.readFileSync(mainPath,'utf8')!==result)throw Error('Concurrent lobby save');
fs.writeFileSync(path.join(review,'install-before.prefab'),original);fs.writeFileSync(path.join(review,'candidate.prefab'),result);fs.writeFileSync(mainPath,result);
for(const file of ['build.txt','checks.txt','import.json','settings-360x640.png','settings-390x844.png','settings-720x1280.png','settings-on.png','settings-off.png'])fs.copyFileSync(path.join(out,file),path.join(review,file));
const controllerPath='Assets/RoyalAves/Scripts/Meta/SettingsScreen.cs';fs.writeFileSync(path.join(review,'SettingsScreen.cs.before'),cp.execFileSync('git',['show','HEAD:'+controllerPath],{cwd:root}));
const policyPath=path.join(root,'Assets/RoyalAves/Editor/RoyalPostmanTextureImportPolicy.cs');
const oldPolicy=fs.readFileSync(policyPath,'utf8').replace(/^            bool settingsArt=.*\r?\n/m,'').replaceAll('pets||settingsArt','pets');
fs.writeFileSync(path.join(review,'RoyalPostmanTextureImportPolicy.cs.before'),oldPolicy);
const audit={scope:'RoyalAvesLobby/Content/TelaConfiguracoes only',removedOldSettingsBlocks:oldTree.ids.size,installedSettingsBlocks:fresh.size,unrelatedBlocksPreserved:unchanged.length,controllerIdentityPreserved:true,fitGuid,sha256:crypto.createHash('sha256').update(result).digest('hex')};
fs.writeFileSync(path.join(review,'installation-audit.json'),JSON.stringify(audit,null,2)+'\n');manifest.status='Installed in real RoyalAvesLobby/Content/TelaConfiguracoes';manifest.nativeChecks=checks.trim();manifest.audit=audit;fs.writeFileSync(manifestPath,JSON.stringify(manifest,null,2)+'\n');
// Reopen a fresh extraction of the INSTALLED subtree in native Unity for final serialization/render validation.
const embedded=[...subtree(after).ids].map(id=>{let b=after.get(id).body;if(id===oldTree.transform.id)b=b.replace(/^  m_Father: .+$/m,'  m_Father: {fileID: 0}');return b.trimEnd();});
fs.writeFileSync(path.join(iso,manifest.prefab),'%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+embedded.join('\n')+'\n');
console.log(JSON.stringify(audit));
