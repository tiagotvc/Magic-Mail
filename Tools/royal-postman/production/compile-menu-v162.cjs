const fs=require('fs'),path=require('path'),cp=require('child_process');
const root=path.resolve(__dirname,'../../..'),review=path.join(__dirname,'review/menu-v162');
const response=path.join(root,'Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.rsp');
const editor='C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data';
let original=fs.readFileSync(response,'utf8'),added=[];
function scan(dir){for(const e of fs.readdirSync(path.join(root,dir),{withFileTypes:true})){const p=dir+'/'+e.name;if(e.isDirectory())scan(p);else if(e.name.endsWith('.cs')&&!original.includes(p))added.push(p);}}
scan('Assets/RoyalAves/Scripts');
let shared=original.replace(/^-out:.*$/m,'-out:"'+path.join(review,'candidate.dll').replaceAll('\\','/')+'"').replace(/^-refout:.*$/m,'-refout:"'+path.join(review,'candidate.ref.dll').replaceAll('\\','/')+'"');
shared+='\n'+added.map(p=>'"'+p+'"').join('\n')+'\n';
const baseline=shared.replace('candidate.dll','baseline.dll').replace('candidate.ref.dll','baseline.ref.dll').replace('"Assets/RoyalAves/Scripts/Meta/LobbyController.cs"','"'+path.join(review,'LobbyController.cs.before').replaceAll('\\','/')+'"');
for(const [name,text]of [['candidate',shared],['baseline',baseline]]){
    const rsp=path.join(review,name+'.rsp');fs.writeFileSync(rsp,text);
    const result=cp.spawnSync(editor+'/NetCoreRuntime/dotnet.exe',[editor+'/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll','@'+rsp],{cwd:root,encoding:'utf8',maxBuffer:8*1024*1024});
    fs.writeFileSync(path.join(review,name+'-compile.log'),(result.stdout||'')+(result.stderr||''));
    console.log(name+' compiler exit '+result.status);if(result.error)console.log(result.error.message);
}
const errors=name=>fs.readFileSync(path.join(review,name+'-compile.log'),'utf8').split(/\r?\n/).filter(l=>l.includes('error CS'));
const candidate=errors('candidate'),prior=errors('baseline');
fs.writeFileSync(path.join(review,'compile-comparison.json'),JSON.stringify({addedCurrentRuntimeSources:added,candidateErrors:candidate,baselineErrors:prior},null,2));
console.log('Candidate errors '+candidate.length+'; baseline errors '+prior.length);console.log(candidate.slice(0,5).join('\n'));
