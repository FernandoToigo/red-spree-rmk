using UnityEngine;

public static class Splash
{
    private static State _state;

    public static void Initialize()
    {
        _state.RemainingSeconds = 3f;
    }
    
    public static Report Update(FrameTime time)
    {
        var report = new Report();

        UpdateRemainingTime(time, ref report);

        return report;
    }

    private static void UpdateRemainingTime(FrameTime time, ref Report report)
    {
        if (_state.RemainingSeconds <= 0f)
        {
            return;
        }
        
        _state.RemainingSeconds = Mathf.Max(0f, _state.RemainingSeconds - time.DeltaSeconds);
        if (_state.RemainingSeconds <= 0f)
        {
            report.HasFinished = true;
        }
    }

    public struct Report
    {
        public bool HasFinished;
    }

    private struct State
    {
        public float RemainingSeconds;
    }
}