// Configuração do menu de ajustes durante a fase (o que abre ao tocar na engrenagem): fundo escuro e os botões redondos de
// música, som e vibração. Tudo editável no Inspector, sem mexer em código: quadro do botão, ícones, o risco que marca
// "desligado" (sprite, com um placeholder), tamanhos, cores e tempos da animação.
//
// Asset: Resources/Settings/GameplaySettingsMenu.asset (menu Royal Aves > Menu de ajustes da fase > Abrir configuração).
using UnityEngine;

namespace RoyalAves.Meta
{
    [CreateAssetMenu(fileName = "GameplaySettingsMenu", menuName = "Royal Aves/Menu de ajustes da fase")]
    public sealed class GameplaySettingsMenuConfig : ScriptableObject
    {
        public const string ResourcePath = "Settings/GameplaySettingsMenu";

        public enum Layout
        {
            [Tooltip("Uma linha por cima da barra de power-ups: os botões saem da engrenagem para a esquerda.")] RowOverBar,
            [Tooltip("Uma coluna que sobe a partir da engrenagem.")] ColumnAboveGear,
        }

        [Header("Disposição")]
        public Layout layout = Layout.RowOverBar;
        [Tooltip("Da esquerda para a direita (a engrenagem fica à direita do último).")]
        public bool musicSoundVibrationOrder = true;

        [Header("Fundo escuro")]
        public Color dimColor = new Color(0f, 0f, 0f, 0.86f);
        [Min(0)] public float dimFadeSeconds = 0.18f;

        [Header("Botão redondo (o mesmo quadro dos power-ups)")]
        public Sprite buttonFrame;
        public Color buttonTint = Color.white;
        [Tooltip("Tamanho em relação ao da engrenagem.")]
        [Min(0.2f)] public float sizeMultiplier = 1f;
        [Tooltip("Distância entre os centros, em tamanhos de botão.")]
        [Min(0.5f)] public float spacing = 1.12f;

        [Header("Ícones (troque quando tiver a arte final)")]
        public Sprite musicIcon;
        public Sprite soundIcon;
        public Sprite vibrationIcon;
        [Range(0.2f, 1.2f)] public float iconScale = 0.62f;
        public Vector2 iconOffset;
        public Color iconTint = Color.white;

        [Header("Risco de \"desligado\" (sprite por cima do ícone)")]
        [Tooltip("Vazio = usa um risco diagonal gerado em código (placeholder).")]
        public Sprite slash;
        public Color slashColor = new Color(0.95f, 0.25f, 0.3f, 1f);
        [Tooltip("Tamanho do risco em relação ao botão.")]
        public Vector2 slashSize = new Vector2(0.78f, 0.78f);
        public Vector2 slashOffset;
        [Tooltip("Quanto o ícone escurece quando está desligado (1 = não escurece).")]
        [Range(0f, 1f)] public float offIconBrightness = 0.55f;

        [Header("Animação")]
        [Tooltip("Tempo que cada botão leva para ir da engrenagem até o seu lugar.")]
        [Min(0.05f)] public float slideSeconds = 0.3f;
        [Tooltip("Atraso de um botão para o seguinte.")]
        [Min(0)] public float stagger = 0.07f;
        [Tooltip("Quanto o botão cresce além do tamanho final na pulsação de chegada (0.15 = 15%).")]
        [Range(0f, 0.5f)] public float popOvershoot = 0.16f;
        [Min(0.05f)] public float closeSeconds = 0.22f;

        [Header("Sair da fase (opcional)")]
        [Tooltip("Mostra também o botão de sair da fase (com o aviso de perder a vida).")]
        public bool showExitButton = false;
        public Sprite exitIcon;
    }
}
