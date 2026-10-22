"""Slice the approved sprite sheet without changing its artwork; retain Unity GUIDs."""
from pathlib import Path
import re
import shutil
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/RoyalAves/Art/Obstacles/ParcelStack'
BACKUP = ROOT / 'Backups/WoodenCrate-before-cartoon-v3'
PREFAB = ROOT / 'Assets/SweetSugar/Resources/Blocks/SolidBlock.prefab'


def backup(path):
    dest = BACKUP / path.relative_to(ROOT)
    if not dest.exists():
        dest.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, dest)


def main():
    sheet = Image.open(ROOT / 'Docs/Art/caixa-cartoon-v3-duas-tabuas.png')
    assert sheet.mode == 'RGBA' and sheet.size == (2172, 724)
    for path in ART.glob('CM_pilha_estado0[1-4]_00.png*'):
        backup(path)
    backup(PREFAB)
    text = PREFAB.read_text(encoding='utf-8')
    fits = []
    for index in range(3):
        sprite = sheet.crop((index * 724, 0, (index + 1) * 724, 724))
        alpha = sprite.getchannel('A')
        assert alpha.getextrema() == (0, 255)
        left, top, right, bottom = alpha.point(lambda a: 255 if a >= 128 else 0).getbbox()
        ppu = 724 / 1.9
        sx, sy = 1.72 * ppu / (right - left), 1.72 * ppu / (bottom - top)
        ox = -(left + right - 724) / (2 * ppu) * sx
        oy = -(724 - top - bottom) / (2 * ppu) * sy
        fits.append((sx, sy, ox, oy))
        path = ART / f'CM_pilha_estado0{index + 1}_00.png'
        sprite.save(path)
        meta = path.with_suffix('.png.meta')
        meta.write_text(re.sub(r'spritePixelsToUnits: [^\n]+', f'spritePixelsToUnits: {ppu:.8f}', meta.read_text(encoding='utf-8')), encoding='utf-8')

    # Remove the fourth child and all of its component documents.
    docs = re.split(r'(?=^--- !u!)', text, flags=re.M)
    docs = [doc for doc in docs if not re.match(r'--- !u!\d+ &(?:810000000000000000[0-4]|8200000000000000004)\n', doc)]
    text = ''.join(docs)
    text = text.replace('  - {fileID: 8100000000000000001}\n', '').replace('  - {fileID: 8100000000000000003}\n', '')
    for index, (sx, sy, ox, oy) in enumerate(fits, 1):
        text, count = re.subn(rf'(  remainingLayers: {index}\n)  visualScale: [^\n]+\n  visualOffset: [^\n]+',
            rf'\g<1>  visualScale: {{x: {sx:.8f}, y: {sy:.8f}}}\n  visualOffset: {{x: {ox:.8f}, y: {oy:.8f}}}', text)
        assert count == 1
    PREFAB.write_text(text, encoding='utf-8')

    # Retire the fourth state's target entries; the first three GUIDs stay intact.
    retired = '578ba430c2ea4dcbb274dfb0a6cae0ef'
    changed = []
    for base in ('Assets/SweetSugar/Resources/Levels', 'Assets/RoyalAves/Laboratory'):
        for path in (ROOT / base).rglob('*.asset'):
            original = path.read_text(encoding='utf-8-sig')
            updated = re.sub(r'^ *- icon: \{fileID: 21300000, guid: ' + retired + r', type: 3\}\n *uiSprite: [01]\n', '', original, flags=re.M)
            if updated != original:
                backup(path)
                path.write_text(updated, encoding='utf-8')
                changed.append(str(path.relative_to(ROOT)))
    # Retired art is preserved in Backups, never imported as a fourth state again.
    for path in ART.glob('CM_pilha_estado04_00.png*'):
        path.unlink()
    # Keep the saved laboratory in sync with its three-state test panel.
    lab = ROOT / 'Assets/RoyalAves/Laboratory/ParcelStackLevel.asset'
    original = lab.read_text(encoding='utf-8')
    cells = re.split(r'(?=^      - blocks:)', original, flags=re.M)
    for i, cell in enumerate(cells):
        if re.search(r'^        position: \{x: 3, y: 4\}$', cell, re.M):
            cell = re.sub(r'^        - squareType: 4\n(?:          [^\n]*\n)*', '', cell, flags=re.M)
            cell = cell.replace('        block: 4\n', '        block: 1\n').replace('        blockLayer: 4\n', '        blockLayer: 1\n')
            cells[i] = cell
    updated = ''.join(cells).replace('      layers: 5\n', '      layers: 4\n')
    updated = re.sub(r'(?m)^(\s*count:) 4$', r'\1 3', updated)
    if updated != original:
        backup(lab)
        lab.write_text(updated, encoding='utf-8')
    targets = ROOT / 'Assets/RoyalAves/Laboratory/ParcelStackTargets.asset'
    original = targets.read_text(encoding='utf-8')
    updated = re.sub(r'(?m)^(\s*count:) 4$', r'\1 3', original)
    if updated != original:
        backup(targets)
        targets.write_text(updated, encoding='utf-8')
    print(f'Applied 3 transparent 724px sprites; updated {len(changed)} target assets; backup: {BACKUP.relative_to(ROOT)}')
    print('Fits:', fits)


if __name__ == '__main__':
    main()
