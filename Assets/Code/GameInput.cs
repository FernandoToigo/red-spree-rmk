using System.Diagnostics;

public static class GameInput
{
    public static void Update(ref Game.Input gameInput, HardInput hardInput)
    {
        TryFire(ref gameInput, hardInput);
        TryToggleAutoFire(ref gameInput, hardInput);
    }

    private static void TryFire(ref Game.Input gameInput, HardInput hardInput)
    {
        if (hardInput.IsLeftMouseButtonDown || hardInput.IsCtrlKeyDown)
        {
            gameInput.FireStraight = true;
        }

        if (hardInput.IsRightMouseButtonDown || hardInput.IsShiftKeyDown)
        {
            gameInput.FireDiagonally = true;
        }
    }

    [Conditional("UNITY_EDITOR")]
    private static void TryToggleAutoFire(ref Game.Input gameInput, HardInput hardInput)
    {
        if (hardInput.IsF1KeyDown)
        {
            gameInput.ToggleAutoFire = true;
        }
    }
}