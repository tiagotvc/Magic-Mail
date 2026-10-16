using System;
using UnityEngine;

namespace RoyalAves.Meta
{
    [CreateAssetMenu(fileName = "RoyalAvesLevelMap", menuName = "Royal Aves/Mapa de níveis")]
    public sealed class LevelMapConfig : ScriptableObject
    {
        public const string ResourcePath = "RoyalAvesLevelMap";
        public bool enabledByDefault = true;
        [Tooltip("Usa o mapa 3D (camera -> RenderTexture) quando o WorldMapKit estiver configurado; senao cai no mapa 2D.")]
        public bool useWorldMap = true;
        [HideInInspector] public int futureLevelsShown;
        public Sprite background;
        public Sprite levelButton;
        public Material horizonMaterial;
        [Range(0.6f, 0.95f)] public float horizon = 0.82f;
        [Range(0.1f, 0.35f)] public float bend = 0.24f;
        [Range(0.1f, 0.4f)] public float perspective = 0.20f;
        [Header("Curvatura do mundo")]
        [Range(8, 22)] public float curveDepth = 11;
        [Range(0, 0.12f)] public float crestCurvature = 0.025f;
        [Range(8, 32)] public float worldRadius = 14;
        [Range(1.5f, 6)] public float cameraHeight = 2.4f;
        [Tooltip("Inclinação dos objetos distantes: eles se erguem ao sair da curva.")]
        [Range(0, 80)] public float horizonObjectTilt = 70;
        [Range(80, 180)] public float nodeSize = 136;
        [Range(8, 24)] public int visibleLevels = 18;
        public Color roadBorder = new Color32(194, 124, 35, 255);
        public Color road = new Color32(255, 240, 184, 255);
        public Color roadStripe = new Color32(193, 101, 222, 255);
        public Color lockedTint = new Color32(150, 162, 179, 255);
        [Min(5)] public int levelsPerRegion = 25;
        public Region[] regions = Array.Empty<Region>();

        [Serializable]
        public class Region
        {
            public string title;
            public Color ground = new Color32(112, 187, 106, 255);
            public Color path = new Color32(170, 224, 147, 255);
            public Color skyTint = Color.white;
            public Color propTint = Color.white;
            public Sprite tree;
            public Sprite building;
            public Sprite character;
            [Range(0, 1)] public float buildings = 0.25f;
        }

        public Region RegionAt(float level)
        {
            if (regions == null || regions.Length == 0) return null;
            return regions[Mathf.Max(0, Mathf.FloorToInt((level - 1) / levelsPerRegion)) % regions.Length];
        }
    }
}
