using UnityEngine;
using UnityEngine.UI;

public class Grid
{
    private int width;
    private int height;
    private float cellSize;
    private RectTransform parentUI;

    public Grid(int width, int height, float cellSize, RectTransform parentUI)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.parentUI = parentUI;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CreateCell(x, y, x + "," + y); // Display coordinates in the cell
            }
        }
    }

    private void CreateCell(int x, int y, string text)
    {
        // Create a new UI Text GameObject for the grid cell
        GameObject cellObj = new GameObject("GridCell_" + x + "_" + y, typeof(Text));
        cellObj.transform.SetParent(parentUI);

        Text uiText = cellObj.GetComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Updated font
        uiText.fontSize = 20;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.color = Color.white;

        // Set the RectTransform properties for the UI Text
        RectTransform rectTransform = cellObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(cellSize, cellSize); // Cell size
        rectTransform.anchorMin = new Vector2(0, 1); // Anchor top-left
        rectTransform.anchorMax = new Vector2(0, 1); // Anchor top-left
        rectTransform.pivot = new Vector2(0, 1); // Pivot top-left

        // Correct the positioning based on the grid's layout
        rectTransform.anchoredPosition = new Vector2(x * cellSize, -y * cellSize);
    }
}
