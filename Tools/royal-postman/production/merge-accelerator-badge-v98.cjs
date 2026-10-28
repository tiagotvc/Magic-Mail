const fs=require('fs'),path=require('path'),crypto=require('crypto'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),stage=path.join(root,'Library/MapValidation/accelerator-badge-v98'),iso=path.join(root,'Library/MapValidation/PreviewProject');
const relative='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab',file=path.join(root,relative),pairs=JSON.parse(fs.readFileSync(path.join(root,'Library/MapValidation/pets-cartoon-v24/guid-map.json'),'utf8'));
const map=s=>pairs.reduce((s,[a,b])=>s.replaceAll(a,b),s),unmap=s=>pairs.reduce((s,[a,b])=>s.replaceAll(b,a),s);
const before=fs.readFileSync(path.join(stage,'before.prefab'),'utf8'),built=unmap(fs.readFileSync(path.join(stage,'assembled.prefab'),'utf8')),current=fs.readFileSync(file,'utf8');
const old=graph(before),next=graph(built),live=graph(current);
function raw(text){return new Map(text.split(/(?=^--- !u!)/m).flatMap(b=>{const h=b.match(/^--- !u!\d+ &(-?\d+)/m);return h?[[h[1],b.replace(/\r/g,'').trimEnd()]]:[]}));}
const liveRaw=raw(current),newRaw=raw(built),ids=new Map(),used=new Set(live.ids.values());
for(const[k,id]of next.ids){let target=live.ids.get(k);if(!target){target=id;while(used.has(target))target=(BigInt('0x'+crypto.randomBytes(8).toString('hex'))&((1n<<62n)-1n)).toString();used.add(target);}ids.set(id,target);}
function remap(body){return body.replace(/^--- !u!(\d+) &(-?\d+)/,(m,kind,id)=>'--- !u!'+kind+' &'+ids.get(id)).replace(/\{fileID: (-?\d+)\}/g,(m,id)=>ids.has(id)?'{fileID: '+ids.get(id)+'}':m);}
function field(body,name){const lines=body.split('\n'),start=lines.findIndex(l=>l.startsWith('  '+name+':'));if(start<0)return null;let end=start+1;while(end<lines.length&&!/^  [A-Za-z_]\w*:/.test(lines[end]))end++;return lines.slice(start,end).join('\n');}
function overlay(body,source,names){for(const name of names){const replacement=field(source,name);if(replacement==null)throw Error('Missing field: '+name);const previous=field(body,name);body=previous==null?body+'\n'+replacement:body.replace(previous,replacement);}return body;}
const accelerator='Pets/Hatchery/Accelerate';
function allowed(b){
 if(b.path===accelerator&&b.kind==='224')return ['m_Children'];
 if(b.path==='Pets/Hatchery'&&b.text.includes('unlockLevels:'))return ['accelerateQuantity','accelerateCost','separateAcceleratorStock'];
 if((b.path===accelerator+'/Quantity'||b.path===accelerator+'/Image')&&b.kind==='1')return ['m_IsActive'];
 return null;
}
const newTree=p=>p===accelerator+'/ResourceBadge'||p.startsWith(accelerator+'/ResourceBadge/')||p===accelerator+'/GoldCost';
function copyAsset(rel){const target=path.join(root,rel);if(fs.existsSync(target))throw Error('Asset already exists: '+rel);fs.mkdirSync(path.dirname(target),{recursive:true});for(const suffix of ['', '.meta'])fs.writeFileSync(target+suffix,unmap(fs.readFileSync(path.join(iso,rel+suffix),'utf8')));}
const edits=new Map(),added=[],audit={changed:[],added:[],preservedComponents:0,preservedCurrentProfile:0};
for(const[k,b]of next.blocks){
 const authored=live.blocks.get(k),fields=allowed(b);if(!newTree(b.path)&&!fields)continue;
 let body=remap(newRaw.get(next.ids.get(k)));
 // Keep a clear gap below the user's current caption in the new cost label.
 if(b.path===accelerator+'/GoldCost'&&b.kind==='224')body=body.replace(/(m_AnchoredPosition: \{x: 0, y: )-130(\})/,'$1-136$2');
 if(newTree(b.path)){if(authored)throw Error('Controls already exist: '+k);added.push(body);audit.added.push(k);}
 else if(authored){
  for(const name of fields){const original=old.blocks.get(k);if(original&&field(original.text,name)!==field(authored.text,name))throw Error('Concurrent edit in requested field: '+k+' '+name);}
  body=overlay(liveRaw.get(live.ids.get(k)),body,fields);if(body!==liveRaw.get(live.ids.get(k))){edits.set(live.ids.get(k),body);audit.changed.push(k);}
 }else throw Error('Existing component missing: '+k);
}
const result='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+[...liveRaw].map(([id,b])=>edits.get(id)||b).concat(added).join('\n')+'\n',verified=graph(result);
for(const[k,b]of live.blocks){const v=verified.blocks.get(k);if(!v)throw Error('Authored component lost: '+k);if(!audit.changed.includes(k)){if(v.text!==b.text)throw Error('Unexpected mutation: '+k);audit.preservedComponents++;}if(b.path.startsWith('Pets/CurrentProfile')){if(v.text!==b.text)throw Error('Profile modified: '+k);audit.preservedCurrentProfile++;}}
if(process.argv.includes('--install')){
 if(!fs.readFileSync(path.join(stage,'review.txt'),'utf8').startsWith('PASS:'))throw Error('Native review not passed');
 if(fs.readFileSync(path.join(stage,'install-before.prefab'),'utf8')!==current||fs.readFileSync(path.join(stage,'candidate.prefab'),'utf8')!==result)throw Error('Prefab changed after review');
 for(const rel of ['Assets/RoyalAves/Art/UI/Menu/RoyalPostman/AcceleratorBadgeV98/Sprites/badge.asset','Assets/RoyalAves/Art/UI/Menu/RoyalPostman/AcceleratorBadgeV98/BadgeGold.mat','Assets/RoyalAves/Art/UI/Menu/RoyalPostman/AcceleratorBadgeV98/BadgeRed.mat','Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Components/AcceleratorStockBadgeV98.prefab'])copyAsset(rel);
 fs.writeFileSync(file,result);fs.writeFileSync(path.join(stage,'installation-audit.json'),JSON.stringify(audit,null,2));console.log('Installed '+audit.changed.length+' requested component changes and '+audit.added.length+' new; '+audit.preservedComponents+' unchanged, including '+audit.preservedCurrentProfile+' profile components.');
}else{
 fs.writeFileSync(path.join(stage,'candidate.prefab'),result);fs.writeFileSync(path.join(stage,'install-before.prefab'),current);fs.writeFileSync(path.join(iso,relative),map(result));fs.writeFileSync(path.join(stage,'installation-audit.json'),JSON.stringify(audit,null,2));console.log('Prepared surgical candidate; '+audit.preservedComponents+' existing components preserved.');
}
