const fs=require('fs'),path=require('path'),crypto=require('crypto'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),preview=path.join(root,'Library/MapValidation/PreviewProject'),stage=path.join(root,'Library/MapValidation/pets-mobile-v17'),prefab='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab',file=path.join(root,prefab);
if(!fs.readFileSync(path.join(stage,'validation.txt'),'utf8').startsWith('PASS:'))throw Error('Native mobile validation has not passed');
const before=fs.readFileSync(path.join(stage,'before.prefab'),'utf8'),current=fs.readFileSync(file,'utf8');if(current!==before)throw Error('Pets prefab changed since staging; rebase the incubation migration before installing');
const mappings=JSON.parse(fs.readFileSync(path.join(stage,'guid-map.json'),'utf8'));let native=fs.readFileSync(path.join(preview,prefab),'utf8');for(const[a,b]of mappings)native=native.replaceAll(b,a);
const old=graph(before),next=graph(native);
const incubation=p=>['Pets/Room','Pets/Body','Pets/Inventory'].some(root=>p===root||p.startsWith(root+'/'));
const frame=p=>['Pets/ItemTooltip/Card','Pets/ItemTooltip/Card/Title','Pets/ItemTooltip/Card/Description','Pets/ItemTooltip/Card/Close'].includes(p);
let replaced=0;
for(const key of new Set([...old.blocks.keys(),...next.blocks.keys()])){
 const a=old.blocks.get(key),b=next.blocks.get(key),item=b||a;if(a?.text===b?.text)continue;
 if(incubation(item.path)){replaced++;continue;}
 if(item.path==='Pets'&&item.kind==='224')continue;
 if(item.path==='Pets'&&item.kind==='114'&&item.text.includes('incubators:')){
  for(const field of ['backButton','helpButton','currentTab','incubatorTab','dismissHelp','defaultCurrentTab','currentPanel','helpPanel','hintTitle','hintDescription','itemTooltip','tooltipCard','tooltipIcon','dismissItemTooltip','currentTabBlue','currentTabGold','incubatorTabBlue','incubatorTabGold','commonEggSeconds','rareEggSeconds','epicEggSeconds','goldPrices']){
   const re=new RegExp('^ +'+field+':[^\\n]*','m');if(a.text.match(re)?.[0]!==b.text.match(re)?.[0])throw Error('Unrelated controller setting changed: '+field);
  }continue;
 }
 if(item.path==='Pets/Divider'&&item.kind==='224'){
  const strip=t=>t.replace(/^  m_(AnchorMin|AnchorMax|AnchoredPosition):[^\n]*/gm,'<responsive divider>');if(strip(a.text)!==strip(b.text))throw Error('Ornament changed');continue;
 }
 if(frame(item.path)&&item.kind==='224'){
  const strip=t=>t.replace(/^  m_(AnchoredPosition|SizeDelta):[^\n]*/gm,'<tooltip fit>');if(strip(a.text)!==strip(b.text))throw Error('Unrelated tooltip change');continue;
 }
 if(item.path==='Pets/HelpPanel/Card/Copy'&&item.kind==='114'){
  const strip=t=>t.replace(/^  m_Text:[\s\S]*$/m,'<updated help>');if(strip(a.text)!==strip(b.text))throw Error('Help styling changed');continue;
 }
 if(item.path==='Pets/Body/Workshop/Rarity'||item.path.startsWith('Pets/Body/Workshop/Rarity/'))continue;
 throw Error('Unrelated authored component changed: '+key);
}
const assigned=new Map(old.ids),used=new Set(assigned.values());for(const key of next.ids.keys())if(!assigned.has(key)){let id;do{id=(crypto.randomBytes(8).readBigUInt64LE()&0x7fffffffffffffffn).toString();}while(id==='0'||used.has(id));used.add(id);assigned.set(key,id);}
const aliases=new Map([...next.ids].map(([key,id])=>[id,key]));
native=native.replace(/^--- !u!(\d+) &(-?\d+)/gm,(all,kind,id)=>aliases.has(id)?'--- !u!'+kind+' &'+assigned.get(aliases.get(id)):all).replace(/fileID: (-?\d+)/g,(all,id)=>aliases.has(id)?'fileID: '+assigned.get(aliases.get(id)):all);
// Native regions/materials stay separate; the generated PNG originals are copied byte-for-byte.
const manifest=JSON.parse(fs.readFileSync(path.join(__dirname,'PETS-MOBILE-V17.json'),'utf8'));
const resources=[];for(const asset of manifest.assets){
 const main=path.join(root,asset.path);if(crypto.createHash('sha256').update(fs.readFileSync(main)).digest('hex')!==asset.sha256)throw Error('Source PNG changed: '+asset.id);
 resources.push(asset.path+'.meta',path.posix.dirname(asset.path)+'/Sprites/'+asset.id+'.asset',path.posix.dirname(asset.path)+'/Sprites/'+asset.id+'.asset.meta');
}
const materialDir='Assets/RoyalAves/Art/UI/Menu/RoyalPostman/Pets/Materials';for(const name of fs.readdirSync(path.join(preview,materialDir)).filter(f=>f.endsWith('-v17.mat'))){const rel=materialDir+'/'+name,meta=fs.readFileSync(path.join(preview,rel+'.meta'),'utf8'),guid=meta.match(/^guid: (\w+)/m)?.[1];if(guid&&native.includes(guid))resources.push(rel,rel+'.meta');}
for(const rel of resources){let data=fs.readFileSync(path.join(preview,rel));if(/\.(asset|mat)$/.test(rel)){let text=data.toString('utf8');for(const[a,b]of mappings)text=text.replaceAll(b,a);data=Buffer.from(text);}const target=path.join(root,rel);fs.mkdirSync(path.dirname(target),{recursive:true});fs.writeFileSync(target,data);}
fs.writeFileSync(file,native);
const final=graph(native);for(const [key,a]of old.blocks)if(key.startsWith('Pets/CurrentProfile')&&a.text!==final.blocks.get(key)?.text)throw Error('Current profile changed during installation');
const preserved=JSON.parse(fs.readFileSync(path.join(stage,'preserved.json'),'utf8'));for(const[n,sha]of Object.entries(preserved))if(crypto.createHash('sha256').update(fs.readFileSync(path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/'+n+'.prefab'))).digest('hex')!==sha)throw Error(n+' changed');
console.log('Installed mobile incubator tab: '+replaced+' incubation components; '+resources.length+' native asset/import files. Current profile, header/tab art, ornament pieces, Home and Navigation preserved.');
