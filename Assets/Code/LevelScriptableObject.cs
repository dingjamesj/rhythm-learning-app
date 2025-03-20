using UnityEngine;

[CreateAssetMenu(fileName = "Level")]
public class LevelScriptableObject : ScriptableObject {

    [SerializeField] protected int levelNumber = 1;
    [SerializeField] protected string levelName = "Name";
    [SerializeField] protected string levelDescription = "Description";
    [SerializeField] [TextArea] protected string levelContents = "4/4: 1 1 1 1";
    [SerializeField] protected int tempo = 100;

    public int GetLevelNumber() {

        return levelNumber;

    }

    public string GetLevelName() {

        return levelName;

    }

    public string GetLevelDescription() {

        return levelDescription;

    }

    public string GetLevelContents() {

        return levelContents;

    }

    public int GetTempo() {

        return tempo;

    }

}
