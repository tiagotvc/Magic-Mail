const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');process.chdir(path.resolve(__dirname,'../..'));
const file='Tools/level-map/ribbon-rook-v71-manifest.json';const m=JSON.parse(fs.readFileSync(file,'utf8'));
m.assets.silhouette=JSON.parse(fs.readFileSync('Tools/level-map/ribbon-rook-silhouette-v71-manifest.json','utf8'));
const folder='Assets/RoyalAves/Art/MapKitV3/RibbonRookV71';fs.mkdirSync(folder,{recursive:true});const guid=()=>crypto.randomBytes(16).toString('hex');
if(!fs.existsSync(folder+'.meta'))fs.writeFileSync(folder+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
const names={backdrop:'christmas-shadow-sky-v71',character:'ribbon-rook-gift-thief-color-v71',silhouette:'ribbon-rook-gift-thief-shadow-v71'};
if(m.assets.backdrop.finalRefinement)names.backdrop='christmas-shadow-sky-v72';
const template=fs.readFileSync('Assets/RoyalAves/Art/MapKitV3/RiverPropsV67/river-stone-left-glossy-v67.png.meta','utf8');
for(const [key,a] of Object.entries(m.assets)){
 a.file=`${folder}/${names[key]}.png`;fs.copyFileSync(a.source,a.file);
 a.guid=fs.existsSync(a.file+'.meta')?fs.readFileSync(a.file+'.meta','utf8').match(/^guid: (\w+)/m)[1]:guid();
 fs.writeFileSync(a.file+'.meta',template.replace(/^guid: \w+/m,`guid: ${a.guid}`));
}
const reference=key=>`{fileID: 21300000, guid: ${m.assets[key].guid}, type: 3}`;
const asset='Assets/RoyalAves/Resources/RoyalAvesWorldMapIslands.asset';let data=fs.readFileSync(asset,'utf8');
const beforeIsland=data.split(/(?=^  - id:)/m)[1];
function set(key,value){const re=new RegExp(`^  ${key}:.*$`,'m');if(re.test(data))data=data.replace(re,`  ${key}: ${value}`);else data=data.replace('  christmasSky:',`  ${key}: ${value}\n  christmasSky:`);}
set('staticSky',1);set('christmasBackdrop',reference('backdrop'));set('christmasVillain',reference('silhouette'));set('christmasVillainCharacter',reference('character'));
set('villainMapSize',1.55);set('villainPosition','{x: 0.45, y: 0.36}');set('villainSize',220);set('villainOpacity',.90);set('villainHop',0);
const marker=data.indexOf('  islands:');let head=data.slice(0,marker),tail=data.slice(marker);
const kit=fs.readFileSync('Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset','utf8');const friendly=kit.match(/^  santaFriendly: (.+)$/m)[1];
head=head.split(/(?=^  - title:)/m).map(chunk=>{
 if(/^  - title: Christmas Dispatch/m.test(chunk))return chunk.replace(/^    story:[\s\S]*?(?=^    treeSize:)/m,`    story:\n    - ${friendly}\n    - ${reference('character')}\n`);
 if(/^  - title: Christmas Ribbon Rescue/m.test(chunk))return chunk.replace(/^    story:[\s\S]*?(?=^    treeSize:)/m,`    story:\n    - ${reference('character')}\n    - ${friendly}\n`);
 return chunk;
}).join('');data=head+tail;
if(data.split(/(?=^  - id:)/m)[1]!==beforeIsland)throw new Error('Authored first island terrain changed');fs.writeFileSync(asset,data);
const row='Assets/RoyalAves/Resources/MapLibrary/Personagens/ribbon-rook-gift-thief-color-v71.asset';
let library=fs.readFileSync('Assets/RoyalAves/Resources/MapLibrary/Props/river-stone-left-glossy-v67.asset','utf8');
library=library.replace('m_Name: river-stone-left-glossy-v67','m_Name: ribbon-rook-gift-thief-color-v71').replace('title: "Glossy river stone left"','title: "Ribbon Rook — Christmas gift thief"').replace('category: 2','category: 4').replace('guid: 9cf128d00c3d686f23b778731b565eea',`guid: ${m.assets.character.guid}`).replace('kind: 15','kind: 8').replace('size: 1.12','size: 1.55');
fs.writeFileSync(row,library);if(!fs.existsSync(row+'.meta'))fs.writeFileSync(row+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
m.integration='Static shadow-only Christmas skyline; no sky clouds, meteors, weather particles or animated star/moon. Larger stationary villain shadow. Full-color original character automatically placed on Christmas islands with an unhidden full body; editable from map library and shared recipes. Authored island 1 terrain and placement data retained.';
fs.writeFileSync(file,JSON.stringify(m,null,2)+'\n');console.log('Installed shadow panorama, matching stationary silhouette and full-color villain.');
