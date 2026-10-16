// Goals of the level about to start, on the level start window: each goal's picture with how many to collect, like
// the prototype ("envelope 50"). Replaces Sweet Sugar's MenuTargetIcon, which showed the pictures without numbers.
// Goals counted from the board (clear all the grass, for example) have no number before the board exists; they show
// only the picture.
using System.Collections.Generic;
using System.Linq;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public class LevelGoalsView : MonoBehaviour
    {
        [Tooltip("Modelo de uma meta (desligado): a figura, com um texto filho para a quantidade.")]
        [SerializeField] Image template;

        readonly List<GameObject> shown = new List<GameObject>();

        void OnEnable() => Refresh();

        public void Refresh()
        {
            // A domain reload can forget the list while generated children still exist in the hierarchy.
            var previous = shown.Concat(transform.Cast<Transform>()
                .Where(t => t != template?.transform && t.name.StartsWith("Meta ")).Select(t => t.gameObject)).Distinct().ToArray();
            foreach (var item in previous)
            {
                if (item == null) continue;
                item.SetActive(false); // Remove from layout immediately when reopening on another level.
                if (Application.isPlaying) Destroy(item);
                else DestroyImmediate(item);
            }
            shown.Clear();
            if (template == null) return;
            template.gameObject.SetActive(false);

            // Read the preview definitions directly. Constructing gameplay counters here tries to count a board
            // that does not exist yet (especially spread goals), and can leave the pre-level UI half populated.
            var level = PlayerPrefs.GetInt("OpenLevel", 1);
            var definitions = Resources.Load<TargetLevel>("Levels/Targets/TargetLevel" + level);
            if (definitions == null) return;
            var goals = definitions.targets.Where(t => t != null).Select(t => new { definition = t, type = t.targetType.GetTarget() })
                .Where(t => t.type != null && (t.type.setCount == SetCount.FromLevel || t.definition.CountDrawer.count > 0)).ToArray();
            if (goals.Any(t => t.type.name != "Stars")) goals = goals.Where(t => t.type.name != "Stars").ToArray();
            foreach (var layout in GetComponents<LayoutGroup>()) layout.enabled = false;
            foreach (var goal in goals.OrderBy(t => t.type.setCount == SetCount.FromLevel ? 0 : t.definition.CountDrawer.count))
            {
                var sprite = goal.definition.sprites.FirstOrDefault(s => s.uiSprite && s.icon != null)?.icon
                    ?? goal.definition.sprites.FirstOrDefault(s => s.icon != null)?.icon;
                if (sprite == null) continue;
                var icon = Instantiate(template, template.transform.parent);
                icon.type = Image.Type.Simple;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.material = null;
                icon.name = "Meta " + sprite.name;
                icon.sprite = sprite;
                var count = icon.GetComponentInChildren<TextMeshProUGUI>(true);
                if (count != null)
                {
                    var amount = goal.type.setCount == SetCount.FromLevel ? 0 : goal.definition.CountDrawer.count;
                    if (goal.type.name == "Stars" && goal.definition.ShowTheScoreForStar.ShowTheScore)
                    {
                        var data = Resources.Load<LevelContainer>("Levels/Level_" + level)?.levelData;
                        if (data != null) amount = amount == 3 ? data.star3 : amount == 2 ? data.star2 : data.star1;
                    }
                    count.text = amount.ToString();
                    count.gameObject.SetActive(amount > 0);
                }
                shown.Add(icon.gameObject);
            }
            Arrange();
            // Finish sprite, quantity and geometry while hidden. The first canvas rebuild sees the final row.
            foreach (var item in shown) item.SetActive(true);
        }

        void OnRectTransformDimensionsChange() { if (shown.Count > 0) Arrange(); }

        void Arrange()
        {
            if (shown.Count == 0 || template == null) return;
            var rect = ((RectTransform)transform).rect;
            var columns = Mathf.Min(3, shown.Count);
            var rows = Mathf.CeilToInt(shown.Count / (float)columns);
            var gap = columns == 2 ? 105f : 30f;
            const float iconSize = 260, groupHeight = 365, rowGap = 20;
            var rowHeight = (rect.height - 12 - (rows - 1) * rowGap) / rows;
            var scale = Mathf.Max(.01f, Mathf.Min(1, (rect.width - 12) /
                (columns * iconSize + (columns - 1) * gap), rowHeight / groupHeight));
            for (var i = 0; i < shown.Count; i++)
            {
                var r = (RectTransform)shown[i].transform;
                var row = i / columns;
                var inRow = Mathf.Min(columns, shown.Count - row * columns);
                r.anchorMin = r.anchorMax = r.pivot = Vector2.one * .5f;
                r.localRotation = Quaternion.identity;
                r.sizeDelta = Vector2.one * iconSize;
                r.localScale = Vector3.one * scale;
                // Centre the complete picture + quantity group, including the quantity underneath the icon.
                var y = (rows - 1) * (rowHeight + rowGap) * .5f - row * (rowHeight + rowGap);
                r.anchoredPosition = new Vector2((i % columns - (inRow - 1) * .5f) * (iconSize + gap) * scale,
                    y + 52.5f * scale);
            }
        }
    }
}
