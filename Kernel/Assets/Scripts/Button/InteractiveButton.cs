using UnityEngine;

public class InteractiveButton : MonoBehaviour
{
    [Header("Textures")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private Texture2D activeTexture;
    [SerializeField] private Texture2D nonActiveTexture;
    [SerializeField] private string textureShaderProperty = "_BaseMap";

    [Header("ButtonPuzzle")]
    [SerializeField] private ButtonPuzzle puzzle;

    [HideInInspector] public bool isActive = false;

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
        if (puzzle == null) return;

        if (isActive)
        {
            isActive = false;
            ApplyTexture(nonActiveTexture);
            puzzle.RefreshPuzzle();
        }
        else
        {
            isActive = true;
            ApplyTexture(activeTexture);
            puzzle.RefreshPuzzle();
        }
    }
}
