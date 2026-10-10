using System;
using UnityEngine;

public class collectible : MonoBehaviour
{
    public static event Action OnCollectibleCollected;
    public int collectValue = 1;
    void Update()
    {
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        { 
            Destroy(gameObject);
            collectible_manager.CollectibleCount += collectValue;
        }
    }
}
