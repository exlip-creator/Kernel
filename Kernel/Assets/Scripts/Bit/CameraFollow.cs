using UnityEngine;

namespace Bit.Robot
{
    [DefaultExecutionOrder(1000)]
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Orbit")]
        [SerializeField] private float cameraDistance = 3f;
        [SerializeField] private float mouseSensitivity = 4f;
        [SerializeField] private float minPitch = -35f;
        [SerializeField] private float maxPitch = 65f;

        [Header("Follow smoothing")]
        [SerializeField] private float followSmoothTime = 0.14f;
        [SerializeField] private float followMaxSpeed = 28f;
        [SerializeField] private float distanceSmoothTimeIn = 0.1f;
        [SerializeField] private float distanceSmoothTimeOut = 0.05f;
        [SerializeField] private bool pivotOffsetUsesYawOnly = true;
        [SerializeField] private Vector3 targetPivotOffset = new Vector3(0f, 1.67f, 0f);

        [Header("Collision")]
        [SerializeField] private LayerMask obstructionMask;
        [SerializeField] private float collisionSphereRadius = 0.22f;
        [SerializeField] private float obstructionPadding = 0.12f;
        [SerializeField] private float minCameraDistance = 0.85f;
        [SerializeField] private float collisionResolveStep = 0.04f;

        [Header("Ceiling pitch limit")]
        [SerializeField] private float ceilingProbeDistance = 2.5f;
        [SerializeField] private float ceilingProbeOriginUp = 0.2f;
        [SerializeField] private float ceilingTightHeadroom = 0.45f;
        [SerializeField] private float ceilingComfortHeadroom = 2.2f;
        [SerializeField] private float ceilingMinPitchWhenTight = -15f;

        [Header("Debug")]
        [SerializeField] private bool drawCollisionDebug;

        private static readonly Collider[] OverlapBuffer = new Collider[16];

        private float _pitch;
        private float _yaw;
        private float _pivotVerticalVelocity;
        private Vector3 _smoothedPivot;
        private float _smoothedDistance;
        private float _distanceVelocity;
        private Camera _camera;

        private Vector3 _debugPivot;
        private Vector3 _debugRayDir;
        private float _debugResolvedDistance;
        private Vector3 _debugHitPoint;
        private bool _debugBlocked;

        public float Yaw => _yaw;
        public float Pitch => _pitch;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            obstructionMask = BuildObstructionMask(obstructionMask);

            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    target = player.transform;
            }

            if (target != null)
            {
                _smoothedPivot = GetPivotWorld(target);
                _smoothedDistance = cameraDistance;
            }
        }

        private void Start()
        {
            if (target != null)
            {
                Vector3 pivot = GetPivotWorld(target);
                _smoothedPivot = pivot;
                _smoothedDistance = cameraDistance;
                Vector3 fromCamToPivot = pivot - transform.position;
                if (fromCamToPivot.sqrMagnitude > 0.001f)
                {
                    Quaternion look = Quaternion.LookRotation(fromCamToPivot, Vector3.up);
                    Vector3 e = look.eulerAngles;
                    _pitch = e.x;
                    if (_pitch > 180f)
                        _pitch -= 360f;
                    _yaw = e.y;
                }
                else
                {
                    Vector3 euler = transform.eulerAngles;
                    _pitch = euler.x;
                    if (_pitch > 180f)
                        _pitch -= 360f;
                    _yaw = euler.y;
                }
            }
            else
            {
                Vector3 euler = transform.eulerAngles;
                _pitch = euler.x;
                if (_pitch > 180f)
                    _pitch -= 360f;
                _yaw = euler.y;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                var playerGo = GameObject.FindGameObjectWithTag("Player");
                if (playerGo == null)
                    return;

                target = playerGo.transform;
                _smoothedPivot = GetPivotWorld(target);
                _smoothedDistance = cameraDistance;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Vector3 pivotWorld = GetPivotWorld(target);
            _smoothedPivot.x = pivotWorld.x;
            _smoothedPivot.z = pivotWorld.z;
            _smoothedPivot.y = Mathf.SmoothDamp(
                _smoothedPivot.y,
                pivotWorld.y,
                ref _pivotVerticalVelocity,
                followSmoothTime,
                followMaxSpeed);

            Vector2 look = BitInput.GetMouseLook(mouseSensitivity);
            _yaw += look.x;
            _pitch -= look.y;
            _pitch = Mathf.Clamp(_pitch, GetEffectiveMinPitch(_smoothedPivot), maxPitch);

            Quaternion orbitRot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 rayDir = orbitRot * Vector3.back;

            float targetDistance = FindClearCameraDistance(_smoothedPivot, rayDir, cameraDistance);
            if (targetDistance < _smoothedDistance - 0.01f)
            {
                _smoothedDistance = targetDistance;
                _distanceVelocity = 0f;
            }
            else
            {
                float distanceSmoothTime = distanceSmoothTimeOut;
                _smoothedDistance = Mathf.SmoothDamp(
                    _smoothedDistance,
                    targetDistance,
                    ref _distanceVelocity,
                    distanceSmoothTime);
            }
            CacheDebugState(_smoothedPivot, rayDir, _smoothedDistance);

            transform.position = _smoothedPivot + rayDir * _smoothedDistance;
            transform.rotation = orbitRot;
        }

        private Vector3 GetPivotWorld(Transform followTarget)
        {
            Vector3 offset = pivotOffsetUsesYawOnly
                ? Quaternion.Euler(0f, _yaw, 0f) * targetPivotOffset
                : followTarget.rotation * targetPivotOffset;
            return followTarget.position + offset;
        }

        private static LayerMask BuildObstructionMask(LayerMask serializedMask)
        {
            int mask = serializedMask.value;
            if (mask == 0 || mask == 1)
                mask = Physics.DefaultRaycastLayers;

            ExcludeLayer(ref mask, "Player");
            ExcludeLayer(ref mask, "UI");
            ExcludeLayer(ref mask, "Ignore Raycast");
            ExcludeLayer(ref mask, "TransparentFX");
            ExcludeLayer(ref mask, "Button");
            ExcludeLayer(ref mask, "Magnetic");
            return mask;
        }

        private static void ExcludeLayer(ref int mask, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
                mask &= ~(1 << layer);
        }

        private float GetHardMinDistance()
        {
            float nearClip = _camera != null ? _camera.nearClipPlane : 0.3f;
            return collisionSphereRadius + nearClip + obstructionPadding;
        }

        private float FindClearCameraDistance(Vector3 pivot, Vector3 rayDir, float desiredDistance)
        {
            _debugBlocked = false;
            float hardMin = GetHardMinDistance();
            float maxDistance = Mathf.Max(desiredDistance, hardMin);

            if (IsCameraPlacementClear(pivot, rayDir, maxDistance, out _))
                return maxDistance;

            _debugBlocked = true;

            float clearDistance = hardMin;
            float blockedDistance = maxDistance;

            for (int i = 0; i < 10; i++)
            {
                float mid = (clearDistance + blockedDistance) * 0.5f;
                if (IsCameraPlacementClear(pivot, rayDir, mid, out Vector3 hitPoint))
                {
                    clearDistance = mid;
                    _debugHitPoint = hitPoint;
                }
                else
                {
                    blockedDistance = mid;
                    _debugHitPoint = pivot + rayDir * mid;
                }
            }

            return Mathf.Max(clearDistance - obstructionPadding, hardMin);
        }

        private bool IsCameraPlacementClear(Vector3 pivot, Vector3 rayDir, float distance, out Vector3 obstructionPoint)
        {
            obstructionPoint = default;
            Vector3 camPos = pivot + rayDir * distance;

            int overlapCount = Physics.OverlapSphereNonAlloc(
                camPos,
                collisionSphereRadius,
                OverlapBuffer,
                obstructionMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < overlapCount; i++)
            {
                if (IsIgnoredObstructionCollider(OverlapBuffer[i]))
                    continue;

                obstructionPoint = OverlapBuffer[i].bounds.ClosestPoint(camPos);
                return false;
            }

            Vector3 toCamera = camPos - pivot;
            float lineLength = toCamera.magnitude;
            if (lineLength > 0.001f)
            {
                Vector3 lineDir = toCamera / lineLength;
                if (Physics.Raycast(pivot, lineDir, out RaycastHit hit, lineLength, obstructionMask,
                        QueryTriggerInteraction.Ignore))
                {
                    if (!IsIgnoredObstructionCollider(hit.collider))
                    {
                        obstructionPoint = hit.point;
                        return false;
                    }
                }

                if (Physics.SphereCast(pivot, collisionSphereRadius, lineDir, out hit, lineLength,
                        obstructionMask, QueryTriggerInteraction.Ignore))
                {
                    if (!IsIgnoredObstructionCollider(hit.collider))
                    {
                        obstructionPoint = hit.point;
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsIgnoredObstructionCollider(Collider collider)
        {
            if (collider == null)
                return true;

            if (collider.isTrigger)
                return true;

            int layer = collider.gameObject.layer;
            if (layer == LayerMask.NameToLayer("Player"))
                return true;

            return collider.CompareTag("Player");
        }

        private float GetEffectiveMinPitch(Vector3 pivot)
        {
            Vector3 probeOrigin = pivot + Vector3.up * ceilingProbeOriginUp;
            if (!Physics.Raycast(probeOrigin, Vector3.up, out RaycastHit ceilingHit, ceilingProbeDistance,
                    obstructionMask, QueryTriggerInteraction.Ignore))
                return minPitch;

            float headroom = ceilingHit.distance;
            if (headroom >= ceilingComfortHeadroom)
                return minPitch;

            float t = Mathf.Clamp01(Mathf.InverseLerp(ceilingTightHeadroom, ceilingComfortHeadroom, headroom));
            float tightMin = Mathf.Max(minPitch, ceilingMinPitchWhenTight);
            return Mathf.Lerp(tightMin, minPitch, t);
        }

        private void CacheDebugState(Vector3 pivot, Vector3 rayDir, float resolvedDistance)
        {
            _debugPivot = pivot;
            _debugRayDir = rayDir;
            _debugResolvedDistance = resolvedDistance;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawCollisionDebug)
                return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_debugPivot, 0.08f);

            if (_debugRayDir.sqrMagnitude < 0.0001f)
                return;

            Vector3 desiredEnd = _debugPivot + _debugRayDir * cameraDistance;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_debugPivot, desiredEnd);

            Vector3 resolvedEnd = _debugPivot + _debugRayDir * _debugResolvedDistance;
            Gizmos.color = _debugBlocked ? Color.red : Color.green;
            Gizmos.DrawLine(_debugPivot, resolvedEnd);
            Gizmos.DrawWireSphere(resolvedEnd, collisionSphereRadius);

            if (_debugBlocked)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(_debugHitPoint, 0.07f);
            }
        }

        private void OnValidate()
        {
            followSmoothTime = Mathf.Max(0.01f, followSmoothTime);
            followMaxSpeed = Mathf.Max(0f, followMaxSpeed);
            distanceSmoothTimeIn = Mathf.Max(0.01f, distanceSmoothTimeIn);
            distanceSmoothTimeOut = Mathf.Max(0.01f, distanceSmoothTimeOut);
            cameraDistance = Mathf.Max(minCameraDistance, cameraDistance);
            minCameraDistance = Mathf.Max(0.1f, minCameraDistance);
            collisionResolveStep = Mathf.Max(0.01f, collisionResolveStep);
            ceilingProbeDistance = Mathf.Max(0.1f, ceilingProbeDistance);
            ceilingProbeOriginUp = Mathf.Max(0f, ceilingProbeOriginUp);
            ceilingComfortHeadroom = Mathf.Max(ceilingTightHeadroom + 0.01f, ceilingComfortHeadroom);
            ceilingMinPitchWhenTight = Mathf.Clamp(ceilingMinPitchWhenTight, minPitch, maxPitch);
        }
    }
}
