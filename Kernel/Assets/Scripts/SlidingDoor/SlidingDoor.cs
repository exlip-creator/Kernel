using UnityEngine;
using System.Collections;

public class SlidingDoor : MonoBehaviour
{
    [SerializeField] private Vector3 liftOffset = new Vector3(0f, 3f, 0f); // мир: вверх
    [SerializeField] private float duration = 2f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    private Vector3 _closedPosition;
    private bool _isOpen;
    private bool _isBusy;
    private void Awake()
    {
        _closedPosition = transform.position;
    }
    public void Open()
    {
        if (_isBusy || _isOpen) return;
        StartCoroutine(SlideRoutine(_closedPosition + liftOffset));
    }
    private IEnumerator SlideRoutine(Vector3 target)
    {
        _isBusy = true;
        Vector3 start = transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = ease.Evaluate(Mathf.Clamp01(elapsed / duration));
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }
        transform.position = target;
        _isOpen = true;
        _isBusy = false;
    }
}
