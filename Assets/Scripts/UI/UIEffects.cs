using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Overworked.UI
{
    /// <summary>
    /// Static utility for UI juice effects: shake, pop, flash, floating text.
    /// All methods operate on VisualElements via scheduled animations.
    /// </summary>
    public static class UIEffects
    {
        // ── Screen Shake ──
        // Shakes the given root element (typically the UI root)
        public static void Shake(VisualElement target, float intensity = 6f, int steps = 6)
        {
            if (target == null) return;
            int step = 0;
            target.schedule.Execute(() =>
            {
                if (step >= steps)
                {
                    target.style.translate = new Translate(0, 0);
                    return;
                }
                float decay = 1f - (float)step / steps;
                float x = Random.Range(-intensity, intensity) * decay;
                float y = Random.Range(-intensity, intensity) * decay;
                target.style.translate = new Translate(x, y);
                step++;
            }).Every(40).ForDuration(steps * 40);
        }

        // ── Scale Pop ──
        // Quick scale up then back to 1 (satisfying click feedback)
        public static void Pop(VisualElement target, float scale = 1.08f, int durationMs = 120)
        {
            if (target == null) return;
            target.style.scale = new Scale(new Vector2(scale, scale));
            target.schedule.Execute(() =>
            {
                target.style.scale = new Scale(Vector2.one);
            }).ExecuteLater(durationMs);
        }

        // ── Punch Scale ──
        // Scale down then back (press feel)
        public static void Punch(VisualElement target, float scale = 0.95f, int durationMs = 80)
        {
            if (target == null) return;
            target.style.scale = new Scale(new Vector2(scale, scale));
            target.schedule.Execute(() =>
            {
                target.style.scale = new Scale(Vector2.one);
            }).ExecuteLater(durationMs);
        }

        // ── Color Flash ──
        // Flash an element's background color then revert
        public static void FlashColor(VisualElement target, Color flashColor, int durationMs = 200)
        {
            if (target == null) return;
            var original = target.resolvedStyle.backgroundColor;
            target.style.backgroundColor = flashColor;
            target.schedule.Execute(() =>
            {
                target.style.backgroundColor = original;
            }).ExecuteLater(durationMs);
        }

        // ── Border Flash ──
        // Flash border color (good for input validation)
        public static void FlashBorder(VisualElement target, Color flashColor, int durationMs = 300)
        {
            if (target == null) return;
            target.style.borderTopColor = flashColor;
            target.style.borderBottomColor = flashColor;
            target.style.borderLeftColor = flashColor;
            target.style.borderRightColor = flashColor;
            target.schedule.Execute(() =>
            {
                target.style.borderTopColor = StyleKeyword.Null;
                target.style.borderBottomColor = StyleKeyword.Null;
                target.style.borderLeftColor = StyleKeyword.Null;
                target.style.borderRightColor = StyleKeyword.Null;
            }).ExecuteLater(durationMs);
        }

        // ── Floating Score Text ──
        // Shows +10 or -5 floating upward and fading out
        public static void FloatingText(VisualElement parent, string text, Color color, Vector2 position)
        {
            if (parent == null) return;

            var label = new Label(text);
            label.style.position = Position.Absolute;
            label.style.left = position.x;
            label.style.top = position.y;
            label.style.fontSize = 13;
            label.style.color = color;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.opacity = 1f;
            label.style.translate = new Translate(0, 0);
            label.style.transitionProperty = new List<StylePropertyName>
            {
                new("opacity"),
                new("translate")
            };
            label.style.transitionDuration = new List<TimeValue>
            {
                new(600, TimeUnit.Millisecond),
                new(600, TimeUnit.Millisecond)
            };
            label.style.transitionTimingFunction = new List<EasingFunction>
            {
                new(EasingMode.EaseOut),
                new(EasingMode.EaseOut)
            };
            label.pickingMode = PickingMode.Ignore;
            parent.Add(label);

            // Trigger animation next frame
            label.schedule.Execute(() =>
            {
                label.style.opacity = 0f;
                label.style.translate = new Translate(0, -40);
            });

            // Remove after animation
            label.schedule.Execute(() => label.RemoveFromHierarchy()).ExecuteLater(650);
        }

        // ── Slide In ──
        // Element slides in from offset
        public static void SlideIn(VisualElement target, float fromX = 30f, int durationMs = 200)
        {
            if (target == null) return;
            target.style.opacity = 0f;
            target.style.translate = new Translate(fromX, 0);

            target.schedule.Execute(() =>
            {
                target.style.opacity = 1f;
                target.style.translate = new Translate(0, 0);
            });
        }

        // ── Pulse Class Toggle ──
        // Adds a class, then removes it after duration (for CSS-driven animations)
        public static void PulseClass(VisualElement target, string className, int durationMs = 400)
        {
            if (target == null) return;
            target.AddToClassList(className);
            target.schedule.Execute(() => target.RemoveFromClassList(className)).ExecuteLater(durationMs);
        }

        // ── Vignette Flash ──
        // Shows a colored edge overlay briefly (for damage/success feedback)
        public static void VignetteFlash(VisualElement parent, Color color, int durationMs = 350)
        {
            if (parent == null) return;

            var vignette = new VisualElement();
            vignette.name = "vignette-flash";
            vignette.pickingMode = PickingMode.Ignore;
            vignette.style.position = Position.Absolute;
            vignette.style.left = 0;
            vignette.style.top = 0;
            vignette.style.right = 0;
            vignette.style.bottom = 0;

            // Inset border glow to simulate vignette
            vignette.style.borderTopWidth = 4;
            vignette.style.borderBottomWidth = 4;
            vignette.style.borderLeftWidth = 4;
            vignette.style.borderRightWidth = 4;
            vignette.style.borderTopColor = color;
            vignette.style.borderBottomColor = color;
            vignette.style.borderLeftColor = color;
            vignette.style.borderRightColor = color;
            vignette.style.opacity = 0.8f;

            vignette.style.transitionProperty = new List<StylePropertyName> { new("opacity") };
            vignette.style.transitionDuration = new List<TimeValue> { new(durationMs, TimeUnit.Millisecond) };
            vignette.style.transitionTimingFunction = new List<EasingFunction> { new(EasingMode.EaseOut) };

            parent.Add(vignette);

            vignette.schedule.Execute(() => vignette.style.opacity = 0f);
            vignette.schedule.Execute(() => vignette.RemoveFromHierarchy()).ExecuteLater(durationMs + 50);
        }
    }
}
