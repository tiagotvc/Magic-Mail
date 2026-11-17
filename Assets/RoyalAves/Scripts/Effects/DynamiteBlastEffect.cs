using UnityEngine;
namespace RoyalAves.Effects
{
    /// <summary>
    /// Explosão da bomba (pacote explosivo). Agora é o ImpactBurst "Potion" (clarão, ondas, raios, fragmentos), vindo do
    /// pool; esta classe só mantém a API que o gameplay já usa e a pausa de impacto (IsEmphasizing).
    /// </summary>
    public sealed class DynamiteBlastEffect : MonoBehaviour
    {
        ImpactBurst burst;
        int playId;
        float emphasisEnd;
        /// <summary>O gameplay espera este tempo, enquanto o clarão e a onda estão no auge, antes de remover a bomba.</summary>
        public bool IsEmphasizing => Time.time < emphasisEnd || (burst != null && burst.Active && burst.PlayId == playId && burst.Age < .28f);

        public static DynamiteBlastEffect Play(Vector3 position,float cellWidth,int radius,bool combined)
        {
            // Efeito magico novo (onda de choque, raios, aura, fragmentos nas cores das pecas); o ImpactBurst antigo fica de reserva
            // para quando o prefab Resources/Effects/PowerUps/PowerUpVfx nao existir.
            if (PowerUpVfx.PlayBomb(position, cellWidth, radius))
            {
                var holder = new GameObject("DynamiteBlast");
                var marker = holder.AddComponent<DynamiteBlastEffect>();
                marker.emphasisEnd = Time.time + .28f;
                Destroy(holder, .6f);
                return marker;
            }
            var preset = combined ? ImpactPreset.Chain : ImpactPreset.Potion;
            // Mesma regra de antes: a explosão combinada cobre o tabuleiro todo; a normal usa o raio da bomba.
            var burst = ImpactBurst.Play(preset, position + Vector3.back * .3f, cellWidth, radius);
            if (burst == null) return null;
            var playback = burst.GetComponent<DynamiteBlastEffect>();
            if (playback == null) playback = burst.gameObject.AddComponent<DynamiteBlastEffect>();
            playback.burst = burst;
            playback.playId = burst.PlayId;
            return playback;
        }
    }
}
