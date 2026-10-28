const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..');process.chdir(root);
const manifestPath='Tools/level-map/lava-v68-manifest.json';
const m=JSON.parse(fs.readFileSync(manifestPath,'utf8'));
const folder='Assets/RoyalAves/Art/MapKitV3/LavaV68';fs.mkdirSync(folder,{recursive:true});
const guid=()=>crypto.randomBytes(16).toString('hex');
if(!fs.existsSync(folder+'.meta'))fs.writeFileSync(folder+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
const template=fs.readFileSync('Assets/RoyalAves/Art/MapKitV3/RiverPropsV67/river-stone-left-glossy-v67.png.meta','utf8');
const specs={lavaStoneLeft:['lava-stone-left-glossy-v68','Volcanic stepping stone left',15,1.12],lavaStoneRight:['lava-stone-right-glossy-v68','Volcanic stepping stone right',15,1.12],lavaSurface:['lava-surface-glossy-v68','Molten lava surface',9,1],lavaFall:['lava-fall-glossy-v68','Glossy lava cascade',17,1.7]};
let resource=fs.readFileSync('Assets/RoyalAves/Resources/RoyalAvesWorldMapIslands.asset','utf8');
for(const [key,a] of Object.entries(m.assets)){
 const [name,title,kind,size]=specs[key];a.file=`${folder}/${name}.png`;
 fs.copyFileSync(a.source,a.file);const metaFile=a.file+'.meta';
 a.guid=fs.existsSync(metaFile)?fs.readFileSync(metaFile,'utf8').match(/^guid: (\w+)/m)[1]:guid();
 let meta=template.replace(/^guid: \w+/m,`guid: ${a.guid}`);
 if(key==='lavaSurface')meta=meta.replace('wrapU: 1','wrapU: 0').replace('wrapV: 1','wrapV: 0');
 fs.writeFileSync(metaFile,meta);
 const reference=`  ${key}: {fileID: 21300000, guid: ${a.guid}, type: 3}`;
 if(resource.includes(`  ${key}:`))resource=resource.replace(new RegExp(`^  ${key}:.*$`,'m'),reference);
 else resource=resource.replace('  levelsPerIsland:',reference+'\n  levelsPerIsland:');
 const category=kind===17?'Efeitos':'Props';const target=`Assets/RoyalAves/Resources/MapLibrary/${category}/${name}.asset`;
 let library=fs.readFileSync('Assets/RoyalAves/Resources/MapLibrary/Props/river-stone-left-glossy-v67.asset','utf8');
 library=library.replace('m_Name: river-stone-left-glossy-v67',`m_Name: ${name}`).replace('title: "Glossy river stone left"',`title: "${title}"`).replace('guid: 9cf128d00c3d686f23b778731b565eea',`guid: ${a.guid}`).replace('kind: 15',`kind: ${kind}`).replace('size: 1.12',`size: ${size}`);
 fs.writeFileSync(target,library);if(!fs.existsSync(target+'.meta'))fs.writeFileSync(target+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
}
fs.writeFileSync('Assets/RoyalAves/Resources/RoyalAvesWorldMapIslands.asset',resource);fs.writeFileSync(manifestPath,JSON.stringify(m,null,2)+'\n');
console.log('Installed 4 lava assets and library entries.');
