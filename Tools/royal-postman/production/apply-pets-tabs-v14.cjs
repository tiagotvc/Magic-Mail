const fs=require('fs'),path=require('path'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),preview=path.join(root,'Library/MapValidation/PreviewProject'),stage=path.join(root,'Library/MapValidation/pets-tabs-v14');
const prefab='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab',target=path.join(root,prefab),output=path.join(root,'Library/MapValidation/pets-mock-v2');
if(!fs.readFileSync(path.join(output,'validation.txt'),'utf8').startsWith('PASS: V14'))throw Error('Native V14 checks have not passed');
const before=fs.readFileSync(path.join(stage,'before.prefab'),'utf8');let native=fs.readFileSync(path.join(preview,prefab),'utf8');
for(const [from,to]of JSON.parse(fs.readFileSync(path.join(stage,'guid-map.json'),'utf8')))native=native.replaceAll(from,to);
const old=graph(before),next=graph(native),stateFields=['currentTabBlue','currentTabGold','incubatorTabBlue','incubatorTabGold'];
const controller=b=>b.path==='Pets'&&b.kind==='114'&&b.text.includes('incubators:');
function fields(b){
 if(controller(b))return stateFields;
 if(/^Pets\/(CurrentTab|IncubatorTab)\/Face$/.test(b.path)&&b.kind==='114')return ['m_Sprite','m_Color'];
 if(/^Pets\/(CurrentTab|IncubatorTab)$/.test(b.path)&&b.kind==='114'&&b.text.includes('m_Interactable:'))return ['m_NormalColor','m_HighlightedColor','m_SelectedColor'];
 return [];
}
const pattern=field=>new RegExp('^  *'+field+': [^\\r\\n]+','gm');
function strip(text,b){return fields(b).reduce((s,k)=>controller(b)?s.replace(new RegExp('^  '+k+':[^\\n]*\\n?','gm'),''):s.replace(pattern(k),k+': <requested>'),text).trim();}
let changes=0;
for(const key of new Set([...old.blocks.keys(),...next.blocks.keys()])){const a=old.blocks.get(key),b=next.blocks.get(key);if(a?.text===b?.text)continue;if(!a||!b||!fields(b).length||strip(a.text,b)!==strip(b.text,b))throw Error('Unrelated native change: '+key);changes++;}
const art='Assets/RoyalAves/Art/UI/Tab/RoyalPostman/Pets/',guid=text=>text.match(/^guid: (\w+)/m)[1],textureMap=[];
for(const id of ['pets-tab-current-v4','pets-tab-incubator-v4','pets-tab-current-gold-v14','pets-tab-incubator-blue-v14']){
 const main=path.join(root,art+id+'.png'),source=path.join(preview,art+id+'.png');
 if(!fs.readFileSync(main).equals(fs.readFileSync(source)))throw Error('Source PNG mismatch: '+id);
 const a=guid(fs.readFileSync(main+'.meta','utf8')),b=guid(fs.readFileSync(source+'.meta','utf8'));if(a!==b)textureMap.push([b,a]);
}
for(const id of ['pets-tab-current-blue-v14','pets-tab-current-gold-v14','pets-tab-incubator-blue-v14','pets-tab-incubator-gold-v14']){
 const rel=art+'Sprites/'+id+'.asset';let data=fs.readFileSync(path.join(preview,rel),'utf8');for(const [a,b]of textureMap)data=data.replaceAll(a,b);
 for(const [suffix,bytes]of [['',data],['.meta',fs.readFileSync(path.join(preview,rel+'.meta'),'utf8')]]){const destination=path.join(root,rel+suffix);if(fs.existsSync(destination)&&fs.readFileSync(destination,'utf8')!==bytes)throw Error('Unexpected existing V14 Sprite: '+rel+suffix);fs.writeFileSync(destination,bytes);}
}
const current=fs.readFileSync(target,'utf8'),live=graph(current),aliases=new Map([...live.ids].map(([key,id])=>[id,key]));let applied=0;
const patched=current.split(/(?=^--- !u!)/m).map(block=>{
 const id=block.match(/^--- !u!\d+ &(-?\d+)/m)?.[1],key=aliases.get(id),item=live.blocks.get(key);if(!item)return block;
 const keys=fields(item);if(!keys.length)return block;const source=next.blocks.get(key);if(!source)throw Error('Missing tab component: '+key);
 for(const field of keys){const re=pattern(field),value=source.text.match(re)?.[0];if(!value)throw Error('Missing tab field '+field);if(re.test(block))block=block.replace(re,()=>value);else if(controller(item))block=block.trimEnd()+'\n'+value+'\n';else throw Error('Current component missing '+field);}
 applied++;return block;
}).join('');
const installed=graph(patched);
for(const [key,item]of live.blocks){const result=installed.blocks.get(key);if(!result||strip(item.text,item)!==strip(result.text,result))throw Error('Unrelated authored field changed: '+key);}
if(fs.readFileSync(target,'utf8')!==current)throw Error('Concurrent save; rerun selective installer');
fs.writeFileSync(path.join(stage,'before-install.prefab'),current);fs.writeFileSync(target,patched);
const review=path.join(root,'Tools/royal-postman/production/review/pets-tabs-v14');fs.mkdirSync(review,{recursive:true});
for(const name of fs.readdirSync(output).filter(n=>/^(pets-|inventory-row-).+\.png$/.test(n)||n==='validation.txt'))fs.copyFileSync(path.join(output,name),path.join(review,name));
fs.appendFileSync(path.join(review,'validation.txt'),'Selective install: '+changes+' validated changes; '+applied+' tab/controller components updated. Other authored fields/IDs and source V4 PNGs preserved. Two original 1254x1254 transparent ImageGen PNGs added; source pixels unchanged. Four native Sprite regions installed. Production runtime/editor compile passed.\n');
console.log('Applied V14: four painted tab states and selection routing installed; '+applied+' tab/controller components updated.');
