using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public sealed class FocusLockedDoor : MonoBehaviour
{
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

    private void OnDisable()
    {
        if (levelState != null)
        {
            levelState.StateChanged -= ApplyState;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryComplete(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryComplete(other);
    }

    private void TryComplete(Collider2D other)
    {
        if (levelState == null || !doorCollider.enabled || other.transform != levelState.PlayerTransform)
        {
            return;
        }

        levelState.CompleteLevel();
    }

    private void ApplyState()
    {
        if (levelState == null)
        {
            return;
        }

        spriteRenderer.enabled = true;
        spriteRenderer.sortingOrder = levelState.GetSortingOrder(FocusLayer.Far);
        doorCollider.enabled = levelState.CurrentStage >= FocusStage.HasKey && levelState.CurrentLayer == FocusLayer.Far;
    }
}
