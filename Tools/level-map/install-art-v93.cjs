const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),manifest=JSON.parse(fs.readFileSync(path.join(__dirname,'art-v93-manifest.json'),'utf8'));
const guid=id=>crypto.createHash('md5').update('magic-mail/v93/'+id).digest('hex');
const protectedFiles=['RoyalAvesWorldMapLayout.asset','RoyalAvesWorldMapSimpleLayout.asset','RoyalAvesMission100Layout.asset','RoyalAvesWorldMapIslands.asset'].map(x=>'Assets/RoyalAves/Resources/'+x);
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,file))).digest('hex');
const before=protectedFiles.map(hash),template=fs.readFileSync(path.join(root,'Assets/RoyalAves/Resources/MapEventsV90/letter-vault.png.meta'),'utf8');
for(const a of manifest.assets){
 const target=path.join(root,a.output),folder=path.dirname(target);fs.mkdirSync(folder,{recursive:true});fs.copyFileSync(a.source,target);
 if(!fs.existsSync(folder+'.meta'))fs.writeFileSync(folder+'.meta',`fileFormatVersion: 2\nguid: ${guid(a.id+'/folder')}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
 if(!fs.existsSync(target+'.meta')){
  let meta=template.replace(/^guid: .+$/m,'guid: '+guid(a.id)).replace(/userData: .*/,'userData: Magic Mail V93 '+a.id+' - original glossy cartoon');
  if(a.id==='character')meta=meta.replace('enableMipMap: 0','enableMipMap: 1').replaceAll('maxTextureSize: 512','maxTextureSize: 2048').replace('spritePivot: {x: 0.5, y: 0.5}','spritePivot: {x: 0.5, y: 0}');
  fs.writeFileSync(target+'.meta',meta);
 }
}
const item=path.join(root,manifest.characterLibrary);
if(!fs.existsSync(item)){
 let data=fs.readFileSync(path.join(root,'Assets/RoyalAves/Resources/MapLibrary/Personagens/courier-wizard-v50.asset'),'utf8');
 data=data.replace(/m_Name: .*/,'m_Name: postal-apprentice-v93').replace(/title: .*/,'title: "Aprendiz de carteiro (pequena)"').replace(/sprite: \{[^\n]+\}/,'sprite: {fileID: 21300000, guid: '+guid('character')+', type: 3}').replace(/size: [\d.]+/,'size: 0.82');
 fs.writeFileSync(item,data);fs.writeFileSync(item+'.meta',`fileFormatVersion: 2\nguid: ${guid('library/character')}\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n`);
}
if(protectedFiles.some((file,i)=>hash(file)!==before[i]))throw new Error('Protected layout changed');
console.log('Installed glossy postal pig and small apprentice library item. Four authored layout/island hashes unchanged. Originals preserved.');
