using Kernel.Combat;
using UnityEngine;

/// <summary>
/// Летит по направлению, попадание через короткий Raycast. Не толкает робота: Rigidbody kinematic, коллайдеры в trigger.
/// Пропускает попадания в <see cref="_ignoreHitsRoot"/> (тело стрелка).
/// </summary>
public class BlasterBolt : MonoBehaviour
{
    [SerializeField] private float speed = 75f;
    [SerializeField] private float defaultMaxDistance = 100f;

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

        RaycastHit[] hits = Physics.RaycastAll(o, _direction, step, _hitMask, QueryTriggerInteraction.Ignore);
        if (hits.Length > 1)
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (ShouldIgnoreHit(hit.collider))
                continue;

            Health hp = hit.collider.GetComponentInParent<Health>();
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
}
