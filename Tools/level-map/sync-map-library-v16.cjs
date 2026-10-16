const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),preview=path.join(root,'Library/MapValidation/PreviewProject');
const tracked=['Assets/RoyalAves/Resources/RoyalAvesWorldMapIslands.asset','Assets/RoyalAves/Resources/RoyalAvesWorldMapLayout.asset','Assets/RoyalAves/Resources/RoyalAvesWorldMapSimpleLayout.asset'];
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,p))).digest('hex'),before=tracked.map(hash);
const guid=t=>t.match(/^guid: (.+)$/m)?.[1].trim();
for(const job of JSON.parse(fs.readFileSync(path.join(__dirname,'map-library-v16-assets.json'),'utf8')).jobs){
 const p='Assets/RoyalAves/Art/MapKitV3/'+job.folder+'/'+job.file+'.meta';
 if(guid(fs.readFileSync(path.join(root,p),'utf8'))!==guid(fs.readFileSync(path.join(preview,p),'utf8')))throw Error('Art GUID changed: '+p);
 fs.copyFileSync(path.join(preview,p),path.join(root,p));
}
let added=0;
const library='Assets/RoyalAves/Resources/MapLibrary';
function copyMissing(relative){const src=path.join(preview,relative),dst=path.join(root,relative);if(fs.statSync(src).isDirectory()){fs.mkdirSync(dst,{recursive:true});for(const f of fs.readdirSync(src))copyMissing(relative+'/'+f);}else if(!fs.existsSync(dst)){fs.copyFileSync(src,dst);added++;}}
copyMissing(library);
const kit='Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset';let text=fs.readFileSync(path.join(root,kit),'utf8');
const binding=fs.readFileSync(path.join(preview,kit),'utf8').match(/^  waterRocks: .+$/m)?.[0];
if(!binding||binding.includes('{fileID: 0}'))throw Error('Water rocks are unbound');
text=/^  waterRocks: .+$/m.test(text)?text.replace(/^  waterRocks: .+$/m,binding):text.trimEnd()+'\n'+binding+'\n';
fs.writeFileSync(path.join(root,kit),text);
const after=tracked.map(hash);if(after.some((h,i)=>h!==before[i]))throw Error('Authoring data changed');
const report={newLibraryFiles:added,artImports:2,authoringIslandsAndLayouts:'unchanged',hashes:Object.fromEntries(tracked.map((p,i)=>[p,after[i]]))};
fs.writeFileSync(path.join(__dirname,'previews/v16/sync-validation.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
