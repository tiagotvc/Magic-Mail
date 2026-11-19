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

    private static Raster[] MakeJewels(Raster source)
    {
        Raster ruby = ExtractJewel(source, 221, 676, 315, 791);
        Raster amber = ExtractJewel(source, 444, 697, 558, 809);
        Raster emerald = ExtractJewel(source, 707, 707, 811, 812);
        Raster sapphire = ExtractJewel(source, 938, 680, 1038, 782);
        Raster magenta = ExtractJewel(source, 520, 190, 734, 221);
        return new[] { emerald, sapphire, magenta, ruby, amber, emerald, sapphire, magenta, ruby, sapphire, ruby, amber };
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

    private static Ink PaintBelt(double latitude, double longitude, double light)
    {
        double upperLip = Smooth(-.287, -.245, latitude) * (1 - Smooth(-.238, -.180, latitude));
        double lowerLip = Smooth(.270, .325, latitude) * (1 - Smooth(.330, .375, latitude));
        double front = .88 + .12 * Math.Cos(longitude + .32);
        Ink body = Ink.Blend(new Ink(118, 36, 181), new Ink(55, 8, 122), Smooth(-.17, .348, latitude));
        body = Ink.Blend(body, new Ink(220, 98, 255), upperLip * front);
        body = Ink.Blend(body, new Ink(131, 53, 214), lowerLip * .80);
        return body.Darken(light * front);
    }

    private static Ink RaisedCrown(Raster source, double x, double y)
    {
        if (x < 461 || x > 791 || y > 239)
            return new Ink(0, 0, 0, 0);
        // Raise the center stone around its lower edge so it still rests in the same seat.
        double sourceY = 237 + (y - 237) / 1.31;
        double u = (x - 626) / 164;
        double v = (sourceY - 196) / 43;
        double outline = 1 - Smooth(.96, 1.055, u * u + v * v);
        Ink stone = source.Sample(x, sourceY);
        double magenta = Smooth(-2, 50, stone.Red - stone.Blue) * Smooth(93, 150, stone.Red);
        double edge = 1 - Smooth(231, 239, y);
        return new Ink(stone.Red, stone.Green, stone.Blue, stone.Alpha * outline * magenta * edge);
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

    private static PosePixel[][] MapPose(Raster source, PaintRow[] rows)
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
                    Ink raised = RaisedCrown(source, sx, sy);
                    if (raised.Alpha > .5)
                    {
                        Ink underlying = new Ink(shell.Body.Red, shell.Body.Green, shell.Body.Blue, shell.Alpha);
                        shell.Body = raised.Over(underlying);
                        shell.Alpha = shell.Body.Alpha;
                    }
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
                    belt.Body = PaintBelt(belt.Latitude, belt.Longitude, belt.FaceLight);
                    result[Belt][index] = belt;
                }
            }
        }
        return result;
    }

    private static void PaintFrame(PosePixel[] pose, PaintRow[] rows, Raster output, int frame, int direction,
        Raster[] jewels = null)
    {
        double turn = direction * Tau * Wrap(frame, Count) / Count;
        for (int i = 0; i < pose.Length; i++)
        {
            PosePixel pixel = pose[i];
            if (pixel.Alpha < .5)
                continue;
            Ink ink = pixel.Row >= 0 ? rows[pixel.Row].Paint(pixel, turn) : pixel.Body;
            output.Pixels[i] = new Ink(ink.Red, ink.Green, ink.Blue, pixel.Alpha).Pack();
        }
        if (jewels != null)
            PaintJewels(output, jewels, turn);
    }

    private static void PaintJewels(Raster belt, Raster[] jewels, double turn)
    {
        const double latitude = .062;
        double radius = Math.Sqrt(1 - latitude * latitude);
        double tiltX = Math.Cos(Tilt);
        double tiltY = Math.Sin(Tilt);
        for (int index = 0; index < jewels.Length; index++)
        {
            double angle = (index + .5) * Tau / jewels.Length + turn;
            double facing = Math.Cos(angle);
            if (facing < -.06)
                continue;
            double foreshorten = Math.Max(0, facing);
            double y = latitude * Math.Cos(Pitch) + radius * facing * Math.Sin(Pitch);
            double x = Width * radius * Math.Sin(angle) * BeltWidth(y * Math.Cos(Pitch)) * .285;
            y *= Height * .340;
            double cx = Size / 2.0 + x * tiltX - y * tiltY;
            double cy = Size / 2.0 + x * tiltY + y * tiltX;
            double width = 59 * (.19 + .81 * foreshorten);
            double height = 59 * (.93 + .07 * foreshorten);
            double opacity = Smooth(-.06, .15, facing);
            double light = .78 + .22 * foreshorten;
            PaintJewel(belt, jewels[index], cx, cy, width, height, tiltX, tiltY, opacity, light);
        }
    }

    private static void PaintJewel(Raster belt, Raster art, double cx, double cy, double width, double height,
        double tiltX, double tiltY, double opacity, double light)
    {
        const int area = 43;
        int x0 = Math.Max(0, (int)Math.Floor(cx - area));
        int y0 = Math.Max(0, (int)Math.Floor(cy - area));
        int x1 = Math.Min(Size - 1, (int)Math.Ceiling(cx + area));
        int y1 = Math.Min(Size - 1, (int)Math.Ceiling(cy + area));
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                int index = y * Size + x;
                Ink substrate = Ink.Unpack(belt.Pixels[index]);
                if (substrate.Alpha < 2)
                    continue;

                // Setting and stone are concentric 2D shapes. Moving around the cuff only narrows
                // their shared width; it does not bend their edges or change their tilt.
                double dx = x + .5 - cx;
                double dy = y + .5 - cy;
                double u = (dx * tiltX + dy * tiltY) / width + .5;
                double v = (-dx * tiltY + dy * tiltX) / height + .5;
                double shadow = 1 - Smooth(-.004, .052, RoundedBox(u - .026, v - .047, .493, .493, .146));
                double wall = 1 - Smooth(-.010, .014, RoundedBox(u - .015, v - .028, .477, .477, .136));
                if (shadow <= 0 && wall <= 0)
                    continue;
                Ink paint = Ink.Blend(substrate, substrate.Darken(.46), shadow * opacity * .81);
                paint = Ink.Blend(paint, new Ink(145, 69, 3).Darken(light), wall * opacity);

                double outer = RoundedBox(u, v, .478, .478, .136);
                double faceAlpha = 1 - Smooth(-.011, .011, outer);
                if (faceAlpha > 0)
                {
                    double goldTone = Smooth(.06, .96, 1 - .60 * v - .40 * u);
                    Ink gold = Ink.Blend(new Ink(200, 103, 3), new Ink(255, 232, 111), goldTone);
                    double bevel = (1 - Smooth(.024, .066, -outer)) * Smooth(.26, .81, goldTone);
                    gold = Ink.Blend(gold, new Ink(255, 244, 168), bevel * .78);
                    double inner = RoundedBox(u, v, .366, .366, .112);
                    gold = Ink.Blend(gold, new Ink(128, 47, 4), (1 - Smooth(.009, .033, inner)) * .83);
                    if (inner < .012)
                    {
                        double sx = (u - .134) / .732 * (art.Width - 1);
                        double sy = (v - .134) / .732 * (art.Height - 1);
                        Ink gem = art.Sample(sx, sy);
                        gold = Ink.Blend(gold, gem, 1 - Smooth(-.012, .012, inner));
                    }
                    paint = Ink.Blend(paint, gold.Darken(light), faceAlpha * opacity);
                }
                belt.Pixels[index] = new Ink(paint.Red, paint.Green, paint.Blue, substrate.Alpha).Pack();
            }
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

    public static void Render(string sourcePath, string outputPath, string previewPath, bool samplesOnly)
    {
        Directory.CreateDirectory(outputPath);
        Directory.CreateDirectory(previewPath);
        Raster source = Raster.Open(sourcePath);
        if (source.Width != 1254 || source.Height != 1254)
            throw new ArgumentException("The texture calibration uses the 1254-pixel master.");
        PaintRow[] rows = MakeRows(source);
        Raster[] jewels = MakeJewels(source);
        PosePixel[][] pose = MapPose(source, rows);
        Raster[] neutral = { new Raster(Size, Size), new Raster(Size, Size), new Raster(Size, Size) };
        Raster[] active = { new Raster(Size, Size), new Raster(Size, Size), new Raster(Size, Size) };
        int[] directions = { -1, 1, 1 };
        Raster together = new Raster(Size, Size);
        for (int part = Top; part <= Bottom; part++)
            PaintFrame(pose[part], rows, neutral[part], 0, directions[part], part == Belt ? jewels : null);
        if (samplesOnly)
        {
            foreach (int frame in new[] { 0, 2, 4, 7, 13 })
            {
                PaintFrame(pose[Top], rows, active[Top], frame, directions[Top]);
                PaintFrame(pose[Bottom], rows, active[Bottom], frame, directions[Bottom]);
                Combine(active[Top], neutral[Belt], active[Bottom], together);
                together.Save(Path.Combine(outputPath, String.Format("sample-{0:00}.png", frame)));
            }
            foreach (int frame in new[] { 1, 4, 7, 13 })
            {
                PaintFrame(pose[Belt], rows, active[Belt], frame, directions[Belt], jewels);
                Combine(neutral[Top], active[Belt], neutral[Bottom], together);
                together.Save(Path.Combine(outputPath, String.Format("sample-cinta-{0:00}.png", frame)));
            }
            return;
        }

        Combine(neutral[Top], neutral[Belt], neutral[Bottom], together);
        together.Save(Path.Combine(outputPath, "posicao-inclinada.png"));
        Raster[] sheets = {
            new Raster(Columns * Size, Count / Columns * Size),
            new Raster(Columns * Size, Count / Columns * Size),
            new Raster(Columns * Size, Count / Columns * Size)
        };
        Raster combinedSheet = new Raster(sheets[Top].Width, sheets[Top].Height);
        string combinedFrames = Path.Combine(outputPath, "quadros");
        Directory.CreateDirectory(combinedFrames);
        for (int i = 0; i < Count; i++)
        {
            string file = String.Format("frame-{0:00}.png", i);
            for (int part = Top; part <= Bottom; part++)
            {
                PaintFrame(pose[part], rows, active[part], i, directions[part], part == Belt ? jewels : null);
                AddToSheet(active[part], sheets[part], i);
            }
            Combine(active[Top], neutral[Belt], active[Bottom], together);
            together.Save(Path.Combine(combinedFrames, file));
            AddToSheet(together, combinedSheet, i);
            Combine(active[Top], neutral[Belt], neutral[Bottom], together);
            together.Save(Path.Combine(previewPath, "topo-" + file));
            Combine(neutral[Top], active[Belt], neutral[Bottom], together);
            together.Save(Path.Combine(previewPath, "cinta-" + file));
            Combine(neutral[Top], neutral[Belt], active[Bottom], together);
            together.Save(Path.Combine(previewPath, "base-" + file));
        }
        sheets[Top].Save(Path.Combine(outputPath, "spritesheet-topo.png"));
        sheets[Belt].Save(Path.Combine(outputPath, "spritesheet-cinta.png"));
        sheets[Bottom].Save(Path.Combine(outputPath, "spritesheet-base.png"));
        combinedSheet.Save(Path.Combine(outputPath, "spritesheet.png"));
    }
}
