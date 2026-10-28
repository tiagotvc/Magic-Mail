const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');process.chdir(path.resolve(__dirname,'../..'));
const file='Tools/level-map/level-rewards-v77-manifest.json',m=JSON.parse(fs.readFileSync(file,'utf8'));
const folder='Assets/RoyalAves/Art/MapKitV3/LevelRewardsV77',guid=()=>crypto.randomBytes(16).toString('hex');fs.mkdirSync(folder,{recursive:true});
if(!fs.existsSync(folder+'.meta'))fs.writeFileSync(folder+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
const template=fs.readFileSync('Assets/RoyalAves/Art/MapKitV3/GlossyBuildingsV74/christmas-cottage-glossy-v74.png.meta','utf8');
for(const a of m.assets){a.file=`${folder}/${a.name}.png`;fs.copyFileSync(a.source,a.file);
 a.guid=fs.existsSync(a.file+'.meta')?fs.readFileSync(a.file+'.meta','utf8').match(/^guid: (\w+)/m)[1]:guid();
 if(!fs.existsSync(a.file+'.meta'))fs.writeFileSync(a.file+'.meta',template.replace(/^guid: \w+/m,`guid: ${a.guid}`).replace(/maxTextureSize: \d+/g,'maxTextureSize: 512').replace('enableMipMap: 0','enableMipMap: 1'));
}
const ref=key=>`{fileID: 21300000, guid: ${m.assets.find(a=>a.key===key).guid}, type: 3}`;
const config='Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset';let data=fs.readFileSync(config,'utf8');
for(const [key,asset] of Object.entries({levelCrown:'crown',simpleCrown:'crown',levelStar:'star',simpleStar:'star',rewardStarGold:'star',rewardStarEmpty:'empty'})){
 const re=new RegExp(`^  ${key}:.*$`,'m');data=re.test(data)?data.replace(re,`  ${key}: ${ref(asset)}`):data.replace(/^  levelStar:.*$/m,line=>line+`\n  ${key}: ${ref(asset)}`);
}
fs.writeFileSync(config,data);fs.writeFileSync(file,JSON.stringify(m,null,2)+'\n');console.log('Installed glossy crown and matched gold/silver stars with cream support in shared map kit.');
