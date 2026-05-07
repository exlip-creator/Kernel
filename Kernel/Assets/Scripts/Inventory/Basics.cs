using UnityEngine;
using UnityEngine.UI;
using System;

[Serializable]
public class ItemData
{
    public int id;
    public string itemName;
    public Sprite icon;
}

public class Basics : MonoBehaviour
{
    [Header("Images")]
    [SerializeField] private Image[] slotImages = new Image[5];
    [SerializeField] private Color baseColor = new Color(1, 1, 1, 0.15f);
    [SerializeField] private Color selectedColor = Color.lightSkyBlue;

    private ItemData[] _slots = new ItemData[5];
    private int _selectedSlotIndex = 0;
    public ItemData selectedItem => _slots[_selectedSlotIndex];

    private void Start()
    {
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
        if (index < 0 || index >= _slots.Length) return;
        _selectedSlotIndex = index;
        RefreshSelectUI();
    }

    public bool AddItem(ItemData item)
    {
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
        for (int i = 0; i < slotImages.Length; i++)
        {
            if (_slots[i] != null)
            {
                slotImages[i].sprite = _slots[i].icon;
                slotImages[i].color = baseColor; 
            }
            else
            {
                slotImages[i].sprite = null;
                slotImages[i].color = baseColor;
            }
        }
    }

    private void RefreshSelectUI()
    {
        for (int i = 0; i < slotImages.Length; i++)
        {
            if (i == _selectedSlotIndex)
            {
                slotImages[i].color = selectedColor;
            }
            else
            {
                slotImages[i].color = baseColor;
            }
        }
    }
}
