const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
process.chdir(path.resolve(__dirname,'../..'));
const manifest='Tools/level-map/ribbon-rook-v73-manifest.json',m=JSON.parse(fs.readFileSync(manifest,'utf8'));
const guid=()=>crypto.randomBytes(16).toString('hex');
const folder='Assets/RoyalAves/Art/MapKitV3/RibbonRookV73';fs.mkdirSync(folder,{recursive:true});
if(!fs.existsSync(folder+'.meta'))fs.writeFileSync(folder+'.meta',`fileFormatVersion: 2\nguid: ${guid()}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
const template=fs.readFileSync('Assets/RoyalAves/Art/MapKitV3/RibbonRookV71/ribbon-rook-gift-thief-color-v71.png.meta','utf8');
for(const key of ['character','parts']){
 const a=m[key];if(!a.source||!a.file)continue;fs.copyFileSync(a.source,a.file);
 a.guid=fs.existsSync(a.file+'.meta')?fs.readFileSync(a.file+'.meta','utf8').match(/^guid: (\w+)/m)[1]:guid();
 if(!fs.existsSync(a.file+'.meta'))fs.writeFileSync(a.file+'.meta',template.replace(/^guid: \w+/m,`guid: ${a.guid}`));
}
const config='Assets/RoyalAves/Resources/RoyalAvesWorldMapIslands.asset';let data=fs.readFileSync(config,'utf8');
const old=data.match(/^  christmasVillainCharacter: .*guid: (\w+)/m)[1];
const sky=data.match(/^  christmasVillain: .+$/m)[0],first=data.split(/(?=^  - id:)/m)[1];
data=data.replaceAll(old,m.character.guid);
if(data.match(/^  christmasVillain: .+$/m)[0]!==sky||data.split(/(?=^  - id:)/m)[1]!==first)throw Error('Sky or authored island modified');
fs.writeFileSync(config,data);
const library='Assets/RoyalAves/Resources/MapLibrary/Personagens/ribbon-rook-gift-thief-color-v71.asset';
let row=fs.readFileSync(library,'utf8').replace(/sprite: \{fileID: 21300000, guid: \w+, type: 3\}/,`sprite: {fileID: 21300000, guid: ${m.character.guid}, type: 3}`).replace(/^  title:.*$/m,'  title: "Ribbon Rook - Standing"');
fs.writeFileSync(library,row);fs.writeFileSync(manifest,JSON.stringify(m,null,2)+'\n');
console.log('Installed upright character; sky and authored island preserved.');
