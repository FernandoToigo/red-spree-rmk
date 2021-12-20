using UnityEngine;
using UnityEngine.UI;

public static class UserInterface
{
    private const float BulletSeparationDistance = 7f;
    private static References _references;
    private static State _state;

    public static void Initialize(References references)
    {
        _references = references;
        InitializeBullets();
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
        ref var bullet = ref _state.Bullets.Tail();
        var hasPendingBulletFire = gameReport.BulletFired;
        var pendingCollectedBullets = gameReport.CollectedBullets;

        _references.BulletsText.text = $"{Game.State.AvailableBulletCount}x";
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
                bullet.Value.Visual.rectTransform.anchoredPosition =
                    new Vector2(bullet.Value.Visual.rectTransform.anchoredPosition.x, -15f);
                bullet.Value.IsVisible = true;
                bullet.Value.Visual.color = new Color(1f, 1f, 1f, 1f);
                bullet.Value.Visual.rectTransform.anchoredPosition = new Vector2(0f, 0f);
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
        bullet.Value.CollectingAnimationPercent = Mathf.Min(1f, bullet.Value.CollectingAnimationPercent + time.DeltaSeconds * 4f);
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