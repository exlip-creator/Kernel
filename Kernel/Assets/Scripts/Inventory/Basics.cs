using UnityEngine;
using UnityEngine.UI;
using System;

[CreateAssetMenu(menuName = "Inventory/Item", fileName = "NewItem")]
public class ItemData : ScriptableObject
{
    public int id;
    public string itemName;
    public Sprite icon;
}

public class Basics : MonoBehaviour
{
    [Serializable]
    private class SlotUI
    {
        public Image background;
        public Image icon;
        public Image selectionFrame;
    }

    [Header("UI Slots")]
    [SerializeField] private SlotUI[] uiSlots = new SlotUI[5];

    [Header("Empty Slot Visuals")]
    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Color emptySlotColor = Color.white;

    [Header("Selection Visuals")]
    [SerializeField] private bool hideSelectionFrameWhenNotSelected = true;

    [Header("Strating item")]
    [SerializeField] private ItemData startingItem;

    private ItemData[] _slots;
    private int _selectedSlotIndex = 0;
    public ItemData selectedItem => (_slots == null || _slots.Length == 0) ? null : _slots[_selectedSlotIndex];

    private void Awake()
    {
        _slots = new ItemData[uiSlots.Length];
    }

    private void Start()
    {
        AddItem(startingItem);
        RefreshUI();
        RefreshSelectUI();
    }

    private void Update()
    {
        HandleSelectedSlot();
    }

    private void HandleSelectedSlot()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
        else if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);
    }

    public void SelectSlot(int index)
    {
        if (_slots == null) return;
        if (index < 0 || index >= _slots.Length) return;
        _selectedSlotIndex = index;
        RefreshSelectUI();
    }

    public bool AddItem(ItemData item)
    {
        if (item == null) return false;
        if (_slots == null) return false;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
            {
                _slots[i] = item;
                RefreshUI();
                return true;
            }
        }

        return false;
    }

    private void RefreshUI()
    {
        if (_slots == null) return;

        for (int i = 0; i < uiSlots.Length; i++)
        {
            var slot = uiSlots[i];
            var item = _slots[i];

            if (slot == null) continue;

            if (slot.background != null)
            {
                slot.background.sprite = emptySlotSprite;
                slot.background.color = emptySlotColor;
            }

            if (slot.icon != null)
            {
                if (item != null && item.icon != null)
                {
                    slot.icon.enabled = true;
                    slot.icon.sprite = item.icon;
                    slot.icon.color = Color.white;
                }
                else
                {
                    slot.icon.enabled = false;
                    slot.icon.sprite = null;
                }
            }
        }
    }

    private void RefreshSelectUI()
    {
        if (_slots == null) return;

        for (int i = 0; i < uiSlots.Length; i++)
        {
            var slot = uiSlots[i];
            if (slot == null || slot.selectionFrame == null) continue;

            bool isSelected = i == _selectedSlotIndex;
            if (hideSelectionFrameWhenNotSelected)
            {
                slot.selectionFrame.enabled = isSelected;
            }
            else
            {
                slot.selectionFrame.enabled = true;
                slot.selectionFrame.color = isSelected ? Color.white : new Color(1f, 1f, 1f, 0f);
            }
        }
    }
}