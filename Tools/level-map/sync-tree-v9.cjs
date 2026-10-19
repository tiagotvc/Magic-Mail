// Only new tree bindings/import metadata; preserve all authored map layouts and camera tuning.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),preview=path.join(root,'Library/MapValidation/PreviewProject');
const files=['Assets/RoyalAves/Art/MapKitV3/SimpleBase/postal-trees-v9.png.meta','Assets/RoyalAves/Art/MapKitV3/SimpleBase/postal-tree-details-v9.png.meta'];
const layouts=['Assets/RoyalAves/Resources/RoyalAvesWorldMapLayout.asset','Assets/RoyalAves/Resources/RoyalAvesWorldMapSimpleLayout.asset'];
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,p))).digest('hex');
const before=layouts.map(hash),guid=s=>s.match(/^guid: (.+)$/m)?.[1].trim();
for(const f of files){const original=fs.readFileSync(path.join(root,f),'utf8'),validated=fs.readFileSync(path.join(preview,f),'utf8');if(guid(original)!==guid(validated))throw Error('GUID changed: '+f);fs.copyFileSync(path.join(preview,f),path.join(root,f));}
const kit='Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset';let original=fs.readFileSync(path.join(root,kit),'utf8'),validated=fs.readFileSync(path.join(preview,kit),'utf8');
function block(text,key){const start=text.indexOf('\n  '+key+':');if(start<0)return null;const rest=text.slice(start+1),next=rest.slice(1).search(/\n  [A-Za-z_]\w*:/);return next<0?rest.trimEnd():rest.slice(0,next+1).trimEnd();}
for(const key of ['postalTrees','postalTreeDetails','treeLeafShader']){const value=block(validated,key);if(!value)throw Error('Missing validated '+key);const old=block(original,key);original=old?original.replace(old,value):original.trimEnd()+'\n'+value+'\n';}
fs.writeFileSync(path.join(root,kit),original);
const after=layouts.map(hash);if(before.some((h,i)=>h!==after[i]))throw Error('Layout changed during sync');
const report={metadata:files,bindings:3,authoredLayouts:'unchanged',hashes:Object.fromEntries(layouts.map((p,i)=>[p,after[i]]))};fs.writeFileSync(path.join(__dirname,'previews/v9/sync-validation.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
