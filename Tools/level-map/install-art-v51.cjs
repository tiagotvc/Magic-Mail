const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),hash=s=>crypto.createHash('md5').update(s).digest('hex');
const items=JSON.parse(fs.readFileSync(path.join(__dirname,'art-v51-manifest.json'),'utf8'));
for(const a of items){
 const folder=path.join(root,'Assets/RoyalAves/Art/MapKitV3/Library',a.folder),png=path.join(folder,a.name+'.png'),old=path.join(folder,a.oldName+'.png');
 if(!fs.existsSync(png))fs.copyFileSync(a.source,png);
 if(!fs.existsSync(png+'.meta'))fs.writeFileSync(png+'.meta',fs.readFileSync(old+'.meta','utf8').replace(/^guid: .+$/m,'guid: '+hash('royalaves/'+a.name)).replaceAll(a.oldName,a.name).replace(/spriteID: [a-f0-9]+/g,'spriteID: '+hash(a.name+'/sprite')));
 const itemDir=path.join(root,'Assets/RoyalAves/Resources/MapLibrary',a.folder),item=path.join(itemDir,a.name+'.asset');
 if(!fs.existsSync(item)){
  const previous=path.join(itemDir,a.oldName+'.asset'),template=fs.existsSync(previous)?previous:path.join(root,'Assets/RoyalAves/Resources/MapLibrary/Props/poste.asset');
  const data=fs.readFileSync(template,'utf8').replace(/m_Name: .+/,'m_Name: '+a.name).replace(/title: .+/,'title: '+JSON.stringify(a.title)).replace(/category: \d+/,'category: '+(a.folder==='Personagens'?4:0)).replace(/sprite: \{[^\n]+\}/,'sprite: {fileID: 21300000, guid: '+hash('royalaves/'+a.name)+', type: 3}').replace(/kind: \d+/,'kind: '+(a.folder==='Personagens'?8:2)).replace(/size: [\d.]+/,'size: '+a.size);
  fs.writeFileSync(item,data);fs.writeFileSync(item+'.meta','fileFormatVersion: 2\nguid: '+hash('royalaves/library/'+a.name)+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n');
 }
 console.log(a.name);
}
for(const profile of ['WorldMapWindowProfiles.json','WorldMapSnowPieceProfiles.json']){
 const file=path.join(root,'Assets/RoyalAves/Resources',profile),data=JSON.parse(fs.readFileSync(file,'utf8'));
 for(const a of items.filter(a=>a.folder==='Construcoes')){
  const old=data.profiles.find(p=>p.name===a.oldName||p.name===a.oldName+'_0');
  if(old&&!data.profiles.some(p=>p.name===a.name))data.profiles.push({...old,name:a.name});
 }
 fs.writeFileSync(file,JSON.stringify(data,null,2)+'\n');
}
