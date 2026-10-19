// Read-only color inspection: generates attachment data, never modifies source image pixels.
const fs=require('node:fs'),path=require('node:path');
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const root=path.resolve(__dirname,'../..');
const dirs=['Assets/RoyalAves/Art/MapKitV3/Library/Construcoes','Assets/RoyalAves/Art/MapKitV3/GreenLand'];
const inputs=dirs.flatMap(dir=>fs.readdirSync(path.join(root,dir)).filter(n=>n.endsWith('.png')).map(n=>dir+'/'+n));
inputs.push('Assets/RoyalAves/Art/MapKitV3/SimpleBase/simple-postal-v6.png');
(async()=>{
 const profiles=[];
 for(const file of inputs){
  const {data,info}=await sharp(path.join(root,file)).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const meta=fs.readFileSync(path.join(root,file+'.meta'),'utf8');
  const atlas=meta.split(/    - serializedVersion:/).slice(1).map(s=>{
   const n=s.match(/name: ([^\r\n]+)/),r=s.match(/rect:[\s\S]*?x: (\d+)[\s\S]*?y: (\d+)[\s\S]*?width: (\d+)[\s\S]*?height: (\d+)/);
   return n&&r?{name:n[1],x:+r[1],y:+r[2],w:+r[3],h:+r[4]}:null;
  }).filter(Boolean);
  const rects=atlas.length?atlas:[{name:path.basename(file,'.png'),x:0,y:0,w:info.width,h:info.height}];
  for(const rect of rects){
   if(file.includes('simple-postal-v6')&&!/house|workshop/.test(rect.name))continue;
   if(file.includes('/GreenLand/')&&!/office|factory|house/.test(rect.name))continue;
   const step=3,w=Math.ceil(rect.w/step),h=Math.ceil(rect.h/step),seen=new Uint8Array(w*h),mask=new Uint8Array(w*h);
   for(let y=0;y<h;y++)for(let x=0;x<w;x++){
    const py=info.height-rect.y-rect.h+Math.min(rect.h-1,y*step),px=rect.x+Math.min(rect.w-1,x*step),i=(py*info.width+px)*4;
    const r=data[i],g=data[i+1],b=data[i+2];
    if(y>h*.35&&y<h*.88&&data[i+3]>200&&b>r+25&&g>r*1.15&&b>g*1.08&&g>40)mask[y*w+x]=1;
   }
   const clusters=[];
   for(let k=0;k<mask.length;k++)if(mask[k]&&!seen[k]){
    const queue=[k];seen[k]=1;let loX=w,hiX=0,loY=h,hiY=0;
    for(let j=0;j<queue.length;j++){const v=queue[j],x=v%w,y=Math.floor(v/w);loX=Math.min(loX,x);hiX=Math.max(hiX,x);loY=Math.min(loY,y);hiY=Math.max(hiY,y);
     for(const n of [v-1,v+1,v-w,v+w])if(n>=0&&n<mask.length&&!seen[n]&&mask[n]&&Math.abs(n%w-x)<=1){seen[n]=1;queue.push(n);}
    }
    const width=(hiX-loX+1)/w,height=(hiY-loY+1)/h,area=queue.length/(w*h);
    if(area>.00035&&area<.025&&width>.018&&width<.25&&height>.018&&height<.25)clusters.push({x:loX/w,y:1-(hiY+1)/h,w:width,h:height,area});
   }
   // Adjacent panes separated by a window cross share one light anchor.
   for(let a=0;a<clusters.length;a++)for(let b=clusters.length-1;b>a;b--){const x=clusters[a],y=clusters[b];
    if(Math.abs(x.x+x.w/2-y.x-y.w/2)<.12&&Math.abs(x.y+x.h/2-y.y-y.h/2)<.12){
     const right=Math.max(x.x+x.w,y.x+y.w),top=Math.max(x.y+x.h,y.y+y.h);x.x=Math.min(x.x,y.x);x.y=Math.min(x.y,y.y);x.w=right-x.x;x.h=top-x.y;x.area+=y.area;clusters.splice(b,1);
    }
   }
   let windows=clusters.filter(c=>c.w<.27&&c.h<.27).sort((a,b)=>b.area-a.area).slice(0,4).map(c=>({x:+c.x.toFixed(5),y:+c.y.toFixed(5),z:+c.w.toFixed(5),w:+c.h.toFixed(5)}));
   // Visually checked primary houses: exclude door glints and blue-purple roof highlights.
   if(rect.name==='postman-cottage-v17_0')windows=[{x:.255,y:.23,z:.105,w:.115}];
   if(rect.name==='green-post-office-polished_0')windows=[{x:.415,y:.525,z:.125,w:.125}];
   if(rect.name==='simple_postal_v6_workshop')windows=[{x:.55,y:.13,z:.05,w:.14}];
   if(rect.name==='forest-post-v19_0')windows=[{x:.522,y:.305,z:.12,w:.20}];
   if(windows.length)profiles.push({name:rect.name,windows});
  }
 }
 fs.writeFileSync(path.join(root,'Assets/RoyalAves/Resources/WorldMapWindowProfiles.json'),JSON.stringify({profiles},null,2));
 console.log(JSON.stringify({profiles:profiles.length,windows:profiles.reduce((n,p)=>n+p.windows.length,0),entries:profiles.map(p=>[p.name,p.windows.length])}));
})();
