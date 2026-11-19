// Chapéu de Mago (power-up de embaralhar): abre a tela do power-up com o botão "Usar!" no meio; ao tocar, embaralha as
// peças com o redemoinho mágico e depois o cajado troca a cor de algumas. Tocar fora do botão cancela sem gastar.
using RoyalAves.Effects;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.GUI.Boost;

namespace RoyalAves.Meta
{
    public static class MagicHat
    {
        /// Abre a tela do chapéu. false = não deu para abrir (falta o prefab dos efeitos, a arte do botão ou a tela):
        /// quem chamou embaralha na hora, como antes.
        public static bool Begin(BoostIcon boost)
        {
            var player = PowerUpVfx.Player;
            if (player == null || boost == null || player.useButtonBorder == null) return false;
            var h = player.hat;
            var entry = new BoosterIntroCatalog.Entry
            {
                type = BoostType.Cap, icon = player.hatIcon, titlePt = h.titlePt, titleEn = h.titleEn,
                instructionPt = h.instructionPt, instructionEn = h.instructionEn,
            };
            var use = new BoosterIntroScreen.UseButton
            {
                border = player.useButtonBorder, center = player.useButtonCenter, font = player.useButtonFont,
                labelPt = h.useLabelPt, labelEn = h.useLabelEn, size = h.useButtonSize,
                onUse = () =>
                {
                    var lm = LevelManager.THIS;
                    if (lm == null) return;
                    lm.ShuffleBoard();
                    lm.ActivatedBoost = null;        // gasta o power-up e libera a barra, como antes
                },
            };
            return BoosterIntroScreen.ShowUse(boost, entry, use);
        }
    }
}
