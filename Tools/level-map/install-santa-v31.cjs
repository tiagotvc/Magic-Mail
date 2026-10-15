// Preserve generated alpha and originals; repoint the existing catalog entries to frontal event art.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),manifest=require('./santa-v31.json');
const template=fs.readFileSync(path.join(root,'Assets/RoyalAves/Art/MapKitV3/Library/Arvores/green-rounded-tree-v27.png.meta'),'utf8');
let kit=fs.readFileSync(path.join(root,'Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset'),'utf8');
for(const job of manifest.jobs){
 const name=path.basename(job.file,'.png'),target=path.join(root,'Assets/RoyalAves/Art/MapKitV3/Library/Personagens',job.file);
 const guid=crypto.createHash('md5').update('MagicMail:santa-v31:'+job.id).digest('hex');
 if(fs.existsSync(target)&&!fs.readFileSync(target).equals(fs.readFileSync(job.source)))throw Error('Refusing overwrite '+target);
 fs.copyFileSync(job.source,target);
 fs.writeFileSync(target+'.meta',template.replace(/^guid: .+$/m,'guid: '+guid).replaceAll('green-rounded-tree-v27',name).replace(/isReadable: \d+/,'isReadable: 1'));
 const item=path.join(root,'Assets/RoyalAves/Resources/MapLibrary/Personagens/arte-'+job.id+'-v30.asset');
 let record=fs.readFileSync(item,'utf8').replace(/^  sprite:.*$/m,'  sprite: {fileID: 21300000, guid: '+guid+', type: 3}').replace(/^  size:.*$/m,'  size: 0.8');
 fs.writeFileSync(item,record);
 kit=kit.replace(new RegExp('^  '+job.binding+':.*$','m'),'  '+job.binding+': {fileID: 21300000, guid: '+guid+', type: 3}');
 console.log(JSON.stringify({file:job.file,guid,sha256:crypto.createHash('sha256').update(fs.readFileSync(target)).digest('hex')}));
}
fs.writeFileSync(path.join(root,'Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset'),kit);
