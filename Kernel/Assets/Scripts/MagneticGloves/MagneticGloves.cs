using UnityEngine;
using StarterAssets;

public class MagneticGloves : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private Basics inventory;
    [SerializeField] private ThirdPersonController player;
    [SerializeField] private Camera cam;

    [Header("Gloves item")]
    [SerializeField] private int glovesID = 0;
    
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
        if (!player) player = GetComponent<ThirdPersonController>();
    }

    private void Update()
    {
        if (!GlovesSelected())
        {
            ClearTarget();
            return;
        }
        
        if (!player)
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
        if (!player) return;

        Vector3 toTarget = _targetTransform.position - player.transform.position;
        float dist = toTarget.magnitude;

        if (dist <= _target.stopDistance)
        {
            ClearTarget();
            return;
        }

        float desiredSpeed = Mathf.Min(maxPullSpeed, pullAcceleration * dist * Time.deltaTime);
        Vector3 pullVelocity = toTarget.normalized * desiredSpeed;
        player.ExternalLaunch(new Vector3(pullVelocity.x, pullVelocity.y, pullVelocity.z));
    }

    private bool GlovesSelected()
    {
        if (!inventory) return false;

        return inventory.selectedItem != null && inventory.selectedItem.id == glovesID;
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
