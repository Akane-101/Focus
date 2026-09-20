using UnityEngine;

public sealed class FocusLayerView : MonoBehaviour
{
    [SerializeField] private FocusLayer representedLayer = FocusLayer.Mid;

    private FocusLevelState levelState;
    private SpriteRenderer[] renderers;
    private Collider2D[] colliders;
    private int[] implicitOrderOffsets;

    private void Awake()
    {
        levelState = FindObjectOfType<FocusLevelState>();
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);
        implicitOrderOffsets = CacheImplicitOrderOffsets(renderers, representedLayer, levelState);
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

        int baseSortingOrder = levelState.GetSortingOrder(representedLayer);
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
                currentRenderer.sortingOrder = baseSortingOrder + GetOrderOffset(currentRenderer, i);
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

    private int GetOrderOffset(SpriteRenderer currentRenderer, int rendererIndex)
    {
        FocusSortOrder sortOrder = FocusSortOrder.FindOnAncestors(currentRenderer.transform);

        if (sortOrder != null)
        {
            return sortOrder.GetOrderOffset();
        }

        if (implicitOrderOffsets != null && rendererIndex >= 0 && rendererIndex < implicitOrderOffsets.Length)
        {
            return implicitOrderOffsets[rendererIndex];
        }

        return 0;
    }

    private static int[] CacheImplicitOrderOffsets(SpriteRenderer[] layerRenderers, FocusLayer layer, FocusLevelState levelState)
    {
        int[] offsets = new int[layerRenderers.Length];

        if (layer != FocusLayer.Mid)
        {
            return offsets;
        }

        int baseSortingOrder = levelState != null ? levelState.GetSortingOrder(layer) : 20;

        for (int i = 0; i < layerRenderers.Length; i++)
        {
            SpriteRenderer currentRenderer = layerRenderers[i];

            if (currentRenderer == null || FocusSortOrder.FindOnAncestors(currentRenderer.transform) != null)
            {
                continue;
            }

            int existingOrder = currentRenderer.sortingOrder;
            offsets[i] = existingOrder == 0 ? 0 : existingOrder - baseSortingOrder;
        }

        return offsets;
    }

    private static bool HasDedicatedController(Transform current)
    {
        return current.GetComponentInParent<FocusGate>() != null ||
               current.GetComponentInParent<FocusDoor>() != null ||
               current.GetComponentInParent<FocusWindDoor>() != null ||
               current.GetComponentInParent<FocusLeaf>() != null ||
               current.GetComponentInParent<FocusSunGlare>() != null ||
               current.GetComponentInParent<FocusLockedDoor>() != null ||
               current.GetComponentInParent<FocusKey>() != null ||
               current.GetComponentInParent<FocusPillar>() != null ||
               current.GetComponentInParent<FocusSlope>() != null ||
               current.GetComponentInParent<FocusElevator>() != null;
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
