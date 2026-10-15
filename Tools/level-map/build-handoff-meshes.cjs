// Original, simple mesh assets for Claude's map prototype. Does not change the running game.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const root = path.resolve(__dirname, '../..');
const folder = path.join(root, 'Assets/RoyalAves/Art/MapKitV2');
function guid() { return crypto.randomUUID().replaceAll('-', ''); }
for (const relative of ['', 'Meshes', 'Materials', 'Sprites', 'Textures']) {
  const directory = path.join(folder, relative);
  fs.mkdirSync(directory, {recursive: true});
  if (!fs.existsSync(directory + '.meta')) fs.writeFileSync(directory + '.meta',
    `fileFormatVersion: 2\nguid: ${guid()}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
}
class Mesh {
  constructor(name) { this.name = name; this.vertices = []; this.faces = []; }
  triangle(points, group) {
    const first = this.vertices.length + 1;
    this.vertices.push(...points);
    this.faces.push({group, ids: [first, first + 1, first + 2]});
  }
  quad(a, b, c, d, group) { this.triangle([a,b,c], group); this.triangle([a,c,d], group); }
  box(x, y, z, width, height, depth, group) {
    const l=x-width/2,r=x+width/2,b=y,t=y+height,n=z-depth/2,f=z+depth/2;
    this.quad([l,t,n],[l,t,f],[r,t,f],[r,t,n],group);
    this.quad([l,b,n],[r,b,n],[r,b,f],[l,b,f],group);
    this.quad([l,b,n],[l,t,n],[r,t,n],[r,b,n],group);
    this.quad([r,b,f],[r,t,f],[l,t,f],[l,b,f],group);
    this.quad([l,b,f],[l,t,f],[l,t,n],[l,b,n],group);
    this.quad([r,b,n],[r,t,n],[r,t,f],[r,b,f],group);
  }
  save() {
    const target=path.join(folder,'Meshes',this.name+'.obj');
    let text=`# Original procedural map prototype mesh. Y up; dimensions in Unity units.\no ${this.name}\ns off\n`;
    text += this.vertices.map(v=>'v '+v.map(x=>Number(x.toFixed(6))).join(' ')).join('\n')+'\n';
    let current='';
    for(const face of this.faces) {
      if(current!==face.group) { current=face.group; text+='g '+current+'\n'; }
      text+='f '+face.ids.join(' ')+'\n';
    }
    fs.writeFileSync(target,text);
    if(!fs.existsSync(target+'.meta')) fs.writeFileSync(target+'.meta',`fileFormatVersion: 2
guid: ${guid()}
ModelImporter:
  serializedVersion: 23
  fileIDToRecycleName: {}
  externalObjects: {}
  materials:
    importMaterials: 0
  animations:
    isReadable: 1
    clipAnimations: []
  meshes:
    globalScale: 1
    meshCompression: 0
    addColliders: 0
    importCameras: 0
    importLights: 0
    useFileUnits: 0
    useFileScale: 0
    weldVertices: 1
    preserveHierarchy: 1
  tangentSpace:
    normalSmoothAngle: 60
    normalImportMode: 1
    tangentImportMode: 3
  importAnimation: 0
  animationType: 0
  userData: Original Codex map prototype; Y up; read/write for shared curvature
  assetBundleName: 
  assetBundleVariant: 
`);
    return {file:'Assets/RoyalAves/Art/MapKitV2/Meshes/'+this.name+'.obj',triangles:this.faces.length,vertices:this.vertices.length};
  }
}
const meshes=[];
const token=new Mesh('postal-level-token');
const segments=32,point=(radius,y,a)=>[radius*Math.cos(a),y,radius*Math.sin(a)];
for(let i=0;i<segments;i++) {
  const a=i/segments*Math.PI*2,b=(i+1)/segments*Math.PI*2;
  token.triangle([[0,0.16,0],point(0.40,0.16,b),point(0.40,0.16,a)],'FacePurple');
  token.quad(point(.46,.15,a),point(.40,.16,a),point(.40,.16,b),point(.46,.15,b),'RimGold');
  token.quad(point(.46,0,a),point(.46,.15,a),point(.46,.15,b),point(.46,0,b),'SideCream');
  token.triangle([[0,0,0],point(.46,0,a),point(.46,0,b)],'SideCream');
}
meshes.push(token.save());
const ground=new Mesh('raised-banks-segment');
const profile=[[-5,.30],[-2.5,.30],[-2.3,0],[2.3,0],[2.5,.30],[5,.30]];
for(let row=0;row<24;row++) {
  const z0=row*.5,z1=(row+1)*.5;
  const offset=z=>.25*Math.sin(z*.35);
  for(let column=0;column<profile.length-1;column++) {
    const [x0,y0]=profile[column],[x1,y1]=profile[column+1];
    const group=column===2?'RoadLightGrass':column===1||column===3?'BankEdge':'GroundDarkGrass';
    ground.quad([x0+offset(z0),y0,z0],[x0+offset(z1),y0,z1],
      [x1+offset(z1),y1,z1],[x1+offset(z0),y1,z0],group);
  }
}
meshes.push(ground.save());
const bridge=new Mesh('postal-plank-bridge');
for(let i=0;i<12;i++) bridge.box(0,.10,-2+(i+.5)*4/12,2.6,.12,4/12-.025,'BridgeTeal');
for(const side of [-1,1]) {
  bridge.box(side*1.34,.11,0,.12,.20,4.1,'RimGold');
  bridge.box(side*.78,-.12,0,.15,.22,4.1,'SideCream');
}
meshes.push(bridge.save());
const water=new Mesh('water-channel');
for(let row=0;row<8;row++)for(let col=0;col<12;col++) {
  const x=-5+col*10/12,z=-3+row*6/8;
  water.quad([x,-.18,z],[x,-.18,z+.75],[x+10/12,-.18,z+.75],[x+10/12,-.18,z],'WaterTeal');
}
meshes.push(water.save());
const island=new Mesh('postal-island-platform');
const outline=[[-4,-4],[4,-4],[4.6,-3],[4.6,3],[4,4],[-4,4],[-4.6,3],[-4.6,-3]];
for(let i=0;i<outline.length;i++) {
  const a=outline[i],b=outline[(i+1)%outline.length];
  island.triangle([[0,0,0],[b[0],0,b[1]],[a[0],0,a[1]]],'GroundDarkGrass');
  island.quad([a[0],-.4,a[1]],[a[0],0,a[1]],[b[0],0,b[1]],[b[0],-.4,b[1]],'BankEdge');
}
meshes.push(island.save());
const tile=new Mesh('hard-level-tile-patch');
for(let row=0;row<4;row++)for(let col=0;col<5;col++)
  tile.box((col-2)*.65,0,(row-1.5)*.65,.60,.045,.60,(row+col)%2?'HardTilePurple':'SideCream');
meshes.push(tile.save());
const colors={FacePurple:[.67,.35,.82],RimGold:[1,.77,.20],SideCream:[1,.94,.78],
  RoadLightGrass:[.70,.88,.59],GroundDarkGrass:[.31,.67,.51],BankEdge:[.23,.49,.37],
  BridgeTeal:[.36,.77,.73],WaterTeal:[.22,.62,.87],HardTilePurple:[.43,.29,.68]};
const shader='650dd9526735d5b46b79224bc6e94025';
const materials=[];
for(const [name,rgb] of Object.entries(colors)) {
  const target=path.join(folder,'Materials',name+'.mat');
  fs.writeFileSync(target,`%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: ${name}
  m_Shader: {fileID: 4800000, guid: ${shader}, type: 3}
  m_Parent: {fileID: 0}
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap: {}
  disabledShaderPasses: []
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs: []
    m_Ints: []
    m_Floats:
    - _Surface: 0
    - _AlphaClip: 0
    - _ZWrite: 1
    - _Cull: 2
    m_Colors:
    - _BaseColor: {r: ${rgb[0]}, g: ${rgb[1]}, b: ${rgb[2]}, a: 1}
    - _Color: {r: ${rgb[0]}, g: ${rgb[1]}, b: ${rgb[2]}, a: 1}
  m_BuildTextureStacks: []
`);
  if(!fs.existsSync(target+'.meta'))fs.writeFileSync(target+'.meta',
    `fileFormatVersion: 2\nguid: ${guid()}\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
  materials.push({name,file:'Assets/RoyalAves/Art/MapKitV2/Materials/'+name+'.mat',color:rgb});
}
fs.writeFileSync(path.join(root,'Tools/level-map/mesh-kit-manifest.json'),JSON.stringify({
  status:'prototype meshes; not wired into the game',coordinateSystem:'Y up, forward +Z, units in Unity meters',
  notes:['Assign provided materials by OBJ group name.',
    'No final UV unwrap: materials are flat colors; Claude can add terrain UVs in the pilot.',
    'Token number belongs to a top-surface child, not to a billboard.',
    'Apply the same curved transform to meshes, tokens, props and interaction geometry.'],meshes,materials},null,2));
console.log(JSON.stringify({meshes:meshes.length,materials:materials.length,triangles:meshes.reduce((n,m)=>n+m.triangles,0)}));
