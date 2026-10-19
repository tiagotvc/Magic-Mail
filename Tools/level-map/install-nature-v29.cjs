// Install generated originals byte for byte; inspect alpha without retouching pixels.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const root=path.resolve(__dirname,'../..'),manifest=require('./nature-glossy-v29.json');
const template=fs.readFileSync(path.join(root,'Assets/RoyalAves/Art/MapKitV3/Library/Arvores/green-rounded-tree-v27.png.meta'),'utf8');
const itemTemplate=fs.readFileSync(path.join(root,'Assets/RoyalAves/Resources/MapLibrary/Arvores/arte-teal-wonky-pine-v20.asset'),'utf8');
const digest=data=>crypto.createHash('sha256').update(data).digest('hex');
(async()=>{
 const validation=[];
 for(const job of manifest.jobs){
  const bytes=fs.readFileSync(job.source),target=path.join(root,'Assets/RoyalAves/Art/MapKitV3/Library',job.category,job.file);
  const {data,info}=await sharp(bytes).ensureAlpha().raw().toBuffer({resolveWithObject:true});let clear=0;
  for(let i=3;i<data.length;i+=4)if(data[i]===0)clear++;
  if(clear/info.width/info.height<.10)throw Error('No clear alpha margin: '+job.id);
  if(fs.existsSync(target)&&digest(fs.readFileSync(target))!==digest(bytes))throw Error('Refusing to overwrite different art: '+target);
  const name=path.basename(job.file,'.png'),guid=crypto.createHash('md5').update('MagicMail:nature-v29:'+job.id).digest('hex');
  fs.writeFileSync(target+'.meta',template.replace(/^guid: .+$/m,'guid: '+guid).replaceAll('green-rounded-tree-v27',name));
  fs.copyFileSync(job.source,target);
  const item=path.join(root,'Assets/RoyalAves/Resources/MapLibrary',job.category,'arte-'+name+'.asset');
  const yaml=itemTemplate.replaceAll('arte-teal-wonky-pine-v20','arte-'+name).replace(/^  title: .+$/m,'  title: '+job.title)
   .replace('guid: 1473c66de9cc45c4ae60afe5682bfaa7','guid: '+guid).replace(/^  category: \d+$/m,'  category: '+(job.category==='Arvores'?1:2))
   .replace(/^  kind: \d+$/m,'  kind: '+job.kind).replace(/^  size: .+$/m,'  size: '+job.size);
  fs.writeFileSync(item,yaml);
  const itemGuid=crypto.createHash('md5').update('MagicMail:nature-v29:item:'+job.id).digest('hex');
  fs.writeFileSync(item+'.meta','fileFormatVersion: 2\nguid: '+itemGuid+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n');
  validation.push({id:job.id,file:path.relative(root,target),width:info.width,height:info.height,transparentFraction:clear/info.width/info.height,sha256:digest(bytes),pixels:'unchanged'});
 }
 const out=path.join(__dirname,'previews/nature-v29');fs.mkdirSync(out,{recursive:true});
 fs.writeFileSync(path.join(out,'art-validation.json'),JSON.stringify(validation,null,2));console.log(JSON.stringify(validation));
})().catch(error=>{console.error(error);process.exitCode=1;});
