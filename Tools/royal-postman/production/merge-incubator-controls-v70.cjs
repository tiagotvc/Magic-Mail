const fs=require('fs'),path=require('path'),crypto=require('crypto'),assert=require('assert'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),stage=path.join(root,'Library/MapValidation/incubator-controls-v70');
const file=path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab');
const before=fs.readFileSync(path.join(stage,'before.prefab'),'utf8');
const pairs=JSON.parse(fs.readFileSync(path.join(root,'Library/MapValidation/pets-cartoon-v24/guid-map.json'),'utf8'));
const built=pairs.reduce((s,[a,b])=>s.replaceAll(b,a),fs.readFileSync(path.join(stage,'assembled.prefab'),'utf8'));
const current=fs.readFileSync(file,'utf8');
const old=graph(before),next=graph(built),live=graph(current);
const allowed=p=>/^Pets\/Body\/Workshop\/Incubator[0-3]\/(PawButton_PLACEHOLDER|Available\/TimeBar_PLACEHOLDER)(\/|$)/.test(p);
function raw(text){const out=new Map();for(const b of text.split(/(?=^--- !u!)/m)){const h=b.match(/^--- !u!\d+ &(-?\d+)/m);if(h)out.set(h[1],b.replace(/\r/g,'').trimEnd());}return out;}
const liveRaw=raw(current),newRaw=raw(built),idMap=new Map(),used=new Set(live.ids.values());
for(const[key,id]of next.ids){let target=live.ids.get(key);if(!target){target=id;while(used.has(target))target=(BigInt('0x'+crypto.randomBytes(8).toString('hex'))&((1n<<62n)-1n)).toString();used.add(target);}idMap.set(id,target);}
const edits=new Map(),added=[],audit={changed:[],added:[],preserved:0};
for(const[key,block]of next.blocks){
 if(!allowed(block.path))continue;
 const previous=old.blocks.get(key);if(previous?.text===block.text)continue;
 const authored=live.blocks.get(key);
 if(previous&&authored?.text!==previous.text)throw Error('User edited the same component during production; rebase on latest prefab: '+key);
 if(!previous&&authored)throw Error('New node already exists: '+key);
 let body=newRaw.get(next.ids.get(key));
 body=body.replace(/^--- !u!(\d+) &(-?\d+)/,(m,kind,id)=>'--- !u!'+kind+' &'+idMap.get(id));
 body=body.replace(/\{fileID: (-?\d+)\}/g,(m,id)=>idMap.has(id)?'{fileID: '+idMap.get(id)+'}':m);
 if(authored){edits.set(live.ids.get(key),body);audit.changed.push(key);}else{added.push(body);audit.added.push(key);}
}
for(const[key,block]of old.blocks)if(allowed(block.path)&&!next.blocks.has(key))throw Error('Unexpected deletion '+key);
const result='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+[...liveRaw].map(([id,b])=>edits.get(id)||b).concat(added).join('\n')+'\n';
const verified=graph(result);
for(const[key,block]of live.blocks)if(!audit.changed.includes(key)){assert.equal(verified.blocks.get(key)?.text,block.text,'Unrelated authored component changed: '+key);audit.preserved++;}
assert(audit.changed.length>20&&audit.added.length>20,'Expected authored controls not assembled');
fs.writeFileSync(path.join(stage,'installation-audit.json'),JSON.stringify(audit,null,2));
if(process.argv.includes('--install')){
 assert(fs.readFileSync(path.join(stage,'review.txt'),'utf8').startsWith('PASS:'),'Native review not passed');
 assert.equal(fs.readFileSync(path.join(stage,'install-before.prefab'),'utf8'),current,'Main prefab changed after review; prepare again');
 assert.equal(fs.readFileSync(path.join(stage,'candidate.prefab'),'utf8'),result,'Candidate changed after native review');
 fs.writeFileSync(file,result);
 console.log('Installed: '+audit.changed.length+' changed, '+audit.added.length+' added, '+audit.preserved+' authored components preserved.');
}else{
 fs.writeFileSync(path.join(stage,'candidate.prefab'),result);
 fs.writeFileSync(path.join(stage,'install-before.prefab'),current);
 const iso=path.join(root,'Library/MapValidation/PreviewProject/Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab');
 fs.writeFileSync(iso,pairs.reduce((s,[a,b])=>s.replaceAll(a,b),result));
 console.log('Prepared audited candidate: '+audit.changed.length+' changed, '+audit.added.length+' added, '+audit.preserved+' authored components preserved.');
}
