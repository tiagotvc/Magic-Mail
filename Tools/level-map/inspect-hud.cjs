const fs=require('fs');
const text=fs.readFileSync('Assets/RoyalAves/Prefabs/RoyalAvesLobby.prefab','utf8');
const blocks=new Map([...text.matchAll(/^--- !u!(\d+) &(-?\d+)\r?\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)].map(m=>[m[2],{type:m[1],data:m[3]}]));
const names=new Map([...blocks].filter(([,b])=>b.type==='1').map(([id,b])=>[id,b.data.match(/m_Name: (.*)/)?.[1]]));
const rects=[...blocks].filter(([,b])=>b.type==='224');
function walk(id,indent=''){const b=blocks.get(id);if(!b)return;const n=names.get(b.data.match(/m_GameObject: \{fileID: (\d+)/)?.[1]);console.log(indent+n+' '+['m_AnchoredPosition','m_SizeDelta'].map(k=>b.data.match(new RegExp(k+': (.*)'))?.[1]).join(' '));for(const [k,v] of rects)if(v.data.includes('m_Father: {fileID: '+id+'}'))walk(k,indent+'  ');}
for(const [id,b] of rects)if(['HUD','Nav'].includes(names.get(b.data.match(/m_GameObject: \{fileID: (\d+)/)?.[1])))walk(id);
