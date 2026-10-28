// Copy original ImageGen output bytes into the project and isolated native preview.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const root=path.resolve(__dirname,'../../..');
const prompts=JSON.parse(fs.readFileSync(path.join(__dirname,'PETS-CORRECTION-V5-PROMPTS.json'),'utf8'));
const names=['exec-117997ff-d4ea-4091-9696-c70f737b8b26.png','exec-8eea1605-d7e4-4bb0-a555-a8ab40613a50.png','exec-16e205b7-ab91-4c9c-bfe4-acee40f12308.png'];
(async()=>{
 const assets=[];
 for(let i=0;i<prompts.assets.length;i++){
  const a=prompts.assets[i],source=path.join('C:/Users/Tiago Carvalho/.codex/generated_images/01a13551-4eae-78c2-b012-d5d47dd589ce',names[i]);
  const relative=`Assets/RoyalAves/Art/UI/${a.group}/RoyalPostman/Pets/${a.id}.png`;
  const m=await sharp(source).metadata();
  for(const base of [root,path.join(root,'Library/MapValidation/PreviewProject')]){const dest=path.join(base,relative);fs.mkdirSync(path.dirname(dest),{recursive:true});fs.copyFileSync(source,dest);}
  assets.push({...a,path:relative,source,width:m.width,height:m.height,sha256:crypto.createHash('sha256').update(fs.readFileSync(source)).digest('hex')});
 }
 fs.writeFileSync(path.join(__dirname,'PETS-CORRECTION-V5.json'),JSON.stringify({created:'2026-10-20',tool:'built-in image_gen',assets},null,2)+'\n');
 console.log(assets.map(a=>`${a.id}: ${a.width}x${a.height}`).join('\n'));
})().catch(e=>{console.error(e);process.exitCode=1;});
