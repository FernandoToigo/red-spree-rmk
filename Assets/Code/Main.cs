using UnityEngine;
using UnityEngine.InputSystem;

public class Main : MonoBehaviour
{
    private void Awake()
    {
        var references = FindObjectOfType<References>();
        Game.Initialize(references);
        UserInterface.Initialize(references);
    }

    public void FixedUpdate()
    {
        var hardInput = GetHardInput();
        var gameInput = new Game.Input();
        var time = new FrameTime
        {
            DeltaSeconds = Time.deltaTime,
            TotalSeconds = Time.timeSinceLevelLoad
        };
        
        GameInput.Update(ref gameInput, hardInput);
        var gameReport = Game.Update(gameInput, time);
        UserInterface.Update(gameReport, time);
    }

    private static HardInput GetHardInput()
    {
        return new HardInput
        {
            IsLeftMouseButtonDown = Mouse.current.leftButton.wasPressedThisFrame,
            IsRightMouseButtonDown = Mouse.current.rightButton.wasPressedThisFrame,
            IsCtrlKeyDown = Keyboard.current.ctrlKey.wasPressedThisFrame,
            IsShiftKeyDown = Keyboard.current.shiftKey.wasPressedThisFrame
        };
    }
}
