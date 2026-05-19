using UnityEngine;

public class HintMarkerRotate : MonoBehaviour
{
    [SerializeField] private float speedDegrees = 90f;
    [SerializeField] private Vector3 axis = Vector3.up;
    
    private void Update() => transform.Rotate(axis, speedDegrees * Time.deltaTime, Space.Self);
}