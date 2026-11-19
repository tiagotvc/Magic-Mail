// Moedas mágicas do fim da fase. Quando os objetivos são concluídos e o bônus final roda (as jogadas que sobraram viram
// especiais e explodem), os objetivos do HUD somem e, no lugar deles, aparece um contador com as moedas que o bônus rendeu.
// Enquanto o bônus pontua, moedas saem do meio do tabuleiro e sobem até o contador com halo, cometa, faíscas e um pequeno
// estouro em cada chegada.
//
// De onde vem o número: as moedas de uma vitória já são "pontos da fase x moedas por ponto" (RoyalAvesProgression,
// creditadas em LobbyController.ShowVictory). O contador mostra só a parte que o bônus acrescentou a essa conta:
//     moedas(pontos agora) - moedas(pontos quando o bônus começou)
// Este efeito NÃO credita nada e não mexe em pontos, estrelas ou no momento em que a fase termina: ele só mostra a parte
// do bônus dentro do valor que a tela de vitória credita em seguida.
//
// É o componente do prefab Resources/Effects/LevelEnd/LevelEndCoinEffect (criado por LevelEndCoinEffectBuilder): arte, cores,
// tamanhos, contagens e tempos ficam no Inspector. O jogo usa a fachada LevelEndCoins; sem o prefab, nada acontece.
//
// Tudo é desenhado em UI, numa camada acima do HUD (como MailDeliveryOverlay), então tabuleiro e HUD usam as mesmas
// coordenadas. O HUD volta ao normal quando outra fase carrega, quando a cena é trocada ou quando o efeito é cancelado.
using System;
using System.Collections.Generic;
using RoyalAves.Meta;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.GUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Effects
{
    /// Fachada usada pelo jogo. Sem o prefab (ou sem o HUD dos objetivos), as chamadas não fazem nada.
    public static class LevelEndCoins
    {
        static LevelEndCoinEffect current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { current = null; }

        /// Começa a acompanhar o bônus final desta fase. Chame antes do bônus pontuar.
        public static LevelEndCoinEffect Begin(LevelManager manager)
        {
            if (manager == null) return null;
            return Start(effect => effect.Run(manager));
        }

        /// Teste visual (em Play, com uma fase aberta): mostra o efeito com um valor inventado e restaura o HUD no fim.
        public static LevelEndCoinEffect Simulate(int coins)
        {
            return Start(effect => effect.RunDemo(Mathf.Max(1, coins)));
        }

        /// Interrompe o efeito e devolve os objetivos ao HUD.
        public static void Cancel()
        {
            if (current != null) current.Close();
            current = null;
        }

        static LevelEndCoinEffect Start(Func<LevelEndCoinEffect, bool> run)
        {
            Cancel();
            var prefab = Resources.Load<LevelEndCoinEffect>(LevelEndCoinEffect.ResourcePath);
            if (prefab == null) return null;
            var effect = UnityEngine.Object.Instantiate(prefab);
            effect.name = "LevelEndCoinEffect (runtime)";
            if (!run(effect)) { effect.Close(); return null; }
            current = effect;
            return effect;
        }
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("Royal Aves/Level End Coin Effect")]
    public sealed class LevelEndCoinEffect : MonoBehaviour
    {
        public const string ResourcePath = "Effects/LevelEnd/LevelEndCoinEffect";

        // ------------------------------------------------------------------------------------------ arte

        [Header("Arte")]
        [Tooltip("Figura da moeda (vazio = a moeda do lobby, LobbyFeaturesConfig.coin).")]
        public Sprite coinSprite;
        [Tooltip("Brilho radial macio (00_Comuns/01_radial_glow).")]
        public Sprite glow;
        public Sprite sparkle4;
        public Sprite sparkle6;
        public Sprite dot;
        [Tooltip("Anel do estouro de chegada (01_Match_Normal/01_anel_unico_suave).")]
        public Sprite ring;
        [Tooltip("Material aditivo (luz): ImpactAdditive. O efeito cria uma cópia por textura.")]
        public Material additive;

        [Header("Cores do Royal Postman")]
        public Color gold = new Color(1f, 0.761f, 0.278f, 1f);       // #FFC247
        public Color violet = new Color(0.506f, 0.169f, 1f, 1f);     // #812BFF
        public Color magenta = new Color(0.820f, 0.169f, 1f, 1f);    // #D12BFF
        public Color pink = new Color(1f, 0.341f, 0.776f, 1f);       // #FF57C6
        public Color cyan = new Color(0.286f, 0.867f, 0.961f, 1f);   // #49DDF5
        [Tooltip("Branco quente, usado só nos núcleos pequenos.")]
        public Color core = new Color(1f, 0.97f, 0.88f, 1f);

        // ------------------------------------------------------------------------------------------ parâmetros

        [Serializable]
        public sealed class EmissionSettings
        {
            [Tooltip("Segundos entre os objetivos sumirem e a primeira moeda sair.")]
            [Min(0)] public float startDelay = 0.18f;
            [Tooltip("Segundos entre uma moeda e a próxima.")]
            [Min(0.01f)] public float interval = 0.075f;
            [Tooltip("Máximo de moedas voando ao mesmo tempo.")]
            [Min(1)] public int maxFlying = 14;
            [Tooltip("Parte do valor ainda não lançado que cada moeda carrega (0,18 = 18%). Maior = menos moedas, contador sobe em saltos maiores.")]
            [Range(0.02f, 1f)] public float fractionPerCoin = 0.18f;
            [Tooltip("Valor mínimo que uma moeda carrega (evita uma fila longa de moedas valendo 1).")]
            [Min(1)] public int minValuePerCoin = 3;
            [Tooltip("Raio, em casas, da área do meio do tabuleiro de onde as moedas nascem.")]
            [Min(0)] public float spawnRadius = 0.45f;
        }

        [Serializable]
        public sealed class FlightSettings
        {
            [Tooltip("Tempo do voo até o contador, em segundos.")]
            [Min(0.05f)] public float seconds = 0.78f;
            [Tooltip("Variação do tempo de voo entre moedas (0,15 = ±15%).")]
            [Range(0f, 0.6f)] public float secondsVariation = 0.15f;
            [Tooltip("Tempo do salto inicial, quando a moeda brota do tabuleiro.")]
            [Min(0.01f)] public float popSeconds = 0.17f;
            [Tooltip("Distância do salto inicial, em casas.")]
            [Min(0)] public float popDistance = 0.75f;
            [Tooltip("Altura do arco, em casas: quanto a moeda chega por baixo do contador.")]
            [Min(0)] public float arcHeight = 1.5f;
            [Tooltip("Abertura lateral da curva, em casas.")]
            [Min(0)] public float sideSwing = 1.5f;
            [Tooltip("Aceleração: 1 = velocidade constante, 2 = sai devagar e chega rápido.")]
            [Range(1f, 4f)] public float acceleration = 2f;
            [Tooltip("Altura da moeda, em casas.")]
            [Min(0.05f)] public float coinSize = 0.62f;
            [Tooltip("Tamanho da moeda ao chegar (multiplica o tamanho normal).")]
            [Range(0.1f, 1.5f)] public float arrivalScale = 0.6f;
            [Tooltip("Voltas que a moeda dá girando no próprio eixo durante o voo.")]
            [Min(0)] public float spinTurns = 2.5f;
        }

        [Serializable]
        public sealed class TrailSettings
        {
            [Tooltip("Halo atrás da moeda: tamanho, em múltiplos da moeda.")]
            [Min(0)] public float haloSize = 2.3f;
            [Range(0f, 1f)] public float haloAlpha = 0.6f;
            [Tooltip("Cometa (rastro de luz): comprimento máximo, em casas.")]
            [Min(0)] public float cometLength = 1.5f;
            [Tooltip("Cometa: largura, em casas.")]
            [Min(0)] public float cometWidth = 0.5f;
            [Tooltip("Cometa: quantos segundos de percurso ele cobre (o comprimento acompanha a velocidade).")]
            [Min(0)] public float cometSeconds = 0.09f;
            [Range(0f, 1f)] public float cometAlpha = 0.7f;
            [Tooltip("Faíscas deixadas por segundo, por moeda.")]
            [Min(0)] public float sparksPerSecond = 30f;
            [Tooltip("Tamanho das faíscas, em casas.")]
            [Min(0)] public float sparkSize = 0.24f;
            [Tooltip("Vida das faíscas, em segundos.")]
            [Min(0.05f)] public float sparkLife = 0.5f;
            [Tooltip("Quanto as faíscas se espalham para os lados do percurso, em casas.")]
            [Min(0)] public float sparkSpread = 0.16f;
            [Tooltip("Limite total de faíscas na tela.")]
            [Min(0)] public int maxSparks = 180;
        }

        [Serializable]
        public sealed class ArrivalSettings
        {
            [Tooltip("Faíscas no estouro de cada chegada.")]
            [Min(0)] public int sparks = 7;
            [Tooltip("Distância que as faíscas do estouro percorrem, em casas.")]
            [Min(0)] public float sparkDistance = 0.9f;
            [Tooltip("Duração do estouro, em segundos.")]
            [Min(0.05f)] public float seconds = 0.42f;
            [Tooltip("Tamanho final do anel, em casas.")]
            [Min(0)] public float ringSize = 1.5f;
            [Tooltip("Tamanho do clarão, em casas.")]
            [Min(0)] public float flashSize = 1.3f;
            [Tooltip("Quanto o contador cresce a cada moeda (0,2 = 20%).")]
            [Range(0f, 1f)] public float counterPop = 0.2f;
            [Tooltip("Duração do pulo do contador, em segundos.")]
            [Min(0.02f)] public float counterPopSeconds = 0.2f;
            [Tooltip("Toca um som nas chegadas.")]
            public bool playSound = true;
            [Tooltip("Som da chegada (vazio = o som de moedas do jogo, SoundBase.cash).")]
            public AudioClip sound;
            [Tooltip("Intervalo mínimo entre dois sons, em segundos.")]
            [Min(0)] public float soundInterval = 0.14f;
        }

        [Serializable]
        public sealed class SourceSettings
        {
            [Tooltip("Brilho no meio do tabuleiro, de onde as moedas saem: tamanho em casas.")]
            [Min(0)] public float size = 3.4f;
            [Range(0f, 1f)] public float alpha = 0.55f;
            [Tooltip("Estrela de raios girando no centro: tamanho em casas (0 = desligada).")]
            [Min(0)] public float raysSize = 2.4f;
            [Range(0f, 1f)] public float raysAlpha = 0.5f;
            [Tooltip("Graus por segundo.")]
            public float raysSpin = 40f;
            [Tooltip("Pulsos por segundo.")]
            [Min(0)] public float pulseSpeed = 1.6f;
            [Tooltip("Faíscas que brotam junto com cada moeda.")]
            [Min(0)] public int birthSparks = 3;
        }

        [Serializable]
        public sealed class CounterSettings
        {
            [Tooltip("Segundos para os objetivos sumirem.")]
            [Min(0.01f)] public float goalsFadeSeconds = 0.25f;
            [Tooltip("Segundos para o contador aparecer.")]
            [Min(0.01f)] public float appearSeconds = 0.38f;
            [Tooltip("Altura da moeda do contador, em relação à altura da área dos objetivos.")]
            [Range(0.1f, 1.5f)] public float iconHeight = 0.74f;
            [Tooltip("Altura do número, em relação à altura da área dos objetivos.")]
            [Range(0.1f, 1.5f)] public float textHeight = 0.56f;
            [Tooltip("Espaço entre a moeda e o número, em relação à altura da área.")]
            [Range(0f, 1f)] public float gap = 0.1f;
            [Tooltip("Largura máxima do contador, em relação à largura da área dos objetivos.")]
            [Range(0.2f, 1.2f)] public float maxWidth = 0.86f;
            [Tooltip("Texto antes do número.")]
            public string prefix = "+";
            [Tooltip("Usa a fonte e a cor dos números dos objetivos.")]
            public bool useGoalTextStyle = true;
            [Tooltip("Cor do número quando \"Use Goal Text Style\" está desligado.")]
            public Color textColor = new Color(0.45f, 0.16f, 0.06f, 1f);
            [Tooltip("Segundos que o número leva para alcançar o valor das moedas que já chegaram.")]
            [Min(0.01f)] public float countSeconds = 0.22f;
            [Tooltip("Brilho atrás da moeda do contador: tamanho em múltiplos da moeda.")]
            [Min(0)] public float glowSize = 2.4f;
            [Range(0f, 1f)] public float glowAlpha = 0.45f;
            [Tooltip("Texto da placa \"Objetivos\" enquanto o contador está no lugar (vazio = não muda a placa).")]
            public string plaqueTitle = "Bônus";
            [Tooltip("Nome do objeto da placa dentro do TopPanel.")]
            public string plaqueObjectName = "PlacaObjetivos";
        }

        public EmissionSettings emission = new EmissionSettings();
        public FlightSettings flight = new FlightSettings();
        public TrailSettings trail = new TrailSettings();
        public ArrivalSettings arrival = new ArrivalSettings();
        public SourceSettings source = new SourceSettings();
        public CounterSettings counter = new CounterSettings();

        [Header("Desenho")]
        [Tooltip("Ordem da camada das moedas acima do HUD (fica abaixo da cortina de \"Pular bônus\", 32020).")]
        public int overlaySortingOrder = 32000;
        [Tooltip("Segundos para as moedas ainda no ar sumirem quando a tela de vitória abre.")]
        [Min(0.01f)] public float finishFadeSeconds = 0.18f;

        [Header("Teste (menu Royal Aves/VFX/Moedas do fim de fase)")]
        [Tooltip("No teste, o valor cresce ao longo destes segundos, como o bônus de verdade.")]
        [Min(0.1f)] public float demoSeconds = 2.5f;

        // ------------------------------------------------------------------------------------------ estado

        sealed class Coin
        {
            public Image image, halo, comet;
            public Vector2 from, kick, previous;
            public float age, duration, side, seed, sparkDebt, cell, fade = 1f;
            public int value;
            public bool hasPrevious;
        }

        sealed class Spark
        {
            public Image image;
            public Vector2 position, velocity;
            public float age, life, sizeFrom, sizeTo, angle, spin, drag, twinkle, seed;
            public Color color;
        }

        LevelManager owner;
        SweetSugar.Scripts.Level.LevelData data;
        float coinsPerPoint, share = 1f;
        int coinsBefore;
        bool demo;
        int demoTarget;
        float demoDoneAt = -1f;

        Canvas hudCanvas;
        RectTransform goalsArea, topPanel;
        TargetGUIGroup goals;
        CanvasGroup goalsFade;
        bool addedGoalsFade;
        float goalsAlphaBefore = 1f;
        TextMeshProUGUI plaque;
        string plaqueBefore;

        RectTransform overlay, backLayer, coinLayer, frontLayer;
        RectTransform counterRoot, counterContent;
        CanvasGroup counterFade;
        Image counterIcon, counterGlow;
        TextMeshProUGUI counterText;
        Image sourceGlow, sourceRays;

        readonly List<Coin> coins = new List<Coin>();
        readonly List<Spark> sparks = new List<Spark>();
        readonly Stack<Image> pool = new Stack<Image>();
        readonly Dictionary<Texture, Material> materialByTexture = new Dictionary<Texture, Material>();
        Material additiveBase;
        bool additiveSearched, ownsAdditiveBase;

        bool started, swapped, finished, restored;
        float clock, nextLaunch, lastLaunch = -10f, swapAge, popAge = 10f, sourceLevel, glowKick, lastSound = -10f;
        int launchedValue, landedValue, shownValue = -1;
        float shown;
        Vector3 boardWorld;
        float cellWorld = 1.2f;

        // ------------------------------------------------------------------------------------------ entrada

        /// Acompanha o bônus desta fase. Devolve false (sem tocar em nada) se não há HUD de objetivos ou taxa de moedas.
        public bool Run(LevelManager manager)
        {
            var config = AreaProgressionConfig.Instance;
            if (manager == null || config == null || config.coinsPerPoint <= 0f) return false;
            // A mesma conta de LobbyController.ShowVictory: repetir uma fase já vencida pode render uma parte menor.
            var firstWin = manager.currentLevel > MetaProgress.LevelsWon;
            coinsPerPoint = config.coinsPerPoint;
            share = firstWin ? 1f : config.replayCoinShare;
            if (share <= 0f) return false;
            owner = manager;
            data = manager.levelData;
            coinsBefore = CoinsFor(LevelManager.Score);
            if (!FindHud()) return false;
            LevelManager.OnLevelLoaded += Close;
            started = true;
            return true;
        }

        /// Teste visual: não depende do estado da fase e restaura o HUD sozinho.
        public bool RunDemo(int amount)
        {
            owner = LevelManager.THIS;
            demo = true;
            demoTarget = amount;
            if (!FindHud()) return false;
            LevelManager.OnLevelLoaded += Close;
            started = true;
            return true;
        }

        // Exatamente a conta (e a ordem das multiplicações) de LobbyController.ShowVictory.
        int CoinsFor(int score) => Mathf.RoundToInt(score * coinsPerPoint * share);

        /// Moedas que o bônus rendeu até agora.
        int Target()
        {
            if (demo) return Mathf.RoundToInt(demoTarget * Mathf.Clamp01(clock / Mathf.Max(0.1f, demoSeconds)));
            return Mathf.Max(0, CoinsFor(LevelManager.Score) - coinsBefore);
        }

        bool FindHud()
        {
            foreach (var candidate in FindObjectsByType<TargetGUIGroup>(FindObjectsSortMode.None))
            {
                if (candidate == null || !candidate.isActiveAndEnabled) continue;
                for (var parent = candidate.transform.parent; parent != null; parent = parent.parent)
                {
                    if (parent.name != "TopPanel") continue;
                    goals = candidate;
                    topPanel = parent as RectTransform;
                    break;
                }
                if (goals != null) break;
            }
            if (goals == null) return false;
            goalsArea = (goals.hg != null ? goals.hg.transform : goals.transform) as RectTransform;
            var canvas = goals.GetComponentInParent<Canvas>();
            hudCanvas = canvas != null ? canvas.rootCanvas : null;
            if (goalsArea == null || goalsArea.parent == null || hudCanvas == null) return false;
            MeasureBoard();
            return true;
        }

        void MeasureBoard()
        {
            cellWorld = owner != null ? owner.squareWidth : 1.2f;
            var field = owner != null ? owner.field : null;
            var camera = ViewCamera;
            boardWorld = camera != null ? camera.transform.position : Vector3.zero;
            if (field == null) return;
            boardWorld = field.GetPosition();
            if (field.squaresArray == null) return;
            var any = false;
            Vector2 min = Vector2.zero, max = Vector2.zero;
            foreach (var square in field.squaresArray)
            {
                if (square == null || square.type == SquareTypes.NONE) continue;
                Vector2 position = square.transform.position;
                if (!any) { min = max = position; any = true; }
                else { min = Vector2.Min(min, position); max = Vector2.Max(max, position); }
            }
            if (any) boardWorld = (min + max) * 0.5f;
        }

        Camera ViewCamera
        {
            get
            {
                var camera = owner != null ? owner.GetComponent<Camera>() : null;
                return camera != null ? camera : Camera.main;
            }
        }

        // ------------------------------------------------------------------------------------------ HUD

        /// Esconde os objetivos (só a opacidade: os objetos continuam ligados) e cria o contador no lugar deles.
        bool SwapHud()
        {
            if (goalsArea == null || hudCanvas == null) return false;

            var layer = goalsArea.gameObject.layer;
            var root = new GameObject("MoedasFimDeFase", typeof(RectTransform), typeof(Canvas));
            root.layer = hudCanvas.gameObject.layer;
            overlay = (RectTransform)root.transform;
            overlay.SetParent(hudCanvas.transform, false);
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            var canvas = root.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingLayerID = hudCanvas.sortingLayerID;
            canvas.sortingOrder = overlaySortingOrder;
            backLayer = Layer("Brilhos");
            coinLayer = Layer("Moedas");
            frontLayer = Layer("Faíscas");

            goalsFade = goalsArea.GetComponent<CanvasGroup>();
            addedGoalsFade = goalsFade == null;
            if (addedGoalsFade) goalsFade = goalsArea.gameObject.AddComponent<CanvasGroup>();
            goalsAlphaBefore = goalsFade.alpha;

            // Contador: irmão da área dos objetivos, com o mesmo retângulo.
            var node = new GameObject("ContadorMoedasBonus", typeof(RectTransform), typeof(LayoutElement));
            node.layer = layer;
            node.GetComponent<LayoutElement>().ignoreLayout = true;
            counterRoot = (RectTransform)node.transform;
            counterRoot.SetParent(goalsArea.parent, false);
            counterRoot.SetSiblingIndex(goalsArea.GetSiblingIndex() + 1);
            counterRoot.anchorMin = goalsArea.anchorMin; counterRoot.anchorMax = goalsArea.anchorMax;
            counterRoot.pivot = goalsArea.pivot;
            counterRoot.anchoredPosition3D = goalsArea.anchoredPosition3D;
            counterRoot.sizeDelta = goalsArea.sizeDelta;
            counterRoot.localRotation = goalsArea.localRotation;
            counterRoot.localScale = goalsArea.localScale;

            var content = new GameObject("Conteúdo", typeof(RectTransform), typeof(CanvasGroup));
            content.layer = layer;
            counterContent = (RectTransform)content.transform;
            counterContent.SetParent(counterRoot, false);
            counterContent.anchorMin = counterContent.anchorMax = new Vector2(0.5f, 0.5f);
            counterContent.pivot = new Vector2(0.5f, 0.5f);
            counterContent.anchoredPosition = Vector2.zero;      // âncora no meio = centro da área, qualquer que seja o pivô dela
            counterContent.sizeDelta = Vector2.zero;
            counterFade = content.GetComponent<CanvasGroup>();
            counterFade.alpha = 0f;
            counterFade.blocksRaycasts = false;
            counterFade.interactable = false;

            counterGlow = NewImage("Brilho", counterContent, layer);
            counterGlow.sprite = glow != null ? glow : SoftDot;
            counterGlow.material = MaterialFor(counterGlow.sprite);
            counterIcon = NewImage("Moeda", counterContent, layer);
            counterIcon.sprite = CoinArt;
            counterIcon.preserveAspect = true;

            var label = new GameObject("Valor", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.layer = layer;
            label.transform.SetParent(counterContent, false);
            counterText = label.GetComponent<TextMeshProUGUI>();
            counterText.raycastTarget = false;
            counterText.alignment = TextAlignmentOptions.Center;
            counterText.textWrappingMode = TextWrappingModes.NoWrap;
            counterText.overflowMode = TextOverflowModes.Overflow;
            counterText.enableAutoSizing = false;
            counterText.margin = Vector4.zero;
            counterText.color = counter.textColor;
            var sample = GoalText();
            if (counter.useGoalTextStyle && sample != null)
            {
                counterText.font = sample.font;
                counterText.fontSharedMaterial = sample.fontSharedMaterial;
                counterText.fontStyle = sample.fontStyle;
                counterText.color = sample.color;
            }
            else
            {
                var art = LobbyFeaturesConfig.Instance;
                if (art != null && art.displayFont != null) counterText.font = art.displayFont;
            }
            var labelRect = counterText.rectTransform;
            labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = new Vector2(0.5f, 0.5f);

            if (!string.IsNullOrEmpty(counter.plaqueTitle) && !string.IsNullOrEmpty(counter.plaqueObjectName) && topPanel != null)
                foreach (var child in topPanel.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name != counter.plaqueObjectName) continue;
                    plaque = child.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (plaque != null) { plaqueBefore = plaque.text; plaque.text = counter.plaqueTitle; }
                    break;
                }

            sourceGlow = Rent(backLayer, glow, true);
            sourceRays = Rent(backLayer, sparkle6, true);

            shown = 0f; shownValue = -1;
            WriteCounter(0);
            swapped = true;
            swapAge = 0f;
            return true;
        }

        TextMeshProUGUI GoalText()
        {
            if (goals == null || goals.list == null) return null;
            foreach (var item in goals.list)
                if (item != null && item.text != null) return item.text;
            return null;
        }

        RectTransform Layer(string layerName)
        {
            var node = new GameObject(layerName, typeof(RectTransform));
            node.layer = overlay.gameObject.layer;
            var rect = (RectTransform)node.transform;
            rect.SetParent(overlay, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        static Image NewImage(string imageName, Transform parent, int layer)
        {
            var node = new GameObject(imageName, typeof(RectTransform), typeof(Image));
            node.layer = layer;
            node.transform.SetParent(parent, false);
            var image = node.GetComponent<Image>();
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            return image;
        }

        Sprite CoinArt
        {
            get
            {
                if (coinSprite != null) return coinSprite;
                var art = LobbyFeaturesConfig.Instance;
                return art != null && art.coin != null ? art.coin : SoftDot;
            }
        }

        static float Aspect(Sprite sprite) =>
            sprite == null || sprite.rect.height <= 0f ? 1f : sprite.rect.width / sprite.rect.height;

        void WriteCounter(int value)
        {
            if (counterText == null || value == shownValue) return;
            shownValue = value;
            counterText.text = (counter.prefix ?? "") + CoinFormat.Short(value);
            LayoutCounter();
        }

        float counterFit = 1f;

        void LayoutCounter()
        {
            if (counterRoot == null || counterText == null || counterIcon == null) return;
            var area = counterRoot.rect;
            var height = Mathf.Max(1f, area.height);
            var iconHeight = height * counter.iconHeight;
            var iconWidth = iconHeight * Aspect(counterIcon.sprite);
            counterText.fontSize = height * counter.textHeight;
            var text = counterText.GetPreferredValues(counterText.text);
            var gap = height * counter.gap;
            var total = iconWidth + gap + text.x;
            counterFit = total > 0f ? Mathf.Min(1f, Mathf.Max(1f, area.width) * counter.maxWidth / total) : 1f;
            var left = -total * 0.5f;
            counterIcon.rectTransform.anchoredPosition = new Vector2(left + iconWidth * 0.5f, 0f);
            counterIcon.rectTransform.sizeDelta = new Vector2(iconWidth, iconHeight);
            counterText.rectTransform.anchoredPosition = new Vector2(left + iconWidth + gap + text.x * 0.5f, 0f);
            counterText.rectTransform.sizeDelta = new Vector2(text.x + 8f, height);
            if (counterGlow != null)
            {
                counterGlow.rectTransform.anchoredPosition = counterIcon.rectTransform.anchoredPosition;
                counterGlow.rectTransform.sizeDelta = Vector2.one * iconHeight * counter.glowSize;
            }
        }

        /// Devolve o HUD ao que era: objetivos visíveis, placa com o texto original, contador e moedas removidos.
        public void RestoreHud()
        {
            if (restored) return;
            restored = true;
            LevelManager.OnLevelLoaded -= Close;
            if (goalsFade != null)
            {
                goalsFade.alpha = goalsAlphaBefore;
                if (addedGoalsFade) Destroy(goalsFade);
            }
            if (plaque != null && plaqueBefore != null) plaque.text = plaqueBefore;
            if (counterRoot != null) Destroy(counterRoot.gameObject);
            if (overlay != null) Destroy(overlay.gameObject);
            coins.Clear(); sparks.Clear(); pool.Clear();
            foreach (var material in materialByTexture.Values)
                if (material != null) Destroy(material);
            materialByTexture.Clear();
            if (ownsAdditiveBase && additiveBase != null) Destroy(additiveBase);
            additiveBase = null;
        }

        /// Restaura o HUD e remove o efeito.
        public void Close()
        {
            RestoreHud();
            if (this != null) Destroy(gameObject);
        }

        void OnDestroy() => RestoreHud();

        // ------------------------------------------------------------------------------------------ ciclo

        void Update()
        {
            if (restored || !started) return;
            // Um erro no efeito devolve o HUD e encerra o efeito; a fase segue normalmente.
            try { Step(); }
            catch (Exception e) { Debug.LogException(e); Close(); }
        }

        void Step()
        {
            var dt = Time.deltaTime;
            clock += dt;

            if (!demo)
            {
                if (owner == null || owner.levelData != data) { Close(); return; }
                var state = owner.gameStatus;
                if (state != GameState.PreWinAnimations && state != GameState.Win) { Close(); return; }
                if (state == GameState.Win && !finished) Finish();
            }

            if (!finished)
            {
                var target = Target();
                if (target > launchedValue)
                {
                    if (!swapped)
                    {
                        // Só troca o HUD quando há moedas de verdade: um bônus sem pontos deixa os objetivos como estão.
                        if (!SwapHud()) { Close(); return; }
                        nextLaunch = clock + emission.startDelay;
                    }
                    else if (clock >= nextLaunch && coins.Count < emission.maxFlying)
                        Launch(target - launchedValue);
                }
                else if (demo && swapped && coins.Count == 0 && landedValue >= demoTarget)
                {
                    if (demoDoneAt < 0f) demoDoneAt = clock;
                    else if (clock - demoDoneAt > 1.6f) { Close(); return; }
                }
            }

            if (!swapped) return;
            swapAge += dt;
            var center = WorldToOverlay(boardWorld);
            var cell = Mathf.Max(1f, Vector2.Distance(center, WorldToOverlay(boardWorld + Vector3.right * cellWorld)));
            var end = CounterPoint();

            UpdateCoins(dt, end);
            UpdateSparks(dt);
            UpdateSource(dt, center, cell);
            UpdateCounter(dt);
        }

        /// A tela de vitória abriu: o número fecha no valor final e as moedas ainda no ar somem.
        void Finish()
        {
            finished = true;
            if (!swapped) return;
            var target = Target();
            if (target > landedValue)
            {
                landedValue = target;
                popAge = 0f; glowKick = 1f;
            }
            launchedValue = Mathf.Max(launchedValue, target);
        }

        void Launch(int pending)
        {
            var value = Mathf.Clamp(Mathf.Max(emission.minValuePerCoin, Mathf.CeilToInt(pending * emission.fractionPerCoin)), 1, pending);
            launchedValue += value;
            nextLaunch = clock + emission.interval;
            lastLaunch = clock;

            var center = WorldToOverlay(boardWorld);
            var cell = Mathf.Max(1f, Vector2.Distance(center, WorldToOverlay(boardWorld + Vector3.right * cellWorld)));
            var angle = UnityEngine.Random.value * Mathf.PI * 2f;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var coin = new Coin
            {
                value = value,
                cell = cell,
                seed = UnityEngine.Random.value * 100f,
                side = UnityEngine.Random.Range(-1f, 1f),
                duration = Mathf.Max(0.05f, flight.seconds * (1f + UnityEngine.Random.Range(-flight.secondsVariation, flight.secondsVariation))),
                from = center + direction * (UnityEngine.Random.value * emission.spawnRadius * cell),
                // O salto abre para os lados e um pouco para cima, como uma fonte.
                kick = new Vector2(direction.x, Mathf.Abs(direction.y) * 0.6f + 0.4f) * (flight.popDistance * cell * UnityEngine.Random.Range(0.55f, 1f)),
                comet = Rent(backLayer, glow, true),
                halo = Rent(backLayer, glow, true),
                image = Rent(coinLayer, CoinArt, false),
            };
            coin.image.preserveAspect = true;
            coins.Add(coin);

            // Faíscas do nascimento.
            for (var i = 0; i < source.birthSparks; i++)
            {
                var a = UnityEngine.Random.value * Mathf.PI * 2f;
                var speed = cell * UnityEngine.Random.Range(1.2f, 2.6f);
                AddSpark(i % 2 == 0 ? sparkle4 : dot, coin.from, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed,
                    trail.sparkLife * UnityEngine.Random.Range(0.6f, 1f), cell * trail.sparkSize * UnityEngine.Random.Range(0.8f, 1.3f), 0f,
                    Palette(i + coins.Count), 4f, 18f);
            }
        }

        void UpdateCoins(float dt, Vector2 end)
        {
            for (var i = coins.Count - 1; i >= 0; i--)
            {
                var coin = coins[i];
                coin.age += dt;
                if (finished)
                {
                    coin.fade -= Time.unscaledDeltaTime / Mathf.Max(0.01f, finishFadeSeconds);
                    if (coin.fade <= 0f) { Release(coin); coins.RemoveAt(i); continue; }
                }

                var cell = coin.cell;
                Vector2 position;
                float scale, travel;
                if (coin.age < flight.popSeconds)
                {
                    var t = coin.age / flight.popSeconds;
                    travel = 0f;
                    position = coin.from + coin.kick * EaseOut(t);
                    scale = Mathf.LerpUnclamped(0.2f, 1.15f, EaseOutBack(t));
                }
                else
                {
                    var t = Mathf.Clamp01((coin.age - flight.popSeconds) / coin.duration);
                    travel = t;
                    if (t >= 1f && !finished) { Land(coin, end); Release(coin); coins.RemoveAt(i); continue; }
                    var p = Mathf.Pow(t, flight.acceleration);
                    var u = 1f - p;
                    var start = coin.from + coin.kick;
                    var control1 = start + coin.kick * 1.4f + new Vector2(coin.side * flight.sideSwing * cell, flight.arcHeight * cell * 0.35f);
                    var control2 = end + new Vector2(coin.side * flight.sideSwing * cell * 0.3f, -flight.arcHeight * cell);
                    position = u * u * u * start + 3f * u * u * p * control1 + 3f * u * p * p * control2 + p * p * p * end;
                    scale = Mathf.Lerp(1.15f, flight.arrivalScale, Smooth(Mathf.InverseLerp(0.45f, 1f, t)));
                }

                var height = cell * flight.coinSize * scale;
                var width = height * Aspect(coin.image.sprite);
                // Giro no próprio eixo: a largura fecha e abre.
                var spin = Mathf.Abs(Mathf.Cos(coin.seed + travel * flight.spinTurns * Mathf.PI * 2f));
                var fadeIn = Mathf.Clamp01(coin.age / (flight.popSeconds * 0.5f)) * coin.fade;
                Place(coin.image, position, width * Mathf.Max(0.14f, spin), height, new Color(1f, 1f, 1f, fadeIn), coin.side * 18f * (1f - travel));

                var pulse = 0.85f + 0.15f * Mathf.Sin(clock * 14f + coin.seed);
                var haloColor = Color.Lerp(gold, pink, 0.5f + 0.5f * Mathf.Sin(clock * 5f + coin.seed));
                Place(coin.halo, position, height * trail.haloSize * pulse, height * trail.haloSize * pulse, A(haloColor, trail.haloAlpha * fadeIn));

                var moved = coin.hasPrevious ? position - coin.previous : Vector2.zero;
                var distance = moved.magnitude;
                if (distance > 0.01f && dt > 0f)
                {
                    var direction = moved / distance;
                    var length = Mathf.Min(trail.cometLength * cell, distance / dt * trail.cometSeconds);
                    var cometColor = Color.Lerp(violet, magenta, 0.5f + 0.5f * Mathf.Sin(coin.seed * 3f));
                    Place(coin.comet, position - direction * (length * 0.45f), length + height * 0.6f, trail.cometWidth * cell * scale,
                        A(cometColor, trail.cometAlpha * fadeIn * Mathf.Clamp01(length / (cell * 0.4f))), Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

                    // Poeira de faíscas ao longo do trecho percorrido neste quadro.
                    coin.sparkDebt += dt * trail.sparksPerSecond;
                    var made = 0;
                    while (coin.sparkDebt >= 1f)
                    {
                        coin.sparkDebt -= 1f;
                        if (made++ >= 4 || sparks.Count >= trail.maxSparks || finished) continue;
                        var normal = new Vector2(-direction.y, direction.x);
                        var at = Vector2.Lerp(coin.previous, position, UnityEngine.Random.value) +
                                 normal * (UnityEngine.Random.Range(-1f, 1f) * trail.sparkSpread * cell);
                        var drift = -direction * (cell * UnityEngine.Random.Range(0.2f, 0.9f)) + normal * (cell * UnityEngine.Random.Range(-0.5f, 0.5f));
                        var pick = UnityEngine.Random.value;
                        AddSpark(pick < 0.55f ? sparkle4 : pick < 0.8f ? dot : sparkle6, at, drift,
                            trail.sparkLife * UnityEngine.Random.Range(0.6f, 1.15f), cell * trail.sparkSize * UnityEngine.Random.Range(0.55f, 1.25f), 0f,
                            Palette(sparks.Count), 2.5f, UnityEngine.Random.Range(14f, 26f));
                    }
                }
                else if (coin.comet != null) coin.comet.color = Color.clear;

                coin.previous = position;
                coin.hasPrevious = true;
            }
        }

        void Land(Coin coin, Vector2 at)
        {
            landedValue += coin.value;
            popAge = 0f;
            glowKick = 1f;
            var cell = coin.cell;

            // Clarão, anel e um núcleo pequeno e claro.
            AddSpark(glow, at, Vector2.zero, arrival.seconds * 0.55f, arrival.flashSize * cell * 0.4f, arrival.flashSize * cell, A(gold, 0.85f), 0f, 0f);
            AddSpark(ring != null ? ring : glow, at, Vector2.zero, arrival.seconds * 0.85f, arrival.ringSize * cell * 0.3f, arrival.ringSize * cell,
                A(Color.Lerp(magenta, pink, UnityEngine.Random.value), 0.9f), 0f, 0f);
            AddSpark(glow, at, Vector2.zero, arrival.seconds * 0.4f, arrival.flashSize * cell * 0.3f, arrival.flashSize * cell * 0.12f, A(core, 0.9f), 0f, 0f);

            var offset = UnityEngine.Random.value * Mathf.PI * 2f;
            for (var i = 0; i < arrival.sparks; i++)
            {
                var angle = offset + (i + UnityEngine.Random.Range(-0.3f, 0.3f)) * Mathf.PI * 2f / Mathf.Max(1, arrival.sparks);
                // Com o arrasto, a faísca percorre mais ou menos "sparkDistance" casas antes de apagar.
                var speed = arrival.sparkDistance * cell * 3.2f / Mathf.Max(0.05f, arrival.seconds) * UnityEngine.Random.Range(0.6f, 1.1f);
                AddSpark(i % 3 == 2 ? sparkle6 : sparkle4, at, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                    arrival.seconds * UnityEngine.Random.Range(0.75f, 1.1f), cell * trail.sparkSize * UnityEngine.Random.Range(1f, 1.7f), 0f,
                    Palette(i), 5f, UnityEngine.Random.Range(12f, 22f));
            }

            if (arrival.playSound && clock - lastSound >= arrival.soundInterval)
            {
                var sounds = SweetSugar.Scripts.SoundBase.Instance;
                var clip = arrival.sound != null ? arrival.sound : sounds != null ? sounds.cash : null;
                if (sounds != null && clip != null) { sounds.PlayOneShot(clip); lastSound = clock; }
            }
        }

        void Release(Coin coin)
        {
            Return(coin.image); Return(coin.halo); Return(coin.comet);
            coin.image = coin.halo = coin.comet = null;
        }

        /// Faísca, anel ou clarão: cresce de sizeFrom a sizeTo (0 = encolhe até sumir), anda, freia e apaga.
        void AddSpark(Sprite sprite, Vector2 position, Vector2 velocity, float life, float sizeFrom, float sizeTo, Color color, float drag, float twinkle)
        {
            if (frontLayer == null || life <= 0f) return;
            sparks.Add(new Spark
            {
                image = Rent(frontLayer, sprite, true),
                position = position, velocity = velocity, life = life, sizeFrom = sizeFrom, sizeTo = sizeTo, color = color, drag = drag,
                twinkle = twinkle, seed = UnityEngine.Random.value * 10f, angle = UnityEngine.Random.value * 360f,
                spin = twinkle > 0f ? UnityEngine.Random.Range(-160f, 160f) : 0f,
            });
        }

        void UpdateSparks(float dt)
        {
            for (var i = sparks.Count - 1; i >= 0; i--)
            {
                var spark = sparks[i];
                spark.age += dt;
                var t = spark.age / spark.life;
                if (t >= 1f) { Return(spark.image); sparks.RemoveAt(i); continue; }
                spark.position += spark.velocity * dt;
                spark.velocity *= Mathf.Clamp01(1f - spark.drag * dt);
                spark.angle += spark.spin * dt;
                float size, alpha;
                if (spark.sizeTo > 0f)
                {
                    // Anéis e clarões: abrem rápido e apagam.
                    size = Mathf.Lerp(spark.sizeFrom, spark.sizeTo, EaseOut(t));
                    alpha = (1f - t) * (1f - t);
                }
                else
                {
                    // Faíscas: surgem, cintilam e encolhem.
                    size = spark.sizeFrom * Mathf.Sin(Mathf.Clamp01(t * 1.15f + 0.12f) * Mathf.PI);
                    alpha = (1f - t * t) * (0.65f + 0.35f * Mathf.Sin(spark.age * spark.twinkle + spark.seed));
                }
                Place(spark.image, spark.position, size, size, A(spark.color, spark.color.a * alpha), spark.angle);
            }
        }

        void UpdateSource(float dt, Vector2 center, float cell)
        {
            // Aceso enquanto saem moedas; apaga pouco depois da última.
            var lit = !finished && (coins.Count > 0 || clock - lastLaunch < 0.35f);
            sourceLevel = Mathf.MoveTowards(sourceLevel, lit ? 1f : 0f, dt * (lit ? 5f : 2.5f));
            var pulse = 1f + 0.08f * Mathf.Sin(clock * source.pulseSpeed * Mathf.PI * 2f);
            if (sourceGlow != null)
                Place(sourceGlow, center, source.size * cell * pulse, source.size * cell * pulse,
                    A(Color.Lerp(violet, magenta, 0.5f + 0.5f * Mathf.Sin(clock * 2.1f)), source.alpha * sourceLevel));
            if (sourceRays != null)
                Place(sourceRays, center, source.raysSize * cell * pulse, source.raysSize * cell * pulse,
                    A(gold, source.raysAlpha * sourceLevel), clock * source.raysSpin);
        }

        void UpdateCounter(float dt)
        {
            if (goalsFade != null) goalsFade.alpha = goalsAlphaBefore * (1f - Smooth(swapAge / counter.goalsFadeSeconds));

            var appear = Mathf.Clamp01((swapAge - counter.goalsFadeSeconds * 0.5f) / counter.appearSeconds);
            popAge += dt;
            var pop = popAge < arrival.counterPopSeconds ? arrival.counterPop * Mathf.Sin(popAge / arrival.counterPopSeconds * Mathf.PI) : 0f;
            if (counterContent != null) counterContent.localScale = Vector3.one * (counterFit * EaseOutBack(appear) * (1f + pop));
            if (counterFade != null) counterFade.alpha = Mathf.Clamp01(appear * 2.5f);

            // O número corre até o valor das moedas que já chegaram.
            shown = Mathf.MoveTowards(shown, landedValue, Mathf.Max(1f, landedValue - shown) / counter.countSeconds * Mathf.Max(dt, Time.unscaledDeltaTime * 0.5f));
            WriteCounter(Mathf.RoundToInt(shown));

            glowKick = Mathf.MoveTowards(glowKick, 0f, dt * 3f);
            if (counterGlow != null)
            {
                var breathe = 0.75f + 0.25f * Mathf.Sin(clock * 4f);
                counterGlow.color = A(Color.Lerp(gold, pink, glowKick * 0.35f), counter.glowAlpha * appear * Mathf.Clamp01(breathe + glowKick * 0.6f));
                counterGlow.rectTransform.localScale = Vector3.one * (1f + 0.25f * glowKick);
            }
        }

        // ------------------------------------------------------------------------------------------ coordenadas

        Vector2 WorldToOverlay(Vector3 world)
        {
            var camera = ViewCamera;
            if (camera == null || overlay == null || hudCanvas == null) return Vector2.zero;
            Vector2 screen = camera.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screen,
                hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hudCanvas.worldCamera, out var point);
            return point;
        }

        /// Centro da moeda do contador, nas coordenadas da camada das moedas.
        Vector2 CounterPoint()
        {
            if (overlay == null) return Vector2.zero;
            RectTransform rect = counterIcon != null ? counterIcon.rectTransform : counterRoot;
            if (rect == null) return Vector2.zero;
            return overlay.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
        }

        // ------------------------------------------------------------------------------------------ pool e material

        Image Rent(RectTransform layer, Sprite sprite, bool light)
        {
            Image image;
            if (pool.Count > 0)
            {
                image = pool.Pop();
                image.transform.SetParent(layer, false);
            }
            else image = NewImage("VFX", layer, overlay.gameObject.layer);
            image.gameObject.SetActive(true);
            image.transform.SetAsLastSibling();
            image.sprite = sprite != null ? sprite : SoftDot;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.material = light ? MaterialFor(image.sprite) : null;
            image.color = Color.clear;
            var rect = image.rectTransform;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.sizeDelta = Vector2.zero;
            return image;
        }

        void Return(Image image)
        {
            if (image == null) return;
            image.color = Color.clear;
            image.gameObject.SetActive(false);
            pool.Push(image);
        }

        // Um material por textura (a lição do PowerUpVfxPlayer): com um material só, sprites de texturas diferentes podem
        // cair no mesmo lote e sair todos com a textura do primeiro.
        Material MaterialFor(Sprite sprite)
        {
            if (!additiveSearched)
            {
                additiveSearched = true;
                additiveBase = additive;
                if (additiveBase == null)
                {
                    var shader = Shader.Find("RoyalAves/ImpactSprite");
                    if (shader != null)
                    {
                        additiveBase = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                        additiveBase.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        additiveBase.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                        ownsAdditiveBase = true;
                    }
                }
            }
            if (additiveBase == null) return null;      // sem o shader: mistura normal da UI
            var texture = sprite != null ? sprite.texture : null;
            if (texture == null) return additiveBase;
            if (materialByTexture.TryGetValue(texture, out var material) && material != null) return material;
            material = new Material(additiveBase) { hideFlags = HideFlags.HideAndDontSave, mainTexture = texture };
            materialByTexture[texture] = material;
            return material;
        }

        // ------------------------------------------------------------------------------------------ utilidades

        Color Palette(int index)
        {
            switch (((index % 6) + 6) % 6)
            {
                case 0: return gold;
                case 1: return pink;
                case 2: return gold;
                case 3: return cyan;
                case 4: return magenta;
                default: return violet;
            }
        }

        static void Place(Image image, Vector2 position, float width, float height, Color color, float angle = 0f)
        {
            if (image == null) return;
            var rect = image.rectTransform;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(Mathf.Max(0f, width), Mathf.Max(0f, height));
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
            image.color = color;
        }

        static Color A(Color color, float alpha) { color.a = Mathf.Clamp01(alpha); return color; }
        static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }
        static float EaseOut(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x); }

        static float EaseOutBack(float x)
        {
            x = Mathf.Clamp01(x);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            var y = x - 1f;
            return 1f + c3 * y * y * y + c1 * y * y;
        }

        // Reserva para máscaras que faltem no prefab: um ponto de luz macio gerado em código.
        static Sprite softDot;

        static Sprite SoftDot
        {
            get
            {
                if (softDot != null) return softDot;
                const int n = 64;
                var texture = new Texture2D(n, n, TextureFormat.RGBA32, false)
                    { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var pixels = new Color[n * n];
                for (var y = 0; y < n; y++)
                    for (var x = 0; x < n; x++)
                    {
                        var dx = (x + 0.5f) / n * 2f - 1f;
                        var dy = (y + 0.5f) / n * 2f - 1f;
                        var d = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                        pixels[y * n + x] = new Color(1f, 1f, 1f, d * d);
                    }
                texture.SetPixels(pixels);
                texture.Apply();
                softDot = Sprite.Create(texture, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                softDot.hideFlags = HideFlags.HideAndDontSave;
                return softDot;
            }
        }
    }
}
