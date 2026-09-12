using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public sealed class FocusKey : MonoBehaviour
{
    private FocusLevelState levelState;
    private SpriteRenderer spriteRenderer;
    private Collider2D keyCollider;
    private bool isCollected;

    private void Awake()
    {
        levelState = FindObjectOfType<FocusLevelState>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        keyCollider = GetComponent<Collider2D>();
        keyCollider.isTrigger = true;

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

    private void OnDisable()
    {
        if (levelState != null)
        {
            levelState.StateChanged -= ApplyState;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryCollect(other);
    }

    private void TryCollect(Collider2D other)
    {
        if (isCollected || levelState == null || levelState.CurrentLayer != FocusLayer.Far || other.transform != levelState.PlayerTransform)
        {
            return;
        }

        isCollected = true;
        spriteRenderer.enabled = false;
        keyCollider.enabled = false;
        levelState.AdvanceStage(FocusStage.HasKey);
    }

    private void ApplyState()
    {
        if (levelState == null || isCollected)
        {
            return;
        }

        spriteRenderer.enabled = true;
        spriteRenderer.sortingOrder = levelState.GetSortingOrder(FocusLayer.Far) + 1;
        keyCollider.enabled = levelState.CurrentLayer == FocusLayer.Far;
    }
}
