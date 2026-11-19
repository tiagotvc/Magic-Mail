using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

public static class RenderContragiro
{
    private const int Size = 384;
    private const int Count = 48;
    private const int Columns = 8;
    private const double CenterX = 626;
    private const double CenterY = 648;
    private const double Width = 509;
    private const double Height = 489;
    private const double Pitch = 11 * Math.PI / 180;
    private const double Tau = 2 * Math.PI;
    private const double Join = -.223;
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

        public Ink Sample(double x, double y)
        {
            x = Math.Max(0, Math.Min(Width - 1, x));
            y = Math.Max(0, Math.Min(Height - 1, y));
            int x0 = (int)x;
            int y0 = (int)y;
            int x1 = Math.Min(x0 + 1, Width - 1);
            int y1 = Math.Min(y0 + 1, Height - 1);
            Ink top = Ink.Blend(Ink.Unpack(Pixels[y0 * Width + x0]), Ink.Unpack(Pixels[y0 * Width + x1]), x - x0);
            Ink bottom = Ink.Blend(Ink.Unpack(Pixels[y1 * Width + x0]), Ink.Unpack(Pixels[y1 * Width + x1]), x - x0);
            return Ink.Blend(top, bottom, y - y0);
        }
    }

    private struct PosePixel
    {
        public double Longitude, Latitude, FaceLight, Alpha;
        public int Row;
        public Ink Body;
        public bool Upper;
    }

    private sealed class PaintRow
    {
        public readonly double Start, End, Phase;
        public readonly int Slots;
        public readonly bool HasGems;
        private readonly Raster[] paintings;
        private readonly Ink rimLight, rimShade;

        public PaintRow(double start, double end, int slots, double phase, Ink light, Ink dark, params Raster[] tiles)
        {
            Start = start;
            End = end;
            Slots = slots;
            Phase = phase;
            paintings = tiles;
            HasGems = tiles.Length > 1;
            rimLight = light;
            rimShade = dark;
        }

        public Ink Paint(PosePixel pixel, double rotation)
        {
            double cell = (pixel.Longitude - rotation) / Tau * Slots + Phase;
            int whole = (int)Math.Floor(cell);
            double u = cell - whole;
            double v = (pixel.Latitude - Start) / (End - Start);
            double rimTone = (1 - v) * .80 + (1 - u) * .20;
            double horizontal = HasGems ? .408 : .461;
            double vertical = HasGems ? .386 : .422;
            double radius = HasGems ? .105 : .085;
            Ink rim = Ink.Blend(rimShade, rimLight, rimTone);

            // Every panel in this row shares the same bounds and bevel, including at the wrap.
            double distance = RoundedBox(u, v, horizontal, vertical, radius);
            double edge = 1 - Smooth(-.008, .010, distance);
            double shadow = 1 - Smooth(-.010, .043, RoundedBox(u + .022, v - .025, horizontal, vertical, radius));
            Ink body = Ink.Blend(pixel.Body, pixel.Body.Darken(.61), shadow * .61);
            if (edge <= 0)
                return body;

            Ink panel;
            if (HasGems)
            {
                double jewelDistance = RoundedBox(u, v, .311, .286, .074);
                double seat = 1 - Smooth(.012, .052, jewelDistance);
                panel = Ink.Blend(rim, new Ink(109, 39, 6), seat * .79);
                Raster gemstone = paintings[Wrap(whole, Slots)];
                Ink paint = SampleArt(gemstone, u, v, .311, .286);
                panel = Ink.Blend(panel, paint, 1 - Smooth(-.017, .007, jewelDistance));
            }
            else
            {
                Ink paint = SampleArt(paintings[0], u, v, horizontal - .035, vertical - .037);
                panel = Ink.Blend(rim, paint, Smooth(.020, .076, -distance));
            }

            return Ink.Blend(body, panel.Darken(pixel.FaceLight), edge);
        }

        private static Ink SampleArt(Raster art, double u, double v, double halfWidth, double halfHeight)
        {
            double x = (u - .5) / (2 * halfWidth) + .5;
            double y = (v - .5) / (2 * halfHeight) + .5;
            return art.Sample(x * (art.Width - 1), y * (art.Height - 1));
        }
    }

    private static double Lerp(double a, double b, double t)
    {
        return a + (b - a) * t;
    }

    private static int Wrap(int value, int period)
    {
        return ((value % period) + period) % period;
    }

    private static double Smooth(double min, double max, double value)
    {
        double t = Math.Max(0, Math.Min(1, (value - min) / (max - min)));
        return t * t * (3 - 2 * t);
    }

    private static double RoundedBox(double u, double v, double halfWidth, double halfHeight, double radius)
    {
        double x = Math.Abs(u - .5) - (halfWidth - radius);
        double y = Math.Abs(v - .5) - (halfHeight - radius);
        double outsideX = Math.Max(0, x);
        double outsideY = Math.Max(0, y);
        return Math.Sqrt(outsideX * outsideX + outsideY * outsideY) + Math.Min(Math.Max(x, y), 0) - radius;
    }

    private static Raster ExtractPanel(Raster source, double top, double bottom, double halfAngle)
    {
        Raster output = new Raster(112, 112);
        for (int y = 0; y < output.Height; y++)
        {
            double latitude = Lerp(top, bottom, y / (double)(output.Height - 1));
            double radius = Math.Sqrt(1 - latitude * latitude);
            for (int x = 0; x < output.Width; x++)
            {
                double longitude = Lerp(-halfAngle, halfAngle, x / (double)(output.Width - 1));
                double sx = CenterX + Width * radius * Math.Sin(longitude);
                double sy = CenterY + Height * (latitude * Math.Cos(Pitch) + radius * Math.Cos(longitude) * Math.Sin(Pitch));
                output.Pixels[y * output.Width + x] = source.Sample(sx, sy).Pack();
            }
        }
        return output;
    }

    private static Raster ExtractJewel(Raster source, double left, double top, double right, double bottom)
    {
        Raster output = new Raster(112, 112);
        for (int y = 0; y < output.Height; y++)
        {
            double sy = Lerp(top, bottom, y / (double)(output.Height - 1));
            for (int x = 0; x < output.Width; x++)
            {
                double sx = Lerp(left, right, x / (double)(output.Width - 1));
                output.Pixels[y * output.Width + x] = source.Sample(sx, sy).Pack();
            }
        }
        return output;
    }

    private static PaintRow[] MakeRows(Raster source)
    {
        Raster orange = ExtractPanel(source, -.827, -.658, .210);
        Raster yellow = ExtractPanel(source, -.564, -.305, .204);
        Raster blue = ExtractPanel(source, .377, .819, .207);
        Raster ruby = ExtractJewel(source, 221, 676, 315, 791);
        Raster amber = ExtractJewel(source, 444, 697, 558, 809);
        Raster emerald = ExtractJewel(source, 707, 707, 811, 812);
        Raster sapphire = ExtractJewel(source, 938, 680, 1038, 782);
        Raster magenta = ExtractJewel(source, 520, 190, 734, 221);

        return new[]
        {
            new PaintRow(-.871, -.620, 10, .5, new Ink(255, 197, 39), new Ink(197, 56, 5), orange),
            new PaintRow(-.620, -.271, 10, .5, new Ink(255, 237, 100), new Ink(218, 111, 4), yellow),
            new PaintRow(-.186, .317, 12, 0, new Ink(255, 238, 111), new Ink(172, 68, 3),
                emerald, sapphire, magenta, ruby, amber, emerald, sapphire, magenta, ruby, sapphire, ruby, amber),
            new PaintRow(.317, .874, 12, 0, new Ink(43, 223, 252), new Ink(13, 49, 177), blue)
        };
    }

    private static Ink PaintBody(Raster source, double sx, double sy, double latitude, double longitude, double light)
    {
        if (latitude < -.871)
        {
            Ink cap = source.Sample(sx, sy);
            // The painted pink cap and its purple seat have no directional markings.
            if (cap.Alpha > 170 && cap.Blue > cap.Red * .36)
                return cap;
            return new Ink(92, 16, 158).Darken(light);
        }

        Ink body;
        if (latitude < -.271)
        {
            body = Ink.Blend(new Ink(106, 30, 144), new Ink(53, 8, 96), Smooth(-.8, -.34, latitude));
        }
        else if (latitude < -.186)
        {
            double track = Smooth(-.271, -.226, latitude) * (1 - Smooth(-.225, -.189, latitude));
            body = Ink.Blend(new Ink(73, 10, 131), new Ink(184, 77, 242), track);
        }
        else if (latitude < .317)
        {
            body = Ink.Blend(new Ink(103, 37, 168), new Ink(48, 8, 111), Smooth(-.185, .316, latitude));
        }
        else if (latitude < .874)
        {
            body = Ink.Blend(new Ink(42, 10, 120), new Ink(32, 8, 93), Smooth(.32, .85, latitude));
        }
        else
        {
            double reflected = Smooth(.875, .945, latitude) * (1 - Smooth(.95, 1, latitude));
            body = Ink.Blend(new Ink(60, 6, 133), new Ink(147, 26, 205), reflected);
        }

        double side = 1 - .08 * Math.Max(0, Math.Sin(longitude));
        return body.Darken(light * side);
    }

    private static PosePixel[] MapPose(Raster source, PaintRow[] rows)
    {
        PosePixel[] result = new PosePixel[Size * Size];
        double tilt = 10.5 * Math.PI / 180;
        double cosine = Math.Cos(tilt);
        double sine = Math.Sin(tilt);
        double pitchCosine = Math.Cos(Pitch);
        double pitchSine = Math.Sin(Pitch);
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                double dx = x + .5 - Size / 2.0;
                double dy = y + .5 - Size / 2.0;
                double sx = CenterX + (dx * cosine + dy * sine) / .285;
                double sy = CenterY + (-dx * sine + dy * cosine) / .340;
                double nx = (sx - CenterX) / Width;
                double ny = (sy - CenterY) / Height;
                double radiusSquared = nx * nx + ny * ny;
                double alpha = 255 * (1 - Smooth(.985, 1.012, radiusSquared));
                if (alpha < .5)
                    continue;

                // A single 2D spherical warp places every panel on the same latitude curves.
                double depth = Math.Sqrt(Math.Max(0, 1 - radiusSquared));
                double latitude = ny * pitchCosine - depth * pitchSine;
                double longitude = Math.Atan2(nx, ny * pitchSine + depth * pitchCosine);
                double light = .74 + .26 * Math.Sqrt(Math.Max(0, depth));
                int row = -1;
                for (int i = 0; i < rows.Length; i++)
                    if (latitude >= rows[i].Start && latitude < rows[i].End)
                    {
                        row = i;
                        break;
                    }
                result[y * Size + x] = new PosePixel
                {
                    Longitude = longitude,
                    Latitude = latitude,
                    Alpha = alpha,
                    FaceLight = light,
                    Row = row,
                    Body = PaintBody(source, sx, sy, latitude, longitude, light),
                    Upper = latitude < Join
                };
            }
        }
        return result;
    }

    private static void PaintFrame(PosePixel[] pose, PaintRow[] rows, Raster upper, Raster lower,
        Raster together, int upperFrame, int lowerFrame)
    {
        double upperTurn = -Tau * Wrap(upperFrame, Count) / Count;
        double lowerTurn = Tau * Wrap(lowerFrame, Count) / Count;
        for (int i = 0; i < pose.Length; i++)
        {
            PosePixel pixel = pose[i];
            if (pixel.Alpha < .5)
                continue;
            Ink ink = pixel.Row >= 0 ? rows[pixel.Row].Paint(pixel, pixel.Upper ? upperTurn : lowerTurn) : pixel.Body;
            int color = new Ink(ink.Red, ink.Green, ink.Blue, pixel.Alpha).Pack();
            if (pixel.Upper)
                upper.Pixels[i] = color;
            else
                lower.Pixels[i] = color;
            together.Pixels[i] = color;
        }
    }

    private static void AddToSheet(Raster frame, Raster sheet, int index)
    {
        int left = index % Columns * Size;
        int top = index / Columns * Size;
        for (int y = 0; y < Size; y++)
            Array.Copy(frame.Pixels, y * Size, sheet.Pixels, (top + y) * sheet.Width + left, Size);
    }

    private static void Combine(Raster upper, Raster lower, Raster output)
    {
        for (int i = 0; i < output.Pixels.Length; i++)
            output.Pixels[i] = upper.Pixels[i] | lower.Pixels[i];
    }

    public static void Render(string sourcePath, string outputPath, string previewPath, bool samplesOnly)
    {
        Directory.CreateDirectory(outputPath);
        Directory.CreateDirectory(previewPath);
        Raster source = Raster.Open(sourcePath);
        if (source.Width != 1254 || source.Height != 1254)
            throw new ArgumentException("The texture calibration uses the 1254-pixel master.");
        PaintRow[] rows = MakeRows(source);
        PosePixel[] pose = MapPose(source, rows);
        Raster upper = new Raster(Size, Size);
        Raster lower = new Raster(Size, Size);
        Raster together = new Raster(Size, Size);
        if (samplesOnly)
        {
            foreach (int frame in new[] { 0, 2, 4, 7, 13 })
            {
                PaintFrame(pose, rows, upper, lower, together, frame, frame);
                together.Save(Path.Combine(outputPath, String.Format("sample-{0:00}.png", frame)));
            }
            return;
        }

        Raster initialUpper = new Raster(Size, Size);
        Raster initialLower = new Raster(Size, Size);
        PaintFrame(pose, rows, initialUpper, initialLower, together, 0, 0);
        together.Save(Path.Combine(outputPath, "posicao-inclinada.png"));
        Raster upperSheet = new Raster(Columns * Size, Count / Columns * Size);
        Raster lowerSheet = new Raster(upperSheet.Width, upperSheet.Height);
        Raster combinedSheet = new Raster(upperSheet.Width, upperSheet.Height);
        string combinedFrames = Path.Combine(outputPath, "quadros");
        Directory.CreateDirectory(combinedFrames);
        for (int i = 0; i < Count; i++)
        {
            string file = String.Format("frame-{0:00}.png", i);
            PaintFrame(pose, rows, upper, lower, together, i, i);
            together.Save(Path.Combine(combinedFrames, file));
            AddToSheet(upper, upperSheet, i);
            AddToSheet(lower, lowerSheet, i);
            AddToSheet(together, combinedSheet, i);
            Combine(upper, initialLower, together);
            together.Save(Path.Combine(previewPath, "topo-" + file));
            Combine(initialUpper, lower, together);
            together.Save(Path.Combine(previewPath, "base-" + file));
        }
        upperSheet.Save(Path.Combine(outputPath, "spritesheet-topo.png"));
        lowerSheet.Save(Path.Combine(outputPath, "spritesheet-base.png"));
        combinedSheet.Save(Path.Combine(outputPath, "spritesheet.png"));
    }
}
