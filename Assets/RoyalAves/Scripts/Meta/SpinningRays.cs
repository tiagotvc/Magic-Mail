// Gira a imagem (as faixas de luz atrás do power-up) continuamente. Ignora Time.timeScale para continuar
// girando mesmo com o jogo pausado atrás da modal.
using UnityEngine;

namespace RoyalAves.Meta
{
    [AddComponentMenu("Royal Aves/Spinning Rays")]
    public class SpinningRays : MonoBehaviour
    {
        [Tooltip("Graus por segundo. Negativo gira no sentido horário.")]
        [SerializeField] float degreesPerSecond = -25f;

        public float DegreesPerSecond { get => degreesPerSecond; set => degreesPerSecond = value; }

        void Update() => transform.Rotate(0f, 0f, degreesPerSecond * Time.unscaledDeltaTime);
    }
}
