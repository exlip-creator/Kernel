using System.Collections;
using Bit.Robot;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class EndingSequence : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private CageUnlock cage;
    [SerializeField] private float delayAfterDoorsOpenSeconds = 3f;

    [Header("UI")]
    [SerializeField] private GameObject endingScreen;

    [Header("Player")]
    [SerializeField] private GameObject playerRoot;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool unlockCursor = true;

    private bool _played;

    private void Awake()
    {
        if (cage == null)
            cage = Object.FindAnyObjectByType<CageUnlock>();

        if (endingScreen != null)
            endingScreen.SetActive(false);
    }

    private void OnEnable()
    {
        if (cage == null) return;

        cage.DoorsOpened += OnCageDoorsOpened;
        if (cage.IsOpen)
            OnCageDoorsOpened();
    }

    private void OnDisable()
    {
        if (cage != null)
            cage.DoorsOpened -= OnCageDoorsOpened;
    }

    private void OnCageDoorsOpened()
    {
        if (_played) return;
        _played = true;
        StartCoroutine(ShowEndingAfterDelay());
    }

    private IEnumerator ShowEndingAfterDelay()
    {
        if (delayAfterDoorsOpenSeconds > 0f)
            yield return new WaitForSeconds(delayAfterDoorsOpenSeconds);

        if (endingScreen != null)
            endingScreen.SetActive(true);

        LockPlayerActions();
    }

    private void LockPlayerActions()
    {
        GameObject player = ResolvePlayerRoot();
        if (player == null) return;

        DisableBehaviour<ThirdPersonController>(player);
        DisableBehaviour<StarterAssetsInputs>(player);
        DisableBehaviour<PlayerInput>(player);
        DisableCharacterControllers(player);
        DisableBehaviour<InteractionRaycaster>(player);
        DisableBehaviour<BlasterWeapon>(player);
        DisableBehaviour<MagneticGloves>(player);
        DisableBehaviour<HandItemView>(player);

        Basics inventory = Object.FindAnyObjectByType<Basics>();
        if (inventory != null)
            inventory.enabled = false;

        Camera main = Camera.main;
        if (main != null)
        {
            CameraFollow follow = main.GetComponent<CameraFollow>();
            if (follow != null)
                follow.enabled = false;
        }

        if (unlockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private GameObject ResolvePlayerRoot()
    {
        if (playerRoot != null)
            return playerRoot;

        if (!string.IsNullOrEmpty(playerTag))
        {
            GameObject tagged = GameObject.FindGameObjectWithTag(playerTag);
            if (tagged != null)
                return tagged;
        }

        CharacterController controller = Object.FindAnyObjectByType<CharacterController>();
        return controller != null ? controller.gameObject : null;
    }

    private static void DisableBehaviour<T>(GameObject root) where T : Behaviour
    {
        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null)
                components[i].enabled = false;
        }
    }

    private static void DisableCharacterControllers(GameObject root)
    {
        CharacterController[] controllers = root.GetComponentsInChildren<CharacterController>(true);
        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] != null)
                controllers[i].enabled = false;
        }
    }
}
