// Preserve ImageGen PNG bytes; inspect source bounds, record provenance, stage for native import.
const fs=require('fs'),path=require('path'),crypto=require('crypto');
const sharp=require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const root=path.resolve(__dirname,'../../..');
const prompts=JSON.parse(fs.readFileSync(path.join(__dirname,'PETS-REFINEMENT-V4-PROMPTS.json'),'utf8'));
const names=['exec-8c0f250e-c13f-415c-a765-6eb0f9118614.png','exec-e25e1227-925e-4a6f-84c9-151f4255d445.png','exec-535f1033-c996-46f6-98d1-2b2260c660a0.png','exec-630955b3-9bf6-4d8d-ab39-241e04df884d.png','exec-2883bc65-4bfc-4976-af75-720dc969999a.png'];
(async()=>{
 const assets=[];
 for(let i=0;i<prompts.assets.length;i++){
  const a=prompts.assets[i],source=path.join('C:/Users/Tiago Carvalho/.codex/generated_images/01a13551-4eae-78c2-b012-d5d47dd589ce',names[i]);
  const relative=`Assets/RoyalAves/Art/UI/${a.group}/RoyalPostman/Pets/${a.id}.png`;
  const m=await sharp(source).metadata();
  for(const base of [root,path.join(root,'Library/MapValidation/PreviewProject')]){const dest=path.join(base,relative);fs.mkdirSync(path.dirname(dest),{recursive:true});fs.copyFileSync(source,dest);}
  assets.push({...a,path:relative,source,width:m.width,height:m.height,sha256:crypto.createHash('sha256').update(fs.readFileSync(source)).digest('hex')});
 }
 fs.writeFileSync(path.join(__dirname,'PETS-REFINEMENT-V4.json'),JSON.stringify({created:'2026-10-20',tool:'built-in image_gen',assets},null,2)+'\n');
 console.log(assets.map(a=>`${a.id}: ${a.width}x${a.height}`).join('\n'));
})().catch(e=>{console.error(e);process.exitCode=1;});
