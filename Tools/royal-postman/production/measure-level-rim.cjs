// Read-only measurements of the native Unity capture; PNG pixels are not modified.
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const fs=require('fs');
(async()=>{
 const result=[];
 for(const file of process.argv.slice(2)){
  const {data,info}=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const gold=(x,y)=>{const p=(y*info.width+x)*4;return data[p]-data[p+2]>20&&data[p+3]>200;};
  const runs=(start,end,test)=>{let out=[],s=-1;for(let n=start;n<=end;n++){if(n<end&&test(n)){if(s<0)s=n;}else if(s>=0){out.push({start:s,end:n-1,size:n-s});s=-1;}}return out;};
  const horizontal=runs(200,670,x=>gold(x,160));const vertical=runs(40,280,y=>gold(435,y));
  result.push({file,left:horizontal[0]?.size,right:horizontal.at(-1)?.size,top:vertical[0]?.size,bottom:vertical.at(-1)?.size});
 }
 console.log(JSON.stringify(result,null,2));
})().catch(e=>{console.error(e);process.exit(1)});
