using UnityEngine;

public class score_script : MonoBehaviour
{
public TMP_Text scoreText;
private int scoreCount=0;

    public void AddScore(int scoreToAdd)
    {
        scoreCount ++ ; scoreText.AddScore(scoreToAdd);
        UpdateScore.Text= "SCORE: " + scoreCount.ToString();

    }
}
