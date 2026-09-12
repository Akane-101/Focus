using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerMove : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 24f;
    [SerializeField] private float deceleration = 30f;
    [SerializeField] private float gravityScale = 4f;

    private Rigidbody2D body;
    private CapsuleCollider2D capsule;
    private SpriteRenderer spriteRenderer;
    private readonly List<object> movementLocks = new List<object>();
    private readonly List<object> rightLocks = new List<object>();
    private readonly List<object> leftLocks = new List<object>();

    private float horizontalInput;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        capsule = GetComponent<CapsuleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        body.gravityScale = gravityScale;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        capsule.direction = CapsuleDirection2D.Vertical;
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        acceleration = Mathf.Max(0.01f, acceleration);
        deceleration = Mathf.Max(0.01f, deceleration);
        gravityScale = Mathf.Max(0.01f, gravityScale);
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            spriteRenderer.flipX = horizontalInput < 0f;
        }
    }

    public bool IsMovementLocked
    {
        get { return movementLocks.Count > 0; }
    }

    public void SetMovementLocked(object source, bool locked)
    {
        if (source == null)
        {
            return;
        }

        if (locked)
        {
            if (!movementLocks.Contains(source))
            {
                movementLocks.Add(source);
            }

            return;
        }

        movementLocks.Remove(source);
    }

    public void SetDirectionBlocked(object source, bool blockRight, bool blockLeft)
    {
        if (source == null)
        {
            return;
        }

        SetDirectionLock(rightLocks, source, blockRight);
        SetDirectionLock(leftLocks, source, blockLeft);
    }

    private static void SetDirectionLock(List<object> locks, object source, bool locked)
    {
        if (locked)
        {
            if (!locks.Contains(source))
            {
                locks.Add(source);
            }

            return;
        }

        locks.Remove(source);
    }

    private void FixedUpdate()
    {
        float input = horizontalInput;

        if (IsMovementLocked || (input > 0f && rightLocks.Count > 0) || (input < 0f && leftLocks.Count > 0))
        {
            input = 0f;
        }

        float targetSpeed = input * moveSpeed;
        float accelerationRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        float nextHorizontalVelocity = Mathf.MoveTowards(body.velocity.x, targetSpeed, accelerationRate * Time.fixedDeltaTime);

        body.velocity = new Vector2(nextHorizontalVelocity, body.velocity.y);
    }
}
