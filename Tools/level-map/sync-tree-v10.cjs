// Sync only the new entrance sprite metadata and binding. Authored layouts are never rewritten.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),preview=path.join(root,'Library/MapValidation/PreviewProject');
const file='Assets/RoyalAves/Art/MapKitV3/SimpleBase/entrance-trees-v10.png.meta',kit='Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset';
const layouts=['Assets/RoyalAves/Resources/RoyalAvesWorldMapLayout.asset','Assets/RoyalAves/Resources/RoyalAvesWorldMapSimpleLayout.asset'];
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,p))).digest('hex'),before=layouts.map(hash),guid=t=>t.match(/^guid: (.+)$/m)?.[1].trim();
const a=fs.readFileSync(path.join(root,file),'utf8'),b=fs.readFileSync(path.join(preview,file),'utf8');if(guid(a)!==guid(b))throw Error('GUID changed');fs.copyFileSync(path.join(preview,file),path.join(root,file));
function block(text,key){const start=text.indexOf('\n  '+key+':');if(start<0)return null;const rest=text.slice(start+1),next=rest.slice(1).search(/\n  [A-Za-z_]\w*:/);return next<0?rest.trimEnd():rest.slice(0,next+1).trimEnd();}
let text=fs.readFileSync(path.join(root,kit),'utf8'),validated=fs.readFileSync(path.join(preview,kit),'utf8'),value=block(validated,'entranceTrees');if(!value)throw Error('No entrance binding');const old=block(text,'entranceTrees');text=old?text.replace(old,value):text.trimEnd()+'\n'+value+'\n';fs.writeFileSync(path.join(root,kit),text);
const after=layouts.map(hash);if(after.some((h,i)=>h!==before[i]))throw Error('Layout changed during sync');
const result={metadata:file,entranceTrees:2,authoredLayouts:'unchanged',hashes:Object.fromEntries(layouts.map((p,i)=>[p,after[i]]))};fs.writeFileSync(path.join(__dirname,'previews/v10/sync-validation.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result));
