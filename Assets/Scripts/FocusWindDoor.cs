using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public sealed class FocusWindDoor : MonoBehaviour
{
    [SerializeField] private float reachThreshold = 1.35f;

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
        if (levelState == null || !doorCollider.enabled || levelState.CurrentStage != FocusStage.ExitOpen || other.transform != levelState.PlayerTransform)
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
        spriteRenderer.sortingOrder = levelState.GetSortingOrder(FocusLayer.Mid);
        doorCollider.enabled = levelState.CurrentStage == FocusStage.ExitOpen;
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
