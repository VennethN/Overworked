using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace Overworked.UI
{
    /// <summary>
    /// On WebGL the browser canvas must have focus for keyboard events to reach Unity.
    /// This helper ensures both browser-level canvas focus and UI Toolkit TextField focus
    /// are maintained so the user can type without needing to click first.
    /// </summary>
    public static class WebGLTextFieldFix
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void WebGLFocusCanvas();

        [DllImport("__Internal")]
        private static extern void WebGLSetupKeyboardCapture();

        private static bool _captureInitialized;

        /// <summary>
        /// Call once at game start to set up persistent keyboard capture.
        /// The browser will automatically re-focus the canvas whenever it loses focus.
        /// </summary>
        public static void InitKeyboardCapture()
        {
            if (_captureInitialized) return;
            _captureInitialized = true;
            WebGLSetupKeyboardCapture();
        }

        /// <summary>
        /// Focus a TextField and keep it focused.
        /// Re-focuses both the browser canvas and the UI Toolkit field whenever focus is lost.
        /// Call StopKeepFocus when the field should no longer hold focus (e.g. minigame ends).
        /// </summary>
        public static void FocusTextField(TextField field)
        {
            if (field == null) return;

            InitKeyboardCapture();
            WebGLFocusCanvas();

            field.schedule.Execute(() =>
            {
                field.Focus();
                var textInput = field.Q("unity-text-input");
                textInput?.Focus();
            }).ExecuteLater(150);

            // Re-focus the field whenever it loses UI Toolkit focus
            field.UnregisterCallback<FocusOutEvent>(OnFocusOut);
            field.RegisterCallback<FocusOutEvent>(OnFocusOut);
        }

        /// <summary>
        /// Stop the persistent re-focus behavior on a TextField.
        /// </summary>
        public static void StopKeepFocus(TextField field)
        {
            if (field == null) return;
            field.UnregisterCallback<FocusOutEvent>(OnFocusOut);
        }

        private static void OnFocusOut(FocusOutEvent evt)
        {
            if (evt.target is not VisualElement ve) return;

            // Walk up to find the TextField
            var field = ve as TextField ?? ve.GetFirstAncestorOfType<TextField>();
            if (field == null || field.panel == null) return;

            // Re-focus after a tiny delay so the click that caused blur can finish
            field.schedule.Execute(() =>
            {
                // Only re-focus if the field is still attached and visible
                if (field.panel == null) return;
                if (field.resolvedStyle.display == DisplayStyle.None) return;
                if (!field.enabledInHierarchy) return;

                WebGLFocusCanvas();
                field.Focus();
            }).ExecuteLater(50);
        }
#else
        public static void InitKeyboardCapture() { }

        public static void FocusTextField(TextField field)
        {
            if (field == null) return;

            field.schedule.Execute(() => field.Focus()).ExecuteLater(100);

            field.UnregisterCallback<FocusOutEvent>(OnFocusOut);
            field.RegisterCallback<FocusOutEvent>(OnFocusOut);
        }

        public static void StopKeepFocus(TextField field)
        {
            if (field == null) return;
            field.UnregisterCallback<FocusOutEvent>(OnFocusOut);
        }

        private static void OnFocusOut(FocusOutEvent evt)
        {
            if (evt.target is not VisualElement ve) return;
            var field = ve as TextField ?? ve.GetFirstAncestorOfType<TextField>();
            if (field == null || field.panel == null) return;

            field.schedule.Execute(() =>
            {
                if (field.panel == null) return;
                if (field.resolvedStyle.display == DisplayStyle.None) return;
                if (!field.enabledInHierarchy) return;
                field.Focus();
            }).ExecuteLater(50);
        }
#endif
    }
}
