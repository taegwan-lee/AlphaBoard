using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class DialogueManager : MonoBehaviour
{
    public Image characterImage;
    public TextMeshProUGUI dialogueText;
    public GameObject dialoguePanel;
    public GameObject choicePanel;
    public AudioSource dialogueSound;

    private DialogueSequence currentDialogue;

    private int currentLineIndex = 0;
    private Coroutine typingCoroutine;
    private bool isTyping = false;

    public RectTransform dialoguePanelRT;     // 패널 애니메이션용
    public RectTransform characterImageRT;    // 캐릭터 애니메이션용
    

    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        int charCount = 0;

        foreach (char c in line)
        {
            dialogueText.text += c;

            // 글자마다 효과음 재생 
            if (charCount % 15 == 0 && dialogueSound != null)
            {
                dialogueSound.PlayOneShot(dialogueSound.clip);
            }

            charCount++;
            yield return new WaitForSeconds(0.03f);
        }

        isTyping = false;
    }

    public void StartDialogue(DialogueSequence sequence)
    {
        currentDialogue = sequence;
        currentLineIndex = 0;
        dialoguePanel.SetActive(true);
        choicePanel.SetActive(false);

        AnimateInDialogue();

        characterImage.sprite = sequence.characterSprite;// 대사별 스프라이트 가능
        
        ShowCurrentLine();
    }

        public void SetDialogueCharacterSprite(Sprite sprite)
    {
        characterImage.sprite = sprite;
    }

    public void OnClickNext()
    {
        if (isTyping)
        {
            // 클릭하면 바로 전체 대사 출력
            StopCoroutine(typingCoroutine);
            dialogueText.text = currentDialogue.lines[currentLineIndex].text;
            isTyping = false;
            return;
        }

        currentLineIndex++;
        if (currentLineIndex >= currentDialogue.lines.Length)
        {
            dialoguePanel.SetActive(false);
            if (currentDialogue.showChoicePanel)
            {
                choicePanel.SetActive(true);
            }
        }
        else
        {
            ShowCurrentLine();
        }
    }

    void ShowCurrentLine()
    {
        DialogueLine line = currentDialogue.lines[currentLineIndex];

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLine(line.text));
    }

    //난이도 저장용
    public void OnConfirmDifficulty()
    {
        GameSettings.SelectedDifficulty = currentDialogue.difficultyLevel;
        SceneManager.LoadScene("MainGame");
    }
    public bool IsDialogueFinished()
    {
        return !isTyping && currentLineIndex >= currentDialogue.lines.Length;
    }

    void AnimateInDialogue()
    {
        Vector2 targetPanelPos = dialoguePanelRT.anchoredPosition;
        Vector2 targetCharacterPos = characterImageRT.anchoredPosition;

        // 슬라이드 인 시작 위치 설정 (아래, 오른쪽에서 등장)
        dialoguePanelRT.anchoredPosition = targetPanelPos + new Vector2(0, -500);
        characterImageRT.anchoredPosition = targetCharacterPos + new Vector2(500, 0);

        // 부드럽게 원래 위치로 이동
        dialoguePanelRT.DOAnchorPos(targetPanelPos, 0.5f).SetEase(Ease.OutBack);
        characterImageRT.DOAnchorPos(targetCharacterPos, 0.5f).SetEase(Ease.OutBack);
    }
}