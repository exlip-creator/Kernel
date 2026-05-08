using System.Collections.Generic;
using UnityEngine;

namespace Kernel.Combat
{
    public sealed class SlimeDamageTrigger : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private string targetTag = "Player";

        [Header("Damage")]
        [SerializeField] private float damageOnEnter = 5f;
        [SerializeField] private float damagePerSecond = 10f;

        private readonly Dictionary<Health, float> _nextTickByTarget = new();

        private void OnDisable()
        {
            _nextTickByTarget.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            Health hp = other.GetComponentInParent<Health>();
            if (hp == null)
                return;

            if (!string.IsNullOrWhiteSpace(targetTag) && !hp.CompareTag(targetTag))
                return;

            if (damageOnEnter > 0f)
                hp.TakeDamage(damageOnEnter);

            if (damagePerSecond > 0f)
                _nextTickByTarget[hp] = Time.time + 1f;
        }

        private void OnTriggerExit(Collider other)
        {
            Health hp = other.GetComponentInParent<Health>();
            if (hp == null)
                return;

            if (!string.IsNullOrWhiteSpace(targetTag) && !hp.CompareTag(targetTag))
                return;

            _nextTickByTarget.Remove(hp);
        }

        private void OnTriggerStay(Collider other)
        {
            if (damagePerSecond <= 0f)
                return;

            Health hp = other.GetComponentInParent<Health>();
            if (hp == null)
                return;

            if (!string.IsNullOrWhiteSpace(targetTag) && !hp.CompareTag(targetTag))
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
    }
}

