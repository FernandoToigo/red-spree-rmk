using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Main : MonoBehaviour
{
    private const string MainSceneName = "Main";
    private const string GameplaySceneName = "Gameplay";
    private const string SplashSceneName = "Splash";
    private MainState _state;
    public Definitions _definitions;

    [RuntimeInitializeOnLoadMethod]
    private static void LoadMainScene()
    {
        SceneManager.LoadScene(MainSceneName, LoadSceneMode.Additive);
    }

    private void Awake()
    {
        InitializeCurrentPhase();
    }

    private void InitializeCurrentPhase()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.name == SplashSceneName)
        {
            _state.CurrentPhase = GamePhase.Splash;
            InitializeSplash();
        }
        else if (activeScene.name == GameplaySceneName)
        {
            _state.CurrentPhase = GamePhase.Gameplay;
            InitializeGameplay();
        }
    }

    private static void InitializeSplash()
    {
        Splash.Initialize();
    }

    private void InitializeGameplay()
    {
        var references = FindObjectOfType<References>();
        Game.Initialize(references, _definitions);
        UserInterface.Initialize(references);
        GameAudio.Initialize(references);
    }

    public void FixedUpdate()
    {
        var hardInput = GetHardInput();
        var time = GetFrameTime();

        RunFrame(time, hardInput);
    }

    private void RunFrame(FrameTime time, HardInput hardInput)
    {
        if (_state.CurrentPhase == GamePhase.Splash)
        {
            SplashFrame(time);
        }
        else if (_state.CurrentPhase == GamePhase.Gameplay)
        {
            GameplayFrame(time, hardInput);
        }
        else if (_state.CurrentPhase == GamePhase.Transitioning)
        {
            TransitioningFrame();
        }
    }

    private void SplashFrame(FrameTime time)
    {
        var splashReport = Splash.Update(time);

        if (splashReport.HasFinished)
        {
            _state.CurrentPhase = GamePhase.Transitioning;
            _state.TransitionOperation = SceneManager.LoadSceneAsync(GameplaySceneName, LoadSceneMode.Additive);
            _state.TransitioningToPhase = GamePhase.Gameplay;
            
            SceneManager.UnloadSceneAsync(SplashSceneName);
        }
    }

    private static void GameplayFrame(FrameTime time, HardInput hardInput)
    {
        var gameInput = new Game.Input();
        
        UserInterface.Update(ref gameInput);
        GameInput.Update(ref gameInput, hardInput);
        var gameReport = Game.Update(gameInput, time);
        UserInterface.Render(gameReport, time);
        GameAudio.Update(gameReport);
    }

    private void TransitioningFrame()
    {
        if (!_state.TransitionOperation.isDone)
        {
            return;
        }
        
        _state.CurrentPhase = _state.TransitioningToPhase;
        if (_state.CurrentPhase == GamePhase.Gameplay)
        {
            InitializeGameplay();
        }
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

    private struct MainState
    {
        public GamePhase CurrentPhase;
        public AsyncOperation TransitionOperation;
        public GamePhase TransitioningToPhase;
    }

    private enum GamePhase
    {
        Transitioning,
        Splash,
        Gameplay
    }
}
