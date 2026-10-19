// Sync validated art imports and new library entries only; never copy authoring islands/layouts.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),preview=path.join(root,'Library/MapValidation/PreviewProject');
const tracked=['Assets/RoyalAves/Resources/RoyalAvesWorldMapIslands.asset','Assets/RoyalAves/Resources/RoyalAvesWorldMapLayout.asset','Assets/RoyalAves/Resources/RoyalAvesWorldMapSimpleLayout.asset'];
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,p))).digest('hex'),before=tracked.map(hash);
const guid=t=>t.match(/^guid: (.+)$/m)?.[1].trim();
for(const name of ['WorldMapLibraryItem','WorldMapGroundDetail']){
 const p='Assets/RoyalAves/Scripts/Meta/WorldMap/'+name+'.cs.meta';
 if(guid(fs.readFileSync(path.join(root,p),'utf8'))!==guid(fs.readFileSync(path.join(preview,p),'utf8')))throw Error('Script GUID mismatch: '+name);
}
let imports=0,added=0;
for(const file of ['ground-details-v14-assets.json','map-library-v15-assets.json'])for(const job of JSON.parse(fs.readFileSync(path.join(__dirname,file),'utf8')).jobs){
 const p='Assets/RoyalAves/Art/MapKitV3/'+job.folder+'/'+job.file+'.meta';
 if(guid(fs.readFileSync(path.join(root,p),'utf8'))!==guid(fs.readFileSync(path.join(preview,p),'utf8')))throw Error('Art GUID mismatch: '+p);
 fs.copyFileSync(path.join(preview,p),path.join(root,p));imports++;
}
const library='Assets/RoyalAves/Resources/MapLibrary';
function copyMissing(relative){
 const src=path.join(preview,relative),dst=path.join(root,relative);
 if(fs.statSync(src).isDirectory()){fs.mkdirSync(dst,{recursive:true});for(const f of fs.readdirSync(src))copyMissing(relative+'/'+f);}
 else if(!fs.existsSync(dst)){fs.copyFileSync(src,dst);added++;}
}
copyMissing(library);if(fs.existsSync(path.join(preview,library+'.meta')))copyMissing(library+'.meta');
const kit='Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset';let text=fs.readFileSync(path.join(root,kit),'utf8'),validated=fs.readFileSync(path.join(preview,kit),'utf8');
function block(t,key){const start=t.indexOf('\n  '+key+':');if(start<0)return null;const rest=t.slice(start+1),next=rest.slice(1).search(/\n  [A-Za-z_]\w*:/);return next<0?rest.trimEnd():rest.slice(0,next+1).trimEnd();}
for(const key of ['groundDetails','waterRipple']){const value=block(validated,key);if(!value||value.includes('{fileID: 0}'))throw Error('Missing '+key);const old=block(text,key);text=old?text.replace(old,value):text.trimEnd()+'\n'+value+'\n';}
fs.writeFileSync(path.join(root,kit),text);const after=tracked.map(hash);if(after.some((h,i)=>h!==before[i]))throw Error('Authoring data changed during sync');
const report={imports,newLibraryFiles:added,authoringIslandsAndLayouts:'unchanged',hashes:Object.fromEntries(tracked.map((p,i)=>[p,after[i]]))};
fs.writeFileSync(path.join(__dirname,'previews/v15/sync-validation.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
