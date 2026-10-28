const graph=text=>{
 const raw=new Map(),names=new Map(),transforms=new Map(),owners=new Map();
 for(const b of text.split(/(?=^--- !u!)/m)){
  const h=b.match(/^--- !u!(\d+) &(-?\d+)/m);if(!h)continue;
  const go=h[1]==='1'?h[2]:b.match(/^  m_GameObject: \{fileID: (-?\d+)\}/m)?.[1];
  const item={text:b.replace(/\r/g,'').trim(),go,kind:h[1]};raw.set(h[2],item);
  if(h[1]==='1')names.set(go,b.match(/^  m_Name: (.+)$/m)?.[1]?.trim());
  if(h[1]==='224'){transforms.set(go,b.match(/^  m_Father: \{fileID: (-?\d+)\}/m)?.[1]);owners.set(h[2],go);}
 }
 const objectPath=go=>{const parts=[];for(let n=0;go&&n<32;n++){parts.unshift(names.get(go)||go);go=owners.get(transforms.get(go));}return parts.join('/');};
 // Unity can reassign IDs to identically named labels on save. Compare semantic paths and references.
 const aliases=new Map();
 for(const[id,item]of raw){const script=item.text.match(/m_Script: .*guid: (\w+)/)?.[1];aliases.set(id,objectPath(item.go)+'#'+item.kind+(script?':'+script:''));}
 const blocks=new Map();
 for(const[id,item]of raw){const key=aliases.get(id);if(blocks.has(key))throw Error('Ambiguous authored component path: '+key);
  const normalized=item.text.replace(/^--- !u!\d+ &-?\d+/,'--- '+key).replace(/fileID: (-?\d+)/g,(match,ref)=>aliases.has(ref)?'fileID: '+aliases.get(ref):match);
  blocks.set(key,{...item,text:normalized,path:objectPath(item.go)});
 }
 return{blocks,ids:new Map([...aliases].map(([id,key])=>[key,id])),within(item){return ['LeftTip','RightTip','LeftLine','RightLine'].some(n=>item?.path==='Pets/Divider/'+n);}};
};

module.exports=graph;
