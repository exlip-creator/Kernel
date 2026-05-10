using UnityEngine;
using System.Collections;

public class HingeDoor : MonoBehaviour
{
    [Header("Hinge")]
    [SerializeField] Transform hinge;
    [SerializeField] Vector3 worldAxis = Vector3.up;
    [SerializeField] float angleDegrees = 90f;
    [SerializeField] float duration = 0.5f;

    private bool _isOpen = false;
    private bool _isBusy = false;

    public void Open()
    {
        if (_isBusy || _isOpen || hinge == null) return;
        StartCoroutine(RotateRoutine(angleDegrees));
    }

    public void Close()
    {
        if (_isBusy || !_isOpen || hinge == null) return;
        StartCoroutine(RotateRoutine(-angleDegrees));
    }

    private IEnumerator RotateRoutine(float angle)
    {
        _isBusy = true;
        Vector3 axis = worldAxis.normalized;
        Vector3 point = hinge.position;
        float target = Mathf.Abs(angle);
        float sign = Mathf.Sign(angle);
        float rotatedAngle = 0f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float totalFar = target * (elapsedTime / duration);
            float step = totalFar - rotatedAngle;

            if (step > 0f)
            {
                transform.RotateAround(point, axis, step * sign);
                rotatedAngle += step;
            }

            yield return null;
        }

        float left = target - rotatedAngle;
        if (left > 0f)
        {
            transform.RotateAround(point, axis, left * sign);
        }

        _isOpen = angle > 0;
        _isBusy = false;
    }
}
