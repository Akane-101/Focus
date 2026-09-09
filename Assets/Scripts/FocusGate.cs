using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public sealed class FocusGate : MonoBehaviour
{
    [SerializeField] private float passThreshold = 0.15f;

    private FocusLevelState levelState;
    private SpriteRenderer spriteRenderer;
    private Collider2D gateCollider;
    private bool hasAdvancedStage;

    private void Awake()
    {
        levelState = FindObjectOfType<FocusLevelState>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        gateCollider = GetComponent<Collider2D>();

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
        ApplyState();
    }

    private void Update()
    {
        if (levelState == null || hasAdvancedStage || levelState.CurrentStage != FocusStage.OpenGate || levelState.CurrentLayer != FocusLayer.Near)
        {
            return;
        }

        Transform playerTransform = levelState.PlayerTransform;

        if (playerTransform != null && playerTransform.position.x <= transform.position.x - passThreshold)
        {
            hasAdvancedStage = true;
            levelState.AdvanceStage(FocusStage.BuildBridge);
        }
    }

    private void OnDisable()
    {
        if (levelState != null)
        {
            levelState.StateChanged -= ApplyState;
        }
    }

    private void ApplyState()
    {
        if (levelState == null)
        {
            return;
        }

        bool isOpen = levelState.CurrentLayer == FocusLayer.Near;
        float alpha = levelState.GetLayerAlpha(FocusLayer.Near);

        spriteRenderer.enabled = true;
        spriteRenderer.sortingOrder = levelState.GetSortingOrder(FocusLayer.Near);

        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
        gateCollider.enabled = !isOpen;
    }
}
