using TMPro;
using UnityEngine;

public class collectible_manager : MonoBehaviour
{
    public TMP_Text TextScore;
    static public int CollectibleCount = 0;
    void Start()
    {
        CollectibleCount = 0;

    }
    void Update()
    {
        TextScore.text = $"score: {CollectibleCount}";
    }
}