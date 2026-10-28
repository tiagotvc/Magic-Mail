// Read-only source-alpha measurements. Does not change image pixels.
const fs=require('fs'),path=require('path');
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const ids=['top-avatar-frame-v7','top-avatar-portrait-v7','top-coin-envelope-v7','top-heart-v7','top-round-existing-v14','top-plus-symbol-existing-v14','top-gear-existing-v14','top-value-container-v7'];
(async()=>{
 const folder=path.resolve(__dirname,'../../../Assets/RoyalAves/Art/RoyalPostmanUI2026'),assets=[];
 for(const id of ids){
  const {data,info}=await sharp(path.join(folder,id+'.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let x0=info.width,y0=info.height,x1=-1,y1=-1;
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>200){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}
  const rect=id==='top-plus-symbol-existing-v14'?{x:32,y:33,width:80,height:80}:{x:x0,y:y0,width:x1-x0+1,height:y1-y0+1};
  assets.push({id,width:info.width,height:info.height,rectangles:[rect]});
 }
 fs.writeFileSync(path.join(__dirname,'TOP-HUD-BOUNDS.json'),JSON.stringify({assets},null,2));console.log(JSON.stringify(assets));
})().catch(e=>{console.error(e);process.exit(1)});
