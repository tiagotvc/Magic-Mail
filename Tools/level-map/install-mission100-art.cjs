const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..');const hash=s=>crypto.createHash('md5').update(s).digest('hex');
const items=JSON.parse(fs.readFileSync(path.join(__dirname,'mission100-assets-v50.json'),'utf8'));
for(const a of items){
 const relative=`Assets/RoyalAves/Art/MapKitV3/Library/${a.folder}/${a.name}.png`,png=path.join(root,relative),guid=hash('royalaves/'+a.name);
 fs.mkdirSync(path.dirname(png),{recursive:true});if(!fs.existsSync(png))fs.copyFileSync(a.source,png);
 if(!fs.existsSync(png+'.meta')){let meta=fs.readFileSync(path.join(root,'Assets/RoyalAves/Art/MapKitV3/Library/Props/mailbox-glossy-v44.png.meta'),'utf8').replace(/^guid: .+$/m,'guid: '+guid).replaceAll('mailbox-glossy-v44',a.name).replace(/spriteID: [a-f0-9]+/g,'spriteID: '+hash(a.name+'/sprite'));fs.writeFileSync(png+'.meta',meta);}
 const item=path.join(root,`Assets/RoyalAves/Resources/MapLibrary/${a.folder}/${a.name}.asset`);fs.mkdirSync(path.dirname(item),{recursive:true});
 if(!fs.existsSync(item)){let data=fs.readFileSync(path.join(root,'Assets/RoyalAves/Resources/MapLibrary/Props/poste.asset'),'utf8').replace('m_Name: poste','m_Name: '+a.name).replace('title: Poste de luz','title: '+JSON.stringify(a.title)).replace('category: 2','category: '+({Construcoes:0,Personagens:4,Props:2}[a.folder])).replace(/sprite: \{[^\n]+\}/,'sprite: {fileID: 21300000, guid: '+guid+', type: 3}').replace('kind: 3','kind: '+a.kind).replace('size: 1.06','size: '+a.size);fs.writeFileSync(item,data);fs.writeFileSync(item+'.meta','fileFormatVersion: 2\nguid: '+hash('royalaves/library/'+a.name)+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n');}
 console.log(a.name);
}
