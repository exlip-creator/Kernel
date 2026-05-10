using UnityEngine;

public class ButtonPuzzle : MonoBehaviour
{
    [Header("Buttons array")]
    [SerializeField] private InteractiveButton[] buttons;

    [Header("Correct combination")]
    [SerializeField] private bool[] correctCombination = { true, false, false, true, false, true, true };

    [SerializeField] private HingeDoor door;

    private void Awake()
    {
        if (buttons == null || correctCombination == null) return;

        if (buttons.Length != correctCombination.Length)
        {
            Debug.LogError(
                $"Количество кнопок ({buttons.Length})" + 
                "не совпадает с комбинацией ({correctCombination.Length})"
                );
        }
    }

    public void RefreshPuzzle()
    {
        if (door == null || buttons == null || correctCombination == null) return;
        if (buttons.Length != correctCombination.Length) return;

        if (IsSolved())
        {
            door.Open();
        }
        else
        {
            door.Close();
        }
    }

    private bool IsSolved()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) return false;

            if (buttons[i].isActive != correctCombination[i]) return false;
        }

        return true;
    }
}
