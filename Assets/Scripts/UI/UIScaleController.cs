using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Lets players adjust UI scale at runtime.
/// Press Ctrl+/Ctrl= to increase, Ctrl- to decrease, Ctrl+0 to reset.
/// Persists the chosen scale across sessions via PlayerPrefs.
/// </summary>
public class UIScaleController : MonoBehaviour
{
    [Header("References")]
    public PanelSettings panelSettings;
    public UIDocument uiDocument;

    [Header("Scale Settings")]
    [Range(0.5f, 2f)] public float uiScale = 1.5f;
    public float scaleStep = 0.1f;
    public float minScale = 0.5f;
    public float maxScale = 2f;

    private const string PREFS_KEY = "UIScale";
    private float _lastAppliedScale;

    public float MinScale => minScale;
    public float MaxScale => maxScale;
    public float CurrentScale => uiScale;

    public System.Action<float> OnScaleChanged;

    public void SetScale(float value)
    {
        uiScale = Mathf.Clamp(value, minScale, maxScale);
        ApplyScale();
        OnScaleChanged?.Invoke(uiScale);
    }

    void OnEnable()
    {
        float defaultScale = Application.isMobilePlatform ? 1f : 1.2f;
        uiScale = PlayerPrefs.GetFloat(PREFS_KEY, defaultScale);
        ApplyScale();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed
                  || kb.leftCommandKey.isPressed || kb.rightCommandKey.isPressed;
        bool changed = false;

        if (!ctrl) goto skipKeys;

        // Ctrl + / Ctrl = to increase
        if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame)
        {
            uiScale = Mathf.Min(uiScale + scaleStep, maxScale);
            changed = true;
        }

        // Ctrl - to decrease
        if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame)
        {
            uiScale = Mathf.Max(uiScale - scaleStep, minScale);
            changed = true;
        }

        // Ctrl 0 to reset
        if (kb.digit0Key.wasPressedThisFrame)
        {
            uiScale = 1f;
            changed = true;
        }

        skipKeys:

        if (changed)
        {
            ApplyScale();
            OnScaleChanged?.Invoke(uiScale);
        }

        // Also apply if changed from Inspector
        if (!Mathf.Approximately(uiScale, _lastAppliedScale))
        {
            ApplyScale();
            OnScaleChanged?.Invoke(uiScale);
        }
    }

    private void ApplyScale()
    {
        uiScale = Mathf.Clamp(uiScale, minScale, maxScale);

        if (panelSettings != null)
            panelSettings.scale = uiScale;

        _lastAppliedScale = uiScale;
        PlayerPrefs.SetFloat(PREFS_KEY, uiScale);
    }
}
