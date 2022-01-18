using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Main : MonoBehaviour
{
    private const string MainSceneName = "Main";
    public Definitions _definitions;

    [RuntimeInitializeOnLoadMethod]
    private static void LoadMainScene()
    {
        SceneManager.LoadScene(MainSceneName, LoadSceneMode.Additive);
    }
    
    private void Awake()
    {
        var references = FindObjectOfType<References>();
        Game.Initialize(references, _definitions);
        UserInterface.Initialize(references);
        GameAudio.Initialize(references);
    }

    public void FixedUpdate()
    {
        var hardInput = GetHardInput();
        var gameInput = new Game.Input();
        var time = GetFrameTime();

        UserInterface.Update(ref gameInput);
        GameInput.Update(ref gameInput, hardInput);
        var gameReport = Game.Update(gameInput, time);
        UserInterface.Render(gameReport, time);
        GameAudio.Update(gameReport);
    }

    private static FrameTime GetFrameTime()
    {
        return new FrameTime
        {
            DeltaSeconds = Time.deltaTime,
        };
    }

    private static HardInput GetHardInput()
    {
        return new HardInput
        {
            IsLeftMouseButtonDown = Mouse.current.leftButton.wasPressedThisFrame,
            IsRightMouseButtonDown = Mouse.current.rightButton.wasPressedThisFrame,
            IsCtrlKeyDown = Keyboard.current.ctrlKey.wasPressedThisFrame,
            IsShiftKeyDown = Keyboard.current.shiftKey.wasPressedThisFrame,
            IsF1KeyDown = Keyboard.current.f1Key.wasPressedThisFrame
        };
    }
}
