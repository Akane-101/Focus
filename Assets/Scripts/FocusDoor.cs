using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public sealed class FocusDoor : MonoBehaviour
{
    [SerializeField] private float enterDistance = 3.5f;
    [SerializeField] private bool requireCurrentLayer;
    [SerializeField] private FocusLayer requiredLayer = FocusLayer.Far;
    [SerializeField] private float minEnterY = -999f;

    private FocusLevelState levelState;
    private SpriteRenderer spriteRenderer;
    private Collider2D doorCollider;

    private void Awake()
    {
        levelState = FindObjectOfType<FocusLevelState>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        doorCollider = GetComponent<Collider2D>();
        doorCollider.isTrigger = true;

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
        if (levelState != null)
        {
            levelState.TryEnterDoor(transform, enterDistance, CanEnter());
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

        spriteRenderer.enabled = true;
        spriteRenderer.sortingOrder = levelState.GetSortingOrder(FocusLayer.Far);
        doorCollider.enabled = true;
    }

    private bool CanEnter()
    {
        if (levelState == null || levelState.PlayerTransform == null)
        {
            return false;
        }

        if (requireCurrentLayer && levelState.CurrentLayer != requiredLayer)
        {
            return false;
        }

        return levelState.PlayerTransform.position.y >= minEnterY;
    }
}
