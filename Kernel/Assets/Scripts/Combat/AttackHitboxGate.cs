using UnityEngine;

namespace Kernel.Combat
{
    /// <summary>
    /// Включает/выключает коллайдер зоны удара (обычно на AttackHitzone).
    /// </summary>
    public sealed class AttackHitboxGate : MonoBehaviour
    {
        [SerializeField] private Collider hitCollider;

        private void Awake()
        {
            if (hitCollider == null)
                hitCollider = GetComponent<Collider>();

            SetActive(false);
        }

        public void SetActive(bool active)
        {
            if (hitCollider != null)
                hitCollider.enabled = active;
        }
    }
}
