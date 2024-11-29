using UnityEngine;

public class Testing : MonoBehaviour
{
    public RectTransform gridParentUI; // Assign this in the Inspector

    void Start()
    {
        if (gridParentUI != null)
        {
            Grid grid = new Grid(4, 4, 100f, gridParentUI);
        }
        else
        {
            Debug.LogError("Grid Parent UI is not assigned!");
        }
    }
}
