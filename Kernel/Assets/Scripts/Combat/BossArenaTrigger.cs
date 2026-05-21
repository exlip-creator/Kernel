using UnityEngine;

namespace Kernel.Combat
{
    /// <summary>
    /// Зона арены босса: при входе игрока активирует бой у <see cref="SpiderBossAI"/>.
    /// </summary>
    public sealed class BossArenaTrigger : MonoBehaviour
    {
        [SerializeField] private SpiderBossAI spiderBoss;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool activateOnce = true;

        private bool _activated;

        private void Awake()
        {
            if (spiderBoss == null)
                spiderBoss = FindFirstObjectByType<SpiderBossAI>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (activateOnce && _activated)
                return;

            if (!IsUnderTaggedPlayer(other, playerTag))
                return;

            if (spiderBoss == null)
                return;

            _activated = true;
            spiderBoss.ActivateCombat();
        }

        private static bool IsUnderTaggedPlayer(Collider other, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return true;

            Transform tr = other.transform;
            while (tr != null)
            {
                if (tr.CompareTag(tag))
                    return true;
                tr = tr.parent;
            }

            return false;
        }

        private void Reset()
        {
            var c = GetComponent<Collider>();
            if (c == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(12f, 4f, 12f);
            }
            else
            {
                c.isTrigger = true;
            }
        }
    }
}
