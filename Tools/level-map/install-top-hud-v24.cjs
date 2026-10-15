// Analyze alpha without changing pixels. UI UVs use the principal opaque connected component.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),manifest=require('./top-hud-v24.json');
const template=fs.readFileSync(path.join(root,'Assets/RoyalAves/Art/MapKitV3/GroundPaint/lawn-brush-v21.png.meta'),'utf8');
(async()=>{
 const results=[];
 for(const [index,job] of manifest.jobs.entries()){
  const {data,info}=await sharp(job.source).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const {width:w,height:h}=info,visited=new Uint8Array(w*h),queue=new Int32Array(w*h);let largest=null;
  for(let i=0;i<w*h;i++)if(!visited[i]&&data[i*4+3]>180){
   let head=0,tail=1,x0=w,y0=h,x1=0,y1=0;queue[0]=i;visited[i]=1;
   while(head<tail){const p=queue[head++],x=p%w,y=Math.floor(p/w);x0=Math.min(x0,x);x1=Math.max(x1,x);y0=Math.min(y0,y);y1=Math.max(y1,y);
    for(const n of [x>0?p-1:-1,x+1<w?p+1:-1,y>0?p-w:-1,y+1<h?p+w:-1])if(n>=0&&!visited[n]&&data[n*4+3]>180){visited[n]=1;queue[tail++]=n;}
   }
   if(!largest||tail>largest.count)largest={count:tail,x0,y0,x1,y1};
  }
  if(!largest)throw Error('No opaque component for '+job.id);
  const {x0,y0,x1,y1}=largest,rect=[x0/w,1-(y1+1)/h,(x1-x0+1)/w,(y1-y0+1)/h];
  const dest=path.join(root,'Assets/RoyalAves/Art/MapKitV3/MapUI',job.file);fs.copyFileSync(job.source,dest);
  const guid=crypto.createHash('md5').update('MagicMail:top-hud-v24:'+job.id).digest('hex');
  fs.writeFileSync(dest+'.meta',template.replace(/^guid: .+$/m,'guid: '+guid).replaceAll(/maxTextureSize: \d+/g,'maxTextureSize: 4096').replaceAll(/textureCompression: \d+/g,'textureCompression: 0').replace(/enableMipMap: \d+/,'enableMipMap: 0').replace(/ignoreMipmapLimit: \d+/,'ignoreMipmapLimit: 1').replace(/filterMode: \d+/,'filterMode: 1'));
  results.push({id:job.id,guid,file:job.file,width:w,height:h,rect,bounds:[x0,y0,x1,y1],sha256:crypto.createHash('sha256').update(fs.readFileSync(dest)).digest('hex')});
 }
 const asset=path.join(root,'Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset');
 let yaml=fs.readFileSync(asset,'utf8').replace(/^  topHudArt:\n(?:  -.*\n)*/m,'').replace(/^  topHudRects:\n(?:  -.*\n)*/m,'');
 yaml+='  topHudArt:\n'+results.map(r=>'  - {fileID: 2800000, guid: '+r.guid+', type: 3}\n').join('');
 yaml+='  topHudRects:\n'+results.map(r=>'  - {x: '+r.rect[0]+', y: '+r.rect[1]+', z: '+r.rect[2]+', w: '+r.rect[3]+'}\n').join('');
 fs.writeFileSync(asset,yaml);
 fs.mkdirSync(path.join(__dirname,'previews/top-hud'),{recursive:true});
 fs.copyFileSync(path.join(__dirname,'previews/navigation-reference/hud-reference-0.png'),path.join(__dirname,'previews/top-hud/reference.png'));
 fs.writeFileSync(path.join(__dirname,'previews/top-hud/assets-validation.json'),JSON.stringify(results,null,2));
 console.log(JSON.stringify(results));
})();
