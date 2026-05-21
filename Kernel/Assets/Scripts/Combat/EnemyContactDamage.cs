using System.Collections.Generic;
using UnityEngine;

namespace Kernel.Combat
{
    
    public sealed class EnemyContactDamage : MonoBehaviour
    {
        [Header("Цель")]
        [SerializeField] private string targetTag = "Player";

        [Header("Урон")]
        [SerializeField] private float damageOnEnter = 8f;
        [SerializeField] private float damagePerSecond = 12f;

        private readonly Dictionary<Health, float> _nextTickByTarget = new();
        private Transform _attackerRoot;

        private void Awake()
        {
            _attackerRoot = transform.root;

            var rb = GetComponent<Rigidbody>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private void OnDisable() => _nextTickByTarget.Clear();

        private void OnTriggerEnter(Collider other)
        {
            if (!TryGetVictimHealth(other, out Health hp))
                return;

            if (damageOnEnter > 0f)
                hp.TakeDamage(damageOnEnter);

            if (damagePerSecond > 0f)
                _nextTickByTarget[hp] = Time.time + 1f;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!TryGetVictimHealth(other, out Health hp))
                return;

            _nextTickByTarget.Remove(hp);
        }

        private void OnTriggerStay(Collider other)
        {
            if (damagePerSecond <= 0f)
                return;

            if (!TryGetVictimHealth(other, out Health hp))
                return;

            if (!_nextTickByTarget.TryGetValue(hp, out float nextTick))
            {
                _nextTickByTarget[hp] = Time.time + 1f;
                return;
            }

            if (Time.time < nextTick)
                return;

            hp.TakeDamage(damagePerSecond);
            _nextTickByTarget[hp] = Time.time + 1f;
        }

        private bool TryGetVictimHealth(Collider other, out Health hp)
        {
            hp = other.GetComponentInParent<Health>();
            if (hp == null)
                return false;

            if (hp.transform.root == _attackerRoot)
                return false;

            if (!string.IsNullOrWhiteSpace(targetTag) && !HasTagInAncestors(other.transform, targetTag))
                return false;

            return true;
        }

        private static bool HasTagInAncestors(Transform t, string tag)
        {
            while (t != null)
            {
                if (t.CompareTag(tag))
                    return true;
                t = t.parent;
            }

            return false;
        }

        private void Reset()
        {
            var c = GetComponent<Collider>();
            if (c == null)
            {
                var s = gameObject.AddComponent<SphereCollider>();
                s.isTrigger = true;
                s.radius = 0.65f;
            }
            else
            {
                c.isTrigger = true;
            }
        }
    }
}
