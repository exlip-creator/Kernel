using System.Collections.Generic;
using UnityEngine;

namespace Kernel.Combat
{
    /// <summary>
    /// Арена: при входе игрока спавнит врагов; после N убийств открывает дверь (HingeDoor).
    /// </summary>
    public sealed class EnemyKillDoorGate : MonoBehaviour
    {
        [Header("Условие")]
        [SerializeField] private int killsRequired = 5;

        [Header("Дверь")]
        [SerializeField] private HingeDoor door;

        [Header("Враги")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform[] spawnPoints;

        [Header("Запуск волны")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool spawnOnArenaEnter = true;

        private int _killCount;
        private bool _waveSpawned;
        private bool _doorOpened;
        private readonly List<Health> _tracked = new();

        private void OnTriggerEnter(Collider other)
        {
            if (!spawnOnArenaEnter || _waveSpawned)
                return;
            if (!IsUnderTaggedPlayer(other, playerTag))
                return;

            SpawnWave();
        }

        private void SpawnWave()
        {
            if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
                return;

            _waveSpawned = true;

            for (int i = 0; i < killsRequired; i++)
            {
                Transform point = spawnPoints[i % spawnPoints.Length];
                if (point == null)
                    continue;

                GameObject enemy = Instantiate(enemyPrefab, point.position, point.rotation);
                TrackEnemy(enemy);
            }
        }

        private void TrackEnemy(GameObject enemy)
        {
            Health health = enemy.GetComponent<Health>();
            if (health == null)
                health = enemy.GetComponentInChildren<Health>();
            if (health == null)
                return;

            _tracked.Add(health);
            health.Died += OnEnemyDied;
        }

        private void OnEnemyDied()
        {
            if (_doorOpened)
                return;

            _killCount++;
            if (_killCount >= killsRequired)
                OpenDoor();
        }

        private void OpenDoor()
        {
            _doorOpened = true;
            if (door != null)
                door.Open();
        }

        private void OnDestroy()
        {
            foreach (Health health in _tracked)
            {
                if (health != null)
                    health.Died -= OnEnemyDied;
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
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(18f, 5f, 14f);
            }
            else
            {
                collider.isTrigger = true;
            }
        }
    }
}
