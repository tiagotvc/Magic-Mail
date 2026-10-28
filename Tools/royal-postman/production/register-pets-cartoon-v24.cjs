const fs=require('fs'),path=require('path'),crypto=require('crypto');
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const root=path.resolve(__dirname,'../../..');
(async()=>{
 const [id,source]=process.argv.slice(2),file=path.join(__dirname,'PETS-CARTOON-V24.json');
 const m=JSON.parse(fs.readFileSync(file,'utf8')),a=m.assets.find(x=>x.id===id);
 if(!a)throw Error('Unknown asset '+id);
 const rel=`Assets/RoyalAves/Art/UI/${a.group}/RoyalPostman/PetsCartoon/${id}.png`,dest=path.join(root,rel);
 if(fs.existsSync(dest))throw Error('Asset already exists; version it instead of overwriting '+id);
 fs.mkdirSync(path.dirname(dest),{recursive:true});fs.copyFileSync(source,dest);
 const meta=await sharp(dest).metadata(),{data,info}=await sharp(dest).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 let transparent=0,partial=0,left=info.width,right=-1,top=info.height,bottom=-1;
 for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++){let alpha=data[(y*info.width+x)*4+3];if(alpha===0)transparent++;if(alpha>0&&alpha<255)partial++;if(alpha>8){left=Math.min(left,x);right=Math.max(right,x);top=Math.min(top,y);bottom=Math.max(bottom,y);}}
 Object.assign(a,{status:'generated',source,path:rel,width:meta.width,height:meta.height,sha256:crypto.createHash('sha256').update(fs.readFileSync(dest)).digest('hex'),hasAlpha:meta.hasAlpha,transparentFraction:transparent/(info.width*info.height),partialAlphaFraction:partial/(info.width*info.height),boundsTopLeft:{left,top,width:right-left+1,height:bottom-top+1},prompt:`Tools/royal-postman/production/pets-cartoon-v24-prompts/${id}.txt`});
 fs.writeFileSync(dest+'.meta','fileFormatVersion: 2\nguid: '+crypto.createHash('sha256').update(rel).digest('hex').slice(0,32)+'\n');
 fs.writeFileSync(file,JSON.stringify(m,null,2)+'\n');console.log(JSON.stringify(a));
})().catch(e=>{console.error(e);process.exitCode=1;});
