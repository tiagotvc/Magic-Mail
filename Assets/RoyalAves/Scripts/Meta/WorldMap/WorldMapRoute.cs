using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Meta.WorldMap
{
    public struct MapPalette
    {
        public Color outerA, outerB, pathA, pathB, rim, face, water, haze, tileA, tileB;

        public static MapPalette Lerp(MapPalette a, MapPalette b, float t) => new MapPalette
        {
            outerA = Color.Lerp(a.outerA, b.outerA, t), outerB = Color.Lerp(a.outerB, b.outerB, t),
            pathA = Color.Lerp(a.pathA, b.pathA, t), pathB = Color.Lerp(a.pathB, b.pathB, t),
            rim = Color.Lerp(a.rim, b.rim, t), face = Color.Lerp(a.face, b.face, t),
            water = Color.Lerp(a.water, b.water, t), haze = Color.Lerp(a.haze, b.haze, t),
            tileA = Color.Lerp(a.tileA, b.tileA, t), tileB = Color.Lerp(a.tileB, b.tileB, t)
        };
    }

    // Rota autorada: posicoes dos niveis em metros, lateral por padroes desenhados por capitulo,
    // canais (lacunas de terreno) com ponte, e paletas por capitulo. Dados puros, sem Unity runtime.
    public sealed class WorldMapRoute
    {
        public struct Gap
        {
            public float start, end;
            public int afterLevel;
        }

        public const int Positions = WorldMapEditorial.Positions;
        public const float Spacing = 1.65f;
        public const float BridgeExtra = 3.45f;
        public const float LandMargin = 1.6f;
        public const float StartPad = 8f;
        public const float EndPad = 14f;

        // Fases depois das quais ha um canal com ponte (editorial: primeira travessia, canal, ponte de troncos...).
        static readonly int[] BridgeAfter =
        {
            9, 18, 27, 38, 44, 52, 58, 62, 64, 67, 69, 72, 74, 77, 79, 92, 108, 119, 124, 131, 138, 150, 163, 175, 190
        };

        static readonly MapPalette[] Palettes =
        {
            // Bairro: verde, creme, roxo
            Pal("#68B78A", "#5DAE81", "#B9E69E", "#ADDF96", "#96D09B", "#4D996E", "#49BFD1", "#DDEED7", "#DDCAE9", "#CCB9DF"),
            // Vila dos Selos: menta, amarelo suave, roxo
            Pal("#8DD6AA", "#7ECB9E", "#F6E7A8", "#EBD992", "#B2E8C4", "#3E9C79", "#4DBAD2", "#E6F6E6", "#B58CE6", "#9C70D6"),
            // Bosque: menta escura, lilas, creme
            Pal("#52A383", "#489876", "#E1C08F", "#D4B07C", "#74BC98", "#2C6F5A", "#3FA9B8", "#D6EBDD", "#C7A6EA", "#AD88DA"),
            // Canal: azul agua, menta, ouro
            Pal("#80D5C2", "#72C9B5", "#F3D88F", "#E8C97A", "#A8E8D8", "#2F9A95", "#35B5D6", "#DDF3F6", "#C79B4E", "#B58A3C"),
            // Oficina: turquesa, violeta, creme
            Pal("#5EC8C6", "#52BDBB", "#B195E6", "#A082DA", "#8DE2DE", "#2C8E99", "#3EB4CC", "#DDF1F3", "#C9B0F0", "#B198E6"),
            // Campo das Pipas: verde claro, palha, roxo
            Pal("#A8DC6C", "#9AD05E", "#EDD99B", "#E2CB88", "#C4EA8E", "#5A9E3E", "#4DB8D0", "#F0F5D8", "#B58CE6", "#9C70D6"),
            // Porto: areia, tabuas, mar azul
            Pal("#F2DFA8", "#E8D394", "#DDAA70", "#CF9B60", "#F8EBC0", "#B6844F", "#34A6E0", "#E2F1F7", "#C79B4E", "#B58A3C"),
            // Serra: pedra clara, ocre, azul petroleo
            Pal("#D9B97F", "#CEAD72", "#EADFC8", "#DDD0B4", "#EBD2A0", "#8A7050", "#4A93A8", "#EDE6D6", "#B58CE6", "#9C70D6"),
            // Jardins: lima, lavanda, ouro
            Pal("#B9E46E", "#ACD960", "#D9C9F1", "#CDBBE8", "#D3F190", "#6AA64A", "#4DBBD8", "#F1F5DE", "#F2CA66", "#E2B650"),
            // Capital: creme, roxo real, ouro
            Pal("#F2E6C9", "#E9DBBC", "#E7DBF5", "#DACCEE", "#FFDC82", "#9A7FC8", "#4DB0D8", "#F6F0E4", "#8A5BD0", "#7547BE")
        };
        public static MapPalette LavaPalette => Pal("#803F49","#713640","#FFC58E","#F7A76F","#E8A36C","#532934","#F77A36","#F4BD91","#D5A18C","#BE8778");

        readonly float[] pos;
        public int LevelSlots { get; }
        public WorldMapIslands Islands { get; }
        public float Curvature=>Islands!=null?Islands.lateralCurvature:1;
        readonly List<Gap> gaps = new List<Gap>();
        public float Length { get; private set; }
        public IReadOnlyList<Gap> Gaps => gaps;

        public WorldMapRoute(WorldMapIslands islands=null,int levelCount=Positions)
        {
            Islands=islands!=null&&islands.islands.Count>0?islands:null;
            LevelSlots=Mathf.Max(1,Islands!=null?Mathf.Max(Islands.Capacity,levelCount):levelCount);
            pos=new float[LevelSlots+2];
            var bridge = Islands==null?new HashSet<int>(BridgeAfter):new HashSet<int>();
            if(Islands!=null)
                for(var i=1;i<Islands.islands.Count;i++)bridge.Add(Islands.FirstLevel(i)-1);
            pos[1] = StartPad;
            for (var i = 2; i <= LevelSlots; i++)
            {
                var extra=bridge.Contains(i-1)?BridgeExtra:0;
                if(extra>0&&Islands!=null)extra=Mathf.Clamp(Islands.AtLevel(i-1).channelWidth,.9f,3.5f)+LandMargin*2-Spacing;
                pos[i]=pos[i-1]+Spacing+extra;
            }
            pos[0] = pos[1] - Spacing;
            pos[LevelSlots + 1] = pos[LevelSlots] + Spacing;
            Length = pos[LevelSlots] + EndPad;
            var ordered=new List<int>(bridge);ordered.Sort();
            foreach (var k in ordered)
            {
                if (k < 1 || k >= LevelSlots) continue;
                gaps.Add(new Gap { start = pos[k] + LandMargin, end = pos[k + 1] - LandMargin, afterLevel = k });
            }
        }

        public float Pos(int level) => pos[Mathf.Clamp(level, 0, LevelSlots + 1)];
        public int IslandIndexAt(float meters)=>Islands!=null?Islands.IndexAtLevel(LevelAt(meters)+.5f):ChapterAtMeters(meters);
        public WorldMapIslands.Island IslandAt(float meters)=>Islands!=null?Islands.islands[IslandIndexAt(meters)]:null;
        public bool LavaAt(float meters,bool fallback=false)=>Islands?.unifiedBase==true
            ?IslandAt(meters)?.groundStyle==WorldMapGroundStyle.Volcanic
            :IslandAt(meters)?.terrain==WorldMapTerrainTheme.LavaLand||(Islands==null&&fallback);

        // Posicao em metros para um cursor em niveis fracionarios.
        public float MetersAt(float levelCursor)
        {
            if (levelCursor <= 1) return pos[1] + (levelCursor - 1) * Spacing;
            if (levelCursor >= LevelSlots) return pos[LevelSlots] + (levelCursor - LevelSlots) * Spacing;
            var i = Mathf.FloorToInt(levelCursor);
            return Mathf.Lerp(pos[i], pos[i + 1], levelCursor - i);
        }

        public float LevelAt(float meters)
        {
            if (meters <= pos[1]) return 1 + (meters - pos[1]) / Spacing;
            if (meters >= pos[LevelSlots]) return LevelSlots + (meters - pos[LevelSlots]) / Spacing;
            var lo = 1;
            var hi = LevelSlots;
            while (hi - lo > 1)
            {
                var mid = (lo + hi) >> 1;
                if (pos[mid] <= meters) lo = mid;
                else hi = mid;
            }
            return lo + (meters - pos[lo]) / (pos[lo + 1] - pos[lo]);
        }

        public float CenterX(float meters)
        {
            // One continuous wave, without per-level corners or chapter resets. Ease the phase to
            // a stop at each crossing so the bridge remains straight and joins both banks smoothly.
            const float ease=.65f;
            float IntegratedRamp(float distance)
            {
                if(distance<=0)return 0;
                if(distance>=ease)return distance-ease*.5f;
                var u=distance/ease;
                return ease*(u*u*u-.5f*u*u*u*u);
            }
            var travel=meters-StartPad;
            foreach(var gap in gaps)
            {
                if(meters<=gap.start-ease)break;
                travel-=meters>=gap.end+ease?gap.end-gap.start+ease:
                    IntegratedRamp(meters-gap.start+ease)-IntegratedRamp(meters-gap.end);
            }
            var amplitude=Islands?.unifiedBase==true?.36f:.30f;
            return amplitude*Mathf.Sin(travel*(Mathf.PI*2/9.6f));
        }

        public bool InGap(float meters, float pad = 0)
        {
            for (var i = 0; i < gaps.Count; i++)
                if (meters > gaps[i].start - pad && meters < gaps[i].end + pad) return true;
            return false;
        }
        public WorldMapIslands.Island CrossingAt(Gap gap)=>Islands?.AtLevel(gap.afterLevel);
        public float WaterHeight(float meters)
        {
            foreach(var gap in gaps)if(meters>=gap.start-.7f&&meters<=gap.end+.7f)
                return -Mathf.Clamp(CrossingAt(gap)?.channelDepth??.23f,.08f,.55f);
            return WorldMapTerrain.WaterY;
        }

        // Painted path ends in rounded green landings before a crossing; open lawns separate runs.
        public float DistanceFromLaneEnd(float meters)
        {
            var distance = Islands?.unifiedBase==true?meters-(Pos(1)-1.4f):1000f;
            foreach (var gap in gaps)
                distance = Mathf.Min(distance, meters <= gap.start
                    ? gap.start - 1f - meters : meters - gap.end - 1f);
            return distance;
        }

        public float InsetAmount(float meters)
        {
            var inset = 1f;
            // Squared width of an elliptical end cap. SmoothStep followed by sqrt made long
            // triangular funnels; a circle opens quickly at the tip and joins the full lane smoothly.
            float Cap(float distance, float empty, float length)
            {
                var t = Mathf.Clamp01((distance - empty) / length);
                return t * (2 - t);
            }
            if(Islands?.unifiedBase==true)inset=Cap(meters,Pos(1)-1.4f,.6f);
            foreach (var gap in gaps)
            {
                if (meters <= gap.start) inset = Mathf.Min(inset, Cap(gap.start - meters, 1f, .6f));
                else if (meters >= gap.end) inset = Mathf.Min(inset, Cap(meters - gap.end, 1f, .6f));
            }
            return inset;
        }

        public static int ChapterOf(int level) =>
            Mathf.Clamp((level - 1) / WorldMapEditorial.LevelsPerChapter, 0, WorldMapEditorial.ChapterTitles.Length - 1);

        public int ChapterAtMeters(float meters) =>
            Mathf.Max(0,(Mathf.RoundToInt(LevelAt(meters))-1)/WorldMapEditorial.LevelsPerChapter);

        // Mistura suave (12 m) na fronteira entre capitulos: cores da proxima regiao aparecem antes da transicao.
        public MapPalette PaletteAt(float meters)
        {
            if(Islands!=null){var island=IslandAt(meters);return Islands.unifiedBase?WorldMapIslands.IllustratedPalette(island):island.Palette;}
            var per = WorldMapEditorial.LevelsPerChapter;
            var level = LevelAt(meters);
            var b = Mathf.RoundToInt((level - 0.5f) / per); // fronteiras ja passadas (mais proxima)
            if (b <= 0) return Palettes[0];
            if (b >= Palettes.Length) return Palettes[Palettes.Length - 1];
            var last = b * per; // ultima fase do capitulo anterior
            var edge = (Pos(last) + Pos(last + 1)) * 0.5f;
            var t = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(edge - 6, edge + 6, meters));
            return MapPalette.Lerp(Palettes[b - 1], Palettes[b], t);
        }

        public static MapPalette PaletteOfChapter(int chapter) => Palettes[Mathf.Clamp(chapter, 0, Palettes.Length - 1)];
        public static MapPalette BasePalette(bool mint)=>Pal("#579B46","#478A40",mint?"#ACDEC0":"#F8D49B",mint?"#97CFB3":"#F1C58F","#94BF60","#417D3E","#21AFE1","#E8EFBD","#D6E4B2","#C3D4A0");

        static MapPalette Pal(string outerA, string outerB, string pathA, string pathB, string rim, string face,
            string water, string haze, string tileA, string tileB) => new MapPalette
        {
            outerA = C(outerA), outerB = C(outerB), pathA = C(pathA), pathB = C(pathB), rim = C(rim), face = C(face),
            water = C(water), haze = C(haze), tileA = C(tileA), tileB = C(tileB)
        };

        static Color C(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
    }
}
