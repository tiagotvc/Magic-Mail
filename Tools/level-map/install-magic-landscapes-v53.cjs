const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),hash=s=>crypto.createHash('md5').update(s).digest('hex');
const items=JSON.parse(fs.readFileSync(path.join(__dirname,'magic-landscapes-v53-manifest.json'),'utf8'));
for(const a of items){
 const art=path.join(root,'Assets/RoyalAves/Art/MapKitV3/Library',a.folder,a.name+'.png');fs.mkdirSync(path.dirname(art),{recursive:true});
 if(!fs.existsSync(art))fs.copyFileSync(a.source,art);
 if(!fs.existsSync(art+'.meta'))fs.writeFileSync(art+'.meta',fs.readFileSync(path.join(root,'Assets/RoyalAves/Art/MapKitV3/Library/Props/mailbox-glossy-v44.png.meta'),'utf8').replace(/^guid: .+$/m,'guid: '+hash('royalaves/'+a.name)).replaceAll('mailbox-glossy-v44',a.name).replace(/spriteID: [a-f0-9]+/g,'spriteID: '+hash(a.name+'/sprite')));
 const item=path.join(root,'Assets/RoyalAves/Resources/MapLibrary',a.folder,a.name+'.asset');fs.mkdirSync(path.dirname(item),{recursive:true});
 if(!fs.existsSync(item)){
  const character=a.folder==='Personagens',building=a.folder==='Construcoes',ground=a.name==='ink-stream-v53';
  let data=fs.readFileSync(path.join(root,'Assets/RoyalAves/Resources/MapLibrary/Props/poste.asset'),'utf8').replace(/m_Name: .+/,'m_Name: '+a.name).replace(/title: .+/,'title: '+JSON.stringify(a.title)).replace(/category: \d+/,'category: '+(character?4:building?0:2)).replace(/sprite: \{[^\n]+\}/,'sprite: {fileID: 21300000, guid: '+hash('royalaves/'+a.name)+', type: 3}').replace(/kind: \d+/,'kind: '+(character?8:building?2:ground?9:1)).replace(/size: [\d.]+/,'size: '+a.size);
  fs.writeFileSync(item,data);fs.writeFileSync(item+'.meta','fileFormatVersion: 2\nguid: '+hash('royalaves/library/'+a.name)+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n');
 }
}
const file=path.join(root,'Assets/RoyalAves/Resources/WorldMapWindowProfiles.json'),data=JSON.parse(fs.readFileSync(file,'utf8'));
for(const p of [{name:'stamp-factory-v53',windows:[{x:.70,y:.24,z:.13,w:.22}]},{name:'parcel-mountain-v53',windows:[{x:.57,y:.14,z:.13,w:.13}]}])if(!data.profiles.some(q=>q.name===p.name))data.profiles.push(p);
fs.writeFileSync(file,JSON.stringify(data,null,2)+'\n');console.log(`Installed ${items.length} separate original PNGs and library entries.`);
