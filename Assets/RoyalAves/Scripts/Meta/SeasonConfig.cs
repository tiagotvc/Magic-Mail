// Seasons of Correio Mágico. Every screen has a BASE theme (its normal look, shared by all seasons); a season only adds to
// it — a name, props placed on the screen — or replaces a piece on purpose (its own art, its own positions).
// Edit all of it in the Level editor > Temporadas tab (Royal Aves > Editor de temporadas), which keeps this asset:
// Assets/RoyalAves/Resources/RoyalAvesSeasons.asset. The game reads the active season when a screen opens, so a new season
// never needs a code change.
//
// Screens wired so far: Loading (the first screen of the app). SeasonProps is the component any screen puts in to show the
// props of the active season, so the other screens only need to host it.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoyalAves.Meta
{
    /// The screens a season can dress. Each one needs a SeasonProps host to show props (see SeasonProps).
    public enum SeasonScreen { Loading = 0, Title = 1, Lobby = 2, Game = 3 }

    /// How a prop moves, if it does (see SeasonPropMotion).
    public enum PropMotion { None = 0, Float = 1, Pulse = 2, Sway = 3, Spin = 4 }

    [CreateAssetMenu(fileName = "RoyalAvesSeasons", menuName = "Royal Aves/Temporadas")]
    public class SeasonConfig : ScriptableObject
    {
        public const string ResourcePath = "RoyalAvesSeasons";

        [Tooltip("Id da temporada em uso: é a que as telas mostram.")]
        public string activeSeasonId;
        [Tooltip("O visual normal da tela de carregamento, que toda temporada herda.")]
        public LoadingLook baseLoading = new LoadingLook();
        [HideInInspector] public bool baseMigrated;      // the base was built from the first season (see the editor)
        public List<Season> seasons = new List<Season>();

        static SeasonConfig instance;

        public static SeasonConfig Instance =>
            instance != null ? instance : instance = Resources.Load<SeasonConfig>(ResourcePath);

        /// The season in use; the first one when the id is missing or unknown.
        public Season Active
        {
            get
            {
                var found = seasons.FirstOrDefault(s => s.id == activeSeasonId);
                return found ?? (seasons.Count > 0 ? seasons[0] : null);
            }
        }

        /// What the loading screen really shows for a season: the base theme, with the season's own art and/or positions
        /// laid over it when it has them.
        public LoadingLook ResolveLoading(Season season)
        {
            var look = baseLoading.Copy();
            if (season == null) return look;
            if (season.customArt && season.loadingArt != null)
            {
                look.art = season.loadingArt;
                look.zoom = season.artZoom;
                look.offset = season.artOffset;
                look.showLogo = season.showLogo;
                look.bottomShade = season.bottomShade;
            }
            if (season.ownLayout)
            {
                look.title = season.titleLayout.Copy();
                look.subtitle = season.subtitleLayout.Copy();
                look.bar = season.barLayout.Copy();
                look.label = season.labelLayout.Copy();
            }
            return look;
        }
    }

    /// Where one element of a screen stands and how big it is. position is in units of the 1080-wide screen, measured from
    /// the middle of the bottom edge (x to the right, y up); scale multiplies its size.
    [Serializable]
    public class ElementLayout
    {
        public Vector2 position;
        [Min(0.1f)] public float scale = 1f;

        public ElementLayout() { }
        public ElementLayout(float x, float y) { position = new Vector2(x, y); }
        public ElementLayout Copy() => new ElementLayout { position = position, scale = scale };
    }

    /// The look of the loading screen: its art and how it is framed, whether the separate logo and the shade show, and where
    /// the name, the subtitle, the bar and "Carregando..." stand. This is both the base theme and, resolved, what is shown.
    [Serializable]
    public class LoadingLook
    {
        public Sprite art;
        [Range(1f, 2.5f)] public float zoom = 1f;
        public Vector2 offset;
        public bool showLogo = true;
        public bool bottomShade = true;
        public ElementLayout title = new ElementLayout(0f, 325f);
        public ElementLayout subtitle = new ElementLayout(0f, 250f);
        public ElementLayout bar = new ElementLayout(0f, 175f);
        public ElementLayout label = new ElementLayout(0f, 85f);

        public LoadingLook Copy() => new LoadingLook
        {
            art = art, zoom = zoom, offset = offset, showLogo = showLogo, bottomShade = bottomShade,
            title = title.Copy(), subtitle = subtitle.Copy(), bar = bar.Copy(), label = label.Copy(),
        };
    }

    [Serializable]
    public class Season
    {
        public string id;
        public MapSeasonLook map = new MapSeasonLook();
        [Tooltip("Nome grande, embaixo na tela de carregamento (ex.: Polo Norte). Aparece em maiúsculas.")]
        public string title;
        [Tooltip("Linha menor logo abaixo do nome (ex.: Uma entrega especial de Natal).")]
        public string subtitle;

        [Tooltip("Troca a arte do tema base por uma própria desta temporada (com seu enquadramento e opções).")]
        public bool customArt;
        [Tooltip("Arte de tela cheia, em proporção de celular (9:16). Só vale com 'Arte própria'.")]
        public Sprite loadingArt;
        [Tooltip("Escurece a base da arte para o nome e a barra aparecerem. Só vale com 'Arte própria'.")]
        public bool bottomShade = true;
        [Tooltip("Mostra o logo Magic Mail separado por cima da arte. Só vale com 'Arte própria'.")]
        public bool showLogo = true;
        [Tooltip("Aproximação da arte (1 = cobre a tela exatamente). Só vale com 'Arte própria'.")]
        [Range(1f, 2.5f)] public float artZoom = 1f;
        [Tooltip("Desloca a arte, em unidades de tela. Só vale com 'Arte própria'.")]
        public Vector2 artOffset;

        [Tooltip("Usa posições e tamanhos próprios desta temporada em vez dos do tema base.")]
        public bool ownLayout;
        public ElementLayout titleLayout = new ElementLayout(0f, 325f);
        public ElementLayout subtitleLayout = new ElementLayout(0f, 250f);
        public ElementLayout barLayout = new ElementLayout(0f, 175f);
        public ElementLayout labelLayout = new ElementLayout(0f, 85f);

        [Tooltip("Adereços desta temporada, de todas as telas (cada um diz em qual tela aparece).")]
        public List<SeasonProp> props = new List<SeasonProp>();
    }

    // Decorations are separate cards over the original scenery, shared by every biome.
    [Serializable] public class MapSeasonLook
    {
        [InspectorName("Neve sobre os objetos")] public bool snow;
        [InspectorName("Cobertura de neve no chão"),Range(0,1)] public float snowCoverage=.72f;
        [Tooltip("Optional full ground material atlas for this season; scenery remains unchanged.")]
        [InspectorName("Atlas de chão da season")] public Sprite groundMaterials;
        [InspectorName("Decorações por categoria")] public List<MapSeasonOverlay> overlays=new List<MapSeasonOverlay>();
    }
    [Serializable] public class MapSeasonOverlay
    {
        [InspectorName("Categoria do objeto")] public WorldMap.PropKind category=WorldMap.PropKind.Tree;
        [InspectorName("Sprite sobreposto")] public Sprite sprite;
        [Tooltip("Position relative to the base sprite's height.")]
        [InspectorName("Posição relativa")] public Vector2 position=new Vector2(0,.75f);
        [InspectorName("Altura relativa"),Range(.03f,1)] public float relativeHeight=.24f;
    }

    /// A decoration of a season placed on a screen: a picture with a place, a size, a turn, a side (over the screen's own
    /// elements or behind them), a tint and, if wanted, a movement.
    [Serializable]
    public class SeasonProp
    {
        public string name = "Adereço";
        public SeasonScreen screen = SeasonScreen.Loading;
        public bool visible = true;
        public Sprite sprite;
        [Tooltip("Posição em unidades da tela (1080 de largura), a partir do meio da base: X para a direita, Y para cima.")]
        public Vector2 position = new Vector2(0f, 600f);
        [Min(0.05f)] public float scale = 1f;
        [Tooltip("Rotação em graus.")]
        public float rotation;
        [Tooltip("Por cima dos elementos da tela, ou por trás deles (logo acima da arte).")]
        public bool inFront = true;
        public Color tint = Color.white;
        public PropMotion motion = PropMotion.None;
        [Range(0f, 5f)] public float motionAmount = 1f;
        [Range(0.1f, 5f)] public float motionSpeed = 1f;

        public SeasonProp Copy() => (SeasonProp)MemberwiseClone();
    }

    /// The movement of a prop at an instant: a pure function, so the game and the editor preview move it identically.
    public static class SeasonPropMotion
    {
        public static void Evaluate(SeasonProp prop, float time, out Vector2 offset, out float scale, out float rotation)
        {
            offset = Vector2.zero;
            scale = 1f;
            rotation = 0f;
            var phase = time * prop.motionSpeed * Mathf.PI;      // speed 1 = one cycle every two seconds
            switch (prop.motion)
            {
                case PropMotion.Float: offset = new Vector2(0f, Mathf.Sin(phase) * 18f * prop.motionAmount); break;
                case PropMotion.Pulse: scale = 1f + Mathf.Sin(phase) * 0.06f * prop.motionAmount; break;
                case PropMotion.Sway: rotation = Mathf.Sin(phase) * 6f * prop.motionAmount; break;
                case PropMotion.Spin: rotation = -time * 40f * prop.motionSpeed * prop.motionAmount; break;
            }
        }
    }
}
