using TMPro;
using UnityEngine;

namespace RoyalAves.Meta.WorldMap
{
    public sealed class WorldMapHudTheme : ScriptableObject
    {
        public TMP_FontAsset font;
        public Material artworkMaterial;
        public Material footerMaterial;
        static WorldMapHudTheme current;
        public static WorldMapHudTheme Current=>current!=null?current:current=Resources.Load<WorldMapHudTheme>("MapHudV57/Theme");
    }
}
