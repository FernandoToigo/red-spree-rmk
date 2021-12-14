public static class GameInput
{
    public static void Update(ref Game.Input gameInput, HardInput hardInput)
    {
        if (hardInput.IsLeftMouseButtonDown)
        {
            gameInput.FireStraight = true;
        }

        if (hardInput.IsRightMouseButtonDown)
        {
            gameInput.FireDiagonally = true;
        }
    }
}