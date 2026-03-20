using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Redirects UI Toolkit rendering to a RenderTexture so the CRT shader
/// can composite and distort it together with the game world.
/// Attach to the same GameObject as CRTController.
/// </summary>
public class CRTUIBridge : MonoBehaviour
{
    [Tooltip("The PanelSettings asset used by your UIDocument")]
    public PanelSettings panelSettings;

    [Tooltip("The CRT material (same one used by CRTRendererFeature)")]
    public Material crtMaterial;

    private RenderTexture _uiRT;
    private int _lastWidth;
    private int _lastHeight;

    private static readonly int UIRTId = Shader.PropertyToID("_UIRT");
    private static readonly int HasUIRTId = Shader.PropertyToID("_HasUIRT");

    void OnEnable()
    {
        CreateRT();
    }

    void OnDisable()
    {
        ReleaseRT();
    }

    void Update()
    {
        // Recreate RT if screen resolution changes
        if (Screen.width != _lastWidth || Screen.height != _lastHeight)
        {
            ReleaseRT();
            CreateRT();
        }
    }

    private void CreateRT()
    {
        if (panelSettings == null || crtMaterial == null)
            return;

        _lastWidth = Screen.width;
        _lastHeight = Screen.height;

        _uiRT = new RenderTexture(_lastWidth, _lastHeight, 0, RenderTextureFormat.ARGB32);
        _uiRT.Create();

        panelSettings.targetTexture = _uiRT;

        // Map screen coordinates to panel coordinates so input still works.
        // Screen space: (0,0) bottom-left, Y up.
        // Panel space: (0,0) top-left, Y down.
        panelSettings.SetScreenToPanelSpaceFunction(ScreenToPanel);

        crtMaterial.SetTexture(UIRTId, _uiRT);
        crtMaterial.SetFloat(HasUIRTId, 1f);
    }

    private Vector2 ScreenToPanel(Vector2 screenPos)
    {
        if (_uiRT == null)
            return new Vector2(float.NaN, float.NaN);

        float x = screenPos.x * _uiRT.width / Screen.width;
        float y = (Screen.height - screenPos.y) * _uiRT.height / Screen.height;
        return new Vector2(x, y);
    }

    private void ReleaseRT()
    {
        if (crtMaterial != null)
        {
            crtMaterial.SetTexture(UIRTId, null);
            crtMaterial.SetFloat(HasUIRTId, 0f);
        }

        if (panelSettings != null)
        {
            panelSettings.SetScreenToPanelSpaceFunction(null);
            panelSettings.targetTexture = null;
        }

        if (_uiRT != null)
        {
            _uiRT.Release();
            Destroy(_uiRT);
            _uiRT = null;
        }
    }
}
