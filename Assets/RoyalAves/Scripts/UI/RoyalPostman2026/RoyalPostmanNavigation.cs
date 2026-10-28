using UnityEngine;

namespace RoyalAves.UI.RoyalPostman2026
{
    // One continuous painted bar; selection is a translucent layer over the active region.
    public sealed class RoyalPostmanNavigation : MonoBehaviour
    {
        public const float GoldRailHeight=24f;
        public const float DividerRedFraction=.85f;
        public int selectedIndex = 2;
        [Tooltip("Ative para usar os tamanhos automáticos antigos; desative para editar os RectTransforms dos ícones manualmente.")]
        public bool resizeSelectedIcons;
        void OnEnable() => Select(selectedIndex);
        public void Select(int index)
        {
            selectedIndex = Mathf.Clamp(index, 0, 4);
            for (int i = 0; i < 5; i++)
            {
                var tab = transform.Find("Tab" + i);
                if (tab == null) continue;
                bool selected = i == selectedIndex;
                foreach (string name in new[] { "ActiveColor", "SelectedLabel", "Marker" })
                {
                    var child = tab.Find(name);
                    if (child != null) child.gameObject.SetActive(selected);
                }
                var icon = tab.Find("Icon") as RectTransform;
                if (icon == null || !resizeSelectedIcons) continue;
                icon.anchoredPosition = new Vector2(0, selected ? 55 : 20);
                icon.sizeDelta = selected ? new Vector2(184, 166) : new Vector2(142, 142);
            }
        }
    }
}
