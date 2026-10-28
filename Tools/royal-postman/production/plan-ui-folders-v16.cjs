const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=path.resolve(__dirname,'../../..'),old='Assets/RoyalAves/Art/RoyalPostmanUI2026';
function group(n){if(/^(navigation|nav-)/.test(n))return'Tab';if(/avatar|portrait|gear|symbol|coin|heart/.test(n)||n==='top-plus-v15'||/^(goal-|booster-)/.test(n)||['paw-cream','lock-gray'].includes(n))return'Icon';if(/^(hero-|home-|modal-|panel-|socket-)/.test(n)||n.includes('value-'))return'Menu';return'Button';}
const files=[];function walk(dir){for(const d of fs.readdirSync(path.join(root,dir),{withFileTypes:true})){const p=dir+'/'+d.name;if(d.isDirectory())walk(p);else if(!p.endsWith('.meta'))files.push(p);}}walk(old);
const moves=files.map(source=>{const ext=path.extname(source),id=path.basename(source,ext),sub=ext==='.asset'?'Sprites/':ext==='.mat'?'Materials/':ext==='.shader'?'Shaders/':'';const target='Assets/RoyalAves/Art/UI/'+group(id)+'/RoyalPostman/'+sub+path.basename(source);const guid=fs.readFileSync(path.join(root,source+'.meta'),'utf8').match(/^guid: (.+)$/m)?.[1];return{source,target,guid,sha256:crypto.createHash('sha256').update(fs.readFileSync(path.join(root,source))).digest('hex')};});
fs.writeFileSync(path.join(__dirname,'UI-FOLDERS-V16.json'),JSON.stringify({moves},null,2));console.log('Planned '+moves.length+' GUID-preserving file moves.');

