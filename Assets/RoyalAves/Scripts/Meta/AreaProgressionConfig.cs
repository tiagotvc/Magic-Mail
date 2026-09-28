// Areas of the Correio Mágico agency. Each area has upgrades (tasks) bought with stars, in order; when all of them are
// done the area chest gives its reward and opens the next area. Levels are one sequence and do not belong to areas.
// Edit Assets/RoyalAves/Resources/RoyalAvesProgression.asset to add areas, upgrades and costs.
using System;
using System.Collections.Generic;
using SweetSugar.Scripts.GUI.Boost;
using UnityEngine;

namespace RoyalAves.Meta
{
    [CreateAssetMenu(fileName = "RoyalAvesProgression", menuName = "Royal Aves/Progressão das áreas")]
    public class AreaProgressionConfig : ScriptableObject
    {
        public const string ResourcePath = "RoyalAvesProgression";

        [Tooltip("Estrelas ganhas na primeira vitória de cada nível. Rejogar um nível não dá estrela.")]
        [Min(0)] public int starsPerFirstWin = 1;

        [Header("Moedas por nível vencido")]
        [Tooltip("Moedas por ponto feito no nível (0,8 = 80% dos pontos viram moedas; 2.000 pontos dão 1.600 moedas).")]
        [Min(0)] public float coinsPerPoint = 0.8f;
        [Tooltip("Parte das moedas dada ao jogar de novo um nível já vencido (1 = o mesmo, 0,25 = um quarto, 0 = nada).")]
        [Range(0, 1)] public float replayCoinShare = 1f;

        [Header("Preços na loja do Sweet Sugar (menu Royal Aves > Ajustar preços da loja)")]
        [Tooltip("Moedas de quem abre o jogo pela primeira vez no aparelho.")]
        [Min(0)] public int startingCoins = 0;
        [Tooltip("Cada pacote de reforços (3 unidades).")]
        [Min(0)] public int boosterPackPrice = 1500;
        [Tooltip("Continuar com +5 movimentos depois de perder.")]
        [Min(0)] public int continuePrice = 900;

        [Tooltip("Na ordem em que o jogador passa por elas.")]
        public List<AreaDefinition> areas = new List<AreaDefinition>();

        static AreaProgressionConfig instance;

        public static AreaProgressionConfig Instance =>
            instance != null ? instance : instance = Resources.Load<AreaProgressionConfig>(ResourcePath);
    }

    [Serializable]
    public class AreaDefinition
    {
        public string id;
        public string title;
        [Tooltip("Fundo da área. As camadas das melhorias são desenhadas por cima, no mesmo retângulo.")]
        public Sprite background;
        [Tooltip("Melhorias, compradas nesta ordem.")]
        public List<AreaTask> tasks = new List<AreaTask>();
        public AreaReward reward = new AreaReward();
    }

    [Serializable]
    public class AreaTask
    {
        public string id;
        public string title;
        [Min(0)] public int starCost = 1;
        [Tooltip("Imagem do tamanho do fundo, com o móvel já na posição final.")]
        public Sprite layer;
        [Tooltip("Ícone da melhoria na lista.")]
        public Sprite icon;
    }

    [Serializable]
    public class AreaReward
    {
        [Tooltip("Moedas do Sweet Sugar (gems).")]
        [Min(0)] public int coins;
        public List<BoosterReward> boosters = new List<BoosterReward>();
    }

    [Serializable]
    public class BoosterReward
    {
        public BoostType type;
        [Min(1)] public int count = 1;
    }
}
