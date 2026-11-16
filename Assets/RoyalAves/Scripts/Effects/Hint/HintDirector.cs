// Liga o efeito de dica (HintEffect) ao jogo. Reaproveita o sistema do Sweet Sugar: o AI.cs continua achando uma jogada
// e esperando o tempo de inatividade; no momento de mostrar a dica ele chama HintDirector.Begin. A partir daí:
//  - lista TODAS as jogadas possíveis do tabuleiro (HintMoveFinder), a jogada achada pelo AI vai primeiro;
//  - mostra uma por vez (showSeconds), com uma pausa entre elas, e quando acabam todas recomeça embaralhando;
//  - cancela na hora quando o jogador toca, o tabuleiro se mexe, a fase termina ou muda.
// Não troca peças de verdade e não altera regras: só lê o tabuleiro e desenha.
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using UnityEngine;

namespace RoyalAves.Effects
{
    public class HintCandidate
    {
        public List<Vector2Int> cells = new List<Vector2Int>();   // casas do contorno (o conjunto final do match)
        public List<Item> pieces = new List<Item>();              // peças que acendem
        public int moverIndex = -1;                                // qual delas escorrega
        public Vector3 direction;                                  // direção do empurrão no mundo
        public string key;

        public void MakeKey()
        {
            var sorted = cells.OrderBy(c => c.x).ThenBy(c => c.y).Select(c => c.x + "," + c.y);
            var mover = moverIndex >= 0 && moverIndex < pieces.Count && pieces[moverIndex] != null && pieces[moverIndex].square != null
                ? pieces[moverIndex].square.col + "," + pieces[moverIndex].square.row : "-";
            key = string.Join(";", sorted) + "|" + mover;
        }
    }

    /// Lê o tabuleiro e devolve todas as trocas que formam um match (3, 4, 5, L, T, cruz).
    public static class HintMoveFinder
    {
        public static bool TryGrid(out HintGrid grid)
        {
            grid = default;
            var lm = LevelManager.THIS;
            if (lm == null || lm.levelData == null) return false;
            var cols = lm.levelData.maxCols; var rows = lm.levelData.maxRows;
            for (var c = 0; c < cols; c++)
                for (var r = 0; r < rows; r++)
                {
                    var s = lm.GetSquare(c, r);
                    if (s == null) continue;
                    var sx = lm.GetSquare(c + 1, r); var sxm = lm.GetSquare(c - 1, r);
                    var sy = lm.GetSquare(c, r + 1); var sym = lm.GetSquare(c, r - 1);
                    var ex = sx != null ? sx.transform.position - s.transform.position
                        : sxm != null ? s.transform.position - sxm.transform.position : new Vector3(lm.squareWidth, 0, 0);
                    var ey = sy != null ? sy.transform.position - s.transform.position
                        : sym != null ? s.transform.position - sym.transform.position : new Vector3(0, -lm.squareWidth, 0);
                    ex.z = 0; ey.z = 0;
                    grid = new HintGrid { ex = ex, ey = ey, origin = s.transform.position - ex * c - ey * r };
                    grid.origin.z = 0;
                    return true;
                }
            return false;
        }

        public static List<HintCandidate> FindAll()
        {
            var result = new List<HintCandidate>();
            var lm = LevelManager.THIS;
            if (lm == null || lm.levelData == null || !TryGrid(out var grid)) return result;
            var cols = lm.levelData.maxCols; var rows = lm.levelData.maxRows;
            var items = new Item[cols, rows];
            var color = new int[cols, rows];
            var movable = new bool[cols, rows];

            for (var c = 0; c < cols; c++)
                for (var r = 0; r < rows; r++)
                {
                    color[c, r] = -1;
                    var sq = lm.GetSquare(c, r);
                    if (sq == null || sq.IsNone()) continue;
                    var it = sq.Item;
                    if (it == null || !it.isActiveAndEnabled || it.falling || it.destroying || it.square != sq ||
                        !it.Combinable || it.IsMulticolorReserved) continue;
                    items[c, r] = it;
                    color[c, r] = it.color;
                    movable[c, r] = sq.CanGoOut() && sq.CanGoInto() && !sq.IsItemLocked;
                }

            var directions = new[] { new Vector2Int(1, 0), new Vector2Int(0, 1) };
            var seen = new HashSet<string>();
            for (var c = 0; c < cols; c++)
                for (var r = 0; r < rows; r++)
                    foreach (var d in directions)
                    {
                        var a = new Vector2Int(c, r); var b = a + d;
                        if (b.x >= cols || b.y >= rows) continue;
                        if (!movable[a.x, a.y] || !movable[b.x, b.y]) continue;
                        if (color[a.x, a.y] == color[b.x, b.y]) continue;
                        var sa = lm.GetSquare(a.x, a.y); var sb = lm.GetSquare(b.x, b.y);
                        var dv = new Vector2(d.x, d.y);
                        if (sa.IsDirectionRestricted(dv) || sa.IsDirectionRestricted(-dv) ||
                            sb.IsDirectionRestricted(dv) || sb.IsDirectionRestricted(-dv)) continue;

                        var ca = color[a.x, a.y]; var cb = color[b.x, b.y];
                        int ColorAt(int x, int y)
                        {
                            if (x < 0 || y < 0 || x >= cols || y >= rows) return -2;
                            if (x == a.x && y == a.y) return cb;
                            if (x == b.x && y == b.y) return ca;
                            return color[x, y];
                        }
                        List<Vector2Int> Match(Vector2Int origin, int col)
                        {
                            var h = new List<Vector2Int> { origin };
                            var v = new List<Vector2Int> { origin };
                            for (var s = 1; ColorAt(origin.x + s, origin.y) == col; s++) h.Add(new Vector2Int(origin.x + s, origin.y));
                            for (var s = 1; ColorAt(origin.x - s, origin.y) == col; s++) h.Add(new Vector2Int(origin.x - s, origin.y));
                            for (var s = 1; ColorAt(origin.x, origin.y + s) == col; s++) v.Add(new Vector2Int(origin.x, origin.y + s));
                            for (var s = 1; ColorAt(origin.x, origin.y - s) == col; s++) v.Add(new Vector2Int(origin.x, origin.y - s));
                            var cells = new List<Vector2Int>();
                            if (h.Count >= 3) cells.AddRange(h);
                            if (v.Count >= 3) foreach (var x in v) if (!cells.Contains(x)) cells.Add(x);
                            return cells;
                        }

                        var matchA = Match(a, cb);   // a peça que estava em B chega em A
                        var matchB = Match(b, ca);   // a peça que estava em A chega em B
                        if (matchA.Count == 0 && matchB.Count == 0) continue;

                        var cand = new HintCandidate();
                        foreach (var cell in matchA.Concat(matchB).Distinct()) cand.cells.Add(cell);
                        foreach (var cell in cand.cells)
                            if (cell != a && cell != b && items[cell.x, cell.y] != null) cand.pieces.Add(items[cell.x, cell.y]);
                        // A peça que se mexe é a que chega ao match maior (empate: a de A, que vai para B).
                        var fromA = matchB.Count > 0 && matchB.Count >= matchA.Count;
                        var moverCell = fromA ? a : b;
                        var otherCell = fromA ? b : a;
                        if (matchA.Count > 0 && matchB.Count > 0)
                            cand.pieces.Add(items[otherCell.x, otherCell.y]);
                        cand.pieces.Add(items[moverCell.x, moverCell.y]);
                        cand.moverIndex = cand.pieces.Count - 1;
                        cand.direction = (grid.Center(otherCell.x, otherCell.y) - grid.Center(moverCell.x, moverCell.y)).normalized;
                        cand.MakeKey();
                        if (seen.Add(cand.key)) result.Add(cand);
                    }
            return result;
        }

        /// Converte a jogada que o AI.cs achou (inclui marmeladas/bônus que o localizador acima não conhece).
        public static HintCandidate FromAI(List<Item> aiItems, Item tip, Vector3 direction)
        {
            if (aiItems == null || !TryGrid(out var grid)) return null;
            var items = aiItems.Where(i => i != null && i.square != null).ToList();
            if (items.Count == 0) return null;
            var cand = new HintCandidate();
            var dir = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.zero;
            if (tip != null && items.Contains(tip) && dir != Vector3.zero)
            {
                var dc = Mathf.RoundToInt(Vector3.Dot(dir, grid.ex.normalized));
                var dr = Mathf.RoundToInt(Vector3.Dot(dir, grid.ey.normalized));
                foreach (var item in items.Where(i => i != tip)) cand.cells.Add(new Vector2Int(item.square.col, item.square.row));
                cand.cells.Add(new Vector2Int(tip.square.col + dc, tip.square.row + dr));
                cand.pieces.AddRange(items.Where(i => i != tip));
                cand.pieces.Add(tip);
                cand.moverIndex = cand.pieces.Count - 1;
                cand.direction = dir;
            }
            else
            {
                foreach (var item in items) cand.cells.Add(new Vector2Int(item.square.col, item.square.row));
                cand.pieces.AddRange(items);
            }
            cand.MakeKey();
            return cand;
        }
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("Royal Aves/Hint Director")]
    public class HintDirector : MonoBehaviour
    {
        public const string ResourcePath = "Effects/Hint/HintEffect";

        [Header("Ciclo de dicas")]
        [Tooltip("Quanto tempo cada dica fica na tela.")]
        [Min(0.8f)] public float showSeconds = 3.2f;
        [Tooltip("Pausa entre uma dica e a seguinte.")]
        [Min(0)] public float pauseSeconds = 0.8f;
        [Tooltip("Embaralha a ordem das dicas a cada volta.")]
        public bool shuffle = true;
        [Tooltip("Limite de dicas por volta (0 = todas as possíveis).")]
        [Min(0)] public int maxHintsPerRound = 0;
        [Tooltip("Depois de tocar na tela sem jogar, a dica volta após o tempo de inatividade padrão do jogo.")]
        public bool rearmAfterTouch = true;

        [SerializeField] HintEffect effect;

        static HintDirector instance;
        Coroutine run;
        bool running;
        int runMoveID = -1;
        bool touchedCancel;

        public static bool IsShowing => instance != null && instance.running;

        static HintDirector Get()
        {
            if (instance != null) return instance;
            var prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null || prefab.GetComponent<HintDirector>() == null) return null;
            var go = Instantiate(prefab);
            go.name = "HintEffect (runtime)";
            instance = go.GetComponent<HintDirector>();
            if (instance.effect == null) instance.effect = go.GetComponent<HintEffect>();
            return instance;
        }

        /// Chamado pelo AI.cs quando a dica deve aparecer. false = prefab ausente (o chamador usa a animação antiga).
        public static bool Begin(List<Item> aiItems, Item aiTip, Vector3 aiDirection)
        {
            var director = Get();
            if (director == null || director.effect == null) return false;
            var lm = LevelManager.THIS;
            if (lm == null) return true;
            if (director.running && director.runMoveID == lm.moveID) return true;   // já está mostrando as dicas desta jogada
            director.StartRun(HintMoveFinder.FromAI(aiItems, aiTip, aiDirection));
            return true;
        }

        public static void CancelAll()
        {
            if (instance != null) instance.Cancel();
        }

        void StartRun(HintCandidate first)
        {
            Cancel();
            runMoveID = LevelManager.THIS.moveID;
            touchedCancel = false;
            running = true;
            run = StartCoroutine(Run(runMoveID, first));
        }

        void Cancel()
        {
            if (run != null) StopCoroutine(run);
            run = null;
            running = false;
            if (effect != null) effect.Stop();
        }

        void OnEnable()
        {
            LevelManager.OnMove += OnBoardEvent;
            LevelManager.OnLevelLoaded += OnBoardEvent;
            LevelManager.OnEnterGame += OnBoardEvent;
            LevelManager.OnWin += OnBoardEvent;
            LevelManager.OnLose += OnBoardEvent;
            InputHandler.OnDown += OnDown;
            InputHandler.OnUp += OnUp;
        }

        void OnDisable()
        {
            LevelManager.OnMove -= OnBoardEvent;
            LevelManager.OnLevelLoaded -= OnBoardEvent;
            LevelManager.OnEnterGame -= OnBoardEvent;
            LevelManager.OnWin -= OnBoardEvent;
            LevelManager.OnLose -= OnBoardEvent;
            InputHandler.OnDown -= OnDown;
            InputHandler.OnUp -= OnUp;
            if (effect != null && effect.IsPlaying) effect.Stop();
            if (instance == this) instance = null;
        }

        void OnBoardEvent() => Cancel();

        void OnDown(Vector2 position)
        {
            if (!running) return;
            touchedCancel = true;
            Cancel();
        }

        void OnUp(Vector2 position)
        {
            if (!touchedCancel || !rearmAfterTouch) return;
            touchedCancel = false;
            StartCoroutine(Rearm());
        }

        IEnumerator Rearm()
        {
            yield return new WaitForSeconds(0.25f);
            var lm = LevelManager.THIS;
            if (lm == null || AI.THIS == null || lm.gameStatus != GameState.Playing || lm.DragBlocked || lm.findMatchesStarted) yield break;
            AI.THIS.StartCoroutine(AI.THIS.CheckPossibleCombines());
        }

        bool Alive(int moveID)
        {
            var lm = LevelManager.THIS;
            return lm != null && (lm.gameStatus == GameState.Playing || lm.gameStatus == GameState.Tutorial) &&
                   !lm.DragBlocked && !lm.findMatchesStarted && lm.moveID == moveID;
        }

        static bool StillValid(HintCandidate cand)
        {
            foreach (var item in cand.pieces)
                if (item == null || !item.isActiveAndEnabled || item.falling || item.destroying || item.square == null || item.square.Item != item)
                    return false;
            return true;
        }

        IEnumerator Run(int moveID, HintCandidate first)
        {
            while (Alive(moveID))
            {
                var list = new List<HintCandidate>();
                if (LevelManager.THIS.gameStatus != GameState.Tutorial) list.AddRange(HintMoveFinder.FindAll());
                if (shuffle)
                    for (var i = list.Count - 1; i > 0; i--)
                    {
                        var j = Random.Range(0, i + 1);
                        (list[i], list[j]) = (list[j], list[i]);
                    }
                if (first != null)
                {
                    list.RemoveAll(c => c.key == first.key);
                    list.Insert(0, first);
                    first = null;
                }
                if (maxHintsPerRound > 0 && list.Count > maxHintsPerRound) list.RemoveRange(maxHintsPerRound, list.Count - maxHintsPerRound);
                if (list.Count == 0) break;

                foreach (var cand in list)
                {
                    if (!Alive(moveID)) break;
                    if (!StillValid(cand) || !HintMoveFinder.TryGrid(out var grid)) continue;

                    var pieces = cand.pieces.Select(MakePiece).ToList();
                    effect.Play(cand.cells, grid, pieces, cand.moverIndex, cand.direction, showSeconds);
                    for (var t = 0f; t < showSeconds; t += Time.deltaTime)
                    {
                        if (!Alive(moveID) || !StillValid(cand)) break;
                        effect.Evaluate(t);
                        yield return null;
                    }
                    effect.Stop();

                    for (var t = 0f; t < pauseSeconds && Alive(moveID); t += Time.deltaTime) yield return null;
                }
            }
            running = false;
            run = null;
            if (effect != null) effect.Stop();
        }

        static HintPiece MakePiece(Item item)
        {
            var renderers = item.GetSpriteRenderers().Where(r => r != null && r.enabled && r.sprite != null).ToArray();
            var main = renderers.OrderByDescending(r => r.bounds.size.x * r.bounds.size.y).FirstOrDefault();
            Transform nudge = item.itemAnimTransform != null && item.itemAnimTransform != item.transform ? item.itemAnimTransform
                : main != null && main.transform != item.transform ? main.transform : item.transform;
            return new HintPiece { nudge = nudge, sources = main != null ? new[] { main } : new SpriteRenderer[0] };
        }
    }
}
