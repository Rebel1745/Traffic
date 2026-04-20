using System.Collections.Generic;

[System.Serializable]
public class GridSaveData
{
    public int Width;
    public int Height;
    public List<GridCellSaveData> Cells = new();
}