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
    }

    private void Update()
    {
        if (spin)
        {
            transform.Rotate(spinDegreesPerSecond * Time.deltaTime, Space.World);
        }

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

        float d = Vector3.Distance(transform.position, _playerProximityTransform.position);
        float maxD = Mathf.Max(0.05f, maxPickupDistance);

        // Вышел из большого триггера или ушёл далеко — сбрасываем, чтобы не держать «залипший» контакт.
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

    private void OnTriggerEnter(Collider other)
    {
        if (!TryResolveInventory(other, out var inv)) return;

        _nearbyInventory = inv;
        _playerProximityTransform = ResolvePlayerTransform(other);
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

    private static Transform ResolvePlayerTransform(Collider other)
    {
        var cc = other.GetComponentInParent<CharacterController>();
        if (cc != null) return cc.transform;

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

        inv = other.GetComponentInParent<Basics>();
        if (inv != null) return true;

        inv = other.GetComponent<Basics>();
        if (inv != null) return true;

        inv = Object.FindFirstObjectByType<Basics>();
        return inv != null;
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptRoot != null) promptRoot.SetActive(visible);
    }
}
