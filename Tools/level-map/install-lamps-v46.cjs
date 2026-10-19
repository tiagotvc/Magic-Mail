const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..');
const items=[['lamp-straight-glossy-v46','Poste reto glossy'],['lamp-dark-forest-v47','Luz da floresta da escuridão'],['lamp-volcano-v47','Luz vulcânica']];
const hash=s=>crypto.createHash('md5').update(s).digest('hex');
for(const [name,title] of items){
  const png=`Assets/RoyalAves/Art/MapKitV3/Library/Props/${name}.png`,item=`Assets/RoyalAves/Resources/MapLibrary/Props/${name}.asset`,guid=hash('royalaves/'+name);
  if(!fs.existsSync(path.join(root,png)))throw Error('Missing '+png);
  if(!fs.existsSync(path.join(root,png+'.meta'))){
    let meta=fs.readFileSync(path.join(root,'Assets/RoyalAves/Art/MapKitV3/Library/Props/mailbox-glossy-v44.png.meta'),'utf8');
    meta=meta.replace(/^guid: .+$/m,'guid: '+guid).replaceAll('mailbox-glossy-v44',name).replace(/spriteID: [a-f0-9]+/g,'spriteID: '+hash(name+'/sprite'));
    fs.writeFileSync(path.join(root,png+'.meta'),meta);
  }
  if(!fs.existsSync(path.join(root,item))){
    let data=fs.readFileSync(path.join(root,'Assets/RoyalAves/Resources/MapLibrary/Props/poste.asset'),'utf8');
    data=data.replace('m_Name: poste','m_Name: '+name).replace('title: Poste de luz','title: '+title).replace(/sprite: \{[^\n]+\}/,'sprite: {fileID: 21300000, guid: '+guid+', type: 3}').replace('size: 1.06','size: 1.14');
    fs.writeFileSync(path.join(root,item),data);fs.writeFileSync(path.join(root,item+'.meta'),'fileFormatVersion: 2\nguid: '+hash('royalaves/library/'+name)+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n');
  }
  console.log(title+' -> '+png);
}
