// Extract UV mapping data only; the reference-derived raster pixels remain untouched.
const fs=require('node:fs'),path=require('node:path');
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const root=path.resolve(__dirname,'../..');
(async()=>{
 const file=process.argv[2]||'Assets/RoyalAves/Art/MapKitV3/GreenLand/ground-reference-plate.png';
 const {data,info}=await sharp(path.join(root,file)).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 const rows=[];
 for(let i=0;i<64;i++){
  const v=i/63*.72,y=Math.min(info.height-1,Math.floor((1-v)*info.height));
  const inside=x=>{const p=(y*info.width+x)*4;return data[p+3]>200&&data[p]>data[p+1]*.52&&data[p+1]>130;};
  let c=Math.floor(info.width*.5);
  if(!inside(c))throw Error('No central lane at row '+i);
  let left=c,right=c;while(left>0&&inside(left-1))left--;while(right<info.width-1&&inside(right+1))right++;
  rows.push({x:Math.max(0,left/info.width-.009),y:Math.min(1,(right+1)/info.width+.009)});
 }
 const dest=process.argv[3]||'Assets/RoyalAves/Art/MapKitV3/GreenLand/ground-reference-bounds.json';
 fs.writeFileSync(path.join(root,dest),JSON.stringify({source:file,maxV:.72,rows},null,2));
 console.log(JSON.stringify({width:info.width,height:info.height,rows:rows.length,first:rows[0],last:rows[63]}));
})().catch(e=>{console.error(e);process.exitCode=1});
