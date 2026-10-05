// Turns plain text into a TMP <sprite name="X"> tag per character, for the gold sprite font. The TMP
// "Sprite Asset" field on a Text component only resolves <sprite> tags — it does not automatically replace
// typed characters that the active font already has its own glyph for, which CorreioMagico SDF does for
// every plain letter/digit. Spaces become <space=N> instead (the sprite sheets have no space glyph).
using System.Text;

namespace RoyalAves.Meta
{
    public static class SpriteFontText
    {
        public static string ToSpriteTags(string text, float spaceWidth = 20f)
        {
            var sb = new StringBuilder();
            foreach (var c in text)
            {
                if (c == ' ') sb.Append($"<space={spaceWidth.ToString(System.Globalization.CultureInfo.InvariantCulture)}>");
                else sb.Append("<sprite name=\"").Append(c).Append("\">");
            }
            return sb.ToString();
        }
    }
}
