public static class GameInput
{
    public static void Update(ref Game.Input gameInput, HardInput hardInput)
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
}