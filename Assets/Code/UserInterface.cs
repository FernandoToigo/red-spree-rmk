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
        InitializeBullets();
    }

    private static void InitializeCollectedBullets()
    {
        _state.CollectedBullets = new ArrayList<CollectedBulletState>(_references.CollectedBulletVisuals.Length);
        _state.AvailableCollectedBulletVisuals = new Stack<TMP_Text>(_references.CollectedBulletVisuals);
    }

    private static void InitializeBullets()
    {
        _state.Bullets = new ArrayList<BulletState>(_references.UiBullets.Length);
        for (var i = 0; i < Game.State.AvailableBulletCount; i++)
        {
            _references.UiBullets[i].enabled = true;
            _references.UiBullets[i].rectTransform.anchoredPosition = new Vector2(0f, 0f);
            _state.Bullets.Add(new BulletState
            {
                IsVisible = true,
                Visual = _references.UiBullets[i]
            });
        }

        for (var i = Game.State.AvailableBulletCount; i < _references.UiBullets.Length; i++)
        {
            _references.UiBullets[i].enabled = false;
            _state.Bullets.Add(new BulletState
            {
                Visual = _references.UiBullets[i]
            });
        }
    }

    public static void Update(Game.Report gameReport, FrameTime time)
    {
        UpdateBulletCount(gameReport, time);
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

    private static void UpdateBulletCount(Game.Report gameReport, FrameTime time)
    {
        _references.BulletsText.text = $"{Game.State.AvailableBulletCount}x";

        var hasPendingBulletFire = gameReport.BulletFired;
        var pendingCollectedBullets = gameReport.CollectedBullets;
        ref var bullet = ref _state.Bullets.Tail();
        while (true)
        {
            if (bullet.Value.IsBeingFired)
            {
                UpdateFiringBullet(ref bullet, time);
            }
            else if (bullet.Value.IsBeingCollected)
            {
                UpdateCollectingBullet(ref bullet, time);
            }
            else if (bullet.Value.IsVisible)
            {
                if (hasPendingBulletFire)
                {
                    const float horizontalMovementTotalSeconds = 0.25f;
                    bullet.Value.IsBeingFired = true;
                    bullet.Value.FiredStartPosition = bullet.Value.Visual.rectTransform.anchoredPosition;
                    bullet.Value.FiredAnimationPercent = 0f;
                    hasPendingBulletFire = false;
                }
            }
            else if (!bullet.Value.IsVisible && pendingCollectedBullets > 0)
            {
                bullet.Value.IsBeingCollected = true;
                bullet.Value.CollectingAnimationPercent = 0f;
                bullet.Value.IsVisible = true;
                bullet.Value.Visual.rectTransform.anchoredPosition =
                    new Vector2(bullet.Value.Visual.rectTransform.anchoredPosition.x, -15f);
                bullet.Value.Visual.color = new Color(1f, 1f, 1f, 1f);
                bullet.Value.Visual.rectTransform.localRotation = Quaternion.identity;
                bullet.Value.Visual.enabled = true;
                pendingCollectedBullets--;
            }

            if (!bullet.HasNext)
            {
                break;
            }

            bullet = ref _state.Bullets.Next(ref bullet);
        }
    }

    private static void UpdateFiringBullet(ref ArrayListNode<BulletState> bullet, FrameTime time)
    {
        bullet.Value.FiredAnimationPercent = Mathf.Min(1f, bullet.Value.FiredAnimationPercent + time.DeltaSeconds * 4f);
        var y = bullet.Value.FiredAnimationPercent * 15f;
        bullet.Value.Visual.color = new Color(1f, 1f, 1f, 1f - bullet.Value.FiredAnimationPercent);
        bullet.Value.Visual.rectTransform.anchoredPosition =
            new Vector2(bullet.Value.Visual.rectTransform.anchoredPosition.x, y);

        if (bullet.Value.FiredAnimationPercent >= 1f)
        {
            bullet.Value.IsBeingFired = false;
            bullet.Value.IsVisible = false;
            bullet.Value.Visual.enabled = false;
            _state.Bullets.Remove(ref bullet);
            _state.Bullets.Add(bullet.Value);
        }
    }

    private static void UpdateCollectingBullet(ref ArrayListNode<BulletState> bullet, FrameTime time)
    {
        bullet.Value.CollectingAnimationPercent =
            Mathf.Min(1f, bullet.Value.CollectingAnimationPercent + time.DeltaSeconds * 4f);
        var y = (1f - bullet.Value.CollectingAnimationPercent) * -15f;
        bullet.Value.Visual.color = new Color(1f, 1f, 1f, bullet.Value.CollectingAnimationPercent);
        bullet.Value.Visual.rectTransform.anchoredPosition =
            new Vector2(bullet.Value.Visual.rectTransform.anchoredPosition.x, y);

        if (bullet.Value.CollectingAnimationPercent >= 1f)
        {
            bullet.Value.IsBeingCollected = false;
        }
    }

    private struct State
    {
        public ArrayList<BulletState> Bullets;
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