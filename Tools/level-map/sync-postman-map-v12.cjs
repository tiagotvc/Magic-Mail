// Persist the approved illustration binding/import. All user map layouts stay byte-identical.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),preview=path.join(root,'Library/MapValidation/PreviewProject');
const layouts=['Assets/RoyalAves/Resources/RoyalAvesWorldMapLayout.asset','Assets/RoyalAves/Resources/RoyalAvesWorldMapSimpleLayout.asset'];
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(path.join(root,p))).digest('hex'),before=layouts.map(hash);
const meta='Assets/RoyalAves/Art/MapKitV3/Characters/postman-map-v12.png.meta',guid=t=>t.match(/^guid: (.+)$/m)?.[1].trim();
if(guid(fs.readFileSync(path.join(root,meta),'utf8'))!==guid(fs.readFileSync(path.join(preview,meta),'utf8')))throw Error('Thumbnail GUID changed');
fs.copyFileSync(path.join(preview,meta),path.join(root,meta));
const kit='Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset';let text=fs.readFileSync(path.join(root,kit),'utf8'),validated=fs.readFileSync(path.join(preview,kit),'utf8');
function block(t,key){const start=t.indexOf('\n  '+key+':');if(start<0)return null;const rest=t.slice(start+1),next=rest.slice(1).search(/\n  [A-Za-z_]\w*:/);return next<0?rest.trimEnd():rest.slice(0,next+1).trimEnd();}
for(const key of ['postmanPrefab','postmanIdle','postmanWave']){const old=block(text,key);if(old)text=text.replace(old+'\n','');}
for(const key of ['postmanSprite']){const value=block(validated,key);if(!value||value.includes('{fileID: 0}'))throw Error('Missing '+key);const old=block(text,key);text=old?text.replace(old,value):text.trimEnd()+'\n'+value+'\n';}
fs.writeFileSync(path.join(root,kit),text);const after=layouts.map(hash);if(after.some((h,i)=>h!==before[i]))throw Error('Layout changed');
const result={bindings:1,metadata:meta,manualLayouts:'unchanged',sourceIllustration:'unchanged',standalonePrototype:'preserved; not used by map',hashes:Object.fromEntries(layouts.map((p,i)=>[p,after[i]]))};fs.writeFileSync(path.join(__dirname,'previews/v12/sync-validation.json'),JSON.stringify(result,null,2));console.log(JSON.stringify(result));
