"""Prepare the Magic Egg prototype, preserving the former Flying Letter serialized IDs."""
from pathlib import Path
import re
import shutil
import uuid
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]


def guid(path, kind='DefaultImporter'):
    meta = path.with_name(path.name + '.meta')
    if meta.exists():
        text = meta.read_text(encoding='utf-8')
        if kind == 'NativeFormatImporter' and 'mainObjectFileID:' not in text:
            meta.write_text(text.replace('  externalObjects: {}\n', '  externalObjects: {}\n  mainObjectFileID: 11400000\n'), encoding='utf-8')
        return re.search(r'^guid: (.+)', text, re.M)[1]
    value = uuid.uuid4().hex
    main_id = '  mainObjectFileID: 11400000\n' if kind == 'NativeFormatImporter' else ''
    meta.write_text(f'fileFormatVersion: 2\nguid: {value}\n{kind}:\n  externalObjects: {{}}\n{main_id}  userData: \n  assetBundleName: \n  assetBundleVariant: \n', encoding='utf-8')
    return value


def folder(path):
    path.mkdir(parents=True, exist_ok=True)
    meta = path.with_name(path.name + '.meta')
    if not meta.exists():
        meta.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n', encoding='utf-8')


def main():
    runtime = ROOT / 'Assets/RoyalAves/Scripts/Gameplay'
    scripts = {name: guid(runtime / f'{name}.cs', 'MonoImporter') for name in
               ['ItemFlyingLetter', 'FlyingLetters', 'FlyingLetterLaboratory']}
    guid(ROOT / 'Assets/RoyalAves/Editor/FlyingLetterTools.cs', 'MonoImporter')
    art = ROOT / 'Assets/RoyalAves/Art/Obstacles/FlyingLetter'
    folder(art)
    image = art / 'CM_ovo_magico_prototipo.png'
    if not image.exists():
        raise FileNotFoundError('Approved Magic Egg art must be present before preparing the prototype.')
    art_guid = guid(image, 'TextureImporter')
    # Use a clean single-sprite importer instead of inheriting obsolete sheet rectangles.
    meta = (ROOT / 'Assets/RoyalAves/Art/Pieces/envelope.png.meta').read_text(encoding='utf-8')
    meta = re.sub(r'^guid: .+', 'guid: ' + art_guid, meta, flags=re.M)
    ppu = max(Image.open(image).size) / 1.9
    meta = re.sub(r'spritePixelsToUnits: .+', f'spritePixelsToUnits: {ppu:.8f}', meta)
    existing_meta = image.with_suffix('.png.meta').read_text(encoding='utf-8')
    existing_id = re.search(r'spriteID: ([0-9a-f]+)', existing_meta)
    meta = re.sub(r'spriteID: [0-9a-f]+', 'spriteID: ' + (existing_id[1] if existing_id else uuid.uuid4().hex), meta)
    image.with_suffix('.png.meta').write_text(meta, encoding='utf-8')

    prefab = ROOT / 'Assets/SweetSugar/Resources/Items/FLYING_LETTER.prefab'
    prefab_guid = guid(prefab, 'PrefabImporter')
    text = (ROOT / 'Assets/SweetSugar/Resources/Items/Item.prefab').read_text(encoding='utf-8')
    text = text.replace('  m_Name: Item\n', '  m_Name: FLYING_LETTER\n', 1)
    text = text.replace('4db5ef986208641a9a215549dea98f11', scripts['ItemFlyingLetter'])
    text = text.replace('  Combinable: 1', '  Combinable: 0').replace('  CombinableWithBonus: 1', '  CombinableWithBonus: 0')
    text = text.replace('  DestroyByNeighbour: 0', '  DestroyByNeighbour: 1')
    text = text.replace('  currentType: 0', '  currentType: 9').replace('  debugType: 0', '  debugType: 9')
    text = text.replace('  canBePooled: 0', '  canBePooled: 1')
    text = re.sub(r'  m_LocalPosition: \{x: 3\.4624631[^\n]*', '  m_LocalPosition: {x: 0, y: 0, z: 0}', text)
    text = text.replace('  - component: {fileID: 114939353484849098}\n', '  - component: {fileID: 998877661}\n')
    docs = re.split(r'(?=^--- !u!)', text, flags=re.M)
    text = ''.join(d for d in docs if not d.startswith('--- !u!114 &114939353484849098\n'))
    text = re.sub(r'(  m_Sprite: )\{[^\n]+\}', rf'\g<1>{{fileID: 21300000, guid: {art_guid}, type: 3}}', text)
    text += f'''--- !u!114 &998877661
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 190504}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 305ae37c52ac84738b529bcbc88dec60, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  itemType: 9
'''
    prefab.write_text(text, encoding='utf-8')
    ref = f'{{fileID: 190504, guid: {prefab_guid}, type: 3}}'
    sprite = f'{{fileID: 21300000, guid: {art_guid}, type: 3}}'
    config = ROOT / 'Assets/SweetSugar/Resources/Levels/TargetEditorScriptable.asset'
    text = config.read_text(encoding='utf-8')
    if '  - name: FlyingLetters\n' not in text:
        text += f'''  - name: FlyingLetters
    localization:
      description:
        text: Quebre os ovos mágicos
        index: -1
      failed:
        text: Faltaram ovos mágicos
        index: -1
    collectAction: 0
    setCount: 1
    CanSwithSublevel: 0
    defaultSprites:
      array:
      - sprites:
          array:
          - icon: {sprite}
            uiSprite: 1
    prefabs:
    - {ref}
'''
        config.write_text(text, encoding='utf-8')
    else:
        start = text.index('  - name: FlyingLetters\n')
        entry = text[start:].replace('Colete as cartas voadoras', 'Quebre os ovos mágicos').replace('Faltaram cartas voadoras', 'Faltaram ovos mágicos')
        text = text[:start] + entry
        config.write_text(text, encoding='utf-8')
    target_index = re.findall(r'^  - name: (.+)', text, re.M).index('FlyingLetters')
    # Build a self-contained laboratory without touching campaign layouts.
    lab = ROOT / 'Assets/RoyalAves/Laboratory/FlyingLetterLevel.asset'
    source = (ROOT / 'Assets/SweetSugar/Resources/Levels/Level_1.asset').read_text(encoding='utf-8')
    header = source[:source.index('  levelData:')].replace('m_Name: Level_1', 'm_Name: FlyingLetterLevel')
    template = re.search(r'      - blocks:.*?(?=      - blocks:)', source, re.S)[0]
    template = template[:template.index('        item:')]
    block = re.search(r'      - blocks:.*?(?=        spawners:)', template, re.S)[0]
    template = template.replace(block, '''      - blocks:
        - squareType: 1
          anotherSquare: 0
          originalPos: {x: 0, y: 0}
          rotate: 0
          size: {x: 0, y: 0}
''')
    template = re.sub(r'        block: \d+', '        block: 1', template)
    template = re.sub(r'        blockLayer: \d+', '        blockLayer: 1', template)
    template = re.sub(r'        obstacle: \d+', '        obstacle: 0', template)
    template = re.sub(r'        obstacleLayer: \d+', '        obstacleLayer: 1', template)
    letters = {(2, 1), (4, 2), (2, 4), (5, 5)}
    fixed = {(1, 2): 0, (2, 2): 1, (3, 2): 0, (2, 3): 0}
    item_guid = re.search(r'^guid: (.+)', (ROOT / 'Assets/SweetSugar/Resources/Items/Item.prefab.meta').read_text(encoding='utf-8'), re.M)[1]
    rocket_path = ROOT / 'Assets/SweetSugar/Resources/Items/VERTICAL_STRIPED.prefab'
    rocket_guid = re.search(r'^guid: (.+)', rocket_path.with_suffix('.prefab.meta').read_text(encoding='utf-8'), re.M)[1]
    rocket_id = re.search(r'^--- !u!1 &(\d+)', rocket_path.read_text(encoding='utf-8'), re.M)[1]
    cells = []
    for row in range(7):
        for col in range(7):
            cell = re.sub(r'        position: .*', f'        position: {{x: {col}, y: {row}}}', template)
            cell = re.sub(r'        enterSquare: \d+', f'        enterSquare: {int(row == 0)}', cell)
            is_letter = (col, row) in letters
            is_rocket = (col, row) == (4, 4)
            color = fixed.get((col, row), 0)
            item_ref = ref if is_letter else f'{{fileID: 190504, guid: {item_guid}, type: 3}}' if (col, row) in fixed else '{fileID: 0}'
            if is_rocket: item_ref = f'{{fileID: {rocket_id}, guid: {rocket_guid}, type: 3}}'
            texture_ref = f'{{fileID: 2800000, guid: {art_guid}, type: 3}}' if is_letter else '{fileID: 0}'
            if (col, row) in fixed:
                piece = 'envelope' if color == 0 else 'gift'
                pg = re.search(r'^guid: (.+)', (ROOT / f'Assets/RoyalAves/Art/Pieces/{piece}.png.meta').read_text(encoding='utf-8'), re.M)[1]
                texture_ref = f'{{fileID: 2800000, guid: {pg}, type: 3}}'
            cell += f'''        item:
          Color: {color}
          ItemType: {9 if is_letter else 4 if is_rocket else 0}
          Texture: {texture_ref}
          colors: {{fileID: 0}}
          Item: {item_ref}
          EnableMarmaladeTargets: 0
          TargetMarmaladePositions: []
          order: 100
        startWithoutItem: 0
        separatorIndexes: 00000000y
'''
            cells.append(cell)
    target_config = text[text.index('  - name: FlyingLetters\n'):].splitlines()
    target_config = '\n'.join('    ' + line[2:] for line in target_config) + '\n'
    target_config = target_config.replace('    - name: FlyingLetters\n', '    target:\n      name: FlyingLetters\n', 1)
    lab.write_text(header + '''  levelData:
    SpawnerExits: 0
    levelNum: 46
    fields:
    - maxRows: 7
      maxCols: 7
      noRegenLevel: 0
      scrollLevel: 0
      levelSquares:
''' + ''.join(cells) + '''      bombTimer: 15
      layers: 1
      switchSublevelNoMatch: 0
      frankingMachines: []
''' + target_config + f'''    limitType: 0
    limit: 30
    colorLimit: 5
    pieceColors: []
    star1: 100
    star2: 500
    star3: 1000
    enableMarmalade: 0
    selectedTutorial: 0
    currentSublevelIndex: 0
    subTargetsContainers:
    - targetPrefab: {ref}
      count: 4
      preCount: 4
      extraObject: {sprite}
    generateIngredientOnlyFromSpawner: 0
''', encoding='utf-8')
    guid(lab, 'NativeFormatImporter')
    targets = ROOT / 'Assets/RoyalAves/Laboratory/FlyingLetterTargets.asset'
    target_header = (ROOT / 'Assets/RoyalAves/Laboratory/ParcelStackTargets.asset').read_text(encoding='utf-8').split('  targets:')[0]
    targets.write_text(target_header.replace('m_Name: ParcelStackTargets', 'm_Name: FlyingLetterTargets') + f'''  targets:
    array:
    - targetType:
        type: {target_index}
      sprites:
        array:
        - icon: {sprite}
          uiSprite: 1
      CountDrawer:
        count: 4
      NotFinishUntilMoveOut: 0
      ShowTheScoreForStar:
        ShowTheScore: 0
''', encoding='utf-8')
    guid(targets, 'NativeFormatImporter')
    pool = ROOT / 'Assets/SweetSugar/Resources/Scriptable/PoolSettings.asset'
    text = pool.read_text(encoding='utf-8')
    if prefab_guid not in text:
        text += f'''  - objectToPool: {ref}
    poolName: FlyingLetters
    amountToPool: 8
    shouldExpand: 1
    inEditor: 1
'''
        pool.write_text(text, encoding='utf-8')
    print(f'Magic Egg ready: target index {target_index}; four eggs in isolated 7x7 laboratory; {ppu:.3f} PPU.')


if __name__ == '__main__':
    main()
