using UnityEngine;

public sealed class HintTrigger : MonoBehaviour
{
    [TextArea(2, 6)]
    [SerializeField] private string hintMassage;

    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool hideOnExit = true;
    
    private bool _isShowing = false;

    private void Awake()
    {
        var rigidBody = GetComponent<Rigidbody>();
        if (rigidBody == null) rigidBody = gameObject.AddComponent<Rigidbody>();

        rigidBody.isKinematic = true;
        rigidBody.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isShowing) return;
        if (!IsUnderTaggedPlayer(other, playerTag)) return;

        HintDisplay.instance?.Show(hintMassage);
        _isShowing = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!hideOnExit) return;
        if (!IsUnderTaggedPlayer(other, playerTag)) return;

        HintDisplay.instance?.Hide();
        _isShowing = false;
    }

    private static bool IsUnderTaggedPlayer(Collider other, string tag)
    {
        Transform trans = other.transform;
        while (trans != null)
        {
            if (trans.CompareTag(tag)) return true;
            trans = trans.parent;
        }

        return false;
    }
}