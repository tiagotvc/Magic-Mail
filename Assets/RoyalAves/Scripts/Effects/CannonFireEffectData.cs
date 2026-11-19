using UnityEngine;

namespace RoyalAves.Effects
{
    // Arte e ajustes do efeito do canhão (boost Cannon), carregados uma vez via
    // Resources.Load("Effects/CannonFireEffectData"); o efeito em si é montado em código (CannonFireEffect / CannonFireRunner).
    // Texturas, não Sprites - mesma nota do RocketSplitEffectData: o fileID do asset principal de uma textura é sempre o
    // fixo 2800000, enquanto o de um Sprite é um hash que só vale depois de uma importação real na Unity.
    //
    // Tudo aqui é editável no Inspector (menu Royal Aves/Efeitos/Canhão/Selecionar configurações do efeito). Medidas "em casas"
    // são múltiplos da largura de uma casa do tabuleiro; tempos em segundos.
    [CreateAssetMenu(menuName = "Royal Aves/Cannon Fire Effect Data")]
    public class CannonFireEffectData : ScriptableObject
    {
        [Header("Arte do canhão")]
        [Tooltip("Linha de cima da folha: canhão parado, 4 quadros quase iguais (balanço sutil).")]
        public Texture2D[] IdleFrames;
        [Tooltip("Linha de baixo da folha: clarão + fumaça desenhados na arte. Só usados se 'Usar quadros de tiro' estiver ligado.")]
        public Texture2D[] FireFrames;
        [Tooltip("Liga os quadros de tiro da arte (têm fumaça cinza). Desligado = o clarão é só o efeito mágico feito em código.")]
        public bool useFireFrames;
        [Min(0.01f)] public float idleFrameTime = 0.07f;
        [Min(0.01f)] public float fireFrameTime = 0.08f;

        [Header("Arte opcional (vazio = desenhado em código)")]
        [Tooltip("Sprite da bala de canhão. Vazio = esfera de ferro em brasa desenhada em código.")]
        public Sprite ballSprite;
        [Tooltip("Sprite da faísca na ponta do pavio. Vazio = estrela de 4 pontas desenhada em código.")]
        public Sprite fuseSparkSprite;
        [Tooltip("Sprite de um gomo do pavio (esticado entre dois pontos da curva). Vazio = retângulo liso.")]
        public Sprite fuseSegmentSprite;
        [Tooltip("Material aditivo (RoyalAves/ImpactSprite). Vazio = criado em tempo de execução a partir do shader.")]
        public Material additiveMaterial;

        [Header("Geometria do canhão")]
        [Tooltip("Pixels por unidade usados ao criar o sprite a partir da textura.")]
        [Min(1f)] public float pixelsPerUnit = 128f;
        [Tooltip("Escala do canhão para uma casa de 1,2 unidade (acompanha o tamanho da casa).")]
        [Min(0.01f)] public float cannonScale = 0.576f;
        [Tooltip("Rotação da arte para o cano apontar para cima (a arte aponta para a direita).")]
        public float artAngle = 90f;
        [Tooltip("Distância em pixels do centro da imagem até a boca do cano.")]
        public float muzzleOffsetPx = 123f;
        [Tooltip("Distância em pixels do centro da imagem até a ponta de trás (onde o pavio prende).")]
        public float breechOffsetPx = 168f;
        [Tooltip("Quanto a boca do cano entra na casa de baixo da coluna, em casas (0 = encostada na borda do tabuleiro).")]
        public float muzzleIntoBoardCells = 0.28f;

        [Header("1. Entrada")]
        [Min(0f)] public float entranceSeconds = 0.38f;
        [Tooltip("De quantas casas abaixo o canhão sobe ao aparecer.")]
        [Min(0f)] public float entranceSlideCells = 0.7f;
        public Color entranceGlowColor = new Color(0.85f, 0.35f, 1f, 1f);
        public Color entranceRingColor = new Color(1f, 0.82f, 0.35f, 1f);
        [Range(0f, 1f)] public float entranceGlowAlpha = 0.6f;
        [Min(0)] public int entranceSparkles = 6;
        [Tooltip("Aura mágica atrás do canhão enquanto ele está em cena.")]
        public Color auraColor = new Color(0.7f, 0.3f, 1f, 1f);
        [Range(0f, 1f)] public float auraAlpha = 0.3f;

        [Header("2. Pavio")]
        [Min(0f)] public float fuseSeconds = 0.7f;
        [Min(0f)] public float fuseLengthCells = 0.75f;
        [Min(0f)] public float fuseWidthCells = 0.07f;
        [Tooltip("+1 = pavio sai para a direita, -1 = para a esquerda.")]
        public float fuseSide = 1f;
        [Range(4, 24)] public int fuseSegments = 12;
        public Color fuseColorA = new Color(0.95f, 0.86f, 0.64f, 1f);
        public Color fuseColorB = new Color(0.74f, 0.58f, 0.36f, 1f);
        public Color fuseSparkColor = new Color(1f, 0.72f, 0.22f, 1f);
        [Min(0f)] public float fuseSparkSizeCells = 0.5f;
        [Min(0f)] public float fuseSparksPerSecond = 45f;
        [Tooltip("Quanto o canhão incha enquanto o pavio queima (0,12 = 12%).")]
        [Min(0f)] public float fuseSwell = 0.12f;
        [Tooltip("Tremor do canhão no fim do pavio, em casas.")]
        [Min(0f)] public float fuseTrembleCells = 0.018f;
        [Tooltip("Luz que toma conta do canhão conforme carrega (cópia aditiva da própria arte).")]
        public Color chargeColor = new Color(1f, 0.6f, 0.25f, 1f);
        [Range(0f, 1f)] public float chargeAlpha = 0.55f;
        [Tooltip("Pontinhos de luz sugados para a boca do cano durante a carga.")]
        [Min(0)] public int chargeMotes = 8;

        [Header("3. Disparo e coice")]
        [Min(0.05f)] public float muzzleFlashSeconds = 0.3f;
        [Min(0f)] public float muzzleFlashSizeCells = 2.4f;
        public Color muzzleGlowColor = new Color(1f, 0.78f, 0.3f, 1f);
        public Color muzzleBurstColor = new Color(1f, 0.52f, 0.14f, 1f);
        public Color muzzleRingColor = new Color(1f, 0.35f, 0.85f, 1f);
        [Min(0)] public int muzzleSparks = 20;
        [Tooltip("Quanto o canhão recua com o coice, em casas.")]
        [Min(0f)] public float recoilCells = 0.5f;
        [Tooltip("Tempo total do coice (recuo + volta).")]
        [Min(0.05f)] public float recoilSeconds = 0.55f;
        [Tooltip("Curva do coice: tempo 0-1, valor = fração da distância de recuo (pico no começo, volta devagar, pequeno quique).")]
        public AnimationCurve recoilCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.13f, 1f), new Keyframe(0.62f, 0.1f), new Keyframe(0.82f, -0.07f), new Keyframe(1f, 0f));
        [Tooltip("Amassa/estica do cano no coice (0,16 = 16%).")]
        [Range(0f, 0.5f)] public float recoilSquash = 0.16f;
        [Tooltip("Balanço do canhão no coice, em graus.")]
        [Min(0f)] public float recoilTiltDegrees = 4f;
        [Tooltip("Tempo que o canhão fica em cena depois do tiro, antes de sumir.")]
        [Min(0f)] public float cannonLingerSeconds = 0.6f;
        [Min(0.01f)] public float cannonFadeSeconds = 0.28f;

        [Header("4. Bala de canhão")]
        [Tooltip("Velocidade da bala, em casas por segundo. Menor = mais lento e mais legível.")]
        [Min(0.5f)] public float ballSpeedCells = 12f;
        [Min(0.05f)] public float ballSizeCells = 0.62f;
        [Tooltip("Giro da bala em graus por segundo (útil com um sprite próprio).")]
        public float ballSpinDegrees;
        [Tooltip("A peça estoura quando a frente da bala chega a esta distância do centro dela, em casas.")]
        public float hitLeadCells = 0.35f;
        [Tooltip("Chama laranja em volta da bala.")]
        public Color ballFlameColor = new Color(1f, 0.5f, 0.12f, 1f);
        [Tooltip("Franja mágica (magenta/violeta) por fora da chama.")]
        public Color ballFringeColor = new Color(0.9f, 0.3f, 1f, 1f);
        [Tooltip("Miolo quente na frente da bala. É o único ponto quase branco do efeito: mantenha pequeno.")]
        public Color ballCoreColor = new Color(1f, 0.95f, 0.72f, 1f);
        [Range(0f, 1f)] public float ballFlameAlpha = 0.75f;
        [Range(0f, 1f)] public float ballFringeAlpha = 0.4f;
        [Range(0f, 1f)] public float ballCoreAlpha = 0.85f;

        [Header("4. Rastro de fogo")]
        [Min(0f)] public float trailLengthCells = 3.2f;
        [Min(0f)] public float trailWidthCells = 0.85f;
        [Tooltip("Afunilamento: 1 = reto até a ponta, menor = mais gordo perto da bala.")]
        [Range(0.2f, 3f)] public float trailTaper = 0.75f;
        [Tooltip("Ondulação da largura (labaredas).")]
        [Range(0f, 0.6f)] public float trailWobble = 0.2f;
        [Tooltip("Balanço lateral da ponta do rastro, em casas.")]
        [Min(0f)] public float trailSwayCells = 0.14f;
        public Color trailFringeColor = new Color(0.82f, 0.25f, 1f, 1f);
        public Color trailFlameColor = new Color(1f, 0.42f, 0.08f, 1f);
        public Color trailGoldColor = new Color(1f, 0.74f, 0.2f, 1f);
        public Color trailCoreColor = new Color(1f, 0.93f, 0.66f, 1f);
        [Range(0f, 1f)] public float trailFringeAlpha = 0.45f;
        [Range(0f, 1f)] public float trailFlameAlpha = 0.8f;
        [Range(0f, 1f)] public float trailGoldAlpha = 0.7f;
        [Range(0f, 1f)] public float trailCoreAlpha = 0.6f;
        [Tooltip("Tempo que o rastro leva para sumir depois que a bala sai ou estoura.")]
        [Min(0.01f)] public float trailFadeSeconds = 0.3f;

        [Header("4. Brasas e faíscas")]
        [Tooltip("Brasas soltas por casa percorrida.")]
        [Min(0f)] public float embersPerCell = 6f;
        [Min(0f)] public float emberSizeCells = 0.1f;
        [Min(0.05f)] public float emberLifeSeconds = 0.6f;
        public Color emberHotColor = new Color(1f, 0.8f, 0.3f, 1f);
        public Color emberColdColor = new Color(1f, 0.3f, 0.06f, 1f);
        public Color emberMagicColor = new Color(0.95f, 0.35f, 1f, 1f);
        [Tooltip("Parte das brasas com a cor mágica.")]
        [Range(0f, 1f)] public float emberMagicShare = 0.22f;
        [Tooltip("Limite de partículas vivas ao mesmo tempo (celular).")]
        [Min(16)] public int maxParticles = 180;

        [Header("4. Impacto em cada peça")]
        [Min(0.05f)] public float impactSeconds = 0.32f;
        [Min(0f)] public float impactSizeCells = 1.5f;
        [Range(0f, 1f)] public float impactAlpha = 0.85f;
        [Min(0)] public int impactSparks = 7;
        [Tooltip("Cor usada quando a casa não tem peça (ou a cor não pôde ser lida).")]
        public Color impactFallbackColor = new Color(1f, 0.74f, 0.25f, 1f);
        [Tooltip("As peças da coluna não ganham também o efeito de combinação comum (o impacto do canhão já cobre).")]
        public bool suppressMatchEffect = true;
        [Tooltip("Segura a queda das peças até a bala terminar + este tempo (mesmo mecanismo do foguete).")]
        [Min(0f)] public float fallHoldAfterLastHit = 0.25f;

        [Header("5. Saída")]
        [Tooltip("Coluna livre até o topo: quantas casas a bala ainda voa depois da última peça, sumindo.")]
        [Min(0f)] public float exitCells = 2.5f;
        [Tooltip("Coluna barrada (caixote, bloco): tamanho do estouro da bala na última casa, em casas.")]
        [Min(0f)] public float endBurstSizeCells = 2.6f;
        [Min(0)] public int endBurstSparks = 16;
        [Tooltip("Tempo extra para brasas e faíscas terminarem depois que a bala some.")]
        [Min(0f)] public float afterglowSeconds = 0.6f;

        [Header("Tela escura")]
        [Range(0f, 1f)] public float dimAlpha = 0.55f;
        [Min(0.01f)] public float dimFadeSeconds = 0.18f;
        [Tooltip("Tempo que a tela continua escura depois que a bala some.")]
        [Min(0f)] public float dimHoldAfterBall = 0.1f;

        [Header("Tremor de câmera")]
        public bool cameraShake = true;
        [Tooltip("Tremor no disparo, em casas.")]
        [Min(0f)] public float fireShakeCells = 0.07f;
        [Min(0.01f)] public float fireShakeSeconds = 0.28f;
        [Tooltip("Tremidinha a cada peça atingida, em casas.")]
        [Min(0f)] public float hitShakeCells = 0.012f;

        [Header("Sons (vazio = sem som; o tiro usa o som de foguete do jogo)")]
        public AudioClip entranceSound;
        public AudioClip fuseSound;
        public AudioClip shotSound;

        /// true quando há pelo menos um quadro do canhão parado.
        public bool HasCannonArt
        {
            get
            {
                if (IdleFrames == null) return false;
                foreach (var texture in IdleFrames) if (texture != null) return true;
                return false;
            }
        }
    }
}
