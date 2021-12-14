using UnityEngine;
using UnityEngine.InputSystem;

public class Main : MonoBehaviour
{
    private void Awake()
    {
        var references = FindObjectOfType<References>();
        Game.Initialize(references);
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
        Game.Update(gameInput, time);
    }

    private static HardInput GetHardInput()
    {
        return new HardInput
        {
            IsLeftMouseButtonDown = Mouse.current.leftButton.wasPressedThisFrame,
            IsRightMouseButtonDown = Mouse.current.rightButton.wasPressedThisFrame
        };
    }
}
