const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=path.resolve(__dirname,'../../..'),review=path.join(__dirname,'review/hatchery-v121'),stage=path.join(root,'Library/MapValidation/hatchery-v121'),iso=path.join(root,'Library/MapValidation/HatcheryV121Validation');
const prefab='Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab',graph=require('./prefab-graph.cjs');
const hash=bytes=>crypto.createHash('sha256').update(bytes).digest('hex');
const baseline=fs.readFileSync(path.join(review,'before.prefab')),current=fs.readFileSync(path.join(root,prefab));
if(hash(baseline)!==hash(current))throw Error('Production prefab changed after baseline; merge user changes before installing.');
for(const name of ['build','checks'])if(!fs.readFileSync(path.join(stage,name+'.txt'),'utf8').startsWith('PASS:'))throw Error('Native '+name+' did not pass.');
const assembled=fs.readFileSync(path.join(stage,'assembled.prefab')),before=graph(baseline.toString()),after=graph(assembled.toString()),changes=[];
const owner='Pets#114:b56ca6d7aaaaa1d18c4d9816296eb4e2';let preservedProfile=0;
for(const [key,b]of before.blocks){const a=after.blocks.get(key);if(a&&a.text===b.text){if(b.path.startsWith('Pets/CurrentProfile'))preservedProfile++;continue;}changes.push({key,removed:!a});if(!key.startsWith('Pets/Hatchery')&&key!==owner)throw Error('Unrelated prefab component changed: '+key);}
if(preservedProfile!==[...before.blocks.values()].filter(b=>b.path.startsWith('Pets/CurrentProfile')).length)throw Error('CurrentProfile changed');
const materialFolder='Assets/RoyalAves/Art/UI/Button/RoyalPostman/Materials/IncubatorV121';
fs.mkdirSync(path.join(root,materialFolder),{recursive:true});for(const file of fs.readdirSync(path.join(iso,materialFolder)))fs.copyFileSync(path.join(iso,materialFolder,file),path.join(root,materialFolder,file));
if(fs.existsSync(path.join(iso,materialFolder+'.meta')))fs.copyFileSync(path.join(iso,materialFolder+'.meta'),path.join(root,materialFolder+'.meta'));
fs.writeFileSync(path.join(root,prefab),assembled);
for(const name of ['build.txt','checks.txt','idle.png','drawer.png','running.png','reveal.png'])fs.copyFileSync(path.join(stage,name),path.join(review,name));
const audit={version:121,date:'2026-10-24',prefab,before:hash(baseline),after:hash(assembled),beforeComponents:before.blocks.size,afterComponents:after.blocks.size,preservedProfile,changes,notes:['Native UI assembled and rendered in a hidden isolated Unity project.','Original header, authored round sprites, blue AllIn1 button materials and profile preserved.','No generated mascot entered the empty production reward catalogue.','New reusable native bevel/clock/reward-text graphics; existing gold rim and neutral face sprites reused.','Live clock stock and inventory labels; original economy and durable reward receipts retained.'],materials:fs.readdirSync(path.join(root,materialFolder)).filter(f=>f.endsWith('.mat')).map(f=>({path:materialFolder+'/'+f,sha256:hash(fs.readFileSync(path.join(root,materialFolder,f)))}))};
fs.writeFileSync(path.join(review,'audit.json'),JSON.stringify(audit,null,2));fs.writeFileSync(path.join(__dirname,'HATCHERY-V121.json'),JSON.stringify({...audit,changes:undefined,checks:'review/hatchery-v121/checks.txt'},null,2));
console.log(JSON.stringify({installed:prefab,components:after.blocks.size,preservedProfile,changedExisting:changes.length,checks:'PASS'}));
