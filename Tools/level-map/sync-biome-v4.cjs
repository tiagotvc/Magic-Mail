// Persist validated import settings and new art bindings; keep camera tuning and manual layout.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),preview=path.join(root,'Library/MapValidation/PreviewProject');
const guid=s=>s.match(/^guid: (\w+)/m)?.[1];
const layout=path.join(root,'Assets/RoyalAves/Resources/RoyalAvesWorldMapLayout.asset');
const hash=()=>crypto.createHash('sha256').update(fs.readFileSync(layout)).digest('hex');
const before=hash(),files=[];
for(const manifest of ['mapkit-v2-image-manifest.json','mapkit-v3-image-manifest.json'])
 for(const item of JSON.parse(fs.readFileSync(path.join(__dirname,manifest),'utf8')).images)
  if(item.recommended)files.push(item.path+'.meta');
for(const file of files){const a=fs.readFileSync(path.join(root,file),'utf8'),b=fs.readFileSync(path.join(preview,file),'utf8');if(guid(a)!==guid(b))throw Error('GUID mismatch: '+file);}
for(const file of files)fs.copyFileSync(path.join(preview,file),path.join(root,file));
const kit='Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset';
let current=fs.readFileSync(path.join(root,kit),'utf8');const validated=fs.readFileSync(path.join(preview,kit),'utf8');
function block(text,key){const start=text.indexOf('\n  '+key+':');if(start<0)return null;const rest=text.slice(start+1);const next=rest.slice(1).search(/\n  [A-Za-z_]\w*:/);return next<0?rest.trimEnd():rest.slice(0,next+1).trimEnd();}
const keys=['glow','vapor','gardenFamily','forestFamily','coastFamily','auroraFamily','geyserFrames','desertFamily','orchardFamily','marshFamily','cloudFamily','crystalFamily','geyserBase','geyserWater','geyserSteam','meteorSprite','snowTerrainMaterials','simpleNature','simplePostal','simpleBridge','simpleCrown','simpleStar','simpleHard','simpleExpress','simpleCloudSmall','simpleCloudWide'];
for(const key of keys)
{const value=block(validated,key);if(!value)throw Error('Missing validated binding: '+key);const old=block(current,key);current=old?current.replace(old,value):current.trimEnd()+'\n'+value+'\n';}
fs.writeFileSync(path.join(root,kit),current);
const profile='Assets/RoyalAves/Resources/RoyalAvesMapPostProfile.asset';
if(!fs.existsSync(path.join(root,profile)))for(const suffix of ['', '.meta'])fs.copyFileSync(path.join(preview,profile+suffix),path.join(root,profile+suffix));
const pipeline=path.join(root,'Assets/SweetSugar/Settings/SweetSugarURP.asset');
fs.writeFileSync(pipeline,fs.readFileSync(pipeline,'utf8').replace(/m_AllowPostProcessAlphaOutput: 0/,'m_AllowPostProcessAlphaOutput: 1'));
const renderer=path.join(root,'Assets/Settings/MapWorldRenderer.asset');
const post=fs.readFileSync(path.join(preview,'Assets/Settings/MapWorldRenderer.asset'),'utf8').match(/^  postProcessData: .+$/m)?.[0];
if(!post)throw Error('Missing post-process resources');fs.writeFileSync(renderer,fs.readFileSync(renderer,'utf8').replace(/^  postProcessData: .+$/m,post));
console.log(JSON.stringify({metadataCopied:files.length,newBindings:keys.length,cameraTuning:'preserved',manualLayout:hash()===before?'unchanged':'changed externally during sync'}));
