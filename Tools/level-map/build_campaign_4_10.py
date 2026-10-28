"""Author campaign levels 4–10, retaining Unity GUIDs and first-run backups.

Uses the project's existing Unity YAML templates; no third-party dependencies.
Run from the project root. --check only validates the authored assets.
"""
import argparse
import csv
import hashlib
import json
from pathlib import Path
import re
import shutil
import struct
from html import escape

ROOT = Path(__file__).resolve().parents[2]
LEVELS = ROOT / 'Assets/SweetSugar/Resources/Levels'
BACKUP = ROOT / 'Backups/Campaign-4-10-before-v1'
PLAN = ROOT / 'Tools/level-map/campaign-progression-v1.csv'

# Row strings run top to bottom. # = absent, . = playable, g = grass, C = box.
LAYOUTS = {
    4: ['#....#', '......', '.ggg..', '..ggg.', '......', '#....#'],
    5: ['##...##', '#C...C#', '.C...C.', '.CgggC.', '.CgggC.', '.gggggg'],
    6: ['.......', 'C..C..C', '#ggg..#', '##ggg##', '#gggg.#', 'C..C..C', '.......'],
    7: ['#.......#', '.CC...CC.', '..gggg...', '.C.ggg.C.', '#..ggg..#'],
    8: ['##...##', '##...##', '..ggg..', '.......', '..ggg..', '##...##', '##...##'],
    9: ['...#...', '.CC#CC.', '.......', '.......', '.CC.CC.', 'ggg#ggg', 'ggg#ggg'],
    10: ['##.....##', '#C.....C#', '.CC...CC.', '.CgggggC.', '.CgggggC.', '#CgggggC#', '##..g..##'],
}

SHAPES = {4: 'cantos recortados', 5: 'leque', 6: 'ampulheta',
          7: 'faixa horizontal recortada', 8: 'cruz', 9: 'H', 10: 'octogono largo'}

# Shared with the calibration tool: rebuilding layouts must retain reviewed thresholds.
with Path(__file__).with_name('campaign-star-thresholds.csv').open(encoding='utf-8-sig', newline='') as source:
    STAR_THRESHOLDS = {int(row['level']): tuple(int(row[f'star{i}']) for i in (1, 2, 3))
                       for row in csv.DictReader(source)}


def entrance(layout, x, y):
    return layout[y][x] != '#' and (y == 0 or layout[y-1][x] == '#')


def read(path):
    return path.read_text(encoding='utf-8-sig')


def replace_one(text, pattern, replacement):
    result, count = re.subn(pattern, replacement, text, flags=re.M)
    assert count == 1, (pattern, count)
    return result


def block_layer(kind):
    return (f'        - squareType: {kind}\n'
            '          anotherSquare: 0\n'
            '          originalPos: {x: 0, y: 0}\n'
            '          rotate: 0\n'
            '          size: {x: 0, y: 0}\n')


def target_entry(kind, count, definitions):
    definition = definitions[kind]
    sprites = definition.split('    defaultSprites:\n      array:\n      - sprites:\n', 1)[1].split('    prefabs:', 1)[0]
    # Definition nests its SpriteList two spaces deeper than TargetObject.
    sprites = '\n'.join(line[2:] for line in sprites.rstrip().splitlines()) + '\n'
    return (f'    - targetType:\n        type: {2 if kind == "SugarSquare" else 4}\n'
            '      sprites:\n' + sprites +
            f'      CountDrawer:\n        count: {count}\n'
            '      NotFinishUntilMoveOut: 0\n'
            '      ShowTheScoreForStar:\n        ShowTheScore: 0\n')


def write_preview(report, plan):
    svg = ['<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="960" viewBox="0 0 1200 960">',
           '<rect width="1200" height="960" fill="#f6f1e8"/>',
           '<g font-family="Arial, sans-serif" fill="#29364a">',
           '<text x="24" y="40" font-size="26">Magic Mail — desafios a cada cinco fases</text>',
           '<text x="24" y="72" font-size="16">Azul: peça · Verde: grama · Marrom: caixa · Contorno interno: duas camadas</text>']
    for i, entry in enumerate(report):
        n = entry['level']
        ox, oy = 24 + (i % 4)*294, 100 + (i // 4)*420
        svg.append(f'<rect x="{ox}" y="{oy}" width="280" height="396" rx="16" fill="white"/>')
        role = plan[n]['papel']
        svg.append(f'<text x="{ox+16}" y="{oy+29}" font-size="20">Fase {n} · {escape(role)}</text>')
        svg.append(f'<text x="{ox+16}" y="{oy+53}" font-size="14">{escape(SHAPES[n])}</text>')
        bx, by = ox + (280-entry['columns']*26)/2, oy+85
        for y, line in enumerate(entry['layout']):
            for x, symbol in enumerate(line):
                if symbol == '#':
                    continue
                fill = {'g': '#64ab5d', 'C': '#b97f45', '.': '#adc9eb'}[symbol]
                svg.append(f'<rect x="{bx+x*26}" y="{by+y*26}" width="24" height="24" rx="5" fill="{fill}"/>')
                layers = entry['grassLayers'] if symbol == 'g' else entry['boxLayers'] if symbol == 'C' else 1
                if layers == 2:
                    svg.append(f'<rect x="{bx+x*26+4}" y="{by+y*26+4}" width="16" height="16" rx="3" fill="none" stroke="#fff0c2" stroke-width="2"/>')
        svg.append(f'<text x="{ox+16}" y="{oy+315}" font-size="14">{entry["moves"]} movimentos · {entry["paletteCount"]} tipos de peça</text>')
        svg.append(f'<text x="{ox+16}" y="{oy+342}" font-size="14">{entry["boxes"]} caixas · {entry["grass"]} gramas</text>')
        svg.append(f'<text x="{ox+16}" y="{oy+377}" font-size="12">Proposta aplicada; dificuldade em Play pendente</text>')
    svg.append('</g></svg>')
    (ROOT / 'Docs/Campanha_Formas_4_a_10.svg').write_text('\n'.join(svg), encoding='utf-8')


def build():
    definitions = dict(re.findall(r'^  - name: (\w+)\n(.*?)(?=^  - name:|\Z)',
                                read(LEVELS / 'TargetEditorScriptable.asset'), re.M | re.S))
    # Template is always read from the first-run backup, so reruns are stable.
    for n in range(4, 11):
        for relative in [f'Level_{n}.asset', f'Targets/TargetLevel{n}.asset']:
            dest = BACKUP / relative
            if not dest.exists():
                dest.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(LEVELS / relative, dest)
    template = read(BACKUP / 'Level_4.asset')
    header = template.split('    fields:\n')[0]
    footer = template.split('    target:\n', 1)[1]
    footer = '    target:\n' + footer
    footer = footer.split('    subTargetsContainers:\n', 1)[0] + '    subTargetsContainers: []\n    generateIngredientOnlyFromSpawner: 0\n'
    target_header = read(BACKUP / 'Targets/TargetLevel4.asset').split('  targets:\n')[0]
    plan = {int(row['fase']): row for row in csv.DictReader(read(PLAN).splitlines())}
    for n, layout in LAYOUTS.items():
        row = plan[n]
        rows, cols = len(layout), len(layout[0])
        assert all(len(line) == cols for line in layout)
        grass_layers = int(row['camadas_grama'])
        box_layers = int(row.get('camadas_caixa') or 1)
        palette_count = int(row['numero_pecas_paleta'])
        field = (f'    fields:\n    - maxRows: {rows}\n      maxCols: {cols}\n'
                 '      noRegenLevel: 0\n      scrollLevel: 0\n      levelSquares:\n')
        for y, line in enumerate(layout):
            for x, symbol in enumerate(line):
                kind = 0 if symbol == '#' else 2 if symbol == 'g' else 4 if symbol == 'C' else 1
                layers = grass_layers if symbol == 'g' else box_layers if symbol == 'C' else 1
                field += '      - blocks:\n' + block_layer(0 if kind == 0 else 1)
                if kind not in (0, 1):
                    field += block_layer(kind) * layers
                field += (f'        spawners: []\n        block: {kind}\n        blockLayer: {layers}\n'
                          '        obstacle: 0\n        obstacleLayer: 1\n'
                          f'        position: {{x: {x}, y: {y}}}\n'
                          '        direction: {x: 0, y: -1}\n'
                          f'        enterSquare: {int(entrance(layout, x, y))}\n'
                          '        isEnterTeleport: 0\n'
                          '        teleportCoordinatesLinked: {x: -1, y: -1}\n'
                          '        teleportCoordinatesLinkedBack: {x: -1, y: -1}\n'
                          f'        guiRect:\n          serializedVersion: 2\n          x: {3+x*53}\n          y: {400+y*53}\n          width: 50\n          height: 50\n'
                          '        item:\n          Color: 0\n          ItemType: 0\n'
                          '          Texture: {fileID: 0}\n          colors: {fileID: 0}\n          Item: {fileID: 0}\n'
                          '          EnableMarmaladeTargets: 0\n          TargetMarmaladePositions: []\n          order: 0\n'
                          '        startWithoutItem: 0\n        separatorIndexes: 00000000\n')
        field += (f'      bombTimer: 15\n      layers: {max(grass_layers,box_layers)+1}\n'
                  '      switchSublevelNoMatch: 0\n      frankingMachines: []\n')
        content = header + field + footer
        content = replace_one(content, r'^  m_Name: Level_4$', f'  m_Name: Level_{n}')
        content = replace_one(content, r'^    levelNum: 4$', f'    levelNum: {n}')
        content = replace_one(content, r'^    limit: \d+$', f'    limit: {row["movimentos_propostos"]}')
        palette = b''.join(struct.pack('<i', color) for color in range(palette_count)).hex()
        content = replace_one(content, r'^    colorLimit: \d+$', f'    colorLimit: {palette_count}\n    pieceColors: {palette}')
        for index, threshold in enumerate(STAR_THRESHOLDS.get(n, ()), 1):
            content = replace_one(content, rf'^    star{index}: \d+$', f'    star{index}: {threshold}')
        (LEVELS / f'Level_{n}.asset').write_text(content, encoding='utf-8', newline='\n')
        targets = target_header.replace('m_Name: TargetLevel4', f'm_Name: TargetLevel{n}') + '  targets:\n    array:\n'
        targets += target_entry('SugarSquare', int(row['celulas_grama']), definitions)
        if int(row['caixas']):
            targets += target_entry('SolidBlock', int(row['caixas']), definitions)
        (LEVELS / f'Targets/TargetLevel{n}.asset').write_text(targets, encoding='utf-8', newline='\n')


def check():
    plan = {int(row['fase']): row for row in csv.DictReader(read(PLAN).splitlines())}
    report = []
    for n, layout in LAYOUTS.items():
        row = plan[n]
        data = read(LEVELS / f'Level_{n}.asset')
        cells = re.findall(r'^      - blocks:\n(.*?)(?=^      - blocks:|^      bombTimer:)', data, re.M | re.S)
        rows, cols = len(layout), len(layout[0])
        assert len(cells) == rows * cols
        assert f'maxRows: {rows}\n      maxCols: {cols}' in data
        assert f'    levelNum: {n}\n' in data
        assert f'    limit: {row["movimentos_propostos"]}\n' in data
        if n in STAR_THRESHOLDS:
            for index, threshold in enumerate(STAR_THRESHOLDS[n], 1):
                assert f'    star{index}: {threshold}\n' in data
        palette_count = int(row['numero_pecas_paleta'])
        palette = b''.join(struct.pack('<i', color) for color in range(palette_count)).hex()
        assert f'pieceColors: {palette}\n' in data
        assert f'    colorLimit: {palette_count}\n' in data
        counts = {2: 0, 4: 0}
        for i, cell in enumerate(cells):
            x, y = i % cols, i // cols
            symbol = layout[y][x]
            kind = 0 if symbol == '#' else 2 if symbol == 'g' else 4 if symbol == 'C' else 1
            layers = int(row['camadas_grama']) if kind == 2 else int(row.get('camadas_caixa') or 1) if kind == 4 else 1
            expected = [0] if kind == 0 else [1] + ([kind] * layers if kind != 1 else [])
            assert [int(v) for v in re.findall(r'squareType: (\d+)', cell)] == expected
            assert f'position: {{x: {x}, y: {y}}}' in cell
            assert f'enterSquare: {int(entrance(layout, x, y))}\n' in cell
            assert f'        block: {kind}\n' in cell
            assert 'ItemType: 0\n' in cell and 'startWithoutItem: 0\n' in cell
            if kind in counts:
                counts[kind] += 1
        assert counts == {2: int(row['celulas_grama']), 4: int(row['caixas'])}
        targets = read(LEVELS / f'Targets/TargetLevel{n}.asset')
        entries = re.findall(r'targetType:\n        type: (\d+).*?CountDrawer:\n        count: (\d+)', targets, re.S)
        expected_targets = [('2', str(counts[2]))] + ([('4', str(counts[4]))] if counts[4] else [])
        assert entries == expected_targets, entries
        # Ensure sprite list has the Unity array shape and current project art references.
        assert '      sprites:\n        array:\n        - icon:' in targets
        for guid in re.findall(r'guid: ([0-9a-f]{32})', targets):
            assert any(guid in read(p) for p in (ROOT / 'Assets').rglob('*.meta')), guid
        # All active cells must belong to a single orthogonally connected board.
        active = {(x,y) for y,line in enumerate(layout) for x,s in enumerate(line) if s != '#'}
        reached, todo = set(), [next(iter(active))]
        while todo:
            p = todo.pop()
            if p in reached or p not in active:
                continue
            reached.add(p)
            x,y = p
            todo.extend([(x-1,y),(x+1,y),(x,y-1),(x,y+1)])
        assert reached == active, f'Level {n}: isolated region'
        # Every vertical segment has an explicit refill entry; no phantom entries in holes.
        for x,y in active:
            top = y
            while top > 0 and layout[top-1][x] != '#':
                top -= 1
            assert entrance(layout,x,top)
        assert len(active) < rows*cols, 'Each revised board needs a real silhouette cutout'
        report.append({'level': n, 'rows': rows, 'columns': cols, 'shape': SHAPES[n],
                       'playableCells': len(active), 'moves': int(row['movimentos_propostos']),
                       'grass': counts[2], 'grassLayers': int(row['camadas_grama']), 'boxes': counts[4],
                       'boxLayers': int(row.get('camadas_caixa') or 1), 'paletteCount': palette_count,
                       'sha256': hashlib.sha256(data.encode()).hexdigest(), 'layout': layout})
    output = ROOT / 'Logs/campaign-4-10-validation.json'
    output.parent.mkdir(exist_ok=True)
    output.write_text(json.dumps({'status': 'static_checks_passed', 'playtested': False, 'levels': report}, indent=2), encoding='utf-8')
    write_preview(report, plan)
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    if not args.check:
        build()
    check()
