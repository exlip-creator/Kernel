using UnityEngine;

namespace Kernel.Combat
{
    /// <summary>
    /// Преследует игрока с обходом препятствий (рейкасты + анти-застревание).
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

        [Header("Обход препятствий")]
        [SerializeField] private LayerMask obstacleMask = ~0;
        [SerializeField] private float probeDistance = 1.35f;
        [SerializeField] private float probeRadius = 0.42f;
        [SerializeField] private float steerStrength = 1.35f;
        [SerializeField] private float stuckSeconds = 0.55f;
        private CharacterController _cc;
        private Health _health;
        private Transform _target;
        private float _verticalVelocity;
        private float _stuckTimer;
        private Vector3 _lastFlatPosition;
        private int _unstuckSide = 1;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _health = GetComponent<Health>();
            probeRadius = Mathf.Max(0.15f, probeRadius > 0f ? probeRadius : _cc.radius * 0.9f);
            _lastFlatPosition = FlatPosition;
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
                Vector3 desiredDir = flatDelta / dist;
                Vector3 moveDir = GetSteeredDirection(desiredDir);
                moveDir = ApplyUnstuck(moveDir, desiredDir);

                Quaternion look = Quaternion.LookRotation(moveDir.sqrMagnitude > 0.0001f ? moveDir : desiredDir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, rotateSpeed * Time.deltaTime);

                Vector3 move = moveDir * moveSpeed;
                move.y = _verticalVelocity;
                _cc.Move(move * Time.deltaTime);

                TrackStuck(desiredDir);
            }
            else
            {
                _cc.Move(new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);
                _stuckTimer = 0f;
            }

            _lastFlatPosition = FlatPosition;
        }

        private Vector3 GetSteeredDirection(Vector3 desiredDir)
        {
            Vector3 origin = transform.position + Vector3.up * (_cc.height * 0.35f);
            float radius = probeRadius;
            float distance = probeDistance;

            int mask = GetObstacleMask();
            if (!Physics.SphereCast(origin, radius, desiredDir, out _, distance, mask, QueryTriggerInteraction.Ignore))
                return desiredDir;

            Vector3 right = Vector3.Cross(Vector3.up, desiredDir).normalized;
            Vector3 left = -right;

            float rightScore = ProbeClearance(origin, desiredDir, right, radius, distance, steerStrength, mask);
            float leftScore = ProbeClearance(origin, desiredDir, left, radius, distance, steerStrength, mask);

            Vector3 avoid = rightScore >= leftScore ? right : left;
            Vector3 blended = (desiredDir + avoid * steerStrength).normalized;
            return blended.sqrMagnitude > 0.0001f ? blended : desiredDir;
        }

        private static float ProbeClearance(
            Vector3 origin,
            Vector3 forward,
            Vector3 side,
            float radius,
            float distance,
            float steer,
            int mask)
        {
            Vector3 dir = (forward + side * steer).normalized;
            if (!Physics.SphereCast(origin, radius, dir, out _, distance, mask, QueryTriggerInteraction.Ignore))
                return 1f;

            if (!Physics.SphereCast(origin, radius, side, out _, distance * 0.85f, mask, QueryTriggerInteraction.Ignore))
                return 0.65f;

            return 0f;
        }

        private Vector3 ApplyUnstuck(Vector3 moveDir, Vector3 desiredDir)
        {
            if (_stuckTimer < stuckSeconds)
                return moveDir;

            Vector3 tangent = Vector3.Cross(Vector3.up, desiredDir).normalized * _unstuckSide;
            return tangent.sqrMagnitude > 0.0001f ? tangent : moveDir;
        }

        private void TrackStuck(Vector3 desiredDir)
        {
            float moved = Vector3.Distance(FlatPosition, _lastFlatPosition);
            if (moved < 0.025f * Time.deltaTime * 30f)
                _stuckTimer += Time.deltaTime;
            else
                _stuckTimer = 0f;

            if (_stuckTimer >= stuckSeconds)
                _unstuckSide = Random.value > 0.5f ? 1 : -1;
        }

        private Vector3 FlatPosition
        {
            get
            {
                Vector3 p = transform.position;
                p.y = 0f;
                return p;
            }
        }

        private int GetObstacleMask()
        {
            int mask = obstacleMask.value & ~(1 << gameObject.layer);
            if (_target != null)
                mask &= ~(1 << _target.gameObject.layer);
            return mask;
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

        private void OnValidate()
        {
            if (_cc == null)
                _cc = GetComponent<CharacterController>();

            if (_cc != null && probeRadius <= 0f)
                probeRadius = _cc.radius * 0.9f;
        }
    }
}
