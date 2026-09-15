using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DeadDawn.Crafting;

namespace DeadDawn.Core
{
    /// <summary>
    /// Centralized authority and registry preventing combat shooting when hovering over,
    /// clicking, or debouncing from interactive UI elements, world prompts, and modal menus.
    /// </summary>
    public static class UIInteractionBlocker
    {
        private struct RegisteredRect
        {
            public Rect rect;
            public int frameCount;
            public float timestamp;
        }

        private static readonly List<RegisteredRect> registeredRects = new List<RegisteredRect>();
        private static float blockUntilTime = 0f;
        private const float DEFAULT_DEBOUNCE = 0.35f;

        /// <summary>
        /// Locks out shooting for a duration after an interaction (e.g. clicking a button or closing a menu).
        /// </summary>
        public static void NotifyInteractionConsumed(float extraDebounceSeconds = DEFAULT_DEBOUNCE)
        {
            blockUntilTime = Mathf.Max(blockUntilTime, Time.unscaledTime + extraDebounceSeconds);
        }

        /// <summary>
        /// Registers an interactive IMGUI screen rect (world prompt, button) so that
        /// hovering or clicking over it blocks weapon fire.
        /// </summary>
        public static void RegisterInteractiveRect(Rect rect)
        {
            int currentFrame = Time.frameCount;
            float currentTime = Time.unscaledTime;

            for (int i = 0; i < registeredRects.Count; i++)
            {
                if (Mathf.Abs(registeredRects[i].rect.x - rect.x) < 2f &&
                    Mathf.Abs(registeredRects[i].rect.y - rect.y) < 2f &&
                    Mathf.Abs(registeredRects[i].rect.width - rect.width) < 2f &&
                    Mathf.Abs(registeredRects[i].rect.height - rect.height) < 2f)
                {
                    registeredRects[i] = new RegisteredRect
                    {
                        rect = rect,
                        frameCount = currentFrame,
                        timestamp = currentTime
                    };
                    return;
                }
            }

            registeredRects.Add(new RegisteredRect
            {
                rect = rect,
                frameCount = currentFrame,
                timestamp = currentTime
            });
        }

        private static void PruneExpiredRects()
        {
            int currentFrame = Time.frameCount;
            float currentTime = Time.unscaledTime;

            for (int i = registeredRects.Count - 1; i >= 0; i--)
            {
                if (currentFrame - registeredRects[i].frameCount > 4 || currentTime - registeredRects[i].timestamp > 0.15f)
                {
                    registeredRects.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Returns true if the pointer is currently over any UI, world prompt, open modal,
        /// or during an interaction debounce recovery period.
        /// </summary>
        public static bool IsPointerOverUI()
        {
            // 1. Paused, Menus Open, Game Over
            if (Time.timeScale <= 0f) return true;
            if (CraftingBench.IsCraftingMenuOpen) return true;
            if (ResourceStash.IsStashMenuOpen) return true;
            if (DeadDawn.UI.DeathScreenUI.IsGameOver) return true;

            // 2. Debounce Window After Menu Closing / Button Pressing
            if (Time.unscaledTime < blockUntilTime) return true;
            if (CraftingBench.WasRecentlyClosed) return true;
            if (ResourceStash.WasRecentlyClosed) return true;

            // 3. Unity UGUI Canvas Elements (EventSystem)
            if (EventSystem.current != null)
            {
                if (EventSystem.current.IsPointerOverGameObject()) return true;

                var ts = Touchscreen.current;
                if (ts != null)
                {
                    for (int i = 0; i < ts.touches.Count; i++)
                    {
                        if (ts.touches[i].press.isPressed)
                        {
                            if (EventSystem.current.IsPointerOverGameObject(ts.touches[i].touchId.ReadValue()))
                                return true;
                        }
                    }
                }
            }

            // 4. Convert Screen Point to IMGUI Screen Space (Y flipped)
            Vector2 mouseScreen = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            Vector2 mouseGUI = new Vector2(mouseScreen.x, Screen.height - mouseScreen.y);

            // 5. Check Active Registered Rects
            PruneExpiredRects();
            for (int i = 0; i < registeredRects.Count; i++)
            {
                if (registeredRects[i].rect.Contains(mouseGUI))
                {
                    return true;
                }
            }

            // 6. Direct Geometric Check on Interactive Stations
            if (BaseGate.IsAnyGatePromptHovered(mouseGUI)) return true;
            if (BaseBuildingSection.IsAnySectionPromptHovered(mouseGUI)) return true;
            if (CraftingBench.IsBenchPromptHovered(mouseGUI)) return true;
            if (ResourceStash.IsStashPromptHovered(mouseGUI)) return true;
            if (BaseManager.IsUpgradePromptHovered(mouseGUI)) return true;

            return false;
        }
    }
}
