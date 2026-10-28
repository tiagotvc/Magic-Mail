"""Audit every campaign asset; --apply updates only thresholds in the reviewed table.

Unbuilt campaign recipes are deliberately reported as pending, not auto-balanced.
Run from any directory. Existing per-level score/star PlayerPrefs are not modified.
"""
import argparse
import csv
from pathlib import Path
import re
import shutil

ROOT = Path(__file__).resolve().parents[2]
LEVELS = ROOT / 'Assets/SweetSugar/Resources/Levels'
TABLE = Path(__file__).with_name('campaign-star-thresholds.csv')


def main(apply=False):
    with TABLE.open(encoding='utf-8-sig', newline='') as source:
        reviewed = {int(row['level']): row for row in csv.DictReader(source)}
    reports = []
    for path in sorted(LEVELS.glob('Level_*.asset'), key=lambda p: int(p.stem.split('_')[1])):
        n = int(path.stem.split('_')[1])
        original = path.read_text(encoding='utf-8-sig')
        text = original
        current = [int(re.search(rf'(?m)^    star{i}: (\d+)$', text).group(1)) for i in (1, 2, 3)]
        limits = current
        entry = reviewed.get(n)
        if entry:
            limits = [int(entry[f'star{i}']) for i in (1, 2, 3)]
            assert 0 < limits[0] < limits[1] < limits[2], (n, limits)
            if apply and current != limits:
                backup = ROOT / 'Backups/Campaign-stars-before-block-review' / path.name
                if not backup.exists():
                    backup.parent.mkdir(parents=True, exist_ok=True)
                    shutil.copy2(path, backup)
                for i, value in enumerate(limits, 1):
                    text, count = re.subn(rf'(?m)^    star{i}: \d+$', f'    star{i}: {value}', text)
                    assert count == 1
                # No layout, move, palette, or objective edits in a star calibration.
                stripped = lambda s: re.sub(r'(?m)^    star[123]: \d+$', '', s)
                assert stripped(original) == stripped(text)
                path.write_text(text, encoding='utf-8', newline='\n')
            if apply:
                assert all(f'    star{i}: {value}\n' in text for i, value in enumerate(limits, 1))
        active = sum(int(kind) != 0 for kind in re.findall(r'^        block: (\d+)$', text, re.M))
        reports.append({'level': n, 'active_cells': active,
                        'moves_or_time': re.search(r'(?m)^    limit: (\d+)$', text).group(1),
                        'star1': limits[0] if apply else current[0],
                        'star2': limits[1] if apply else current[1],
                        'star3': limits[2] if apply else current[2],
                        'valid_threshold_order': 0 < current[0] < current[1] < current[2],
                        'status': entry['basis'] if entry else 'legacy_asset_campaign_recipe_not_built'})
    assert len(reports) == 100
    target = ROOT / 'Docs/Campanha_Auditoria_Estrelas.csv'
    with target.open('w', encoding='utf-8-sig', newline='') as output:
        writer = csv.DictWriter(output, fieldnames=list(reports[0]))
        writer.writeheader()
        writer.writerows(reports)
    # Regressions from recent wins, plus equality/adjacent score boundaries.
    for n, score in [(6, 2650), (6, 3110), (7, 2680)]:
        row = reviewed[n]
        thresholds = [int(row[f'star{i}']) for i in (1, 2, 3)]
        assert sum(score >= value for value in thresholds) == 3
        assert sum(thresholds[2] - 1 >= value for value in thresholds) == 2
        assert sum(thresholds[2] >= value for value in thresholds) == 3
    print(f'Audited {len(reports)} levels; reviewed {len(reviewed)}; apply={apply}.')
    print('Recent wins on levels 6 and 7: three stars. Threshold boundaries checked.')
    print('Legacy assets awaiting campaign construction:', sum(r['status'].startswith('legacy') for r in reports))
    print('Invalid threshold order:', [r['level'] for r in reports if not r['valid_threshold_order']])


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply', action='store_true')
    main(parser.parse_args().apply)
