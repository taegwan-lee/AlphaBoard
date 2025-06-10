using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterSelect : MonoBehaviour
{
    public DialogueSequence dialogueSequence; //대사 넣을거
    public DialogueManager dialogueManager;

    public void OnSelectCharacter()
    {
        dialogueManager.StartDialogue(dialogueSequence);
    }
}
