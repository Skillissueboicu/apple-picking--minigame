using UnityEngine;
using TMPro;

public class score_script : MonoBehaviour
{
    public TMP_Text scoreText;
    private int scoreCount = 0;

    public void AddScore(int scoreToAdd)
    {
        scoreCount += scoreToAdd;
        scoreText.text = "SCORE: " + scoreCount.ToString();
    }
}

