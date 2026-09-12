using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum FocusLayer
{
    Far,
    Mid,
    Near
}

public enum FocusStage
{
    OpenGate,
    BuildBridge,
    ReachExit,
    WaitFirstWind,
    DoorBlown,
    LeafReady,
    LeafCollected,
    ReachedDoor,
    ExitOpen,
    WaitKey,
    HasKey
}

public sealed class FocusLevelState : MonoBehaviour
{
    public const int PlayerOrderOffset = 5;

    private const int FarSortingOrder = 10;
    private const int MidSortingOrder = 20;
    private const int NearSortingOrder = 30;

    [SerializeField] private FocusLayer initialLayer = FocusLayer.Mid;
    [SerializeField] private FocusStage initialStage = FocusStage.OpenGate;
    [SerializeField] private string playerObjectName = "Player";
    [SerializeField] private bool autoLoadNextScene = true;
    [SerializeField] private float focusTransitionDuration = 0.45f;
    [SerializeField] private float unfocusedBlur = 2.2f;

    public event Action StateChanged;

    public FocusLayer CurrentLayer { get; private set; }

    public FocusStage CurrentStage { get; private set; } = FocusStage.OpenGate;

    public int MidFocusCount { get; private set; }

    public Transform PlayerTransform { get; private set; }

    public float FocusTransitionDuration
    {
        get { return focusTransitionDuration; }
    }

    public float UnfocusedBlur
    {
        get { return unfocusedBlur; }
    }

    private void Awake()
    {
        CurrentLayer = initialLayer;
        CurrentStage = initialStage;
        EnsureCompositor();

        GameObject playerObject = GameObject.Find(playerObjectName);

        if (playerObject == null)
        {
            Debug.LogError($"Missing player object: {playerObjectName}");
            enabled = false;
            return;
        }

        PlayerTransform = playerObject.transform;
    }

    public void SetLayer(FocusLayer nextLayer)
    {
        if (CurrentLayer == nextLayer)
        {
            return;
        }

        if (nextLayer == FocusLayer.Mid)
        {
            MidFocusCount++;
        }

        CurrentLayer = nextLayer;
        NotifyStateChanged();
    }

    public void AdvanceStage(FocusStage nextStage)
    {
        if (nextStage <= CurrentStage)
        {
            return;
        }

        CurrentStage = nextStage;
        NotifyStateChanged();
    }

    public void CompleteLevel()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (autoLoadNextScene && nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
            return;
        }

        Debug.Log("Level clear.");
    }

    public int GetSortingOrder(FocusLayer layer)
    {
        return GetSortingOrder(layer, 0);
    }

    public int GetSortingOrder(FocusLayer layer, int orderOffset)
    {
        switch (layer)
        {
            case FocusLayer.Near:
                return NearSortingOrder + orderOffset;
            case FocusLayer.Mid:
                return MidSortingOrder + orderOffset;
            default:
                return FarSortingOrder + orderOffset;
        }
    }

    private void NotifyStateChanged()
    {
        Action handler = StateChanged;

        if (handler != null)
        {
            handler();
        }
    }

    private static void EnsureCompositor()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera != null && mainCamera.GetComponent<FocusLayerCompositor>() == null)
        {
            mainCamera.gameObject.AddComponent<FocusLayerCompositor>();
        }
    }

    private void OnValidate()
    {
        focusTransitionDuration = Mathf.Max(0.01f, focusTransitionDuration);
        unfocusedBlur = Mathf.Max(0f, unfocusedBlur);
    }
}
