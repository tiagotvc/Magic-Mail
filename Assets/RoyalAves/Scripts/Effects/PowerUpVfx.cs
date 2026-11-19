// Fachada que o jogo usa para tocar os efeitos mágicos (match, bomba, line cleaner). Cria o player a partir do prefab
// Resources/Effects/PowerUps/PowerUpVfx na primeira vez; se o prefab não existir, as chamadas não fazem nada (o jogo segue
// com os efeitos antigos). Nada aqui decide pontuação, remoção de peças ou movimento.
using System.Collections.Generic;
using System.Linq;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using UnityEngine;

namespace RoyalAves.Effects
{
    public static class PowerUpVfx
    {
        static PowerUpVfxPlayer player;
        static bool searched;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { player = null; searched = false; }

        /// O player do jogo (null se o prefab não existe).
        public static PowerUpVfxPlayer Player
        {
            get
            {
                if (player != null) return player;
                var prefab = Resources.Load<PowerUpVfxPlayer>(PowerUpVfxPlayer.ResourcePath);
                searched = true;
                if (prefab == null) return null;
                player = Object.Instantiate(prefab);
                player.name = "PowerUpVfx (runtime)";
                player.manual = false;
                return player;
            }
        }

        public static bool IsAvailable
        {
            get
            {
                if (player != null) return true;
                if (!searched) return Player != null;
                return false;
            }
        }

        /// Enquanto os efeitos de bomba, line cleaner e flecha rodam, as peças que eles destroem não ganham também o efeito de match
        /// (o efeito deles já cobre cada peça).
        public static float SuppressMatchUntil;

        public static void SuppressMatchFor(float seconds) => SuppressMatchUntil = Mathf.Max(SuppressMatchUntil, Time.time + seconds);

        static float CellSize => LevelManager.THIS != null ? LevelManager.THIS.squareWidth : 1.2f;

        public static void PlayMatch(Vector3 position, Color pieceColor)
        {
            if (Time.time < SuppressMatchUntil) return;
            var p = Player;
            if (p != null) p.PlayMatch(position, pieceColor, CellSize);
        }

        /// Girocóptero: decolagem e rastro mágico que seguem o objeto enquanto ele voa.
        public static void PlaySeekerFlight(Transform follow)
        {
            var p = Player;
            if (p != null) p.PlaySeekerFlight(follow, CellSize);
        }

        /// Girocóptero: marcador dourado no alvo durante o voo.
        public static void PlaySeekerMarker(Vector3 target, float seconds)
        {
            var p = Player;
            if (p != null) p.PlaySeekerMarker(target, seconds, CellSize);
        }

        /// Girocóptero: impacto na peça-alvo (substitui o efeito de match daquela peça).
        public static void PlaySeekerImpact(Vector3 target, Color pieceColor)
        {
            var p = Player;
            if (p == null) return;
            p.PlaySeekerImpact(target, pieceColor, CellSize);
            SuppressMatchFor(0.1f);
        }

        /// Light ball, 1: carga mágica em volta do globo. false = sem prefab (o jogo usa o efeito antigo).
        public static bool PlayLightBallCharge(Transform owner, Transform follow)
        {
            var p = Player;
            return p != null && p.PlayLightBallCharge(owner, follow, CellSize);
        }

        /// Ajustes do giro e do vento do seeker (do prefab dos efeitos, ou os padrões se ele não existir).
        static readonly PowerUpVfxPlayer.WhirlSettings defaultWhirl = new PowerUpVfxPlayer.WhirlSettings();
        public static PowerUpVfxPlayer.WhirlSettings Whirl
        {
            get { var p = IsAvailable ? Player : null; return p != null ? p.whirl : defaultWhirl; }
        }

        /// Vento de giro em volta do seeker. seconds &lt;= 0: em laço até ele sumir (voo).
        public static void PlaySeekerWhirl(Transform follow, float seconds, float strength = 1f)
        {
            var p = Player;
            if (p != null) p.PlaySeekerWhirl(follow, seconds, CellSize, strength);
        }

        /// Color bomb parada: brilho permanente (raios coloridos e halo quente). null = sem prefab dos efeitos.
        public static PowerUpVfxPlayer.IdleGlow PlayLightBallIdle(Transform follow)
        {
            var p = Player;
            return p != null ? p.PlayLightBallIdle(follow, CellSize) : null;
        }

        /// Light ball, 2: conexão luminosa do globo até a peça.
        public static bool PlayLightBallBeam(Transform owner, Transform target, Color pieceColor)
        {
            var p = Player;
            return p != null && p.PlayLightBallBeam(owner, target, pieceColor);
        }

        /// Light ball, 3: explosão coordenada do globo e das peças conectadas (substitui o efeito de match delas).
        public static bool DetonateLightBall(Transform owner)
        {
            var p = Player;
            if (p == null || !p.DetonateLightBall(owner)) return false;
            SuppressMatchFor(0.25f);
            return true;
        }

        public static void CancelLightBall(Transform owner)
        {
            if (player != null) player.CancelLightBall(owner);
        }

        /// Combinação de dois light balls. Devolve o roteiro (posição dos globos e hora em que a onda chega em cada peça),
        /// ou null se não há prefab (o jogo usa a sequência antiga).
        public static PowerUpVfxPlayer.DoubleBallPlan PlayDoubleLightBall(Transform ballA, Transform ballB, IList<Vector3> cells, IList<Color> colors)
        {
            var p = Player;
            if (p == null) return null;
            var plan = p.PlayDoubleLightBall(ballA, ballB, cells, colors, CellSize);
            if (plan != null) SuppressMatchFor(plan.Total);
            return plan;
        }

        static readonly PowerUpVfxPlayer.PresenceSettings defaultPresence = new PowerUpVfxPlayer.PresenceSettings();

        /// Ajustes da chegada e da junção dos power-ups (os do prefab; sem prefab, os valores padrão).
        public static PowerUpVfxPlayer.PresenceSettings Presence
        {
            get { var p = IsAvailable ? Player : null; return p != null ? p.presence : defaultPresence; }
        }

        /// Chegada de um power-up no tabuleiro: luz, anel e fitas girando em volta dele.
        public static void PlayArrival(Transform follow, PowerUpKind kind)
        {
            var p = Player;
            if (p != null) p.PlayArrival(follow, kind, CellSize);
        }

        /// Junção de dois power-ups: brilho atrás e fitas girando. Devolve o roteiro do movimento (null sem prefab).
        public static PowerUpVfxPlayer.MergePlan PlayMerge(Transform a, Transform b)
        {
            var p = Player;
            return p == null ? null : p.PlayMerge(a, b, CellSize);
        }

        /// Duas bombas: carga lado a lado.
        public static void PlayDoubleBombCharge(Transform a, Transform b, float seconds)
        {
            var p = Player;
            if (p != null) p.PlayDoubleBombCharge(a, b, seconds, CellSize);
        }

        /// Duas bombas: explosão dupla, por cima da explosão normal.
        public static void PlayDoubleBombBurst(Vector3 center, float cellWidth, float radiusCells)
        {
            var p = Player;
            if (p == null) return;
            SuppressMatchFor(p.PlayDoubleBombBurst(center, radiusCells, cellWidth) * 0.6f);
        }

        /// Põe o sprite de um globo acima da tela escura da combinação dupla.
        public static void RaiseAboveDim(SpriteRenderer renderer)
        {
            if (player != null) player.RaiseAboveDim(renderer);
        }

        /// Chapéu de Mago: redemoinho do embaralhamento. Devolve o roteiro (onde cada peça fica em cada instante) ou null sem prefab.
        public static PowerUpVfxPlayer.HatShufflePlan PlayHatShuffle(IList<Transform> pieces, IList<Color> colors, Vector3 center)
        {
            var p = Player;
            return p == null ? null : p.PlayHatShuffle(pieces, colors, center, CellSize);
        }

        /// Chapéu de Mago: cajado lançando uma magia em cada alvo. Devolve quando cada uma chega (para trocar a cor nessa hora).
        public static PowerUpVfxPlayer.StaffCastPlan PlayStaffCast(Vector3 staffPosition, IList<Vector3> targets, IList<Color> newColors)
        {
            var p = Player;
            return p == null ? null : p.PlayStaffCast(staffPosition, targets, newColors, CellSize);
        }

        // A tela escura é só para power-ups (reforços usados pelo jogador: flecha, canhão, martelo...). Bomba e
        // limpador de linha que estouram no tabuleiro por combinação NÃO escurecem a tela; só escurecem quando
        // são disparados enquanto um power-up está em uso.
        // Usa o campo activatedBoost: a propriedade ActivatedBoost nunca é nula (devolve um ícone vazio).
        // Só os power-ups de toque no tabuleiro contam (flecha, canhão, martelo): light ball e girocóptero não escurecem.
        static bool PowerUpInUse
        {
            get
            {
                var boost = LevelManager.THIS != null ? LevelManager.THIS.activatedBoost : null;
                if (boost == null) return false;
                var type = boost.type;
                return type == SweetSugar.Scripts.GUI.Boost.BoostType.Arrow ||
                       type == SweetSugar.Scripts.GUI.Boost.BoostType.Cannon ||
                       type == SweetSugar.Scripts.GUI.Boost.BoostType.Hammer;
            }
        }

        public static bool PlayBomb(Vector3 center, float cellWidth, float radiusCells)
        {
            var p = Player;
            if (p == null) return false;
            var cells = new List<Vector3>(); var colors = new List<Color>();
            var field = LevelManager.THIS != null ? LevelManager.THIS.field : null;
            if (field != null)
            {
                var reach = Mathf.Max(0.5f, radiusCells) * cellWidth + cellWidth * 0.55f;
                foreach (var square in field.GetSquares())
                {
                    if (square == null || square.IsNone()) continue;
                    var offset = square.transform.position - center; offset.z = 0f;
                    if (offset.magnitude > reach) continue;
                    cells.Add(square.transform.position);
                    colors.Add(PieceColors.Of(square.Item));
                }
            }
            var duration = p.PlayBomb(center, radiusCells, cellWidth, cells, colors, PowerUpInUse);
            SuppressMatchFor(duration + 0.2f);
            return true;
        }

        public static bool PlayLine(Vector3 origin, bool horizontal, Vector3 endA, Vector3 endB, bool stopA, bool stopB, IList<Square> squares)
        {
            var p = Player;
            if (p == null) return false;
            var cells = squares != null ? squares.Select(s => s.transform.position).ToList() : new List<Vector3>();
            var colors = squares != null ? squares.Select(s => PieceColors.Of(s.Item)).ToList() : new List<Color>();
            var duration = p.PlayLine(origin, horizontal, endA, endB, stopA, stopB, cells, colors, CellSize, PowerUpInUse);
            SuppressMatchFor(duration + 0.2f);
            return true;
        }
    }

    /// Cor média da arte de uma peça (lida na GPU, funciona com texturas não legíveis), guardada por sprite para não repetir a leitura.
    public static class PieceColors
    {
        static readonly Dictionary<Sprite, Color> cache = new Dictionary<Sprite, Color>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => cache.Clear();

        public static Color Of(Item item)
        {
            if (item == null) return Color.clear;
            try
            {
                var renderer = item.GetSpriteRenderers().Where(r => r != null && r.sprite != null)
                    .OrderByDescending(r => r.bounds.size.x * r.bounds.size.y).FirstOrDefault();
                return renderer == null ? Color.clear : Of(renderer.sprite);
            }
            catch (System.Exception) { return Color.clear; }
        }

        public static Color Of(Sprite sprite)
        {
            if (sprite == null) return Color.clear;
            if (cache.TryGetValue(sprite, out var cached)) return cached;
            var result = Sample(sprite);
            cache[sprite] = result;
            return result;
        }

        static Color Sample(Sprite sprite)
        {
            try
            {
                var texture = sprite.texture; var rect = sprite.textureRect;
                var target = RenderTexture.GetTemporary(16, 16, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(texture, target, new Vector2(rect.width / texture.width, rect.height / texture.height), new Vector2(rect.x / texture.width, rect.y / texture.height));
                var previous = RenderTexture.active;
                RenderTexture.active = target;
                var read = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                read.ReadPixels(new Rect(0, 0, 16, 16), 0, 0);
                read.Apply();
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                var pixels = read.GetPixels();
                Object.Destroy(read);
                double r = 0, g = 0, b = 0, w = 0;
                foreach (var c in pixels)
                {
                    var chroma = Mathf.Max(c.r, c.g, c.b) - Mathf.Min(c.r, c.g, c.b);
                    var weight = c.a * (0.15f + chroma);
                    r += c.r * weight; g += c.g * weight; b += c.b * weight; w += weight;
                }
                if (w <= 1e-4) return Color.clear;
                Color.RGBToHSV(new Color((float)(r / w), (float)(g / w), (float)(b / w)), out var h, out var sat, out var val);
                var result = Color.HSVToRGB(h, Mathf.Min(1f, sat * 1.25f), Mathf.Min(1f, 0.55f + val * 0.6f));
                result.a = 1f;
                return result;
            }
            catch (System.Exception) { return Color.clear; }
        }
    }
}
