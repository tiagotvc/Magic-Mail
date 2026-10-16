const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..');
const files=[
 ['Assets/RoyalAves/Scripts/Meta/WorldMap/WorldMapWindowLight.cs','MonoImporter'],
 ['Assets/RoyalAves/Art/MapKitV2/Shaders/MapWindowGlow.shader','ShaderImporter'],
 ['Assets/RoyalAves/Art/MapKitV2/Shaders/MapFrozenIce.hlsl','ShaderIncludeImporter'],
 ['Assets/RoyalAves/Resources/WorldMapWindowProfiles.json','TextScriptImporter']
];
for(const [file,importer] of files){
 const meta=path.join(root,file+'.meta');if(fs.existsSync(meta))continue;
 const guid=crypto.randomBytes(16).toString('hex');
 fs.writeFileSync(meta,`fileFormatVersion: 2\nguid: ${guid}\n${importer}:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
}
const shaderMeta=fs.readFileSync(path.join(root,files[1][0]+'.meta'),'utf8'),guid=shaderMeta.match(/^guid: (.+)$/m)[1];
const kit=path.join(root,'Assets/RoyalAves/Resources/RoyalAvesWorldMapKit.asset');let text=fs.readFileSync(kit,'utf8');
const line=`  windowGlow: {fileID: 4800000, guid: ${guid}, type: 3}`;
if(/^  windowGlow:/m.test(text))text=text.replace(/^  windowGlow:.*$/m,line);else text=text.replace(/^(  glow:.*)$/m,'$1\n'+line);
// Install only the shader reference; current authoring settings and active season stay intact.
fs.writeFileSync(kit,text);console.log('Window glow shader linked; 4 source metadata files checked.');
