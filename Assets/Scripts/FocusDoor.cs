using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public sealed class FocusDoor : MonoBehaviour
{
    private FocusLevelState levelState;
    private SpriteRenderer spriteRenderer;
    private Collider2D doorCollider;

    private void Awake()
    {
        levelState = FindObjectOfType<FocusLevelState>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        doorCollider = GetComponent<Collider2D>();

        if (levelState == null)
        {
            Debug.LogError("Missing FocusLevelState in scene.");
            enabled = false;
            return;
        }

        doorCollider.isTrigger = true;
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

        bool isActive = levelState.CurrentStage == FocusStage.ReachExit && levelState.CurrentLayer == FocusLayer.Far;

        spriteRenderer.enabled = true;
        spriteRenderer.sortingOrder = levelState.GetSortingOrder(FocusLayer.Far);
        doorCollider.enabled = isActive;
    }
}
