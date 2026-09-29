// Simple idle animation for a static portrait (the HUD avatar badge): a soft breathing pulse and an
// occasional, clearly-visible swap to a happier expression picked from a small pool. This is
// independent of ManagerFace/ManagerExpressionSet — that pair is built around the Gerente's full
// game-state-driven mood system (nervous, worried, victory...) and expects a much larger set of
// pictures (blinks, glances, in-between frames) than a simple decorative badge needs or has.
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Characters
{
    [RequireComponent(typeof(Image))]
    public class AvatarIdleAnimator : MonoBehaviour
    {
        [Tooltip("Vazio = usa o sprite já atribuído no Image ao iniciar.")]
        [SerializeField] Sprite neutral;
        [Tooltip("Expressões extras mostradas de vez em quando, sorteadas.")]
        [SerializeField] Sprite[] smiles;

        [Header("Sorriso ocasional")]
        [SerializeField] Vector2 smileEvery = new Vector2(5f, 10f);
        [SerializeField] Vector2 smileFor = new Vector2(2.5f, 4f);

        [Header("Respiração")]
        [SerializeField, Range(0f, 0.05f)] float breathing = 0.012f;

        Image image;
        Vector3 baseScale;
        float nextSmile, smileLeft;

        void Awake()
        {
            image = GetComponent<Image>();
            baseScale = transform.localScale;
            if (neutral == null) neutral = image.sprite;
            ScheduleSmile();
        }

        void OnEnable()
        {
            image.sprite = neutral;
            transform.localScale = baseScale;
            smileLeft = 0f;
        }

        void OnDisable()
        {
            transform.localScale = baseScale;
        }

        void Update()
        {
            var dt = Time.unscaledDeltaTime;
            UpdateSmile(dt);
            Breathe();
        }

        void UpdateSmile(float dt)
        {
            if (smileLeft > 0f)
            {
                smileLeft -= dt;
                if (smileLeft <= 0f) { image.sprite = neutral; ScheduleSmile(); }
                return;
            }
            if (smiles == null || smiles.Length == 0) return;
            nextSmile -= dt;
            if (nextSmile <= 0f)
            {
                var pick = smiles[Random.Range(0, smiles.Length)];
                if (pick != null) image.sprite = pick;
                smileLeft = Random.Range(smileFor.x, smileFor.y);
            }
        }

        void Breathe()
        {
            var breath = breathing * Mathf.Sin(Time.unscaledTime * 2f * Mathf.PI / 3.2f);
            var scale = transform.localScale;
            scale.y = baseScale.y * (1f + breath);
            transform.localScale = scale;
        }

        void ScheduleSmile() => nextSmile = Random.Range(smileEvery.x, smileEvery.y);
    }
}
