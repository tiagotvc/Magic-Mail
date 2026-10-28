const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');process.chdir(path.resolve(__dirname,'../..'));
const manifestPath='Tools/level-map/auto-islands-v70-manifest.json';const m=JSON.parse(fs.readFileSync(manifestPath,'utf8'));
m.assets.gifts=JSON.parse(fs.readFileSync('Tools/level-map/auto-islands-gifts-v70-manifest.json','utf8'));
const folder='Assets/RoyalAves/Art/MapKitV3/AutoIslandsV70';fs.mkdirSync(folder,{recursive:true});
const guid=()=>crypto.randomBytes(16).toString('hex');
if(!fs.existsSync(folder+'.meta'))fs.writeFileSync(folder+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
const specs={inkPuddle:['ink-puddle-overhead-v70','Deep magical ink puddle',2,9,.82],winterCabin:['christmas-post-office-front-v70','Christmas post office (front)',0,2,1.62],winterDepot:['christmas-dispatch-front-v70','Christmas dispatch (front)',0,2,1.82],stampWorkshop:['stamp-workshop-front-v70','Stamp workshop (front)',0,2,1.6],gifts:['christmas-parcels-overhead-v70','Christmas parcels (overhead)',2,9,.72]};
const template=fs.readFileSync('Assets/RoyalAves/Art/MapKitV3/RiverPropsV67/river-stone-left-glossy-v67.png.meta','utf8');
for(const [key,a] of Object.entries(m.assets)){
 const [name,title,category,kind,size]=specs[key];a.file=`${folder}/${name}.png`;fs.copyFileSync(a.source,a.file);
 a.guid=fs.existsSync(a.file+'.meta')?fs.readFileSync(a.file+'.meta','utf8').match(/^guid: (\w+)/m)[1]:guid();
 fs.writeFileSync(a.file+'.meta',template.replace(/^guid: \w+/m,`guid: ${a.guid}`));
 const row=`Assets/RoyalAves/Resources/MapLibrary/${category===0?'Construcoes':'Props'}/${name}.asset`;
 let library=fs.readFileSync('Assets/RoyalAves/Resources/MapLibrary/Props/river-stone-left-glossy-v67.asset','utf8');
 library=library.replace('m_Name: river-stone-left-glossy-v67',`m_Name: ${name}`).replace('title: "Glossy river stone left"',`title: "${title}"`).replace('category: 2',`category: ${category}`).replace('guid: 9cf128d00c3d686f23b778731b565eea',`guid: ${a.guid}`).replace('kind: 15',`kind: ${kind}`).replace('size: 1.12',`size: ${size}`);
 fs.writeFileSync(row,library);if(!fs.existsSync(row+'.meta'))fs.writeFileSync(row+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
}
const artRoot='Assets/RoyalAves/Art/MapKitV3/Library/';
function sprite(relative){const g=fs.readFileSync(artRoot+relative+'.meta','utf8').match(/^guid: (\w+)/m)[1];return `{fileID: 21300000, guid: ${g}, type: 3}`;}
const ref=k=>`{fileID: 21300000, guid: ${m.assets[k].guid}, type: 3}`;
const tree=n=>sprite('Arvores/'+n+'.png'),house=n=>sprite('Construcoes/'+n+'.png');
const kit=fs.readFileSync('Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset','utf8');
const kitRef=k=>kit.match(new RegExp(`^  ${k}: (.+)$`,'m'))[1];
const green=tree('green-rounded-tree-v27'),lilac=tree('envelope-blossom-lilac-glossy-v65'),coral=tree('envelope-blossom-coral-glossy-v65'),teal=tree('envelope-blossom-turquoise-glossy-v65'),pine=tree('pine-classic-glossy-v29'),willow=tree('dark-ink-willow-glossy-v64'),copper=tree('volcanic-copperleaf-glossy-v64');
const cottage=house('postal-cottage-glossy-v62'),townhouse=house('postal-townhouse-glossy-v62'),stamp=ref('stampWorkshop');
const rocks=sprite('Props/water-stones-v16.png');
const lava=JSON.parse(fs.readFileSync('Tools/level-map/lava-v69-manifest.json','utf8')).assets;
const lavaRef=k=>`{fileID: 21300000, guid: ${lava[k].guid}, type: 3}`;
const recipes=[
 ['Letter Gardens',false,0,0,[green,coral,teal],[cottage,stamp,townhouse],[rocks],[],[.12,.47,.78],[.40,.87,.84]],
 ['Ink Springs',false,1,1,[willow,lilac,teal],[townhouse,stamp,cottage],[ref('inkPuddle'),rocks],[],[.23,.19,.55],[.66,.54,.93]],
 ['Melody Lagoon',false,3,2,[teal,green,lilac],[stamp,cottage,townhouse],[rocks],[],[.06,.42,.60],[.37,.88,.85]],
 ['Blossom Post',false,6,4,[coral,lilac,teal],[cottage,stamp,townhouse],[rocks,ref('inkPuddle')],[],[.40,.23,.66],[.96,.66,.81]],
 ['Ember Stampworks',false,2,3,[copper,willow,green],[stamp,townhouse,cottage],[lavaRef('lavaStoneLeft'),lavaRef('lavaStoneRight')],[],[.28,.12,.39],[.96,.52,.30]],
 ['Christmas Dispatch',true,5,0,[pine,green,teal],[ref('winterCabin'),ref('winterDepot')],[ref('gifts'),rocks],[kitRef('santaFriendly')],[.08,.27,.58],[.53,.87,.91]],
 ['Christmas Ribbon Rescue',true,6,0,[pine,teal,lilac],[ref('winterDepot'),ref('winterCabin')],[ref('gifts'),rocks],[kitRef('santaVillain'),kitRef('santaFriendly')],[.15,.20,.48],[.72,.73,.94]]
];
const color=rgb=>`{r: ${rgb[0]}, g: ${rgb[1]}, b: ${rgb[2]}, a: 1}`;
let recipeYaml='  automaticSequence: 1\n  plannedLevels: 200\n  recipes:\n';
for(const r of recipes){recipeYaml+=`  - title: ${r[0]}\n    christmas: ${+r[1]}\n    groundStyle: ${r[2]}\n    liquid: ${r[3]}\n    skyTop: ${color(r[8])}\n    skyBottom: ${color(r[9])}\n`;
 for(const [i,field] of [[4,'trees'],[5,'buildings'],[6,'groundProps'],[7,'story']])recipeYaml+=r[i].length?`    ${field}:\n`+r[i].map(x=>'    - '+x+'\n').join(''):`    ${field}: []\n`;
 recipeYaml+=`    treeSize: ${r[1]?1.55:1.48}\n    buildingSize: ${r[1]?1.72:1.6}\n    propSize: ${r[3]===1?.88:r[1]?.82:.70}\n    density: ${r[1]?1.25:1}\n`;
}
const asset='Assets/RoyalAves/Resources/RoyalAvesWorldMapIslands.asset';let data=fs.readFileSync(asset,'utf8');
let [head,...parts]=data.split(/(?=^  - id:)/m);
const firstBefore=parts[0];
head=head.replace(/^  automaticSequence:[\s\S]*?(?=  islands:)/m,'').replace('  islands:',recipeYaml+'  islands:');
const set=(s,key,value)=>{const re=new RegExp(`^    ${key}:.*$`,'m');return re.test(s)?s.replace(re,`    ${key}: ${value}`):s.replace(/(^    title:.*$)/m,`$1\n    ${key}: ${value}`);};
for(let i=1;i<14;i++){
 let s=parts[i];const seasonal=i%3===2,normal=i-Math.floor(i/3),recipe=seasonal?5+Math.floor(i/3)%2:normal%5,variant=seasonal?Math.floor(i/6):Math.floor(normal/5),r=recipes[recipe],lake=!seasonal&&normal%3===1?3:0;
 const fields={title:r[0]+' '+(variant+1),recipeIndex:recipe,sceneryVariant:variant,groundStyle:r[2],terrain:0,liquid:r[3],skyTop:color(r[8]),skyBottom:color(r[9]),background:'{fileID: 0}',customColors:0,customPathColor:0,customChannelColor:0,palette:0,channelWidth:seasonal?2.45:2.1,channelDepth:.18,bankShape:i%3,waterMotion:1,lakeLevels:lake,crossing:lake?2:1,waterPlants:+(!seasonal&&r[3]!==3),channelRocks:0,sideWaterfall:+(!seasonal&&lake>0),waterfallLateral:2.45,waterfallSize:1.5,waterfallOnRight:i%2,waterfallSprite:'{fileID: 0}',weather:seasonal?4:0,weatherIntensity:seasonal?.25:0,meteors:0};
 for(const [k,v] of Object.entries(fields))s=set(s,k,v);
 s=s.replace(/^    stones:[\s\S]*?(?=^    [a-zA-Z])/m,'    stones: []\n').replace(/^    plants:[\s\S]*?(?=^    [a-zA-Z])/m,'    plants: []\n');parts[i]=s;
}
if(firstBefore!==parts[0])throw new Error('First island modified');fs.writeFileSync(asset,head+parts.join(''));
const backups='Library/MapValidation/auto-islands-v70/backup';
for(const file of ['RoyalAvesMission100Layout.asset','RoyalAvesWorldMapLayout.asset','RoyalAvesWorldMapSimpleLayout.asset']){
 const p='Assets/RoyalAves/Resources/'+file;if(!fs.existsSync(p))continue;let text=fs.readFileSync(p,'utf8');
 const chunks=text.split(/(?=^  - index:)/m);const zero=chunks.find(x=>/^  - index: 0$/m.test(x));
 const tail=text.match(/^  hiddenExtras:[\s\S]*$/m)?.[0]||'';
 text=chunks.filter((x,j)=>j===0||!/^  - index: (\d+)/m.test(x)||Number(x.match(/^  - index: (\d+)/m)[1])===0||Number(x.match(/^  - index: (\d+)/m)[1])>=14).join('');
 // A trailing hiddenExtras list can belong to a removed final chapter chunk.
 if(!/^  hiddenExtras:/m.test(text))text+=tail;
 text=text.replace(/^  - river-(\d+)-.*\r?\n/gm,(line,n)=>+n>15&&+n<=200?'':line);
 if(zero&&!text.includes(zero.split(/^  hiddenExtras:/m)[0]))throw new Error('First layout modified: '+file);
 fs.writeFileSync(p,text);
}
for(const file of ['Assets/RoyalAves/Scripts/Meta/WorldMap/WorldMapIslandRecipe.cs','Assets/RoyalAves/Scripts/Meta/WorldMap/WorldMapAutoScenery.cs','Assets/RoyalAves/Editor/RoyalAvesMapAutomaticControls.cs'])if(!fs.existsSync(file+'.meta'))fs.writeFileSync(file+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\n`);
m.sequence=Array.from({length:14},(_,i)=>({island:i+1,from:i*15+1,to:Math.min(200,i*15+15),recipe:i===0?'Preserved':i%3===2?recipes[5+Math.floor(i/3)%2][0]:recipes[(i-Math.floor(i/3))%5][0]}));
fs.writeFileSync(manifestPath,JSON.stringify(m,null,2)+'\n');console.log('Installed 5 sprites, 7 shared recipes, 200 planned positions. First island retained byte-for-byte.');
