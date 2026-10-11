using UnityEngine.InputSystem;

// Edge-triggered menu navigation from keyboard or gamepad.
public static class MenuInput
{
    static Keyboard Kb => Keyboard.current;
    static Gamepad Pad => Gamepad.current;

    public static bool Up =>
        (Kb != null && (Kb.upArrowKey.wasPressedThisFrame || Kb.wKey.wasPressedThisFrame)) ||
        (Pad != null && (Pad.dpad.up.wasPressedThisFrame || Pad.leftStick.up.wasPressedThisFrame));

    public static bool Down =>
        (Kb != null && (Kb.downArrowKey.wasPressedThisFrame || Kb.sKey.wasPressedThisFrame)) ||
        (Pad != null && (Pad.dpad.down.wasPressedThisFrame || Pad.leftStick.down.wasPressedThisFrame));

    public static bool Left =>
        (Kb != null && (Kb.leftArrowKey.wasPressedThisFrame || Kb.aKey.wasPressedThisFrame)) ||
        (Pad != null && (Pad.dpad.left.wasPressedThisFrame || Pad.leftStick.left.wasPressedThisFrame));

    public static bool Right =>
        (Kb != null && (Kb.rightArrowKey.wasPressedThisFrame || Kb.dKey.wasPressedThisFrame)) ||
        (Pad != null && (Pad.dpad.right.wasPressedThisFrame || Pad.leftStick.right.wasPressedThisFrame));

    public static bool Confirm =>
        (Kb != null && (Kb.enterKey.wasPressedThisFrame || Kb.numpadEnterKey.wasPressedThisFrame ||
                        Kb.spaceKey.wasPressedThisFrame || Kb.jKey.wasPressedThisFrame)) ||
        (Pad != null && Pad.buttonSouth.wasPressedThisFrame);

    public static bool Back =>
        (Kb != null && (Kb.escapeKey.wasPressedThisFrame || Kb.backspaceKey.wasPressedThisFrame)) ||
        (Pad != null && Pad.buttonEast.wasPressedThisFrame);

    public static bool Pause =>
        (Kb != null && Kb.escapeKey.wasPressedThisFrame) ||
        (Pad != null && Pad.startButton.wasPressedThisFrame);
}
