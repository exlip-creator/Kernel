using UnityEngine;

public class InteractiveButton : MonoBehaviour
{
    [Header("Textures")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Texture2D activeTexture;
    [SerializeField] private Texture2D nonActiveTexture;
    [SerializeField] private string textureShaderProperty = "_BaseMap";

    [Header("Door")]
    [SerializeField] private HingeDoor door;

    private bool _isActive = false;

    private void Awake()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        ApplyTexture(nonActiveTexture);
    }

    private void ApplyTexture(Texture2D texture)
    {
        if (meshRenderer == null || texture == null) return;

        Material material = meshRenderer.material;

        if (material.HasProperty(textureShaderProperty))
        {
            material.SetTexture(textureShaderProperty, texture);
        }
        else
        {
            material.mainTexture = texture;
        }
    }

    public void Interact()
    {
        if (_isActive)
        {
            _isActive = false;
            ApplyTexture(nonActiveTexture);
            if (door != null)
            {
                door.Close();
            }
        }
        else
        {
            _isActive = true;
            ApplyTexture(activeTexture);
            if (door != null)
            {
                door.Open();
            }
        }
    }
}
