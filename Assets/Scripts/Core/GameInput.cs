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
            var es = EventSystem.current;
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return es == null || !es.IsPointerOverGameObject();
            var touch = Touchscreen.current;
            if (touch == null) return false;
            // Every finger counts, not only the first one down: a thumb resting on the screen must not block the tap.
            var touches = touch.touches;
            for (int i = 0; i < touches.Count; i++)
                if (touches[i].press.wasPressedThisFrame && (es == null || !es.IsPointerOverGameObject(touches[i].touchId.ReadValue())))
                    return true;
            return false;
        }

        static InputAction back;
        static int backFrame = -1;

        /// <summary>Android Back (Escape on a keyboard): pause in a level, close or leave everywhere else.
        /// An action, not a polled key: a gesture-navigation Back is a press and release in the same instant, and
        /// polling `wasPressedThisFrame` missed it when both landed in one frame (seen on the 30 fps map, QA 15/11).</summary>
        public static bool BackPressed()
        {
            if (back == null)
            {
                back = new InputAction("Back", InputActionType.Button, "<Keyboard>/escape");
                back.performed += _ => backFrame = Time.frameCount;
                back.Enable();
            }
            return backFrame == Time.frameCount;
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
    }
}
