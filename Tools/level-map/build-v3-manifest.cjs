// Analyze generated alpha and define independent Unity Sprite subassets. Does not edit pixels.
const fs=require('node:fs'),path=require('node:path');
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const root=path.resolve(__dirname,'../..');
const jobs=JSON.parse(fs.readFileSync(path.join(__dirname,'mapkit-v3-generation-jobs.json'),'utf8'));
const list=Array.isArray(jobs)?jobs:jobs.jobs;
const composition=path.join(__dirname,'composition-assets.json');
if(fs.existsSync(composition))list.push(...JSON.parse(fs.readFileSync(composition,'utf8')).jobs);
const layers=path.join(__dirname,'polish-layer-assets.json');
if(fs.existsSync(layers))list.push(...JSON.parse(fs.readFileSync(layers,'utf8')).jobs);
const tokens=path.join(__dirname,'token-polish-assets.json');
if(fs.existsSync(tokens))list.push(...JSON.parse(fs.readFileSync(tokens,'utf8')).jobs);
const reference=path.join(__dirname,'reference-style-assets.json');
if(fs.existsSync(reference))list.push(...JSON.parse(fs.readFileSync(reference,'utf8')).jobs);
const styleFit=path.join(__dirname,'style-fit-assets.json');
if(fs.existsSync(styleFit))list.push(...JSON.parse(fs.readFileSync(styleFit,'utf8')).jobs);
const marginDetails=path.join(__dirname,'margin-detail-assets.json');
if(fs.existsSync(marginDetails))list.push(...JSON.parse(fs.readFileSync(marginDetails,'utf8')).jobs);
const overrides={'shared-mail-floaters':{columns:3,rows:2},'green-letter-factory':{x:[0,.43,.65,1]}};
const biomes=path.join(__dirname,'biome-v4-assets.json');
if(fs.existsSync(biomes))list.push(...JSON.parse(fs.readFileSync(biomes,'utf8')).jobs);
const seasons=path.join(__dirname,'season-v5-assets.json');
if(fs.existsSync(seasons))list.push(...JSON.parse(fs.readFileSync(seasons,'utf8')).jobs);
const simple=path.join(__dirname,'simple-v6-assets.json');
if(fs.existsSync(simple))list.push(...JSON.parse(fs.readFileSync(simple,'utf8')).jobs);
const emblems=path.join(__dirname,'level-emblem-assets.json');
if(fs.existsSync(emblems))list.push(...JSON.parse(fs.readFileSync(emblems,'utf8')).jobs);
const postman=path.join(__dirname,'postman-rig-v8-assets.json');
if(fs.existsSync(postman))list.push(...JSON.parse(fs.readFileSync(postman,'utf8')).jobs);
const postalTrees=path.join(__dirname,'postal-tree-v9-assets.json');
if(fs.existsSync(postalTrees))list.push(...JSON.parse(fs.readFileSync(postalTrees,'utf8')).jobs);
const entranceTrees=path.join(__dirname,'entrance-tree-v10-assets.json');
if(fs.existsSync(entranceTrees))list.push(...JSON.parse(fs.readFileSync(entranceTrees,'utf8')).jobs);
const postmanMap=path.join(__dirname,'postman-map-v12-assets.json');
if(fs.existsSync(postmanMap))list.push(...JSON.parse(fs.readFileSync(postmanMap,'utf8')).jobs);
const groundDetails=path.join(__dirname,'ground-details-v14-assets.json');
if(fs.existsSync(groundDetails))list.push(...JSON.parse(fs.readFileSync(groundDetails,'utf8')).jobs);
const library=path.join(__dirname,'map-library-v15-assets.json');
if(fs.existsSync(library))list.push(...JSON.parse(fs.readFileSync(library,'utf8')).jobs);
const libraryV16=path.join(__dirname,'map-library-v16-assets.json');
if(fs.existsSync(libraryV16))list.push(...JSON.parse(fs.readFileSync(libraryV16,'utf8')).jobs);
const libraryV17=path.join(__dirname,'map-library-v17-assets.json');
if(fs.existsSync(libraryV17))list.push(...JSON.parse(fs.readFileSync(libraryV17,'utf8')).jobs);
const libraryV18=path.join(__dirname,'map-library-v18-assets.json');
if(fs.existsSync(libraryV18))list.push(...JSON.parse(fs.readFileSync(libraryV18,'utf8')).jobs);
const libraryV19=path.join(__dirname,'map-library-v19-assets.json');
if(fs.existsSync(libraryV19))list.push(...JSON.parse(fs.readFileSync(libraryV19,'utf8')).jobs);
const treesV20=path.join(__dirname,'map-library-v20-assets.json');
if(fs.existsSync(treesV20))list.push(...JSON.parse(fs.readFileSync(treesV20,'utf8')).jobs);
const groundV21=path.join(__dirname,'ground-art-v21-assets.json');
if(fs.existsSync(groundV21))list.push(...JSON.parse(fs.readFileSync(groundV21,'utf8')).jobs);
const historical=new Set(['ground-painted-neutral','ground-illustrated-green-v2','ground-reference-plate','level-faces-reference','postal-scenery-variety','postal-navigation-icons','postal-bridge-variants']);
function components(data,w,h){
 const seen=new Uint8Array(w*h),stack=new Int32Array(w*h),out=[];
 for(let k=0;k<w*h;k++){
  if(seen[k]||data[k*4+3]<200)continue;
  let length=1,count=0,x0=w,y0=h,x1=0,y1=0;stack[0]=k;seen[k]=1;
  while(length){const p=stack[--length],x=p%w,y=Math.floor(p/w);count++;x0=Math.min(x0,x);x1=Math.max(x1,x);y0=Math.min(y0,y);y1=Math.max(y1,y);
   for(const n of [x? p-1:-1,x<w-1?p+1:-1,y?p-w:-1,y<h-1?p+w:-1])if(n>=0&&!seen[n]&&data[n*4+3]>=200){seen[n]=1;stack[length++]=n;}
  }
  if(count>1000)out.push({x0,y0,x1,y1,count});
 }
 return out.sort((a,b)=>b.count-a.count);
}
(async()=>{
 const images=[];
 for(const job of list){
  const family=job.id,folder='Assets/RoyalAves/Art/MapKitV3/'+job.folder+'/';
  const revised=folder+family+'-cartoon.png',original=folder+job.file;
  const source=fs.existsSync(path.join(root,revised))?revised:original;
  if(!fs.existsSync(path.join(root,source)))continue;
  const {data,info}=await sharp(path.join(root,source)).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const ov=overrides[family]||{},columns=ov.columns||job.columns,rows=ov.rows||job.rows;
  const names=job.names.length?job.names:['background'];
  const shapes=job.transparent?components(data,info.width,info.height).slice(0,names.length):[];
  shapes.sort((a,b)=>Math.floor((a.y0+a.y1)*.5/info.height*rows)-Math.floor((b.y0+b.y1)*.5/info.height*rows)||a.x0-b.x0);
  const sprites=[];
  for(let i=0;i<names.length;i++){
   const col=i%columns,row=Math.floor(i/columns);
   const animation=family==='thermal-geyser-v4';
   const shape=!animation&&shapes.length===names.length?shapes[i]:null;
   const left=shape?Math.max(0,shape.x0-3):Math.floor(info.width*(ov.x?ov.x[col]:col/columns)),right=shape?Math.min(info.width,shape.x1+4):Math.floor(info.width*(ov.x?ov.x[col+1]:(col+1)/columns));
   const top=animation?Math.max(0,Math.min(...shapes.map(s=>s.y0))-3):shape?Math.max(0,shape.y0-3):Math.floor(info.height*row/rows),bottom=animation?Math.min(info.height,Math.max(...shapes.map(s=>s.y1))+4):shape?Math.min(info.height,shape.y1+4):Math.floor(info.height*(row+1)/rows);
   let x0=right,y0=bottom,x1=left,y1=top,count=0;
   for(let y=top;y<bottom;y++)for(let x=left;x<right;x++)if(data[(y*info.width+x)*4+3]>=200){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);count++;}
   if(!count)throw Error('Empty sprite '+family+'/'+names[i]);
   // Keep each named cell separate; tight mesh uses alpha to avoid transparent padding in world size.
   sprites.push({name:(family+'_'+names[i]).replaceAll('-','_'),rect:{x:left,y:info.height-bottom,width:right-left,height:bottom-top},pivot:{x:.5,y:0},alphaBounds:{x:x0,y:info.height-y1-1,width:x1-x0+1,height:y1-y0+1},opaquePixels:count});
  }
  images.push({id:family,path:source,recommended:!historical.has(family),transparent:job.transparent,kind:job.kind||(job.names.length?'prop':'background'),width:info.width,height:info.height,columns,rows,sprites,status:historical.has(family)?'historical':source===revised?'cartoon-revision':'generated',supersedes:source===revised?original:null});
 }
 const manifest={version:1,note:'Independent named Unity sprites. Alpha bounds are analysis only; source pixels preserved. Background plates stay individual.',images};
 fs.writeFileSync(path.join(__dirname,'mapkit-v3-image-manifest.json'),JSON.stringify(manifest,null,2));
 console.log(JSON.stringify({images:images.length,sprites:images.reduce((n,x)=>n+x.sprites.length,0)}));
})().catch(e=>{console.error(e);process.exitCode=1;});
