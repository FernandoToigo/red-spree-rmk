using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    public static void Update(Game.Report gameReport, FrameTime time)
    {
        TryUpdateBulletCount(gameReport);
        UpdateCollectedBullets(gameReport, time);
    }

    private static void UpdateCollectedBullets(Game.Report gameReport, FrameTime time)
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

        if (_state.CollectedBullets.Count == 0)
        {
            return;
        }

        ref var bullet = ref _state.CollectedBullets.Tail();

        while (true)
        {
            bullet.Value.AnimationPercent =
                Mathf.Min(1f, bullet.Value.AnimationPercent + time.DeltaSeconds);
            var y = bullet.Value.AnimationPercent * 30f;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _references.CollectedBulletsParent,
                _references.Camera.WorldToScreenPoint(bullet.Value.Source.position),
                null,
                out var sourcePosition);

            bullet.Value.Visual.rectTransform.anchoredPosition = sourcePosition + new Vector2(0f, y);

            if (bullet.Value.AnimationPercent >= 1f)
            {
                bullet.Value.Visual.gameObject.SetActive(false);
                _state.AvailableCollectedBulletVisuals.Push(bullet.Value.Visual);
                _state.CollectedBullets.Remove(ref bullet);
            }

            if (!bullet.HasNext)
            {
                break;
            }

            bullet = ref _state.CollectedBullets.Next(ref bullet);
        }
    }

    private static void TryUpdateBulletCount(Game.Report gameReport)
    {
        if (gameReport.FiredBullet || gameReport.CollectedBullets > 0)
        {
            UpdateBulletCount();
        }
    }

    private static void UpdateBulletCount()
    {
        _references.BulletsText.text = $"{Game.State.AvailableBulletCount}x";
    }

    private struct State
    {
        public Stack<TMP_Text> AvailableCollectedBulletVisuals;
        public ArrayList<CollectedBulletState> CollectedBullets;
    }

    private struct CollectedBulletState
    {
        public float AnimationPercent;
        public TMP_Text Visual;
        public Transform Source;
    }

    private struct BulletState
    {
        public bool IsBeingFired;
        public bool IsVisible;
        public bool IsBeingCollected;
        public float CollectingAnimationPercent;
        public Vector2 FiredStartPosition;
        public float FiredAnimationPercent;
        public Image Visual;
    }
}