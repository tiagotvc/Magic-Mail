"""Build or --check the authored levels 11–30 without changing levels 1–10.

Recipes are explicit, editable grids. Unity GUIDs and first-run backups are retained.
The star table is shared with the calibration tool. No external Python dependencies.
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

from build_campaign_4_10 import block_layer, entrance, read, replace_one

ROOT = Path(__file__).resolve().parents[2]
LEVELS = ROOT / 'Assets/SweetSugar/Resources/Levels'
RECIPES = Path(__file__).with_name('campaign-11-30-recipes.json')
BACKUP = ROOT / 'Backups/Campaign-11-30-before-v1'
KINDS = {'#': 0, '.': 1, 'g': 2, 'C': 4, 'A': 12, 'M': 13}


def definitions():
    return dict(re.findall(r'^  - name: (\w+)\n(.*?)(?=^  - name:|\Z)',
                          read(LEVELS / 'TargetEditorScriptable.asset'), re.M | re.S))


def goals(recipe):
    flat = ''.join(recipe['layout'])
    return {name: count for name, count in [
        ('MailCubby', len(recipe['cabinets']) * 36), ('MachineLetters', recipe['machineLetters']),
        ('SolidBlock', flat.count('C')), ('SugarSquare', flat.count('g'))] if count > 0}


def target_entry(name, count, defs):
    sprites = defs[name].split('    defaultSprites:\n      array:\n      - sprites:\n', 1)[1].split('    prefabs:', 1)[0]
    sprites = '\n'.join(line[2:] for line in sprites.rstrip().splitlines()) + '\n'
    return (f'    - targetType:\n        type: {list(defs).index(name)}\n      sprites:\n{sprites}'
            f'      CountDrawer:\n        count: {count}\n      NotFinishUntilMoveOut: 0\n'
            '      ShowTheScoreForStar:\n        ShowTheScore: 0\n')


def expected_layers(symbol, recipe):
    if symbol == '#':
        return [0]
    return [1] + ([KINDS[symbol]] * (recipe['boxLayers'] if symbol == 'C' else
                                   recipe['grassLayers'] if symbol == 'g' else 1) if symbol != '.' else [])


def refill_entry(grid, x, y):
    return grid[y][x] not in '#AM' and entrance(grid,x,y)


def validate_recipe(recipe):
    grid = recipe['layout']
    rows, cols = len(grid), len(grid[0])
    assert 5 <= rows <= 9 and 6 <= cols <= 9
    assert all(len(line) == cols and set(line) <= KINDS.keys() for line in grid)
    assert 3 <= recipe['palette'] <= 7
    assert 0 < recipe['boxLayers'] <= 3 and 0 < recipe['grassLayers'] <= 2
    assert (recipe['role'] == 'desafio') == (recipe['level'] % 5 == 0)
    assert bool(recipe['machines']) == (recipe['machineLetters'] > 0)
    if recipe['level'] < 21:
        assert not recipe['machines']
    cabinets = set()
    for x, y in recipe['cabinets']:
        group = {(x,y),(x+1,y),(x,y+1),(x+1,y+1)}
        assert not cabinets & group
        for xx, yy in group:
            assert 0 <= yy < rows and 0 <= xx < cols and grid[yy][xx] == 'A'
        cabinets |= group
    actual_cabinets = {(x,y) for y,line in enumerate(grid) for x,s in enumerate(line) if s == 'A'}
    assert cabinets == actual_cabinets
    machines = {(x,y) for y,line in enumerate(grid) for x,s in enumerate(line) if s == 'M'}
    assert machines == {tuple(p) for p in recipe['machines']}
    active = {(x,y) for y,line in enumerate(grid) for x,s in enumerate(line) if s != '#'}
    reached, todo = set(), [next(iter(active))]
    while todo:
        p = todo.pop()
        if p in reached or p not in active:
            continue
        reached.add(p)
        x,y = p
        todo += [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]
    assert reached == active and len(active) < rows*cols
    # Permanent furniture needs an adjacent site where a match can actually be made.
    # Destructible boxes are considered open after clearing; holes/furniture remain blocked.
    def match_site(x,y):
        available = active - cabinets - machines
        if (x,y) not in available:
            return False
        for dx,dy in [(1,0),(0,1)]:
            count = 1
            for sign in (-1,1):
                xx,yy = x+sign*dx,y+sign*dy
                while (xx,yy) in available:
                    count += 1
                    xx,yy = xx+sign*dx,yy+sign*dy
            if count >= 3:
                return True
        return False
    for group in [set((x+dx,y+dy) for dx in (0,1) for dy in (0,1))
                  for x,y in recipe['cabinets']]:
        perimeter = {(x+dx,y+dy) for x,y in group for dx,dy in [(-1,0),(1,0),(0,-1),(0,1)]} - group
        assert sum(match_site(x,y) for x,y in perimeter) >= 2, (recipe['level'], 'no match access', group)
    if machines:
        assert len(machines) >= 2
        for x,y in machines:
            neighbours = [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]
            assert any(p not in active for p in neighbours), (recipe['level'], 'interior machine',x,y)
            # U corners have no adjacent normal piece; they remain reachable by an area special.
            assert any(match_site(xx,yy) for xx,yy in neighbours) or any(
                match_site(xx,yy) and abs(xx-x)+abs(yy-y) <= 2 for xx,yy in active), (recipe['level'],'inaccessible machine',x,y)
        reached, todo = set(), [next(iter(machines))]
        while todo:
            p = todo.pop()
            if p in reached or p not in machines:
                continue
            reached.add(p)
            x,y = p
            todo += [(x-1,y),(x+1,y),(x,y-1),(x,y+1)]
        assert reached == machines, (recipe['level'],'machine formation must be contiguous')
        formation = recipe['machineFormation']
        if formation == 'bottom_row':
            assert machines == {(x,rows-1) for x in range(cols) if (x,rows-1) in active}
        elif formation in ('left_column','right_column'):
            x = 0 if formation == 'left_column' else cols-1
            assert machines == {(x,y) for y in range(rows) if (x,y) in active}
        else:
            assert formation == 'u'
            top = min(y for x,y in machines)
            expected = {(0,y) for y in range(top,rows)} | {(cols-1,y) for y in range(top,rows)} | {(x,rows-1) for x in range(cols)}
            assert machines == expected
    # Furniture is permanent during refill. Every other cell must have a possible gravity path
    # from an entry, allowing a diagonal only when at least one of its two corner routes is open.
    available = active - cabinets - machines
    filled = {(x,y) for x,y in available if refill_entry(grid,x,y)}
    todo = list(filled)
    while todo:
        x,y = todo.pop()
        for dx in (-1,0,1):
            p = x+dx,y+1
            if p not in available or p in filled:
                continue
            if dx and (x+dx,y) not in available and (x,y+1) not in available:
                continue
            filled.add(p)
            todo.append(p)
    assert filled == available, (recipe['level'],'cells without a gravity path',available-filled)


def build(recipes, stars):
    defs = definitions()
    for recipe in recipes:
        validate_recipe(recipe)
    for recipe in recipes:
        n = recipe['level']
        for relative in [f'Level_{n}.asset', f'Targets/TargetLevel{n}.asset']:
            dest = BACKUP / relative
            if not dest.exists():
                dest.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(LEVELS / relative, dest)
    # Use the known campaign template's compatibility target handler, not a new unimplemented target class.
    template = read(ROOT / 'Backups/Campaign-4-10-before-v1/Level_4.asset')
    cell = re.findall(r'^      - blocks:\n(.*?)(?=^      - blocks:)', template, re.M | re.S)[0]
    item_tail = cell[cell.index('        isEnterTeleport:'):]
    item_tail = replace_one(item_tail, r'^        separatorIndexes:.*$', '        separatorIndexes: 00000000')
    footer = '    target:\n' + template.split('    target:\n', 1)[1]
    footer = footer.split('    subTargetsContainers:\n', 1)[0] + '    subTargetsContainers: []\n    generateIngredientOnlyFromSpawner: 0\n'
    header = template.split('    fields:\n', 1)[0]
    target_header = read(ROOT / 'Backups/Campaign-4-10-before-v1/Targets/TargetLevel4.asset').split('  targets:\n')[0]
    for recipe in recipes:
        n, grid = recipe['level'], recipe['layout']
        rows, cols = len(grid), len(grid[0])
        field = (f'    fields:\n    - maxRows: {rows}\n      maxCols: {cols}\n'
                 '      noRegenLevel: 0\n      scrollLevel: 0\n      levelSquares:\n')
        for y,line in enumerate(grid):
            for x,symbol in enumerate(line):
                layers = expected_layers(symbol, recipe)
                field += '      - blocks:\n' + ''.join(block_layer(t) for t in layers)
                count = recipe['boxLayers'] if symbol == 'C' else recipe['grassLayers'] if symbol == 'g' else 1
                field += (f'        spawners: []\n        block: {KINDS[symbol]}\n        blockLayer: {count}\n'
                          '        obstacle: 0\n        obstacleLayer: 1\n'
                          f'        position: {{x: {x}, y: {y}}}\n        direction: {{x: 0, y: -1}}\n'
                          f'        enterSquare: {int(refill_entry(grid,x,y))}\n')
                tail = replace_one(item_tail, r'^          x: \d+$', f'          x: {3+x*53}')
                tail = replace_one(tail, r'^          y: \d+$', f'          y: {400+y*53}')
                field += tail
        field += (f'      bombTimer: 15\n      layers: {max(len(expected_layers(s,recipe)) for line in grid for s in line)}\n'
                  '      switchSublevelNoMatch: 0\n      frankingMachines: []\n')
        text = header + field + footer
        text = replace_one(text, r'^  m_Name: Level_4$', f'  m_Name: Level_{n}')
        text = replace_one(text, r'^    levelNum: 4$', f'    levelNum: {n}')
        text = replace_one(text, r'^    limit: \d+$', f'    limit: {recipe["moves"]}')
        palette = b''.join(struct.pack('<i',c) for c in range(recipe['palette'])).hex()
        text = replace_one(text, r'^    colorLimit: \d+$', f'    colorLimit: {recipe["palette"]}\n    pieceColors: {palette}')
        for i,value in enumerate(stars[n],1):
            text = replace_one(text, rf'^    star{i}: \d+$', f'    star{i}: {value}')
        (LEVELS / f'Level_{n}.asset').write_text(text, encoding='utf-8', newline='\n')
        targets = target_header.replace('m_Name: TargetLevel4', f'm_Name: TargetLevel{n}') + '  targets:\n    array:\n'
        targets += ''.join(target_entry(name,count,defs) for name,count in goals(recipe).items())
        (LEVELS / f'Targets/TargetLevel{n}.asset').write_text(targets, encoding='utf-8', newline='\n')


def check(recipes, stars):
    defs = definitions()
    report = []
    for recipe in recipes:
        validate_recipe(recipe)
        n, grid = recipe['level'], recipe['layout']
        data = read(LEVELS / f'Level_{n}.asset')
        cells = re.findall(r'^      - blocks:(?: \[\])?\n(.*?)(?=^      - blocks:|^      bombTimer:)', data, re.M | re.S)
        rows,cols = len(grid),len(grid[0])
        assert len(cells) == rows*cols
        for pattern in [f'    levelNum: {n}\n',f'maxRows: {rows}\n      maxCols: {cols}\n',
                        f'    limit: {recipe["moves"]}\n',f'    colorLimit: {recipe["palette"]}\n']:
            assert pattern in data
        palette = b''.join(struct.pack('<i',c) for c in range(recipe['palette'])).hex()
        assert re.search(rf'^    pieceColors: {palette}i?$', data, re.M)
        for i,cell in enumerate(cells):
            x,y = i%cols,i//cols
            symbol = grid[y][x]
            actual_layers = [int(t) for t in re.findall(r'squareType: (\d+)',cell)]
            assert actual_layers == expected_layers(symbol,recipe) or (symbol == '#' and not actual_layers)
            assert f'        block: {KINDS[symbol]}\n' in cell
            assert f'position: {{x: {x}, y: {y}}}' in cell
            assert f'enterSquare: {int(refill_entry(grid,x,y))}\n' in cell
            assert 'ItemType: 0\n' in cell and 'startWithoutItem: 0\n' in cell
        targets = read(LEVELS / f'Targets/TargetLevel{n}.asset')
        actual = [(int(t),int(c)) for t,c in re.findall(r'targetType:\n        type: (\d+).*?CountDrawer:\n        count: (\d+)', targets,re.S)]
        assert actual == [(list(defs).index(name),count) for name,count in goals(recipe).items()]
        assert 0 < stars[n][0] < stars[n][1] < stars[n][2]
        for i,value in enumerate(stars[n],1):
            assert f'    star{i}: {value}\n' in data
        report.append({'level': n,'role':recipe['role'],'rows':rows,'columns':cols,
                       'moves':recipe['moves'],'palette':recipe['palette'],'goals':goals(recipe),
                       'stars':stars[n],'sha256':hashlib.sha256(data.encode()).hexdigest()})
    output = ROOT / 'Logs/campaign-11-30-validation.json'
    output.parent.mkdir(exist_ok=True)
    output.write_text(json.dumps({'staticChecks':'passed','playtested':False,'levels':report},indent=2),encoding='utf-8')
    print(f'Validated {len(report)} levels: layouts, groups, match access, entries, palette, goals and stars.')


def preview(recipes):
    svg = ['<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="1930" viewBox="0 0 1200 1930">',
           '<rect width="1200" height="1930" fill="#f6f1e8"/>','<g font-family="Arial,sans-serif" fill="#29364a">',
           '<text x="24" y="38" font-size="25">Magic Mail — fases 11 a 30</text>',
           '<text x="24" y="68" font-size="15">Azul: peça · Verde: grama · Marrom: caixa · Roxo: armário · Rosa: franqueadora</text>']
    for i,r in enumerate(recipes):
        x,y = 20+(i%4)*295,90+(i//4)*365
        rows,cols = len(r['layout']),len(r['layout'][0])
        svg += [f'<rect x="{x}" y="{y}" width="280" height="350" rx="15" fill="white"/>',
                f'<text x="{x+14}" y="{y+27}" font-size="19">{r["level"]} · {escape(r["role"])}</text>',
                f'<text x="{x+14}" y="{y+50}" font-size="13">{r["moves"]} movimentos · {r["palette"]} peças</text>']
        bx,by = x+(280-cols*24)/2,y+65
        for yy,line in enumerate(r['layout']):
            for xx,s in enumerate(line):
                if s == '#':
                    continue
                color = {'.':'#adc9eb','C':'#b97f45','g':'#64ab5d','A':'#8d79be','M':'#d77397'}[s]
                svg.append(f'<rect x="{bx+xx*24}" y="{by+yy*24}" width="22" height="22" rx="4" fill="{color}"/>')
                layers = r['boxLayers'] if s == 'C' else r['grassLayers'] if s == 'g' else 1
                if layers > 1:
                    svg.append(f'<rect x="{bx+xx*24+4}" y="{by+yy*24+4}" width="14" height="14" rx="2" fill="none" stroke="#fff0c2"/>')
        svg.append(f'<text x="{x+14}" y="{y+304}" font-size="12">Armários: {len(r["cabinets"])} · Máquinas: {len(r["machines"])}</text>')
        svg.append(f'<text x="{x+14}" y="{y+330}" font-size="12">Cartas da máquina: {r["machineLetters"]} · Play pendente</text>')
    svg.append('</g></svg>')
    (ROOT/'Docs/Campanha_Formas_11_a_30.svg').write_text('\n'.join(svg),encoding='utf-8')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check',action='store_true')
    parser.add_argument('--level',type=int,choices=range(11,31),help='Rebuild only this level; validate the full batch afterwards.')
    parser.add_argument('--from-level',type=int,choices=range(11,31),default=11)
    args = parser.parse_args()
    recipes = json.loads(read(RECIPES))
    assert [r['level'] for r in recipes] == list(range(11,31))
    with Path(__file__).with_name('campaign-star-thresholds.csv').open(encoding='utf-8-sig',newline='') as source:
        stars = {int(r['level']):[int(r[f'star{i}']) for i in (1,2,3)] for r in csv.DictReader(source)}
    selected = [r for r in recipes if (args.level is None or r['level'] == args.level) and r['level'] >= args.from_level]
    protected = set(range(1,31)) - {r['level'] for r in selected}
    before = {p:hashlib.sha256(p.read_bytes()).hexdigest() for n in protected
              for p in [LEVELS/f'Level_{n}.asset',LEVELS/f'Targets/TargetLevel{n}.asset']}
    if not args.check:
        build(selected,stars)
    check(recipes,stars)
    preview(recipes)
    assert all(hashlib.sha256(p.read_bytes()).hexdigest() == value for p,value in before.items())
    print('Levels outside the selected range and their targets preserved.')
