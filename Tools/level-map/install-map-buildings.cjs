// Install independent generated PNGs and their library records; never save map layouts.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const root=path.resolve(__dirname,'../..');
const manifestName=process.argv[2],version=process.argv[3];
if(!/^map-library-v\d+-assets\.json$/.test(manifestName||'')||!/^v\d+$/.test(version||''))throw Error('Expected manifest filename and version');
const jobs=JSON.parse(fs.readFileSync(path.join(__dirname,manifestName),'utf8')).jobs;
const absolute=p=>path.join(root,p);
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const tracked=['RoyalAvesWorldMapIslands','RoyalAvesWorldMapLayout','RoyalAvesWorldMapSimpleLayout'].map(n=>'Assets/RoyalAves/Resources/'+n+'.asset');
const hashes=()=>Object.fromEntries(tracked.map(p=>[p,hash(absolute(p))]));
const before=hashes();
const artTemplate=fs.readFileSync(absolute('Assets/RoyalAves/Art/MapKitV3/Library/Construcoes/postal-dock-v16.png.meta'),'utf8');
const itemTemplate=fs.readFileSync(absolute('Assets/RoyalAves/Resources/MapLibrary/Construcoes/arte-postal-dock-v16.asset'),'utf8');
const metaTemplate=fs.readFileSync(absolute('Assets/RoyalAves/Resources/MapLibrary/Construcoes/arte-postal-dock-v16.asset.meta'),'utf8');
const guid=()=>crypto.randomBytes(16).toString('hex');
async function run(){
 const checks=[];
 for(const job of jobs){
  const tree=job.kind==='tree'&&job.folder==='Library/Arvores';
  if(!/^[a-z0-9-]+$/.test(job.id)||job.file!==job.id+'.png'||(!tree&&job.folder!=='Library/Construcoes')||!job.source||!fs.existsSync(job.source))throw Error('Incomplete or invalid job: '+job.id);
  const category=tree?'Arvores':'Construcoes',artFolder='Assets/RoyalAves/Art/MapKitV3/Library/'+category+'/',catalogFolder='Assets/RoyalAves/Resources/MapLibrary/'+category+'/';
  fs.mkdirSync(absolute(artFolder),{recursive:true});fs.mkdirSync(absolute(catalogFolder),{recursive:true});
  const {data,info}=await sharp(job.source).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let clear=0,visible=0,x0=info.width,y0=info.height,x1=0,y1=0;
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++){
   const alpha=data[(y*info.width+x)*4+3];if(alpha===0)clear++;
   if(alpha>=200){visible++;x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}
  }
  if(clear<info.width*info.height*.15||visible<10000)throw Error('Invalid transparent sprite: '+job.id);
  const aspect=(x1-x0+1)/(y1-y0+1),size=Math.round(Math.min(job.size,1.8/aspect)*100)/100;
  const art=artFolder+job.file,catalog=catalogFolder+'arte-'+job.id+'.asset';
  const sourceHash=hash(job.source);
  if(fs.existsSync(absolute(art))&&hash(absolute(art))!==sourceHash)throw Error('Refusing to replace an existing image: '+art);
  const savedRecord=fs.existsSync(absolute(catalog))?fs.readFileSync(absolute(catalog),'utf8'):'';
  const artGuid=fs.existsSync(absolute(art+'.meta'))?fs.readFileSync(absolute(art+'.meta'),'utf8').match(/^guid: (.+)$/m)[1]:savedRecord.match(/sprite: \{fileID: [^,]+, guid: ([a-f0-9]+)/)?.[1]||guid();
  if(!fs.existsSync(absolute(catalog))){
   if(!fs.existsSync(absolute(catalog+'.meta')))fs.writeFileSync(absolute(catalog+'.meta'),metaTemplate.replace(/^guid: .+$/m,'guid: '+guid()));
   const record=itemTemplate.replaceAll('arte-postal-dock-v16','arte-'+job.id).replace('title: Cais postal','title: '+job.title).replace('f44e419a7aca4c208c9c79911d243560',artGuid).replace('size: 1.45','size: '+size).replace('category: 0','category: '+(tree?1:0)).replace('kind: 2','kind: '+(tree?0:2));
   fs.writeFileSync(absolute(catalog),record);
  }
  if(!fs.existsSync(absolute(art+'.meta'))){
   const meta=artTemplate.replace(/^guid: .+$/m,'guid: '+artGuid).replaceAll('postal-dock-v16',job.id).replace(/width: 1312/,'width: '+info.width).replace(/height: 1199/,'height: '+info.height);
   fs.writeFileSync(absolute(art+'.meta'),meta);
  }
  if(!fs.existsSync(absolute(art)))fs.copyFileSync(job.source,absolute(art));
  checks.push({file:art,catalog,width:info.width,height:info.height,clearPixels:clear,visiblePixels:visible,alphaBounds:{x:x0,y:y0,width:x1-x0+1,height:y1-y0+1},suggestedHeight:size,sourceHash});
 }
 const after=hashes();if(JSON.stringify(before)!==JSON.stringify(after))throw Error('Authoring data changed during install');
 const output=path.join(__dirname,'previews',version);fs.mkdirSync(output,{recursive:true});
 fs.writeFileSync(path.join(output,'asset-validation.json'),JSON.stringify({checks,authoring:'unchanged',hashes:after},null,2));
 console.log(JSON.stringify({assets:checks.length,pixelsAndAlpha:'copied without modification',authoring:'unchanged'}));
}
run().catch(error=>{console.error(error);process.exitCode=1;});
