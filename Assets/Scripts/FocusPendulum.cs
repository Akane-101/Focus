using UnityEngine;

public sealed class FocusPendulum : MonoBehaviour
{
    [SerializeField] private Transform bob;
    [SerializeField] private float maxAngle = 55f;
    [SerializeField] private float period = 3.6f;

    private FocusLevelState levelState;
    private Transform playerTransform;
    private Vector3 bobRestPosition;
    private Vector3 bobPivot;
    private Vector3 lastBobPosition;
    private bool hasBobPosition;

    private void Awake()
    {
        levelState = FindObjectOfType<FocusLevelState>();

        if (bob == null)
        {
            Transform found = transform.Find("Bob");
            bob = found != null ? found : transform;
        }

        bobRestPosition = bob.position;
        SpriteRenderer bobRenderer = bob.GetComponent<SpriteRenderer>();
        float topY = bobRenderer != null ? bobRenderer.bounds.max.y : bob.position.y;
        bobPivot = new Vector3(bob.position.x, topY, bob.position.z);

        if (levelState == null)
        {
            Debug.LogError("Missing FocusLevelState in scene.");
            enabled = false;
        }
    }

    private void Start()
    {
        if (levelState != null)
        {
            playerTransform = levelState.PlayerTransform;
        }
    }

    private void LateUpdate()
    {
        ApplySwing();
        CarryPlayer();
    }

    private void ApplySwing()
    {
        transform.localRotation = Quaternion.identity;

        if (bob == null)
        {
            return;
        }

        float wave = period <= 0.01f ? 0f : (Time.time * (Mathf.PI * 2f / period));
        float angle = maxAngle * Mathf.Sin(wave);
        Quaternion swing = Quaternion.Euler(0f, 0f, angle);
        bob.position = bobPivot + swing * (bobRestPosition - bobPivot);
        bob.rotation = swing;
    }

    private void CarryPlayer()
    {
        if (bob == null)
        {
            return;
        }

        if (!hasBobPosition)
        {
            lastBobPosition = bob.position;
            hasBobPosition = true;
            return;
        }

        Vector3 delta = bob.position - lastBobPosition;
        lastBobPosition = bob.position;

        if (!IsPlayerRiding())
        {
            return;
        }

        playerTransform.position += delta;
    }

    private bool IsPlayerRiding()
    {
        if (levelState == null || playerTransform == null || levelState.CurrentLayer != FocusLayer.Mid)
        {
            return false;
        }

        Collider2D bobCollider = bob.GetComponent<Collider2D>();
        Collider2D playerCollider = playerTransform.GetComponent<Collider2D>();

        if (bobCollider == null || playerCollider == null || !bobCollider.enabled)
        {
            return false;
        }

        return bobCollider.bounds.Intersects(playerCollider.bounds) &&
               playerTransform.position.y >= bob.position.y - 0.25f;
    }
}
