mergeInto(LibraryManager.library, {
    /**
     * Force browser focus onto the Unity canvas element.
     * On WebGL, the canvas must have browser focus for keyboard events
     * to reach Unity (and thus UI Toolkit TextFields).
     */
    WebGLFocusCanvas: function () {
        var canvas = document.querySelector('#unity-canvas') || document.querySelector('canvas');
        if (canvas) {
            canvas.focus();
        }
    },

    /**
     * Set up persistent keyboard capture so the Unity canvas always
     * receives keyboard events without requiring the user to click it.
     * Call once at game start.
     */
    WebGLSetupKeyboardCapture: function () {
        var canvas = document.querySelector('#unity-canvas') || document.querySelector('canvas');
        if (!canvas || canvas._keyboardCaptureSetup) return;
        canvas._keyboardCaptureSetup = true;

        // Make canvas focusable
        canvas.setAttribute('tabindex', '0');

        // Re-focus canvas on any click within the page
        document.addEventListener('mousedown', function () {
            setTimeout(function () { canvas.focus(); }, 0);
        });

        // Re-focus canvas when it loses focus (unless an input/textarea needs it)
        canvas.addEventListener('blur', function () {
            setTimeout(function () {
                var active = document.activeElement;
                var tag = active ? active.tagName.toLowerCase() : '';
                // Don't steal focus from native input elements (e.g. mobile keyboard)
                if (tag !== 'input' && tag !== 'textarea' && tag !== 'select') {
                    canvas.focus();
                }
            }, 0);
        });

        // Forward keyboard events from document to canvas when canvas doesn't have focus
        function forwardKey(e) {
            if (document.activeElement === canvas) return;
            var active = document.activeElement;
            var tag = active ? active.tagName.toLowerCase() : '';
            if (tag === 'input' || tag === 'textarea' || tag === 'select') return;
            canvas.focus();
        }
        document.addEventListener('keydown', forwardKey, true);

        // Initial focus
        canvas.focus();
    }
});
