using System.Collections.Generic;

[System.Serializable]
public class GridSaveData
{
    public int width;
    public int height;
    public List<GridCellSaveData> cells = new();
}