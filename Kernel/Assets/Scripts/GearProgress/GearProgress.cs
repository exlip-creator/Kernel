using TMPro;
using UnityEngine;

public class GearProgress : MonoBehaviour
{
    public static GearProgress Instance { get; private set; }

    [SerializeField] private int total = 7;
    [SerializeField] private TextMeshProUGUI counterText;

    public int Total => total;
    public int Collected { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        RefreshHud();
    }

    public string GetProgressText()
    {
        return $"Шестеренки: {Collected}/{Total}";
    }

    private void RefreshHud()
    {
        if (counterText != null)
            counterText.text = GetProgressText();
    }

    [SerializeField] private SlidingDoor gearDoor;
    public bool TryCollect()
    {
        if (Collected >= Total) return false;
        Collected++;
        RefreshHud();
        if (Collected >= Total && gearDoor != null)
            gearDoor.Open();
        return true;
    }
}