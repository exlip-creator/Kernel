using UnityEngine;

public class BoxOpening : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private bool openOnlyOnce = true;
    [Header("Gear")]
    [SerializeField] private bool containsGear;
    [SerializeField] private bool gearCollected;
    private bool _isOpen;
    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }
    public void Interact()
    {
        if (openOnlyOnce && _isOpen) return;
        if (animator == null) return;
        animator.SetTrigger(openTriggerName);
        _isOpen = true;

        if (containsGear && !gearCollected)
        {
            gearCollected = true;
            GearProgress.Instance?.TryCollect();
        }
    }
}
