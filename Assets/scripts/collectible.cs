using System;
using UnityEngine;

public class collectible : MonoBehaviour
{
    public static event Action OnCollectibleCollected;

    void Update()
    {
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            OnCollectibleCollected?.Invoke();
            Destroy(gameObject);
        }
    }
}
