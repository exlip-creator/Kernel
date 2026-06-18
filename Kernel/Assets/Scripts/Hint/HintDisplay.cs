using TMPro;
using UnityEngine;

public sealed class HintDisplay : MonoBehaviour
{
    public static HintDisplay instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI hintText;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Hide();
    }

    public void Show(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Hide();
            return;
        }

        hintText.text = message;
        panel.SetActive(true);
    }

    public void Hide() => panel.SetActive(false);
    
}
