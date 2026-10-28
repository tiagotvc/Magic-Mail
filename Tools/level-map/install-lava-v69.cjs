const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
process.chdir(path.resolve(__dirname,'../..'));
const file='Tools/level-map/lava-v69-manifest.json';const m=JSON.parse(fs.readFileSync(file,'utf8'));
const previous=JSON.parse(fs.readFileSync('Tools/level-map/lava-v68-manifest.json','utf8'));
const folder='Assets/RoyalAves/Art/MapKitV3/LavaV69';fs.mkdirSync(folder,{recursive:true});
const guid=()=>crypto.randomBytes(16).toString('hex');
if(!fs.existsSync(folder+'.meta'))fs.writeFileSync(folder+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
let resource=fs.readFileSync('Assets/RoyalAves/Resources/RoyalAvesWorldMapIslands.asset','utf8');
for(const key of ['lavaStoneLeft','lavaStoneRight','lavaSurface']){
 const a=m.assets[key];
 const old=previous.assets[key];a.file=old.file.replace('/LavaV68/','/LavaV69/').replace('-v68.png','-v69.png');
 fs.copyFileSync(a.source,a.file);
 a.guid=fs.existsSync(a.file+'.meta')?fs.readFileSync(a.file+'.meta','utf8').match(/^guid: (\w+)/m)[1]:guid();
 fs.writeFileSync(a.file+'.meta',fs.readFileSync(old.file+'.meta','utf8').replace(/^guid: \w+/m,`guid: ${a.guid}`));
 resource=resource.replace(new RegExp(`^  ${key}:.*$`,'m'),`  ${key}: {fileID: 21300000, guid: ${a.guid}, type: 3}`);
 const name=path.basename(old.file,'.png');const library=`Assets/RoyalAves/Resources/MapLibrary/Props/${name}.asset`;
 fs.writeFileSync(library,fs.readFileSync(library,'utf8').replace(old.guid,a.guid));
}
// Keep a default cascade out at the side, clear of the level platforms. Custom poses are preserved.
resource=resource.split(/(?=^  - id:)/m).map(s=>/^    liquid: 3$/m.test(s)?s.replace('    waterfallLateral: 1.95','    waterfallLateral: 2.45'):s).join('');
fs.writeFileSync('Assets/RoyalAves/Resources/RoyalAvesWorldMapIslands.asset',resource);
m.assets.lavaFall=previous.assets.lavaFall;
m.bridge=JSON.parse(fs.readFileSync('Tools/level-map/bridge-v68-manifest.json','utf8'));
fs.writeFileSync(file,JSON.stringify(m,null,2)+'\n');console.log('Installed selected glossy lava art.');
