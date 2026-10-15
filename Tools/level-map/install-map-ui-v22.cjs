// Inspect alpha only; generated PNG pixels are copied byte for byte.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),manifest=require('./map-ui-v22-assets.json');
const art=path.join(root,'Assets/RoyalAves/Art/MapKitV3/MapUI');
const template=fs.readFileSync(path.join(root,'Assets/RoyalAves/Art/MapKitV3/Library/Arvores/teal-wonky-pine-v20.png.meta'),'utf8');
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
(async()=>{
 fs.mkdirSync(art,{recursive:true});const checks=[];
 for(const job of manifest.jobs){
  const {data,info}=await sharp(job.source).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let clear=0;for(let i=3;i<data.length;i+=4)if(data[i]===0)clear++;
  if(clear/info.width/info.height<.10)throw Error('Opaque background: '+job.id);
  const target=path.join(art,job.file),meta=target+'.meta';
  if(fs.existsSync(target)&&hash(target)!==hash(job.source))throw Error('Refusing to overwrite '+target);
  if(!fs.existsSync(target))fs.copyFileSync(job.source,target);
  const metadata=fs.existsSync(meta)?fs.readFileSync(meta,'utf8'):template.replace(/^guid: .+$/m,'guid: '+crypto.randomBytes(16).toString('hex')).replaceAll('teal-wonky-pine-v20','nav-'+job.id+'-v22');
  // Keep native detail for enlarged previews, with mip filtering for smooth phone-sized icons.
  fs.writeFileSync(meta,metadata.replaceAll(/maxTextureSize: \d+/g,'maxTextureSize: 4096').replaceAll(/textureCompression: \d+/g,'textureCompression: 0').replace(/enableMipMap: \d+/,'enableMipMap: 1').replace(/ignoreMipmapLimit: \d+/,'ignoreMipmapLimit: 1').replace(/filterMode: \d+/,'filterMode: 2'));
  checks.push({id:job.id,file:path.relative(root,target).replaceAll('\\','/'),width:info.width,height:info.height,transparent:clear/info.width/info.height,sha256:hash(target)});
 }
 const guid=file=>fs.readFileSync(path.join(root,file+'.meta'),'utf8').match(/^guid: ([a-f0-9]+)/m)[1];
 const ref=file=>'{fileID: 21300000, guid: '+guid(file)+', type: 3}';
 const kitPath=path.join(root,'Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset');let kit=fs.readFileSync(kitPath,'utf8');
 const fields={expressiveTrees:['crooked-canopy-v20','coral-fan-tree-v20','teal-wonky-pine-v20','crooked-mail-palm-v20'].map(name=>ref('Assets/RoyalAves/Art/MapKitV3/Library/Arvores/'+name+'.png')),
  expressivePostOffice:ref('Assets/RoyalAves/Art/MapKitV3/Library/Construcoes/forest-post-v19.png'),expressiveWorkshop:ref('Assets/RoyalAves/Art/MapKitV3/Library/Construcoes/seal-workshop-v19.png'),
  mapUiTemplate:'{fileID: 8256221740269461732, guid: '+guid('Assets/RoyalAves/Prefabs/RoyalAvesLobby.prefab')+', type: 3}',
  mapNavIcons:['events','ranking','map','teams','collection'].map(name=>ref('Assets/RoyalAves/Art/MapKitV3/MapUI/nav-'+name+'-v22.png'))};
 // The prefab root fileID comes from its actual root GameObject, not its Canvas component.
 const prefab=fs.readFileSync(path.join(root,'Assets/RoyalAves/Prefabs/RoyalAvesLobby.prefab'),'utf8');
 const rootId=Array.from(prefab.matchAll(/--- !u!1 &(-?\d+)\r?\nGameObject:([\s\S]*?)(?=--- !u!|$)/g)).find(match=>/^  m_Name: RoyalAvesLobby\r?$/m.test(match[2]))?.[1];
 if(!rootId)throw Error('Lobby root not found');fields.mapUiTemplate='{fileID: '+rootId+', guid: '+guid('Assets/RoyalAves/Prefabs/RoyalAvesLobby.prefab')+', type: 3}';
 for(const [name,value]of Object.entries(fields)){
  const block='  '+name+':'+(Array.isArray(value)?'\n'+value.map(v=>'  - '+v).join('\n'):' '+value);
  const pattern=new RegExp('^  '+name+':[^\\r\\n]*(?:\\r?\\n  - [^\\r\\n]*)*','m');
  kit=pattern.test(kit)?kit.replace(pattern,block):kit.trimEnd()+'\n'+block+'\n';
 }
 fs.writeFileSync(kitPath,kit);
 const out=path.join(__dirname,'previews/navigation');fs.mkdirSync(out,{recursive:true});fs.writeFileSync(path.join(out,'icons-validation.json'),JSON.stringify(checks,null,2));
 console.log(JSON.stringify({icons:checks.length,pixels:'unchanged',alpha:'validated'}));
})().catch(error=>{console.error(error);process.exitCode=1;});
