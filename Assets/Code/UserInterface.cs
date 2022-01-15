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
        StartShowingCollectedBullets(gameReport);
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
        if (_state.CollectedBullets.Count == 0)
        {
            return;
        }

        ref var collectedBulletNode = ref _state.CollectedBullets.Tail();

        while (true)
        {
            collectedBulletNode.Value.AnimationPercent =
                Mathf.Min(1f, collectedBulletNode.Value.AnimationPercent + time.DeltaSeconds);
            var y = collectedBulletNode.Value.AnimationPercent * 30f;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _references.CollectedBulletsParent,
                _references.Camera.WorldToScreenPoint(collectedBulletNode.Value.Source.position),
                null,
                out var sourcePosition);

            collectedBulletNode.Value.Visual.rectTransform.anchoredPosition = sourcePosition + new Vector2(0f, y);

            if (collectedBulletNode.Value.AnimationPercent >= 1f)
            {
                collectedBulletNode.Value.Visual.gameObject.SetActive(false);
                _state.AvailableCollectedBulletVisuals.Push(collectedBulletNode.Value.Visual);
                _state.CollectedBullets.Remove(ref collectedBulletNode);
            }

            if (!collectedBulletNode.HasNext)
            {
                break;
            }

            collectedBulletNode = ref _state.CollectedBullets.Next(ref collectedBulletNode);
        }
    }

    private static void StartShowingCollectedBullets(Game.Report gameReport)
    {
        if (gameReport.CollectedBullets > 0)
        {
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