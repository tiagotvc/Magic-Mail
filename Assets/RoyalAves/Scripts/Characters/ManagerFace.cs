// The red-haired manager on screen, animated by swapping her expression pictures (ManagerExpressionSet).
// In a level (mood Game) she follows the board:
// - she blinks and now and then glances to one side;
// - when the goals are well ahead of the moves spent ("winning easily") she also smiles between blinks and glances;
// - with 5 moves (or 10 seconds) left she gets nervous, more so as they run out;
// - winning while nervous makes her relieved; any other win shows the victory face; losing makes her sad.
// The other moods are for the other screens: Idle (title, loading, lobby) blinks, glances and smiles; Worried, Sad,
// Relieved and Victory hold that expression, for Sweet Sugar's popups.
using System.Collections.Generic;
using SweetSugar.Scripts.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Characters
{
    public enum ManagerMood { Game, Idle, Worried, Sad, Relieved, Victory }

    [RequireComponent(typeof(Image))]
    public class ManagerFace : MonoBehaviour
    {
        [SerializeField] ManagerMood mood = ManagerMood.Game;
        [Tooltip("Vazio = Resources/GerenteExpressoes.")]
        [SerializeField] ManagerExpressionSet expressions;

        [Header("Piscar, olhar para os lados e sorrir (segundos, mínimo e máximo)")]
        [SerializeField] Vector2 blinkEvery = new Vector2(2.2f, 5f);
        [SerializeField] Vector2 glanceEvery = new Vector2(5f, 11f);
        [SerializeField] Vector2 glanceFor = new Vector2(0.7f, 1.4f);
        [Tooltip("Só quando está vencendo fácil (ou nas telas fora do jogo).")]
        [SerializeField] Vector2 smileEvery = new Vector2(3.5f, 7f);
        [SerializeField] Vector2 smileFor = new Vector2(1.2f, 2.2f);
        [Tooltip("Cara pensativa, de vez em quando, quando o jogo está parelho.")]
        [SerializeField] Vector2 thinkEvery = new Vector2(14f, 24f);
        [SerializeField] Vector2 thinkFor = new Vector2(1.2f, 1.8f);
        [Tooltip("Tempo de cada imagem intermediária na troca de expressão.")]
        [SerializeField, Range(0.02f, 0.2f)] float inBetween = 0.06f;

        [Header("Tela de jogo")]
        [Tooltip("Movimentos restantes em que ela começa a ficar nervosa.")]
        [SerializeField, Min(1)] int nervousMoves = 5;
        [Tooltip("Segundos restantes em que ela fica nervosa, nos níveis por tempo.")]
        [SerializeField, Min(1)] int nervousSeconds = 10;
        [Tooltip("Quanto os objetivos precisam estar à frente dos movimentos gastos para ela achar fácil (0,2 = 20%).")]
        [SerializeField, Range(0.05f, 1)] float easyLead = 0.2f;
        [Tooltip("Um sorriso rápido a cada combo, quando não está nervosa.")]
        [SerializeField] bool smileOnCombo = true;

        [Header("Respiração")]
        [SerializeField, Range(0, 0.05f)] float breathing = 0.012f;

        enum Face { Calm, Easy, Nervous, Worried, Relieved, Victory, Sad }

        const float SadBeforeTears = 1.8f;

        readonly Queue<(Sprite sprite, float seconds)> frames = new Queue<(Sprite, float)>();
        Image image;
        Vector3 baseScale;
        Face face;
        int nervousLevel;
        float stateTime;
        Sprite frameSprite;
        float frameLeft;
        float nextBlink, nextGlance, nextSmile, nextFidget, nextThink;
        float pop;

        // What is on screen: the wanted picture and the in-between pictures still to show on the way to it.
        readonly List<Sprite> tween = new List<Sprite>();
        Sprite goal;
        float tweenLeft;

        // The level being played, to tell an easy win and a tense one.
        int startLimit = -1;
        int startGoals;
        bool easy;
        bool wonTense;
        float nextEasyCheck;

        /// Whether the last level was won with her nervous (few moves left); the victory screen then shows her relieved.
        public static bool LastWinWasTense { get; private set; }

        public ManagerMood Mood
        {
            get => mood;
            set => mood = value;
        }

        void Awake()
        {
            image = GetComponent<Image>();
            baseScale = transform.localScale;
            if (expressions == null) expressions = ManagerExpressionSet.Instance;
            if (expressions == null)
            {
                Debug.LogWarning($"Royal Aves: falta Resources/{ManagerExpressionSet.ResourcePath} (menu Royal Aves > Aplicar gerente ruiva).", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            LevelManager.OnCombo += OnCombo;
            face = Evaluate(out nervousLevel);
            Enter(face, nervousLevel, false);
            tween.Clear();
            image.sprite = goal = BaseSprite();
        }

        void OnDisable()
        {
            LevelManager.OnCombo -= OnCombo;
            transform.localScale = baseScale;
        }

        void Update()
        {
            var dt = Time.unscaledDeltaTime; // she keeps blinking while Sweet Sugar pauses the game under a popup
            var next = Evaluate(out var level);
            if (next != face || next == Face.Nervous && level != nervousLevel)
                Enter(next, level, true);
            stateTime += dt;
            Ambient();

            if (frameSprite != null && (frameLeft -= dt) <= 0) frameSprite = null;
            if (frameSprite == null && frames.Count > 0)
                (frameSprite, frameLeft) = frames.Dequeue();
            Show(frameSprite != null ? frameSprite : BaseSprite(), dt);
            Breathe(dt);
        }

        Face Evaluate(out int level)
        {
            level = 0;
            switch (mood)
            {
                case ManagerMood.Idle: return Face.Easy;
                case ManagerMood.Worried: level = 2; return Face.Worried;
                case ManagerMood.Sad: return Face.Sad;
                case ManagerMood.Relieved: return Face.Relieved;
                case ManagerMood.Victory: return Face.Victory;
            }

            var game = LevelManager.THIS;
            if (game == null || game.levelData == null) return Face.Calm;
            switch (game.gameStatus)
            {
                case GameState.PreWinAnimations:
                case GameState.Win:
                    // Decided on the frame the level is won: nervous then means relieved now.
                    if (face != Face.Relieved && face != Face.Victory) wonTense = face == Face.Nervous;
                    LastWinWasTense = wonTense;
                    return wonTense ? Face.Relieved : Face.Victory;
                case GameState.PreFailed:
                case GameState.BombFailed:
                    level = 2;
                    return Face.Worried;
                case GameState.GameOver:
                    return Face.Sad;
                case GameState.Map:
                case GameState.PrepareGame:
                    startLimit = -1;
                    easy = false;
                    LastWinWasTense = false;
                    return Face.Calm;
            }

            TrackLevel(game);
            var left = game.levelData.limit;
            var threshold = game.levelData.limitType == LIMIT.MOVES ? nervousMoves : nervousSeconds;
            if (left <= threshold)
            {
                // 5-4 moves left: a bit nervous; 3-2: nervous; 1-0: very nervous.
                level = left > threshold * 0.6f ? 0 : left > threshold * 0.2f ? 1 : 2;
                return Face.Nervous;
            }
            return easy ? Face.Easy : Face.Calm;
        }

        // "Winning easily": the share of the goals already collected is well ahead of the share of moves spent.
        void TrackLevel(LevelManager game)
        {
            if (game.gameStatus != GameState.Playing || Time.unscaledTime < nextEasyCheck) return;
            nextEasyCheck = Time.unscaledTime + 0.5f;
            var goals = RemainingGoals(game);
            if (startLimit < 0)
            {
                startLimit = game.levelData.limit;
                startGoals = goals;
            }
            var spent = startLimit > 0 ? 1f - game.levelData.limit / (float)startLimit : 0;
            if (spent <= 0 || startGoals <= 0)
            {
                easy = false;
                return;
            }
            var lead = 1f - goals / (float)startGoals - spent;
            easy = easy ? lead >= easyLead * 0.5f : lead >= easyLead;
        }

        static int RemainingGoals(LevelManager game)
        {
            var counters = game.levelData.TargetCounters;
            if (counters == null) return 0;
            var remaining = 0;
            foreach (var counter in counters)
                if (counter != null && counter.targetLevel != null && !counter.IsTargetStars())
                    remaining += Mathf.Max(0, counter.count);
            return remaining;
        }

        void Enter(Face next, int level, bool animate)
        {
            if (animate && next != Face.Calm && next != Face.Easy) pop = 1;
            face = next;
            nervousLevel = level;
            stateTime = 0;
            frames.Clear();
            frameSprite = null;
            var now = Time.unscaledTime;
            nextBlink = now + Random.Range(blinkEvery.x, blinkEvery.y);
            nextGlance = now + Random.Range(glanceEvery.x, glanceEvery.y);
            nextSmile = now + Random.Range(smileEvery.x, smileEvery.y) * 0.5f;
            nextFidget = now + Random.Range(1.2f, 2.8f);
            nextThink = now + Random.Range(thinkEvery.x, thinkEvery.y);
        }

        // Blinks, glances, smiles and nervous fidgets, one at a time.
        void Ambient()
        {
            if (frameSprite != null || frames.Count > 0) return;
            var now = Time.unscaledTime;
            switch (face)
            {
                case Face.Calm:
                case Face.Easy:
                    if (face == Face.Easy && now >= nextSmile)
                    {
                        // Mostly the happy face, sometimes the confident one.
                        var smiling = Random.value < 0.4f && expressions.confident != null ? expressions.confident : expressions.happy;
                        Queue(smiling, Random.Range(smileFor.x, smileFor.y));
                        nextSmile = now + Random.Range(smileEvery.x, smileEvery.y);
                        nextBlink = Mathf.Max(nextBlink, now + smileFor.y + 0.4f);
                    }
                    else if (face == Face.Calm && now >= nextThink)
                    {
                        Queue(expressions.thinking, Random.Range(thinkFor.x, thinkFor.y));
                        nextThink = now + Random.Range(thinkEvery.x, thinkEvery.y);
                    }
                    else if (now >= nextGlance)
                    {
                        Glance();
                        nextGlance = now + Random.Range(glanceEvery.x, glanceEvery.y);
                    }
                    else if (now >= nextBlink)
                    {
                        Blink();
                        nextBlink = now + Random.Range(blinkEvery.x, blinkEvery.y);
                    }
                    break;
                case Face.Nervous:
                case Face.Worried:
                    // A flicker to a neighbouring level of worry keeps her restless.
                    if (now >= nextFidget)
                    {
                        Queue(expressions.Nervous(nervousLevel < 2 ? nervousLevel + 1 : 1), Random.Range(0.25f, 0.5f));
                        nextFidget = now + Random.Range(1.2f, 2.8f);
                    }
                    break;
            }
        }

        void Blink()
        {
            Queue(expressions.blinkHalf, 0.05f);
            Queue(expressions.blinkClosed, 0.09f);
            Queue(expressions.blinkHalf, 0.05f);
        }

        void Glance()
        {
            var first = Random.value < 0.5f ? expressions.lookLeft : expressions.lookRight;
            Queue(first, Random.Range(glanceFor.x, glanceFor.y));
            if (Random.value < 0.35f)
                Queue(first == expressions.lookLeft ? expressions.lookRight : expressions.lookLeft, Random.Range(glanceFor.x, glanceFor.y));
            if (Random.value < 0.5f) Blink();
        }

        void OnCombo()
        {
            if (!smileOnCombo || mood != ManagerMood.Game || face != Face.Calm && face != Face.Easy) return;
            frames.Clear();
            frameSprite = null;
            Queue(expressions.surprised, 0.3f);
            Queue(expressions.happy, 1.1f);
        }

        void Queue(Sprite sprite, float seconds)
        {
            if (sprite != null) frames.Enqueue((sprite, seconds));
        }

        Sprite BaseSprite()
        {
            switch (face)
            {
                case Face.Nervous: return expressions.Nervous(nervousLevel);
                case Face.Worried: return expressions.Nervous(2);
                case Face.Relieved: return expressions.Or(expressions.relieved);
                case Face.Victory: return expressions.Or(expressions.victory);
                case Face.Sad:
                    return expressions.Or(stateTime < SadBeforeTears || expressions.crying == null ? expressions.sad : expressions.crying);
                default: return expressions.neutral;
            }
        }

        // A new expression is reached through the in-between pictures (ManagerExpressionSet.Between), one every
        // inBetween seconds; blinks and glances have none and swap at once.
        void Show(Sprite target, float dt)
        {
            if (target != goal)
            {
                goal = target;
                tween.Clear();
                tween.AddRange(expressions.Between(image.sprite, target));
                tweenLeft = inBetween;
            }
            if (tween.Count > 0)
            {
                image.sprite = tween[0];
                if ((tweenLeft -= dt) <= 0)
                {
                    tween.RemoveAt(0);
                    tweenLeft = inBetween;
                }
                if (tween.Count > 0) return;
            }
            if (image.sprite != goal) image.sprite = goal;
        }

        // A slow breath (the pivot is at her feet) and a small pop when her mood changes.
        void Breathe(float dt)
        {
            pop = Mathf.Max(0, pop - dt / 0.3f);
            var bump = 0.06f * Mathf.Sin(pop * Mathf.PI);
            var breath = breathing * Mathf.Sin(Time.unscaledTime * 2 * Mathf.PI / 3.2f);
            transform.localScale = new Vector3(baseScale.x * (1 + bump), baseScale.y * (1 + bump + breath), baseScale.z);
        }
    }
}
