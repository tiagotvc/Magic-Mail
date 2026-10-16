// Register the generated PNG without changing its pixels or replacing existing map objects.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const root=path.resolve(__dirname,'../..');
const png='Assets/RoyalAves/Art/MapKitV3/Library/Props/mailbox-glossy-v44.png';
const library='Assets/RoyalAves/Resources/MapLibrary/Props/caixa-correio-glossy-v44.asset';
const guid=crypto.createHash('md5').update('royalaves/mailbox-glossy-v44').digest('hex');
if(!fs.existsSync(path.join(root,png)))throw Error('Copy the generated PNG first');
if(!fs.existsSync(path.join(root,png+'.meta'))){
  let meta=fs.readFileSync(path.join(root,'Assets/RoyalAves/Art/MapKitV3/Library/Props/shrub-green-glossy-v29.png.meta'),'utf8');
  meta=meta.replace(/^guid: .+$/m,'guid: '+guid).replaceAll('shrub-green-glossy-v29','mailbox-glossy-v44');
  meta=meta.replace(/spriteID: [a-f0-9]+/g,()=> 'spriteID: '+crypto.createHash('md5').update('royalaves/mailbox-glossy-v44/sprite').digest('hex'));
  fs.writeFileSync(path.join(root,png+'.meta'),meta);
}
if(!fs.existsSync(path.join(root,library))){
  let item=fs.readFileSync(path.join(root,'Assets/RoyalAves/Resources/MapLibrary/Props/caixa-correio.asset'),'utf8');
  item=item.replace('m_Name: caixa-correio','m_Name: caixa-correio-glossy-v44').replace('title: Caixa de correio','title: Caixa de correio glossy');
  item=item.replace(/sprite: \{[^\n]+\}/,'sprite: {fileID: 21300000, guid: '+guid+', type: 3}');
  fs.writeFileSync(path.join(root,library),item);
  fs.writeFileSync(path.join(root,library+'.meta'),'fileFormatVersion: 2\nguid: '+crypto.createHash('md5').update('royalaves/library/mailbox-glossy-v44').digest('hex')+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n');
}
console.log(JSON.stringify({png,library,guid}));
