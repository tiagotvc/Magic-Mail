// Install only the six verified divider components, preserving all other authored prefab edits.
const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'../../..'),preview=path.join(root,'Library/MapValidation/PreviewProject');
const relative='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab';
const output=path.join(root,'Library/MapValidation/pets-mock-v2');
if(!fs.readFileSync(path.join(output,'validation.txt'),'utf8').startsWith('PASS:'))throw Error('Native validation has not passed');
const blocks=t=>t.split(/(?=^--- !u!)/m);
const id=b=>b.match(/^--- !u!\d+ &(-?\d+)/m)?.[1];
const native=blocks(fs.readFileSync(path.join(preview,relative),'utf8'));
const names=new Map(native.filter(b=>b.startsWith('--- !u!1 ')).map(b=>[id(b),b.match(/^  m_Name: (.+)$/m)?.[1]?.trim()]));
const current=fs.readFileSync(path.join(root,relative),'utf8');
const main=blocks(current),index=new Map(main.map((b,i)=>[id(b),i]));
const allowed=new Set(['LeftTip','RightTip','LeftLine','RightLine']);let changed=0;
for(const block of native){
 const name=names.get(block.match(/^  m_GameObject: \{fileID: (-?\d+)\}/m)?.[1]);
 if(!allowed.has(name))continue;
 let keys=[];
 if(block.startsWith('--- !u!224 '))keys=['m_LocalPosition','m_AnchoredPosition','m_SizeDelta'];
 else if(name.endsWith('Line')&&block.startsWith('--- !u!114 ')&&block.includes('m_PixelsPerUnitMultiplier:'))keys=['m_PixelsPerUnitMultiplier'];
 if(!keys.length)continue;
 if(name.endsWith('Line')&&block.startsWith('--- !u!224 ')){
  const height=Number(block.match(/^  m_SizeDelta: .*y: ([\d.]+)/m)?.[1]);
  if(Math.abs(height-11.44)>.001||!Number.isFinite(height))throw Error('Native divider thickness was not validated');
 }
 const at=index.get(id(block));if(at===undefined)throw Error('Authored divider component changed: '+name);
 for(const key of keys){const pattern=new RegExp('^  '+key+': .+$','m'),line=block.match(pattern)?.[0];if(!line||!pattern.test(main[at]))throw Error('Missing divider field: '+name+'/'+key);main[at]=main[at].replace(pattern,line);}
 changed++;
}
if(changed!==6)throw Error('Unexpected divider component count: '+changed);
const review=path.join(root,'Tools/royal-postman/production/review/pets-divider-v7');fs.mkdirSync(review,{recursive:true});
fs.writeFileSync(path.join(root,'Library/MapValidation/pets-divider-v7/before-apply.prefab'),current);
fs.writeFileSync(path.join(root,relative),main.join(''));
for(const name of ['pets-empty-720.png','pets-active-720.png','pets-active-detail-2160.png','validation.txt'])fs.copyFileSync(path.join(output,name),path.join(review,name));
fs.appendFileSync(path.join(review,'validation.txt'),'Selective install: only four divider RectTransforms and two line sampling scales changed. Bar/tips are 2.2x thicker, tips keep uniform aspect, central ornament unchanged.\n');
console.log('Applied six divider components only; current screen layout and source assets preserved.');
