using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private ItemData item;

    [Header("Pickup")]
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private int startSlotIndex = 1;

    [Tooltip("Максимальное расстояние до игрока для подсказки и нажатия E (метры). Триггер может быть больше — подобрать всё равно нельзя, пока не подойдёшь близко.")]
    [SerializeField] private float maxPickupDistance = 1.4f;

    [Tooltip("Если инвентарь (Basics) висит на UI/отдельном объекте — укажи его сюда. Иначе скрипт ищет Basics на игроке или первый в сцене.")]
    [SerializeField] private Basics inventoryOverride;

    [SerializeField] private string playerTag = "Player";

    [Header("Prompt")]
    [SerializeField] private GameObject promptRoot;

    [Header("Spin")]
    [SerializeField] private bool spin = true;
    [SerializeField] private Vector3 spinDegreesPerSecond = new Vector3(0f, 90f, 0f);

    private Basics _nearbyInventory;
    private Transform _playerProximityTransform;
    private bool _inPickupRange;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void OnEnable()
    {
        SetPromptVisible(false);
        _inPickupRange = false;
        RefreshNearbyPlayer();
    }

    private void Update()
    {
        if (spin)
            transform.Rotate(spinDegreesPerSecond * Time.deltaTime, Space.World);

        if (_nearbyInventory == null || _playerProximityTransform == null)
            RefreshNearbyPlayer();

        UpdatePickupRange();

        if (_nearbyInventory == null || !_inPickupRange) return;
        if (!Input.GetKeyDown(pickupKey)) return;
        if (item == null) return;

        bool added = _nearbyInventory.AddItem(item, startSlotIndex);
        if (!added) return;

        SetPromptVisible(false);
        Destroy(gameObject);
    }

    private void UpdatePickupRange()
    {
        if (_nearbyInventory == null || _playerProximityTransform == null)
        {
            if (_inPickupRange)
            {
                _inPickupRange = false;
                SetPromptVisible(false);
            }

            return;
        }

        float d = GetDistanceToPlayer(_playerProximityTransform);
        float maxD = Mathf.Max(0.05f, maxPickupDistance);

        if (d > maxD * 3f)
        {
            _nearbyInventory = null;
            _playerProximityTransform = null;
            _inPickupRange = false;
            SetPromptVisible(false);
            return;
        }

        bool ok = d <= maxD;
        if (ok == _inPickupRange) return;

        _inPickupRange = ok;
        SetPromptVisible(ok);
    }

    private float GetDistanceToPlayer(Transform player)
    {
        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            Vector3 playerCenter = cc.transform.TransformPoint(cc.center);
            return Vector3.Distance(GetPickupPoint(), playerCenter);
        }

        return Vector3.Distance(GetPickupPoint(), player.position);
    }

    private Vector3 GetPickupPoint()
    {
        var col = GetComponent<Collider>();
        if (col != null)
            return col.bounds.center;

        return transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryRegisterPlayerCollider(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (_nearbyInventory != null && _playerProximityTransform != null) return;
        TryRegisterPlayerCollider(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (_playerProximityTransform == null) return;

        var cc = other.GetComponentInParent<CharacterController>();
        if (cc != null && cc.transform == _playerProximityTransform)
        {
            _nearbyInventory = null;
            _playerProximityTransform = null;
            _inPickupRange = false;
            SetPromptVisible(false);
        }
    }

    private void TryRegisterPlayerCollider(Collider other)
    {
        if (!TryResolveInventory(other, out var inv)) return;

        Transform player = ResolvePlayerTransform(other);
        if (player == null) return;

        _nearbyInventory = inv;
        _playerProximityTransform = player;
        UpdatePickupRange();
    }

    private void RefreshNearbyPlayer()
    {
        if (!TryResolveInventory(null, out var inv)) return;

        CharacterController[] controllers = Object.FindObjectsByType<CharacterController>();
        float maxD = Mathf.Max(0.05f, maxPickupDistance);
        float triggerReach = GetTriggerReach();

        CharacterController best = null;
        float bestDist = float.MaxValue;

        foreach (CharacterController cc in controllers)
        {
            if (cc == null || !cc.enabled || !cc.gameObject.activeInHierarchy)
                continue;

            if (!string.IsNullOrEmpty(playerTag) && !cc.CompareTag(playerTag))
                continue;

            float dist = Vector3.Distance(GetPickupPoint(), cc.transform.TransformPoint(cc.center));
            if (dist > triggerReach + maxD)
                continue;

            if (dist < bestDist)
            {
                bestDist = dist;
                best = cc;
            }
        }

        if (best == null) return;

        _nearbyInventory = inv;
        _playerProximityTransform = best.transform;
        UpdatePickupRange();
    }

    private float GetTriggerReach()
    {
        if (TryGetComponent<SphereCollider>(out var sphere))
        {
            float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            return sphere.radius * scale;
        }

        var col = GetComponent<Collider>();
        if (col != null)
            return col.bounds.extents.magnitude;

        return 0.5f;
    }

    private static Transform ResolvePlayerTransform(Collider other)
    {
        if (other == null) return null;

        var cc = other.GetComponentInParent<CharacterController>();
        if (cc != null) return cc.transform;

        if (other.CompareTag("Player"))
            return other.transform.root;

        var root = other.transform.root;
        if (root != null) return root;

        return other.transform;
    }

    private bool TryResolveInventory(Collider other, out Basics inv)
    {
        if (inventoryOverride != null)
        {
            inv = inventoryOverride;
            return true;
        }

        if (other != null)
        {
            inv = other.GetComponentInParent<Basics>();
            if (inv != null) return true;

            inv = other.GetComponent<Basics>();
            if (inv != null) return true;
        }

        inv = Object.FindAnyObjectByType<Basics>();
        return inv != null;
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptRoot != null) promptRoot.SetActive(visible);
    }
}
