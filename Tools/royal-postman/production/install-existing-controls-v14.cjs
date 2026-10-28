// Updates only button artwork/leaf glyph transforms. Preserves authored Home layout.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=path.resolve(__dirname,'../../..');
const home='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Home.prefab';
const source=path.join(root,'Library/MapValidation/PreviewProject',home),target=path.join(root,home);
function parse(text){
 const blocks=text.split(/(?=^--- !u!)/m),objects=new Map(),rects=new Map(),images=[];
 for(let index=0;index<blocks.length;index++){
  const block=blocks[index],header=block.match(/^--- !u!(\d+) &(-?\d+)/);if(!header)continue;
  const entry={index,type:+header[1],id:header[2],block};
  if(entry.type===1)objects.set(entry.id,block.match(/^  m_Name: (.*)$/m)?.[1]);
  if(entry.type===224||entry.type===4){entry.go=block.match(/m_GameObject: \{fileID: (-?\d+)\}/)?.[1];entry.parent=block.match(/m_Father: \{fileID: (-?\d+)\}/)?.[1];rects.set(entry.id,entry);}
  if(entry.type===114&&/^  m_Sprite:/m.test(block)){entry.go=block.match(/m_GameObject: \{fileID: (-?\d+)\}/)?.[1];images.push(entry);}
 }
 const paths=new Map(),rectPaths=new Map(),imagePaths=new Map();
 function getPath(rect){if(paths.has(rect.go))return paths.get(rect.go);const parent=rects.get(rect.parent);const result=parent?[getPath(parent),objects.get(rect.go)].filter(Boolean).join('/'):'';paths.set(rect.go,result);return result;}
 for(const rect of rects.values())rectPaths.set(getPath(rect),rect);
 for(const image of images)imagePaths.set(paths.get(image.go),image);
 return {blocks,rectPaths,imagePaths};
}
const original=fs.readFileSync(target,'utf8'),from=parse(fs.readFileSync(source,'utf8')),to=parse(original);
const artwork=[];
for(const button of ['CoinPlus','LifePlus','SettingsButton'])for(const child of ['FixedShoulder','MovingCenter/Face',button==='SettingsButton'?'MovingCenter/Gear':'MovingCenter/PlusGlyph'])artwork.push(button+'/'+child);
function copyLine(dst,src,key){const regex=new RegExp('^  '+key+':.*$','m'),line=src.block.match(regex)?.[0];if(!line||!regex.test(dst.block))throw new Error('Missing '+key);dst.block=dst.block.replace(regex,line);to.blocks[dst.index]=dst.block;}
for(const name of artwork){const dst=to.imagePaths.get(name),src=from.imagePaths.get(name);if(!dst||!src)throw new Error('Missing Image '+name);for(const key of ['m_Material','m_Sprite'])copyLine(dst,src,key);}
for(const button of ['CoinPlus','LifePlus','SettingsButton']){
 const leaf=button+'/MovingCenter/'+(button==='SettingsButton'?'Gear':'PlusGlyph'),dst=to.rectPaths.get(leaf),src=from.rectPaths.get(leaf);
 if(!dst||!src)throw new Error('Missing RectTransform '+leaf);
 copyLine(dst,src,'m_SizeDelta');if(button!=='SettingsButton')copyLine(dst,src,'m_LocalRotation');
}
const changed=new Set([...artwork.map(name=>to.imagePaths.get(name).index),...['CoinPlus/MovingCenter/PlusGlyph','LifePlus/MovingCenter/PlusGlyph','SettingsButton/MovingCenter/Gear'].map(name=>to.rectPaths.get(name).index)]);
const before=parse(original);for(let i=0;i<to.blocks.length;i++)if(!changed.has(i)&&to.blocks[i]!==before.blocks[i])throw new Error('Unexpected layout change in block '+i);
const backup=path.join(root,'Library/MapValidation/AuthoringBackup');fs.mkdirSync(backup,{recursive:true});fs.writeFileSync(path.join(backup,'Home-before-existing-controls.prefab'),original);
fs.writeFileSync(target,to.blocks.join(''));
console.log('UPDATED '+artwork.length+' Image components and 3 glyph RectTransforms; all other Home blocks preserved.');
for(const [a,b] of [['UI/Buttons/fechar.png','RoyalPostmanUI2026/top-round-existing-v14.png'],['UI/Buttons/fechar.png','RoyalPostmanUI2026/top-plus-symbol-existing-v14.png'],['UI/Buttons/engrenagem-plana.png','RoyalPostmanUI2026/top-gear-existing-v14.png']]){
 const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,'Assets/RoyalAves/Art',file))).digest('hex');if(hash(a)!==hash(b))throw new Error('Source differs: '+b);
}
console.log('PASS source PNGs unchanged; manual Home layout preserved.');
