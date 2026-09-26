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

        if (CurrentLayer == FocusLayer.Mid)
        {
            MidFocusCount = 1;
        }

        EnsureCompositor();
        RestartLevel.EnsureHud();

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

    private static bool IsAnyIceHeld()
    {
        FocusIce[] iceBlocks = FindObjectsOfType<FocusIce>();
        for (int i = 0; i < iceBlocks.Length; i++)
        {
            if (iceBlocks[i] != null && iceBlocks[i].IsHeld)
            {
                return true;
            }
        }

        return false;
    }

    public void TryEnterDoor(Transform door, float enterDistance, bool hardConditionMet)
    {
        if (!hardConditionMet || door == null || PlayerTransform == null || !Input.GetKeyDown(KeyCode.F) || IsAnyIceHeld())
        {
            return;
        }

        if (!IsPlayerAtDoor(door, PlayerTransform, enterDistance))
        {
            return;
        }

        CompleteLevel();
    }

    private static bool IsPlayerAtDoor(Transform door, Transform player, float enterDistance)
    {
        Vector2 playerPos = player.position;
        Collider2D doorCollider = door.GetComponent<Collider2D>();

        if (doorCollider != null)
        {
            Vector2 closest = doorCollider.ClosestPoint(playerPos);
            if (Vector2.Distance(playerPos, closest) <= enterDistance)
            {
                return true;
            }

            Bounds bounds = doorCollider.bounds;
            float dx = Mathf.Abs(playerPos.x - bounds.center.x) - bounds.extents.x;
            float dy = Mathf.Abs(playerPos.y - bounds.center.y) - bounds.extents.y;
            return dx <= enterDistance && dy <= enterDistance * 2f;
        }

        return Vector2.Distance(playerPos, (Vector2)door.position) <= enterDistance;
    }

    public void CompleteLevel()
    {
        if (!autoLoadNextScene)
        {
            Debug.Log("Level clear.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        string nextSceneName = GetNextLevelName(activeScene.name);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        int nextBuildIndex = activeScene.buildIndex + 1;

        if (activeScene.buildIndex >= 0 && nextBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextBuildIndex);
            return;
        }

        Debug.Log("Level clear.");
    }

    private static string GetNextLevelName(string currentName)
    {
        if (currentName == "Level1" || currentName == "1")
        {
            return "Level2";
        }

        if (currentName == "Level2" || currentName == "2")
        {
            return "Level3";
        }

        if (currentName == "Level3" || currentName == "3")
        {
            return "Level4";
        }

        if (currentName == "Level4" || currentName == "4")
        {
            return "Level5";
        }

        if (currentName == "Level5" || currentName == "5")
        {
            return "Level6";
        }

        if (currentName == "Level6" || currentName == "6")
        {
            return "Level7";
        }

        if (currentName == "Level7" || currentName == "7")
        {
            return "Level8";
        }

        if (currentName == "Level8" || currentName == "8")
        {
            return "Level9";
        }

        if (currentName == "Level9" || currentName == "9")
        {
            return "Level10";
        }

        if (currentName == "Level10" || currentName == "10")
        {
            return "Level11";
        }

        return null;
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
