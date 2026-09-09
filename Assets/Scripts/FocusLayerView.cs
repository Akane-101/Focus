using UnityEngine;

public sealed class FocusLayerView : MonoBehaviour
{
    [SerializeField] private FocusLayer representedLayer = FocusLayer.Mid;

    private FocusLevelState levelState;
    private SpriteRenderer[] renderers;

    private void Awake()
    {
        levelState = FindObjectOfType<FocusLevelState>();
        renderers = GetComponentsInChildren<SpriteRenderer>(true);

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

    private void ApplyState()
    {
        if (levelState == null || renderers == null)
        {
            return;
        }

        float alpha = levelState.GetLayerAlpha(representedLayer);
        int sortingOrder = levelState.GetSortingOrder(representedLayer);

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer currentRenderer = renderers[i];

            if (currentRenderer == null ||
                currentRenderer.GetComponentInParent<FocusGate>() != null ||
                currentRenderer.GetComponentInParent<FocusDoor>() != null ||
                currentRenderer.GetComponentInParent<FocusBridge>() != null)
            {
                continue;
            }

            currentRenderer.enabled = true;
            currentRenderer.sortingOrder = sortingOrder;

            Color color = currentRenderer.color;
            color.a = alpha;
            currentRenderer.color = color;
        }
    }
}
