// Arte do tabuleiro "Royal Postman" (Assets/Royal Postman Art/chess). O asset fica em Resources para o runtime
// alcançar as imagens por referência: troque os sprites arrastando no Inspector de
// Assets/RoyalAves/Resources/RoyalPostmanGameplayKit.asset. Sem o asset (ou sem os sprites), o jogo usa a arte antiga.
using UnityEngine;

namespace RoyalAves.Meta
{
    public sealed class RoyalPostmanGameplayKit : ScriptableObject
    {
        public const string ResourcePath = "RoyalPostmanGameplayKit";

        [Header("Casas do tabuleiro")]
        [Tooltip("Fundo atrás de cada casa (square_bg).")]
        public Sprite squareUnder;
        [Tooltip("Casa clara do xadrez.")]
        public Sprite squareLight;
        [Tooltip("Casa escura do xadrez.")]
        public Sprite squareDark;

        [Tooltip("Quanto da célula a casa ocupa: 1 = sem espaço, menor = fio de fundo mais grosso entre as casas.")]
        [Range(0.8f, 1f)] public float tileFill = 0.97f;

        [Header("Borda do tabuleiro")]
        [Tooltip("Curva com o arco em cima à esquerda (border_corner_up_left).")]
        public Sprite cornerUpLeft;
        [Tooltip("Curva com o arco em cima à direita.")]
        public Sprite cornerUpRight;
        [Tooltip("Curva com o arco embaixo à esquerda.")]
        public Sprite cornerDownLeft;
        [Tooltip("Curva com o arco embaixo à direita.")]
        public Sprite cornerDownRight;
        [Tooltip("Trecho reto, desenhado em pé (border_reto). É esticado ao comprimento de cada lado.")]
        public Sprite straight;
        [Tooltip("Espessura da borda, em fração do tamanho de uma casa.")]
        [Range(0.03f, 0.3f)] public float borderThickness = 0.11f;
        [Tooltip("Quanto da espessura a borda se afasta da grade: 0 = centrada na linha da grade, 0.5 = encostada por fora.")]
        [Range(0f, 1f)] public float borderOffset = 0.5f;

        [Tooltip("Raio externo do arco das curvas, em pixels do sprite (usado para preencher o vão dos cantos internos).")]
        public float cornerArcRadius = 26f;

        [Header("Fundo da fase (não usado: o fundo é o Background da cena)")]
        public Sprite background;

        public bool HasBorder => cornerUpLeft != null && cornerUpRight != null && cornerDownLeft != null
                                 && cornerDownRight != null && straight != null;

        static RoyalPostmanGameplayKit cached;

        /// Null quando o kit não existe: quem usa cai na arte anterior.
        public static RoyalPostmanGameplayKit Load()
        {
            if (cached == null) cached = Resources.Load<RoyalPostmanGameplayKit>(ResourcePath);
            return cached;
        }
    }
}
