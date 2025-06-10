using UnityEngine;

public enum Difficulty
{
    Easy,
    Normal,
    Hard
}

[CreateAssetMenu(menuName = "Dialogue/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject
{
    public DialogueLine[] lines;
    public Sprite characterSprite; // 대사용 이미지

    public Difficulty difficultyLevel;
    public bool showChoicePanel = false; //선택창 메인게임에서는 필요없으니까
}
