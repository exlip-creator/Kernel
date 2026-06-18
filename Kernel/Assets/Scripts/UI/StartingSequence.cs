using UnityEngine;
using StarterAssets;

public class StartScreenController : MonoBehaviour
{
    [SerializeField] private GameObject startScreen;
    [SerializeField] private StarterAssetsInputs starterAssetsInputs;
    [SerializeField] private ThirdPersonController thirdPersonController; 

    private void Start()
    {
        startScreen.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartGame()
    {
        startScreen.SetActive(false);
        starterAssetsInputs.enabled = true;
        thirdPersonController.enabled = true;
        
        starterAssetsInputs.SetCursorEnabled(false);
    }
}