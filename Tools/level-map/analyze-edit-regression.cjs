const sharp = require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const path = require('node:path');
(async () => {
 const dir = path.join(__dirname, 'previews/polish');
 const a = await sharp(path.join(dir,'edit-before.png')).raw().toBuffer({resolveWithObject:true});
 const b = await sharp(path.join(dir,'edit-after.png')).raw().toBuffer();
 let sum=0,max=0;
 const thresholds=[0,2,4,8,16],counts=thresholds.map(()=>0),regions=[0,0,0,0];
 for(let i=0;i<a.data.length;i+=a.info.channels){
  let d=0;
  for(let c=0;c<3;c++){const q=Math.abs(a.data[i+c]-b[i+c]);sum+=q;d=Math.max(d,q);}
  max=Math.max(max,d);
  thresholds.forEach((t,k)=>{if(d>t)counts[k]++;});
  if(d>8)regions[Math.floor(i/a.info.channels/(a.info.width*a.info.height)*4)]++;
 }
 console.log(JSON.stringify({width:a.info.width,height:a.info.height,max,mean:sum/(a.info.width*a.info.height*3),thresholds,counts,verticalQuarters:regions}));
})();
