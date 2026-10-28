const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');process.chdir(path.resolve(__dirname,'../..'));
const file='Tools/level-map/ink-puddles-v78-manifest.json',m=JSON.parse(fs.readFileSync(file,'utf8'));
const folder='Assets/RoyalAves/Art/MapKitV3/InkPuddlesV78',guid=()=>crypto.randomBytes(16).toString('hex');fs.mkdirSync(folder,{recursive:true});
if(!fs.existsSync(folder+'.meta'))fs.writeFileSync(folder+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
const template=fs.readFileSync('Assets/RoyalAves/Art/MapKitV3/GlossyBuildingsV74/christmas-cottage-glossy-v74.png.meta','utf8');
const itemTemplate=fs.readFileSync('Assets/RoyalAves/Resources/MapLibrary/Props/ink-puddle-overhead-v70.asset','utf8');
for(const a of m.assets){
 a.file=`${folder}/${a.name}.png`;fs.copyFileSync(a.source,a.file);
 a.guid=fs.existsSync(a.file+'.meta')?fs.readFileSync(a.file+'.meta','utf8').match(/^guid: (\w+)/m)[1]:guid();
 if(!fs.existsSync(a.file+'.meta'))fs.writeFileSync(a.file+'.meta',template.replace(/^guid: \w+/m,`guid: ${a.guid}`).replace(/maxTextureSize: \d+/g,'maxTextureSize: 512').replace('enableMipMap: 0','enableMipMap: 1'));
 a.library=`Assets/RoyalAves/Resources/MapLibrary/Props/${a.name}.asset`;
 fs.writeFileSync(a.library,itemTemplate.replace(/^  m_Name:.*$/m,`  m_Name: ${a.name}`).replace(/^  title:.*$/m,`  title: ${JSON.stringify(a.title)}`).replace(/^  sprite:.*$/m,`  sprite: {fileID: 21300000, guid: ${a.guid}, type: 3}`).replace(/^  size:.*$/m,'  size: 0.88'));
 if(!fs.existsSync(a.library+'.meta'))fs.writeFileSync(a.library+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
}
const config='Assets/RoyalAves/Resources/RoyalAvesWorldMapIslands.asset',data=fs.readFileSync(config,'utf8'),split=data.indexOf('  islands:');let head=data.slice(0,split);
const rocks='9c156696cd5943cfbd8b24560b80d35e',ref=key=>m.assets.find(a=>a.key===key).guid;
for(const [title,colors] of Object.entries({'Letter Gardens':['blue'],'Ink Springs':['purple','blue','teal'],'Melody Lagoon':['teal'],'Blossom Post':['purple','blue']})){
 const start=head.indexOf('  - title: '+title+'\n');if(start<0)throw Error('Missing shared recipe '+title);
 const end=head.indexOf('  - title:',start+1);let block=head.slice(start,end<0?head.length:end);
 const entries=colors.map(ref);entries.push(rocks);
 block=block.replace(/^    groundProps:[\s\S]*?(?=^    [a-zA-Z])/m,'    groundProps:\n'+entries.map(g=>`    - {fileID: 21300000, guid: ${g}, type: 3}\n`).join(''));
 head=head.slice(0,start)+block+(end<0?'':head.slice(end));
}
fs.writeFileSync(config,head+data.slice(split));
m.usage='Three overhead glossy ink puddles in Props library. Normal island recipes select matching colors automatically. Authored first-island data and saved placements unchanged; old artwork retained for existing authored uses.';
fs.writeFileSync(file,JSON.stringify(m,null,2)+'\n');console.log('Installed blue, violet and turquoise overhead ink puddles in library and shared normal-island recipes.');
