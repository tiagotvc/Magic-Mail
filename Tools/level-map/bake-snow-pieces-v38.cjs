// Read-only alpha inspection. Writes region/attachment data, never changes bitmap pixels.
const fs=require('node:fs'),path=require('node:path'),{execFileSync}=require('node:child_process');
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),kit=fs.readFileSync(path.join(root,'Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset'),'utf8');
const metas=execFileSync('rg',['--files','Assets/RoyalAves/Art','-g','*.png.meta'],{cwd:root,encoding:'utf8'}).trim().split(/\r?\n/),byGuid=new Map();
for(const p of metas){const txt=fs.readFileSync(path.join(root,p),'utf8');byGuid.set(txt.match(/^guid: (.+)$/m)?.[1],p);}
function ref(text){return [...text.matchAll(/fileID: (-?\d+), guid: ([a-f0-9]+), type: 3/g)].map(m=>({id:m[1],guid:m[2]}));}
const trees=ref(kit.match(/  treeDecorationProfiles:[\s\S]*?(?=\n  [a-zA-Z]|$)/)?.[0]||'');
const painted=[...kit.matchAll(/  - source: \{fileID: (-?\d+), guid: ([a-f0-9]+), type: 3\}\r?\n    overlay: \{fileID: (-?\d+), guid: ([a-f0-9]+), type: 3\}/g)].map(m=>({source:{id:m[1],guid:m[2]},overlay:{id:m[3],guid:m[4]}}));
async function read(reference){
 const meta=byGuid.get(reference.guid);if(!meta)return null;
 const text=fs.readFileSync(path.join(root,meta),'utf8'),file=meta.slice(0,-5);
 const {data,info}=await sharp(path.join(root,file)).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 let rect={x:0,y:0,w:info.width,h:info.height},name=path.basename(file,'.png');
 if(reference.id!=='21300000'){
  const s=text.split(/    - serializedVersion:/).find(s=>new RegExp('internalID: '+reference.id+'(?:\\r?\\n|$)').test(s));if(!s)return null;
  const r=s.match(/rect:[\s\S]*?x: (\d+)[\s\S]*?y: (\d+)[\s\S]*?width: (\d+)[\s\S]*?height: (\d+)/);if(!r)return null;
  rect={x:+r[1],y:+r[2],w:+r[3],h:+r[4]};name=s.match(/name: ([^\r\n]+)/)[1];
 }
 const top=info.height-rect.y-rect.h;
 const alpha=(x,y)=>x>=0&&x<rect.w&&y>=0&&y<rect.h?data[((top+y)*info.width+rect.x+x)*4+3]:0;
 return {name,rect,alpha};
}
function regions(image,frost){
 const {rect,alpha}=image,step=2,w=Math.ceil(rect.w/step),h=Math.ceil(rect.h/step),mask=new Uint8Array(w*h),seen=new Uint8Array(w*h);
 let bottom=0,top=rect.h;
 for(let y=0;y<rect.h;y+=step)for(let x=0;x<rect.w;x+=step)if(alpha(x,y)>=128){bottom=Math.max(bottom,y);top=Math.min(top,y);}
 for(let y=0;y<h;y++)for(let x=0;x<w;x++){
  const px=Math.min(rect.w-1,x*step),py=Math.min(rect.h-1,y*step);if(alpha(px,py)<128)continue;
  if(!frost){mask[y*w+x]=1;continue;}
  if((bottom-py)/Math.max(1,bottom-top)<.30)continue;
  let edge=0;const thick=rect.h*(.035+.010*Math.sin(px/rect.w*19+.5));
  for(let k=1;k<=6;k++)if(alpha(px,Math.round(py-thick*k/6))<128)edge++;
  if(edge>=2)mask[y*w+x]=1;
 }
 const found=[];
 for(let at=0;at<mask.length;at++)if(mask[at]&&!seen[at]){
  const q=[at];seen[at]=1;let minX=w,maxX=0,minY=h,maxY=0;
  for(let i=0;i<q.length;i++){const v=q[i],x=v%w,y=Math.floor(v/w);minX=Math.min(minX,x);maxX=Math.max(maxX,x);minY=Math.min(minY,y);maxY=Math.max(maxY,y);
   for(const n of [v-1,v+1,v-w,v+w])if(n>=0&&n<mask.length&&mask[n]&&!seen[n]&&Math.abs(n%w-x)<=1){seen[n]=1;q.push(n);}
  }
  if(q.length<Math.max(25,w*h*.0008))continue;
  found.push({area:q.length,x:Math.max(0,(minX*step-2)/rect.w),y:Math.max(0,1-((maxY+1)*step+2)/rect.h),z:Math.min(1,((maxX-minX+1)*step+4)/rect.w),w:Math.min(1,((maxY-minY+1)*step+4)/rect.h)});
 }
 return found.sort((a,b)=>b.area-a.area).slice(0,8).sort((a,b)=>b.y-a.y||a.x-b.x).map(({area,...r})=>r);
}
(async()=>{
 const profiles=[];
 for(const r of trees){const image=await read(r);if(image)profiles.push({name:image.name,frost:regions(image,true),painted:[]});}
 for(const r of painted){const source=await read(r.source),overlay=await read(r.overlay);if(!source||!overlay)continue;
  let p=profiles.find(p=>p.name===source.name);if(!p){p={name:source.name,frost:[],painted:[]};profiles.push(p);}p.painted=regions(overlay,false);
 }
 const file=path.join(root,'Assets/RoyalAves/Resources/WorldMapSnowPieceProfiles.json');fs.writeFileSync(file,JSON.stringify({profiles},null,2));
 console.log(JSON.stringify(profiles.map(p=>({name:p.name,frost:p.frost.length,painted:p.painted.length}))));
})().catch(e=>{console.error(e);process.exitCode=1;});
