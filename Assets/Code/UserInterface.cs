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
        _state.CollectedBullets = new ArrayLinkedList<CollectedBulletState>(_references.CollectedBulletVisuals.Length);
        _state.AvailableCollectedBulletVisuals = new Stack<TMP_Text>(_references.CollectedBulletVisuals);
    }

    public static void Update(ref Game.Input gameInput)
    {
        UpdateTimeFactor(ref gameInput);
        TryStartGame(ref gameInput);
    }

    private static void TryStartGame(ref Game.Input gameInput)
    {
        if (_references.StartButton.IsUp)
        {
            gameInput.StartGame = true;
            _references.StartCanvas.alpha = 0f;
            _references.StartCanvas.blocksRaycasts = false;
        }
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
        TryShowRestartScreen(gameReport);
    }

    private static void TryShowRestartScreen(Game.Report gameReport)
    {
        if (!gameReport.Died)
        {
            return;
        }
        
        _references.StartCanvas.alpha = 1f;
        _references.StartCanvas.blocksRaycasts = true;
    }

    private static void TryUpdateUpgradeAnimation(Game.Report gameReport, FrameTime time)
    {
        if (gameReport.BulletPenetrationUpgraded)
        {
            _state.UpgradeAnimationPercent = 1f;
        }
        else
        {
            const float animationPercentPerSecond = 1f / 5f;
            _state.UpgradeAnimationPercent =
                Mathf.Max(0, _state.UpgradeAnimationPercent - time.DeltaSeconds * animationPercentPerSecond);
        }

        const float textAnimationDeltaY = 30f;
        const float textAnimationPercentCut = 0.15f;

        if (_state.UpgradeAnimationPercent >= 1f - textAnimationPercentCut)
        {
            var stagePercent = (1f - _state.UpgradeAnimationPercent) / textAnimationPercentCut;
            var y = Mathf.Lerp(-textAnimationDeltaY, 0f, stagePercent);
            _references.UpgradeText.rectTransform.anchoredPosition = new Vector2(0f, y);
            _references.UpgradeText.alpha = Mathf.Lerp(0f, 1f, stagePercent);
        }
        else if (_state.UpgradeAnimationPercent < textAnimationPercentCut)
        {
            var stagePercent = (0.15f - _state.UpgradeAnimationPercent) / textAnimationPercentCut;
            var y = Mathf.Lerp(0f, textAnimationDeltaY, stagePercent);
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
        if (gameReport.GameStarted || gameReport.FiredBullet || gameReport.CollectedBullets > 0)
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
        public ArrayLinkedList<CollectedBulletState> CollectedBullets;
        public float UpgradeAnimationPercent;
    }

    private struct CollectedBulletState
    {
        public float AnimationPercent;
        public TMP_Text Visual;
        public Transform Source;
    }
}