// Tela de ABERTURA DE BAÚ: fundo preto em tela cheia, por cima de toda a interface. O baú entra, treme e deixa escapar
// luz pela tampa (antecipação), abre com um clarão e os power-ups saem de dentro dele em curvas, cada um com rastro de
// luz e faíscas, até pousarem em fila (ou em grade, se forem muitos) acima do baú, com a quantidade ("x2") e um brilho
// com raios girando atrás. Um toque durante a animação pula para o final; um toque no final fecha e avisa quem chamou.
//
// Como chamar (de qualquer lugar, em qualquer cena):
//     ChestOpenScreen.Show(new[] { new ChestReward(BoostType.Hammer, 2), new ChestReward(BoostType.Arrow, 1) }, () => { ... });
//
// IMPORTANTE: a tela SÓ MOSTRA os prêmios. Ela não credita nada ao jogador: quem abre o baú é que deve creditar
// (por exemplo InitScript.Instance.BuyBoost(tipo, 0, quantidade)), antes ou depois de chamar Show.
//
// Tudo é montado em código a partir dos ajustes deste componente, que ficam no prefab
// Resources/Effects/Chest/ChestOpenScreen (criado pelo ChestOpenBuilder; menu Royal Aves > Efeitos > Baú). Sem o prefab a
// tela ainda funciona, com os valores padrão e as máscaras geradas em código.
//
// ARTE DO BAÚ: enquanto os campos "Chest Body / Chest Lid / Chest Interior" estiverem vazios, é desenhado um baú
// provisório em código (ChestOpenArt). Para usar a arte de verdade basta arrastar os sprites para esses campos no prefab.
using System;
using System.Collections.Generic;
using RoyalAves.Meta;
using SweetSugar.Scripts;
using SweetSugar.Scripts.GUI.Boost;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoyalAves.Effects
{
    /// Um prêmio que sai do baú: o power-up e quantos.
    [Serializable]
    public struct ChestReward
    {
        public BoostType type;
        [Min(1)] public int amount;
        [Tooltip("Opcional: figura própria deste prêmio. Vazio = a figura do power-up cadastrada na tela/catálogos.")]
        public Sprite icon;

        public ChestReward(BoostType type, int amount, Sprite icon = null)
        {
            this.type = type;
            this.amount = amount;
            this.icon = icon;
        }
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("Royal Aves/Chest Open Screen")]
    public sealed class ChestOpenScreen : MonoBehaviour
    {
        public const string ResourcePath = "Effects/Chest/ChestOpenScreen";
        const float TwoPi = 6.2831853f;

        [Serializable]
        public class IconEntry
        {
            public BoostType type = BoostType.Arrow;
            public Sprite icon;
        }

        // ------------------------------------------------------------------------------------------ ajustes

        [Header("Arte do baú (vazio = baú provisório desenhado em código)")]
        [Tooltip("Corpo do baú fechado, SEM a tampa. Enquanto estiver vazio, corpo, tampa e interior são os provisórios.")]
        public Sprite chestBody;
        [Tooltip("Tampa, em peça separada (ela sobe e inclina ao abrir). Vazio com o corpo preenchido = sem tampa separada.")]
        public Sprite chestLid;
        [Tooltip("Boca/interior do baú aberto (aparece atrás do corpo quando a tampa sobe). Opcional.")]
        public Sprite chestInterior;
        [Tooltip("Opcional: imagem do baú INTEIRO já aberto. Se preenchida, troca o corpo por ela no momento da abertura e esconde a tampa e o interior.")]
        public Sprite chestOpened;
        [Tooltip("Liga: a tampa é desenhada na frente do corpo. Desligado: atrás (o corpo cobre a base da tampa).")]
        public bool lidInFront;
        [Tooltip("Largura do corpo do baú, em unidades do palco (o palco tem 1080 de largura).")]
        [Min(10)] public float chestWidth = 470f;
        [Tooltip("Centro do corpo do baú no palco (0,0 = centro da tela).")]
        public Vector2 chestPosition = new Vector2(0f, -470f);
        [Tooltip("Largura da tampa, em múltiplos da largura do baú.")]
        [Min(0.05f)] public float lidWidth = 1f;
        [Tooltip("Onde fica a BASE da tampa fechada, a partir do meio da borda de cima do corpo (em larguras do baú).")]
        public Vector2 lidOffset = new Vector2(0f, -0.03f);
        [Tooltip("Largura do interior, em múltiplos da largura do baú.")]
        [Min(0.05f)] public float interiorWidth = 0.9f;
        [Tooltip("Centro do interior, a partir do meio da borda de cima do corpo (em larguras do baú).")]
        public Vector2 interiorOffset = new Vector2(0f, 0.01f);
        [Tooltip("De onde os power-ups saem, a partir do meio da borda de cima do corpo (em larguras do baú).")]
        public Vector2 mouthOffset = new Vector2(0f, 0.02f);

        [Header("Tampa abrindo")]
        [Tooltip("Quanto a tampa sobe ao abrir, em larguras do baú.")]
        public float lidOpenLift = 0.2f;
        [Tooltip("Inclinação da tampa aberta, em graus.")]
        public float lidOpenAngle = -7f;
        [Tooltip("Escala da tampa aberta (Y menor = tampa deitada para trás).")]
        public Vector2 lidOpenScale = new Vector2(0.96f, 0.7f);
        [Tooltip("Quanto o baú incha no instante em que abre.")]
        [Range(0f, 0.5f)] public float openPunch = 0.12f;

        [Header("Fundo")]
        public Color backgroundColor = Color.black;
        [Range(0f, 1f)] public float backgroundAlpha = 1f;
        [Tooltip("Luz difusa atrás do baú.")]
        public Color ambientColor = new Color(0.506f, 0.169f, 1f, 1f);
        [Range(0f, 1f)] public float ambientAlpha = 0.4f;
        [Min(0)] public float ambientSize = 1500f;

        [Header("Tempo (segundos)")]
        [Min(0)] public float fadeIn = 0.25f;
        [Tooltip("Entrada do baú (cai e cresce com um quique).")]
        [Min(0.01f)] public float chestEnter = 0.45f;
        [Tooltip("Antecipação: o baú treme e a luz escapa pela tampa.")]
        [Min(0)] public float anticipation = 0.95f;
        [Tooltip("Tempo da tampa abrindo.")]
        [Min(0.01f)] public float openDuration = 0.32f;
        [Tooltip("Espera entre abrir e o primeiro power-up sair.")]
        [Min(0)] public float firstLaunchDelay = 0.08f;
        [Tooltip("Intervalo entre a saída de um power-up e a do próximo.")]
        [Min(0)] public float launchStagger = 0.16f;
        [Tooltip("Duração do voo de cada power-up.")]
        [Min(0.05f)] public float flightDuration = 0.85f;
        [Tooltip("Tempo para o brilho atrás do power-up acender depois que ele pousa.")]
        [Min(0.01f)] public float glowFade = 0.45f;
        [Tooltip("Espera, depois de tudo pousar, até aparecer o \"toque para continuar\".")]
        [Min(0)] public float continueDelay = 0.5f;
        [Min(0)] public float fadeOut = 0.25f;
        [Tooltip("Liga: um toque durante a animação pula direto para o final.")]
        public bool tapSkips = true;

        [Header("Antecipação")]
        [Tooltip("Inclinação máxima do tremor, em graus.")]
        public float shakeAngle = 5f;
        [Tooltip("Tremidas por segundo.")]
        public float shakeFrequency = 9f;
        [Tooltip("Quantos pulinhos o baú dá.")]
        [Min(0)] public int bounceCount = 2;
        public float bounceHeight = 26f;
        [Range(0f, 0.4f)] public float squash = 0.08f;
        [Tooltip("Quanto a tampa bate (sobe) durante o tremor.")]
        public float lidRattle = 14f;
        [Tooltip("Luz que escapa pela fresta da tampa.")]
        public Color leakColor = new Color(1f, 0.76f, 0.28f, 1f);
        [Range(0f, 1f)] public float leakAlpha = 0.9f;
        [Min(0)] public int leakSparkleCount = 8;

        [Header("Abertura (clarão)")]
        public Color flashColor = new Color(1f, 0.86f, 0.55f, 1f);
        [Range(0f, 1f)] public float flashAlpha = 0.9f;
        [Min(0)] public float flashSize = 1500f;
        [Min(0.05f)] public float flashDuration = 0.55f;
        public Color ringColor = new Color(0.82f, 0.169f, 1f, 1f);
        [Range(0f, 1f)] public float ringAlpha = 0.8f;
        [Min(0)] public float ringSize = 1300f;
        [Min(0.05f)] public float ringDuration = 0.6f;
        [Tooltip("Raios que estouram de dentro do baú e depois ficam girando devagar atrás dele.")]
        [Min(0)] public int burstRayCount = 14;
        public Color burstRayColor = new Color(1f, 0.76f, 0.28f, 1f);
        [Range(0f, 1f)] public float burstRayAlpha = 0.6f;
        [Tooltip("Intensidade dos raios depois do estouro (fração da intensidade do estouro).")]
        [Range(0f, 1f)] public float burstRayIdle = 0.35f;
        [Min(0)] public float burstRayLength = 640f;
        [Min(1)] public float burstRayWidth = 70f;
        [Tooltip("Graus por segundo.")]
        public float burstRaySpin = 10f;
        [Tooltip("Coluna de luz que sobe da boca do baú.")]
        public Color beamColor = new Color(1f, 0.76f, 0.28f, 1f);
        [Range(0f, 1f)] public float beamAlpha = 0.6f;
        [Tooltip("Intensidade da coluna depois do estouro (fração).")]
        [Range(0f, 1f)] public float beamIdle = 0.45f;
        [Tooltip("Largura da coluna, em larguras do baú.")]
        [Min(0)] public float beamWidth = 1.15f;
        [Min(0)] public float beamHeight = 900f;
        [Tooltip("Luz quente dentro da boca do baú, depois de aberto.")]
        [Range(0f, 1f)] public float innerGlowAlpha = 0.85f;
        [Tooltip("Faíscas que espirram quando o baú abre.")]
        [Min(0)] public int burstSparkleCount = 26;
        [Tooltip("Leque das faíscas, em graus (0 = direita, 90 = para cima).")]
        public Vector2 burstFan = new Vector2(25f, 155f);
        public Vector2 burstSparkleSpeed = new Vector2(700f, 2000f);
        public float burstGravity = 900f;

        [Header("Voo dos power-ups")]
        [Tooltip("Quanto o power-up sobe ao sair do baú (altura do 1º ponto de controle da curva).")]
        public float launchHeight = 620f;
        [Tooltip("Quanto ele sai para o lado OPOSTO ao do destino antes de fazer a curva.")]
        public float launchSpread = 240f;
        [Tooltip("Quanto ele passa por cima do lugar onde vai pousar.")]
        public float arcOver = 240f;
        [Tooltip("Quanto ele chega por fora (pelo lado) do lugar onde vai pousar.")]
        public float arcSide = 200f;
        [Tooltip("Ondulação lateral durante o voo (o movimento \"mágico\").")]
        public float swirlAmplitude = 70f;
        [Tooltip("Quantas ondulações ao longo do voo.")]
        public float swirlTurns = 1.5f;
        [Tooltip("Quanto ele gira durante o voo, em graus (para no lugar ao pousar).")]
        public float spinDegrees = 300f;
        [Tooltip("Tamanho com que sai do baú (1 = tamanho final).")]
        [Range(0.02f, 1f)] public float startScale = 0.2f;

        [Header("Rastro e faíscas do voo")]
        [Min(2)] public int trailSegments = 18;
        [Tooltip("Comprimento do rastro, em segundos de voo.")]
        [Min(0.02f)] public float trailSeconds = 0.3f;
        [Tooltip("Largura da fita colorida, em múltiplos do tamanho do power-up.")]
        [Min(0)] public float trailWidth = 0.5f;
        [Tooltip("Largura do miolo claro, em múltiplos do tamanho do power-up.")]
        [Min(0)] public float trailCoreWidth = 0.14f;
        [Range(0f, 1f)] public float trailAlpha = 0.85f;
        [Range(0f, 1f)] public float trailCoreAlpha = 0.7f;
        [Tooltip("Tempo que o rastro leva para sumir depois do pouso.")]
        [Min(0.01f)] public float trailFade = 0.3f;
        [Tooltip("Bola de luz atrás do power-up enquanto voa: tamanho em múltiplos do power-up.")]
        [Min(0)] public float headGlowSize = 1.7f;
        [Range(0f, 1f)] public float headGlowAlpha = 0.6f;
        [Tooltip("Faíscas soltas ao longo do voo, por power-up.")]
        [Min(0)] public int flightSparkles = 12;
        [Min(0.05f)] public float sparkleLife = 0.7f;
        [Tooltip("Tamanho mínimo e máximo das faíscas.")]
        public Vector2 sparkleSize = new Vector2(24f, 64f);
        [Tooltip("Quanto as faíscas se afastam de onde nasceram, por segundo.")]
        public float sparkleDrift = 90f;
        public float sparkleGravity = 160f;

        [Header("Power-ups pousados")]
        [Tooltip("Tamanho da figura de cada power-up.")]
        [Min(10)] public float iconSize = 250f;
        [Tooltip("Quando o baú dá um prêmio só, a figura fica maior (multiplicador).")]
        [Min(0.1f)] public float singleScale = 1.3f;
        [Min(1)] public int maxPerRow = 3;
        [Tooltip("Distância entre os centros, em múltiplos do tamanho da figura (X = na fila, Y = entre filas).")]
        public Vector2 spacing = new Vector2(1.3f, 1.5f);
        [Tooltip("Centro da área onde eles pousam (0,0 = centro da tela).")]
        public Vector2 rewardsCenter = new Vector2(0f, 330f);
        [Tooltip("Tamanho máximo da área dos prêmios; passando disso, as figuras encolhem para caber.")]
        public Vector2 rewardsArea = new Vector2(980f, 800f);
        [Tooltip("Quanto a figura incha ao pousar.")]
        [Range(0f, 1f)] public float arrivalPop = 0.22f;
        [Min(0.01f)] public float popDuration = 0.3f;
        [Tooltip("Flutuação suave depois de pousar (altura em unidades do palco e ciclos por segundo).")]
        public float idleBob = 10f;
        public float bobSpeed = 0.5f;

        [Header("Brilho atrás de cada power-up")]
        [Tooltip("Modelo do brilho (glow + raios girando + faíscas): um UIMagicGlow, filho desligado deste prefab. Ajuste nele.")]
        public UIMagicGlow glowTemplate;
        [Tooltip("Tamanho do brilho, em múltiplos do tamanho da figura.")]
        [Min(0)] public float glowSize = 1.9f;
        [Tooltip("Liga: o glow de cada power-up usa uma cor da paleta (os raios e faíscas continuam com as cores do modelo).")]
        public bool tintGlowWithPalette = true;
        [Tooltip("Cores usadas em sequência: rastro, anel de chegada e glow de cada power-up.")]
        public Color[] palette =
        {
            new Color(0.506f, 0.169f, 1f, 1f),      // violeta  #812BFF
            new Color(1f, 0.341f, 0.776f, 1f),      // rosa     #FF57C6
            new Color(0.286f, 0.867f, 0.961f, 1f),  // ciano    #49DDF5
            new Color(0.82f, 0.169f, 1f, 1f),       // magenta  #D12BFF
            new Color(1f, 0.76f, 0.28f, 1f),        // dourado  #FFC247
        };

        [Header("Textos")]
        [Tooltip("Vazio = a fonte de títulos do jogo (RoyalAvesFeatures > Display Font).")]
        public TMP_FontAsset font;
        [Tooltip("{0} = quantidade.")]
        public string amountFormat = "x{0}";
        public bool showAmountOfOne = true;
        public float amountSize = 78f;
        public Color amountColor = Color.white;
        [Tooltip("Posição da quantidade a partir do centro da figura, em múltiplos do tamanho da figura.")]
        public Vector2 amountOffset = new Vector2(0f, -0.62f);
        public Color textOutlineColor = new Color(0.157f, 0.055f, 0.275f, 1f);
        [Range(0f, 1f)] public float textOutlineWidth = 0.22f;
        public string titlePt = "Você ganhou!";
        public string titleEn = "You got!";
        public float titleSize = 96f;
        public Color titleColor = new Color(1f, 0.82f, 0.36f, 1f);
        public Vector2 titlePosition = new Vector2(0f, 820f);
        public string continuePt = "Toque para continuar";
        public string continueEn = "Tap to continue";
        public float continueSize = 46f;
        public Color continueColor = new Color(1f, 1f, 1f, 0.9f);
        public Vector2 continuePosition = new Vector2(0f, -860f);

        [Header("Figuras dos power-ups (têm prioridade sobre os catálogos)")]
        public List<IconEntry> icons = new List<IconEntry>();
        [Tooltip("Usada quando um power-up não tem figura em lugar nenhum.")]
        public Sprite fallbackIcon;

        [Header("Máscaras e material aditivo (vazio = geradas em código)")]
        public Sprite glowSprite;
        public Sprite sparkleSprite;
        public Sprite dotSprite;
        public Sprite ringSprite;
        public Material additive;

        [Header("Sons (opcionais)")]
        public AudioClip openSound;
        public AudioClip launchSound;
        public AudioClip arriveSound;

        [Header("Tela")]
        [Tooltip("Ordem do Canvas: acima de todas as outras telas do jogo.")]
        public int sortingOrder = 32750;
        [Tooltip("Tamanho do palco: tudo é posicionado nesta área, que encolhe ou cresce para caber em qualquer proporção de tela.")]
        public Vector2 designSize = new Vector2(1080f, 1920f);

        // ------------------------------------------------------------------------------------------ estado

        sealed class Item
        {
            public Vector2 slot, control1, control2;
            public float size, side, launch, arrive, phase;
            public Color color;
            public Image icon, headGlow, ring;
            public Image[] sparkles;
            public ChestTrailGraphic trail, core;
            public RectTransform glowRect;
            public UIMagicGlow glow;
            public TMP_Text amount;
            public bool launched, arrived;
        }

        readonly List<ChestReward> rewards = new List<ChestReward>();
        readonly List<Item> items = new List<Item>();
        readonly List<Vector2> trailPoints = new List<Vector2>();
        Action onClosed;
        RectTransform root, stage, chestBack, chestFront, raysRoot, bodyRect, lidRect;
        CanvasGroup group;
        Image bodyImage, lidImage, interiorImage, ambient, leak, innerGlow, beam, flash, ring;
        Image[] burstRays = new Image[0], burstSparkles = new Image[0], leakSparkles = new Image[0];
        TMP_Text title, continueLabel;
        Material ownedAdditive;
        Vector2 ground, mouth, bodyTop, lidClosed;
        float bodyHeight, time, closeTime;
        float tEnter, tAntic, tOpen, tSettled, tContinue;
        bool built, closing, skipped, openPlayed, showingOpened;

        public static ChestOpenScreen Current { get; private set; }
        public static bool IsOpen => Current != null;

        // ------------------------------------------------------------------------------------------ fachada

        /// Abre a tela com os prêmios dados (um ou mais). onClosed é chamado UMA vez, quando a tela termina de fechar.
        /// A tela só mostra: creditar os power-ups é com quem chamou. Devolve false se não deu para abrir (nesse caso
        /// onClosed é chamado na hora, para o fluxo de quem chamou nunca ficar preso).
        public static bool Show(IList<ChestReward> rewards, Action onClosed = null)
        {
            ChestOpenScreen view = null;
            try
            {
                if (!Application.isPlaying) throw new InvalidOperationException("a tela só abre com o jogo rodando (Play)");
                if (rewards == null || rewards.Count == 0) throw new ArgumentException("nenhum prêmio para mostrar");
                if (Current != null) Current.Finish();

                var prefab = Resources.Load<ChestOpenScreen>(ResourcePath);
                if (prefab != null) view = Instantiate(prefab);
                else
                {
                    Debug.LogWarning("Abertura de baú: prefab Resources/" + ResourcePath + " não encontrado; usando os valores padrão. " +
                                     "Crie-o pelo menu Royal Aves > Efeitos > Baú > Recriar prefab.");
                    view = new GameObject().AddComponent<ChestOpenScreen>();
                }
                view.name = "Abertura de baú";
                foreach (var reward in rewards) view.rewards.Add(reward);
                view.Build();
                view.onClosed = onClosed;
                Current = view;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("Abertura de baú: não consegui abrir a tela (" + e.Message + ").\n" + e);
                if (view != null) { view.onClosed = null; Destroy(view.gameObject); }
                if (Current == view) Current = null;
                onClosed?.Invoke();
                return false;
            }
        }

        /// Atalho para um prêmio só.
        public static bool Show(BoostType type, int amount, Action onClosed = null) =>
            Show(new[] { new ChestReward(type, amount) }, onClosed);

        /// Fecha a tela aberta (com o fade de saída). onClosed de quem abriu é chamado normalmente.
        public static void Hide()
        {
            if (Current != null) Current.BeginClose();
        }

        // ------------------------------------------------------------------------------------------ ciclo de vida

        void Update()
        {
            if (!built) return;
            var delta = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            time += delta;
            if (!closing && EventSystem.current == null && Input.GetMouseButtonDown(0)) OnTap();   // cena sem EventSystem
            Evaluate(time);
            if (!closing)
            {
                group.alpha = fadeIn > 0f ? Mathf.Clamp01(time / fadeIn) : 1f;
                return;
            }
            closeTime += delta;
            group.alpha = fadeOut > 0f ? 1f - Mathf.Clamp01(closeTime / fadeOut) : 0f;
            if (closeTime >= fadeOut) Finish();
        }

        void OnTap()
        {
            if (!built || closing || time < Mathf.Max(0.3f, fadeIn)) return;
            if (time < tSettled)
            {
                if (!tapSkips) return;
                time = tSettled;
                skipped = true;
                return;
            }
            if (time < tSettled + 0.2f) return;   // evita fechar com o mesmo toque duplo que pulou a animação
            BeginClose();
        }

        void BeginClose()
        {
            if (closing) return;
            closing = true;
            closeTime = 0f;
        }

        void Finish()
        {
            if (Current == this) Current = null;
            var callback = onClosed;
            onClosed = null;
            built = false;
            Destroy(gameObject);
            if (callback == null) return;
            try { callback(); }
            catch (Exception e) { Debug.LogException(e); }
        }

        void OnApplicationQuit() => onClosed = null;

        void OnDestroy()
        {
            if (Current == this) Current = null;
            if (ownedAdditive != null) Destroy(ownedAdditive);
            // Destruída por fora (troca de cena, por exemplo): quem abriu ainda precisa ser avisado.
            var callback = onClosed;
            onClosed = null;
            if (callback == null) return;
            try { callback(); }
            catch (Exception e) { Debug.LogException(e); }
        }

        // ------------------------------------------------------------------------------------------ recursos

        Sprite GlowSprite => glowSprite != null ? glowSprite : ChestOpenArt.Glow;
        Sprite SparkleSprite => sparkleSprite != null ? sparkleSprite : ChestOpenArt.Star;
        Sprite DotSprite => dotSprite != null ? dotSprite : ChestOpenArt.Glow;
        Sprite RingSprite => ringSprite != null ? ringSprite : ChestOpenArt.Ring;

        /// Material aditivo: o do prefab; sem ele, um criado com o shader do projeto; sem o shader, o padrão da UI.
        Material Additive
        {
            get
            {
                if (additive != null) return additive;
                if (ownedAdditive != null) return ownedAdditive;
                var shader = Shader.Find("RoyalAves/ImpactSprite");
                if (shader == null) return null;
                ownedAdditive = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                ownedAdditive.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                ownedAdditive.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                return ownedAdditive;
            }
        }

        Color PaletteColor(int index) =>
            palette != null && palette.Length > 0 ? palette[((index % palette.Length) + palette.Length) % palette.Length] : Color.white;

        Sprite IconFor(ChestReward reward)
        {
            if (reward.icon != null) return reward.icon;
            if (icons != null)
                foreach (var entry in icons)
                    if (entry != null && entry.type == reward.type && entry.icon != null) return entry.icon;
            var shop = BoostShopCatalog.Find(reward.type);
            if (shop != null && shop.icon != null) return shop.icon;
            var intro = BoosterIntroCatalog.Instance;
            if (intro != null)
                foreach (var entry in intro.entries)
                    if (entry != null && entry.type == reward.type && entry.icon != null) return entry.icon;
            return fallbackIcon != null ? fallbackIcon : ChestOpenArt.Star;
        }

        /// Valores padrão do brilho atrás de cada power-up (usados no modelo do prefab e quando não há modelo).
        public static void ApplyDefaultGlow(UIMagicGlow glow)
        {
            glow.glowColor = new Color(1f, 0.76f, 0.28f, 1f);
            glow.rayColor = new Color(1f, 0.84f, 0.42f, 1f);
            glow.sparkleColor = new Color(1f, 0.93f, 0.66f, 1f);
            glow.glowAlpha = 0.9f; glow.glowSize = 1f; glow.glowPulse = 0.07f; glow.glowSpeed = 0.6f;
            glow.innerGlowAlpha = 0.45f; glow.innerGlowSize = 0.5f;
            glow.proceduralRays = true;
            glow.rayCount = 18; glow.rayAlpha = 0.4f; glow.rayLength = 1f; glow.rayLengthVariation = 0.45f; glow.rayWidth = 0.085f;
            glow.rayFlicker = 0.45f; glow.raySpin = 14f; glow.rayHaloWidth = 3f; glow.rayHaloAlpha = 0.35f;
            glow.sparkleCount = 10; glow.sparkleSizeMin = 0.035f; glow.sparkleSizeMax = 0.09f; glow.sparkleRadius = new Vector2(0.3f, 1f);
            glow.sparklePeriod = 2.2f; glow.sparkleRise = 0f; glow.sparkleOutward = true; glow.sparkleStarShare = 0.8f;
            glow.dotCount = 0; glow.ringCount = 0; glow.staticRingAlpha = 0f;
            glow.autoPlay = true;
        }

        // ------------------------------------------------------------------------------------------ montagem

        static RectTransform NewRect(Transform parent, string label)
        {
            var go = new GameObject(label, typeof(RectTransform)) { layer = 5 };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        Image NewImage(Transform parent, string label, Sprite sprite, bool light)
        {
            var rect = NewRect(parent, label);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            if (light)
            {
                var material = Additive;
                if (material != null) image.material = material;
                image.color = new Color(1f, 1f, 1f, 0f);
                image.enabled = false;
            }
            return image;
        }

        Image[] NewImages(Transform parent, string label, int count, Func<int, Sprite> sprite)
        {
            var images = new Image[Mathf.Max(0, count)];
            for (var i = 0; i < images.Length; i++) images[i] = NewImage(parent, label + " " + i, sprite(i), true);
            return images;
        }

        TextMeshProUGUI NewText(Transform parent, string label, float size, Color color, Vector2 bounds)
        {
            var rect = NewRect(parent, label);
            rect.sizeDelta = bounds;
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            var face = font;
            if (face == null && LobbyFeaturesConfig.Instance != null) face = LobbyFeaturesConfig.Instance.displayFont;
            if (face != null) text.font = face;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            if (textOutlineWidth > 0f)
            {
                text.outlineWidth = textOutlineWidth;
                text.outlineColor = textOutlineColor;
            }
            return text;
        }

        static float Aspect(Sprite sprite) =>
            sprite != null && sprite.rect.width > 0.01f ? sprite.rect.height / sprite.rect.width : 1f;

        void Build()
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(CanvasGroup)) { layer = 5 };
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            group = canvasObject.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            root = (RectTransform)canvasObject.transform;

            // Fundo preto: cobre tudo e recebe o toque (nada do jogo atrás é clicável enquanto a tela está aberta).
            var dim = NewImage(root, "Fundo preto", null, false);
            dim.rectTransform.anchorMin = Vector2.zero;
            dim.rectTransform.anchorMax = Vector2.one;
            dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
            dim.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, backgroundAlpha);
            dim.raycastTarget = true;
            var button = dim.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OnTap);

            stage = NewRect(root, "Palco");
            stage.sizeDelta = designSize;

            // ---- baú: medidas
            var placeholder = chestBody == null;
            var bodySprite = placeholder ? ChestOpenArt.ChestBody : chestBody;
            var lidSprite = placeholder ? ChestOpenArt.ChestLid : chestLid;
            var interiorSprite = placeholder ? ChestOpenArt.ChestInterior : chestInterior;
            bodyHeight = chestWidth * Aspect(bodySprite);
            ground = chestPosition + new Vector2(0f, -bodyHeight * 0.5f);      // ponto de apoio: o baú escala e treme em volta dele
            bodyTop = new Vector2(0f, bodyHeight);                              // relativo ao ponto de apoio
            lidClosed = bodyTop + lidOffset * chestWidth;
            mouth = ground + bodyTop + mouthOffset * chestWidth;

            // ---- camadas, de trás para a frente
            ambient = NewImage(stage, "Luz atrás do baú", GlowSprite, true);
            raysRoot = NewRect(stage, "Raios do estouro");
            raysRoot.anchoredPosition = mouth;
            burstRays = NewImages(raysRoot, "Raio", burstRayCount, _ => ChestOpenArt.Spike);
            foreach (var ray in burstRays) ray.rectTransform.pivot = new Vector2(0f, 0.5f);
            var glowLayer = NewRect(stage, "Brilhos dos power-ups");

            chestBack = NewRect(stage, "Baú (atrás)");
            if (lidSprite != null && !lidInFront) lidImage = NewImage(chestBack, "Tampa", lidSprite, false);
            if (interiorSprite != null)
            {
                interiorImage = NewImage(chestBack, "Interior", interiorSprite, false);
                var width = chestWidth * interiorWidth;
                interiorImage.rectTransform.sizeDelta = new Vector2(width, width * Aspect(interiorSprite));
                interiorImage.rectTransform.anchoredPosition = bodyTop + interiorOffset * chestWidth;
            }
            innerGlow = NewImage(chestBack, "Luz de dentro", GlowSprite, true);
            innerGlow.rectTransform.anchoredPosition = bodyTop + mouthOffset * chestWidth;
            innerGlow.rectTransform.sizeDelta = new Vector2(chestWidth * 1.25f, chestWidth * 0.8f);

            beam = NewImage(stage, "Coluna de luz", ChestOpenArt.Beam, true);
            beam.rectTransform.pivot = new Vector2(0.5f, 0f);
            beam.rectTransform.anchoredPosition = mouth;
            var trailLayer = NewRect(stage, "Rastros");
            var rewardLayer = NewRect(stage, "Power-ups");

            chestFront = NewRect(stage, "Baú (frente)");
            bodyImage = NewImage(chestFront, "Corpo", bodySprite, false);
            bodyRect = bodyImage.rectTransform;
            bodyRect.pivot = new Vector2(0.5f, 0f);
            bodyRect.sizeDelta = new Vector2(chestWidth, bodyHeight);
            if (lidSprite != null && lidInFront) lidImage = NewImage(chestFront, "Tampa", lidSprite, false);
            if (lidImage != null)
            {
                lidRect = lidImage.rectTransform;
                lidRect.pivot = new Vector2(0.5f, 0f);
                var width = chestWidth * lidWidth;
                lidRect.sizeDelta = new Vector2(width, width * Aspect(lidSprite));
                lidRect.anchoredPosition = lidClosed;
            }
            leak = NewImage(chestFront, "Luz na fresta", GlowSprite, true);
            leak.rectTransform.anchoredPosition = lidClosed;

            var fxLayer = NewRect(stage, "Clarão e faíscas");
            leakSparkles = NewImages(fxLayer, "Faísca da fresta", leakSparkleCount, _ => DotSprite);
            ring = NewImage(fxLayer, "Anel da abertura", RingSprite, true);
            flash = NewImage(fxLayer, "Clarão", GlowSprite, true);
            burstSparkles = NewImages(fxLayer, "Faísca da abertura", burstSparkleCount, i => Hash(i, 21) < 0.6f ? SparkleSprite : DotSprite);
            var labelLayer = NewRect(stage, "Quantidades");

            // ---- tempos
            tEnter = fadeIn * 0.5f;
            tAntic = tEnter + chestEnter;
            tOpen = tAntic + anticipation;

            // ---- power-ups
            var slots = Slots(rewards.Count, out var size);
            for (var i = 0; i < rewards.Count; i++)
                items.Add(BuildItem(i, rewards[i], slots[i], size, glowLayer, trailLayer, rewardLayer, fxLayer, labelLayer));
            var last = items.Count > 0 ? items[items.Count - 1].arrive : tOpen + openDuration;
            tSettled = last + glowFade;
            tContinue = tSettled + continueDelay;

            // ---- textos
            if (!string.IsNullOrEmpty(titlePt) || !string.IsNullOrEmpty(titleEn))
            {
                title = NewText(stage, "Título", titleSize, titleColor, new Vector2(1000f, 160f));
                title.rectTransform.anchoredPosition = titlePosition;
                title.gameObject.AddComponent<LocalizedTextTMP>().SetTexts(titlePt, titleEn);
            }
            if (!string.IsNullOrEmpty(continuePt) || !string.IsNullOrEmpty(continueEn))
            {
                continueLabel = NewText(stage, "Toque para continuar", continueSize, continueColor, new Vector2(1000f, 100f));
                continueLabel.rectTransform.anchoredPosition = continuePosition;
                continueLabel.gameObject.AddComponent<LocalizedTextTMP>().SetTexts(continuePt, continueEn);
            }

            built = true;
            Evaluate(0f);
        }

        /// Lugares onde os power-ups pousam: uma fila, ou uma grade centralizada quando passam de "Max Per Row".
        Vector2[] Slots(int count, out float size)
        {
            var perRow = Mathf.Max(1, maxPerRow);
            var rows = Mathf.CeilToInt(count / (float)perRow);
            var columns = Mathf.CeilToInt(count / (float)rows);
            size = iconSize * (count == 1 ? singleScale : 1f);
            var needX = size * ((columns - 1) * spacing.x + 1f);
            var needY = size * ((rows - 1) * spacing.y + 1f);
            var fit = Mathf.Min(1f, rewardsArea.x / Mathf.Max(1f, needX), rewardsArea.y / Mathf.Max(1f, needY));
            size *= fit;

            var slots = new Vector2[count];
            var index = 0;
            for (var row = 0; row < rows; row++)
            {
                var inRow = Mathf.Min(columns, count - index);
                var y = rewardsCenter.y + ((rows - 1) * 0.5f - row) * spacing.y * size;
                for (var column = 0; column < inRow; column++)
                    slots[index++] = new Vector2(rewardsCenter.x + (column - (inRow - 1) * 0.5f) * spacing.x * size, y);
            }
            return slots;
        }

        Item BuildItem(int index, ChestReward reward, Vector2 slot, float size, Transform glowLayer, Transform trailLayer,
            Transform rewardLayer, Transform fxLayer, Transform labelLayer)
        {
            var item = new Item { slot = slot, size = size, color = PaletteColor(index), phase = Hash(index, 31) };
            item.side = slot.x > 1f ? 1f : slot.x < -1f ? -1f : (index % 2 == 0 ? 1f : -1f);
            item.control1 = mouth + new Vector2(-item.side * launchSpread, launchHeight);
            item.control2 = slot + new Vector2(item.side * arcSide, arcOver);
            item.launch = tOpen + firstLaunchDelay + index * launchStagger;
            item.arrive = item.launch + flightDuration;

            // Brilho atrás (glow + raios girando + faíscas): uma cópia do modelo do prefab.
            if (glowSize > 0f)
            {
                if (glowTemplate != null)
                {
                    item.glow = Instantiate(glowTemplate, glowLayer);
                    item.glow.name = "Brilho " + index;
                    item.glowRect = (RectTransform)item.glow.transform;
                    item.glowRect.anchorMin = item.glowRect.anchorMax = item.glowRect.pivot = new Vector2(0.5f, 0.5f);
                    // Se o modelo estava ligado, a cópia traz as imagens que ele já tinha gerado: o Rebuild abaixo refaz tudo.
                    for (var child = item.glowRect.childCount - 1; child >= 0; child--) Destroy(item.glowRect.GetChild(child).gameObject);
                }
                else
                {
                    item.glowRect = NewRect(glowLayer, "Brilho " + index);
                    item.glow = item.glowRect.gameObject.AddComponent<UIMagicGlow>();
                    ApplyDefaultGlow(item.glow);
                }
                var material = Additive;
                if (item.glow.additive == null) item.glow.additive = material != null ? material : Canvas.GetDefaultCanvasMaterial();
                if (item.glow.glowSprite == null) item.glow.glowSprite = GlowSprite;
                if (item.glow.sparkleStar == null) item.glow.sparkleStar = SparkleSprite;
                if (item.glow.sparkleDot == null) item.glow.sparkleDot = DotSprite;
                if (item.glow.ringSprite == null) item.glow.ringSprite = RingSprite;
                if (tintGlowWithPalette) item.glow.glowColor = Color.Lerp(item.color, Color.white, 0.15f);
                item.glow.intensity = 0f;
                item.glowRect.anchoredPosition = slot;
                item.glowRect.sizeDelta = new Vector2(size * glowSize, size * glowSize);
                item.glowRect.localScale = Vector3.one;
                item.glow.gameObject.SetActive(true);
                item.glow.Rebuild();
            }

            item.trail = NewTrail(trailLayer, "Rastro " + index);
            item.core = NewTrail(trailLayer, "Miolo do rastro " + index);
            item.headGlow = NewImage(rewardLayer, "Luz do voo " + index, GlowSprite, true);
            item.icon = NewImage(rewardLayer, "Power-up " + index + " (" + reward.type + ")", IconFor(reward), false);
            item.icon.preserveAspect = true;
            item.icon.rectTransform.sizeDelta = new Vector2(size, size);
            item.icon.color = new Color(1f, 1f, 1f, 0f);
            item.sparkles = NewImages(fxLayer, "Faísca do voo " + index, flightSparkles, j => Hash(index * 97 + j, 22) < 0.65f ? SparkleSprite : DotSprite);
            item.ring = NewImage(fxLayer, "Anel de chegada " + index, RingSprite, true);

            if (!string.IsNullOrEmpty(amountFormat) && (reward.amount > 1 || showAmountOfOne))
            {
                item.amount = NewText(labelLayer, "Quantidade " + index, amountSize * Mathf.Clamp(size / Mathf.Max(1f, iconSize), 0.6f, 1.3f),
                    amountColor, new Vector2(size * 1.4f, amountSize * 1.6f));
                item.amount.text = string.Format(amountFormat, Mathf.Max(1, reward.amount));
                item.amount.rectTransform.anchoredPosition = slot + amountOffset * size;
                item.amount.rectTransform.localScale = Vector3.zero;
            }
            return item;
        }

        ChestTrailGraphic NewTrail(Transform parent, string label)
        {
            var rect = NewRect(parent, label);
            var trail = rect.gameObject.AddComponent<ChestTrailGraphic>();
            trail.raycastTarget = false;
            var material = Additive;
            if (material != null) trail.material = material;
            trail.SetProfile(ChestOpenArt.TrailProfile);
            trail.enabled = false;
            return trail;
        }

        // ------------------------------------------------------------------------------------------ animação

        static float Hash(int a, int b)
        {
            unchecked
            {
                var x = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ 0x9E3779B9u;
                x ^= x >> 13; x *= 0x5bd1e995u; x ^= x >> 15;
                return (x & 0xFFFFFF) / 16777216f;
            }
        }

        static float EaseOutCubic(float x)
        {
            x = 1f - Mathf.Clamp01(x);
            return 1f - x * x * x;
        }

        static float EaseOutBack(float x)
        {
            x = Mathf.Clamp01(x);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        /// Posiciona e pinta uma imagem; com opacidade zero ela é desligada (não gasta desenho).
        static void Paint(Image image, Vector2 position, Vector2 size, Color color, float alpha)
        {
            if (image == null) return;
            var visible = alpha > 0.003f && size.x > 0.01f && size.y > 0.01f;
            if (image.enabled != visible) image.enabled = visible;
            if (!visible) return;
            image.rectTransform.anchoredPosition = position;
            image.rectTransform.sizeDelta = size;
            color.a = Mathf.Clamp01(alpha);
            image.color = color;
        }

        static void Hide(Image image)
        {
            if (image != null && image.enabled) image.enabled = false;
        }

        static void SetAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null) return;
            var color = graphic.color;
            color.a = Mathf.Clamp01(alpha);
            graphic.color = color;
        }

        void Play(AudioClip clip)
        {
            if (clip != null && !skipped && SoundBase.Instance != null) SoundBase.Instance.PlayOneShot(clip);
        }

        /// Caminho do power-up, de dentro do baú até o lugar dele (f de 0 a 1): uma curva em S com uma ondulação lateral.
        Vector2 Path(Item item, float f)
        {
            var a = 1f - f;
            var point = a * a * a * mouth + 3f * a * a * f * item.control1 + 3f * a * f * f * item.control2 + f * f * f * item.slot;
            if (swirlAmplitude == 0f) return point;
            var tangent = 3f * a * a * (item.control1 - mouth) + 6f * a * f * (item.control2 - item.control1) + 3f * f * f * (item.slot - item.control2);
            if (tangent.sqrMagnitude < 0.0001f) return point;
            var normal = new Vector2(-tangent.y, tangent.x).normalized;
            return point + normal * (Mathf.Sin(f * swirlTurns * TwoPi) * Mathf.Sin(f * Mathf.PI) * swirlAmplitude * item.side);
        }

        void Evaluate(float t)
        {
            // O palco (1080 x 1920) encolhe ou cresce para caber inteiro em qualquer proporção de tela.
            var area = root.rect;
            var fit = Mathf.Min(area.width / Mathf.Max(1f, designSize.x), area.height / Mathf.Max(1f, designSize.y));
            stage.localScale = new Vector3(fit, fit, 1f);

            EvaluateChest(t);
            for (var i = 0; i < items.Count; i++) EvaluateItem(items[i], i, t);

            if (title != null)
            {
                var k = Mathf.Clamp01((t - tOpen - 0.15f) / 0.35f);
                title.rectTransform.localScale = Vector3.one * Mathf.Max(0.001f, EaseOutBack(k));
                SetAlpha(title, titleColor.a * Mathf.Clamp01(k * 3f));
            }
            if (continueLabel != null)
                SetAlpha(continueLabel, continueColor.a * Mathf.Clamp01((t - tContinue) / 0.3f) * (0.65f + 0.35f * Mathf.Sin(t * 4f)));
        }

        void EvaluateChest(float t)
        {
            var enter = Mathf.Clamp01((t - tEnter) / Mathf.Max(0.01f, chestEnter));
            var antic = t >= tOpen || anticipation <= 0f ? 0f : Mathf.Clamp01((t - tAntic) / anticipation);
            var since = t - tOpen;                                         // segundos desde a abertura (negativo antes)
            var open = Mathf.Clamp01(since / Mathf.Max(0.01f, openDuration));
            var power = antic * Mathf.Sqrt(antic);                          // a antecipação cresce até o estouro

            // ---- baú inteiro: entrada, tremor, pulinhos e o tranco da abertura
            var angle = Mathf.Sin((t - tAntic) * shakeFrequency * TwoPi) * shakeAngle * power;
            var hopPhase = antic * bounceCount;
            var hop = Mathf.Abs(Mathf.Sin(hopPhase * Mathf.PI)) * bounceHeight * antic;
            var squashWave = Mathf.Cos(hopPhase * TwoPi) * squash * antic;
            var recoil = since >= 0f ? Mathf.Sin(Mathf.Clamp01(since / 0.28f) * Mathf.PI) * openPunch : 0f;
            var scale = Mathf.Max(0.001f, EaseOutBack(enter));
            var chestScale = new Vector3(scale * (1f + squashWave * 0.6f + recoil), scale * (1f - squashWave - recoil * 0.6f), 1f);
            var position = ground + new Vector2(0f, hop + (1f - EaseOutCubic(enter)) * 160f);
            var rotation = Quaternion.Euler(0f, 0f, angle);
            chestBack.anchoredPosition = chestFront.anchoredPosition = position;
            chestBack.localScale = chestFront.localScale = chestScale;
            chestBack.localRotation = chestFront.localRotation = rotation;
            var chestAlpha = Mathf.Clamp01(enter * 4f);
            SetAlpha(bodyImage, chestAlpha);
            SetAlpha(lidImage, chestAlpha);

            // ---- baú aberto numa imagem só (opcional)
            var opened = chestOpened != null && since >= openDuration * 0.35f;
            if (opened != showingOpened)
            {
                showingOpened = opened;
                var sprite = opened ? chestOpened : (chestBody != null ? chestBody : ChestOpenArt.ChestBody);
                bodyImage.sprite = sprite;
                bodyRect.sizeDelta = new Vector2(chestWidth, chestWidth * Aspect(sprite));
                if (lidImage != null) lidImage.gameObject.SetActive(!opened);
                if (interiorImage != null) interiorImage.gameObject.SetActive(!opened);
            }

            // ---- tampa
            if (lidRect != null)
            {
                var k = EaseOutBack(open);
                var rattle = Mathf.Abs(Mathf.Sin((t - tAntic) * shakeFrequency * 1.7f * Mathf.PI)) * lidRattle * power;
                lidRect.anchoredPosition = lidClosed + new Vector2(0f, lidOpenLift * chestWidth * k + rattle);
                lidRect.localRotation = Quaternion.Euler(0f, 0f, lidOpenAngle * k);
                lidRect.localScale = new Vector3(Mathf.LerpUnclamped(1f, lidOpenScale.x, k), Mathf.LerpUnclamped(1f, lidOpenScale.y, k), 1f);
            }
            SetAlpha(interiorImage, since < 0f ? 0f : Mathf.Clamp01(since / 0.08f));

            // ---- luzes do baú
            var flicker = 0.78f + 0.22f * Mathf.Sin(t * 31f);
            var leakNow = since < 0f ? power * flicker : 1f - Mathf.Clamp01(since / 0.3f);
            Paint(leak, lidClosed, new Vector2(chestWidth * 1.15f, chestWidth * (0.18f + 0.2f * power + 0.25f * open)), leakColor, leakAlpha * leakNow);
            Paint(innerGlow, bodyTop + mouthOffset * chestWidth, new Vector2(chestWidth * 1.25f, chestWidth * 0.8f), leakColor,
                since < 0f ? 0f : innerGlowAlpha * Mathf.Clamp01(since / 0.1f) * (0.86f + 0.14f * Mathf.Sin(t * 3f)));
            Paint(ambient, chestPosition, new Vector2(ambientSize, ambientSize), ambientColor,
                ambientAlpha * (0.3f * enter + 0.2f * power + 0.5f * Mathf.Clamp01(since / 0.4f)));

            for (var i = 0; i < leakSparkles.Length; i++)
            {
                if (antic <= 0f) { Hide(leakSparkles[i]); continue; }
                var phase = Mathf.Repeat((t - tAntic) / 0.55f + Hash(i, 1), 1f);
                var offset = new Vector2((Hash(i, 2) - 0.5f) * chestWidth * 0.85f, phase * chestWidth * 0.4f);
                var dot = sparkleSize.x * (0.5f + Hash(i, 3));
                Paint(leakSparkles[i], position + lidClosed + offset, new Vector2(dot, dot), leakColor, leakAlpha * power * Mathf.Sin(Mathf.PI * phase));
            }

            // ---- estouro da abertura
            if (since < 0f)
            {
                Hide(flash); Hide(ring); Hide(beam);
                foreach (var ray in burstRays) Hide(ray);
                foreach (var sparkle in burstSparkles) Hide(sparkle);
                return;
            }
            if (!openPlayed) { openPlayed = true; Play(openSound); }

            var flashK = Mathf.Clamp01(since / flashDuration);
            var flashNow = flashSize * (0.25f + 0.75f * EaseOutCubic(flashK));
            Paint(flash, mouth, new Vector2(flashNow, flashNow), flashColor, flashAlpha * (1f - flashK) * (1f - flashK));
            var ringK = Mathf.Clamp01(since / ringDuration);
            var ringNow = ringSize * EaseOutCubic(ringK);
            Paint(ring, mouth, new Vector2(ringNow, ringNow), ringColor, ringAlpha * (1f - ringK));

            var settle = Mathf.Clamp01((since - 0.25f) / 0.9f);              // do estouro para o estado de repouso
            var beamNow = beamAlpha * Mathf.Lerp(1f, beamIdle, settle) * (0.9f + 0.1f * Mathf.Sin(t * 5f));
            Paint(beam, mouth, new Vector2(chestWidth * beamWidth, beamHeight * EaseOutCubic(since / 0.3f)), beamColor, beamNow);

            raysRoot.localRotation = Quaternion.Euler(0f, 0f, since * burstRaySpin);
            var grow = EaseOutBack(since / 0.4f);
            for (var i = 0; i < burstRays.Length; i++)
            {
                var ray = burstRays[i];
                var step = 360f / burstRays.Length;
                ray.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i * step + Hash(i, 4) * step * 0.6f);
                var length = burstRayLength * (0.55f + 0.45f * Hash(i, 5)) * grow;
                var wave = 0.7f + 0.3f * Mathf.Sin(t * (2f + 3f * Hash(i, 6)) + Hash(i, 7) * TwoPi);
                Paint(ray, Vector2.zero, new Vector2(length, burstRayWidth), burstRayColor, burstRayAlpha * Mathf.Lerp(1f, burstRayIdle, settle) * wave);
            }

            for (var i = 0; i < burstSparkles.Length; i++)
            {
                var life = Mathf.Lerp(0.7f, 1.4f, Hash(i, 8));
                var k = since / life;
                if (k >= 1f) { Hide(burstSparkles[i]); continue; }
                var direction = Mathf.Lerp(burstFan.x, burstFan.y, Hash(i, 9)) * Mathf.Deg2Rad;
                var travel = Mathf.Lerp(burstSparkleSpeed.x, burstSparkleSpeed.y, Hash(i, 10)) * (1f - Mathf.Exp(-2.6f * since)) / 2.6f;
                var at = mouth + new Vector2(Mathf.Cos(direction), Mathf.Sin(direction)) * travel + new Vector2(0f, -0.5f * burstGravity * since * since);
                var dot = Mathf.Lerp(sparkleSize.x, sparkleSize.y, Hash(i, 11)) * (0.45f + 0.55f * Mathf.Sin(Mathf.PI * k));
                var tint = Color.Lerp(PaletteColor(i), Color.white, 0.35f);
                burstSparkles[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, since * 140f * (Hash(i, 12) - 0.5f));
                Paint(burstSparkles[i], at, new Vector2(dot, dot), tint, (1f - k) * (0.6f + 0.4f * Mathf.Sin(t * 18f + Hash(i, 13) * 40f)));
            }
        }

        void EvaluateItem(Item item, int index, float t)
        {
            var local = t - item.launch;
            var u = local / flightDuration;
            if (u < 0f)
            {
                SetAlpha(item.icon, 0f);
                Hide(item.headGlow); Hide(item.ring);
                foreach (var sparkle in item.sparkles) Hide(sparkle);
                if (item.trail.enabled) item.trail.enabled = false;
                if (item.core.enabled) item.core.enabled = false;
                if (item.glow != null) item.glow.intensity = 0f;
                if (item.amount != null) item.amount.rectTransform.localScale = Vector3.zero;
                return;
            }
            if (!item.launched) { item.launched = true; Play(launchSound); }

            // ---- figura: voando ou pousada
            var flying = u < 1f;
            var after = t - item.arrive;
            Vector2 position;
            float scale, spin;
            if (flying)
            {
                var f = EaseOutCubic(u);
                position = Path(item, f);
                scale = Mathf.LerpUnclamped(startScale, 1f, EaseOutBack(u));
                spin = (1f - f) * spinDegrees * item.side;
            }
            else
            {
                if (!item.arrived) { item.arrived = true; Play(arriveSound); }
                var pop = after < popDuration ? Mathf.Sin(after / popDuration * Mathf.PI) * arrivalPop : 0f;
                position = item.slot + new Vector2(0f, Mathf.Sin((after * bobSpeed + item.phase) * TwoPi) * idleBob * Mathf.Clamp01(after));
                scale = 1f + pop;
                spin = 0f;
            }
            var iconRect = item.icon.rectTransform;
            iconRect.anchoredPosition = position;
            iconRect.localScale = new Vector3(scale, scale, 1f);
            iconRect.localRotation = Quaternion.Euler(0f, 0f, spin);
            SetAlpha(item.icon, Mathf.Clamp01(u * 10f));

            // ---- rastro: a fita passa pelos lugares onde a figura esteve nos últimos "Trail Seconds"
            var trailNow = flying ? Mathf.Clamp01(u * 6f) : 1f - Mathf.Clamp01(after / trailFade);
            var showTrail = trailNow > 0.003f && trailWidth > 0f;
            if (item.trail.enabled != showTrail) item.trail.enabled = showTrail;
            var showCore = trailNow > 0.003f && trailCoreWidth > 0f;
            if (item.core.enabled != showCore) item.core.enabled = showCore;
            if (showTrail || showCore)
            {
                trailPoints.Clear();
                var segments = Mathf.Max(2, trailSegments);
                for (var k = 0; k <= segments; k++)
                {
                    var past = Mathf.Clamp01((local - k * trailSeconds / segments) / flightDuration);
                    trailPoints.Add(past >= 1f ? item.slot : Path(item, EaseOutCubic(past)));
                }
                var headColor = item.color; headColor.a = trailAlpha * trailNow;
                var tailColor = PaletteColor(index + 1); tailColor.a = trailAlpha * trailNow * 0.7f;
                if (showTrail) item.trail.SetTrail(trailPoints, item.size * trailWidth, headColor, tailColor);
                var coreColor = Color.Lerp(item.color, Color.white, 0.7f); coreColor.a = trailCoreAlpha * trailNow;
                if (showCore) item.core.SetTrail(trailPoints, item.size * trailCoreWidth, coreColor, coreColor);
            }
            var head = item.size * headGlowSize * scale;
            Paint(item.headGlow, position, new Vector2(head, head), item.color, headGlowAlpha * trailNow);

            // ---- faíscas soltas pelo caminho
            for (var j = 0; j < item.sparkles.Length; j++)
            {
                var seed = index * 97 + j;
                var born = (j + Hash(seed, 14)) / item.sparkles.Length * flightDuration;
                var age = local - born;
                var life = sparkleLife * (0.6f + 0.8f * Hash(seed, 15));
                if (age < 0f || age >= life) { Hide(item.sparkles[j]); continue; }
                var k = age / life;
                var origin = Path(item, EaseOutCubic(born / flightDuration)) +
                             new Vector2(Hash(seed, 16) - 0.5f, Hash(seed, 17) - 0.5f) * (item.size * 0.45f);
                var direction = Hash(seed, 18) * TwoPi;
                var at = origin + new Vector2(Mathf.Cos(direction), Mathf.Sin(direction)) * (sparkleDrift * age) + new Vector2(0f, -sparkleGravity * age * age);
                var envelope = Mathf.Sin(Mathf.PI * k);
                var dot = Mathf.Lerp(sparkleSize.x, sparkleSize.y, Hash(seed, 19)) * (0.4f + 0.6f * envelope);
                item.sparkles[j].rectTransform.localRotation = Quaternion.Euler(0f, 0f, age * 90f + Hash(seed, 20) * 90f);
                Paint(item.sparkles[j], at, new Vector2(dot, dot), Color.Lerp(item.color, Color.white, 0.45f), envelope);
            }

            // ---- chegada: anel, brilho atrás e quantidade
            if (flying) Hide(item.ring);
            else
            {
                var k = Mathf.Clamp01(after / 0.45f);
                var ringNow = item.size * Mathf.Lerp(0.7f, 2.4f, EaseOutCubic(k));
                Paint(item.ring, item.slot, new Vector2(ringNow, ringNow), Color.Lerp(item.color, Color.white, 0.3f), 0.85f * (1f - k));
            }
            if (item.glow != null)
            {
                var k = Mathf.Clamp01((after + 0.08f) / glowFade);
                item.glow.intensity = k;
                item.glowRect.localScale = Vector3.one * (0.4f + 0.6f * EaseOutBack(k));
            }
            if (item.amount != null)
            {
                var k = Mathf.Clamp01((after - 0.08f) / 0.3f);
                item.amount.rectTransform.localScale = Vector3.one * (k <= 0f ? 0f : EaseOutBack(k));
                SetAlpha(item.amount, amountColor.a * Mathf.Clamp01(k * 4f));
            }
        }
    }
}
