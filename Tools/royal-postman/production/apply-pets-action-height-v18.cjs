const fs=require('fs'),path=require('path'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),stage=path.join(root,'Library/MapValidation/pets-mobile-v17'),preview=path.join(root,'Library/MapValidation/PreviewProject');
if(!fs.readFileSync(path.join(stage,'validation.txt'),'utf8').startsWith('PASS:'))throw Error('Native checks have not passed');
const rel='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab',file=path.join(root,rel),before=fs.readFileSync(path.join(stage,'before-height.prefab'),'utf8');if(fs.readFileSync(file,'utf8')!==before)throw Error('Authored prefab changed since height staging');
const mappings=JSON.parse(fs.readFileSync(path.join(stage,'guid-map.json'),'utf8'));let native=fs.readFileSync(path.join(preview,rel),'utf8');for(const[a,b]of mappings)native=native.replaceAll(b,a);
const old=graph(before),next=graph(native);let changed=0;
for(const key of new Set([...old.blocks.keys(),...next.blocks.keys()])){
 const a=old.blocks.get(key),b=next.blocks.get(key);if(a?.text===b?.text)continue;if(!a||!b)throw Error('Unexpected structural change '+key);
 let fields=[];
 if(b.kind==='224'&&(b.path.startsWith('Pets/Body/Workshop/Incubator')||b.path==='Pets/Body/Workshop/Rarity'))fields=['m_AnchoredPosition','m_SizeDelta'];
 else if(b.kind==='114'&&/\/(UseItem|Accelerate)\/(FixedGold|MovingCenter\/Face)$/.test(b.path))fields=['m_Type','m_PixelsPerUnitMultiplier'];
 else if(b.kind==='114'&&/\/(UseItem|Accelerate)\/MovingCenter\/Label$/.test(b.path))fields=['m_FontSize','m_MaxSize'];
 const strip=s=>fields.reduce((t,f)=>t.replace(new RegExp('^ +'+f+':[^\\n]*','gm'),f+': <height>'),s);
 if(fields.length===0||strip(a.text)!==strip(b.text))throw Error('Unrelated change: '+key);changed++;
}
const aliases=new Map([...next.ids].map(([key,id])=>[id,key]));
native=native.replace(/^--- !u!(\d+) &(-?\d+)/gm,(all,kind,id)=>aliases.has(id)?'--- !u!'+kind+' &'+old.ids.get(aliases.get(id)):all).replace(/fileID: (-?\d+)/g,(all,id)=>aliases.has(id)?'fileID: '+old.ids.get(aliases.get(id)):all);
for(const color of ['purple','blue']){const rel='Assets/RoyalAves/Art/UI/Button/RoyalPostman/Pets/Sprites/pets-action-'+color+'-v17.asset';let data=fs.readFileSync(path.join(preview,rel),'utf8');for(const[a,b]of mappings)data=data.replaceAll(b,a);fs.writeFileSync(path.join(root,rel),data);}
fs.writeFileSync(file,native);console.log('Installed taller buttons: 600x140 (+27%), panel/spacing/icons/type adjusted, painted cap proportions retained through 9-slice. '+changed+' selective component updates.');
