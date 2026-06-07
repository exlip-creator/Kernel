using UnityEngine;

public class InteractionRaycaster : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float maxDistance = 3f;
    [SerializeField] private LayerMask buttonLayer = ~0;

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;

        if (playerCamera == null) playerCamera = Camera.main;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, buttonLayer, QueryTriggerInteraction.Ignore))
        {
            var button = hit.collider.GetComponentInParent<InteractiveButton>();
            if (button != null)
            {
                button.Interact();
                return;
            }

            var chest = hit.collider.GetComponentInParent<BoxOpening>();
            if (chest != null)
            {
                chest.Interact();
                return;
            }

            var cage = hit.collider.GetComponentInParent<CageUnlock>();
            if (cage != null && cage.TryInteract())
                return;
        }

        CageUnlock[] cages = Object.FindObjectsByType<CageUnlock>();
        for (int i = 0; i < cages.Length; i++)
        {
            if (cages[i] != null && cages[i].TryInteract())
                return;
        }
    }
}
