using System.Collections;
using UnityEngine;

namespace Kernel.Combat
{
    /// <summary>
    /// Повесьте на объект с коллайдером Is Trigger. При входе игрока (тег по умолчанию Player) создаёт префаб врага в точке спавна.
    /// </summary>
    public sealed class EnemySpawnTrigger : MonoBehaviour
    {
        [Header("Кого спавнить")]
        [SerializeField] private GameObject enemyPrefab;

        [Header("Где")]
        [Tooltip("Если пусто — позиция и поворот этого объекта (триггера).")]
        [SerializeField] private Transform spawnPoint;

        [Header("Кто активирует")]
        [SerializeField] private string playerTag = "Player";

        [Header("Поведение")]
        [SerializeField] private bool spawnOnce = true;
        [SerializeField] private float delaySeconds;
        [Tooltip("Выключить коллайдер после спавна (зона больше не реагирует).")]
        [SerializeField] private bool disableColliderAfterSpawn = true;

        private bool _spawned;
        private Coroutine _pending;

        private void OnDisable()
        {
            if (_pending != null)
            {
                StopCoroutine(_pending);
                _pending = null;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (enemyPrefab == null) return;
            if (spawnOnce && _spawned) return;
            if (!IsUnderTaggedPlayer(other, playerTag)) return;

            if (_pending != null)
                return;

            if (delaySeconds > 0f)
                _pending = StartCoroutine(SpawnAfterDelay());
            else
                DoSpawn();
        }

        private IEnumerator SpawnAfterDelay()
        {
            yield return new WaitForSeconds(delaySeconds);
            _pending = null;
            DoSpawn();
        }

        private void DoSpawn()
        {
            if (enemyPrefab == null) return;
            if (spawnOnce && _spawned) return;

            _spawned = true;

            Transform t = spawnPoint != null ? spawnPoint : transform;
            Instantiate(enemyPrefab, t.position, t.rotation);

            if (disableColliderAfterSpawn)
            {
                foreach (var c in GetComponents<Collider>())
                    c.enabled = false;
            }
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
                box.size = new Vector3(6f, 4f, 3f);
            }
            else
            {
                c.isTrigger = true;
            }
        }
    }
}
