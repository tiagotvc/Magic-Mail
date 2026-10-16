const fs=require('node:fs'),readline=require('node:readline'),path=require('node:path');
const source='C:/Users/Tiago Carvalho/.codex/sessions/2026/10/13/rollout-2026-10-13T09-51-23-01a13551-4eae-78c2-b012-d5d47dd589ce.jsonl';
(async()=>{
 let previousImages;
 for await(const line of readline.createInterface({input:fs.createReadStream(source),crlfDelay:Infinity})){
  if(!/"role"\s*:\s*"user"/.test(line.slice(0,500)))continue;
  let entry;try{entry=JSON.parse(line);}catch{continue;}const m=entry.payload;
  if(m?.role!=='user'||!Array.isArray(m.content))continue;
  const text=m.content.filter(c=>c.type==='input_text').map(c=>c.text).join(' ');
  const images=m.content.map(c=>typeof c.image_url==='string'?c.image_url:c.image_url?.url).filter(s=>s?.startsWith('data:image/'));
  if(text.includes('glossy as partes')&&previousImages){
   const out=path.join(__dirname,'concepts/references');fs.mkdirSync(out,{recursive:true});
   for(const i of [2,4]){if(!previousImages[i])continue;const match=previousImages[i].match(/^data:image\/([^;]+);base64,(.*)$/s);const dest=path.join(out,'crash-user-'+i+'.'+(match[1]==='jpeg'?'jpg':match[1]));fs.writeFileSync(dest,Buffer.from(match[2],'base64'));console.log(dest);}
   break;
  }
  if(images.length)previousImages=images;
 }
})();
