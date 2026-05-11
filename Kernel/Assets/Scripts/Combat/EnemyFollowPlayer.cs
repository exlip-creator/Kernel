using UnityEngine;

namespace Kernel.Combat
{
    /// <summary>
    /// Преследует объект с тегом Player (или заданный Target). Нужен CharacterController и коллайдер для попаданий бластера.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class EnemyFollowPlayer : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float rotateSpeed = 420f;
        [SerializeField] private float stoppingDistance = 1.35f;
        [SerializeField] private float gravity = -18f;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private Transform targetOverride;

        private CharacterController _cc;
        private Health _health;
        private Transform _target;
        private float _verticalVelocity;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _health = GetComponent<Health>();
        }

        private void Start() => ResolveTarget();

        private void Update()
        {
            if (_health != null && _health.IsDead)
                return;

            if (_target == null)
                ResolveTarget();
            if (_target == null)
                return;

            Vector3 flatDelta = _target.position - transform.position;
            flatDelta.y = 0f;
            float dist = flatDelta.magnitude;

            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += gravity * Time.deltaTime;

            if (dist > stoppingDistance && dist > 0.02f)
            {
                Vector3 dir = flatDelta / dist;
                Quaternion look = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, rotateSpeed * Time.deltaTime);

                Vector3 move = dir * moveSpeed;
                move.y = _verticalVelocity;
                _cc.Move(move * Time.deltaTime);
            }
            else
            {
                _cc.Move(new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);
            }
        }

        private void ResolveTarget()
        {
            if (targetOverride != null)
            {
                _target = targetOverride;
                return;
            }

            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            _target = p != null ? p.transform : null;
        }
    }
}
