const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),manifest=require('./snow-glossy-v33.json');
const template=fs.readFileSync(path.join(root,'Assets/RoyalAves/Art/MapKitV3/Library/Arvores/green-rounded-tree-v27.png.meta'),'utf8');
(async()=>{
 const rows=[],report=[];
 for(const job of manifest.jobs){
  const data=fs.readFileSync(job.source),target=path.join(root,'Assets/RoyalAves/Art/MapKitV3/Christmas',job.file);
  if(fs.existsSync(target)&&!fs.readFileSync(target).equals(data))throw Error('Refusing different existing snow art '+target);
  const name=path.basename(job.file,'.png'),guid=crypto.createHash('md5').update('MagicMail:snow-v33:'+job.id).digest('hex');
  const sourceGuid=fs.readFileSync(path.join(root,job.base+'.meta'),'utf8').match(/^guid: (.+)$/m)[1];
  fs.copyFileSync(job.source,target);
  fs.writeFileSync(target+'.meta',template.replace(/^guid: .+$/m,'guid: '+guid).replaceAll('green-rounded-tree-v27',name));
  rows.push('  - source: {fileID: 21300000, guid: '+sourceGuid+', type: 3}\n    overlay: {fileID: 21300000, guid: '+guid+', type: 3}');
  const meta=await sharp(data).metadata(),original=await sharp(path.join(root,job.base)).metadata();
  const stats=await sharp(data).stats();
  if(!meta.hasAlpha||stats.channels[3].min!==0)throw Error('Snow lacks transparent alpha');
  report.push({file:path.relative(root,target),width:meta.width,height:meta.height,baseWidth:original.width,baseHeight:original.height,sha256:crypto.createHash('sha256').update(data).digest('hex'),pixels:'original generated PNG copied unchanged'});
 }
 const asset=path.join(root,'Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset');let kit=fs.readFileSync(asset,'utf8');
 const block='  paintedSnow:\n'+rows.join('\n')+'\n';
 const pattern=/^  paintedSnow:.*\r?\n(?:  - source:.*\r?\n    overlay:.*\r?\n)*/m;
 kit=pattern.test(kit)?kit.replace(pattern,block):kit.trimEnd()+'\n'+block;fs.writeFileSync(asset,kit);
 const output=path.join(__dirname,'previews/snow-v33');fs.mkdirSync(output,{recursive:true});
 fs.writeFileSync(path.join(output,'art-validation.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report));
})().catch(e=>{console.error(e);process.exitCode=1;});
