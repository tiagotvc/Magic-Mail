const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),manifest=require('./santa-v30.json');
const template=fs.readFileSync(path.join(root,'Assets/RoyalAves/Art/MapKitV3/Library/Arvores/green-rounded-tree-v27.png.meta'),'utf8');
const itemTemplate=fs.readFileSync(path.join(root,'Assets/RoyalAves/Resources/MapLibrary/Personagens/carteiro.asset'),'utf8');
const results=[];
for(const job of manifest.jobs){
 const name=path.basename(job.file,'.png'),target=path.join(root,'Assets/RoyalAves/Art/MapKitV3/Library/Personagens',job.file);
 const guid=crypto.createHash('md5').update('MagicMail:santa-v30:'+job.id).digest('hex');
 if(fs.existsSync(target)&&!fs.readFileSync(target).equals(fs.readFileSync(job.source)))throw Error('Refusing overwrite: '+target);
 fs.writeFileSync(target+'.meta',template.replace(/^guid: .+$/m,'guid: '+guid).replaceAll('green-rounded-tree-v27',name).replace(/isReadable: \d+/,'isReadable: 1'));
 fs.copyFileSync(job.source,target);
 const item=path.join(root,'Assets/RoyalAves/Resources/MapLibrary/Personagens','arte-'+name+'.asset');
 fs.writeFileSync(item,itemTemplate.replace(/^  m_Name: .+$/m,'  m_Name: arte-'+name).replace(/^  title: .+$/m,'  title: '+job.title).replace('guid: 5dc57dd7f31049a9cc5154623886038a','guid: '+guid).replace(/^  size: .+$/m,'  size: 1.35'));
 fs.writeFileSync(item+'.meta','fileFormatVersion: 2\nguid: '+crypto.createHash('md5').update('MagicMail:santa-v30:item:'+job.id).digest('hex')+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n');
 results.push({id:job.id,guid,file:path.relative(root,target),sha256:crypto.createHash('sha256').update(fs.readFileSync(target)).digest('hex')});
}
const kitPath=path.join(root,'Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset');let kit=fs.readFileSync(kitPath,'utf8');
for(const [i,field] of ['santaFriendly','santaVillain'].entries()){
 const line='  '+field+': {fileID: 21300000, guid: '+results[i].guid+', type: 3}';
 const pattern=new RegExp('^  '+field+':.*$','m');kit=pattern.test(kit)?kit.replace(pattern,line):kit.trimEnd()+'\n'+line+'\n';
}
fs.writeFileSync(kitPath,kit);console.log(JSON.stringify(results));
