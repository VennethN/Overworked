using UnityEngine;
using UnityEngine.InputSystem;

[ExecuteAlways]
public class CRTController : MonoBehaviour
{
    [Header("CRT Material")]
    public Material crtMaterial;

    [Header("Screen Curvature")]
    [Range(0f, 10f)] public float curvature = 3f;

    [Header("Scanlines")]
    [Range(0f, 1f)] public float scanlineIntensity = 0.25f;
    [Range(0f, 5f)] public float scanlineSpeed = 0.5f;

    [Header("Color")]
    [Range(0f, 0.02f)] public float rgbOffset = 0.003f;
    [Range(0.5f, 2f)] public float brightness = 1.1f;

    [Header("Effects")]
    [Range(0f, 2f)] public float vignetteStrength = 0.8f;
    [Range(0f, 0.1f)] public float flicker = 0.02f;

    [Header("Static Wave")]
    [Range(0f, 0.2f)] public float staticIntensity = 0.05f;
    [Range(0f, 10f)] public float staticSpeed = 2f;
    [Range(0.01f, 0.3f)] public float staticWidth = 0.1f;
    [Range(0f, 0.3f)] public float noiseIntensity = 0.05f;
    [Range(0f, 1f)] public float staticDisruption = 0.3f;

    [Header("Click Glitch")]
    [Range(0.02f, 0.4f)] public float clickRadius = 0.15f;
    [Range(0f, 1f)] public float clickStrength = 0.6f;
    [Range(0.1f, 1f)] public float clickDuration = 0.35f;

    private static readonly int CurvatureId = Shader.PropertyToID("_Curvature");
    private static readonly int ScanlineIntensityId = Shader.PropertyToID("_ScanlineIntensity");
    private static readonly int ScanlineSpeedId = Shader.PropertyToID("_ScanlineSpeed");
    private static readonly int RGBOffsetId = Shader.PropertyToID("_RGBOffset");
    private static readonly int VignetteStrengthId = Shader.PropertyToID("_VignetteStrength");
    private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
    private static readonly int FlickerId = Shader.PropertyToID("_Flicker");
    private static readonly int StaticIntensityId = Shader.PropertyToID("_StaticIntensity");
    private static readonly int StaticSpeedId = Shader.PropertyToID("_StaticSpeed");
    private static readonly int StaticWidthId = Shader.PropertyToID("_StaticWidth");
    private static readonly int NoiseIntensityId = Shader.PropertyToID("_NoiseIntensity");
    private static readonly int StaticDisruptionId = Shader.PropertyToID("_StaticDisruption");
    private static readonly int ClickUVId = Shader.PropertyToID("_ClickUV");
    private static readonly int ClickTimeId = Shader.PropertyToID("_ClickTime");
    private static readonly int ClickRadiusId = Shader.PropertyToID("_ClickRadius");
    private static readonly int ClickStrengthId = Shader.PropertyToID("_ClickStrength");
    private static readonly int ClickDurationId = Shader.PropertyToID("_ClickDuration");
    private static readonly int UnscaledTimeId = Shader.PropertyToID("_UnscaledTime");

    void Update()
    {
        if (crtMaterial == null)
            return;

        // Detect click and send UV position to shader
        if (Application.isPlaying)
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 screenPos = mouse.position.ReadValue();
#if UNITY_WEBGL && !UNITY_EDITOR
                Vector2 clickUV = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
#else
                Vector2 clickUV = new Vector2(screenPos.x / Screen.width, 1f - screenPos.y / Screen.height);
#endif
                crtMaterial.SetVector(ClickUVId, clickUV);
                crtMaterial.SetFloat(ClickTimeId, Time.unscaledTime);
            }
        }

        crtMaterial.SetFloat(CurvatureId, curvature);
        crtMaterial.SetFloat(ScanlineIntensityId, scanlineIntensity);
        crtMaterial.SetFloat(ScanlineSpeedId, scanlineSpeed);
        crtMaterial.SetFloat(RGBOffsetId, rgbOffset);
        crtMaterial.SetFloat(VignetteStrengthId, vignetteStrength);
        crtMaterial.SetFloat(BrightnessId, brightness);
        crtMaterial.SetFloat(FlickerId, flicker);
        crtMaterial.SetFloat(StaticIntensityId, staticIntensity);
        crtMaterial.SetFloat(StaticSpeedId, staticSpeed);
        crtMaterial.SetFloat(StaticWidthId, staticWidth);
        crtMaterial.SetFloat(NoiseIntensityId, noiseIntensity);
        crtMaterial.SetFloat(StaticDisruptionId, staticDisruption);
        crtMaterial.SetFloat(ClickRadiusId, clickRadius);
        crtMaterial.SetFloat(ClickStrengthId, clickStrength);
        crtMaterial.SetFloat(ClickDurationId, clickDuration);
        crtMaterial.SetFloat(UnscaledTimeId, Time.unscaledTime);
    }
}
