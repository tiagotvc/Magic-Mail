const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),manifest=JSON.parse(fs.readFileSync(path.join(__dirname,'pig-badge-v98-manifest.json'),'utf8'));
const guid=id=>crypto.createHash('md5').update('magic-mail/pig-badge-v98/'+id).digest('hex');
const target=path.join(root,manifest.output),folder=path.dirname(target);fs.mkdirSync(folder,{recursive:true});fs.copyFileSync(manifest.source,target);
if(!fs.existsSync(folder+'.meta'))fs.writeFileSync(folder+'.meta',`fileFormatVersion: 2\nguid: ${guid('folder')}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
if(!fs.existsSync(target+'.meta')){
 let meta=fs.readFileSync(path.join(root,'Assets/RoyalAves/Resources/MapEventsV93/postal-pig.png.meta'),'utf8').replace(/^guid: .+$/m,'guid: '+guid('sprite')).replace(/spriteID: [a-f0-9]+/g,'spriteID: '+guid('sprite-id')).replace(/userData: .*/,'userData: Postal pig badge V98 - Christmas red body and emerald container; empty slot');
 fs.writeFileSync(target+'.meta',meta);
}
console.log('Installed simple pig badge with container; original generation and older designs preserved.');
