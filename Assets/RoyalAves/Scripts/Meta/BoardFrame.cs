// The frame drawn around the match-3 board, assembled from the pieces the user drew: four corners placed at the
// corners and two straight bands tiled along the edges between them. The 100 Sweet Sugar levels use 17 different grid
// shapes (5x5 up to 11x11, and a level's fields may differ from one another), so nothing is drawn per level — the same
// six pieces cover every board.
//
// Sweet Sugar has no frame around the board at all (its "Border" is edge decoration on each square), so this is new.
// It builds itself when the game starts, from Resources/MolduraTabuleiro (BoardFrameSettings), which serves both
// game.unity and gameStatic.unity without either scene being edited.
//
// Everything is laid out in "band units": the pieces are imported at 256 pixels per unit and were all normalised to a
// 256 px band, so one unit is exactly one band width, and the whole thing is then scaled to the thickness wanted.
using System.Linq;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Level;
using UnityEngine;

namespace RoyalAves.Meta
{
    public class BoardFrame : MonoBehaviour
    {
        public BoardFrameSettings settings;

        SpriteRenderer topLeft, topRight, bottomLeft, bottomRight, top, bottom, left, right;
        FieldBoard shownField;
        int shownSquares;
        static BoardFrame current;

        /// Builds the frame once the game is running, wherever the level is played.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Spawn()
        {
            if (current != null) return;
            var settings = Resources.Load<BoardFrameSettings>(BoardFrameSettings.ResourcePath);
            if (settings == null || !settings.IsComplete) return;
            var made = new GameObject("MolduraTabuleiro");
            DontDestroyOnLoad(made);
            current = made.AddComponent<BoardFrame>();
        }

        void Awake()
        {
            if (settings == null) settings = Resources.Load<BoardFrameSettings>(BoardFrameSettings.ResourcePath);
            if (settings == null || !settings.IsComplete) return;
            topLeft = Piece("CantoCimaEsq", settings.cornerTopLeft, SpriteDrawMode.Simple);
            topRight = Piece("CantoCimaDir", settings.cornerTopRight, SpriteDrawMode.Simple);
            bottomLeft = Piece("CantoBaixoEsq", settings.cornerBottomLeft, SpriteDrawMode.Simple);
            bottomRight = Piece("CantoBaixoDir", settings.cornerBottomRight, SpriteDrawMode.Simple);
            top = Piece("FaixaCima", settings.edgeHorizontal, SpriteDrawMode.Tiled);
            bottom = Piece("FaixaBaixo", settings.edgeHorizontal, SpriteDrawMode.Tiled);
            left = Piece("FaixaEsq", settings.edgeVertical, SpriteDrawMode.Tiled);
            right = Piece("FaixaDir", settings.edgeVertical, SpriteDrawMode.Tiled);
            Hide();
        }

        SpriteRenderer Piece(string name, Sprite sprite, SpriteDrawMode mode)
        {
            var made = new GameObject(name);
            made.transform.SetParent(transform, false);
            var renderer = made.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = mode;
            return renderer;
        }

        void LateUpdate()
        {
            if (topLeft == null) return;
            var field = CurrentField();
            if (field == null || field.squaresArray == null || field.squaresArray.Length == 0)
            {
                shownField = null;
                Hide();
                return;
            }

            // Sublevels swap the field and boosters may clear squares, so the fit is redone whenever either changes.
            var squares = field.squaresArray.Count(s => s != null && !s.IsNone());
            if (squares == 0)
            {
                Hide();
                return;
            }
            if (field != shownField || squares != shownSquares)
            {
                shownField = field;
                shownSquares = squares;
                Fit(field);
            }
        }

        // LevelManager.field indexes its list by the current sublevel and throws while a level is being built.
        static FieldBoard CurrentField()
        {
            var manager = LevelManager.THIS;
            if (manager == null || manager.fieldBoards == null) return null;
            var index = manager.CurrentSubLevel - 1;
            return index >= 0 && index < manager.fieldBoards.Count ? manager.fieldBoards[index] : null;
        }

        void Fit(FieldBoard field)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var square in field.squaresArray)
            {
                if (square == null || square.IsNone()) continue;
                Vector2 spot = square.transform.position;
                min = Vector2.Min(min, spot);
                max = Vector2.Max(max, spot);
            }
            if (min.x > max.x) return;

            // The positions are square centres: half a cell reaches the outer edge of the board.
            var half = new Vector2(field.squareWidth, field.squareHeight) * 0.5f;
            min -= half + Vector2.one * settings.padding;
            max += half + Vector2.one * settings.padding;

            var thickness = Mathf.Max(0.01f, settings.thickness);
            transform.localScale = Vector3.one * thickness;
            var centre = (min + max) * 0.5f;
            transform.position = new Vector3(centre.x, centre.y, field.transform.position.z + settings.depth);

            // From here on everything is in band units, which the scale above turns into world units.
            var opening = (max - min) / thickness;
            var corner = CornerSize();
            var hx = opening.x * 0.5f + 1f;          // the band sits outside the opening
            var hy = opening.y * 0.5f + 1f;

            Place(topLeft, -hx + corner.x * 0.5f, hy - corner.y * 0.5f);
            Place(topRight, hx - corner.x * 0.5f, hy - corner.y * 0.5f);
            Place(bottomLeft, -hx + corner.x * 0.5f, -hy + corner.y * 0.5f);
            Place(bottomRight, hx - corner.x * 0.5f, -hy + corner.y * 0.5f);

            // The straight bands run between the corners and a little way under them, so no seam shows.
            var run = 2f * (hx - corner.x) + settings.overlap * 2f;
            Stretch(top, 0f, hy - 0.5f, run, 1f);
            Stretch(bottom, 0f, -hy + 0.5f, run, 1f);
            var rise = 2f * (hy - corner.y) + settings.overlap * 2f;
            Stretch(left, -hx + 0.5f, 0f, 1f, rise);
            Stretch(right, hx - 0.5f, 0f, 1f, rise);

            SortBehind(field);
        }

        Vector2 CornerSize()
        {
            var sprite = settings.cornerTopLeft;
            return new Vector2(sprite.rect.width, sprite.rect.height) / sprite.pixelsPerUnit;
        }

        static void Place(SpriteRenderer piece, float x, float y)
        {
            piece.transform.localPosition = new Vector3(x, y, 0f);
            piece.enabled = true;
        }

        /// A band with no room left means the corners already meet: it is simply not drawn.
        static void Stretch(SpriteRenderer piece, float x, float y, float w, float h)
        {
            if (w <= 0.01f || h <= 0.01f)
            {
                piece.enabled = false;
                return;
            }
            piece.transform.localPosition = new Vector3(x, y, 0.01f);   // just behind the corners
            piece.size = new Vector2(w, h);
            piece.enabled = true;
        }

        void Hide()
        {
            foreach (var piece in new[] { topLeft, topRight, bottomLeft, bottomRight, top, bottom, left, right })
                if (piece != null) piece.enabled = false;
        }

        // The frame takes the squares' own sorting layer and order, and goes behind them on z alone. A lower order
        // would also put it behind the blurred room: that background is a screen-space canvas on sorting layer 0 at
        // order 0, the same as the squares, so anything at order -1 disappears under it.
        void SortBehind(FieldBoard field)
        {
            var renderers = field.GetComponentsInChildren<SpriteRenderer>(true).ToList();
            if (renderers.Count == 0) return;
            var layer = renderers[0].sortingLayerID;
            var order = renderers.Min(r => r.sortingOrder);
            foreach (var piece in new[] { topLeft, topRight, bottomLeft, bottomRight, top, bottom, left, right })
            {
                if (piece == null) continue;
                piece.sortingLayerID = layer;
                piece.sortingOrder = order;
            }
        }
    }
}
