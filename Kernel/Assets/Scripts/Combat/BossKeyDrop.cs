using UnityEngine;

namespace Kernel.Combat
{
    /// <summary>
    /// Показывает ключ на месте босса после его смерти (объект скрыт до этого).
    /// </summary>
    public sealed class BossKeyDrop : MonoBehaviour
    {
        [SerializeField] private Health bossHealth;
        [SerializeField] private GameObject keyPickup;
        [SerializeField] private Vector3 dropOffset = new(0f, 0.35f, 0f);
        [SerializeField] private bool hideKeyUntilDrop = true;

        private bool _dropped;

        private void Awake()
        {
            if (bossHealth == null)
                bossHealth = GetComponent<Health>();

            if (keyPickup == null)
            {
                GameObject found = GameObject.Find("Cage_Key");
                if (found != null)
                    keyPickup = found;
            }

            if (hideKeyUntilDrop && keyPickup != null)
                keyPickup.SetActive(false);
        }

        private void OnEnable()
        {
            if (bossHealth != null)
                bossHealth.Died += OnBossDied;
        }

        private void OnDisable()
        {
            if (bossHealth != null)
                bossHealth.Died -= OnBossDied;
        }

        private void OnBossDied()
        {
            if (_dropped || keyPickup == null)
                return;

            _dropped = true;
            keyPickup.transform.SetPositionAndRotation(transform.position + dropOffset, Quaternion.identity);
            keyPickup.SetActive(true);
        }
    }
}
