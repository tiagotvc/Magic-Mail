// Extract the user's supplied reference unchanged for local visual comparison.
// No resizing, retouching, or pixel editing is performed.
const fs=require('node:fs'),path=require('node:path');
const source='C:/Users/Tiago Carvalho/.codex/sessions/2026/10/13/rollout-2026-10-13T09-51-23-01a13551-4eae-78c2-b012-d5d47dd589ce.jsonl';
const needle=process.argv[2]||'SUPERFÍCIE DAS ABAS',prefix=process.argv[3]||'reference-upload';
const fd=fs.openSync(source,'r'),size=fs.fstatSync(fd).size,length=Math.min(size,64*1024*1024),buffer=Buffer.alloc(length);
fs.readSync(fd,buffer,0,length,size-length);fs.closeSync(fd);
const lines=buffer.toString('utf8').split('\n');
let target;
for(const line of lines){let entry;try{entry=JSON.parse(line);}catch{continue;}
 const message=entry.payload;
 if(message?.role==='user'&&Array.isArray(message.content)&&message.content.some(c=>typeof c.text==='string'&&c.text.includes(needle)))target=message;
}
if(!target)throw Error('Reference message not present in the recent session tail');
let n=0;
const output=path.join(__dirname,'previews/navigation-reference');fs.mkdirSync(output,{recursive:true});
for(const item of target.content){
 const url=typeof item.image_url==='string'?item.image_url:item.image_url?.url;
 if(url?.startsWith('data:image/')){
  const match=url.match(/^data:image\/([^;]+);base64,(.*)$/s);if(!match)continue;
  const file=path.join(output,prefix+'-'+n+++'.'+(match[1]==='jpeg'?'jpg':match[1]));
  fs.writeFileSync(file,Buffer.from(match[2],'base64'));process.stdout.write(JSON.stringify({file})+'\n');
 }else if(item.type?.includes('image'))process.stdout.write(JSON.stringify({type:item.type,keys:Object.keys(item),urlType:typeof item.image_url})+'\n');
}
