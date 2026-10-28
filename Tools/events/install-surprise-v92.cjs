const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..');
const manifest=JSON.parse(fs.readFileSync(path.join(__dirname,'events-surprise-v92-manifest.json'),'utf8'));
const guid=()=>crypto.randomBytes(16).toString('hex');
for(const asset of manifest.assets){
 const target=path.join(root,asset.output),folder=path.dirname(target),letter=asset.id==='letter';
 fs.mkdirSync(folder,{recursive:true});fs.copyFileSync(asset.source,target);
 if(!fs.existsSync(folder+'.meta'))fs.writeFileSync(folder+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
 if(!fs.existsSync(target+'.meta')){
  const base=fs.readFileSync(path.join(root,'Assets/RoyalAves/Resources/EventsV88/backstage.png.meta'),'utf8');
  fs.writeFileSync(target+'.meta',base.replace(/^guid: .+$/m,'guid: '+guid()).replaceAll('2048',letter?'512':'2048').replace('alphaIsTransparency: 0','alphaIsTransparency: '+(letter?'1':'0')).replace(/userData: .*/,`userData: Events V92 ${asset.id} - cartoon surprise`));
 }
}
console.log('Installed V92 scene and separate transparent animated letter; originals preserved.');
