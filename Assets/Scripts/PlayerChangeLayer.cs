using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerChangeLayer : MonoBehaviour
{
    private const int PlayerOrderOffset = 5;

    private FocusLevelState levelState;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        levelState = FindObjectOfType<FocusLevelState>();
        spriteRenderer = GetComponent<SpriteRenderer>();

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
            levelState.StateChanged += ApplySortingOrder;
        }
    }

    private void Start()
    {
        ApplySortingOrder();
    }

    private void Update()
    {
        if (levelState == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            levelState.SetLayer(FocusLayer.Near);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            levelState.SetLayer(FocusLayer.Mid);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            levelState.SetLayer(FocusLayer.Far);
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            levelState.SetLayer(GetPreviousLayer(levelState.CurrentLayer));
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            levelState.SetLayer(GetNextLayer(levelState.CurrentLayer));
        }
    }

    private void OnDisable()
    {
        if (levelState != null)
        {
            levelState.StateChanged -= ApplySortingOrder;
        }
    }

    private void ApplySortingOrder()
    {
        if (levelState == null || spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sortingOrder = levelState.GetSortingOrder(levelState.CurrentLayer) + PlayerOrderOffset;
    }

    private static FocusLayer GetPreviousLayer(FocusLayer currentLayer)
    {
        switch (currentLayer)
        {
            case FocusLayer.Far:
                return FocusLayer.Mid;
            case FocusLayer.Mid:
                return FocusLayer.Near;
            default:
                return FocusLayer.Far;
        }
    }

    private static FocusLayer GetNextLayer(FocusLayer currentLayer)
    {
        switch (currentLayer)
        {
            case FocusLayer.Far:
                return FocusLayer.Near;
            case FocusLayer.Mid:
                return FocusLayer.Far;
            default:
                return FocusLayer.Mid;
        }
    }
}
