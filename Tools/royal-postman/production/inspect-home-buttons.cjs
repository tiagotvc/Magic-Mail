// Read-only alpha measurements for native assembly; never modifies raster pixels.
const fs=require('fs'),path=require('path');
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
(async()=>{
 const dir=path.resolve(__dirname,'../../../Assets/RoyalAves/Art/RoyalPostmanUI2026'),assets=[];
 for(const id of ['level-base-v6','pet-base-v6']){
  const {data,info}=await sharp(path.join(dir,id+'.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let x0=info.width,y0=info.height,x1=-1,y1=-1;
  const alpha=(x,y)=>data[(y*info.width+x)*4+3];
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(alpha(x,y)>200){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}
  const entry={id,width:info.width,height:info.height,rectangles:[{x:x0,y:y0,width:x1-x0+1,height:y1-y0+1}]};
  if(id.includes('rim')){
   const cx=Math.round((x0+x1)/2),cy=Math.round((y0+y1)/2);let l=cx,r=cx,t=cy,b=cy;
   while(l>x0&&alpha(l,cy)<200)l--;while(r<x1&&alpha(r,cy)<200)r++;
   while(t>y0&&alpha(cx,t)<200)t--;while(b<y1&&alpha(cx,b)<200)b++;
   entry.opening={x:l,y:t,width:r-l,height:b-t};
  }
  assets.push(entry);
 }
 fs.writeFileSync(path.join(__dirname,'HOME-BUTTON-BOUNDS.json'),JSON.stringify({assets},null,2));console.log(JSON.stringify(assets));
})().catch(e=>{console.error(e);process.exit(1)});
