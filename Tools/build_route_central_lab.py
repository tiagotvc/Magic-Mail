"""Prepare isolated Route Central lab assets; never modifies campaign layouts."""
from pathlib import Path
import re
from build_flying_letter_prototype import guid, ROOT


def main():
    for name in ['RouteCentralCell', 'RouteCentralLaboratory', 'RoutePotions']:
        guid(ROOT / f'Assets/RoyalAves/Scripts/Gameplay/{name}.cs', 'MonoImporter')
    guid(ROOT / 'Assets/RoyalAves/Editor/RouteCentralLaboratoryTools.cs', 'MonoImporter')
    labdir = ROOT / 'Assets/RoyalAves/Laboratory'
    art = '{fileID: 21300000, guid: 358c89574b9c4424a6882a33a85eab51, type: 3}'
    visual = '{fileID: 1000, guid: a7adb69fa6a7448c8d24375a03392567, type: 3}'
    script = guid(ROOT / 'Assets/RoyalAves/Scripts/Gameplay/RouteCentralCell.cs')
    cell = (labdir / 'FrankingMachineCell.prefab').read_text(encoding='utf-8')
    cell = cell.replace('FrankingMachineCell', 'RouteCentralCell').replace('4d27ee197f8e58f408d480bdabce58f8', script)
    cell = re.sub(r'  visualPrefab: .*', '  artwork: ' + art, cell)
    cell += ('  standArtwork: {fileID: 21300000, guid: e00ce058d20346e3a798ef3f57348493, type: 3}\n'
             '  flaskArtwork: {fileID: 21300000, guid: b7f2b69179d34b0a96300bc996dd1825, type: 3}\n')
    (labdir / 'RouteCentralCell.prefab').write_text(cell, encoding='utf-8')
    guid(labdir / 'RouteCentralCell.prefab', 'PrefabImporter')

    configpath = ROOT / 'Assets/SweetSugar/Resources/Levels/TargetEditorScriptable.asset'
    config = configpath.read_text(encoding='utf-8')
    egg = re.search(r'  - name: FlyingLetters\n.*?(?=\n  - name: |\Z)', config, re.S)[0]
    entry = egg.replace('FlyingLetters', 'RoutePotions').replace('Quebre os ovos mágicos', 'Quebre as quatro poções').replace('Faltaram ovos mágicos', 'Faltaram poções').replace('setCount: 1', 'setCount: 0')
    entry = re.sub(r'guid: 7f6edba89dc44238b0041b5c4972a0a5', 'guid: 358c89574b9c4424a6882a33a85eab51', entry)
    entry = re.sub(r'    - \{fileID: 190504[^\n]+', '    - ' + visual, entry)
    if '  - name: RoutePotions\n' not in config:
        config = config.rstrip() + '\n' + entry.strip('\n') + '\n'
        configpath.write_text(config, encoding='utf-8')
    index = re.findall(r'^  - name: (.+)', config, re.M).index('RoutePotions')

    source = (labdir / 'FlyingLetterLevel.asset').read_text(encoding='utf-8')
    prefix, grid = source.split('      levelSquares:\n', 1)
    raw, suffix = grid.split('      bombTimer:', 1)
    cells = re.findall(r'      - blocks:.*?(?=      - blocks:|\Z)', raw, re.S)
    assert len(cells) == 49
    normal_guid = guid(ROOT / 'Assets/SweetSugar/Resources/Items/Item.prefab')
    normal = f'{{fileID: 190504, guid: {normal_guid}, type: 3}}'
    rocketpath = ROOT / 'Assets/SweetSugar/Resources/Items/VERTICAL_STRIPED.prefab'
    rocket = f'{{fileID: {re.search(r"--- !u!1 &(\d+)", rocketpath.read_text(encoding="utf-8"))[1]}, guid: {guid(rocketpath)}, type: 3}}'
    fixed = {(2, 1): 0, (2, 2): 1, (2, 3): 0, (1, 2): 0}
    result = []
    for i, text in enumerate(cells):
        row, col = divmod(i, 7)
        top = text[:text.index('        item:')]
        top = re.sub(r'        block: \d+', '        block: 1', top)
        central = row in (2, 3) and col in (3, 4)
        if central:
            # SolidBlock ID is read from the serialized enum, not inferred from another lab.
            enums = (ROOT / 'Assets/SweetSugar/Scripts/Blocks/Square.cs').read_text(encoding='utf-8')
            enum = re.search(r'enum SquareTypes\s*\{(.*?)\}', enums, re.S)[1]
            value = -1
            for member in enum.split(','):
                member = re.sub(r'//[^\n]*', '', member).strip()
                if not member: continue
                if '=' in member:
                    name, number = member.split('='); value = int(number.strip())
                else: name = member; value += 1
                if name.strip() == 'SolidBlock': break
            top = top.replace('        spawners:', f'        - squareType: {value}\n          anotherSquare: 0\n          originalPos: {{x: 0, y: 0}}\n          rotate: 0\n          size: {{x: 0, y: 0}}\n        spawners:', 1)
            top = re.sub(r'        block: \d+', f'        block: {value}', top)
        color = fixed.get((col, row), [0, 1, 2, 6][(row + 2 * col) % 4])
        isrocket = (col, row) == (3, 5)
        itemref = '{fileID: 0}' if central else rocket if isrocket else normal
        top += f'''        item:
          Color: {color}
          ItemType: {4 if isrocket else 0}
          Texture: {{fileID: 0}}
          colors: {{fileID: 0}}
          Item: {itemref}
          EnableMarmaladeTargets: 0
          TargetMarmaladePositions: []
          order: 100
        startWithoutItem: 0
        separatorIndexes: 00000000y
'''
        result.append(top)
    suffix = '      bombTimer:' + suffix
    start = suffix.index('    target:')
    end = suffix.index('    limitType:')
    target = entry.replace('  - name: RoutePotions', '  target:\n    name: RoutePotions', 1)
    target = '\n'.join('  ' + line for line in target.rstrip().splitlines()) + '\n'
    suffix = suffix[:start] + target + suffix[end:]
    suffix = suffix.replace('layers: 1', 'layers: 2').replace('limit: 30', 'limit: 35').replace('colorLimit: 5', 'colorLimit: 4').replace('pieceColors: []', 'pieceColors: 00000000010000000200000006000000')
    suffix = re.sub(r'    subTargetsContainers:.*?(?=    generateIngredient)', '    subTargetsContainers:\n    - targetPrefab: ' + visual + '\n      count: 4\n      preCount: 4\n      extraObject: ' + art + '\n', suffix, flags=re.S)
    prefix = prefix.replace('FlyingLetterLevel', 'RouteCentralLevel').replace('levelNum: 46', 'levelNum: 61')
    path = labdir / 'RouteCentralLevel.asset'
    path.write_text(prefix + '      levelSquares:\n' + ''.join(result) + suffix, encoding='utf-8')
    guid(path, 'NativeFormatImporter')
    targets = (labdir / 'FlyingLetterTargets.asset').read_text(encoding='utf-8').replace('FlyingLetterTargets', 'RouteCentralTargets')
    targets = re.sub(r'        type: \d+', f'        type: {index}', targets)
    targets = targets.replace('7f6edba89dc44238b0041b5c4972a0a5', '358c89574b9c4424a6882a33a85eab51')
    path = labdir / 'RouteCentralTargets.asset'
    path.write_text(targets, encoding='utf-8')
    guid(path, 'NativeFormatImporter')
    print(f'Route Central lab prepared: 7x7, four blocking cells, four potions, target {index}.')


if __name__ == '__main__':
    main()
