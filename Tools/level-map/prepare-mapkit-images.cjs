const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const root = path.resolve(__dirname, '../..');
const provenance = JSON.parse(fs.readFileSync(path.join(__dirname,'mapkit-v2-image-prompts.json'),'utf8'));
const definitions = {
  dog: [2,2,['dog_idle','dog_wag','dog_sniff','dog_carry']],
  mailbox: [2,1,['mailbox_closed','mailbox_open']],
  lamp: [1,1,['postal_lamp']], tree: [1,1,['round_purple_tree']],
  props: [2,2,['prop_parcels','prop_flowers','prop_sign','prop_shrub']],
  landmarks: [4,2,['landmark_boat','landmark_windmill','landmark_lighthouse','landmark_mountain_hut','landmark_clock','landmark_grand_agency','landmark_tunnel','landmark_kite']],
  terrain: [2,2,[]]
};
const guid = () => crypto.randomUUID().replaceAll('-','');
const revisionPath=path.join(__dirname,'mapkit-v2-glow-sources.json');
const revisions=fs.existsSync(revisionPath)?JSON.parse(fs.readFileSync(revisionPath,'utf8')):[];
const manifest=[];
for(const source of [...provenance.sources,...revisions]) {
  const [columns,rows,names]=definitions[source.key];
  const original=source.hint.match(/as (C:\\[^\n]+?\.png) by default/)[1];
  const file=path.join(root,'Assets/RoyalAves/Art/MapKitV2', names.length ? 'Sprites' : 'Textures',source.file);
  fs.copyFileSync(original,file);
  const png=fs.readFileSync(file), width=png.readUInt32BE(16),height=png.readUInt32BE(20);
  const sprites=names.map((name,index)=>{
    const col=index%columns,row=Math.floor(index/columns);
    // The sniff pose extends two pixels beyond the nominal midline. Move the
    // vertical cut into the actual transparent gutter, without editing the PNG.
    const cuts=source.file==='dog-courier-poses-glow-spaced.png'?[0,Math.round(width*650/1254),width]:null;
    const x=cuts?cuts[col]:Math.floor(col*width/columns),y=height-Math.floor((row+1)*height/rows);
    return {name,internalID:21300001+index,spriteID:crypto.createHash('md5').update('MapKitV2:'+name).digest('hex'),rect:{x,y,width:(cuts?cuts[col+1]:Math.floor((col+1)*width/columns))-x,height:Math.floor((row+1)*height/rows)-Math.floor(row*height/rows)},pivot:{x:0.5,y:0.05}};
  });
  const meta=file+'.meta';
  const existingGuid=fs.existsSync(meta)?fs.readFileSync(meta,'utf8').match(/^guid: (\w+)/m)?.[1]:null;
  const mode=names.length>1?2:names.length?1:0;
  const ids=mode===2?sprites.map(s=>`  - first:\n      213: ${s.internalID}\n    second: ${s.name}`).join('\n'):'  []';
  const entries=mode===2?sprites.map(s=>`    - serializedVersion: 2\n      name: ${s.name}\n      rect:\n        serializedVersion: 2\n        x: ${s.rect.x}\n        y: ${s.rect.y}\n        width: ${s.rect.width}\n        height: ${s.rect.height}\n      alignment: 9\n      pivot: {x: 0.5, y: 0.05}\n      border: {x: 0, y: 0, z: 0, w: 0}\n      outline: []\n      physicsShape: []\n      tessellationDetail: -1\n      bones: []\n      spriteID: ${s.spriteID}\n      internalID: ${s.internalID}\n      vertices: []\n      indices: \n      edges: []\n      weights: []`).join('\n'):'    []';
  fs.writeFileSync(meta,`fileFormatVersion: 2
guid: ${existingGuid||guid()}
TextureImporter:
  internalIDToNameTable: ${mode===2?'\n'+ids:'[]'}
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  compressionQuality: 100
  spriteMode: ${mode}
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 9
  spritePivot: {x: 0.5, y: 0.05}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: ${names.length?1:0}
  textureType: ${names.length?8:0}
  textureShape: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 100
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
  spriteSheet:
    serializedVersion: 2
    sprites: ${mode===2?'\n'+entries:'[]'}
    outline: []
    physicsShape: []
    bones: []
    spriteID: ${guid()}
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {}
  userData: MapKitV2 AI-generated prototype; pivot refinement required in editor
  assetBundleName: 
  assetBundleVariant: 
`);
  const latest=revisions.filter(r=>r.key===source.key).at(-1);
  const recommended=source.key!=='terrain' && (!latest || source.file===latest.file);
  manifest.push({key:source.key,path:path.relative(root,file).replaceAll('\\','/'),recommended,width,height,pngColorType:png[25],columns,rows,sprites,source:original});
}
fs.writeFileSync(path.join(__dirname,'mapkit-v2-image-manifest.json'),JSON.stringify({status:'prototype-assets-not-wired',generator:'image_gen',spriteCount:manifest.filter(m=>m.recommended).reduce((n,m)=>n+m.sprites.length,0),totalSpritesIncludingSuperseded:manifest.reduce((n,m)=>n+m.sprites.length,0),images:manifest},null,2)+'\n');
console.log(JSON.stringify({images:manifest.length,recommendedImages:manifest.filter(m=>m.recommended).length,recommendedSprites:manifest.filter(m=>m.recommended).reduce((n,m)=>n+m.sprites.length,0)}));
