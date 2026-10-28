const fs=require('fs'),path=require('path'),crypto=require('crypto'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),stage=path.join(root,'Library/MapValidation/hatchery-v87'),iso=path.join(root,'Library/MapValidation/PreviewProject');
const relative='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab',file=path.join(root,relative),pairs=JSON.parse(fs.readFileSync(path.join(root,'Library/MapValidation/pets-cartoon-v24/guid-map.json'),'utf8'));
const unmap=s=>pairs.reduce((s,[a,b])=>s.replaceAll(b,a),s),before=fs.readFileSync(path.join(stage,'before.prefab'),'utf8'),built=unmap(fs.readFileSync(path.join(stage,'assembled.prefab'),'utf8')),current=fs.readFileSync(file,'utf8');
const old=graph(before),next=graph(built),live=graph(current);
function raw(text){return new Map(text.split(/(?=^--- !u!)/m).flatMap(b=>{const h=b.match(/^--- !u!\d+ &(-?\d+)/m);return h?[[h[1],b.replace(/\r/g,'').trimEnd()]]:[]}));}
const liveRaw=raw(current),newRaw=raw(built),ids=new Map(),used=new Set(live.ids.values());
for(const[k,id]of next.ids){let target=live.ids.get(k);if(!target){target=id;while(used.has(target))target=(BigInt('0x'+crypto.randomBytes(8).toString('hex'))&((1n<<62n)-1n)).toString();used.add(target);}ids.set(id,target);}
function remap(body){return body.replace(/^--- !u!(\d+) &(-?\d+)/,(m,kind,id)=>'--- !u!'+kind+' &'+ids.get(id)).replace(/\{fileID: (-?\d+)\}/g,(m,id)=>ids.has(id)?'{fileID: '+ids.get(id)+'}':m);}
function field(body,name){const lines=body.split('\n'),start=lines.findIndex(l=>l.startsWith('  '+name+':'));if(start<0)return null;let end=start+1;while(end<lines.length&&!/^  [A-Za-z_]\w*:/.test(lines[end]))end++;return lines.slice(start,end).join('\n');}
function overlay(body,source,names){for(const name of names){const replacement=field(source,name);if(replacement==null)throw Error('Missing requested field: '+name);const previous=field(body,name);body=previous==null?body+'\n'+replacement:body.replace(previous,replacement);}return body;}
function allowed(b){
 if(b.path==='Pets'&&b.kind==='224')return ['m_Children'];
 if(b.path==='Pets'&&b.text.includes('backButton:'))return ['hatchery','workshop','items','currentTab','incubatorTab','previousIncubator','nextIncubator','defaultCurrentTab'];
 if(/^Pets\/(Room|Body|Divider|Inventory|CurrentTab|IncubatorTab)$/.test(b.path)&&b.kind==='1')return ['m_IsActive'];
 if(b.path==='Pets/Header/Title'&&b.kind==='224')return ['m_SizeDelta'];
 if(b.path==='Pets/Header/Title'&&b.text.includes('portuguese:'))return ['portuguese','english','petsPortuguese','petsEnglish','titleHeight'];
 return null;
}
const newTree=p=>/^Pets\/(Hatchery|HatcheryTabs)(\/|$)/.test(p);
const edits=new Map(),added=[],audit={changed:[],added:[],preservedComponents:0,preservedCurrentProfile:0};
for(const[k,b]of next.blocks){
 const authored=live.blocks.get(k),fields=allowed(b);if(!newTree(b.path)&&!fields)continue;
 let body=remap(newRaw.get(next.ids.get(k)));
 if(newTree(b.path)){if(authored)throw Error('Hatchery already exists; do not rebuild an authored screen: '+k);added.push(body);audit.added.push(k);}
 else if(authored){body=overlay(liveRaw.get(live.ids.get(k)),body,fields);if(body!==liveRaw.get(live.ids.get(k))){edits.set(live.ids.get(k),body);audit.changed.push(k);}}
 else throw Error('Missing existing component: '+k);
}
const result='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'+[...liveRaw].map(([id,b])=>edits.get(id)||b).concat(added).join('\n')+'\n',verified=graph(result);
for(const[k,b]of live.blocks){const v=verified.blocks.get(k);if(!v)throw Error('Authored object lost: '+k);if(!audit.changed.includes(k)){if(v.text!==b.text)throw Error('Unexpected component mutation: '+k);audit.preservedComponents++;}if(b.path.startsWith('Pets/CurrentProfile')){if(v.text!==b.text)throw Error('Profile modified: '+k);audit.preservedCurrentProfile++;}}
function copyAsset(rel){const source=path.join(iso,rel),target=path.join(root,rel);fs.mkdirSync(path.dirname(target),{recursive:true});fs.writeFileSync(target,unmap(fs.readFileSync(source,'utf8')));fs.writeFileSync(target+'.meta',unmap(fs.readFileSync(source+'.meta','utf8')));}
if(process.argv.includes('--install')){
 if(!fs.readFileSync(path.join(stage,'review.txt'),'utf8').startsWith('PASS:'))throw Error('Native review not passed');
 if(fs.readFileSync(path.join(stage,'install-before.prefab'),'utf8')!==current||fs.readFileSync(path.join(stage,'candidate.prefab'),'utf8')!==result)throw Error('Prefab changed since review; re-prepare against current edits');
 const art='Assets/RoyalAves/Art/UI/Menu/RoyalPostman/HatcheryV87';
 for(const name of fs.readdirSync(path.join(iso,art,'Sprites')).filter(n=>n.endsWith('.asset')))copyAsset(art+'/Sprites/'+name);
 for(const name of fs.readdirSync(path.join(iso,art)).filter(n=>n.endsWith('.mat')))copyAsset(art+'/'+name);
 copyAsset('Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Components/HatcheryHeaderV86.prefab');
 fs.writeFileSync(file,result);fs.writeFileSync(path.join(stage,'installation-audit.json'),JSON.stringify(audit,null,2));console.log('Installed: '+audit.changed.length+' existing components updated, '+audit.added.length+' new; '+audit.preservedComponents+' unchanged, including '+audit.preservedCurrentProfile+' profile components.');
}else{
 fs.writeFileSync(path.join(stage,'candidate.prefab'),result);fs.writeFileSync(path.join(stage,'install-before.prefab'),current);
 fs.writeFileSync(path.join(iso,relative),pairs.reduce((s,[a,b])=>s.replaceAll(a,b),result));fs.writeFileSync(path.join(stage,'installation-audit.json'),JSON.stringify(audit,null,2));console.log('Prepared: '+audit.changed.length+' requested component changes; '+audit.preservedCurrentProfile+' profile components exactly preserved.');
}
