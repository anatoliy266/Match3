using UnityEngine;

[CreateAssetMenu(fileName = "SessionData", menuName = "Scriptable Objects/SessionData")]
public class SessionData : ScriptableObject
{
    public int currentLevelId;
    private const string LevelKey = "Save_CurrentLevel";
    public void LoadProgress()
    {
        currentLevelId = PlayerPrefs.GetInt(LevelKey, 1); 
    }

    // Вызывается, когда игрок прошел уровень
    public void SaveProgress(int nextLevelId)
    {
        currentLevelId = nextLevelId;
        PlayerPrefs.SetInt(LevelKey, currentLevelId);
        PlayerPrefs.Save(); // Принудительно записываем на диск
    }
}
