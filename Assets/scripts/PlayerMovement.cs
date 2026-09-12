using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 30f;

    [Header("Sprite")]
    [SerializeField] private Transform sprite;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Squash & Stretch")]
    [SerializeField] private float squashAmount = 0.08f;
    [SerializeField] private float squashSpeed = 12f;

    [Header("Walking Animation")]
    [SerializeField] private Sprite[] walkingFrames;
    [SerializeField] private float animationSpeed = 0.12f;

    private Vector3 currentVelocity;
    private Vector3 originalSpriteScale;
    private float animationTimer;
    private int currentFrame;
    private bool isWalking;

    private void Awake()
    {
        if (sprite == null)
        {
            Transform child = transform.Find("Sprite");

            if (child != null)
            {
                sprite = child;
            }
        }

        if (spriteRenderer == null && sprite != null)
        {
            spriteRenderer = sprite.GetComponent<SpriteRenderer>();
        }

        if (sprite != null)
        {
            originalSpriteScale = sprite.localScale;
        }
    }

    private void Update()
    {
        HandleMovement();
        HandleSprite();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(horizontal, 0f, vertical);
        input = Vector3.ClampMagnitude(input, 1f);

        Vector3 targetVelocity = input * moveSpeed;

        float accelerationRate;

        if (input.sqrMagnitude > 0.01f)
        {
            accelerationRate = acceleration;
        }
        else
        {
            accelerationRate = deceleration;
        }

        currentVelocity = Vector3.MoveTowards(
            currentVelocity,
            targetVelocity,
            accelerationRate * Time.deltaTime
        );

        transform.position += currentVelocity * Time.deltaTime;

        isWalking = currentVelocity.sqrMagnitude > 0.01f;
    }

    private void HandleSprite()
    {
        if (sprite == null || spriteRenderer == null)
        {
            return;
        }

        if (isWalking)
        {
            HandleSquashAndStretch();
            HandleWalkingAnimation();
            HandleSpriteDirection();
        }
        else
        {
            ResetSprite();
        }
    }

    private void HandleSquashAndStretch()
    {
        float speedPercent = Mathf.Clamp01(currentVelocity.magnitude / moveSpeed);

        float squash = Mathf.Sin(Time.time * squashSpeed)
                       * squashAmount
                       * speedPercent;

        Vector3 targetScale = originalSpriteScale;

        targetScale.y *= 1f + squash;
        targetScale.x *= 1f - squash * 0.5f;

        sprite.localScale = Vector3.Lerp(
            sprite.localScale,
            targetScale,
            12f * Time.deltaTime
        );
    }

    private void HandleWalkingAnimation()
    {
        if (walkingFrames == null || walkingFrames.Length == 0)
        {
            return;
        }

        animationTimer += Time.deltaTime;

        if (animationTimer >= animationSpeed)
        {
            animationTimer -= animationSpeed;

            currentFrame++;

            if (currentFrame >= walkingFrames.Length)
            {
                currentFrame = 0;
            }

            spriteRenderer.sprite = walkingFrames[currentFrame];
        }
    }

    private void HandleSpriteDirection()
    {
        if (Mathf.Abs(currentVelocity.x) < 0.01f)
        {
            return;
        }

        if (currentVelocity.x < 0f)
        {
            spriteRenderer.flipX = true;
        }
        else
        {
            spriteRenderer.flipX = false;
        }
    }

    private void ResetSprite()
    {
        sprite.localScale = Vector3.Lerp(
            sprite.localScale,
            originalSpriteScale,
            15f * Time.deltaTime
        );

        animationTimer = 0f;
        currentFrame = 0;

        if (walkingFrames != null && walkingFrames.Length > 0)
        {
            spriteRenderer.sprite = walkingFrames[0];
        }
    }
}