using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Captures the full screen (game + UI) after rendering and applies the CRT shader on top.
/// No PanelSettings.targetTexture needed — UI input works normally.
///
/// IMPORTANT: Remove the CRT Renderer Feature from your Renderer2D asset when using this.
/// This script replaces the renderer feature approach entirely.
/// </summary>
public class CRTUIBridge : MonoBehaviour
{
    [Tooltip("The CRT material (same one used by CRTController)")]
    public Material crtMaterial;

    private RenderTexture _captureRT;
    private static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
    private static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");

    void OnEnable()
    {
        StartCoroutine(CRTLoop());
    }

    void OnDisable()
    {
        if (_captureRT != null)
        {
            _captureRT.Release();
            Destroy(_captureRT);
            _captureRT = null;
        }
    }

    IEnumerator CRTLoop()
    {
        var waitEOF = new WaitForEndOfFrame();
        while (true)
        {
            yield return waitEOF;

            if (crtMaterial == null) continue;

            // Resize capture RT if screen resolution changed
            if (_captureRT == null || _captureRT.width != Screen.width || _captureRT.height != Screen.height)
            {
                if (_captureRT != null) { _captureRT.Release(); Destroy(_captureRT); }
                _captureRT = new RenderTexture(Screen.width, Screen.height, 0);
            }

            // Capture the full screen (game world + UI overlay) into RT
            ScreenCapture.CaptureScreenshotIntoRenderTexture(_captureRT);

            // Blit.hlsl's vertex shader needs _BlitScaleBias for UV mapping.
            // Outside the render pipeline this isn't set, so we provide identity (scale 1, offset 0).
            crtMaterial.SetVector(BlitScaleBiasId, new Vector4(1, 1, 0, 0));
            crtMaterial.SetTexture(BlitTextureId, _captureRT);

            // Reset render target to the screen backbuffer and draw
            RenderTexture.active = null;
            crtMaterial.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Triangles, 3);
        }
    }
}
