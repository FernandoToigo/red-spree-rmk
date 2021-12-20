using System.Collections.Generic;
using UnityEngine;

public static class Game
{
    private static readonly int ZombieRunAnimationTrigger = Animator.StringToHash("Run");
    private static readonly int ZombieDieAnimationTrigger = Animator.StringToHash("Die");
    private static readonly int PlayerDieAnimationTrigger = Animator.StringToHash("Died");
    private const float ZombieVelocity = 50f;
    private const float PlayerVelocity = 50f;
    private static References _references;
    public static GameState State;

    public static void Initialize(References references)
    {
        _references = references;
        State.ActiveBullets = new ArrayList<BulletComponent>(references.Bullets.Length);
        State.InactiveBullets = new Stack<BulletComponent>(references.Bullets.Length);
        State.ActiveZombies = new ArrayList<EnemyComponent>(references.Zombies.Length);
        State.InactiveZombies = new Stack<EnemyComponent>(references.Zombies.Length);
        var width = _references.PixelPerfectCamera.refResolutionX + 30;
        var height = _references.PixelPerfectCamera.refResolutionY + 30;
        State.VisibilityBounds = new Rect(
            _references.PixelPerfectCamera.transform.position.x - width * 0.5f,
            _references.PixelPerfectCamera.transform.position.y - height * 0.5f,
            width,
            height);
        State.AvailableBulletCount = 3;
        State.PlayerVelocity = PlayerVelocity;

        InitializeBullets();
        InitializeZombies();
    }

    private static void InitializeBullets()
    {
        foreach (var bullet in _references.Bullets)
        {
            bullet.State.CollidedEnemies = new ReusableArray<EnemyComponent>(10);
            bullet.RigidBody.simulated = false;
            State.InactiveBullets.Push(bullet);
        }
    }

    private static void InitializeZombies()
    {
        foreach (var zombie in _references.Zombies)
        {
            zombie.RigidBody.simulated = false;
            State.InactiveZombies.Push(zombie);
        }
    }

    public static Report Update(Input input, FrameTime time)
    {
        var report = new Report();

        TryFireStraight(input, ref report);
        TryCollideWithEnemies(ref report);
        TrySpawnZombies(time);
        UpdateBullets();
        UpdateZombies();
        return report;
    }

    private static void TryCollideWithEnemies(ref Report report)
    {
        if (!State.IsDead)
        {
            for (var i = 0; i < _references.Player.CollidedEnemies.UsableLength; i++)
            {
                if (_references.Player.CollidedEnemies.Data[i].State.IsDead)
                {
                    var bullets = Random.value <= 0.2f ? 2 : 1;
                    report.CollectedBulletsSource = _references.Player.CollidedEnemies.Data[i].transform;
                    report.CollectedBullets += bullets;
                    State.AvailableBulletCount += bullets;
                }
                else
                {
                    State.IsDead = true;
                    State.PlayerVelocity = 0f;
                    _references.Player.Animator.SetTrigger(PlayerDieAnimationTrigger);
                }
            }
        }

        _references.Player.CollidedEnemies.Clear();
    }

    private static void TrySpawnZombies(FrameTime time)
    {
        const float zombieTickSeconds = 0.25f;
        State.ZombieTickCooldown += time.DeltaSeconds;

        if (State.ZombieTickCooldown < zombieTickSeconds)
        {
            return;
        }

        State.ZombieTickCooldown -= zombieTickSeconds;
        var x = time.TotalSeconds;
        const float p = 15f;
        const float maxZombies = 3f;
        // https://www.desmos.com/calculator/1o7gniviux
        var waveFactor = ((Mathf.Sin((x - p / 4f) * Mathf.PI * 2f / p) + 1f) / 2f) * Mathf.Pow(x / p, 1.5f);
        var percentSpawned = Mathf.Clamp01(State.ActiveZombies.Count / (waveFactor * maxZombies));

        if (Random.value < (1f - percentSpawned))
        {
            ActivateZombie();
        }
    }

    private static void UpdateBullets()
    {
        if (State.ActiveBullets.Count == 0)
        {
            return;
        }

        ref var bullet = ref State.ActiveBullets.Tail();

        while (true)
        {
            var shouldDeactivate = false;
            for (var i = 0; i < bullet.Value.State.CollidedEnemies.UsableLength; i++)
            {
                var enemy = bullet.Value.State.CollidedEnemies.Data[i];
                if (enemy.State.IsDead)
                {
                    continue;
                }

                enemy.State.IsDead = true;
                bullet.Value.State.RemainingHits--;

                if (bullet.Value.State.RemainingHits <= 0)
                {
                    KillEnemy(enemy);
                    shouldDeactivate = true;
                    break;
                }
            }

            if (!State.VisibilityBounds.Contains(bullet.Value.transform.position))
            {
                shouldDeactivate = true;
            }

            if (shouldDeactivate)
            {
                DeactivateBullet(ref bullet);
            }

            bullet.Value.State.CollidedEnemies.Clear();

            if (!bullet.HasNext)
            {
                break;
            }

            bullet = ref State.ActiveBullets.Next(ref bullet);
        }
    }

    private static void UpdateZombies()
    {
        if (State.ActiveZombies.Count == 0)
        {
            return;
        }

        ref var zombie = ref State.ActiveZombies.Tail();

        while (true)
        {
            if (!State.VisibilityBounds.Contains(zombie.Value.transform.position))
            {
                DeactivateZombie(ref zombie);
            }

            zombie.Value.RigidBody.velocity = GetZombieVelocity(zombie.Value);

            if (!zombie.HasNext)
            {
                break;
            }

            zombie = ref State.ActiveZombies.Next(ref zombie);
        }
    }

    private static void TryFireStraight(Input input, ref Report report)
    {
        if (State.IsDead)
        {
            return;
        }
        
        if (!input.FireStraight)
        {
            return;
        }

        if (State.AvailableBulletCount <= 0)
        {
            return;
        }

        State.AvailableBulletCount--;
        report.BulletFired = true;
        FireBullet(Vector2.right);
    }

    private static void FireBullet(Vector2 direction)
    {
        const float bulletVelocity = 500f;
        var bullet = State.InactiveBullets.Pop();
        bullet.State.RemainingHits = 1;
        bullet.RigidBody.simulated = true;
        bullet.RigidBody.velocity = direction * bulletVelocity;
        bullet.transform.position = _references.GunNozzle.position;
        State.ActiveBullets.Add(bullet);
    }

    private static void DeactivateBullet(ref ArrayListNode<BulletComponent> bullet)
    {
        State.ActiveBullets.Remove(ref bullet);
        State.InactiveBullets.Push(bullet.Value);
        bullet.Value.RigidBody.simulated = false;
        bullet.Value.transform.position = new Vector3(-1000f, 0f, 0f);
    }

    private static void ActivateZombie()
    {
        var zombie = State.InactiveZombies.Pop();
        zombie.State.IsDead = false;
        zombie.State.SpeedFactor = Random.Range(0.8f, 1.5f);
        zombie.State.Index = State.ActiveZombies.Add(zombie);
        zombie.RigidBody.simulated = true;
        zombie.RigidBody.velocity = GetZombieVelocity(zombie);
        zombie.transform.position = _references.ZombieSpawn.position;
        zombie.Animator.SetTrigger(ZombieRunAnimationTrigger);
    }

    private static Vector2 GetZombieVelocity(EnemyComponent zombie)
    {
        if (zombie.State.IsDead)
        {
            return new Vector2(-State.PlayerVelocity, 0f);
        }
        
        var velocity = -(ZombieVelocity + State.PlayerVelocity) * zombie.State.SpeedFactor;
        return new Vector2(velocity, 0f);
    }

    private static void KillEnemy(EnemyComponent enemy)
    {
        enemy.Animator.ResetTrigger(ZombieRunAnimationTrigger);
        enemy.Animator.SetTrigger(ZombieDieAnimationTrigger);
        enemy.State.IsDead = true;
        enemy.RigidBody.velocity = new Vector2(-State.PlayerVelocity, 0f);
    }

    private static void DeactivateZombie(int index)
    {
        DeactivateZombie(ref State.ActiveZombies.GetAt(index));
    }

    private static void DeactivateZombie(ref ArrayListNode<EnemyComponent> zombie)
    {
        State.ActiveZombies.Remove(ref zombie);
        State.InactiveZombies.Push(zombie.Value);
        zombie.Value.RigidBody.simulated = false;
        zombie.Value.transform.position = new Vector3(-1000f, 0f, 0f);
    }

    public struct Report
    {
        public bool BulletFired;
        public int CollectedBullets;
        public Transform CollectedBulletsSource;
    }

    public struct Input
    {
        public bool FireStraight;
        public bool FireDiagonally;
    }
}

public struct GameState
{
    public float PlayerVelocity;
    public bool IsDead;
    public Rect VisibilityBounds;
    public float ZombieTickCooldown;
    public int AvailableBulletCount;
    public ArrayList<BulletComponent> ActiveBullets;
    public Stack<BulletComponent> InactiveBullets;
    public ArrayList<EnemyComponent> ActiveZombies;
    public Stack<EnemyComponent> InactiveZombies;
}