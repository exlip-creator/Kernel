using UnityEngine;

namespace Kernel.Combat
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class SpiderBossAI : MonoBehaviour
    {
        private enum BossState
        {
            Dormant,
            Chase,
            Attack,
            Return,
            Dead
        }

        [Header("Цель")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private Transform targetOverride;

        [Header("Движение")]
        [SerializeField] private float moveSpeed = 2.2f;
        [SerializeField] private float rotateSpeed = 360f;
        [SerializeField] private float stoppingDistance = 2.2f;
        [SerializeField] private float returnArrivalDistance = 0.85f;
        [SerializeField] private float gravity = -18f;

        [Header("Земля")]
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float groundProbeUp = 1.5f;
        [SerializeField] private float groundProbeDown = 4f;

        [Header("Атака")]
        [SerializeField] private float attackRange = 2.8f;
        [SerializeField] private float attackReachPadding = 0.35f;
        [SerializeField] private float attackCooldown = 3f;
        [SerializeField] private float attackWindup = 0.6f;
        [SerializeField] private float attackActive = 0.35f;
        [SerializeField] private float attackRecovery = 0.8f;

        [Header("Ссылки")]
        [SerializeField] private AttackHitboxGate attackHitbox;
        [SerializeField] private Animator animator;

        private CharacterController _cc;
        private Health _health;
        private Transform _target;
        private BossState _state = BossState.Dormant;
        private float _verticalVelocity;
        private float _nextAttackTime;
        private float _attackPhaseTimer;
        private bool _hitboxActive;
        private int _animatorAttackHash;
        private int _animatorSpeedHash;
        private int _animatorDieHash;
        private int _animatorIdleStateHash;
        private int _animatorWalkStateHash;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private float _groundY;

        public bool CombatActive => _state != BossState.Dormant && _state != BossState.Dead && _state != BossState.Return;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _health = GetComponent<Health>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);

            if (attackHitbox == null)
                attackHitbox = GetComponentInChildren<AttackHitboxGate>();

            _animatorAttackHash = Animator.StringToHash("Attack");
            _animatorSpeedHash = Animator.StringToHash("Speed");
            _animatorDieHash = Animator.StringToHash("Die");
            _animatorIdleStateHash = Animator.StringToHash("Idle");
            _animatorWalkStateHash = Animator.StringToHash("Walk");

            if (animator == null)
                Debug.LogWarning($"{nameof(SpiderBossAI)} on '{name}': Animator not found. Assign it on BlackWidowVisual or add Animator + SpiderBoss controller.", this);
            else if (animator.runtimeAnimatorController == null)
                Debug.LogWarning($"{nameof(SpiderBossAI)} on '{name}': Animator has no RuntimeAnimatorController. Assign Assets/Spiders/Animations/SpiderBoss.controller.", animator);

            _cc.stepOffset = 0.15f;
            _verticalVelocity = -2f;
        }

        private void Start()
        {
            CaptureHomePose();
            SetRootY(_groundY);
            ResetLocomotionToIdle();

            if (_health != null)
                _health.HpChanged += OnHpChanged;
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.HpChanged -= OnHpChanged;
        }

        public void ActivateCombat()
        {
            if (_state == BossState.Dead)
                return;

            _state = BossState.Chase;
            _verticalVelocity = -2f;
            ResolveTarget();
        }

        public void DeactivateCombat()
        {
            if (_state == BossState.Dead || _state == BossState.Dormant || _state == BossState.Return)
                return;

            _hitboxActive = false;
            attackHitbox?.SetActive(false);
            _verticalVelocity = -2f;

            if (GetFlatDistanceToSpawn() <= GetReturnFinishDistance())
            {
                FinishReturn();
                return;
            }

            _state = BossState.Return;
        }

        private void LateUpdate()
        {
            if (_state != BossState.Dormant || animator == null)
                return;

            if (IsLocomotionWalkPlaying())
                ResetLocomotionToIdle();
        }

        private void Update()
        {
            if (_state == BossState.Dormant || _state == BossState.Dead)
                return;

            if (_health != null && _health.IsDead)
            {
                EnterDead();
                return;
            }

            if (_state == BossState.Return)
            {
                UpdateReturn();
                return;
            }

            if (_target == null)
                ResolveTarget();

            if (_target == null)
                return;

            switch (_state)
            {
                case BossState.Chase:
                    UpdateChase();
                    break;
                case BossState.Attack:
                    UpdateAttack();
                    break;
            }
        }

        private void UpdateChase()
        {
            Vector3 flatDelta = _target.position - transform.position;
            flatDelta.y = 0f;
            float dist = flatDelta.magnitude;

            if (dist <= GetEffectiveAttackRange() && Time.time >= _nextAttackTime)
            {
                BeginAttack();
                return;
            }

            float speedParam = 0f;

            if (dist > GetEffectiveStoppingDistance() && dist > 0.02f)
            {
                Vector3 dir = flatDelta / dist;
                Quaternion look = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, rotateSpeed * Time.deltaTime);

                MoveOnGround(dir * moveSpeed);
                speedParam = 1f;
            }
            else
            {
                MoveOnGround(Vector3.zero);
            }

            SetAnimatorSpeed(speedParam);
        }

        private void BeginAttack()
        {
            _state = BossState.Attack;
            _attackPhaseTimer = attackWindup + attackActive + attackRecovery;
            _nextAttackTime = Time.time + attackCooldown;
            SetAnimatorSpeed(0f);

            if (animator != null)
                animator.SetTrigger(_animatorAttackHash);

            Vector3 flatDelta = _target.position - transform.position;
            flatDelta.y = 0f;
            if (flatDelta.sqrMagnitude > 0.02f)
            {
                Quaternion look = Quaternion.LookRotation(flatDelta.normalized);
                transform.rotation = look;
            }
        }

        private void UpdateReturn()
        {
            float dist = GetFlatDistanceToSpawn();

            if (dist <= GetReturnFinishDistance())
            {
                FinishReturn();
                return;
            }

            Vector3 flatDelta = _spawnPosition - transform.position;
            flatDelta.y = 0f;
            Vector3 dir = flatDelta / dist;
            Quaternion look = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, rotateSpeed * Time.deltaTime);

            Vector3 before = transform.position;
            MoveOnGround(dir * moveSpeed);
            float moved = HorizontalDistance(before, transform.position);

            SetAnimatorSpeed(moved > 0.02f ? 1f : 0f);
        }

        private void FinishReturn()
        {
            Vector3 pos = _spawnPosition;
            if (TrySampleGroundY(_spawnPosition.x, _spawnPosition.z, _spawnPosition.y, out float rootY))
                pos.y = rootY;
            else
                pos.y = _groundY;

            _cc.enabled = false;
            transform.SetPositionAndRotation(pos, _spawnRotation);
            _cc.enabled = true;

            _verticalVelocity = -2f;
            _state = BossState.Dormant;
            ResetLocomotionToIdle();
        }

        private void UpdateAttack()
        {
            MoveOnGround(Vector3.zero);

            float elapsed = (attackWindup + attackActive + attackRecovery) - _attackPhaseTimer;

            if (!_hitboxActive && elapsed >= attackWindup)
            {
                _hitboxActive = true;
                attackHitbox?.SetActive(true);
            }

            if (_hitboxActive && elapsed >= attackWindup + attackActive)
            {
                _hitboxActive = false;
                attackHitbox?.SetActive(false);
            }

            _attackPhaseTimer -= Time.deltaTime;
            if (_attackPhaseTimer <= 0f)
            {
                _hitboxActive = false;
                attackHitbox?.SetActive(false);
                _state = BossState.Chase;
            }
        }

        private void MoveOnGround(Vector3 horizontalVelocity)
        {
            horizontalVelocity.y = 0f;
            if (horizontalVelocity.sqrMagnitude > 0.0001f)
                _cc.Move(horizontalVelocity * Time.deltaTime);

            ApplyGravity();
            _cc.Move(new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);
            ClampVerticalDrift();
        }

        private void ApplyGravity()
        {
            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += gravity * Time.deltaTime;
        }

        private float GetRootYForFeet(float feetY) => feetY - (_cc.center.y - _cc.height * 0.5f);

        private bool UsesLocalGroundSampling() =>
            _state == BossState.Chase || _state == BossState.Return || _state == BossState.Attack;

        private float GetGroundYAt(float x, float z)
        {
            float referenceY = transform.position.y;
            if (TrySampleGroundY(x, z, referenceY, out float rootY))
                return rootY;

            return _groundY;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private bool TrySampleGroundY(float x, float z, float referenceY, out float rootY)
        {
            rootY = 0f;
            Vector3 rayOrigin = new Vector3(x, referenceY + groundProbeUp, z);
            RaycastHit[] hits = Physics.RaycastAll(
                rayOrigin,
                Vector3.down,
                groundProbeUp + groundProbeDown,
                groundLayers,
                QueryTriggerInteraction.Ignore);

            float bestDistance = float.MaxValue;
            bool found = false;

            foreach (RaycastHit hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                if (hit.distance >= bestDistance)
                    continue;

                bestDistance = hit.distance;
                rootY = GetRootYForFeet(hit.point.y);
                found = true;
            }

            return found;
        }

        private void ClampVerticalDrift()
        {
            float targetY = UsesLocalGroundSampling()
                ? GetGroundYAt(transform.position.x, transform.position.z)
                : _groundY;

            float deltaY = targetY - transform.position.y;
            if (Mathf.Abs(deltaY) <= 0.02f)
            {
                if (_cc.isGrounded)
                    _verticalVelocity = -2f;
                return;
            }

            if (deltaY < -0.35f)
            {
                SetRootY(targetY);
                return;
            }

            _cc.Move(new Vector3(0f, deltaY, 0f));
            _verticalVelocity = -2f;
        }

        private void SetRootY(float rootY)
        {
            Vector3 pos = transform.position;
            pos.y = rootY;
            _cc.enabled = false;
            transform.position = pos;
            _cc.enabled = true;
            _verticalVelocity = -2f;
        }

        private void EnterDead()
        {
            _state = BossState.Dead;
            attackHitbox?.SetActive(false);
            SetAnimatorSpeed(0f);

            if (animator != null)
                animator.SetTrigger(_animatorDieHash);
        }

        private void OnHpChanged(float current, float max)
        {
            if (_state == BossState.Dead || _state == BossState.Dormant)
                return;

            if (current < max && _state != BossState.Attack && animator != null)
                animator.SetTrigger(Animator.StringToHash("Hit"));
        }

        private void SetAnimatorSpeed(float speed)
        {
            if (animator == null)
                return;

            animator.SetFloat(_animatorSpeedHash, speed, 0f, 0f);
        }

        private void CaptureHomePose()
        {
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            _groundY = _spawnPosition.y;

            if (TrySampleGroundY(_spawnPosition.x, _spawnPosition.z, _spawnPosition.y, out float sampledY))
                _groundY = sampledY;
        }

        private float GetFlatDistanceToSpawn()
        {
            Vector3 flatDelta = _spawnPosition - transform.position;
            flatDelta.y = 0f;
            return flatDelta.magnitude;
        }

        private float GetReturnFinishDistance() => Mathf.Max(returnArrivalDistance, _cc.radius * 0.35f);

        private float GetMinCenterDistance()
        {
            float bossRadius = GetScaledRadius(_cc, transform);
            float targetRadius = 0.5f;

            if (_target != null && _target.TryGetComponent(out CharacterController targetCc))
                targetRadius = GetScaledRadius(targetCc, _target);

            return bossRadius + targetRadius + _cc.skinWidth + 0.05f;
        }

        private static float GetScaledRadius(CharacterController cc, Transform owner)
        {
            Vector3 scale = owner.lossyScale;
            float horizontalScale = Mathf.Max(scale.x, scale.z);
            return cc.radius * horizontalScale;
        }

        private float GetEffectiveAttackRange()
        {
            float minCenter = GetMinCenterDistance();
            return Mathf.Max(attackRange, minCenter + attackReachPadding);
        }

        private float GetEffectiveStoppingDistance()
        {
            float minCenter = GetMinCenterDistance();
            float stop = Mathf.Max(stoppingDistance, minCenter - 0.1f);
            return Mathf.Min(stop, GetEffectiveAttackRange() - 0.05f);
        }

        private bool IsLocomotionWalkPlaying()
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            return info.shortNameHash == _animatorWalkStateHash;
        }

        private void ResetLocomotionToIdle()
        {
            if (animator == null)
                return;

            animator.SetFloat(_animatorSpeedHash, 0f);
            animator.ResetTrigger(_animatorAttackHash);
            animator.ResetTrigger(Animator.StringToHash("Hit"));

            if (animator.HasState(0, _animatorIdleStateHash))
                animator.Play(_animatorIdleStateHash, 0, 0f);

            animator.Update(0f);
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

        public void OnAttackHit() => attackHitbox?.SetActive(true);
        public void OnAttackEnd() => attackHitbox?.SetActive(false);
    }
}
