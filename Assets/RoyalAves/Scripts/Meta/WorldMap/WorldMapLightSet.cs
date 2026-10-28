using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Meta.WorldMap
{
    public enum WorldMapLightTint { Warm, Pale, Mint, Rose, Custom }

    // Luzes decorativas do mapa (raios de sol cartoon + brilhos), posicionadas por fase/lateral e editaveis no Inspector.
    // Resources/RoyalAvesWorldMapLights.asset: menu Royal Aves > Mapa 3D > Luzes do mapa.
    public sealed class WorldMapLightSet : ScriptableObject
    {
        public const string ResourcePath = "RoyalAvesWorldMapLights";

        [Serializable]
        public sealed class Shaft
        {
            public string name = "Luz";
            public bool enabled = true;
            [Tooltip("Posicao na rota, em fases (ex.: 3.5 = entre a fase 3 e 4).")]
            public float level = 3;
            [Tooltip("Metros ao lado do centro do caminho (+ direita, - esquerda). E o ponto onde a luz toca o chao.")]
            public float lateral = 3.5f;
            [Tooltip("Altura do ponto de contato acima do chao (m).")]
            [Range(0, 3)] public float height = .2f;
            [Tooltip("TAMANHO geral: multiplica comprimento e largura do feixe (e dos brilhos).")]
            [Range(.2f, 4)] public float scale = 1;
            [Tooltip("ROTACAO em graus: positivo = a luz vem de cima, da ESQUERDA; negativo = da direita. 0 = vertical.")]
            [Range(-75, 75)] public float angle = 42;
            [Tooltip("Comprimento do feixe (m).")]
            [Range(1, 20)] public float length = 6;
            [Tooltip("Largura na base, perto do chao (m). O feixe afunila para a fonte.")]
            [Range(.3f, 8)] public float width = 1.7f;
            [Range(1, 6)] public int bands = 3;
            [Range(0,1)] public float raySpacing=.5f;
            [Range(0, 1)] public float intensity = .55f;
            public WorldMapLightTint tint = WorldMapLightTint.Warm;
            public Color customColor = new Color(1, .93f, .6f);
            [Range(0, 10)] public int sparkles = 4;
            [Range(.03f, .4f)] public float sparkleSize = .09f;
            public int seed = 1;

            public Color Color
            {
                get
                {
                    switch (tint)
                    {
                        case WorldMapLightTint.Pale: return new Color(1, .98f, .88f);
                        case WorldMapLightTint.Mint: return new Color(.75f, 1, .88f);
                        case WorldMapLightTint.Rose: return new Color(1, .8f, .88f);
                        case WorldMapLightTint.Custom: return customColor;
                        default: return new Color(1, .92f, .55f);
                    }
                }
            }
        }

        // Luzes avulsas opcionais (avancado). O caminho principal e o editor curvo: Efeitos > Raio de sol (luz).
        public List<Shaft> shafts = new List<Shaft>();
    }
}
