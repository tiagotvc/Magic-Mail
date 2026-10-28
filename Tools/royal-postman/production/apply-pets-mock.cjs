// Install the verified Pets view without rewriting Home, Navigation or other screen registrations.
const fs = require('fs'), path = require('path'), crypto = require('crypto');
const root = path.resolve(__dirname, '../../..');
const preview = path.join(root, 'Library/MapValidation/PreviewProject');
const resolve = relative => path.join(root, relative);
const source = relative => path.join(preview, relative);
const hash = p => crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const guid = text => text.match(/^guid: (.+)$/m)?.[1];
const report = resolve('Library/MapValidation/pets-mock-v2/validation.txt');
if (!fs.readFileSync(report, 'utf8').startsWith('PASS: V6 two categorized')) throw Error('Native V6 validation has not passed');
const manifests = ['PETS-V2-ASSETS.json', 'PETS-ORNAMENT-V3.json', 'PETS-REFINEMENT-V4.json', 'PETS-CORRECTION-V5.json', 'PETS-INVENTORY-V6.json'].map(p => JSON.parse(fs.readFileSync(path.join(__dirname, p), 'utf8')));
const assets = manifests.flatMap(m => m.assets).filter(a => !a.status);
const files = [];
for (const a of assets) {
  if (hash(source(a.path)) !== a.sha256) throw Error('Source pixels changed: ' + a.id);
  files.push(a.path, path.posix.dirname(a.path) + '/Sprites/' + a.id + '.asset');
}
const base = 'Assets/RoyalAves/';
const controllerCode=base+'Scripts/UI/RoyalPostman2026/RoyalPostmanPetsScreen.cs';
if(hash(resolve(controllerCode))!==hash(source(controllerCode)) || guid(fs.readFileSync(resolve(controllerCode+'.meta'),'utf8'))!==guid(fs.readFileSync(source(controllerCode+'.meta'),'utf8')))
  throw Error('Verified Pets controller code or MonoScript GUID does not match the project');
files.push(base + 'Art/UI/Menu/RoyalPostman/Pets/Sprites/pets-divider-tip-left-v5.asset',
  base + 'Art/UI/Menu/RoyalPostman/Pets/Sprites/pets-divider-tip-right-v5.asset',
  base + 'Art/UI/Menu/RoyalPostman/Pets/Sprites/pets-divider-straight-v5.asset',
  base + 'Art/UI/Menu/RoyalPostman/Pets/Sprites/pets-scroll-track-sliced-v5.asset',
  base + 'Art/UI/Menu/RoyalPostman/Pets/Sprites/pets-scroll-fill-v5.asset',
  base + 'Art/UI/Menu/RoyalPostman/Pets/Sprites/pets-header-red-home-v4.asset',
  base + 'Art/UI/Menu/RoyalPostman/Pets/Sprites/pets-header-gold-home-v4.asset',
  base + 'Art/UI/Menu/RoyalPostman/Pets/pets-incubator-empty-v1.png',
  base + 'Art/UI/Icon/RoyalPostman/Pets/pets-egg-v1.png',
  base + 'Fonts/Pets/Nunito-ExtraBold.ttf',
  base + 'Prefabs/RoyalPostmanUI2026/Pets.prefab');
const registry = base + 'Resources/RoyalPostmanUI2026/RuntimeKit.asset';
const protectedPaths = ['Home', 'Navigation'].map(n => base + 'Prefabs/RoyalPostmanUI2026/' + n + '.prefab')
  .concat(['pets-egg-v1', 'pets-incubator-empty-v1'].map((n, i) => base + 'Art/UI/' + (i ? 'Menu' : 'Icon') + '/RoyalPostman/Pets/' + n + '.png'));
const protectedHashes = protectedPaths.map(p => [p, hash(resolve(p))]);
const backup = resolve('Library/MapValidation/main-before-pets-mock-v2');
for (const p of [base + 'Prefabs/RoyalPostmanUI2026/Pets.prefab', registry]) {
  const target = path.join(backup, p); fs.mkdirSync(path.dirname(target), { recursive: true });
  if (!fs.existsSync(target)) { fs.copyFileSync(resolve(p), target); fs.copyFileSync(resolve(p + '.meta'), target + '.meta'); }
}
// A running editor may already have assigned GUIDs to newly downloaded files. Keep them.
const replacements = new Map();
for (const p of files) if (fs.existsSync(resolve(p + '.meta'))) {
  const old = guid(fs.readFileSync(resolve(p + '.meta'), 'utf8'));
  const incoming = guid(fs.readFileSync(source(p + '.meta'), 'utf8'));
  if (old && incoming && old !== incoming) replacements.set(incoming, old);
}
const remap = text => { for (const [from, to] of replacements) text = text.replaceAll(from, to); return text; };
for (const p of files) {
  fs.mkdirSync(path.dirname(resolve(p)), { recursive: true });
  if (/\.(asset|prefab|mat)$/.test(p)) fs.writeFileSync(resolve(p), remap(fs.readFileSync(source(p), 'utf8')));
  else fs.copyFileSync(source(p), resolve(p));
  fs.writeFileSync(resolve(p + '.meta'), remap(fs.readFileSync(source(p + '.meta'), 'utf8')));
  let folder = path.posix.dirname(p);
  while (folder.startsWith(base)) {
    if (!fs.existsSync(resolve(folder + '.meta')) && fs.existsSync(source(folder + '.meta')))
      fs.copyFileSync(source(folder + '.meta'), resolve(folder + '.meta'));
    folder = path.posix.dirname(folder);
  }
}
const petsRecord = /^  - id: pets\r?\n[\s\S]*?(?=^  - id:|^  rocket:)/m;
const incoming = fs.readFileSync(source(registry), 'utf8').match(petsRecord)?.[0];
const current = fs.readFileSync(resolve(registry), 'utf8');
if (!incoming || !petsRecord.test(current)) throw Error('Pets registry entry was not found');
fs.writeFileSync(resolve(registry), current.replace(petsRecord, remap(incoming)));
for (const [p, before] of protectedHashes) if (hash(resolve(p)) !== before) throw Error('Protected asset changed: ' + p);
const review = resolve('Tools/royal-postman/production/review/pets-inventory-v6'); fs.mkdirSync(review, { recursive: true });
for (const name of fs.readdirSync(resolve('Library/MapValidation/pets-mock-v2')).filter(n => /\.(png|txt)$/.test(n) && n !== 'inventory-scrolled.png'))
  fs.copyFileSync(resolve('Library/MapValidation/pets-mock-v2/' + name), path.join(review, name));
console.log('Installed ' + files.length + ' verified Pets files with GUID preservation. Home, Navigation, egg and apparatus unchanged.');
