using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PufferLaunch.Core
{
    /// <summary>One input surface for keyboard, mouse and touch (Input System).</summary>
    public static class GameInput
    {
        const float SwipeMinPixels = 60f;
        static Vector2 touchStart;
        static int swipeFrame = -1;
        static int swipeDir;

        /// <summary>False while paused or showing results, so gameplay ignores taps meant for menus.</summary>
        public static bool GameplayEnabled = true;

        /// <summary>Space, left click or a new touch — ignoring presses on UI buttons.</summary>
        public static bool ActionPressed()
        {
            if (!GameplayEnabled) return false;
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;
            if (PointerOverUi()) return false;
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
            var touch = Touchscreen.current;
            return touch != null && touch.primaryTouch.press.wasPressedThisFrame;
        }

        public static bool PausePressed()
        {
            var kb = Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
        }

        /// <summary>-1 left, +1 right, 0 none: arrow keys or a horizontal swipe.</summary>
        public static int SteerPressed()
        {
            if (!GameplayEnabled) return 0;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) return -1;
                if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) return 1;
            }
            return Swipe();
        }

        static int Swipe()
        {
            if (swipeFrame == Time.frameCount) return swipeDir;
            swipeFrame = Time.frameCount;
            swipeDir = 0;
            var touch = Touchscreen.current;
            if (touch == null) return 0;
            var primary = touch.primaryTouch;
            if (primary.press.wasPressedThisFrame) touchStart = primary.position.ReadValue();
            if (primary.press.wasReleasedThisFrame)
            {
                float dx = primary.position.ReadValue().x - touchStart.x;
                if (Mathf.Abs(dx) > SwipeMinPixels) swipeDir = dx > 0 ? 1 : -1;
            }
            return swipeDir;
        }

        static bool PointerOverUi()
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
                return es.IsPointerOverGameObject(touch.primaryTouch.touchId.ReadValue());
            return es.IsPointerOverGameObject();
        }
    }
}
