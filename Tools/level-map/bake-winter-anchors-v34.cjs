// Read existing alpha only. Bake attachment coordinates; no image pixels are changed.
const fs=require('node:fs'),path=require('node:path');
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),kitPath=path.join(root,'Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset');
function files(dir){return fs.readdirSync(dir,{withFileTypes:true}).flatMap(e=>e.isDirectory()?files(path.join(dir,e.name)):[path.join(dir,e.name)]);}
const imageMeta=new Map();
for(const dir of ['Assets/RoyalAves/Art/MapKitV2','Assets/RoyalAves/Art/MapKitV3','Assets/RoyalAves/Art/Maps'])
 for(const p of files(path.join(root,dir)).filter(p=>p.endsWith('.png.meta'))){const m=fs.readFileSync(p,'utf8').match(/^guid: (.+)$/m);if(m)imageMeta.set(m[1],p);}
const refs=new Map();
function ref(text){for(const m of text.matchAll(/fileID: (-?\d+), guid: ([a-f0-9]+), type: 3/g))if(m[1]!=='0')refs.set(m[2]+':'+m[1],{id:m[1],guid:m[2]});}
for(const p of files(path.join(root,'Assets/RoyalAves/Resources/MapLibrary/Arvores')).filter(p=>p.endsWith('.asset')))ref(fs.readFileSync(p,'utf8'));
const kit=fs.readFileSync(kitPath,'utf8');
for(const name of ['tree','pine','postalPine','postalTrees','expressiveTrees','entranceTrees']){
 const m=kit.match(new RegExp('^  '+name+':.*(?:\\r?\\n  - .*)*','m'));if(m)ref(m[0]);
}
(async()=>{
 const rows=[],report=[];
 for(const {id,guid} of refs.values()){
  const metaPath=imageMeta.get(guid);if(!metaPath)continue;
  const meta=fs.readFileSync(metaPath,'utf8'),png=metaPath.slice(0,-5);
  const {data,info}=await sharp(png).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let rect={x:0,y:0,width:info.width,height:info.height};
  if(id!=='21300000'){
   const sections=meta.split(/    - serializedVersion:/).slice(1);
   const section=sections.find(s=>new RegExp('internalID: '+id+'(?:\\r?\\n|$)').test(s));if(!section)continue;
   const r=section.match(/rect:[\s\S]*?x: (\d+)[\s\S]*?y: (\d+)[\s\S]*?width: (\d+)[\s\S]*?height: (\d+)/);
   if(!r)continue;rect={x:+r[1],y:+r[2],width:+r[3],height:+r[4]};
  }
  const top=info.height-rect.y-rect.height;
  const alpha=(x,y)=>data[((top+y)*info.width+rect.x+x)*4+3]>=128;
  let lo=rect.width,hi=0;
  for(let y=0;y<rect.height;y++)for(let x=0;x<rect.width;x++)if(alpha(x,y)){lo=Math.min(lo,x);hi=Math.max(hi,x);}
  if(hi<=lo)continue;
  function anchor(t){
   const x=Math.round(lo+(hi-lo)*t),minimum=Math.max(6,Math.floor(rect.height*.009));let run=0,start=-1;
   const greenCanopy=/green|pine|canopy|palm/i.test(png);
   for(let y=rect.height-1;y>=0;y--){
    const i=((top+y)*info.width+rect.x+x)*4,r=data[i],g=data[i+1],b=data[i+2];
    // Curved trunks can reach a side column. For green/teal trees, ignore warm wood
    // and its cream outline in the lower half, so the support is foliage, not a root.
    const wood=greenCanopy&&y>rect.height*.45&&((r>g*1.12&&r>b*1.5)||(r>170&&g>140&&b>110&&Math.max(r,g,b)-Math.min(r,g,b)<100));
    if(alpha(x,y)&&!wood){if(run===0)start=y;run++;if(run>=minimum)return {x:(x+.5)/rect.width,y:1-(start-Math.max(2,rect.height*.006))/rect.height};}else run=0;
   }
   throw Error('No supported lower branch in '+png);
  }
  const left=anchor(.23),right=anchor(.77);
  rows.push('  - source: {fileID: '+id+', guid: '+guid+', type: 3}\n    leftIce: {x: '+left.x.toFixed(7)+', y: '+left.y.toFixed(7)+'}\n    rightIce: {x: '+right.x.toFixed(7)+', y: '+right.y.toFixed(7)+'}');
  report.push({image:path.relative(root,png),id,left,right});
 }
 const block='  treeDecorationProfiles:\n'+rows.join('\n')+'\n';
 const pattern=/^  treeDecorationProfiles:.*\r?\n(?:  - source:.*\r?\n    leftIce:.*\r?\n    rightIce:.*\r?\n)*/m;
 const fresh=fs.readFileSync(kitPath,'utf8');fs.writeFileSync(kitPath,pattern.test(fresh)?fresh.replace(pattern,block):fresh.trimEnd()+'\n'+block);
 const out=path.join(__dirname,'previews/winter-v34');fs.mkdirSync(out,{recursive:true});fs.writeFileSync(path.join(out,'tree-anchors.json'),JSON.stringify(report,null,2));
 console.log(JSON.stringify({profiles:report.length,images:'read-only alpha inspection; no PNG edits'}));
})().catch(e=>{console.error(e);process.exitCode=1;});
