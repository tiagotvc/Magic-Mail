// Read-only alpha inspection. Never retouch or transform generated pixels here.
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
(async()=>{
 for(const name of (process.argv.length>2?process.argv.slice(2):['exec-7e674fa6-3cce-4ea6-b962-afa77eadd10f.png','exec-de145b74-b287-447e-9ff7-15b30105feea.png'])){
  const path='C:/Users/Tiago Carvalho/.codex/generated_images/01a13551-4eae-78c2-b012-d5d47dd589ce/'+name;
  const {data,info}=await sharp(path).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let clear=0,soft=0,solid=0;
  for(let i=3;i<data.length;i+=4){if(data[i]===0)clear++;else if(data[i]<250)soft++;else solid++;}
  const samples=[[.05,.05],[.15,.4],[.2,.5],[.3,.8],[.5,.5],[.98,.98]].map(([x,y])=>{const n=(Math.floor(y*info.height)*info.width+Math.floor(x*info.width))*4;return {x,y,rgba:[...data.subarray(n,n+4)]};});
  console.log(JSON.stringify({name,width:info.width,height:info.height,clear,soft,solid,samples}));
 }
})();
