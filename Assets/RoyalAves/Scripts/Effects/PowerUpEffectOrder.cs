using System.Linq;
using UnityEngine;

namespace RoyalAves.Effects
{
    // Correio Mágico: regra geral - todo efeito de ativação de power-up (foguete + rastro, hélice, bomba, globo
    // multicolorido) desenha na camada de ordenação "Item mask", que no projeto já fica acima de "Default" (onde
    // ficam o tabuleiro e os efeitos de match comuns, FireworkSplash/_ExplosionAround, com sortingOrder em torno de
    // 0-10 - ver ProjectSettings/TagManager.asset). Como a ordem entre camadas diferentes nunca empata (ao
    // contrário da ordem numérica dentro da mesma camada), um power-up nunca fica escondido atrás da explosão de
    // um match comum.
    public static class PowerUpEffectOrder
    {
        public const string SortingLayer = "Item mask";
        public const int Value = 100;

        // Põe todo renderer do efeito (sprites, partículas, linhas) nessa camada/ordem, preservando a ordem
        // relativa que já havia entre eles (ex.: o rastro de chama atrás da metade do foguete).
        public static void Raise(GameObject root)
        {
            if (root == null) return;
            var renderers = root.GetComponentsInChildren<Renderer>(true).OrderBy(r => r.sortingOrder).ToArray();
            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingLayerName = SortingLayer;
                renderers[i].sortingOrder = Value + i;
            }
        }
    }
}
