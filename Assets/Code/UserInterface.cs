using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class UserInterface
{
    private static References _references;
    private static State _state;

    public static void Initialize(References references)
    {
        _references = references;
        InitializeCollectedBullets();
        UpdateBulletCount();
    }

    private static void InitializeCollectedBullets()
    {
        _state.CollectedBullets = new ArrayList<CollectedBulletState>(_references.CollectedBulletVisuals.Length);
        _state.AvailableCollectedBulletVisuals = new Stack<TMP_Text>(_references.CollectedBulletVisuals);
    }

    public static void Update(ref Game.Input gameInput)
    {
        UpdateTimeFactor(ref gameInput);
    }

    private static void UpdateTimeFactor(ref Game.Input gameInput)
    {
        if (_state.UpgradeAnimationPercent >= 0.7f)
        {
            gameInput.ChangedTimeFactor = Mathf.Lerp(1f, 0.2f, (1f - _state.UpgradeAnimationPercent) / 0.3f);
        }
        else if (_state.UpgradeAnimationPercent < 0.3f)
        {
            gameInput.ChangedTimeFactor = Mathf.Lerp(0.2f, 1f, (0.3f - _state.UpgradeAnimationPercent) / 0.3f);
        }
    }

    public static void Render(Game.Report gameReport, FrameTime time)
    {
        TryUpdateBulletCount(gameReport);
        UpdateKillCount();
        ShowCollectedBullets(gameReport);
        UpdateCollectedBullets(time);
        TryUpdateUpgradeAnimation(gameReport, time);
    }

    private static void TryUpdateUpgradeAnimation(Game.Report gameReport, FrameTime time)
    {
        if (gameReport.BulletPenetrationUpgraded)
        {
            _state.UpgradeAnimationPercent = 1f;
        }
        else
        {
            const float percentAnimationPerSecond = 1f / 5f;
            _state.UpgradeAnimationPercent =
                Mathf.Max(0, _state.UpgradeAnimationPercent - time.DeltaSeconds * percentAnimationPerSecond);
        }

        if (_state.UpgradeAnimationPercent >= 0.7f)
        {
            var stagePercent = (1f - _state.UpgradeAnimationPercent) / 0.3f;
            var y = Mathf.Lerp(-5f, 0f, stagePercent);
            _references.UpgradeText.rectTransform.anchoredPosition = new Vector2(0f, y);
            _references.UpgradeText.alpha = Mathf.Lerp(0f, 1f, stagePercent);
        }
        else if (_state.UpgradeAnimationPercent < 0.3f)
        {
            var stagePercent = (0.3f - _state.UpgradeAnimationPercent) / 0.3f;
            var y = Mathf.Lerp(0f, 5f, stagePercent);
            _references.UpgradeText.rectTransform.anchoredPosition = new Vector2(0f, y);
            _references.UpgradeText.alpha = Mathf.Lerp(1f, 0f, stagePercent);
        }
    }

    private static void UpdateCollectedBullets(FrameTime time)
    {
        var collectedBulletIterator = _state.CollectedBullets.Iterate();
        while (collectedBulletIterator.Next())
        {
            ref var collectedBulletNode = ref collectedBulletIterator.Current();

            collectedBulletNode.Value.AnimationPercent =
                Mathf.Min(1f, collectedBulletNode.Value.AnimationPercent + time.DeltaSeconds);

            var position = GetCollectedBulletPosition(collectedBulletNode.Value.Source);

            const float collectedAnimationDeltaY = 120f;
            var y = collectedBulletNode.Value.AnimationPercent * collectedAnimationDeltaY;
            collectedBulletNode.Value.Visual.rectTransform.anchoredPosition = position + new Vector2(0f, y);

            if (collectedBulletNode.Value.AnimationPercent >= 1f)
            {
                collectedBulletNode.Value.Visual.gameObject.SetActive(false);
                _state.AvailableCollectedBulletVisuals.Push(collectedBulletNode.Value.Visual);
                _state.CollectedBullets.Remove(ref collectedBulletNode);
            }
        }
    }

    private static Vector2 GetCollectedBulletPosition(Transform source)
    {
        var positionOnScreen = _references.Camera.WorldToScreenPoint(source.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _references.CollectedBulletsParent,
            positionOnScreen,
            _references.Camera,
            out var position);

        return position;
    }

    private static void ShowCollectedBullets(Game.Report gameReport)
    {
        if (gameReport.CollectedBullets <= 0)
        {
            return;
        }
        
        var visual = _state.AvailableCollectedBulletVisuals.Pop();
        visual.text = $"+{gameReport.CollectedBullets}";
        visual.gameObject.SetActive(true);
        _state.CollectedBullets.Add(new CollectedBulletState
        {
            Visual = visual,
            AnimationPercent = 0f,
            Source = gameReport.CollectedBulletsSource
        });
    }

    private static void TryUpdateBulletCount(Game.Report gameReport)
    {
        if (gameReport.FiredBullet || gameReport.CollectedBullets > 0)
        {
            UpdateBulletCount();
        }
    }

    private static void UpdateKillCount()
    {
        _references.KillCountText.text = Game.State.EnemiesKilled.ToString();
    }

    private static void UpdateBulletCount()
    {
        _references.BulletsText.text = $"{Game.State.AvailableBulletCount}x";
    }

    private struct State
    {
        public Stack<TMP_Text> AvailableCollectedBulletVisuals;
        public ArrayList<CollectedBulletState> CollectedBullets;
        public float UpgradeAnimationPercent;
    }

    private struct CollectedBulletState
    {
        public float AnimationPercent;
        public TMP_Text Visual;
        public Transform Source;
    }
}