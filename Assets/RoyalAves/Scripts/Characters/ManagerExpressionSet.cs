// The expressions of the red-haired manager, Correio Mágico's main character: one picture per expression, all with the
// same framing. A single asset (Resources/GerenteExpressoes) serves every scene, so swapping a picture here changes it in
// the whole game.
// The in-between pictures make the changes smooth: going from one expression to another passes through the pictures
// between them (Between), e.g. neutral > small smile > open smile > happy.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Characters
{
    [CreateAssetMenu(menuName = "Royal Aves/Expressões da gerente", fileName = "GerenteExpressoes")]
    public class ManagerExpressionSet : ScriptableObject
    {
        public const string ResourcePath = "GerenteExpressoes";

        [Header("Parada")]
        public Sprite neutral;
        public Sprite blinkHalf;
        public Sprite blinkClosed;
        public Sprite lookLeft;
        public Sprite lookRight;
        [Tooltip("De vez em quando, quando o jogo está parelho.")]
        public Sprite thinking;

        [Header("Reações")]
        public Sprite happy;
        [Tooltip("Alterna com a feliz quando está vencendo fácil.")]
        public Sprite confident;
        [Tooltip("Um instante antes do sorriso, nos combos.")]
        public Sprite surprised;
        [Tooltip("Da menos para a mais nervosa.")]
        public Sprite[] nervous = new Sprite[3];
        public Sprite relieved;
        public Sprite victory;
        public Sprite sad;
        public Sprite crying;

        [Header("Intermediárias (deixam a troca de expressão fluida)")]
        [Tooltip("Neutra > sorriso > sorriso aberto > feliz.")]
        public Sprite smile;
        public Sprite smileOpen;
        [Tooltip("Neutra > leve > nervosa 1 > média > nervosa 2 > forte > nervosa 3.")]
        public Sprite worryLight;
        public Sprite worryMid;
        public Sprite worryHigh;
        [Tooltip("Nervosa 1 > aliviando > aliviada > aliviada suave > neutra.")]
        public Sprite reliefStart;
        public Sprite reliefSoft;

        static ManagerExpressionSet instance;

        [NonSerialized] Dictionary<Sprite, List<Sprite>> links;
        [NonSerialized] readonly Dictionary<(Sprite, Sprite), List<Sprite>> paths = new Dictionary<(Sprite, Sprite), List<Sprite>>();

        public static ManagerExpressionSet Instance =>
            instance != null ? instance : instance = Resources.Load<ManagerExpressionSet>(ResourcePath);

        public Sprite Nervous(int level)
        {
            if (nervous == null || nervous.Length == 0) return neutral;
            return Or(nervous[Mathf.Clamp(level, 0, nervous.Length - 1)]);
        }

        /// The sprite, or the neutral face when that expression has no picture.
        public Sprite Or(Sprite sprite) => sprite != null ? sprite : neutral;

        void OnValidate()
        {
            links = null;
            paths.Clear();
        }

        /// The pictures to show on the way from one expression to another (neither end included); empty when they are
        /// not related (blinks and glances swap at once).
        public List<Sprite> Between(Sprite from, Sprite to)
        {
            if (from == null || to == null || from == to) return None;
            if (paths.TryGetValue((from, to), out var cached)) return cached;
            if (links == null) BuildLinks();
            var path = None;
            if (links.ContainsKey(from) && links.ContainsKey(to))
            {
                // Breadth-first search: the shortest way through the in-between pictures.
                var cameFrom = new Dictionary<Sprite, Sprite> { [from] = null };
                var open = new Queue<Sprite>();
                open.Enqueue(from);
                while (open.Count > 0 && !cameFrom.ContainsKey(to))
                {
                    var current = open.Dequeue();
                    foreach (var next in links[current])
                        if (!cameFrom.ContainsKey(next))
                        {
                            cameFrom[next] = current;
                            open.Enqueue(next);
                        }
                }
                if (cameFrom.ContainsKey(to))
                {
                    path = new List<Sprite>();
                    for (var step = cameFrom[to]; step != null && step != from; step = cameFrom[step]) path.Insert(0, step);
                }
            }
            paths[(from, to)] = path;
            return path;
        }

        static readonly List<Sprite> None = new List<Sprite>();

        void BuildLinks()
        {
            links = new Dictionary<Sprite, List<Sprite>>();
            var n = nervous ?? new Sprite[0];
            Sprite N(int i) => i < n.Length ? n[i] : null;
            Chain(neutral, smile, smileOpen, happy, victory);
            Chain(smile, confident);
            Chain(neutral, surprised, smileOpen);
            Chain(neutral, thinking);
            Chain(neutral, worryLight, N(0), worryMid, N(1), worryHigh, N(2));
            Chain(N(0), reliefStart, relieved, reliefSoft, neutral);
            Chain(N(2), sad, crying);
        }

        // Links each picture to the next one, skipping missing pictures.
        void Chain(params Sprite[] steps)
        {
            Sprite previous = null;
            foreach (var step in steps)
            {
                if (step == null) continue;
                if (!links.ContainsKey(step)) links[step] = new List<Sprite>();
                if (previous != null && previous != step)
                {
                    if (!links[previous].Contains(step)) links[previous].Add(step);
                    if (!links[step].Contains(previous)) links[step].Add(previous);
                }
                previous = step;
            }
        }
    }
}
