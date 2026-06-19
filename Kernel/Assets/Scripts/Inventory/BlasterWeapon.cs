using Kernel.Combat;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class BlasterWeapon : MonoBehaviour
{
    [Header("Инвентарь")]
    [SerializeField] private Basics inventory;
    [SerializeField] private bool findInventoryInSceneIfMissing = true;
    [SerializeField] private ItemData blasterItem;
    [SerializeField] private bool matchByIdFallback = true;

    [Header("Прицеливание")]
    [SerializeField] private Camera aimCamera;
    [Tooltip("Точка на дуле (позиция выхода луча). Направление задаётся полем ниже — у многих моделей ствол смотрит не по transform.forward.")]
    [SerializeField] private Transform muzzle;
    [Tooltip("Направление выстрела в локальных осях Muzzle. (0,0,1) — как синяя ось Gizmo. Если летит в робота — попробуй (0,0,-1) или (1,0,0).")]
    [SerializeField] private Vector3 shotDirectionInMuzzleSpace = new Vector3(0f, 0f, 1f);
    [SerializeField] private float muzzleForwardBias = 0.08f;

    [Header("Чьи коллайдеры игнорировать")]
    [Tooltip("По умолчанию — объект с CharacterController выше по иерархии (робот). Луч и снаряд не нанесут себе урон.")]
    [SerializeField] private Transform ownerHitIgnoreRoot;
    [SerializeField] private bool skipHitsOnOwner = true;

    [Header("Выстрел")]
    [SerializeField] private BlasterBolt projectilePrefab;
    [SerializeField] private float range = 80f;
    [SerializeField] private float damage = 18f;
    [SerializeField] private float fireCooldown = 0.18f;
    [SerializeField] private LayerMask hitMask = ~0;
    [Tooltip("Hitscan: луч начинает чуть сзади точки выстрела — не «пролетает» сквозь цель в упор.")]
    [SerializeField] private float hitscanOriginBackOffset = 0.45f;

    [Header("Звук (опционально)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fireClip;

    [Header("Частицы (опционально)")]
    [Tooltip("Дочерний Particle System на Muzzle: Looping и Play On Awake выключены.")]
    [SerializeField] private ParticleSystem muzzleParticles;

    [Header("Отладка")]
    [SerializeField] private bool drawDebugRay;

    private float _nextFireTime;

    private void Awake()
    {
        if (aimCamera == null) aimCamera = Camera.main;
        ResolveOwnerHitIgnoreRoot();
    }

    private void ResolveOwnerHitIgnoreRoot()
    {
        if (ownerHitIgnoreRoot != null) return;
        var cc = GetComponentInParent<CharacterController>();
        if (cc != null)
            ownerHitIgnoreRoot = cc.transform;
        else
            ownerHitIgnoreRoot = transform.root;
    }

    private void Update()
    {
        ResolveInventory();
        if (!BlasterEquipped()) return;
        if (Time.time < _nextFireTime) return;
        if (!GetFireDown()) return;

        Fire();
        _nextFireTime = Time.time + fireCooldown;
    }

    private void ResolveInventory()
    {
        if (inventory != null) return;
        inventory = GetComponentInParent<Basics>();
        if (inventory == null && findInventoryInSceneIfMissing)
            inventory = Object.FindAnyObjectByType<Basics>();
    }

    private bool BlasterEquipped()
    {
        if (inventory == null || blasterItem == null) return false;

        var selected = inventory.selectedItem;
        if (selected == null) return false;
        if (selected == blasterItem) return true;
        return matchByIdFallback && selected.id == blasterItem.id;
    }

    private void Fire()
    {
        ResolveOwnerHitIgnoreRoot();

        Vector3 origin;
        Vector3 direction;

        if (muzzle != null)
        {
            direction = MuzzleToWorldDirection(muzzle, shotDirectionInMuzzleSpace);
            origin = muzzle.position + direction * muzzleForwardBias;
        }
        else if (aimCamera != null)
        {
            Ray r = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            origin = r.origin;
            direction = r.direction;
        }
        else
        {
            return;
        }

        if (projectilePrefab != null)
        {
            BlasterBolt bolt = Instantiate(projectilePrefab);
            bolt.Launch(origin, direction, damage, hitMask, range, skipHitsOnOwner ? ownerHitIgnoreRoot : null);
        }
        else if (TryHitscan(origin, direction, range, out RaycastHit hit))
        {
            Health hp = hit.collider.GetComponentInParent<Health>();
            if (hp != null)
                hp.TakeDamage(damage);

            if (drawDebugRay)
                Debug.DrawLine(origin, hit.point, Color.yellow, 0.15f);
        }

        PlayFireSound(origin);

        if (muzzleParticles != null)
            muzzleParticles.Play();

        if (drawDebugRay)
            Debug.DrawRay(origin, direction * range, Color.cyan, 0.25f);
    }

    private bool TryHitscan(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit bestHit)
    {
        bestHit = default;

        float back = Mathf.Max(0f, hitscanOriginBackOffset);
        Vector3 rayStart = origin - direction * back;
        float rayLen = maxDistance + back;

        RaycastHit[] hits = Physics.RaycastAll(rayStart, direction, rayLen, hitMask, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0) return false;
        if (hits.Length > 1)
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        const float rayEpsilon = 0.02f;
        foreach (RaycastHit h in hits)
        {
            if (h.distance + rayEpsilon < back)
                continue;

            if (skipHitsOnOwner && IsUnderOwner(h.collider))
                continue;

            bestHit = h;
            return true;
        }

        return false;
    }

    private bool IsUnderOwner(Collider c)
    {
        if (c == null || ownerHitIgnoreRoot == null) return false;
        return c.transform == ownerHitIgnoreRoot || c.transform.IsChildOf(ownerHitIgnoreRoot);
    }

    private static Vector3 MuzzleToWorldDirection(Transform muzzle, Vector3 localDirection)
    {
        Vector3 d = muzzle.TransformDirection(localDirection);
        if (d.sqrMagnitude < 1e-8f)
            d = muzzle.forward;
        return d.normalized;
    }

    private void PlayFireSound(Vector3 position)
    {
        if (fireClip == null)
            return;

        if (audioSource != null)
            audioSource.PlayOneShot(fireClip);
        else
            AudioSource.PlayClipAtPoint(fireClip, position);
    }

    private static bool GetFireDown()
    {
        if (Input.GetMouseButtonDown(0))
            return true;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
#endif

        return false;
    }
}
