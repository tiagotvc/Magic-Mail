const fs=require('fs'),path=require('path'),crypto=require('crypto');
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const root=path.resolve(__dirname,'../../..'),m=JSON.parse(fs.readFileSync(path.join(__dirname,'TOP-HUD-V15-MANIFEST.json'),'utf8'));
const template=fs.readFileSync(path.join(root,'Assets/RoyalAves/Art/RoyalPostmanUI2026/top-avatar-frame-v7.png.meta'),'utf8');
(async()=>{const assets=[];for(const a of m.assets){
 const file=path.join(root,a.output);if(!fs.existsSync(file+'.meta'))fs.writeFileSync(file+'.meta',template.replace(/^guid: .+$/m,'guid: '+crypto.createHash('md5').update('royal-postman-v15/'+a.id).digest('hex')));
 const {data,info}=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});let x0=info.width,y0=info.height,x1=-1,y1=-1;
 for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>200){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}
 assets.push({id:a.id,width:info.width,height:info.height,rectangles:[{x:x0,y:y0,width:x1-x0+1,height:y1-y0+1}]});
 a.actualWidth=info.width;a.actualHeight=info.height;a.native4K=info.width>=3840||info.height>=3840;
 }
 m.resolutionNote='Built-in ImageGen returned 1254 square / 2172x724 panel despite 4K prompts. Explicit 3840 resubmission also returned 1254. No artificial upscaling or pixel modifications performed.';
 m.resolutionRetry={source:'C:/Users/Tiago Carvalho/.codex/generated_images/01a13551-4eae-78c2-b012-d5d47dd589ce/exec-42dab3fc-65b0-4edf-9086-eb2732ce93d0.png',actualWidth:1254,actualHeight:1254,used:false};
 fs.writeFileSync(path.join(__dirname,'TOP-HUD-V15-MANIFEST.json'),JSON.stringify(m,null,2));fs.writeFileSync(path.join(__dirname,'TOP-HUD-V15-BOUNDS.json'),JSON.stringify({assets},null,2));console.log('8 original PNGs and actual native dimensions recorded; no raster edits.');
})().catch(e=>{console.error(e);process.exit(1);});

