const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'../..');
(async()=>{
 const jobs=['polish-layer-assets.json','token-polish-assets.json'].flatMap(f=>JSON.parse(fs.readFileSync(path.join(__dirname,f))).jobs);
 for(const j of jobs){
  const {data,info}=await sharp(path.join(root,j.destination)).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const p=(x,y)=>Array.from(data.subarray((Math.floor(y*info.height)*info.width+Math.floor(x*info.width))*4,(Math.floor(y*info.height)*info.width+Math.floor(x*info.width))*4+4));
  if(j.kind==='terrain'&&(p(.5,.1)[3]>2||p(.5,.95)[3]<200))throw Error('Ground/sky alpha separation failed: '+j.id);
  if(j.transparent&&p(.01,.01)[3]>2)throw Error('Transparent asset corner is opaque: '+j.id);
  if(j.kind==='background'&&p(.5,.1)[3]!==255)throw Error('Sky layer not opaque: '+j.id);
  console.log(JSON.stringify({id:j.id,width:info.width,height:info.height,corner:p(.01,.01),upper:p(.5,.1),left:p(.1,.5),lower:p(.5,.95)}));
 }
})();
