using UnityEngine;

namespace Kernel.Combat
{
    /// <summary>
    /// Тестовый босс-паук: dormant до активации арены, затем преследование и атаки.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class SpiderBossAI : MonoBehaviour
    {
        private enum BossState
        {
            Dormant,
            Chase,
            Attack,
            Dead
        }

        [Header("Цель")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private Transform targetOverride;

        [Header("Движение")]
        [SerializeField] private float moveSpeed = 2.2f;
        [SerializeField] private float rotateSpeed = 360f;
        [SerializeField] private float stoppingDistance = 2.2f;
        [SerializeField] private float gravity = -18f;

        [Header("Атака")]
        [SerializeField] private float attackRange = 2.8f;
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

        public bool CombatActive => _state != BossState.Dormant && _state != BossState.Dead;

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

            if (animator == null)
                Debug.LogWarning($"{nameof(SpiderBossAI)} on '{name}': Animator not found. Assign it on BlackWidowVisual or add Animator + SpiderBoss controller.", this);
            else if (animator.runtimeAnimatorController == null)
                Debug.LogWarning($"{nameof(SpiderBossAI)} on '{name}': Animator has no RuntimeAnimatorController. Assign Assets/Spiders/Animations/SpiderBoss.controller.", animator);
        }

        private void Start()
        {
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
            ResolveTarget();
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

            if (dist <= attackRange && Time.time >= _nextAttackTime)
            {
                BeginAttack();
                return;
            }

            ApplyGravity();
            float speedParam = 0f;

            if (dist > stoppingDistance && dist > 0.02f)
            {
                Vector3 dir = flatDelta / dist;
                Quaternion look = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, rotateSpeed * Time.deltaTime);

                Vector3 move = dir * moveSpeed;
                move.y = _verticalVelocity;
                _cc.Move(move * Time.deltaTime);
                speedParam = 1f;
            }
            else
            {
                _cc.Move(new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);
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

        private void UpdateAttack()
        {
            ApplyGravity();
            _cc.Move(new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);

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

        private void ApplyGravity()
        {
            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += gravity * Time.deltaTime;
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
            if (animator != null)
                animator.SetFloat(_animatorSpeedHash, speed);
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

        // Animation Events (опционально на клипах атаки)
        public void OnAttackHit() => attackHitbox?.SetActive(true);
        public void OnAttackEnd() => attackHitbox?.SetActive(false);
    }
}
