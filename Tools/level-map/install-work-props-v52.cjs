const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),hash=s=>crypto.createHash('md5').update(s).digest('hex');
for(const a of JSON.parse(fs.readFileSync(path.join(__dirname,'work-props-v52-manifest.json'),'utf8'))){
 const art=path.join(root,'Assets/RoyalAves/Art/MapKitV3/Library',a.folder,a.name+'.png');fs.mkdirSync(path.dirname(art),{recursive:true});if(!fs.existsSync(art))fs.copyFileSync(a.source,art);
 if(!fs.existsSync(art+'.meta'))fs.writeFileSync(art+'.meta',fs.readFileSync(path.join(root,'Assets/RoyalAves/Art/MapKitV3/Library/Props/mailbox-glossy-v44.png.meta'),'utf8').replace(/^guid: .+$/m,'guid: '+hash('royalaves/'+a.name)).replaceAll('mailbox-glossy-v44',a.name).replace(/spriteID: [a-f0-9]+/g,'spriteID: '+hash(a.name+'/sprite')));
 const item=path.join(root,'Assets/RoyalAves/Resources/MapLibrary',a.folder,a.name+'.asset');fs.mkdirSync(path.dirname(item),{recursive:true});
 if(!fs.existsSync(item)){const category=a.folder==='Personagens'?4:2;const text=fs.readFileSync(path.join(root,'Assets/RoyalAves/Resources/MapLibrary/Props/poste.asset'),'utf8').replace(/m_Name: .+/,'m_Name: '+a.name).replace(/title: .+/,'title: '+JSON.stringify(a.title)).replace(/category: \d+/,'category: '+category).replace(/sprite: \{[^\n]+\}/,'sprite: {fileID: 21300000, guid: '+hash('royalaves/'+a.name)+', type: 3}').replace(/kind: \d+/,'kind: '+(a.name==='ink-footprint-v52'?9:1)).replace(/size: [\d.]+/,'size: '+a.size);fs.writeFileSync(item,text);fs.writeFileSync(item+'.meta','fileFormatVersion: 2\nguid: '+hash('royalaves/library/'+a.name)+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n');}
 console.log(a.name);
}
