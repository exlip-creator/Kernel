using UnityEngine;
using System.Collections;

public class HingeDoor : MonoBehaviour
{
    [Header("Hinge")]
    [SerializeField] Transform hinge;
    [SerializeField] Vector3 worldAxis = Vector3.up;
    [SerializeField] float angleDegrees = 90f;
    [SerializeField] float duration = 0.5f;

    private bool _isOpen;
    private bool _isBusy;
    private bool _rotateHingeTransform;

    private void Awake()
    {
        if (hinge == null)
        {
            _rotateHingeTransform = false;
            return;
        }

        _rotateHingeTransform = transform == hinge || transform.IsChildOf(hinge);
    }

    public void Open()
    {
        if (_isBusy || _isOpen || hinge == null)
            return;

        StartCoroutine(RotateRoutine(angleDegrees));
    }

    public void Close()
    {
        if (_isBusy || !_isOpen || hinge == null)
            return;

        StartCoroutine(RotateRoutine(-angleDegrees));
    }

    private IEnumerator RotateRoutine(float angle)
    {
        _isBusy = true;

        if (_rotateHingeTransform)
        {
            Transform pivot = hinge;
            Quaternion startRot = pivot.localRotation;
            Quaternion endRot = startRot * Quaternion.Euler(0f, angle, 0f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                pivot.localRotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }

            pivot.localRotation = endRot;
        }
        else
        {
            Vector3 axis = (hinge != null ? hinge.up : worldAxis).normalized;
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
                transform.RotateAround(point, axis, left * sign);
        }

        _isOpen = angle > 0;
        _isBusy = false;
    }
}
