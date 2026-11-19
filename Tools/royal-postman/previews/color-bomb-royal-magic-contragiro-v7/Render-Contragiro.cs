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
    private const double Tilt = 10.5 * Math.PI / 180;
    private const double Tau = 2 * Math.PI;
    private const int Top = 0;
    private const int Belt = 1;
    private const int Bottom = 2;
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

        public Ink Over(Ink background)
        {
            double under = background.Alpha * (1 - Alpha / 255);
            double coverage = Alpha + under;
            if (coverage <= 0)
                return new Ink(0, 0, 0, 0);
            return new Ink((Red * Alpha + background.Red * under) / coverage,
                (Green * Alpha + background.Green * under) / coverage,
                (Blue * Alpha + background.Blue * under) / coverage, coverage);
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
        public Ink Detail;
    }

    private sealed class Repaint
    {
        private readonly Raster art;
        private readonly double scale, sourceLeft, sourceTop;
        private static readonly PointF[] BeltUpper = {
            new PointF(95, 528), new PointF(145, 470), new PointF(230, 518), new PointF(350, 568),
            new PointF(500, 603), new PointF(650, 631), new PointF(790, 651), new PointF(935, 659),
            new PointF(1060, 652), new PointF(1175, 623)
        };
        private static readonly PointF[] BeltLower = {
            new PointF(95, 695), new PointF(145, 755), new PointF(230, 809), new PointF(350, 855),
            new PointF(500, 900), new PointF(650, 928), new PointF(790, 947), new PointF(935, 952),
            new PointF(1060, 917), new PointF(1175, 832)
        };
        private static readonly PointF[] CrownLower = {
            new PointF(470, 151), new PointF(500, 185), new PointF(550, 208), new PointF(650, 235),
            new PointF(750, 252), new PointF(850, 258), new PointF(930, 251), new PointF(963, 234)
        };

        public Repaint(Raster art)
        {
            this.art = art;
            if (art.Width != 1254 || art.Height != 1254)
                throw new ArgumentException("The detail masks use the registered 1254-pixel repaint.");
            int left = art.Width, right = 0, top = art.Height;
            for (int y = 0; y < art.Height; y++)
            {
                for (int x = 0; x < art.Width; x++)
                {
                    if (((art.Pixels[y * art.Width + x] >> 24) & 255) < 160)
                        continue;
                    left = Math.Min(left, x);
                    right = Math.Max(right, x);
                    top = Math.Min(top, y);
                }
            }
            if (right <= left)
                throw new ArgumentException("The repaint contains no opaque sprite.");
            sourceLeft = left;
            sourceTop = top;
            scale = 319.0 / (right - left);
        }

        private void Source(double x, double y, out double sx, out double sy)
        {
            sx = sourceLeft + (x - 32.5) / scale;
            sy = sourceTop + (y - 18.0) / scale;
        }

        private static double HeightAt(PointF[] edge, double x)
        {
            for (int i = 1; i < edge.Length; i++)
                if (x <= edge[i].X)
                    return Lerp(edge[i - 1].Y, edge[i].Y, (x - edge[i - 1].X) / (edge[i].X - edge[i - 1].X));
            return edge[edge.Length - 1].Y;
        }

        public Ink BeltAt(double x, double y)
        {
            double sx, sy;
            Source(x, y, out sx, out sy);
            if (sx < 92 || sx > 1178)
                return new Ink(0, 0, 0, 0);
            double weight = Smooth(0, 13, sy - HeightAt(BeltUpper, sx))
                * Smooth(0, 17, HeightAt(BeltLower, sx) - sy);
            Ink painted = art.Sample(sx, sy);
            return new Ink(painted.Red, painted.Green, painted.Blue, painted.Alpha * weight);
        }

        public Ink CrownAt(double x, double y)
        {
            double sx, sy;
            Source(x, y, out sx, out sy);
            if (sx < 470 || sx > 963 || sy < 49 || sy > 260)
                return new Ink(0, 0, 0, 0);
            Ink painted = art.Sample(sx, sy);
            Ink left = art.Sample(sx - 11, sy);
            Ink right = art.Sample(sx + 11, sy);
            double across = Math.Min(left.Alpha, right.Alpha);
            if (across > painted.Alpha)
            {
                // Close resampling nicks between two painted edge samples without expanding the silhouette.
                Ink adjacent = Ink.Blend(left, right, .5);
                double weight = (across - painted.Alpha) / 255;
                painted = Ink.Blend(painted, adjacent, weight);
                painted = new Ink(painted.Red, painted.Green, painted.Blue, across);
            }
            double edge = Smooth(0, 9, HeightAt(CrownLower, sx) - sy);
            double sides = Smooth(470, 484, sx) * (1 - Smooth(948, 963, sx));
            double purple = Smooth(.19, .34, painted.Blue / Math.Max(1, painted.Red));
            double alpha = Smooth(14, 251, painted.Alpha) * edge * sides * purple;
            return new Ink(painted.Red, painted.Green, painted.Blue, alpha * 255);
        }
    }

    private sealed class PaintRow
    {
        public readonly double Start, End, Phase;
        public readonly int Slots;
        public double[] SlotBrightness;
        private readonly Raster painting;
        private readonly Ink rimLight, rimShade;

        public PaintRow(double start, double end, int slots, double phase, Ink light, Ink dark, Raster tile)
        {
            Start = start;
            End = end;
            Slots = slots;
            Phase = phase;
            painting = tile;
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
            const double horizontal = .461;
            const double vertical = .422;
            const double radius = .085;
            Ink rim = Ink.Blend(rimShade, rimLight, rimTone);

            // Every panel in this row shares the same bounds and bevel, including at the wrap.
            double distance = RoundedBox(u, v, horizontal, vertical, radius);
            double edge = 1 - Smooth(-.008, .010, distance);
            double shadow = 1 - Smooth(-.005, .038, RoundedBox(u - .020, v - .062, horizontal, vertical, radius));
            Ink body = Ink.Blend(pixel.Body, pixel.Body.Darken(.50), shadow * .73);
            double wall = 1 - Smooth(-.006, .012, RoundedBox(u - .009, v - .044, horizontal, vertical, radius));
            Ink side = rimShade.Darken(pixel.FaceLight * Lerp(.91, .68, v));
            body = Ink.Blend(body, side, wall);
            if (edge <= 0)
                return body;

            Ink paint = SampleArt(painting, u, v, horizontal - .035, vertical - .037);
            Ink panel = Ink.Blend(rim, paint, Smooth(.023, .086, -distance));

            double brightness = SlotBrightness == null ? 1 : SlotBrightness[Wrap(whole, Slots)];
            return Ink.Blend(body, panel.Darken(pixel.FaceLight * brightness), edge);
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

    private static PaintRow[] MakeRows(Raster source)
    {
        Raster orange = ExtractPanel(source, -.827, -.658, .210);
        Raster yellow = ExtractPanel(source, -.564, -.305, .204);
        Raster blue = ExtractPanel(source, .377, .819, .207);
        return new[]
        {
            new PaintRow(-.871, -.620, 10, .5, new Ink(255, 197, 39), new Ink(197, 56, 5), orange),
            new PaintRow(-.620, -.271, 10, .5, new Ink(255, 237, 100), new Ink(218, 111, 4), yellow),
            new PaintRow(.366, .874, 12, 0, new Ink(43, 223, 252), new Ink(13, 49, 177), blue)
            {
                SlotBrightness = new[] { .99, .92, 1, .94, .97, .92, .96, 1, .95, .90, .99, .94 }
            }
        };
    }

    private static Ink PaintBody(Raster source, double sx, double sy, double latitude, double longitude, double light)
    {
        if (latitude < -.871)
        {
            Ink cap = source.Sample(sx, sy);
            // The new painting supplies the pink stone; retain only the old purple seat underneath.
            if (cap.Alpha > 170 && cap.Blue > cap.Red * 1.02)
                return cap;
            return new Ink(92, 16, 158).Darken(light);
        }

        Ink body;
        if (latitude < -.271)
        {
            body = Ink.Blend(new Ink(106, 30, 144), new Ink(53, 8, 96), Smooth(-.8, -.34, latitude));
        }
        else if (latitude < .366)
        {
            body = new Ink(42, 9, 91);
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

    private static Ink PaintBelt(Repaint art, double x, double y, PosePixel pose)
    {
        double latitude = pose.Latitude;
        double upperLip = Smooth(-.287, -.245, latitude) * (1 - Smooth(-.238, -.180, latitude));
        double lowerLip = Smooth(.270, .325, latitude) * (1 - Smooth(.330, .375, latitude));
        double front = .88 + .12 * Math.Cos(pose.Longitude + .32);
        Ink body = Ink.Blend(new Ink(118, 36, 181), new Ink(55, 8, 122), Smooth(-.17, .348, latitude));
        body = Ink.Blend(body, new Ink(220, 98, 255), upperLip * front);
        body = Ink.Blend(body, new Ink(131, 53, 214), lowerLip * .80);
        body = body.Darken(pose.FaceLight * front);

        Ink painted = art.BeltAt(x, y);
        return Ink.Blend(body, painted, painted.Alpha / 255);
    }

    private static PosePixel Project(double x, double y)
    {
        double squared = x * x + y * y;
        double depth = Math.Sqrt(Math.Max(0, 1 - squared));
        double cosine = Math.Cos(Pitch);
        double sine = Math.Sin(Pitch);
        return new PosePixel
        {
            Longitude = Math.Atan2(x, y * sine + depth * cosine),
            Latitude = y * cosine - depth * sine,
            Alpha = 255 * (1 - Smooth(.985, 1.012, squared)),
            FaceLight = .74 + .26 * Math.Sqrt(depth),
            Row = -1
        };
    }

    private static double BeltWidth(double silhouetteLatitude)
    {
        double shoulder = Smooth(-.287, -.164, silhouetteLatitude)
            * (1 - Smooth(.265, .376, silhouetteLatitude));
        return 1 + .103 * shoulder;
    }

    private static PosePixel ProjectBelt(double x, double y)
    {
        // The width varies along the visible profile, with rounded ends where the cuff meets the shell.
        PosePixel belt = Project(x / BeltWidth(y * Math.Cos(Pitch)), y);
        belt.Alpha *= Smooth(-.289, -.271, belt.Latitude) * (1 - Smooth(.358, .377, belt.Latitude));
        return belt;
    }

    private static int RowAt(PosePixel pixel, PaintRow[] rows, int first, int last)
    {
        for (int i = first; i <= last; i++)
            if (pixel.Latitude >= rows[i].Start && pixel.Latitude < rows[i].End)
                return i;
        return -1;
    }

    private static PosePixel[][] MapPose(Raster source, Repaint art, PaintRow[] rows)
    {
        PosePixel[][] result = new[] { new PosePixel[Size * Size], new PosePixel[Size * Size], new PosePixel[Size * Size] };
        double cosine = Math.Cos(Tilt);
        double sine = Math.Sin(Tilt);
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
                int index = y * Size + x;
                PosePixel shell = Project(nx, ny);
                double latitude = shell.Latitude;
                if (latitude < -.202)
                {
                    shell.Alpha *= 1 - Smooth(-.230, -.202, latitude);
                    shell.Body = PaintBody(source, sx, sy, latitude, shell.Longitude, shell.FaceLight);
                    shell.Row = RowAt(shell, rows, 0, 1);
                    shell.Detail = art.CrownAt(x + .5, y + .5);
                    result[Top][index] = shell;
                }
                else if (latitude > .318)
                {
                    shell.Alpha *= Smooth(.318, .344, latitude);
                    shell.Body = PaintBody(source, sx, sy, latitude, shell.Longitude, shell.FaceLight);
                    shell.Row = RowAt(shell, rows, 2, 2);
                    result[Bottom][index] = shell;
                }

                if (ny > -.33 && ny < .59 && Math.Abs(nx) < 1.11)
                {
                    PosePixel belt = ProjectBelt(nx, ny);
                    belt.Body = PaintBelt(art, x + .5, y + .5, belt);
                    result[Belt][index] = belt;
                }
            }
        }
        return result;
    }

    private static void PaintFrame(PosePixel[] pose, PaintRow[] rows, Raster output, int frame, int direction)
    {
        double turn = direction * Tau * Wrap(frame, Count) / Count;
        for (int i = 0; i < pose.Length; i++)
        {
            PosePixel pixel = pose[i];
            if (pixel.Alpha < .5 && pixel.Detail.Alpha < .5)
                continue;
            Ink ink = pixel.Row >= 0 ? rows[pixel.Row].Paint(pixel, turn) : pixel.Body;
            Ink color = new Ink(ink.Red, ink.Green, ink.Blue, pixel.Alpha);
            output.Pixels[i] = pixel.Detail.Alpha > .5 ? pixel.Detail.Over(color).Pack() : color.Pack();
        }
    }

    private static void AddToSheet(Raster frame, Raster sheet, int index)
    {
        int left = index % Columns * Size;
        int top = index / Columns * Size;
        for (int y = 0; y < Size; y++)
            Array.Copy(frame.Pixels, y * Size, sheet.Pixels, (top + y) * sheet.Width + left, Size);
    }

    private static void Combine(Raster upper, Raster belt, Raster lower, Raster output)
    {
        for (int i = 0; i < output.Pixels.Length; i++)
        {
            int seam = belt.Pixels[i];
            int shell = upper.Pixels[i] | lower.Pixels[i];
            int alpha = (seam >> 24) & 255;
            if (alpha == 0)
                output.Pixels[i] = shell;
            else if (alpha == 255 || shell == 0)
                output.Pixels[i] = seam;
            else
                output.Pixels[i] = Ink.Unpack(seam).Over(Ink.Unpack(shell)).Pack();
        }
    }

    public static void Render(string sourcePath, string paintedPath, string outputPath, string previewPath, bool samplesOnly)
    {
        Directory.CreateDirectory(outputPath);
        Directory.CreateDirectory(previewPath);
        Raster source = Raster.Open(sourcePath);
        if (source.Width != 1254 || source.Height != 1254)
            throw new ArgumentException("The texture calibration uses the 1254-pixel master.");
        PaintRow[] rows = MakeRows(source);
        Repaint art = new Repaint(Raster.Open(paintedPath));
        PosePixel[][] pose = MapPose(source, art, rows);
        Raster upper = new Raster(Size, Size);
        Raster lower = new Raster(Size, Size);
        Raster upperRest = new Raster(Size, Size);
        Raster lowerRest = new Raster(Size, Size);
        Raster belt = new Raster(Size, Size);
        Raster together = new Raster(Size, Size);
        PaintFrame(pose[Top], rows, upperRest, 0, -1);
        PaintFrame(pose[Bottom], rows, lowerRest, 0, 1);
        PaintFrame(pose[Belt], rows, belt, 0, 0);
        if (samplesOnly)
        {
            foreach (int frame in new[] { 0, 4, 13 })
            {
                PaintFrame(pose[Top], rows, upper, frame, -1);
                PaintFrame(pose[Bottom], rows, lower, frame, 1);
                Combine(upper, belt, lower, together);
                together.Save(Path.Combine(outputPath, String.Format("sample-{0:00}.png", frame)));
            }
            belt.Save(Path.Combine(outputPath, "sample-cinturao.png"));
            return;
        }

        Combine(upperRest, belt, lowerRest, together);
        together.Save(Path.Combine(outputPath, "posicao-inclinada.png"));
        belt.Save(Path.Combine(outputPath, "cinturao-fixo.png"));
        Raster upperSheet = new Raster(Columns * Size, Count / Columns * Size);
        Raster lowerSheet = new Raster(upperSheet.Width, upperSheet.Height);
        Raster combinedSheet = new Raster(upperSheet.Width, upperSheet.Height);
        string combinedFrames = Path.Combine(outputPath, "quadros");
        Directory.CreateDirectory(combinedFrames);
        for (int i = 0; i < Count; i++)
        {
            string file = String.Format("frame-{0:00}.png", i);
            PaintFrame(pose[Top], rows, upper, i, -1);
            PaintFrame(pose[Bottom], rows, lower, i, 1);
            AddToSheet(upper, upperSheet, i);
            AddToSheet(lower, lowerSheet, i);
            Combine(upper, belt, lower, together);
            together.Save(Path.Combine(combinedFrames, file));
            AddToSheet(together, combinedSheet, i);
            Combine(upper, belt, lowerRest, together);
            together.Save(Path.Combine(previewPath, "topo-" + file));
            Combine(upperRest, belt, lower, together);
            together.Save(Path.Combine(previewPath, "base-" + file));
        }
        upperSheet.Save(Path.Combine(outputPath, "spritesheet-topo.png"));
        lowerSheet.Save(Path.Combine(outputPath, "spritesheet-base.png"));
        combinedSheet.Save(Path.Combine(outputPath, "spritesheet.png"));
    }
}
