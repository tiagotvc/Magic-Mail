// Engine of the "Posicionar objetos" part of the area editor: where each object of an upgrade stands on the background,
// the cutting of a sheet of objects into separate PNGs, and the baking of the placed objects into the picture the game
// wants (AreaTask.layer: the size of the background, transparent, furniture already in place) plus its icon.
// Placements are kept in <area art folder>/placements.json so a layer can be adjusted and baked again later; the game
// never reads that file, it only sees the baked PNGs.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoyalAves.Meta;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    [Serializable]
    public class PlacedObject
    {
        public string source;                       // asset path of the cut-out PNG
        public float cx = 0.5f, cy = 0.5f;          // centre, as a fraction of the background (y from the top)
        public float w = 0.3f;                      // width, as a fraction of the background width
    }

    [Serializable]
    public class TaskPlacement
    {
        public string id;
        public List<PlacedObject> objects = new List<PlacedObject>();
    }

    [Serializable]
    public class PlacementFile
    {
        public List<TaskPlacement> tasks = new List<TaskPlacement>();

        public TaskPlacement Get(string taskId)
        {
            var found = tasks.FirstOrDefault(t => t.id == taskId);
            if (found == null) { found = new TaskPlacement { id = taskId }; tasks.Add(found); }
            return found;
        }
    }

    public static class RoyalAvesAreaPlacer
    {
        const string ArtRoot = "Assets/RoyalAves/Art/";
        const int MinObjectPixels = 1200;   // smaller islands are specks, not objects
        const int GapPixels = 2;            // pixels apart that still count as the same object

        // ---- files -------------------------------------------------------------------------------------------------

        public static string ToDisk(string assetPath) =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);

        // The folder of the background picture is the folder of the area.
        public static string AreaFolder(AreaDefinition area)
        {
            if (area == null || area.background == null) return null;
            var dir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(area.background))?.Replace('\\', '/');
            return dir != null && dir.StartsWith(ArtRoot.TrimEnd('/')) ? dir : null;
        }

        public static PlacementFile Load(string folder)
        {
            var path = ToDisk(folder + "/placements.json");
            try { if (File.Exists(path)) return JsonUtility.FromJson<PlacementFile>(File.ReadAllText(path)) ?? new PlacementFile(); }
            catch (Exception e) { Debug.LogWarning("[Áreas] placements.json ilegível: " + e.Message); }
            return new PlacementFile();
        }

        public static void Save(string folder, PlacementFile file)
        {
            File.WriteAllText(ToDisk(folder + "/placements.json"), JsonUtility.ToJson(file, true));
        }

        // Copies a PNG from anywhere on disk into <folder>/objetos and returns its asset path.
        public static string ImportPng(string diskFile, string folder)
        {
            var norm = diskFile.Replace('\\', '/');
            var data = Application.dataPath.Replace('\\', '/');
            if (norm.StartsWith(data)) return "Assets" + norm.Substring(data.Length);
            var dir = folder + "/objetos";
            Directory.CreateDirectory(ToDisk(dir));
            var asset = dir + "/" + Path.GetFileName(diskFile);
            File.Copy(diskFile, ToDisk(asset), true);
            AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceUpdate);
            return asset;
        }

        static Color32[] ReadPixels(string diskFile, out int w, out int h)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                tex.LoadImage(File.ReadAllBytes(diskFile));
                w = tex.width; h = tex.height;
                return tex.GetPixels32();
            }
            finally { UnityEngine.Object.DestroyImmediate(tex); }
        }

        static void WritePng(string diskFile, Color32[] pixels, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            try
            {
                tex.SetPixels32(pixels);
                Directory.CreateDirectory(Path.GetDirectoryName(diskFile));
                File.WriteAllBytes(diskFile, tex.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(tex); }
        }

        // ---- cutting a sheet into objects ------------------------------------------------------------------------

        // Finds the separate objects of a sheet with a transparent background and saves each as <sheet>_<n>.png in
        // <folder>/objetos, left to right. Returns the asset paths.
        // placed: when given, also receives each object ready to use, positioned where it stands in the picture (that is how a
        // layer that is already baked is turned back into objects that can be moved).
        public static List<string> SplitSheet(string sheetDiskFile, string folder, List<PlacedObject> placed = null)
        {
            var px = ReadPixels(sheetDiskFile, out var w, out var h);
            var label = new int[w * h];
            var boxes = new List<int[]>(); // minx, miny, maxx, maxy, count
            var stack = new Stack<int>();
            for (var start = 0; start < px.Length; start++)
            {
                if (px[start].a <= 25 || label[start] != 0) continue;
                var id = boxes.Count + 1;
                var box = new[] { int.MaxValue, int.MaxValue, -1, -1, 0 };
                label[start] = id; stack.Push(start);
                while (stack.Count > 0)
                {
                    var j = stack.Pop();
                    int x = j % w, y = j / w;
                    box[0] = Math.Min(box[0], x); box[1] = Math.Min(box[1], y);
                    box[2] = Math.Max(box[2], x); box[3] = Math.Max(box[3], y); box[4]++;
                    for (var dy = -GapPixels; dy <= GapPixels; dy++)
                    for (var dx = -GapPixels; dx <= GapPixels; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        var k = ny * w + nx;
                        if (px[k].a > 25 && label[k] == 0) { label[k] = id; stack.Push(k); }
                    }
                }
                boxes.Add(box);
            }

            var name = Path.GetFileNameWithoutExtension(sheetDiskFile);
            var result = new List<string>();
            var n = 0;
            foreach (var box in boxes.Select((b, i) => (b, id: i + 1)).Where(t => t.b[4] >= MinObjectPixels).OrderBy(t => t.b[0]))
            {
                int cw = box.b[2] - box.b[0] + 1, ch = box.b[3] - box.b[1] + 1;
                var crop = new Color32[cw * ch];
                for (var y = 0; y < ch; y++)
                for (var x = 0; x < cw; x++)
                {
                    var src = (box.b[1] + y) * w + box.b[0] + x;
                    if (label[src] == box.id) crop[y * cw + x] = px[src]; // other objects' bits stay transparent
                }
                var asset = folder + "/objetos/" + name + "_" + (++n) + ".png";
                WritePng(ToDisk(asset), crop, cw, ch);
                result.Add(asset);
                if (placed != null)
                {
                    float imageW = w, imageH = h;
                    placed.Add(new PlacedObject
                    {
                        source = asset,
                        cx = (box.b[0] + box.b[2] + 1) / 2f / imageW,
                        cy = 1f - (box.b[1] + box.b[3] + 1) / 2f / imageH, // arrays run bottom-up, positions count from the top
                        w = cw / imageW,
                    });
                }
            }
            AssetDatabase.Refresh();
            return result;
        }

        // Everything a picture drew, cropped to its edges, as ONE object standing where it stands in the picture.
        public static PlacedObject WholeObject(string pictureDiskFile, string folder, string name)
        {
            var px = ReadPixels(pictureDiskFile, out var w, out var h);
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (var i = 0; i < px.Length; i++)
            {
                if (px[i].a <= 25) continue;
                int x = i % w, y = i / w;
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
            }
            if (maxX < 0) return null;
            int cw = maxX - minX + 1, ch = maxY - minY + 1;
            var crop = new Color32[cw * ch];
            for (var y = 0; y < ch; y++) Array.Copy(px, (minY + y) * w + minX, crop, y * cw, cw);
            var asset = folder + "/objetos/" + name + "_inteiro.png";
            WritePng(ToDisk(asset), crop, cw, ch);
            AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceUpdate);
            return new PlacedObject
            {
                source = asset,
                cx = (minX + maxX + 1) / 2f / w,
                cy = 1f - (minY + maxY + 1) / 2f / h,
                w = cw / (float)w,
            };
        }

        // ---- baking --------------------------------------------------------------------------------------------------

        // Draws the task's objects, in list order, on a transparent picture the size of the background and saves it as
        // <folder>/<task id>.png (the layer) and <folder>/icone-<task id>.png (the icon). Returns an error message or null.
        public static string Bake(AreaDefinition area, AreaTask task, TaskPlacement placement, out Sprite layer, out Sprite icon)
        {
            layer = icon = null;
            var folder = AreaFolder(area);
            if (folder == null) return "O fundo da área precisa estar dentro de " + ArtRoot + ".";
            if (placement.objects.Count == 0) return "Esta etapa não tem objetos.";
            if (string.IsNullOrWhiteSpace(task.id)) return "A etapa não tem id (ele dá o nome ao arquivo).";

            ReadPixels(ToDisk(AssetDatabase.GetAssetPath(area.background)), out var W, out var H);
            var canvas = new float[W * H * 4]; // premultiplied, rows from the bottom like Unity textures

            foreach (var o in placement.objects)
            {
                if (string.IsNullOrEmpty(o.source) || !File.Exists(ToDisk(o.source))) return "Falta o arquivo do objeto: " + o.source;
                var src = ReadPixels(ToDisk(o.source), out var sw, out var sh);
                var pw = Mathf.Max(1, Mathf.RoundToInt(o.w * W));
                var ph = Mathf.Max(1, Mathf.RoundToInt(pw * (float)sh / sw));
                var scaled = Resample(src, sw, sh, pw, ph);
                var left = Mathf.RoundToInt(o.cx * W - pw / 2f);
                var top = Mathf.RoundToInt(o.cy * H - ph / 2f);
                var bottomRow = H - top - ph; // row of the object's lowest line, counted from the bottom
                for (var y = 0; y < ph; y++)
                {
                    var dy = bottomRow + y;
                    if (dy < 0 || dy >= H) continue;
                    for (var x = 0; x < pw; x++)
                    {
                        var dx = left + x;
                        if (dx < 0 || dx >= W) continue;
                        var s = (y * pw + x) * 4;
                        var d = (dy * W + dx) * 4;
                        var inv = 1f - scaled[s + 3];
                        for (var c = 0; c < 4; c++) canvas[d + c] = scaled[s + c] + canvas[d + c] * inv;
                    }
                }
            }

            var pixels = new Color32[W * H];
            int minX = W, minY = H, maxX = -1, maxY = -1;
            for (var i = 0; i < pixels.Length; i++)
            {
                var a = canvas[i * 4 + 3];
                if (a <= 0f) continue;
                pixels[i] = new Color32(To8(canvas[i * 4] / a), To8(canvas[i * 4 + 1] / a), To8(canvas[i * 4 + 2] / a), To8(a));
                if (a > 0.1f)
                {
                    int x = i % W, y = i / W;
                    minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                }
            }
            if (maxX < 0) return "Os objetos ficaram todos fora do fundo.";

            // Where the furniture is inside the picture: the build-up effect sweeps just this region.
            task.bounds = new Rect(minX / (float)W, minY / (float)H, (maxX - minX + 1) / (float)W, (maxY - minY + 1) / (float)H);

            task.outline = ComputeOutline(pixels, W, H, OutlinePoints);

            var layerAsset = folder + "/" + task.id + ".png";
            var iconAsset = folder + "/icone-" + task.id + ".png";
            WritePng(ToDisk(layerAsset), pixels, W, H);

            // Icon: what the layer drew, cropped and centred in a 256 square.
            int cw = maxX - minX + 1, ch = maxY - minY + 1;
            var crop = new Color32[cw * ch];
            for (var y = 0; y < ch; y++) Array.Copy(pixels, (minY + y) * W + minX, crop, y * cw, cw);
            const int side = 256;
            var room = Mathf.RoundToInt(side * 0.88f);
            var k = Mathf.Min((float)room / cw, (float)room / ch);
            int iw = Mathf.Max(1, Mathf.RoundToInt(cw * k)), ih = Mathf.Max(1, Mathf.RoundToInt(ch * k));
            var small = Resample(crop, cw, ch, iw, ih);
            var square = new Color32[side * side];
            int ox = (side - iw) / 2, oy = (side - ih) / 2;
            for (var y = 0; y < ih; y++)
            for (var x = 0; x < iw; x++)
            {
                var s = (y * iw + x) * 4;
                var a = small[s + 3];
                if (a > 0f) square[(oy + y) * side + ox + x] = new Color32(To8(small[s] / a), To8(small[s + 1] / a), To8(small[s + 2] / a), To8(a));
            }
            WritePng(ToDisk(iconAsset), square, side, side);

            AssetDatabase.ImportAsset(layerAsset, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(iconAsset, ImportAssetOptions.ForceUpdate);
            layer = AssetDatabase.LoadAssetAtPath<Sprite>(layerAsset);
            icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconAsset);
            return layer == null ? "A camada foi gravada, mas não virou sprite (a pasta está em " + ArtRoot + "?)." : null;
        }

        const int OutlinePoints = 96;       // points kept on the silhouette for the burst of the build-up effect
        const int NormalRadius = 14;        // how far around an edge pixel the object's mass is looked at to tell "outwards"

        // Points on the edge of what the layer drew: (x, y) in 0..1 (y from the bottom) and the unit direction pointing out of
        // the object there. Edge pixels are picked evenly by count, so longer edges get proportionally more points.
        static Vector4[] ComputeOutline(Color32[] px, int w, int h, int count)
        {
            bool Solid(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && px[y * w + x].a > 76;
            var edge = new List<int>();
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                if (Solid(x, y) && (!Solid(x - 1, y) || !Solid(x + 1, y) || !Solid(x, y - 1) || !Solid(x, y + 1)))
                    edge.Add(y * w + x);
            if (edge.Count == 0) return null;

            var points = new Vector4[Mathf.Min(count, edge.Count)];
            for (var k = 0; k < points.Length; k++)
            {
                var i = edge[(int)((k + 0.5f) / points.Length * edge.Count)];
                int x = i % w, y = i / w;
                float sx = 0, sy = 0;
                for (var dy = -NormalRadius; dy <= NormalRadius; dy += 2)
                for (var dx = -NormalRadius; dx <= NormalRadius; dx += 2)
                    if (Solid(x + dx, y + dy)) { sx += dx; sy += dy; }
                var n = new Vector2(-sx, -sy);               // away from the mass of the object
                n = n.sqrMagnitude < 1e-3f ? Vector2.up : n.normalized;
                points[k] = new Vector4(x / (float)w, y / (float)h, n.x, n.y);
            }
            return points;
        }

        static byte To8(float v) => (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);

        // Premultiplied RGBA floats. Shrinking averages every source pixel under each target pixel (no jagged edges
        // and no dark halos); enlarging is bilinear.
        static float[] Resample(Color32[] src, int sw, int sh, int tw, int th)
        {
            var dst = new float[tw * th * 4];
            if (sw >= tw && sh >= th)
            {
                float fx = (float)sw / tw, fy = (float)sh / th;
                for (var ty = 0; ty < th; ty++)
                {
                    float y0 = ty * fy, y1 = y0 + fy;
                    for (var tx = 0; tx < tw; tx++)
                    {
                        float x0 = tx * fx, x1 = x0 + fx;
                        float r = 0, g = 0, b = 0, a = 0, total = 0;
                        for (var iy = (int)y0; iy < Mathf.Min(sh, Mathf.CeilToInt(y1)); iy++)
                        {
                            var wy = Mathf.Min(iy + 1, y1) - Mathf.Max(iy, y0);
                            for (var ix = (int)x0; ix < Mathf.Min(sw, Mathf.CeilToInt(x1)); ix++)
                            {
                                var wgt = wy * (Mathf.Min(ix + 1, x1) - Mathf.Max(ix, x0));
                                var p = src[iy * sw + ix];
                                var pa = p.a / 255f;
                                r += p.r / 255f * pa * wgt; g += p.g / 255f * pa * wgt; b += p.b / 255f * pa * wgt;
                                a += pa * wgt; total += wgt;
                            }
                        }
                        if (total <= 0) continue;
                        var o = (ty * tw + tx) * 4;
                        dst[o] = r / total; dst[o + 1] = g / total; dst[o + 2] = b / total; dst[o + 3] = a / total;
                    }
                }
                return dst;
            }
            for (var ty = 0; ty < th; ty++)
            {
                var sy = Mathf.Clamp((ty + 0.5f) * sh / th - 0.5f, 0, sh - 1);
                int y0 = (int)sy, y1 = Mathf.Min(sh - 1, y0 + 1);
                var wy = sy - y0;
                for (var tx = 0; tx < tw; tx++)
                {
                    var sx = Mathf.Clamp((tx + 0.5f) * sw / tw - 0.5f, 0, sw - 1);
                    int x0 = (int)sx, x1 = Mathf.Min(sw - 1, x0 + 1);
                    var wx = sx - x0;
                    var o = (ty * tw + tx) * 4;
                    Accumulate(dst, o, src[y0 * sw + x0], (1 - wx) * (1 - wy));
                    Accumulate(dst, o, src[y0 * sw + x1], wx * (1 - wy));
                    Accumulate(dst, o, src[y1 * sw + x0], (1 - wx) * wy);
                    Accumulate(dst, o, src[y1 * sw + x1], wx * wy);
                }
            }
            return dst;
        }

        static void Accumulate(float[] dst, int o, Color32 p, float weight)
        {
            var a = p.a / 255f;
            dst[o] += p.r / 255f * a * weight; dst[o + 1] += p.g / 255f * a * weight;
            dst[o + 2] += p.b / 255f * a * weight; dst[o + 3] += a * weight;
        }
    }
}
