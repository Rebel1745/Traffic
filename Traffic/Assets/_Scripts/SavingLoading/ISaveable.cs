public interface ISaveable
{
    string SaveKey { get; }
    void PopulateSaveData(GameSaveData saveData);
    void LoadFromSaveData(GameSaveData saveData);
}