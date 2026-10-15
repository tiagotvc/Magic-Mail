"""Keep temporary item scaling relative to each prefab's resting size.

Create dedicated animation overrides; never change shared source clips or frame timing.
Run from the project root. Generated asset GUIDs are deterministic.
"""
from pathlib import Path
import math
import re
import uuid

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'Assets'
OUT = ASSETS / 'RoyalAves/Animations/BoardScale'
NAMESPACE = uuid.UUID('da9a6ee5-0cce-4a02-9b52-461b752e9634')


def guid_for(path):
    return uuid.uuid5(NAMESPACE, path.relative_to(ROOT).as_posix()).hex


def save_asset(path, text, file_id):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding='utf-8', newline='\n')
    meta = Path(str(path) + '.meta')
    if not meta.exists():
        meta.write_text(f'fileFormatVersion: 2\nguid: {guid_for(path)}\nNativeFormatImporter:\n'
                        f'  externalObjects: {{}}\n  mainObjectFileID: {file_id}\n'
                        '  userData: \n  assetBundleName: \n  assetBundleVariant: \n', encoding='utf-8')
    return re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8'), re.M)[1]


def number(value, factor):
    parsed = float(value)
    return format(parsed * factor, '.9g') if math.isfinite(parsed) else value


def normalize_clip(text, sprite_scale):
    def section(match):
        scalar = match[1] == 'm_EditorCurves'

        def curve(block):
            value = block[0]
            path = re.search(r'^    path: *(.*)$', value, re.M)
            if path is None or path[1] not in ('', 'Sprite'):
                return value
            factor = 0.42 if path[1] == '' else sprite_scale
            if scalar:
                attribute = re.search(r'^    attribute: (.+)$', value, re.M)
                if attribute is None or attribute[1] not in ('m_LocalScale.x', 'm_LocalScale.y'):
                    return value
                samples = [float(v) for v in re.findall(r'^        value: ([^\n]+)$', value, re.M)]
                reference = samples[-1] if samples and samples[-1] > 0 else (samples[0] if samples else 1.0)
                if math.isclose(reference, 0.88, abs_tol=1e-5):
                    factor /= 0.88
                return re.sub(r'(?m)^(        (?:value|inSlope|outSlope): )([^\n]+)$',
                              lambda m: m[1] + number(m[2], factor), value)
            samples = [float(v) for v in re.findall(r'^        value: \{x: ([^,]+),', value, re.M)]
            reference = samples[-1] if samples and samples[-1] > 0 else (samples[0] if samples else 1.0)
            if math.isclose(reference, 0.88, abs_tol=1e-5):
                factor /= 0.88
            return re.sub(r'(?m)^(        (?:value|inSlope|outSlope): )\{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}',
                          lambda m: m[1] + '{x: ' + number(m[2], factor) + ', y: ' +
                          number(m[3], factor) + ', z: ' + m[4] + '}', value)

        body = re.sub(r'(?ms)^  - curve:\n.*?(?=^  - curve:|\Z)', curve, match[2])
        return f'  {match[1]}:\n' + body

    return re.sub(r'(?ms)^  (m_ScaleCurves|m_EditorCurves): *\n(.*?)(?=^  [A-Za-z_]|\Z)', section, text)


def main():
    registry = {}
    for pattern in ('*.anim.meta', '*.controller.meta', '*.overrideController.meta'):
        for meta in ASSETS.rglob(pattern):
            if OUT in meta.parents:
                continue
            guid = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8-sig'), re.M)
            if guid:
                registry[guid[1]] = Path(str(meta)[:-5])
    original_controllers = {
        'Items': 'fa899764163cc4c4dba4d39b913015da',
        'Dynamite': '97024a5b6607f944fbfca640f0c5080a',
        'Multicolor': '68b2f7c6c0fb4a198ca0ec57447c33d2',
    }
    mappings = {}
    changed_clips = set()
    for name, original_guid in original_controllers.items():
        text = registry[original_guid].read_text(encoding='utf-8-sig')
        base_guid = original_guid
        overrides = {}
        if 'AnimatorOverrideController:' in text:
            base_guid = re.search(r'm_Controller: \{fileID: \d+, guid: (\w+)', text)[1]
            overrides = dict(re.findall(r'm_OriginalClip: \{[^}]*guid: (\w+)[^}]*\}\n'
                                       r'    m_OverrideClip: \{[^}]*guid: (\w+)', text))
        base_text = registry[base_guid].read_text(encoding='utf-8-sig')
        originals = set(re.findall(r'm_Motion: \{fileID: 7400000, guid: (\w+)', base_text)) | overrides.keys()
        for scale in (0.88, 1.0):
            label = f'{name}_{int(scale * 100)}'
            replacements = {}
            for original in sorted(originals):
                effective = overrides.get(original, original)
                path = registry[effective]
                source = path.read_text(encoding='utf-8-sig')
                normalized = normalize_clip(source, scale)
                if normalized != source:
                    output = OUT / f'{path.stem}_{effective[:8]}_{int(scale * 100)}.anim'
                    effective = save_asset(output, normalized, 7400000)
                    changed_clips.add(output)
                if effective != original:
                    replacements[original] = effective
            text = ('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!221 &22100000\n'
                    'AnimatorOverrideController:\n  m_ObjectHideFlags: 0\n'
                    '  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n'
                    f'  m_PrefabAsset: {{fileID: 0}}\n  m_Name: BoardScale_{label}\n'
                    f'  m_Controller: {{fileID: 9100000, guid: {base_guid}, type: 2}}\n' +
                    ('  m_Clips:\n' if replacements else '  m_Clips: []\n'))
            for original, effective in replacements.items():
                text += (f'  - m_OriginalClip: {{fileID: 7400000, guid: {original}, type: 2}}\n'
                         f'    m_OverrideClip: {{fileID: 7400000, guid: {effective}, type: 2}}\n')
            output = OUT / f'{label}.overrideController'
            mappings[(original_guid, scale)] = save_asset(output, text, 22100000)
    folder_meta = Path(str(OUT) + '.meta')
    if not folder_meta.exists():
        folder_meta.write_text(f'fileFormatVersion: 2\nguid: {guid_for(OUT)}\nfolderAsset: yes\n'
                               'DefaultImporter:\n  externalObjects: {}\n  userData: \n'
                               '  assetBundleName: \n  assetBundleVariant: \n', encoding='utf-8')
    count = 0
    generated = {value: key[0] for key, value in mappings.items()}
    for prefab in (ASSETS / 'SweetSugar/Resources/Items').rglob('*.prefab'):
        text = prefab.read_text(encoding='utf-8-sig')
        scale = 0.88 if 'm_LocalScale: {x: 0.88, y: 0.88' in text else 1.0

        def replace(match):
            original = generated.get(match[1], match[1])
            target = mappings.get((original, scale))
            return f'm_Controller: {{fileID: 22100000, guid: {target}, type: 2}}' if target else match[0]

        updated = re.sub(r'm_Controller: \{fileID: \d+, guid: (\w+), type: 2\}', replace, text)
        if updated != text:
            prefab.write_text(updated, encoding='utf-8', newline='\n')
            count += 1
    print(f'Normalized {len(changed_clips)} clips; assigned dedicated controllers to {count} prefabs.')


if __name__ == '__main__':
    main()
