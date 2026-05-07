using UnityEngine;

public class MagneticGloves : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private Basics inventory;
    [SerializeField] private Rigidbody player;
    [SerializeField] private Camera cam;

    [Header("Gloves item")]
    [SerializeField] private int glovesID = 1;
    
    [Header("Raycast")]
    [SerializeField] private float maxDistance = 25.0f;
    [SerializeField] private LayerMask targetMask = ~0;

    [Header("Pull")]
    [SerializeField] private float pullAcceleration = 45.0f;
    [SerializeField] private float maxPullSpeed = 14.0f;

    private MagneticItem _target;
    private Transform _targetTransform;

    private void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!player) player = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (!GlovesSelected())
        {
            ClearTarget();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryPickTarget();
        }

        if (Input.GetMouseButtonDown(1))
        {
            ClearTarget();
        }
    }

    private void FixedUpdate()
    {
        if (_target == null || _targetTransform == null) return;

        Vector3 toTarget = _targetTransform.position - player.position;
        float dist = toTarget.magnitude;

        if (dist <= _target.stopDistance)
        {
            ClearTarget();
            return;
        }

        player.AddForce(toTarget.normalized * pullAcceleration, ForceMode.Acceleration);

        Vector3 vec = player.linearVelocity;
        Vector3 horizontalSpeed = new Vector3(vec.x, 0, vec.z);
        if (horizontalSpeed.magnitude > maxPullSpeed)
        {
            Vector3 clamped = horizontalSpeed.normalized * maxPullSpeed;
            player.linearVelocity = new Vector3(clamped.x, vec.y, clamped.z);
        }
    }

    private bool GlovesSelected()
    {
        if (!inventory) return false;

        return inventory.selectedItem.id == 1;
    }

    private void TryPickTarget()
    {
        if (!cam) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));
        if (!Physics.Raycast(
            ray, out RaycastHit hit, 
            maxDistance, targetMask,
            QueryTriggerInteraction.Ignore)
        ) {
            return;
        }

        var mt = hit.collider.GetComponentInParent<MagneticItem>();
        if (!mt) return;

        _target = mt;
        _targetTransform = mt.transform;
    }

    private void ClearTarget()
    {
        _target = null;
        _targetTransform = null;
    }
}
