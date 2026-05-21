#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Kernel.EditorTools
{
    /// <summary>
    /// Добавляет коллайдеры на префабы бочек/ящиков без физики.
    /// </summary>
    public static class AddPropColliders
    {
        private static readonly string[] PrefabFolders =
        {
            "Assets/FreeLowpolyScifiObjects/Prefabs/Barrels",
            "Assets/FreeLowpolyScifiObjects/Prefabs/Boxes",
            "Assets/FreeLowpolyScifiObjects/Prefabs/Structures",
        };

        [MenuItem("Kernel/Fix Props/Add colliders to barrels and boxes")]
        public static void AddCollidersToProps()
        {
            int changed = 0;
            string[] guids = AssetDatabase.FindAssets("t:Prefab", PrefabFolders);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                    continue;

                bool modified = EnsureCollider(root);
                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    changed++;
                }

                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Prop colliders: updated {changed} prefab(s).");
        }

        private static bool EnsureCollider(GameObject root)
        {
            if (root.GetComponentInChildren<Collider>() != null)
                return false;

            MeshFilter meshFilter = root.GetComponentInChildren<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                MeshCollider meshCollider = root.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = false;
                return true;
            }

            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = 1.15f;
            capsule.radius = 0.38f;
            capsule.center = new Vector3(0f, 0.55f, 0f);
            capsule.direction = 1;
            return true;
        }
    }
}
#endif
