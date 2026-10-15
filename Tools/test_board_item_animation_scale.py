import re
import unittest
from pathlib import Path
from NormalizeBoardItemAnimations import normalize_clip

ROOT = Path(__file__).resolve().parents[1]
ANIMATIONS = ROOT / 'Assets/RoyalAves/Animations/BoardScale'


def sprite_samples(text):
    section = re.search(r'(?ms)^  m_ScaleCurves: *\n(.*?)(?=^  [A-Za-z_])', text)[1]
    return [(float(x), float(y)) for x, y in re.findall(r'value: \{x: ([^,]+), y: ([^,]+)', section)]


class BoardItemAnimationScaleTests(unittest.TestCase):
    def test_landing_returns_to_prefab_size(self):
        source = (ROOT / 'Assets/SweetSugar/Animation/stop.anim').read_text(encoding='utf-8-sig')
        result = normalize_clip(source, 0.88)
        self.assertEqual(sprite_samples(result)[-1], (0.88, 0.88))
        self.assertEqual(re.findall(r'^        time: .+$', source, re.M),
                         re.findall(r'^        time: .+$', result, re.M))
        self.assertEqual(source.split('  m_Events:')[1], result.split('  m_Events:')[1])

    def test_existing_multicolor_size_is_not_reduced_twice(self):
        source = (ROOT / 'Assets/RoyalAves/Animations/Multicolor/MulticolorAppear.anim').read_text(encoding='utf-8-sig')
        self.assertEqual(sprite_samples(normalize_clip(source, 0.88)), sprite_samples(source))

    def test_transient_pulse_is_preserved(self):
        source = (ROOT / 'Assets/SweetSugar/Animation/idle 1.anim').read_text(encoding='utf-8-sig')
        before = sprite_samples(source)
        after = sprite_samples(normalize_clip(source, 0.88))
        self.assertGreater(max(x for x, y in after), 0.88)
        self.assertEqual(after[0], (0.88, 0.88))
        self.assertEqual(after[-1], (0.88, 0.88))
        self.assertEqual(len(before), len(after))
        for a, b in zip(before, after):
            self.assertAlmostEqual(b[0], a[0] * 0.88, places=6)

    def test_override_references_exist(self):
        guids = set()
        for meta in (ROOT / 'Assets').rglob('*.meta'):
            match = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8-sig'), re.M)
            if match:
                guids.add(match[1])
        for controller in ANIMATIONS.glob('*.overrideController'):
            text = controller.read_text()
            for guid in re.findall(r'guid: (\w+)', text):
                self.assertIn(guid, guids, controller.name)


if __name__ == '__main__':
    unittest.main()
