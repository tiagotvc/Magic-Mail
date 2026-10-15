const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const path=require('node:path');
(async()=>{
 const fs=require('node:fs');
 const blocks=fs.readFileSync(path.join(__dirname,'../../Assets/RoyalAves/Prefabs/RoyalAvesLobby.prefab'),'utf8').split(/\n--- /);
 const byId=new Map(blocks.map(b=>[b.match(/&(-?\d+)/)?.[1],b]));
 const nav=blocks.find(b=>b.startsWith('!u!1 ')&&/^  m_Name: Nav$/m.test(b));
 const navRt=byId.get(nav.match(/component: \{fileID: (\d+)\}/)[1]);
 for(const m of navRt.matchAll(/^  - \{fileID: (\d+)\}/gm)){
  const rt=byId.get(m[1]),go=byId.get(rt.match(/m_GameObject: \{fileID: (\d+)\}/)[1]);
  const iconRt=[...rt.matchAll(/^  - \{fileID: (\d+)\}/gm)].map(v=>byId.get(v[1])).find(b=>/^  m_Name: Icon$/m.test(byId.get(b.match(/m_GameObject: \{fileID: (\d+)\}/)[1])));
  console.log(JSON.stringify({tab:go.match(/m_Name: (.*)/)[1],scale:rt.match(/m_LocalScale: (.*)/)[1],iconScale:iconRt?.match(/m_LocalScale: (.*)/)[1]}));
 }
 const art=path.resolve(__dirname,'../../Assets/RoyalAves/Art/MapKitV3/MapUI');
 for(const id of ['map','events','ranking','teams','collection']){
  const {data,info}=await sharp(path.join(art,'nav-'+id+'-v22.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let x0=info.width,y0=info.height,x1=0,y1=0;
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>128){x0=Math.min(x,x0);x1=Math.max(x,x1);y0=Math.min(y,y0);y1=Math.max(y,y1);}
  console.log(JSON.stringify({id,width:info.width,height:info.height,ink:[x0,y0,x1,y1],scaleAt170:[(x1-x0+1)*170/Math.max(info.width,info.height),(y1-y0+1)*170/Math.max(info.width,info.height)]}));
 }
 const {data,info}=await sharp(path.join(__dirname,'previews/navigation-reference/reference-upload-1.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 function pixel(x,y){const n=(y*info.width+x)*4;return [...data.subarray(n,n+3)];}
 const points=[[420,135],[420,200],[420,270],[420,335],[650,136],[650,210],[650,330],[380,136],[380,210],[380,330],[180,130],[150,150],[151,200],[500,106],[500,110],[500,114],[500,118],[500,123],[405,190],[407,190],[409,190]];
 console.log(JSON.stringify({referenceSize:[info.width,info.height],samples:points.map(([x,y])=>({x,y,rgb:pixel(x,y)}))}));
 const actual=await sharp(path.join(__dirname,'previews/navigation-reference/tab-0.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 const compare=points.filter(([x,y])=>y>=130&&x<700).map(([x,y])=>{
  const u=Math.round((x-144)*1080/1300),v=Math.round((y-8.7)*1080/1300),n=(v*actual.info.width+u)*4;
  return {reference:[x,y],target:pixel(x,y),actual:[...actual.data.subarray(n,n+3)]};
 });
 let bb=[216,285,0,0];for(let y=0;y<235;y++)for(let x=0;x<216;x++){
  const n=(y*actual.info.width+x)*4,[r,g,b]=actual.data.subarray(n,n+3);
  if(g>r+25&&b>r+15&&g>115){bb[0]=Math.min(bb[0],x);bb[1]=Math.min(bb[1],y);bb[2]=Math.max(bb[2],x);bb[3]=Math.max(bb[3],y);}
 }
 console.log(JSON.stringify({matchedSamples:compare,mapInk:bb,goldTop:80}));
})();
