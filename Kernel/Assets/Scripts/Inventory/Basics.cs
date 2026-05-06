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
    [SerializeField] private Image[] slotImages;
    [SerializeField] private Color baseColor = new Color(1, 1, 1, 0.15f);
    [SerializeField] private Color selectedColor = Color.lightSkyBlue;

    [Header("Data")]
    private ItemData[] slots = new ItemData[5];
    private int selectedSlotIndex = 0;
    private ItemData selectedItem => slots[selectedSlotIndex];

    public bool AddItem(ItemData item)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
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
            if (slots[i] != null)
            {
                slotImages[i].sprite = slots[i].icon;
                slotImages[i].color = Color.white; 
            }
            else
            {
                slotImages[i].sprite = null;
                slotImages[i].color = new Color(1, 1, 1, 0.15f);
            }
        }
    }
}
