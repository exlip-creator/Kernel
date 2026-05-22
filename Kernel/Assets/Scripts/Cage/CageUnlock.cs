using System.Collections;
using UnityEngine;


public sealed class CageUnlock : MonoBehaviour
{
    [Header("Key")]
    [SerializeField] private ItemData requiredKey;
    [SerializeField] private bool matchKeyById = true;
    [SerializeField] private Basics inventory;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Максимальная дистанция до точки взаимодействия (метры).")]
    [SerializeField] private float maxInteractDistance = 4f;
    [SerializeField] private bool openOnlyOnce = true;

    [Header("Doors")]
    [SerializeField] private Transform doorPivotLeft;
    [SerializeField] private Transform doorPivotRight;
    [SerializeField] private float openAngleDegrees = 90f;
    [SerializeField] private float openDuration = 0.55f;

    private static readonly string LeftPivotName = "cage_turnable_door_left";
    private static readonly string RightPivotName = "cage_turnable_door_right";

    private bool _inInteractRange;
    private Transform _playerTransform;
    private bool _isOpen;
    private bool _isOpening;

    public bool IsOpen => _isOpen;
    public event System.Action DoorsOpened;

    private void Reset()
    {
        EnsureTriggerCollider();
    }

    private void Awake()
    {
        if (inventory == null)
            inventory = Object.FindAnyObjectByType<Basics>();

        EnsureTriggerCollider();
        EnsureKinematicRigidbody();
    }

    private void Start()
    {
        ResolveDoorPivots();
    }

    private void Update()
    {
        if (_isOpen || _isOpening) return;

        RefreshPlayerProximity();

        if (!_inInteractRange || _playerTransform == null) return;
        if (!Input.GetKeyDown(interactKey)) return;
        if (!HasRequiredKeySelected()) return;

        Open();
    }

    public bool TryInteract()
    {
        if (_isOpen || _isOpening) return false;
        if (!HasRequiredKeySelected()) return false;

        RefreshPlayerProximity();
        if (!_inInteractRange) return false;

        Open();
        return true;
    }

    public void Open()
    {
        if (openOnlyOnce && _isOpen) return;
        if (_isOpening) return;

        ResolveDoorPivots();
        if (doorPivotLeft == null && doorPivotRight == null) return;

        _isOpening = true;
        StartCoroutine(OpenDoorsRoutine());
    }

    private IEnumerator OpenDoorsRoutine()
    {

        if (doorPivotLeft != null)
            StartCoroutine(RotatePivotAroundHinge(doorPivotLeft, -openAngleDegrees));
        if (doorPivotRight != null)
            StartCoroutine(RotatePivotAroundHinge(doorPivotRight, -openAngleDegrees));

        yield return new WaitForSeconds(openDuration);

        _isOpen = true;
        _isOpening = false;
        DoorsOpened?.Invoke();
    }

    private IEnumerator RotatePivotAroundHinge(Transform pivot, float angle)
    {
        Vector3 axis = pivot.up;
        Vector3 point = pivot.position;
        float target = Mathf.Abs(angle);
        float sign = Mathf.Sign(angle);
        float rotated = 0f;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, openDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float totalSoFar = target * Mathf.Clamp01(elapsed / duration);
            float step = totalSoFar - rotated;

            if (step > 0f)
            {
                pivot.RotateAround(point, axis, step * sign);
                rotated += step;
            }

            yield return null;
        }

        float left = target - rotated;
        if (left > 0f)
            pivot.RotateAround(point, axis, left * sign);
    }

    private bool HasRequiredKeySelected()
    {
        if (requiredKey == null || inventory == null) return false;

        ItemData selected = inventory.selectedItem;
        if (selected == null) return false;
        if (selected == requiredKey) return true;

        return matchKeyById && selected.id == requiredKey.id;
    }

    private void RefreshPlayerProximity()
    {
        CharacterController[] controllers =
            Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None);

        float maxD = Mathf.Max(0.5f, maxInteractDistance);
        Vector3 interactPoint = GetInteractionPoint();

        CharacterController best = null;
        float bestDist = float.MaxValue;

        foreach (CharacterController cc in controllers)
        {
            if (cc == null || !cc.enabled || !cc.gameObject.activeInHierarchy)
                continue;

            if (!string.IsNullOrEmpty(playerTag) && !cc.CompareTag(playerTag))
                continue;

            Vector3 playerCenter = cc.transform.TransformPoint(cc.center);
            float dist = Vector3.Distance(interactPoint, playerCenter);
            if (dist > maxD)
                continue;

            if (dist < bestDist)
            {
                bestDist = dist;
                best = cc;
            }
        }

        if (best == null)
        {
            _inInteractRange = false;
            _playerTransform = null;
            return;
        }

        _inInteractRange = true;
        _playerTransform = best.transform;
    }

    private Vector3 GetInteractionPoint()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            return col.bounds.center;

        if (doorPivotLeft != null && doorPivotRight != null)
            return (doorPivotLeft.position + doorPivotRight.position) * 0.5f;

        return transform.position;
    }

    private void ResolveDoorPivots()
    {
        if (doorPivotLeft == null)
            doorPivotLeft = FindChildRecursive(transform, LeftPivotName);
        if (doorPivotRight == null)
            doorPivotRight = FindChildRecursive(transform, RightPivotName);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryRegisterPlayerCollider(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryRegisterPlayerCollider(other);
    }

    private void OnTriggerExit(Collider other)
    {
        CharacterController cc = other.GetComponentInParent<CharacterController>();
        if (cc == null || _playerTransform == null) return;
        if (cc.transform != _playerTransform) return;

        _inInteractRange = false;
        _playerTransform = null;
    }

    private void TryRegisterPlayerCollider(Collider other)
    {
        CharacterController cc = other.GetComponentInParent<CharacterController>();
        if (cc == null) return;
        if (!string.IsNullOrEmpty(playerTag) && !cc.CompareTag(playerTag)) return;

        _playerTransform = cc.transform;

        Vector3 playerCenter = cc.transform.TransformPoint(cc.center);
        float maxD = Mathf.Max(0.5f, maxInteractDistance);
        _inInteractRange = Vector3.Distance(GetInteractionPoint(), playerCenter) <= maxD;
    }

    private void EnsureTriggerCollider()
    {
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(4f, 3.5f, 4f);
            box.center = new Vector3(-0.55f, 1.35f, -4.75f);
            return;
        }

        collider.isTrigger = true;
    }

    private void EnsureKinematicRigidbody()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent.name == childName) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName) return child;

            Transform found = FindChildRecursive(child, childName);
            if (found != null) return found;
        }

        return null;
    }
}
