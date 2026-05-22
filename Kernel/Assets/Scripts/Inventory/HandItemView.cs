using UnityEngine;

/// <summary>
/// Показывает modelRoot в руке, когда в инвентаре выбран нужный ItemData.
/// </summary>
public class HandItemView : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private Basics inventory;
    [Tooltip("Если Basics не на игроке, а на UI — оставь включённым: инвентарь ищется в сцене.")]
    [SerializeField] private bool findInventoryInSceneIfMissing = true;

    [Header("Item to show in hands")]
    [SerializeField] private ItemData item;
    [Tooltip("Если ItemData не совпал ссылкой (редко), можно включить сравнение по id.")]
    [SerializeField] private bool matchByIdFallback = true;
    [SerializeField] private GameObject modelRoot;

    [Header("Attach (Robot Kyle: Right_Hand)")]
    [Tooltip("Перетащи Transform кости правой кисти. Один раз в Awake modelRoot станет её ребёнком.")]
    [SerializeField] private Transform handBone;
    [SerializeField] private bool reparentModelToHandBone = true;
    [Tooltip("После перепривязки обнулить local pos/rot и scale=1 у modelRoot.")]
    [SerializeField] private bool resetLocalTransformAfterReparent = true;

    [Header("Grip (локально у modelRoot)")]
    [SerializeField] private bool applyGripPose = true;
    [SerializeField] private Vector3 localPosition;
    [SerializeField] private Vector3 localEuler;
    [SerializeField] private Vector3 localScale = Vector3.one;

    [Header("Поднять руку (кодом)")]
    [SerializeField] private Transform handRaiseBone;
    [SerializeField] private bool raiseHandWhenEquipped = false;
    [SerializeField] private Vector3 handRaiseLocalEuler = new Vector3(-35f, 10f, 8f);

    [Header("Animator (опционально)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string animatorBoolWhenEquipped = "";

    private bool _attached;
    private bool _equippedVisible;
    private int _animBoolHash;
    private bool _hasAnimBool;
    private bool _animatorHasBoolParam;
    private bool _scannedAnimatorForBoolParam;

    private void Awake()
    {
        CacheAnimatorBool();
        ResolveInventory();
        if (animator == null) animator = GetComponentInParent<Animator>();
        TryAttachToHand();
    }

    private void Start()
    {
        ResolveInventory();
        TryAttachToHand();
        Refresh();
    }

    private void CacheAnimatorBool()
    {
        _hasAnimBool = !string.IsNullOrEmpty(animatorBoolWhenEquipped);
        _animBoolHash = _hasAnimBool ? Animator.StringToHash(animatorBoolWhenEquipped) : 0;
    }

    private void ResolveInventory()
    {
        if (inventory != null) return;
        inventory = GetComponentInParent<Basics>();
        if (inventory == null && findInventoryInSceneIfMissing)
        {
            inventory = Object.FindAnyObjectByType<Basics>();
        }
    }

    private void TryAttachToHand()
    {
        if (_attached) return;
        if (!reparentModelToHandBone || handBone == null || modelRoot == null) return;

        if (modelRoot.transform.parent == handBone)
        {
            _attached = true;
            return;
        }

        modelRoot.transform.SetParent(handBone, worldPositionStays: false);
        if (resetLocalTransformAfterReparent)
        {
            modelRoot.transform.localPosition = Vector3.zero;
            modelRoot.transform.localRotation = Quaternion.identity;
            modelRoot.transform.localScale = Vector3.one;
        }

        _attached = true;
    }

    private void OnEnable()
    {
        Refresh();
        ApplyGripPoseNow();
    }

    private void OnValidate()
    {
        CacheAnimatorBool();
        ApplyGripPoseNow();
    }

    private void Update()
    {
        Refresh();
    }

    private void LateUpdate()
    {
        ApplyGripPoseNow();
        ApplyHandRaise();
    }

    private void ApplyGripPoseNow()
    {
        if (modelRoot == null || !applyGripPose) return;

        var t = modelRoot.transform;
        t.localPosition = localPosition;
        t.localRotation = Quaternion.Euler(localEuler);
        t.localScale = localScale;
    }

    private void ApplyHandRaise()
    {
        if (!raiseHandWhenEquipped || handRaiseBone == null) return;
        if (!_equippedVisible) return;

        handRaiseBone.localRotation = handRaiseBone.localRotation * Quaternion.Euler(handRaiseLocalEuler);
    }

    private void Refresh()
    {
        ResolveInventory();

        if (modelRoot == null || item == null)
        {
            _equippedVisible = false;
            if (modelRoot != null && modelRoot.activeSelf)
            {
                modelRoot.SetActive(false);
            }

            return;
        }

        bool shouldShow = false;
        if (inventory != null)
        {
            var selected = inventory.selectedItem;
            shouldShow = selected == item;
            if (!shouldShow && matchByIdFallback && selected != null)
            {
                shouldShow = selected.id == item.id;
            }
        }

        _equippedVisible = shouldShow;

        if (modelRoot.activeSelf != shouldShow)
        {
            modelRoot.SetActive(shouldShow);
        }

        SafeSetAnimatorBool(shouldShow);
    }

    private void SafeSetAnimatorBool(bool value)
    {
        if (animator == null || !_hasAnimBool) return;
        if (!animator.isActiveAndEnabled) return;
        if (animator.runtimeAnimatorController == null) return;

        if (!_scannedAnimatorForBoolParam && animator.runtimeAnimatorController != null)
        {
            _scannedAnimatorForBoolParam = true;
            foreach (var p in animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Bool && p.nameHash == _animBoolHash)
                {
                    _animatorHasBoolParam = true;
                    break;
                }
            }
        }

        if (!_animatorHasBoolParam) return;

        animator.SetBool(_animBoolHash, value);
    }
}
