using NaughtyAttributes; // Optional: only if you are using NaughtyAttributes
using UnityEngine;

[ExecuteInEditMode]
public class ShowVisuals : MonoBehaviour
{
    [Header("Toggle the visibility of MeshRenderers")]
    public bool Show = true;

    void ToggleMeshRenderers()
    {
        // Toggle visibility
        Show = !Show;
        SetMeshRenderersVisibility(gameObject, Show);
    }

    void SetMeshRenderersVisibility(GameObject parent, bool visibility)
    {
        // Get the MeshRenderer on the parent object (if any) and set its visibility
        MeshRenderer meshRenderer = parent.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = visibility;
        }

        // Loop through each child object recursively
        foreach (Transform child in parent.transform)
        {
            SetMeshRenderersVisibility(child.gameObject, visibility);
        }
    }

    void OnValidate()
    {
        // Update visibility in the Editor whenever the "Show" field changes
        SetMeshRenderersVisibility(gameObject, Show);
    }
}
