const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');process.chdir(path.resolve(__dirname,'../..'));
const file='Tools/level-map/mist-v79-manifest.json',m=JSON.parse(fs.readFileSync(file,'utf8'));
const folder='Assets/RoyalAves/Art/MapKitV3/MistV79',guid=()=>crypto.randomBytes(16).toString('hex');fs.mkdirSync(folder,{recursive:true});
if(!fs.existsSync(folder+'.meta'))fs.writeFileSync(folder+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
const template=fs.readFileSync('Assets/RoyalAves/Art/MapKitV3/GlossyBuildingsV74/christmas-cottage-glossy-v74.png.meta','utf8'),itemTemplate=fs.readFileSync('Assets/RoyalAves/Resources/MapLibrary/Efeitos/geiser.asset','utf8');
for(const a of m.assets){
 a.file=`${folder}/${a.name}.png`;fs.copyFileSync(a.source,a.file);
 a.guid=fs.existsSync(a.file+'.meta')?fs.readFileSync(a.file+'.meta','utf8').match(/^guid: (\w+)/m)[1]:guid();
 const meta=fs.existsSync(a.file+'.meta')?fs.readFileSync(a.file+'.meta','utf8'):template.replace(/^guid: \w+/m,`guid: ${a.guid}`).replace(/maxTextureSize: \d+/g,'maxTextureSize: 1024').replace('enableMipMap: 0','enableMipMap: 1').replace('spriteMeshType: 1','spriteMeshType: 0');
 fs.writeFileSync(a.file+'.meta',meta.replace('mipMapsPreserveCoverage: 1','mipMapsPreserveCoverage: 0'));
 a.library=`Assets/RoyalAves/Resources/MapLibrary/Efeitos/${a.name}.asset`;
 fs.writeFileSync(a.library,itemTemplate.replace(/^  m_Name:.*$/m,`  m_Name: ${a.name}`).replace(/^  title:.*$/m,`  title: ${JSON.stringify(a.title)}`).replace(/^  sprite:.*$/m,`  sprite: {fileID: 21300000, guid: ${a.guid}, type: 3}`).replace(/^  kind:.*$/m,'  kind: 18').replace(/^  size:.*$/m,'  size: 0.65'));
 if(!fs.existsSync(a.library+'.meta'))fs.writeFileSync(a.library+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
}
const config='Assets/RoyalAves/Resources/RoyalAvesWorldMapIslands.asset',data=fs.readFileSync(config,'utf8'),split=data.indexOf('  islands:');let head=data.slice(0,split);
const ref=key=>m.assets.find(a=>a.key===key).guid;
for(const [title,colors,opacity] of [['Letter Gardens',['pale'],.72],['Ink Springs',['violet','teal'],.64],['Melody Lagoon',['pale','teal'],.64],['Blossom Post',['pale','violet'],.66],['Ember Stampworks',['teal','violet'],.70],['Christmas Dispatch',['pale'],.74],['Christmas Ribbon Rescue',['pale','violet'],.64]]){
 const start=head.indexOf('  - title: '+title+'\n');if(start<0)throw Error('Missing shared recipe '+title);
 const end=head.indexOf('  - title:',start+1);let block=head.slice(start,end<0?head.length:end);
 block=block.replace(/^    mist:[\s\S]*?(?=^    [a-zA-Z])/m,'').replace(/^    mistSize:.*\n/m,'').replace(/^    mistOpacity:.*\n/m,'');
 const fields='    mist:\n'+colors.map(key=>`    - {fileID: 21300000, guid: ${ref(key)}, type: 3}\n`).join('')+`    mistSize: 0.65\n    mistOpacity: ${opacity}\n`;
 block=block.replace(/^    treeSize:/m,fields+'    treeSize:');head=head.slice(0,start)+block+(end<0?'':head.slice(end));
}
fs.writeFileSync(config,head+data.slice(split));m.usage='Three translucent static lateral mist ribbons in Effects library and shared automatic island recipes. Mist enum appended to preserve saved kind values. Soft alpha blend, no shadow, no movement/pulse, editable opacity. First island and saved layouts retained.';fs.writeFileSync(file,JSON.stringify(m,null,2)+'\n');console.log('Installed three mist variants in Effects library and shared island recipes.');
