using UnityEngine;

public sealed class FocusBridge : MonoBehaviour
{
    [SerializeField] private float clearThresholdX = 3f;

    private FocusLevelState levelState;
    private SpriteRenderer[] renderers;
    private Collider2D[] colliders;
    private bool hasAdvancedStage;

    private void Awake()
    {
        levelState = FindObjectOfType<FocusLevelState>();
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);

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
        if (levelState == null || hasAdvancedStage || levelState.CurrentStage != FocusStage.BuildBridge || levelState.CurrentLayer != FocusLayer.Mid)
        {
            return;
        }

        Transform playerTransform = levelState.PlayerTransform;

        if (playerTransform != null && playerTransform.position.x >= clearThresholdX)
        {
            hasAdvancedStage = true;
            levelState.AdvanceStage(FocusStage.ReachExit);
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

        bool isUnlocked = levelState.CurrentStage != FocusStage.OpenGate;
        bool isSolid = isUnlocked && levelState.CurrentLayer == FocusLayer.Mid;
        float alpha = levelState.GetLayerAlpha(FocusLayer.Mid);
        int sortingOrder = levelState.GetSortingOrder(FocusLayer.Mid);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = true;
                renderers[i].sortingOrder = sortingOrder;

                Color color = renderers[i].color;
                color.a = alpha;
                renderers[i].color = color;
            }
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = isSolid;
            }
        }
    }
}
