using UnityEngine;

public class apple_spawn : MonoBehaviour
{
    public GameObject Spawn;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
      Instantiate(Spawn);

    }

    // Update is called once per frame
    void Update()
    {
    
    }
}
