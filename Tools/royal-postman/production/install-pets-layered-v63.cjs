const fs=require('fs'),path=require('path'),crypto=require('crypto'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),stage=path.join(root,'Library/MapValidation/pets-layered-v63'),file='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab';
const before=fs.readFileSync(path.join(stage,'before.prefab'),'utf8');
let next=fs.readFileSync(path.join(stage,'assembled.prefab'),'utf8');
for(const[a,b]of JSON.parse(fs.readFileSync(path.join(root,'Library/MapValidation/pets-cartoon-v24/guid-map.json'),'utf8')))next=next.replaceAll(b,a);
const a=graph(before),b=graph(next),ids=new Map(),used=new Set();
for(const[k,newId]of b.ids){let id=a.ids.get(k)||newId;while(used.has(id))id=(BigInt('0x'+crypto.randomBytes(8).toString('hex'))&((1n<<62n)-1n)).toString();used.add(id);ids.set(newId,id);}
function raw(s){const out=new Map();for(const t of s.split(/(?=^--- !u!)/m)){const h=t.match(/^--- !u!\d+ &(-?\d+)/m);if(h)out.set(h[1],t.replace(/\r/g,'').trimEnd());}return out;}
const ar=raw(before),br=raw(next),blocks=[],audit={changed:[],added:[],removed:[],preservedCurrentProfile:0};
const allowed=p=>['Pets/Room','Pets/Body','Pets/Inventory','Pets/Divider','Pets/Header','Pets/CurrentTab','Pets/IncubatorTab'].some(prefix=>p===prefix||p.startsWith(prefix+'/'));
for(const[k,item]of a.blocks)if(!b.blocks.has(k)){if(!allowed(item.path))throw Error('Unexpected removal '+k);audit.removed.push(k);}
for(const[k,item]of b.blocks){
 const old=a.blocks.get(k);
 if(old&&old.text===item.text){blocks.push(ar.get(a.ids.get(k)));if(item.path.startsWith('Pets/CurrentProfile'))audit.preservedCurrentProfile++;continue;}
 if(item.path.startsWith('Pets/CurrentProfile')){
  if(k!=='Pets/CurrentProfile#1'||old.text.replace('m_IsActive: 1','m_IsActive: 0')!==item.text)throw Error('Unexpected current profile change '+k);
 }else if(item.path!=='Pets'&&!allowed(item.path)&&old?.text!==item.text)throw Error('Unexpected change outside incubator: '+k);
 let body=br.get(b.ids.get(k));
 body=body.replace(/^--- !u!(\d+) &(-?\d+)/,(m,kind,id)=>'--- !u!'+kind+' &'+ids.get(id));
 body=body.replace(/\{fileID: (-?\d+)\}/g,(m,id)=>ids.has(id)?'{fileID: '+ids.get(id)+'}':m);
 blocks.push(body);(old?audit.changed:audit.added).push(k);
}
const merged='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+blocks.join('\n')+'\n';
graph(merged);
fs.writeFileSync(path.join(stage,'candidate.prefab'),merged);
fs.writeFileSync(path.join(stage,'installation-audit.json'),JSON.stringify(audit,null,2));
if(process.argv.includes('--install')){
 const review=fs.readFileSync(path.join(stage,'review.txt'),'utf8');if(!review.startsWith('PASS:'))throw Error('Native validation not passed');
 if(fs.readFileSync(path.join(root,file),'utf8')!==before)throw Error('Main prefab changed; refusing to overwrite new edits');
 // Install the audited semantic merge, retaining original IDs and untouched authored blocks.
 fs.writeFileSync(path.join(root,file),merged);
 console.log('Installed audited layout: '+audit.changed.length+' changed, '+audit.added.length+' new, '+audit.removed.length+' removed components; '+audit.preservedCurrentProfile+' current-profile components retained exactly.');
}else console.log('Prepared audited candidate: '+JSON.stringify(Object.fromEntries(Object.entries(audit).map(([k,v])=>[k,Array.isArray(v)?v.length:v]))));

