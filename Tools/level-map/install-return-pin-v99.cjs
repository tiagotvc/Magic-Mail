const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),m=JSON.parse(fs.readFileSync(path.join(__dirname,'return-pin-v99-manifest.json'),'utf8'));
const guid=id=>crypto.createHash('md5').update('magic-mail/return-pin-v99/'+id).digest('hex');
const target=path.join(root,m.output),folder=path.dirname(target);fs.mkdirSync(folder,{recursive:true});fs.copyFileSync(m.source,target);
if(!fs.existsSync(folder+'.meta'))fs.writeFileSync(folder+'.meta',`fileFormatVersion: 2\nguid: ${guid('folder')}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
if(!fs.existsSync(target+'.meta')){let meta=fs.readFileSync(path.join(root,'Assets/RoyalAves/Resources/MapEventsV98/postal-pig-badge.png.meta'),'utf8').replace(/^guid: .+$/m,'guid: '+guid('sprite')).replace(/spriteID: [a-f0-9]+/g,'spriteID: '+guid('sprite-id')).replace(/userData: .*/,'userData: Glossy return pin V99 - separate live avatar');fs.writeFileSync(target+'.meta',meta);}
const png=fs.readFileSync(target);console.log('Installed original transparent return pin:',png.readUInt32BE(16),png.readUInt32BE(20));
