const fs=require('fs'),path=require('path'),graph=require('./prefab-graph.cjs');
const root=path.resolve(__dirname,'../../..'),file=path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab'),text=fs.readFileSync(file,'utf8'),g=graph(text);
let patch='*** Begin Patch\n*** Update File: '+file.replaceAll('\\','/')+'\n';const changes=[];
for(const name of ['Accelerate','UseItem']){
 const base='Pets/Body/Workshop/Incubator0/Panel/Available/'+name,rect=g.blocks.get(base+'#224'),bg=g.blocks.get(base+'/rounded_btn_bg#224');
 const size=bg.text.match(/m_SizeDelta: \{x: ([\d.-]+), y: ([\d.-]+)\}/),scale=bg.text.match(/m_LocalScale: \{x: ([\d.-]+), y: ([\d.-]+), z: ([\d.-]+)\}/),position=bg.text.match(/m_AnchoredPosition: \{x: ([\d.-]+), y: ([\d.-]+)\}/);
 const rootScale=.8,width=Number(size[1])*Number(scale[1])*rootScale,gap=40,center=(name==='UseItem'?-1:1)*(width+gap)/2;
 const x=center-Number(position[1])*rootScale;
 const id=g.ids.get(base+'#224'),block=text.split(/(?=^--- !u!)/m).find(b=>new RegExp('^--- !u!224 &'+id+'\\r?\\n').test(b)).trimEnd().replace(/\r/g,'');
 const next=block.replace(/m_LocalScale: .+/,'m_LocalScale: {x: 0.8, y: 0.8, z: 1}').replace(/m_AnchoredPosition: \{x: [\d.-]+, y: ([\d.-]+)\}/,(_,y)=>'m_AnchoredPosition: {x: '+x.toFixed(6)+', y: '+y+'}');
 changes.push({order:text.indexOf(block.split('\n')[0]),block,next,name,width,center,x});
}
changes.sort((a,b)=>a.order-b.order);for(const{block,next}of changes)patch+='@@\n'+block.split('\n').map((l,i)=>l===next.split('\n')[i]?' '+l:'-'+l+'\n+'+next.split('\n')[i]).join('\n')+'\n';
console.log(JSON.stringify({patch:patch+'*** End Patch',geometry:changes.map(({name,width,center,x})=>({name,width,center,x})),outerMargin:(1040-(2*changes[0].width+40))/2}));
