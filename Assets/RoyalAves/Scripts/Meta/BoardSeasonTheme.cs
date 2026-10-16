using UnityEngine;

namespace RoyalAves.Meta
{
    [CreateAssetMenu(menuName = "Royal Aves/Temporada visual", fileName = "NovaTemporada")]
    public sealed class BoardSeasonTheme : ScriptableObject
    {
        [Tooltip("Moldura sazonal completa do HUD; preserve os encaixes do retrato e dos contadores.")]
        public Sprite hudFrame;
        public bool snow;
        public Sprite snowCap;
        [Min(0.001f)] public float snowVisibleWidth = 1f;
        [Min(0.1f)] public float snowHeightScale = 1f;
        [Min(0.1f)] public float snowWidthInCells = 0.60f;
    }
}
