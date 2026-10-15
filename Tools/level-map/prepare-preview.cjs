// Prepare a small, isolated Unity project to render the production map without closing
// the user's game editor. Save facades below are preview-only; game compilation uses real code.
const fs = require('node:fs');
const path = require('node:path');
const {execFileSync} = require('node:child_process');
const root = path.resolve(__dirname, '../..');
const destination = path.join(root, 'Library/MapValidation/PreviewProject');
const files = execFileSync('rg', ['--files', 'Assets', '-g', '*.meta'], {cwd: root, encoding: 'utf8'}).trim().split(/\r?\n/);
const byGuid = new Map();
for (const file of files) {
  const text = fs.readFileSync(path.join(root, file), 'utf8');
  const guid = text.match(/^guid: ([0-9a-f]{32})/m)?.[1];
  if (guid) byGuid.set(guid, file.slice(0, -5));
}
const visited = new Set();
let copied = 0;
function copyAsset(relative) {
  relative = relative.replaceAll('\\', '/');
  if (visited.has(relative)) return;
  visited.add(relative);
  const source = path.join(root, relative);
  if (!fs.existsSync(source) || !fs.statSync(source).isFile()) return;
  if (/\.(cs|dll|asmdef|asmref)$/i.test(relative)) return;
  const target = path.join(destination, relative);
  fs.mkdirSync(path.dirname(target), {recursive: true});
  fs.copyFileSync(source, target);
  if (fs.existsSync(source + '.meta')) fs.copyFileSync(source + '.meta', target + '.meta');
  copied++;
  if (/\.(asset|prefab|mat|shader|controller|anim)$/i.test(relative)) {
    for (const match of fs.readFileSync(source, 'utf8').matchAll(/guid: ([0-9a-f]{32})/g)) {
      const dependency = byGuid.get(match[1]);
      if (dependency) copyAsset(dependency);
    }
  }
}
function copySource(relative) {
  const target = path.join(destination, relative);
  fs.mkdirSync(path.dirname(target), {recursive: true});
  fs.copyFileSync(path.join(root, relative), target);
  if (fs.existsSync(path.join(root, relative + '.meta')))
    fs.copyFileSync(path.join(root, relative + '.meta'), target + '.meta');
}
for (const name of fs.readdirSync(path.join(root, 'Assets/RoyalAves/Scripts/Meta')))
  if (name.endsWith('.cs') && name.includes('Map')) copySource('Assets/RoyalAves/Scripts/Meta/' + name);
for (const file of ['Assets/RoyalAves/Scripts/Meta/LobbyFeaturesConfig.cs',
  'Assets/RoyalAves/Editor/RoyalAvesLevelMapPreview.cs',
  'Assets/SweetSugar/Scripts/MapScripts/IMapProgressManager.cs',
  'Assets/SweetSugar/Scripts/MapScripts/PlayerPrefsMapProgressManager.cs']) copySource(file);
for (const file of ['Assets/RoyalAves/Resources/RoyalAvesLevelMap.asset',
  'Assets/RoyalAves/Resources/RoyalAvesFeatures.asset',
  'Assets/RoyalAves/Prefabs/RoyalAvesLobby.prefab',
  'Assets/TextMesh Pro/Resources/TMP Settings.asset']) copyAsset(file);
// Shader include dependencies use filenames rather than GUID references.
for (const file of fs.readdirSync(path.join(root, 'Assets/TextMesh Pro/Shaders')))
  if (file.endsWith('.cginc')) copyAsset('Assets/TextMesh Pro/Shaders/' + file);
fs.mkdirSync(path.join(destination, 'Assets/Editor'), {recursive: true});
fs.writeFileSync(path.join(destination, 'Assets/PreviewSaveFacades.cs'), `
namespace RoyalAves.Meta { public static class MetaProgress { public static int NextLevel => 1; } }
namespace SweetSugar.Scripts.MapScripts {
    public static class LevelsMap {
        public static IMapProgressManager _mapProgressManager = new PlayerPrefsMapProgressManager();
    }
}
`);
const ugui = fs.readdirSync(path.join(root, 'Library/PackageCache')).find(x => x.startsWith('com.unity.ugui@'));
fs.mkdirSync(path.join(destination, 'Packages'), {recursive: true});
fs.writeFileSync(path.join(destination, 'Packages/manifest.json'), JSON.stringify({dependencies:{
  'com.unity.ugui': 'file:' + path.join(root, 'Library/PackageCache', ugui).replaceAll('\\', '/')
}}, null, 2));
fs.mkdirSync(path.join(destination, 'ProjectSettings'), {recursive: true});
copySource('ProjectSettings/ProjectVersion.txt');
fs.writeFileSync(path.join(destination, 'Assets/Editor/PreviewBatch.cs'), `
using UnityEditor;
public static class PreviewBatch {
    public static void Run() {
        try { RoyalAves.EditorTools.RoyalAvesLevelMapPreview.ValidateAndRender(); EditorApplication.Exit(0); }
        catch (System.Exception error) { UnityEngine.Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
`);
console.log(JSON.stringify({project: destination, copiedAssets: copied}));
