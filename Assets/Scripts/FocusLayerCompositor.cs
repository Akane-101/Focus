using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class FocusLayerCompositor : MonoBehaviour
{
    private const int BlurPasses = 5;
    private const int FillPass = 2;
    private const int BlendPass = 1;
    private const int KawasePass = 0;
    private const string FarObjectName = "Depth_Far";
    private const string MidObjectName = "Depth_Mid";
    private const string NearObjectName = "Depth_Near";

    private static readonly FocusLayer[] CompositeOrder =
    {
        FocusLayer.Far,
        FocusLayer.Mid,
        FocusLayer.Near
    };

    private Camera mainCamera;
    private FocusLevelState levelState;
    private Material fxMaterial;
    private Color backgroundColor;

    private readonly Camera[] layerCameras = new Camera[3];
    private readonly RenderTexture[] layerTextures = new RenderTexture[3];
    private readonly float[] sharpness = new float[3];

    private Camera playerCamera;
    private RenderTexture playerTexture;
    private int textureWidth;
    private int textureHeight;
    private int originalCullingMask;
    private bool hasCapturedBackground;

    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
        originalCullingMask = mainCamera.cullingMask;
        mainCamera.allowMSAA = false;
    }

    private void Start()
    {
        levelState = FindObjectOfType<FocusLevelState>();
        Shader fxShader = Shader.Find("Hidden/FocusLayerFX");

        if (levelState == null || fxShader == null)
        {
            Debug.LogError("Focus layer compositor is missing FocusLevelState or Hidden/FocusLayerFX.");
            enabled = false;
            return;
        }

        fxMaterial = new Material(fxShader);
        fxMaterial.hideFlags = HideFlags.HideAndDontSave;
        AssignWorldLayers();
        AssignPlayerLayer();
        SnapSharpness();
        CaptureBackground();
        ExcludeFocusLayersFromMainCamera();
        EnsureResources();
    }

    private void LateUpdate()
    {
        if (levelState == null || fxMaterial == null)
        {
            return;
        }

        AnimateSharpness();
        EnsureResources();
        SyncLayerCameras();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (fxMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        RenderTexture composite = RenderTexture.GetTemporary(source.descriptor);
        fxMaterial.SetColor("_Color", backgroundColor);
        Graphics.Blit(source, composite, fxMaterial, FillPass);
        Graphics.Blit(source, composite, fxMaterial, BlendPass);

        for (int i = 0; i < CompositeOrder.Length; i++)
        {
            FocusLayer layer = CompositeOrder[i];
            RenderTexture layerTexture = layerTextures[(int)layer];

            if (layerTexture == null)
            {
                continue;
            }

            float blurAmount = (1f - Mathf.SmoothStep(0f, 1f, sharpness[(int)layer])) * levelState.UnfocusedBlur;
            RenderTexture blurred = BlurLayer(layerTexture, source.descriptor, blurAmount);
            Graphics.Blit(blurred, composite, fxMaterial, BlendPass);

            if (blurred != layerTexture)
            {
                RenderTexture.ReleaseTemporary(blurred);
            }

            if (layer == levelState.CurrentLayer && playerTexture != null && !DrawsPlayerInLayer(layer))
            {
                Graphics.Blit(playerTexture, composite, fxMaterial, BlendPass);
            }
        }

        Graphics.Blit(composite, destination);
        RenderTexture.ReleaseTemporary(composite);
    }

    private void OnDestroy()
    {
        if (mainCamera != null)
        {
            mainCamera.cullingMask = originalCullingMask;
        }

        for (int i = 0; i < layerCameras.Length; i++)
        {
            if (layerCameras[i] != null)
            {
                Destroy(layerCameras[i].gameObject);
            }
        }

        if (playerCamera != null)
        {
            Destroy(playerCamera.gameObject);
        }

        ReleaseLayerTextures();

        if (fxMaterial != null)
        {
            Destroy(fxMaterial);
        }
    }

    private void AssignWorldLayers()
    {
        AssignNamedLayer(FarObjectName, FocusLayer.Far);
        AssignNamedLayer(MidObjectName, FocusLayer.Mid);
        AssignNamedLayer(NearObjectName, FocusLayer.Near);
    }

    private static void AssignNamedLayer(string objectName, FocusLayer layer)
    {
        GameObject layerObject = GameObject.Find(objectName);

        if (layerObject != null)
        {
            FocusUnityLayers.Assign(layerObject.transform, layer);
        }
    }

    private void AssignPlayerLayer()
    {
        if (levelState != null && levelState.PlayerTransform != null)
        {
            int playerLayer = LayerMask.NameToLayer(FocusUnityLayers.Player);

            if (playerLayer >= 0)
            {
                FocusUnityLayers.Assign(levelState.PlayerTransform, playerLayer);
            }
        }
    }

    private void SnapSharpness()
    {
        for (int i = 0; i < sharpness.Length; i++)
        {
            sharpness[i] = levelState.CurrentLayer == (FocusLayer)i ? 1f : 0f;
        }
    }

    private void AnimateSharpness()
    {
        float duration = Mathf.Max(0.01f, levelState.FocusTransitionDuration);
        float step = Time.deltaTime / duration;

        for (int i = 0; i < sharpness.Length; i++)
        {
            float target = levelState.CurrentLayer == (FocusLayer)i ? 1f : 0f;
            sharpness[i] = Mathf.MoveTowards(sharpness[i], target, step);
        }
    }

    private void CaptureBackground()
    {
        if (hasCapturedBackground)
        {
            return;
        }

        backgroundColor = mainCamera.backgroundColor;
        backgroundColor.a = 1f;
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        hasCapturedBackground = true;
    }

    private void ExcludeFocusLayersFromMainCamera()
    {
        mainCamera.cullingMask = originalCullingMask & ~FocusUnityLayers.GetCullingMask();
    }

    private void EnsureResources()
    {
        int width = Mathf.Max(2, mainCamera.pixelWidth);
        int height = Mathf.Max(2, mainCamera.pixelHeight);

        if (textureWidth == width && textureHeight == height && layerTextures[0] != null && playerTexture != null)
        {
            return;
        }

        textureWidth = width;
        textureHeight = height;
        ReleaseLayerTextures();

        for (int i = 0; i < CompositeOrder.Length; i++)
        {
            FocusLayer layer = CompositeOrder[i];
            int layerIndex = (int)layer;

            RenderTexture layerTexture = CreateLayerTexture("FocusLayerRT_" + layer);
            layerTextures[layerIndex] = layerTexture;

            if (layerCameras[layerIndex] == null)
            {
                layerCameras[layerIndex] = CreateLayerCamera("FocusLayerCamera_" + layer);
            }
        }

        if (playerTexture == null)
        {
            playerTexture = CreateLayerTexture("FocusLayerRT_Player");
        }

        if (playerCamera == null)
        {
            playerCamera = CreateLayerCamera("FocusLayerCamera_Player");
        }
    }

    private RenderTexture CreateLayerTexture(string textureName)
    {
        RenderTexture layerTexture = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.ARGB32)
        {
            name = textureName,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            antiAliasing = 1
        };
        layerTexture.Create();
        return layerTexture;
    }

    private Camera CreateLayerCamera(string cameraName)
    {
        GameObject cameraObject = new GameObject(cameraName);
        cameraObject.hideFlags = HideFlags.HideAndDontSave;
        cameraObject.transform.SetParent(transform, false);

        Camera layerCamera = cameraObject.AddComponent<Camera>();
        layerCamera.enabled = true;
        layerCamera.allowMSAA = false;
        layerCamera.allowHDR = false;
        layerCamera.depth = mainCamera.depth - 4;
        return layerCamera;
    }

    private void SyncLayerCameras()
    {
        for (int i = 0; i < CompositeOrder.Length; i++)
        {
            FocusLayer layer = CompositeOrder[i];
            int layerIndex = (int)layer;
            Camera layerCamera = layerCameras[layerIndex];
            int unityLayer = FocusUnityLayers.GetIndex(layer);

            if (layerCamera == null || unityLayer < 0 || layerTextures[layerIndex] == null)
            {
                continue;
            }

            int cullingMask = 1 << unityLayer;

            if (DrawsPlayerInLayer(layer))
            {
                int playerLayer = LayerMask.NameToLayer(FocusUnityLayers.Player);

                if (playerLayer >= 0)
                {
                    cullingMask |= 1 << playerLayer;
                }
            }

            ConfigureOverlayCamera(layerCamera, layerTextures[layerIndex], cullingMask, mainCamera.depth - 4 + layerIndex);
        }

        int overlayPlayerLayer = LayerMask.NameToLayer(FocusUnityLayers.Player);
        bool drawPlayerOverlay = !DrawsPlayerInLayer(levelState.CurrentLayer);

        if (playerCamera != null && playerTexture != null && overlayPlayerLayer >= 0 && drawPlayerOverlay)
        {
            ConfigureOverlayCamera(playerCamera, playerTexture, 1 << overlayPlayerLayer, mainCamera.depth - 1);
        }
        else if (playerCamera != null)
        {
            playerCamera.enabled = false;
            playerCamera.targetTexture = null;
        }
    }

    private bool DrawsPlayerInLayer(FocusLayer layer)
    {
        return layer == FocusLayer.Mid && levelState != null && levelState.CurrentLayer == FocusLayer.Mid;
    }

    private void ConfigureOverlayCamera(Camera overlayCamera, RenderTexture targetTexture, int cullingMask, float depth)
    {
        overlayCamera.CopyFrom(mainCamera);
        overlayCamera.enabled = true;
        overlayCamera.allowMSAA = false;
        overlayCamera.allowHDR = false;
        overlayCamera.targetTexture = targetTexture;
        overlayCamera.cullingMask = cullingMask;
        overlayCamera.clearFlags = CameraClearFlags.SolidColor;
        overlayCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        overlayCamera.depth = depth;
    }

    private RenderTexture BlurLayer(RenderTexture source, RenderTextureDescriptor descriptor, float blurAmount)
    {
        if (blurAmount <= 0.01f)
        {
            return source;
        }

        RenderTextureDescriptor blurDescriptor = descriptor;
        blurDescriptor.depthBufferBits = 0;
        blurDescriptor.msaaSamples = 1;

        RenderTexture bufferA = RenderTexture.GetTemporary(blurDescriptor);
        RenderTexture bufferB = RenderTexture.GetTemporary(blurDescriptor);
        Graphics.Blit(source, bufferA);

        for (int i = 0; i < BlurPasses; i++)
        {
            fxMaterial.SetFloat("_Offset", (0.5f + i) * blurAmount);
            Graphics.Blit(bufferA, bufferB, fxMaterial, KawasePass);

            RenderTexture swap = bufferA;
            bufferA = bufferB;
            bufferB = swap;
        }

        RenderTexture.ReleaseTemporary(bufferB);
        return bufferA;
    }

    private void ReleaseLayerTextures()
    {
        for (int i = 0; i < layerTextures.Length; i++)
        {
            if (layerTextures[i] != null)
            {
                layerTextures[i].Release();
                Destroy(layerTextures[i]);
                layerTextures[i] = null;
            }
        }

        if (playerTexture != null)
        {
            playerTexture.Release();
            Destroy(playerTexture);
            playerTexture = null;
        }
    }
}
