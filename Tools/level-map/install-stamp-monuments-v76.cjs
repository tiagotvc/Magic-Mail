const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');process.chdir(path.resolve(__dirname,'../..'));
const file='Tools/level-map/stamp-monuments-v76-manifest.json',m=JSON.parse(fs.readFileSync(file,'utf8'));
const folder='Assets/RoyalAves/Art/MapKitV3/StampMonumentsV76',guid=()=>crypto.randomBytes(16).toString('hex');fs.mkdirSync(folder,{recursive:true});
if(!fs.existsSync(folder+'.meta'))fs.writeFileSync(folder+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
const textureTemplate=fs.readFileSync('Assets/RoyalAves/Art/MapKitV3/GlossyBuildingsV74/christmas-cottage-glossy-v74.png.meta','utf8');
const libraryTemplate=fs.readFileSync('Assets/RoyalAves/Resources/MapLibrary/Props/royal-stamp-v52.asset','utf8');
for(const a of m.assets){
 a.file=`${folder}/${a.name}.png`;fs.copyFileSync(a.source,a.file);
 a.guid=fs.existsSync(a.file+'.meta')?fs.readFileSync(a.file+'.meta','utf8').match(/^guid: (\w+)/m)[1]:guid();
 if(!fs.existsSync(a.file+'.meta'))fs.writeFileSync(a.file+'.meta',textureTemplate.replace(/^guid: \w+/m,`guid: ${a.guid}`));
 a.library=`Assets/RoyalAves/Resources/MapLibrary/Props/${a.name}.asset`;
 const item=libraryTemplate.replace(/^  m_Name:.*$/m,`  m_Name: ${a.name}`).replace(/^  title:.*$/m,`  title: ${JSON.stringify(a.title)}`).replace(/^  sprite:.*$/m,`  sprite: {fileID: 21300000, guid: ${a.guid}, type: 3}`).replace(/^  kind:.*$/m,'  kind: 2').replace(/^  size:.*$/m,`  size: ${a.size}`);
 fs.writeFileSync(a.library,item);if(!fs.existsSync(a.library+'.meta'))fs.writeFileSync(a.library+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
}
const config='Assets/RoyalAves/Resources/RoyalAvesWorldMapIslands.asset';let data=fs.readFileSync(config,'utf8');
const split=data.indexOf('  islands:'),tail=data.slice(split);let head=data.slice(0,split);
const models={'Letter Gardens':'royal','Ink Springs':'curved','Melody Lagoon':'curved','Blossom Post':'royal','Ember Stampworks':'royal','Christmas Dispatch':'christmas','Christmas Ribbon Rescue':'christmas'};
for(const [title,key] of Object.entries(models)){
 const start=head.indexOf('  - title: '+title+'\n');if(start<0)throw Error('Missing shared recipe '+title);
 const end=head.indexOf('  - title:',start+1);let block=head.slice(start,end<0?head.length:end);
 block=block.replace(/^    monuments:[\s\S]*?(?=^    [a-zA-Z])/m,'').replace(/^    monumentSize:.*\n/m,'');
 const a=m.assets.find(a=>a.key===key);block=block.replace(/^    treeSize:/m,`    monuments:\n    - {fileID: 21300000, guid: ${a.guid}, type: 3}\n    monumentSize: ${a.size}\n    treeSize:`);
 head=head.slice(0,start)+block+(end<0?'':head.slice(end));
}
fs.writeFileSync(config,head+tail);m.usage='Frontal glossy stamp monuments in Props library and shared automatic island recipes. Royal/curved stamps on normal recipes, Christmas stamp on seasonal recipes. Authored island settings and saved layouts untouched.';
fs.writeFileSync(file,JSON.stringify(m,null,2)+'\n');console.log('Installed three frontal glossy stamp monuments in Props library and shared recipes. Authored island settings/layouts retained.');
