using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadBarnMinigame()
    {
        SceneManager.LoadScene("BarnMinigame");
    }

    public void LoadFieldMinigame()
    {
        SceneManager.LoadScene("FieldMinigame");
    }

    public void LoadFarmScene()
    {
        SceneManager.LoadScene("FarmScene");
    }
}