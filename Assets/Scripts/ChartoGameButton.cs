using UnityEngine;
using UnityEngine.SceneManagement;

public class ChartoGameButton : MonoBehaviour
{
    public string GameScene; 

    public void LoadNextScene()
    {
        SceneManager.LoadScene(GameScene);
    }
}
