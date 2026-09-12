using UnityEngine;

public sealed class FocusLayerView : MonoBehaviour
{
    [SerializeField] private FocusLayer representedLayer = FocusLayer.Mid;

    private FocusLevelState levelState;
    private SpriteRenderer[] renderers;
    private Collider2D[] colliders;

    private void Awake()
    {
        levelState = FindObjectOfType<FocusLevelState>();
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);
        FocusUnityLayers.Assign(transform, representedLayer);

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
        if (levelState == null)
        {
            return;
        }

        int sortingOrder = levelState.GetSortingOrder(representedLayer);
        bool isCurrentLayer = levelState.CurrentLayer == representedLayer;

        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer currentRenderer = renderers[i];

                if (currentRenderer == null || HasDedicatedController(currentRenderer.transform))
                {
                    continue;
                }

                bool isBridge = BelongsToBridge(currentRenderer.transform);
                currentRenderer.enabled = !isBridge || isCurrentLayer;
                currentRenderer.sortingOrder = sortingOrder;
            }
        }

        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D currentCollider = colliders[i];

                if (currentCollider == null || HasDedicatedController(currentCollider.transform))
                {
                    continue;
                }

                currentCollider.enabled = isCurrentLayer;
            }
        }
    }

    private static bool HasDedicatedController(Transform current)
    {
        return current.GetComponentInParent<FocusGate>() != null ||
               current.GetComponentInParent<FocusDoor>() != null ||
               current.GetComponentInParent<FocusWindDoor>() != null ||
               current.GetComponentInParent<FocusLeaf>() != null ||
               current.GetComponentInParent<FocusSunGlare>() != null ||
               current.GetComponentInParent<FocusLockedDoor>() != null ||
               current.GetComponentInParent<FocusKey>() != null;
    }

    private static bool BelongsToBridge(Transform current)
    {
        while (current != null)
        {
            if (current.GetComponent<FocusBridge>() != null)
            {
                return true;
            }

            if (current.name.ToLowerInvariant().Contains("bridge"))
            {
                return true;
            }

            if (current.GetComponent<FocusLayerView>() != null)
            {
                return false;
            }

            current = current.parent;
        }

        return false;
    }
}
