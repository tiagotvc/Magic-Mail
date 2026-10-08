// Continuous "flowing light" for a static glow/cone sprite: scrolls its texture's offset in a loop instead
// of spawning particles. Individual particles (stars, little cones, anything) always read as a separate
// shape rising and stacking up one after another — this moves the glow INSIDE one fixed silhouette instead,
// so there's never a visible discrete piece to notice, just continuous motion.
using UnityEngine;

namespace RoyalAves.Meta
{
    public class ScrollingGlow : MonoBehaviour
    {
        [Tooltip("Renderer cujo material vai rolar (ex.: o Particle System Renderer do cone).")]
        [SerializeField] Renderer target;
        [Tooltip("Velocidade e direção da rolagem (Y positivo = a textura sobe = a luz parece subir).")]
        [SerializeField] Vector2 scrollSpeed = new Vector2(0f, 0.6f);

        Material runtimeMaterial;
        Vector2 offset;

        void Awake()
        {
            // .material (não .sharedMaterial) já instancia uma cópia própria pra esse objeto — mexer nela
            // não afeta o asset original nem outros objetos que usem o mesmo material.
            if (target != null) runtimeMaterial = target.material;
        }

        void Update()
        {
            if (runtimeMaterial == null) return;
            offset += scrollSpeed * Time.deltaTime;
            runtimeMaterial.mainTextureOffset = offset;
        }
    }
}
