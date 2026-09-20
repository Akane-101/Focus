using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public sealed class FocusWindDoor : MonoBehaviour
{
    [SerializeField] private float reachThreshold = 1.35f;
    [SerializeField] private float enterDistance = 3.5f;

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
        TryAdvanceFromWind();
        TryMarkReached();
        TryOpenForExit();
        if (levelState != null)
        {
            levelState.TryEnterDoor(transform, enterDistance, true);
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
        spriteRenderer.sortingOrder = levelState.GetSortingOrder(FocusLayer.Mid, FocusSortOrder.Resolve(transform, 0));
        doorCollider.enabled = true;
    }

    private void TryAdvanceFromWind()
    {
        if (levelState == null || levelState.CurrentStage != FocusStage.WaitFirstWind || levelState.CurrentLayer != FocusLayer.Mid || levelState.MidFocusCount < 1)
        {
            return;
        }

        levelState.AdvanceStage(FocusStage.DoorBlown);
    }

    private void TryMarkReached()
    {
        if (levelState == null || levelState.CurrentStage != FocusStage.LeafCollected || levelState.CurrentLayer != FocusLayer.Far)
        {
            return;
        }

        Transform playerTransform = levelState.PlayerTransform;

        if (playerTransform != null && playerTransform.position.x >= transform.position.x - reachThreshold)
        {
            levelState.AdvanceStage(FocusStage.ReachedDoor);
        }
    }

    private void TryOpenForExit()
    {
        if (levelState == null || levelState.CurrentStage != FocusStage.ReachedDoor || levelState.CurrentLayer != FocusLayer.Mid)
        {
            return;
        }

        levelState.AdvanceStage(FocusStage.ExitOpen);
    }
}
