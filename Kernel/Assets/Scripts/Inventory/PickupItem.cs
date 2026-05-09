using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private ItemData item;

    [Header("Pickup")]
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private int startSlotIndex = 1;

    [Tooltip("Если инвентарь (Basics) висит на UI/отдельном объекте — укажи его сюда. Иначе скрипт ищет Basics на игроке или первый в сцене.")]
    [SerializeField] private Basics inventoryOverride;

    [Header("Prompt")]
    [SerializeField] private GameObject promptRoot;

    [Header("Spin")]
    [SerializeField] private bool spin = true;
    [SerializeField] private Vector3 spinDegreesPerSecond = new Vector3(0f, 90f, 0f);

    private Basics _nearbyInventory;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        // С CharacterController у игрока часто нет Rigidbody — без Rigidbody на одной из сторон
        // Unity не шлёт события триггера. Кинематический RB на предмете это исправляет.
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void OnEnable()
    {
        SetPromptVisible(false);
    }

    private void Update()
    {
        if (spin)
        {
            transform.Rotate(spinDegreesPerSecond * Time.deltaTime, Space.World);
        }

        if (_nearbyInventory == null) return;
        if (!Input.GetKeyDown(pickupKey)) return;
        if (item == null) return;

        bool added = _nearbyInventory.AddItem(item, startSlotIndex);
        if (!added) return;

        SetPromptVisible(false);
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!TryResolveInventory(other, out var inv)) return;

        _nearbyInventory = inv;
        SetPromptVisible(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!TryResolveInventory(other, out var inv)) return;
        if (inv != _nearbyInventory) return;

        _nearbyInventory = null;
        SetPromptVisible(false);
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

