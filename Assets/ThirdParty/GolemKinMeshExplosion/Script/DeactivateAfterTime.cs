using UnityEngine;

namespace GolemKin.Explosion
{
    public class DeactivateAfterTime : MonoBehaviour
    {
        #region Variables

        [SerializeField, Tooltip("Time in seconds before the object gets deactivated.")]
        private float deactivationTime = 5f; // Time in seconds before the object is deactivated

        private float timer;

        #endregion

        #region Unity Methods

        private void OnEnable()
        {
            // Reset the timer whenever the object is enabled
            timer = deactivationTime;
        }

        private void Update()
        {
            // Count down the timer
            timer -= Time.deltaTime;

            // If the timer reaches zero, deactivate the GameObject
            if (timer <= 0f)
            {
                gameObject.SetActive(false);
            }
        }

        #endregion
    }
}