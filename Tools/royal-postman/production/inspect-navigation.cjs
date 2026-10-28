// Read-only alpha bounds for separate native Sprite rectangles. Does not alter pixels.
const fs=require('fs');
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const path=require('path');
(async()=>{
 const folder=path.resolve(__dirname,'../../../Assets/RoyalAves/Art/RoyalPostmanUI2026');
 const entries=[];
 for(const id of ['navigation-bar-approved-v2',...['clipboard','trophy','home','teams','album'].map(n=>'navigation-'+n+'-individual-v3')]){
  const {data,info}=await sharp(path.join(folder,id+'.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const bounds=(left,right)=>{
   let x0=info.width,y0=info.height,x1=-1,y1=-1;
   for(let y=0;y<info.height;y++)for(let x=left;x<right;x++)if(data[(y*info.width+x)*4+3]>200){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}
   return {x:x0,y:y0,width:x1-x0+1,height:y1-y0+1};
  };
  const cuts=[0,info.width];
  entries.push({id,width:info.width,height:info.height,rectangles:cuts.slice(0,-1).map((x,i)=>bounds(x,cuts[i+1]))});
 }
 fs.writeFileSync(path.join(__dirname,'NAVIGATION-BOUNDS.json'),JSON.stringify({assets:entries},null,2));
 console.log(JSON.stringify(entries));
})().catch(e=>{console.error(e);process.exit(1)});
