// Apply only the reviewed move manifest; keep all Unity .meta GUIDs intact.
const fs = require('fs'), path = require('path'), crypto = require('crypto');
const root = path.resolve(__dirname, '../../..');
const { moves } = JSON.parse(fs.readFileSync(path.join(__dirname, 'UI-FOLDERS-V16.json'), 'utf8'));
const absolute = relative => {
  const p = path.resolve(root, relative);
  if (!p.startsWith(root + path.sep)) throw new Error('Path leaves project: ' + relative);
  return p;
};
const hash = p => crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const guid = p => fs.readFileSync(p + '.meta', 'utf8').match(/^guid: (.+)$/m)?.[1];
// Check the entire plan before changing any paths.
for (const m of moves) {
  const source = absolute(m.source), target = absolute(m.target);
  const current = fs.existsSync(source) ? source : target;
  if (!fs.existsSync(current) || guid(current) !== m.guid || hash(current) !== m.sha256)
    throw new Error('Source changed since review: ' + m.source);
  if (current === source && (fs.existsSync(target) || fs.existsSync(target + '.meta')))
    throw new Error('Destination exists: ' + m.target);
}
for (const m of moves) {
  const source = absolute(m.source), target = absolute(m.target);
  if (!fs.existsSync(source)) continue;
  fs.mkdirSync(path.dirname(target), { recursive: true });
  fs.renameSync(source, target);
  fs.renameSync(source + '.meta', target + '.meta');
}
for (const m of moves) {
  const target = absolute(m.target);
  if (guid(target) !== m.guid || hash(target) !== m.sha256)
    throw new Error('Migration verification failed: ' + m.target);
}
console.log('Verified ' + moves.length + ' unchanged assets with preserved GUIDs.');
