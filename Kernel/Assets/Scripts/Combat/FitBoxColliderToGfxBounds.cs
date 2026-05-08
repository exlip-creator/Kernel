using UnityEngine;

namespace Kernel.Combat
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class FitBoxColliderToGfxBounds : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private Transform gfxRoot;

        [Header("Tuning")]
        [SerializeField] private Vector3 padding = new(0.2f, 0f, 0.2f);
        [SerializeField] private float triggerHeight = 0.2f;
        [SerializeField] private float triggerCenterY = 0.05f;

        private BoxCollider _box;

        private void Reset()
        {
            _box = GetComponent<BoxCollider>();
            TryAutoFindGfxRoot();
            Fit();
        }

        private void Awake()
        {
            _box = GetComponent<BoxCollider>();
            if (gfxRoot == null)
                TryAutoFindGfxRoot();

            Fit();
        }

        private void OnValidate()
        {
            _box = GetComponent<BoxCollider>();
            if (gfxRoot == null)
                TryAutoFindGfxRoot();

            triggerHeight = Mathf.Max(0.01f, triggerHeight);
            Fit();
        }

        private void TryAutoFindGfxRoot()
        {
            if (transform.parent == null)
                return;

            Transform sibling = transform.parent.Find("Gfx");
            if (sibling != null)
                gfxRoot = sibling;
        }

        private void Fit()
        {
            if (_box == null || gfxRoot == null)
                return;

            Renderer[] renderers = gfxRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return;

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                worldBounds.Encapsulate(renderers[i].bounds);

            // Convert world AABB to local-space AABB by transforming its corners.
            Vector3 ext = worldBounds.extents;
            Vector3 cen = worldBounds.center;
            Vector3[] corners =
            {
                cen + new Vector3(-ext.x, -ext.y, -ext.z),
                cen + new Vector3(-ext.x, -ext.y,  ext.z),
                cen + new Vector3(-ext.x,  ext.y, -ext.z),
                cen + new Vector3(-ext.x,  ext.y,  ext.z),
                cen + new Vector3( ext.x, -ext.y, -ext.z),
                cen + new Vector3( ext.x, -ext.y,  ext.z),
                cen + new Vector3( ext.x,  ext.y, -ext.z),
                cen + new Vector3( ext.x,  ext.y,  ext.z),
            };

            Matrix4x4 w2l = transform.worldToLocalMatrix;
            Vector3 localMin = w2l.MultiplyPoint3x4(corners[0]);
            Vector3 localMax = localMin;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector3 p = w2l.MultiplyPoint3x4(corners[i]);
                localMin = Vector3.Min(localMin, p);
                localMax = Vector3.Max(localMax, p);
            }

            Vector3 localSize = localMax - localMin;
            Vector3 localCenter = (localMin + localMax) * 0.5f;

            localSize += new Vector3(Mathf.Abs(padding.x), Mathf.Abs(padding.y), Mathf.Abs(padding.z));

            // We only care about covering the slime's area on the floor.
            localCenter.y = triggerCenterY;
            localSize.y = triggerHeight;

            _box.isTrigger = true;
            _box.center = localCenter;
            _box.size = localSize;
        }
    }
}

