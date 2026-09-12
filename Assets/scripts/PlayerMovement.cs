using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public enum StretchAxis
    {
        X,
        Y,
        Z
    }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float deceleration = 30f;

    [Header("Sprite")]
    [SerializeField] private Transform sprite;
    [SerializeField] private Renderer spriteRenderer;

    [Header("Squash & Stretch")]
    [SerializeField] private StretchAxis stretchAxis = StretchAxis.Y;
    [SerializeField] private float stretchAmount = 0.08f;
    [SerializeField] private float stretchSpeed = 12f;
    [SerializeField] private float squashMultiplier = 0.5f;

    [Header("Walking Animation")]
    [SerializeField] private Texture2D[] walkingFrames;
    [SerializeField] private float animationSpeed = 0.12f;

    private Vector3 currentVelocity;
    private Vector3 originalSpriteScale;

    private float animationTimer;
    private int currentFrame;
    private bool isWalking;

    private Material spriteMaterial;
    private Rigidbody rb;

    // Stores which direction the sprite is facing.
    private bool facingLeft = false;

    private void Awake()
    {
        // Get Rigidbody if present.
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            // Prevent the player from rotating due to physics.
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        // Automatically find the child named "Sprite".
        if (sprite == null)
        {
            Transform child = transform.Find("Sprite");

            if (child != null)
            {
                sprite = child;
            }
        }

        // Automatically find the Renderer on the Sprite child.
        if (spriteRenderer == null && sprite != null)
        {
            spriteRenderer = sprite.GetComponent<Renderer>();
        }

        // Store the original sprite scale.
        if (sprite != null)
        {
            originalSpriteScale = sprite.localScale;
        }

        // Create an instance of the material.
        if (spriteRenderer != null)
        {
            spriteMaterial = spriteRenderer.material;
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

        // Change facing direction only when actually moving horizontally.
        if (Mathf.Abs(currentVelocity.x) > 0.01f)
        {
            facingLeft = currentVelocity.x < 0f;
        }
    }

    private void HandleSprite()
    {
        if (sprite == null || spriteRenderer == null || spriteMaterial == null)
        {
            return;
        }

        if (isWalking)
        {
            HandleSquashAndStretch();
            HandleWalkingAnimation();
        }
        else
        {
            ResetSprite();
        }

        // Apply facing direction AFTER squash/stretch.
        HandleSpriteDirection();
    }

    private void HandleSquashAndStretch()
    {
        float speedPercent = Mathf.Clamp01(
            currentVelocity.magnitude / moveSpeed
        );

        float wave = Mathf.Sin(Time.time * stretchSpeed);

        float stretch = wave * stretchAmount * speedPercent;
        float squash = -stretch * squashMultiplier;

        Vector3 targetScale = originalSpriteScale;

        switch (stretchAxis)
        {
            case StretchAxis.X:
                targetScale.x *= 1f + stretch;
                targetScale.y *= 1f + squash;
                break;

            case StretchAxis.Y:
                targetScale.y *= 1f + stretch;
                targetScale.x *= 1f + squash;
                break;

            case StretchAxis.Z:
                targetScale.z *= 1f + stretch;
                targetScale.x *= 1f + squash;
                break;
        }

        // Apply the facing direction to the target scale.
        if (facingLeft)
        {
            targetScale.x = -Mathf.Abs(targetScale.x);
        }
        else
        {
            targetScale.x = Mathf.Abs(targetScale.x);
        }

        sprite.localScale = Vector3.Lerp(
            sprite.localScale,
            targetScale,
            12f * Time.deltaTime
        );
    }

    private void HandleSpriteDirection()
    {
        // Preserve the current squash/stretch amount
        // while applying the facing direction.
        Vector3 scale = sprite.localScale;

        if (facingLeft)
        {
            scale.x = -Mathf.Abs(scale.x);
        }
        else
        {
            scale.x = Mathf.Abs(scale.x);
        }

        sprite.localScale = scale;
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

            spriteMaterial.mainTexture = walkingFrames[currentFrame];
        }
    }

    private void ResetSprite()
    {
        // Reset squash/stretch while preserving facing direction.
        Vector3 targetScale = originalSpriteScale;

        if (facingLeft)
        {
            targetScale.x = -Mathf.Abs(targetScale.x);
        }
        else
        {
            targetScale.x = Mathf.Abs(targetScale.x);
        }

        sprite.localScale = Vector3.Lerp(
            sprite.localScale,
            targetScale,
            15f * Time.deltaTime
        );

        animationTimer = 0f;
        currentFrame = 0;

        if (walkingFrames != null && walkingFrames.Length > 0)
        {
            spriteMaterial.mainTexture = walkingFrames[0];
        }
    }
}