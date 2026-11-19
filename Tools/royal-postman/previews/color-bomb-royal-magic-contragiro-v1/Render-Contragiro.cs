using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

public static class RenderContragiro
{
    private const int Size = 384;
    private const int Count = 48;
    private const int TileSize = 128;
    private const int Columns = 8;
    private const double CenterX = 626.0;
    private const double Tau = 2 * Math.PI;

    private struct Rail
    {
        public readonly double Y, Radius, Rise;

        public Rail(double y, double radius, double rise)
        {
            Y = y;
            Radius = radius;
            Rise = rise;
        }

        public double At(double x)
        {
            double u = Math.Min(1, Math.Abs(x - CenterX) / Radius);
            return Y - Rise * u * u;
        }
    }

    private struct Tile
    {
        public readonly double TopLeft, TopRight, BottomLeft, BottomRight;

        public Tile(double topLeft, double topRight, double bottomLeft, double bottomRight)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
        }

        public double At(double u, double v)
        {
            return Lerp(Lerp(TopLeft, BottomLeft, v), Lerp(TopRight, BottomRight, v), u);
        }
    }

    private struct Ink
    {
        public readonly double Red, Green, Blue, Alpha;

        public Ink(double red, double green, double blue, double alpha = 255)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        public static Ink Blend(Ink a, Ink b, double t)
        {
            return new Ink(Lerp(a.Red, b.Red, t), Lerp(a.Green, b.Green, t),
                Lerp(a.Blue, b.Blue, t), Lerp(a.Alpha, b.Alpha, t));
        }

        public Ink Darken(double factor)
        {
            return new Ink(Red * factor, Green * factor, Blue * factor, Alpha);
        }

        public int Pack()
        {
            return ((int)(Alpha + .5) << 24) | ((int)(Red + .5) << 16)
                | ((int)(Green + .5) << 8) | (int)(Blue + .5);
        }

        public static Ink Unpack(int value)
        {
            return new Ink((value >> 16) & 255, (value >> 8) & 255, value & 255, (value >> 24) & 255);
        }
    }

    private sealed class Raster
    {
        public readonly int Width, Height;
        public readonly int[] Pixels;

        public Raster(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new int[width * height];
        }

        public static Raster Open(string path)
        {
            using (Bitmap loaded = new Bitmap(path))
            using (Bitmap bitmap = loaded.Clone(new Rectangle(0, 0, loaded.Width, loaded.Height), PixelFormat.Format32bppArgb))
            {
                Raster raster = new Raster(bitmap.Width, bitmap.Height);
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                for (int y = 0; y < bitmap.Height; y++)
                    Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), raster.Pixels, y * bitmap.Width, bitmap.Width);
                bitmap.UnlockBits(data);
                return raster;
            }
        }

        public void Save(string path)
        {
            using (Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
            {
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, Width, Height),
                    ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                for (int y = 0; y < Height; y++)
                    Marshal.Copy(Pixels, y * Width, IntPtr.Add(data.Scan0, y * data.Stride), Width);
                bitmap.UnlockBits(data);
                bitmap.Save(path, ImageFormat.Png);
            }
        }

        public Ink Sample(double x, double y, bool wrap = false)
        {
            if (wrap)
                x = x - Math.Floor(x / Width) * Width;
            else
                x = Math.Max(0, Math.Min(Width - 1, x));
            y = Math.Max(0, Math.Min(Height - 1, y));
            int x0 = (int)x;
            int y0 = (int)y;
            int x1 = x0 + 1;
            if (x1 == Width)
                x1 = wrap ? 0 : x0;
            int y1 = Math.Min(y0 + 1, Height - 1);
            Ink top = Ink.Blend(Ink.Unpack(Pixels[y0 * Width + x0]), Ink.Unpack(Pixels[y0 * Width + x1]), x - x0);
            Ink bottom = Ink.Blend(Ink.Unpack(Pixels[y1 * Width + x0]), Ink.Unpack(Pixels[y1 * Width + x1]), x - x0);
            return Ink.Blend(top, bottom, y - y0);
        }
    }

    private struct Mapping
    {
        public int Layer;
        public double U, V, Weight, Shade;
        public Ink Original;
    }

    private sealed class Layer
    {
        public readonly Rail Top, Bottom;
        public readonly int Direction;
        public readonly double Origin;
        public readonly Raster Atlas;

        public Layer(Raster source, Rail top, Rail bottom, int direction, double origin, Ink seam, params Tile[] tiles)
        {
            Top = top;
            Bottom = bottom;
            Direction = direction;
            Origin = origin;
            Atlas = new Raster(TileSize * tiles.Length, TileSize);

            for (int y = 0; y < TileSize; y++)
            {
                double v = y / (double)(TileSize - 1);
                for (int slot = 0; slot < tiles.Length; slot++)
                {
                    for (int x = 0; x < TileSize; x++)
                    {
                        double u = (x + .5) / TileSize;
                        double sx = tiles[slot].At(u, v);
                        double sy = Lerp(top.At(sx), bottom.At(sx), v);
                        Ink paint = source.Sample(sx, sy);
                        double edge = Smooth(0, .034, Math.Min(u, 1 - u));
                        double coverage = Smooth(205, 251, paint.Alpha);
                        Atlas.Pixels[y * Atlas.Width + slot * TileSize + x] =
                            Ink.Blend(seam, new Ink(paint.Red, paint.Green, paint.Blue), edge * coverage).Pack();
                    }
                }
            }
        }

        public bool Locate(double x, double y, out Mapping mapping)
        {
            mapping = new Mapping();
            double top = Top.At(x);
            double bottom = Bottom.At(x);
            double v = (y - top) / (bottom - top);
            if (v <= 0 || v >= 1)
                return false;

            double latitudeWidth = Lerp(Top.Radius, Bottom.Radius, v);
            double horizontal = (x - CenterX) / latitudeWidth;
            if (Math.Abs(horizontal) >= .995)
                return false;

            mapping.U = (Math.Asin(horizontal) / Tau) * Atlas.Width + Origin * TileSize;
            mapping.V = v * (TileSize - 1);
            mapping.Weight = Smooth(0, .05, Math.Min(v, 1 - v))
                * (1 - Smooth(.94, .995, Math.Abs(horizontal)));
            mapping.Shade = 1 - .18 * horizontal * horizontal;
            return true;
        }
    }

    private static double Lerp(double a, double b, double t)
    {
        return a + (b - a) * t;
    }

    private static double Smooth(double low, double high, double value)
    {
        double t = Math.Max(0, Math.Min(1, (value - low) / (high - low)));
        return t * t * (3 - 2 * t);
    }

    private static Layer[] BuildLayers(Raster source)
    {
        Rail orangeTop = new Rail(273, 236, 45);
        Rail goldJoint = new Rail(425, 379, 62);
        Rail yellowBottom = new Rail(609, 469, 67);
        Rail beltTop = new Rail(634, 506, 61);
        Rail beltBottom = new Rail(883, 500, 91);
        Rail blueBottom = new Rail(1104, 360, 111);
        Tile orangeLeft = new Tile(407, 534, 288, 496);
        Tile orangeCenter = new Tile(534, 719, 496, 760);
        Tile orangeRight = new Tile(719, 848, 760, 956);
        Tile yellowLeft = new Tile(276, 490, 206, 455);
        Tile yellowCenter = new Tile(490, 769, 455, 796);
        Tile yellowRight = new Tile(769, 971, 796, 1044);
        Tile ruby = new Tile(183, 380, 185, 381);
        Tile amber = new Tile(380, 630, 381, 630);
        Tile emerald = new Tile(630, 881, 630, 880);
        Tile sapphire = new Tile(881, 1072, 880, 1072);
        Tile blueLeft = new Tile(276, 491, 341, 499);
        Tile blueCenter = new Tile(491, 762, 499, 747);
        Tile blueRight = new Tile(762, 973, 747, 917);

        return new[]
        {
            new Layer(source, orangeTop, goldJoint, -1, .5, new Ink(73, 19, 106),
                orangeCenter, orangeRight, orangeLeft, orangeCenter, orangeRight, orangeLeft, orangeCenter, orangeLeft),
            new Layer(source, goldJoint, yellowBottom, -1, .5, new Ink(78, 18, 107),
                yellowCenter, yellowRight, yellowCenter, yellowLeft, yellowCenter, yellowRight, yellowCenter, yellowRight, yellowLeft),
            new Layer(source, beltTop, beltBottom, 1, 0, new Ink(62, 9, 129),
                emerald, sapphire, ruby, amber, emerald, sapphire, ruby, emerald, amber, sapphire, ruby, amber),
            new Layer(source, beltBottom, blueBottom, 1, .5, new Ink(33, 10, 102),
                blueCenter, blueRight, blueCenter, blueLeft, blueCenter, blueRight, blueCenter, blueLeft, blueCenter, blueLeft)
        };
    }

    private static Mapping[] BuildMapping(Raster source, Layer[] layers)
    {
        Mapping[] map = new Mapping[Size * Size];
        // Fixed 2D pose. Screen-space tilt is clockwise and the horizontal scale is narrower.
        double angle = 10.5 * Math.PI / 180;
        double cosine = Math.Cos(angle);
        double sine = Math.Sin(angle);
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                double dx = x + .5 - Size / 2.0;
                double dy = y + .5 - Size / 2.0;
                double sx = CenterX + (dx * cosine + dy * sine) / .285;
                double sy = 649 + (-dx * sine + dy * cosine) / .340;
                Ink sharp = source.Sample(sx, sy);
                Ink from = sharp.Alpha >= 250 ? sharp : SmoothSilhouette(source, sx, sy);
                Mapping current = new Mapping { Layer = -1, Original = from };
                if (from.Alpha > 8)
                {
                    for (int i = 0; i < layers.Length; i++)
                    {
                        Mapping point;
                        if (!layers[i].Locate(sx, sy, out point))
                            continue;
                        point.Layer = i;
                        point.Original = from;
                        current = point;
                        break;
                    }
                }
                map[y * Size + x] = current;
            }
        }
        return map;
    }

    private static Ink SmoothSilhouette(Raster source, double x, double y)
    {
        Ink[] taps =
        {
            source.Sample(x - 1.1, y - .9), source.Sample(x + 1.1, y - .9),
            source.Sample(x - 1.1, y + .9), source.Sample(x + 1.1, y + .9)
        };
        double coverage = 0;
        double red = 0;
        double green = 0;
        double blue = 0;
        foreach (Ink tap in taps)
        {
            double weight = 255 * Math.Max(0, Math.Min(1, (tap.Alpha - 12) / 237));
            coverage += weight;
            red += tap.Red * weight;
            green += tap.Green * weight;
            blue += tap.Blue * weight;
        }
        if (coverage < 4)
            return new Ink(0, 0, 0, 0);
        return new Ink(red / coverage, green / coverage, blue / coverage, coverage / taps.Length);
    }

    private static void PaintFrame(Raster target, Mapping[] map, Layer[] layers, int frame)
    {
        double fraction = (frame % Count) / (double)Count;
        for (int i = 0; i < map.Length; i++)
        {
            Mapping point = map[i];
            Ink color = point.Original;
            if (point.Layer >= 0)
            {
                Layer layer = layers[point.Layer];
                Ink shifted = layer.Atlas.Sample(point.U - layer.Direction * layer.Atlas.Width * fraction, point.V, true);
                shifted = shifted.Darken(point.Shade);
                Ink mixed = Ink.Blend(color, shifted, point.Weight);
                color = new Ink(mixed.Red, mixed.Green, mixed.Blue, color.Alpha);
            }
            target.Pixels[i] = color.Alpha < 1 ? 0 : color.Pack();
        }
    }

    public static void Render(string sourcePath, string outputPath, bool sampleOnly)
    {
        Directory.CreateDirectory(outputPath);
        Raster source = Raster.Open(sourcePath);
        if (source.Width != 1254 || source.Height != 1254)
            throw new ArgumentException("The texture calibration uses the 1254-pixel master.");

        Layer[] layers = BuildLayers(source);
        Mapping[] map = BuildMapping(source, layers);
        Raster target = new Raster(Size, Size);
        if (sampleOnly)
        {
            foreach (int index in new[] { 0, 2, 4, 9 })
            {
                PaintFrame(target, map, layers, index);
                target.Save(Path.Combine(outputPath, String.Format("sample-{0:00}.png", index)));
            }
            return;
        }

        string frames = Path.Combine(outputPath, "quadros");
        Directory.CreateDirectory(frames);
        Raster sheet = new Raster(Columns * Size, (Count / Columns) * Size);
        for (int frame = 0; frame < Count; frame++)
        {
            PaintFrame(target, map, layers, frame);
            target.Save(Path.Combine(frames, String.Format("color-bomb-{0:00}.png", frame)));
            int left = frame % Columns * Size;
            int top = frame / Columns * Size;
            for (int y = 0; y < Size; y++)
                Array.Copy(target.Pixels, y * Size, sheet.Pixels, (top + y) * sheet.Width + left, Size);
        }
        PaintFrame(target, map, layers, Count);
        Raster first = Raster.Open(Path.Combine(frames, "color-bomb-00.png"));
        for (int i = 0; i < target.Pixels.Length; i++)
            if (target.Pixels[i] != first.Pixels[i])
                throw new InvalidOperationException("The loop did not return to the initial frame.");
        sheet.Save(Path.Combine(outputPath, "spritesheet.png"));
    }
}
