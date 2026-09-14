using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Tilemaps;
using UnityEditorInternal;
using UnityEngine;

public class player : MonoBehaviour
{
    private float direction;
    private Rigidbody2D rigidbody2D;
    private SpriteRenderer spriteRenderer;
    private float Speed = 20;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.position = new Vector3(-4.2f, 6f, 2.1f);
        rigidbody2D = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 dirrection = Vector3.zero;

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            dirrection += new Vector3(2f, 0f, 0f);
                   }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            dirrection += new Vector3(-2f, 0f, 0f);
            spriteRenderer.flipX = true;
        }
        else
        {
            spriteRenderer.flipX = false;
        }
        dirrection = dirrection.normalized * Speed;
        transform.position += dirrection * Time.deltaTime;
    }
}
