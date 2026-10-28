// Read-only pixel inspection. PNG pixels are never modified by this tool.
const fs = require('fs');
const path = require('path');
const sharp = require('C:/Users/Tiago Carvalho/Documents/projetos/ai-traffic-app/node_modules/sharp');
const root = path.resolve(__dirname, '../../..');
const folder = path.join(root, 'Assets/RoyalAves/Art/RoyalPostmanUI2026');
(async () => {
  const report = [];
  for (const name of fs.readdirSync(folder).filter(n => n.endsWith('.png'))) {
    const { data, info } = await sharp(path.join(folder, name)).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
    const bounds = threshold => {
      let x0 = info.width, y0 = info.height, x1 = -1, y1 = -1, count = 0;
      for (let y = 0; y < info.height; y++) for (let x = 0; x < info.width; x++) {
        if (data[(y * info.width + x) * 4 + 3] < threshold) continue;
        x0 = Math.min(x0, x); y0 = Math.min(y0, y); x1 = Math.max(x1, x); y1 = Math.max(y1, y); count++;
      }
      return { x: x0, y: y0, width: x1 - x0 + 1, height: y1 - y0 + 1, count };
    };
    let centerAlphaMax = 0, edgePixels = 0;
    for (let y = 0; y < info.height; y++) for (let x = 0; x < info.width; x++) {
      const a = data[(y * info.width + x) * 4 + 3];
      if (x > info.width * .35 && x < info.width * .65 && y > info.height * .35 && y < info.height * .65) centerAlphaMax = Math.max(centerAlphaMax, a);
      if ((x < 4 || y < 4 || x >= info.width - 4 || y >= info.height - 4) && a > 10) edgePixels++;
    }
    report.push({ id: name.slice(0, -4), width: info.width, height: info.height, alphaBounds: bounds(1), solidBounds: bounds(200), centerAlphaMax, edgePixels });
  }
  fs.writeFileSync(path.join(__dirname, 'ALPHA-REPORT.json'), JSON.stringify(report, null, 2) + '\n');
  console.log(JSON.stringify(report.map(r => ({ id: r.id, width: r.width, height: r.height, padding: r.alphaBounds, centerAlphaMax: r.centerAlphaMax, edgePixels: r.edgePixels })), null, 2));
})().catch(e => { console.error(e); process.exit(1); });
