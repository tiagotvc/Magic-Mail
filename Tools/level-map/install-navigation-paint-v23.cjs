// Preserve the generated PNGs byte for byte. Read alpha only to locate the backplate UVs.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),manifest=require('./navigation-paint-v23.json');
const template=fs.readFileSync(path.join(root,'Assets/RoyalAves/Art/MapKitV3/GroundPaint/lawn-brush-v21.png.meta'),'utf8');
(async()=>{
 const results=[];
 for(const job of manifest.jobs){
  const {data,info}=await sharp(job.source).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const rows=[];
  for(let y=0;y<info.height;y++){
   let count=0,left=info.width,right=0;
   for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>245){count++;left=Math.min(left,x);right=Math.max(right,x);}
   if(count>info.width*.78)rows.push({y,left,right,count});
  }
  const top=rows[0].y,bottom=rows[rows.length-1].y;
  const full=rows.filter(r=>r.y>top+15&&r.y<top+45);
  const left=Math.min(...full.map(r=>r.left)),right=Math.max(...full.map(r=>r.right));
  const file=path.join(root,'Assets/RoyalAves/Art/MapKitV3/MapUI',job.file);fs.copyFileSync(job.source,file);
  const guid=job.id==='base'?'472e9fca04ae4beb89f4b7cd98678096':'0f51b696f09246c395e10ff7ae274890';
  fs.writeFileSync(file+'.meta',template.replace(/^guid: .+$/m,'guid: '+guid).replaceAll(/maxTextureSize: \d+/g,'maxTextureSize: 4096').replaceAll(/textureCompression: \d+/g,'textureCompression: 0').replace(/enableMipMap: \d+/,'enableMipMap: 1').replace(/ignoreMipmapLimit: \d+/,'ignoreMipmapLimit: 1').replace(/filterMode: \d+/,'filterMode: 2'));
  const rect=[left/info.width,1-(bottom+1)/info.height,(right-left+1)/info.width,(bottom-top+1)/info.height];
  results.push({id:job.id,file:job.file,guid,width:info.width,height:info.height,bounds:[left,top,right,bottom],rect,sha256:crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex')});
 }
 fs.writeFileSync(path.join(__dirname,'previews/navigation-reference/paint-validation.json'),JSON.stringify(results,null,2));
 console.log(JSON.stringify(results));
})();
