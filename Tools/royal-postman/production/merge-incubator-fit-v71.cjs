const fs=require('fs'),path=require('path'),crypto=require('crypto'),assert=require('assert'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),stage=path.join(root,'Library/MapValidation/incubator-fit-v71');
const file=path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab');
const pairs=JSON.parse(fs.readFileSync(path.join(root,'Library/MapValidation/pets-cartoon-v24/guid-map.json'),'utf8'));
const before=fs.readFileSync(path.join(stage,'before.prefab'),'utf8');
const built=pairs.reduce((s,[a,b])=>s.replaceAll(b,a),fs.readFileSync(path.join(stage,'assembled.prefab'),'utf8'));
const current=fs.readFileSync(file,'utf8');
const canonical=s=>s.replace(/(Incubator[0-3])\/Machine\/BaseFront\/PawButton_PLACEHOLDER/g,'$1/PawButton_PLACEHOLDER');
function model(text){const g=graph(text);return{blocks:new Map([...g.blocks].map(([k,b])=>[canonical(k),{...b,path:canonical(b.path),text:canonical(b.text)}])),ids:new Map([...g.ids].map(([k,id])=>[canonical(k),id]))};}
const old=model(before),next=model(built),live=model(current);
const control=p=>/^Pets\/Body\/Workshop\/Incubator[0-3]\/(PawButton_PLACEHOLDER|Status)(\/|$)/.test(p);
const parentKey=k=>k.match(/^Pets\/Body\/Workshop\/Incubator([0-3])(\/Machine\/BaseFront)?#224$/);
function raw(text){return new Map(text.split(/(?=^--- !u!)/m).flatMap(b=>{const h=b.match(/^--- !u!\d+ &(-?\d+)/m);return h?[[h[1],b.replace(/\r/g,'').trimEnd()]]:[]}));}
const liveRaw=raw(current),newRaw=raw(built),ids=new Map(),used=new Set(live.ids.values());
for(const[k,id]of next.ids){let target=live.ids.get(k);if(!target){target=id;while(used.has(target))target=(BigInt('0x'+crypto.randomBytes(8).toString('hex'))&((1n<<62n)-1n)).toString();used.add(target);}ids.set(id,target);}
const edits=new Map(),added=[],audit={changed:[],added:[],parentLinksOnly:[],preserved:0};
const childrenPattern=/^  m_Children:(?: \[\])?\n(?:  - \{fileID: -?\d+\}\n)*/m;
for(const[k,b]of next.blocks){
 const parent=parentKey(k);if(!control(b.path)&&!parent)continue;
 const previous=old.blocks.get(k),authored=live.blocks.get(k);
 if(previous?.text===b.text&&!parent)continue;
 let body;
 if(parent){
  assert(authored,'Parent missing: '+k);body=liveRaw.get(live.ids.get(k));
  const buttonId=live.ids.get('Pets/Body/Workshop/Incubator'+parent[1]+'/PawButton_PLACEHOLDER#224');
  const match=body.match(childrenPattern);assert(match,'Children missing: '+k);
  let list=[...match[0].matchAll(/fileID: (-?\d+)/g)].map(m=>m[1]);
  list=parent[2]?[...new Set([...list,buttonId])]:list.filter(id=>id!==buttonId);
  const replacement=list.length?'  m_Children:\n'+list.map(id=>'  - {fileID: '+id+'}\n').join(''):'  m_Children: []\n';
  body=body.replace(childrenPattern,replacement);audit.parentLinksOnly.push(k);
 }else{
  if(previous&&authored?.text!==previous.text)throw Error('Concurrent edit of the same control: '+k);
  if(!previous&&authored)throw Error('New component already exists: '+k);
  body=newRaw.get(next.ids.get(k));
  body=body.replace(/^--- !u!(\d+) &(-?\d+)/,(m,kind,id)=>'--- !u!'+kind+' &'+ids.get(id));
  body=body.replace(/\{fileID: (-?\d+)\}/g,(m,id)=>ids.has(id)?'{fileID: '+ids.get(id)+'}':m);
 }
 if(authored){edits.set(live.ids.get(k),body);audit.changed.push(k);}else{added.push(body);audit.added.push(k);}
}
const result='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+[...liveRaw].map(([id,b])=>edits.get(id)||b).concat(added).join('\n')+'\n';
const verified=model(result),verifiedRaw=raw(result);
for(const[k,b]of live.blocks){
 const v=verified.blocks.get(k);assert(v,'Authored component lost: '+k);
 if(parentKey(k)){
  const oldBody=liveRaw.get(live.ids.get(k)).replace(childrenPattern,''),newBody=verifiedRaw.get(verified.ids.get(k)).replace(childrenPattern,'');
  assert.equal(newBody,oldBody,'Parent geometry changed: '+k);
 }else if(!audit.changed.includes(k)){assert.equal(v.text,b.text,'Unrelated component changed: '+k);audit.preserved++;}
}
assert(audit.changed.length>=28&&audit.added.length===4,'Expected mounted controls missing');
fs.writeFileSync(path.join(stage,'installation-audit.json'),JSON.stringify(audit,null,2));
if(process.argv.includes('--install')){
 assert(fs.readFileSync(path.join(stage,'review.txt'),'utf8').startsWith('PASS:'),'Native review not passed');
 if(fs.readFileSync(path.join(stage,'install-before.prefab'),'utf8')!==current)throw Error('Main prefab changed after review; preserve latest authored edits and prepare again');
 if(fs.readFileSync(path.join(stage,'candidate.prefab'),'utf8')!==result)throw Error('Candidate changed after native review');
 fs.writeFileSync(file,result);
 console.log('Installed '+audit.changed.length+' control/parent changes, '+audit.added.length+' shadows; '+audit.preserved+' other components retained; parent positions/sizes untouched.');
}else{
 fs.writeFileSync(path.join(stage,'candidate.prefab'),result);fs.writeFileSync(path.join(stage,'install-before.prefab'),current);
 fs.writeFileSync(path.join(root,'Library/MapValidation/PreviewProject/Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab'),pairs.reduce((s,[a,b])=>s.replaceAll(a,b),result));
 console.log('Prepared mounted candidate: '+audit.changed.length+' changed, '+audit.added.length+' added; '+audit.preserved+' other components retained.');
}
