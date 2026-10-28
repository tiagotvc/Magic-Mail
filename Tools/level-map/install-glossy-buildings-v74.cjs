const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
process.chdir(path.resolve(__dirname,'../..'));
const manifest='Tools/level-map/glossy-buildings-v74-manifest.json',m=JSON.parse(fs.readFileSync(manifest,'utf8'));
const folder='Assets/RoyalAves/Art/MapKitV3/GlossyBuildingsV74',guid=()=>crypto.randomBytes(16).toString('hex');
fs.mkdirSync(folder,{recursive:true});
if(!fs.existsSync(folder+'.meta'))fs.writeFileSync(folder+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
const resources='Assets/RoyalAves/Resources/';
const files=['RoyalAvesWorldMapIslands.asset','MapLibrary/Construcoes/christmas-post-office-front-v70.asset','MapLibrary/Construcoes/christmas-dispatch-front-v70.asset','MapLibrary/Construcoes/stamp-workshop-front-v70.asset'];
const before=fs.readFileSync(resources+files[0],'utf8'),islands=before.slice(before.indexOf('  islands:'));
for(const a of m.assets){
 a.file=folder+'/'+a.output;fs.copyFileSync(a.source,a.file);
 a.previousGuid=fs.readFileSync(a.input+'.meta','utf8').match(/^guid: (\w+)/m)[1];
 a.guid=fs.existsSync(a.file+'.meta')?fs.readFileSync(a.file+'.meta','utf8').match(/^guid: (\w+)/m)[1]:guid();
 if(!fs.existsSync(a.file+'.meta'))fs.writeFileSync(a.file+'.meta',fs.readFileSync(a.input+'.meta','utf8').replace(/^guid: \w+/m,`guid: ${a.guid}`));
}
for(const name of files){let text=fs.readFileSync(resources+name,'utf8');for(const a of m.assets)text=text.replaceAll(a.previousGuid,a.guid);
 if(name===files[0]&&text.slice(text.indexOf('  islands:'))!==islands)throw Error('Authored island settings changed');
 fs.writeFileSync(resources+name,text);
}
fs.writeFileSync(manifest,JSON.stringify(m,null,2)+'\n');console.log('Installed three glossy building revisions in recipes and existing library entries. Island settings/layouts retained.');
