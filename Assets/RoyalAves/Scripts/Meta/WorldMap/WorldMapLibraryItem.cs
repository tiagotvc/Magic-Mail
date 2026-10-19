using UnityEngine;

namespace RoyalAves.Meta.WorldMap
{
    public enum WorldMapAssetCategory { Buildings,Trees,Props,Effects,Characters }
    [CreateAssetMenu(fileName="Novo item",menuName="Royal Aves/Item da biblioteca do mapa")]
    public sealed class WorldMapLibraryItem : ScriptableObject
    {
        public string title;
        public string Label=>string.IsNullOrWhiteSpace(title)?name:title;
        public WorldMapAssetCategory category;
        public Sprite sprite;
        public PropKind kind;
        [Range(.12f,3)] public float size=1;
    }
}
