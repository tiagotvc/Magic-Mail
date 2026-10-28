const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),m=JSON.parse(fs.readFileSync(path.join(__dirname,'pig-layers-v101-manifest.json'),'utf8'));
const guid=id=>crypto.createHash('md5').update('magic-mail/pig-layers-v101/'+id).digest('hex');
const template=fs.readFileSync(path.join(root,'Assets/RoyalAves/Resources/MapEventsV98/postal-pig-badge.png.meta'),'utf8');
for(const a of m.assets){const target=path.join(root,a.output),folder=path.dirname(target);fs.mkdirSync(folder,{recursive:true});fs.copyFileSync(a.source,target);
 if(!fs.existsSync(folder+'.meta'))fs.writeFileSync(folder+'.meta',`fileFormatVersion: 2\nguid: ${guid('folder')}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
 if(!fs.existsSync(target+'.meta'))fs.writeFileSync(target+'.meta',template.replace(/^guid: .+$/m,'guid: '+guid(a.id)).replace(/spriteID: [a-f0-9]+/g,'spriteID: '+guid(a.id+'/sprite')).replaceAll('maxTextureSize: 512','maxTextureSize: '+a.maxSize).replace(/userData: .*/,'userData: Pig bank V101 separate sprite - '+a.id));
}
console.log('Installed '+m.assets.length+' original transparent layer sprites. Production shortcut unchanged pending selection.');
