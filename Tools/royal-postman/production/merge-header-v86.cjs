const fs=require('fs'),path=require('path'),crypto=require('crypto'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),stage=path.join(root,'Library/MapValidation/header-v86'),iso=path.join(root,'Library/MapValidation/PreviewProject');
const file=path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab');
const pairs=JSON.parse(fs.readFileSync(path.join(root,'Library/MapValidation/pets-cartoon-v24/guid-map.json'),'utf8'));
const unmap=s=>pairs.reduce((s,[a,b])=>s.replaceAll(b,a),s);
const before=fs.readFileSync(path.join(stage,'before.prefab'),'utf8'),built=unmap(fs.readFileSync(path.join(stage,'assembled.prefab'),'utf8')),current=fs.readFileSync(file,'utf8');
const old=graph(before),next=graph(built),live=graph(current),header=p=>p==='Pets/Header'||p.startsWith('Pets/Header/');
function raw(text){return new Map(text.split(/(?=^--- !u!)/m).flatMap(b=>{const h=b.match(/^--- !u!\d+ &(-?\d+)/m);return h?[[h[1],b.replace(/\r/g,'').trimEnd()]]:[]}));}
const liveRaw=raw(current),newRaw=raw(built),ids=new Map(),used=new Set(live.ids.values());
for(const[k,id]of next.ids){let target=live.ids.get(k);if(!target){target=id;while(used.has(target))target=(BigInt('0x'+crypto.randomBytes(8).toString('hex'))&((1n<<62n)-1n)).toString();used.add(target);}ids.set(id,target);}
const edits=new Map(),added=[],audit={changed:[],added:[],preservedOutsideHeader:0};
for(const[k,b]of next.blocks){
 if(!header(b.path))continue;const previous=old.blocks.get(k),authored=live.blocks.get(k);
 if(previous?.text===b.text)continue;
 if(previous&&authored?.text!==previous.text)throw Error('Concurrent header edit: '+k);
 if(!previous&&authored)throw Error('New header component already exists: '+k);
 let body=newRaw.get(next.ids.get(k));body=body.replace(/^--- !u!(\d+) &(-?\d+)/,(m,kind,id)=>'--- !u!'+kind+' &'+ids.get(id));
 body=body.replace(/\{fileID: (-?\d+)\}/g,(m,id)=>ids.has(id)?'{fileID: '+ids.get(id)+'}':m);
 if(authored){edits.set(live.ids.get(k),body);audit.changed.push(k);}else{added.push(body);audit.added.push(k);}
}
for(const[k,b]of old.blocks)if(header(b.path)&&!next.blocks.has(k))throw Error('Existing header component deleted: '+k);
const result='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+[...liveRaw].map(([id,b])=>edits.get(id)||b).concat(added).join('\n')+'\n';
const verified=graph(result);for(const[k,b]of live.blocks)if(!header(b.path)){if(verified.blocks.get(k)?.text!==b.text)throw Error('Unrelated change: '+k);audit.preservedOutsideHeader++;}
function copyAsset(rel){const source=path.join(iso,rel),target=path.join(root,rel);fs.mkdirSync(path.dirname(target),{recursive:true});fs.writeFileSync(target,unmap(fs.readFileSync(source,'utf8')));fs.writeFileSync(target+'.meta',unmap(fs.readFileSync(source+'.meta','utf8')));}
if(process.argv.includes('--install')){
 if(!fs.readFileSync(path.join(stage,'review.txt'),'utf8').startsWith('PASS:'))throw Error('Native review has not passed');
 if(fs.readFileSync(path.join(stage,'install-before.prefab'),'utf8')!==current)throw Error('Prefab edited after native review; prepare against latest version');
 if(fs.readFileSync(path.join(stage,'candidate.prefab'),'utf8')!==result)throw Error('Candidate differs from native reviewed version');
 for(const name of ['background','title-pt','title-en'])copyAsset('Assets/RoyalAves/Art/UI/Menu/RoyalPostman/HeaderV86/Sprites/'+name+'.asset');
 copyAsset('Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Components/HatcheryHeaderV86.prefab');
 fs.writeFileSync(file,result);fs.writeFileSync(path.join(stage,'installation-audit.json'),JSON.stringify(audit,null,2));
 console.log('Installed header only: '+audit.changed.length+' changes, '+audit.added.length+' additions, '+audit.preservedOutsideHeader+' components outside header preserved.');
}else{
 fs.writeFileSync(path.join(stage,'candidate.prefab'),result);fs.writeFileSync(path.join(stage,'install-before.prefab'),current);
 fs.writeFileSync(path.join(iso,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab'),pairs.reduce((s,[a,b])=>s.replaceAll(a,b),result));
 fs.writeFileSync(path.join(stage,'installation-audit.json'),JSON.stringify(audit,null,2));console.log('Prepared header-only candidate; '+audit.preservedOutsideHeader+' outside components preserved.');
}
