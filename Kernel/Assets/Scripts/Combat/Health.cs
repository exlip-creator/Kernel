using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kernel.Combat
{
    public sealed class Health : MonoBehaviour
    {
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private float currentHp = 100f;

        [Header("Death")]
        [SerializeField] private bool restartSceneOnDeath = true;
        [SerializeField] private float restartDelaySeconds = 1.75f;
        [SerializeField] private float fallImpulse = 1.5f;

        [Header("Смерть врага")]
        [Tooltip("Уничтожить объект без рэгдолла и без перезапуска сцены (для врагов с бластером).")]
        [SerializeField] private bool simpleDeath;
        [SerializeField] private float simpleDeathDestroyDelay;

        public float MaxHp => maxHp;
        public float CurrentHp => currentHp;
        public bool IsDead => _dead;

        public event System.Action<float, float> HpChanged;
        public event System.Action Died;

        private bool _dead;

        private void OnValidate()
        {
            maxHp = Mathf.Max(1f, maxHp);
            currentHp = Mathf.Clamp(currentHp, 0f, maxHp);
            restartDelaySeconds = Mathf.Max(0f, restartDelaySeconds);
            fallImpulse = Mathf.Max(0f, fallImpulse);
            simpleDeathDestroyDelay = Mathf.Max(0f, simpleDeathDestroyDelay);
        }

        private void Awake()
        {
            maxHp = Mathf.Max(1f, maxHp);
            currentHp = Mathf.Clamp(currentHp, 0f, maxHp);
            HpChanged?.Invoke(currentHp, maxHp);
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || currentHp <= 0f || _dead)
                return;

            currentHp = Mathf.Max(0f, currentHp - amount);
            HpChanged?.Invoke(currentHp, maxHp);
            if (currentHp <= 0f)
                Die();
        }

        private void Die()
        {
            _dead = true;
            Died?.Invoke();

            if (simpleDeath)
            {
                StartCoroutine(DestroySelfAfterSimpleDeathDelay());
                return;
            }

            var controller = GetComponent<CharacterController>();
            var thirdPerson = GetComponent<StarterAssets.ThirdPersonController>();
            var inputs = GetComponent<StarterAssets.StarterAssetsInputs>();
            var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();

            if (thirdPerson != null) thirdPerson.enabled = false;
            if (inputs != null) inputs.enabled = false;
            if (playerInput != null) playerInput.enabled = false;

            float capsuleHeight = 1.8f;
            float capsuleRadius = 0.28f;
            Vector3 capsuleCenter = new(0f, 0.9f, 0f);

            if (controller != null)
            {
                capsuleHeight = controller.height;
                capsuleRadius = controller.radius;
                capsuleCenter = controller.center;
                controller.enabled = false;
            }

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();

            rb.useGravity = true;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            CapsuleCollider col = GetComponent<CapsuleCollider>();
            if (col == null)
                col = gameObject.AddComponent<CapsuleCollider>();

            col.direction = 1; 
            col.height = capsuleHeight;
            col.radius = capsuleRadius;
            col.center = capsuleCenter;

            if (fallImpulse > 0f)
                rb.AddForce(Vector3.down * fallImpulse, ForceMode.Impulse);

            if (restartSceneOnDeath)
                StartCoroutine(RestartSceneAfterDelay());
        }

        private IEnumerator RestartSceneAfterDelay()
        {
            if (restartDelaySeconds > 0f)
                yield return new WaitForSeconds(restartDelaySeconds);

            Scene active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex);
        }

        private IEnumerator DestroySelfAfterSimpleDeathDelay()
        {
            if (simpleDeathDestroyDelay > 0f)
                yield return new WaitForSeconds(simpleDeathDestroyDelay);

            Destroy(gameObject);
        }
    }
}
