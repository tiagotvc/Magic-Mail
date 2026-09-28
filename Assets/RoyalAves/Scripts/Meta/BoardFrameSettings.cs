// The pieces the board frame is built from, and how they are fitted. It lives in Resources so the frame can assemble
// itself when the game starts, instead of being placed in game.unity and gameStatic.unity: one place to configure,
// both scenes served, and no Sweet Sugar scene has to be edited at all.
//
// The 100 levels use 17 grid shapes (5x5 up to 11x11), so nothing is drawn per level: four corners are placed and the
// two straight bands are tiled between them, however wide and tall the board turns out to be.
// The asset is made by "Royal Aves > Aplicar moldura do tabuleiro"; tune it in the Inspector.
using UnityEngine;

namespace RoyalAves.Meta
{
    public class BoardFrameSettings : ScriptableObject
    {
        public const string ResourcePath = "MolduraTabuleiro";

        [Header("Peças")]
        public Sprite cornerTopLeft;
        public Sprite cornerTopRight;
        public Sprite cornerBottomLeft;
        public Sprite cornerBottomRight;

        [Tooltip("Straight band tiled along the top and bottom edges.")]
        public Sprite edgeHorizontal;

        [Tooltip("Straight band tiled along the left and right edges.")]
        public Sprite edgeVertical;

        [Header("Encaixe")]
        [Tooltip("Thickness of the band in world units. A board cell is 1.2, so about a third of a cell reads well.")]
        public float thickness = 0.42f;

        [Tooltip("Gap left between the outermost squares and the inner edge of the frame.")]
        public float padding = 0.12f;

        [Tooltip("How far behind the board the frame sits. It shares the squares' sorting order, so this is what puts " +
                 "it behind them — and it stays in front of the blurred room, which is a canvas 100 units away.")]
        public float depth = 0.5f;

        [Tooltip("How far the straight bands run under the corners, in band widths, so no seam shows between them.")]
        public float overlap = 0.05f;

        public bool IsComplete => cornerTopLeft != null && cornerTopRight != null && cornerBottomLeft != null &&
                                  cornerBottomRight != null && edgeHorizontal != null && edgeVertical != null;
    }
}
