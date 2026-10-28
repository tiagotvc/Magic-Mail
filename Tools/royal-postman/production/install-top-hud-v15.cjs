const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=path.resolve(__dirname,'../../..'),preview=path.join(root,'Library/MapValidation/PreviewProject'),home='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Home.prefab';
const top=new Set(['Avatar','CoinCounter','Coin','CoinValue','CoinPlus','LifeCounter','Life','LivesValue','LifeStatus','LifePlus','SettingsButton']);
function parse(text){
 const blocks=text.split(/(?=^--- !u!)/m),entries=new Map(),names=new Map(),rects=new Map();
 blocks.forEach((block,index)=>{const h=block.match(/^--- !u!(\d+) &(-?\d+)/);if(!h)return;const e={block,index,type:+h[1],id:h[2],go:block.match(/m_GameObject: \{fileID: (-?\d+)\}/)?.[1]};entries.set(e.id,e);
 if(e.type===1)names.set(e.id,block.match(/^  m_Name: (.*)$/m)?.[1]);
 if(e.type===224||e.type===4){e.parent=block.match(/m_Father: \{fileID: (-?\d+)\}/)?.[1];rects.set(e.id,e);}});
 const paths=new Map();function visit(r){if(paths.has(r.go))return paths.get(r.go);const parent=rects.get(r.parent),p=parent?[visit(parent),names.get(r.go)].filter(Boolean).join('/'):'';paths.set(r.go,p);return p;}for(const r of rects.values())visit(r);return{blocks,entries,paths};
}
const target=path.join(root,home),original=fs.readFileSync(target,'utf8'),to=parse(original),from=parse(fs.readFileSync(path.join(preview,home),'utf8'));let changed=0;
for(const [id,e]of to.entries){const p=to.paths.get(e.go);if(!p||!top.has(p.split('/')[0])||![224,114].includes(e.type))continue;
 const replacement=from.entries.get(id);if(!replacement||replacement.go!==e.go||replacement.type!==e.type)throw Error('Changed authored identity at '+p);
 if(e.block!==replacement.block){to.blocks[e.index]=replacement.block;changed++;}}
const backup=path.join(root,'Library/MapValidation/AuthoringBackup');fs.mkdirSync(backup,{recursive:true});fs.writeFileSync(path.join(backup,'Home-before-top-v15.prefab'),original);
const m=JSON.parse(fs.readFileSync(path.join(__dirname,'TOP-HUD-V15-MANIFEST.json'),'utf8'));let copies=0;
for(const a of m.assets)for(const suffix of ['.asset','.asset.meta']){const relative='Assets/RoyalAves/Art/RoyalPostmanUI2026/Sprites/'+a.id+suffix;fs.copyFileSync(path.join(preview,relative),path.join(root,relative));copies++;}
for(const id of ['top-body-v15-fixed','top-body-v15-moving'])for(const suffix of ['.mat','.mat.meta']){const relative='Assets/RoyalAves/Art/RoyalPostmanUI2026/Materials/'+id+suffix;fs.copyFileSync(path.join(preview,relative),path.join(root,relative));}
for(const name of top){if(name==='Home')continue;const relative='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/'+name+'.prefab';if(fs.existsSync(path.join(root,relative)))fs.copyFileSync(path.join(preview,relative),path.join(root,relative));}
fs.writeFileSync(target,to.blocks.join(''));console.log('Applied '+changed+' top-HUD component changes; all other authored Home blocks unchanged.');
fs.writeFileSync(path.join(__dirname,'TOP-HUD-V15-INSTALL.json'),JSON.stringify({changedComponents:changed,scope:[...top],untouchedOtherHomeBlocks:true,sourceNative4K:false,backup:'Library/MapValidation/AuthoringBackup/Home-before-top-v15.prefab'},null,2));

