using UnityEngine;
using UnityEngine.InputSystem;

namespace LostKingdom
{
    // Polling the installed Input System keeps this small demo independent of action assets.
    public static class DemoInput
    {
        public static Vector2 Move
        {
            get
            {
                if (Keyboard.current == null) return Vector2.zero;
                var keyboard = Keyboard.current;
                int right = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) ? 1 : 0;
                int left = (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) ? 1 : 0;
                int forward = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) ? 1 : 0;
                int backward = (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) ? 1 : 0;
                return Vector2.ClampMagnitude(new Vector2(right - left, forward - backward), 1);
            }
        }
        public static Vector2 Look => Mouse.current == null ? Vector2.zero : Mouse.current.delta.ReadValue();
        public static bool Jump => Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        public static bool Sprint => Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
        public static bool Interact(Key key)
        {
            if (Keyboard.current == null) return false;
            bool requestedKey = Keyboard.current[key].wasPressedThisFrame;
            // E is a common adventure-game interaction key; preserve Enter for existing UI guidance/tests.
            return requestedKey || (key == Key.Enter && Keyboard.current.eKey.wasPressedThisFrame);
        }
        public static bool Pause => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }
}
