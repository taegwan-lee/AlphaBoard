using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

//UI 관리 스크립트

public class GameUIManager : MonoBehaviour
{
    public GameObject GameOverPanel;
    public TextMeshProUGUI WinnerText;
    public Button RestartButton;

    public void ShowGameOver(int blackCount, int whiteCount)
    {
        GameOverPanel.SetActive(true);

        if (blackCount > whiteCount)
            WinnerText.text = "Black WIN! (" + blackCount + " : " + whiteCount + ")";
        else if (whiteCount > blackCount)
            WinnerText.text = "White WIN! (" + whiteCount + " : " + blackCount + ")";
        else
            WinnerText.text = "DRAW!";

        RestartButton.onClick.RemoveAllListeners();
        RestartButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
    }
}
