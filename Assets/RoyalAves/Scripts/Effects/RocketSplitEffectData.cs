using UnityEngine;

namespace RoyalAves.Effects
{
    // Art + material the rocket's split-and-fly effect needs at runtime, loaded once via
    // Resources.Load("Effects/RocketSplitEffectData") since the effect is built entirely in code (see RocketSplitEffect).
    [CreateAssetMenu(menuName = "Royal Aves/Rocket Split Effect Data")]
    public class RocketSplitEffectData : ScriptableObject
    {
        // Textures, not Sprites: a texture's main asset fileID is always the fixed 2800000, while the generated
        // Sprite sub-asset's fileID is a hash assigned at import time - hand-writing that hash into this .asset
        // without an actual Unity import has broken this reference twice already. RocketSplitEffect builds the
        // Sprite it needs from these at runtime instead.
        public Texture2D HalfLeft;
        public Texture2D HalfRight;
        [Tooltip("Bocal separado atrás de cada metade. Vazio = a chama sai direto do anel dourado da metade.")]
        public Texture2D EngineNozzle;
        public Material FlameMaterial;

        [Header("Cauda de foguete: fogo cartoon")]
        public Color fireHot = new Color(1f, 0.97f, 0.35f);
        public Color fireMid = new Color(1f, 0.56f, 0.02f);
        public Color fireEnd = new Color(1f, 0.18f, 0.02f);
        [Tooltip("Labaredas por segundo.")]
        [Min(0)] public float fireRate = 150f;
        [Tooltip("Tamanho das labaredas (1 = padrão).")]
        [Min(0)] public float fireSize = 1.25f;
        [Tooltip("Quanto tempo cada labareda fica no rastro, em segundos.")]
        [Min(0.05f)] public float fireLifetime = 0.5f;
        [Tooltip("Largura da boca do fogo (espalhamento lateral).")]
        [Min(0)] public float fireSpread = 0.2f;

        [Header("Rastro contínuo colado na metade (vermelho por fora, laranja no meio, amarelo no miolo)")]
        [Min(0.02f)] public float trailTime = 0.36f;
        [Tooltip("Largura do rastro (1 = padrão).")]
        [Min(0)] public float trailWidth = 1f;

        [Header("Fumaça amarelada")]
        public Color smokeStart = new Color(1f, 0.88f, 0.32f);
        public Color smokeEnd = new Color(0.98f, 0.6f, 0.16f);
        [Range(0f, 1f)] public float smokeAlpha = 0.8f;
        [Tooltip("Nuvens por segundo.")]
        [Min(0)] public float smokeRate = 42f;
        [Tooltip("Tamanho das nuvens (1 = padrão).")]
        [Min(0)] public float smokeSize = 1f;
        [Tooltip("Quanto tempo a fumaça fica para trás, em segundos.")]
        [Min(0.1f)] public float smokeLifetime = 0.95f;

        [Header("Faíscas")]
        [Min(0)] public float sparkRate = 36f;
    }
}
