using UnityEngine;

namespace Kernel.Movement
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class LadderClimbZone : MonoBehaviour
    {
        [Header("Climb")]
        [Min(0f)]
        public float climbSpeed = 2.25f;

        [Header("Snap")]
        public bool snapToCenter = true;

        [Min(0f)]
        public float snapSpeed = 12f;

        private BoxCollider _box;

        public Vector3 Up => transform.up;

        private void Reset()
        {
            _box = GetComponent<BoxCollider>();
            _box.isTrigger = true;

            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        private void Awake()
        {
            _box = GetComponent<BoxCollider>();
            _box.isTrigger = true;

            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        public Vector3 GetSnapDelta(Vector3 worldPosition)
        {
            if (!snapToCenter)
                return Vector3.zero;

            // Keep the player's Y; snap only in the horizontal plane.
            Vector3 target = transform.position;
            target.y = worldPosition.y;

            Vector3 delta = target - worldPosition;
            return Vector3.ProjectOnPlane(delta, Vector3.up);
        }
    }
}

