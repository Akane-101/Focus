using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public sealed class FocusSunGlare : MonoBehaviour
{
    [SerializeField] private float glareFadeSpeed = 4f;

    private FocusLevelState levelState;
    private PlayerMove playerMove;
    private SpriteRenderer spriteRenderer;
    private Collider2D sunCollider;
    private FocusGlareOverlay glareOverlay;
    private bool isBlocking;

    private void Awake()
    {
        levelState = FindObjectOfType<FocusLevelState>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        sunCollider = GetComponent<Collider2D>();
        sunCollider.isTrigger = true;

        if (levelState == null)
        {
            Debug.LogError("Missing FocusLevelState in scene.");
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (levelState != null)
        {
            levelState.StateChanged += ApplyState;
        }
    }

    private void Start()
    {
        if (levelState != null && levelState.PlayerTransform != null)
        {
            playerMove = levelState.PlayerTransform.GetComponent<PlayerMove>();
        }

        glareOverlay = FocusGlareOverlay.EnsureOnMainCamera();
        ApplyState();
    }

    private void Update()
    {
        bool inLight = IsPlayerInLight();
        bool shouldBlock = inLight;

        if (shouldBlock != isBlocking)
        {
            isBlocking = shouldBlock;

            if (playerMove != null)
            {
                playerMove.SetDirectionBlocked(this, isBlocking, false);
            }
        }

        if (glareOverlay != null)
        {
            float target = inLight ? 1f : 0f;
            glareOverlay.Intensity = Mathf.MoveTowards(glareOverlay.Intensity, target, glareFadeSpeed * Time.deltaTime);
        }
    }

    private void OnDisable()
    {
        if (levelState != null)
        {
            levelState.StateChanged -= ApplyState;
        }

        if (playerMove != null)
        {
            playerMove.SetDirectionBlocked(this, false, false);
        }
    }

    private void ApplyState()
    {
        if (levelState == null)
        {
            return;
        }

        spriteRenderer.enabled = true;
        spriteRenderer.sortingOrder = levelState.GetSortingOrder(FocusLayer.Mid, FocusSortOrder.Resolve(transform, -1));
        sunCollider.enabled = levelState.CurrentStage >= FocusStage.DoorBlown;
    }

    private bool IsPlayerInLight()
    {
        if (levelState == null ||
            levelState.CurrentStage < FocusStage.DoorBlown ||
            levelState.CurrentLayer != FocusLayer.Mid ||
            levelState.PlayerTransform == null)
        {
            return false;
        }

        Collider2D playerCollider = levelState.PlayerTransform.GetComponent<Collider2D>();

        if (playerCollider == null)
        {
            return false;
        }

        return sunCollider.bounds.Intersects(playerCollider.bounds);
    }
}
