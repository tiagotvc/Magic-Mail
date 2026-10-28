// One-time narrow data migration. Recorded values protect later hand tuning from reruns.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..'),report=path.join(__dirname,'map-palette-v94-migration.json');
if(fs.existsSync(report)){console.log('V94 tuning already applied; retaining subsequent manual edits.');process.exit(0);}
const read=f=>fs.readFileSync(path.join(root,f),'utf8'),write=(f,s)=>fs.writeFileSync(path.join(root,f),s),hash=s=>crypto.createHash('sha256').update(s).digest('hex');
const files=[],sizes=[];
const islands='Assets/RoyalAves/Resources/RoyalAvesWorldMapIslands.asset',oldIslands=read(islands);
const recipeArea=oldIslands.slice(oldIslands.indexOf('  recipes:'),oldIslands.indexOf('  islands:'));
const houses=new Set([...recipeArea.matchAll(/    buildings:\r?\n((?:    -[^\n]*\r?\n)+)/g)].flatMap(m=>[...m[1].matchAll(/guid: ([a-f0-9]{32})/g)].map(x=>x[1])));
if(houses.size<3)throw new Error('Missing building recipe references');
let newIslands=oldIslands.replace(/^(  vibrance:) 10\r?$/m,'$1 4').replace(/^(    buildingSize:) ([\d.]+)(\r?)$/gm,(m,key,n,cr)=>{const next=Number((Number(n)*.8).toFixed(5));sizes.push({file:islands,from:Number(n),to:next});return key+' '+next+cr;});
files.push({file:islands,before:hash(oldIslands),after:hash(newIslands)});
const layout='Assets/RoyalAves/Resources/RoyalAvesMission100Layout.asset',oldLayout=read(layout);let count=0;
const newLayout=oldLayout.split(/(?=^ {4,}- id:)/m).map(block=>{
 const sprite=block.match(/^\s+sprite: \{[^\n]*guid: ([a-f0-9]{32})/m)?.[1];if(!houses.has(sprite)||!/^\s+kind: 2\r?$/m.test(block))return block;
 const before=block;block=block.replace(/^(\s+height:) ([\d.]+)(\r?)$/m,(m,key,n,cr)=>{const next=Number((Number(n)*.8).toFixed(5));sizes.push({file:layout,id:before.match(/- id: (.*)/)?.[1].trim(),from:Number(n),to:next});return key+' '+next+cr;});
 if(before.replace(/^(\s+height:) [\d.]+/m,'$1')!==block.replace(/^(\s+height:) [\d.]+/m,'$1'))throw new Error('Unexpected authored layout edit');count++;return block;
}).join('');
if(count!==2)throw new Error('Expected exactly two authored opening houses, got '+count);
files.push({file:layout,before:hash(oldLayout),after:hash(newLayout)});
const library='Assets/RoyalAves/Resources/MapLibrary/Construcoes';const edits=[];
for(const name of fs.readdirSync(path.join(root,library)).filter(n=>n.endsWith('.asset'))){const file=library+'/'+name,old=read(file),sprite=old.match(/sprite: \{[^\n]*guid: ([a-f0-9]{32})/)?.[1];if(!houses.has(sprite))continue;
 const next=old.replace(/^(  size:) ([\d.]+)(\r?)$/m,(m,key,n,cr)=>{const value=Number((Number(n)*.8).toFixed(5));sizes.push({file,from:Number(n),to:value});return key+' '+value+cr;});edits.push([file,next]);files.push({file,before:hash(old),after:hash(next)});
}
write(islands,newIslands);write(layout,newLayout);for(const [file,text]of edits)write(file,text);
fs.writeFileSync(report,JSON.stringify({date:'2026-10-18',houseScale:.8,ground:'sage/olive, muted cream/sand, neutral path edge shadows',scope:'Two authored house heights only; first-island anchors and all other object fields unchanged. Seven automatic recipe sizes plus matching library defaults. Vibrance 10 to 4.',files,sizes},null,2)+'\n');
console.log(JSON.stringify({authoredHouses:count,recipeBuildings:7,libraryDefaults:edits.length,vibrance:4,firstIslandAnchors:'unchanged'}));
