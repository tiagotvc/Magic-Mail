const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=path.resolve(__dirname,'../../..'),stage=path.join(root,'Library/MapValidation/hatchery-v87'),review=path.join(__dirname,'review/hatchery-v87'),file=path.join(root,'Tools/royal-postman/HATCHERY-V87-ASSETS.json');
const data=JSON.parse(fs.readFileSync(file,'utf8'));fs.mkdirSync(review,{recursive:true});
for(const name of fs.readdirSync(stage).filter(n=>n.endsWith('.png')||['review.txt','build.txt','sprites.txt','installation-audit.json','before.prefab'].includes(n)))fs.copyFileSync(path.join(stage,name),path.join(review,name));
for(const asset of data.assets){const target=fs.readFileSync(path.join(root,asset.path)),source=fs.readFileSync(asset.source);if(!target.equals(source))throw Error('Original PNG changed: '+asset.key);asset.sha256=crypto.createHash('sha256').update(target).digest('hex');}
data.round_buttons.source_hashes=Object.fromEntries(data.round_buttons.source_pngs.map(p=>[p,crypto.createHash('sha256').update(fs.readFileSync(path.join(root,p))).digest('hex')]));
data.installation_audit=JSON.parse(fs.readFileSync(path.join(stage,'installation-audit.json'),'utf8'));data.native_result=fs.readFileSync(path.join(stage,'review.txt'),'utf8').trim();
fs.writeFileSync(file,JSON.stringify(data,null,2)+'\n');
const final=fs.readFileSync(path.join(root,'Assets/RoyalAves/Prefabs/RoyalPostmanUI2026/Pets.prefab'),'utf8');if(final!==fs.readFileSync(path.join(stage,'candidate.prefab'),'utf8'))throw Error('Authored prefab changed after installation; inspect current edit before claiming exact candidate identity');
console.log('Confirmed installed candidate,9 unchanged PNG originals, audited profile preservation and native PASS; copied local artifacts.');
