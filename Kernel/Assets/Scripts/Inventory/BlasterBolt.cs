using Kernel.Combat;
using UnityEngine;

public class BlasterBolt : MonoBehaviour
{
    [SerializeField] private float speed = 75f;
    [SerializeField] private float defaultMaxDistance = 100f;

    [Header("Ближний бой")]
    [Tooltip("Луч начинает чуть сзади — попадания, когда снаряд или дуло внутри коллайдера врага, не пропадают.")]
    [SerializeField] private float rayBackSkin = 0.45f;
    [Tooltip("Если луч не нашёл цель — проверка сферой по траектории (враг вплотную / только CharacterController).")]
    [SerializeField] private float closeProbeRadius = 0.22f;

    private float _damage;
    private LayerMask _hitMask;
    private Vector3 _direction;
    private float _travelled;
    private float _maxDistance;
    private Transform _ignoreHitsRoot;

    public void Launch(Vector3 origin, Vector3 direction, float damage, LayerMask hitMask, float maxDistance, Transform ignoreHitsRoot)
    {
        _ignoreHitsRoot = ignoreHitsRoot;
        DisablePhysicsPush();

        transform.position = origin;
        _direction = direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector3.forward;
        transform.rotation = Quaternion.LookRotation(_direction);
        _damage = damage;
        _hitMask = hitMask;
        _travelled = 0f;
        _maxDistance = maxDistance > 0f ? maxDistance : defaultMaxDistance;
    }

    private void DisablePhysicsPush()
    {
        foreach (var rb in GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (var c in GetComponentsInChildren<Collider>())
            c.isTrigger = true;
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        Vector3 o = transform.position;

        if (TryHitAlongStep(o, step, out Collider hitCol))
        {
            Health hp = hitCol.GetComponentInParent<Health>();
            if (hp != null)
                hp.TakeDamage(_damage);

            Destroy(gameObject);
            return;
        }

        transform.position += _direction * step;
        _travelled += step;
        if (_travelled >= _maxDistance)
            Destroy(gameObject);
    }

    private bool ShouldIgnoreHit(Collider c)
    {
        if (c == null || _ignoreHitsRoot == null) return false;
        return c.transform == _ignoreHitsRoot || c.transform.IsChildOf(_ignoreHitsRoot);
    }

    private bool TryHitAlongStep(Vector3 o, float step, out Collider hitCollider)
    {
        hitCollider = null;

        float back = Mathf.Max(0f, rayBackSkin);
        Vector3 rayStart = o - _direction * back;
        float rayLen = step + back;

        RaycastHit[] hits = Physics.RaycastAll(rayStart, _direction, rayLen, _hitMask, QueryTriggerInteraction.Ignore);
        if (hits.Length > 1)
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        const float rayEpsilon = 0.02f;
        foreach (RaycastHit hit in hits)
        {
            if (hit.distance + rayEpsilon < back)
                continue;

            if (ShouldIgnoreHit(hit.collider))
                continue;

            hitCollider = hit.collider;
            return true;
        }

        float r = Mathf.Max(0.05f, closeProbeRadius);
        if (TryProbeOverlapSphere(o, r, out hitCollider))
            return true;
        if (step > 1e-4f && TryProbeOverlapSphere(o + _direction * (step * 0.5f), r, out hitCollider))
            return true;
        if (step > 1e-4f && TryProbeOverlapSphere(o + _direction * step, r, out hitCollider))
            return true;

        return false;
    }

    private bool TryProbeOverlapSphere(Vector3 center, float radius, out Collider hitCollider)
    {
        hitCollider = null;
        Collider[] cols = Physics.OverlapSphere(center, radius, _hitMask, QueryTriggerInteraction.Ignore);
        if (cols.Length == 0)
            return false;

        float best = float.MaxValue;
        Collider bestCol = null;

        foreach (Collider c in cols)
        {
            if (c == null || ShouldIgnoreHit(c))
                continue;

            Health hp = c.GetComponentInParent<Health>();
            if (hp == null)
                continue;

            Vector3 p = c.bounds.ClosestPoint(center);
            float d = (p - center).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestCol = c;
            }
        }

        if (bestCol == null)
            return false;

        hitCollider = bestCol;
        return true;
    }
}
