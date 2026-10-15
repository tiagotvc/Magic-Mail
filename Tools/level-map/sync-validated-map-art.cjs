// Copy only validated art metadata and the WorldMapKit; preserve the user's manual layout.
const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'../..');
const preview=path.join(root,'Library/MapValidation/PreviewProject');
const guid=file=>fs.readFileSync(file,'utf8').match(/^guid: (.+)$/m)?.[1];
const paths=[];
for(const name of ['mapkit-v2-image-manifest.json','mapkit-v3-image-manifest.json']){
 const manifest=JSON.parse(fs.readFileSync(path.join(__dirname,name),'utf8'));
 for(const image of manifest.images)if(image.recommended)paths.push(image.path+'.meta');
}
// Verify GUIDs before any writes so saved sprite references remain valid.
for(const relative of paths){
 const source=path.join(preview,relative),target=path.join(root,relative);
 if(!fs.existsSync(source))throw Error('Missing validated metadata: '+relative);
 if(fs.existsSync(target)&&guid(source)!==guid(target))throw Error('GUID mismatch: '+relative);
}
for(const relative of paths)fs.copyFileSync(path.join(preview,relative),path.join(root,relative));
const kit='Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset';
fs.copyFileSync(path.join(preview,kit),path.join(root,kit));
console.log(JSON.stringify({metadataCopied:paths.length,kitCopied:kit,manualLayout:'preserved'}));
