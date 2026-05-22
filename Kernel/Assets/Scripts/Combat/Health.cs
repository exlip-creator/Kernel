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

        [Header("Звук")]
        [Tooltip("Если пусто — создаётся на этом объекте при смерти.")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip deathClip;
        [SerializeField] private AudioClip fallImpactClip;
        [SerializeField, Range(0f, 1f)] private float deathVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float fallImpactVolume = 1f;
        [Tooltip("Минимальная скорость удара для звука падения.")]
        [SerializeField] private float fallImpactMinSpeed = 2.5f;

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
            fallImpactMinSpeed = Mathf.Max(0f, fallImpactMinSpeed);
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
            PlayDeathSound();

            if (simpleDeath)
            {
                StartCoroutine(DestroySelfAfterSimpleDeathDelay());
                return;
            }

            // Disable common control scripts (safe even if missing).
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

            // Switch to physics so the character can fall.
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();

            rb.useGravity = true;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            CapsuleCollider col = GetComponent<CapsuleCollider>();
            if (col == null)
                col = gameObject.AddComponent<CapsuleCollider>();

            col.direction = 1; // Y axis
            col.height = capsuleHeight;
            col.radius = capsuleRadius;
            col.center = capsuleCenter;

            if (fallImpulse > 0f)
                rb.AddForce(Vector3.down * fallImpulse, ForceMode.Impulse);

            if (fallImpactClip != null)
            {
                DeathFallImpactSound impact = GetComponent<DeathFallImpactSound>();
                if (impact == null)
                    impact = gameObject.AddComponent<DeathFallImpactSound>();
                impact.Configure(fallImpactClip, fallImpactVolume, fallImpactMinSpeed, ResolveAudioSource());
            }

            if (restartSceneOnDeath)
                StartCoroutine(RestartSceneAfterDelay());
        }

        private AudioSource ResolveAudioSource()
        {
            if (audioSource != null)
                return audioSource;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            return audioSource;
        }

        private void PlayDeathSound()
        {
            if (deathClip == null)
                return;

            // Враги (simpleDeath) сразу Destroy — PlayOneShot на объекте обрывается.
            if (simpleDeath)
            {
                AudioSource.PlayClipAtPoint(deathClip, transform.position, deathVolume);
                return;
            }

            ResolveAudioSource().PlayOneShot(deathClip, deathVolume);
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
