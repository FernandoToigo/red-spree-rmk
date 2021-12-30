using System.Collections.Generic;
using UnityEngine;

public static class Game
{
    private static readonly int ZombieRunAnimationTrigger = Animator.StringToHash("Run");
    private static readonly int ZombieDieAnimationTrigger = Animator.StringToHash("Die");
    private static readonly int PlayerDieAnimationTrigger = Animator.StringToHash("Died");
    private static readonly int PlayerShotDiagonallyAnimationTrigger = Animator.StringToHash("ShotDiagonally");
    private const float ZombieVelocity = 50f;
    private const float VultureVelocity = 100f;
    private const float PlayerVelocity = 50f;
    private static References _references;
    public static GameState State;

    public static void Initialize(References references)
    {
        State = new GameState
        {
            PlayerVelocity = PlayerVelocity
        };
        _references = references;

        InitializeBounds();
        InitializeBullets();
        InitializeZombies();
        InitializeVultures();
        InitializePhysics();
    }

    private static void InitializePhysics()
    {
        Physics.autoSimulation = false;
    }

    private static void InitializeBounds()
    {
        var cameraPosition = _references.PixelPerfectCamera.transform.position;
        var width = _references.PixelPerfectCamera.refResolutionX + 50;
        var height = _references.PixelPerfectCamera.refResolutionY + 50;
        State.VisibilityBounds = new Rect(
            cameraPosition.x - width * 0.5f,
            cameraPosition.y - height * 0.5f,
            width,
            height);
        State.VultureMinHorizontalPosition = cameraPosition.x + _references.PixelPerfectCamera.refResolutionX * -0.5f + 5;
        State.VultureMaxHorizontalPosition = cameraPosition.x + _references.PixelPerfectCamera.refResolutionX * 0.5f - 5;
        State.VultureDiveHorizontalPosition = cameraPosition.x + _references.PixelPerfectCamera.refResolutionX * 0.15f;
    }

    private static void InitializeBullets()
    {
        State.AvailableBulletCount = 3;
        State.ActiveBullets = new ArrayList<BulletComponent>(_references.Bullets.Length);
        State.InactiveBullets = new Stack<BulletComponent>(_references.Bullets.Length);

        foreach (var bullet in _references.Bullets)
        {
            bullet.State.CollidedZombies = new ReusableArray<EnemyComponent>(10);
            bullet.State.CollidedVultures = new ReusableArray<EnemyComponent>(10);
            bullet.RigidBody.detectCollisions = false;
            State.InactiveBullets.Push(bullet);
        }
    }

    private static void InitializeZombies()
    {
        State.ActiveZombies = new ArrayList<EnemyComponent>(_references.Zombies.Length);
        State.InactiveZombies = new Stack<EnemyComponent>(_references.Zombies.Length);

        foreach (var zombie in _references.Zombies)
        {
            zombie.RigidBody.detectCollisions = false;
            State.InactiveZombies.Push(zombie);
        }
    }

    private static void InitializeVultures()
    {
        State.Vultures = new ArrayList<Vulture>(_references.Vultures.Length);
        State.InactiveVultures = new Stack<EnemyComponent>(_references.Vultures.Length);

        foreach (var vulture in _references.Vultures)
        {
            vulture.RigidBody.detectCollisions = false;
            State.InactiveVultures.Push(vulture);
        }
    }

    public static Report Update(Input input, FrameTime time)
    {
        var report = new Report();

        TryFireStraight(input, ref report);
        TryFireDiagonally(input, ref report);
        TryCollideWithEnemies(ref report);
        TrySpawnZombies(time, ref report);
        TrySpawnVultures(time);
        UpdateBullets(time);
        UpdateZombies();
        UpdateVultures();
        UpdatePhysics(time);
        
        return report;
    }

    private static void UpdatePhysics(FrameTime time)
    {
        Physics.Simulate(time.DeltaSeconds);
    }

    private static void TryCollideWithEnemies(ref Report report)
    {
        if (!State.IsDead)
        {
            for (var i = 0; i < _references.Player.CollidedEnemies.UsableLength; i++)
            {
                if (_references.Player.CollidedEnemies.Data[i].State.IsDead)
                {
                    var bullets = Random.value <= 0.5f ? 2 : 1;
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

    private static void TrySpawnZombies(FrameTime time, ref Report report)
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
            report.SpawnedZombies++;
        }
    }

    private static void TrySpawnVultures(FrameTime time)
    {
        const float vultureTickSeconds = 0.25f;
        State.VultureTickCooldown += time.DeltaSeconds;

        if (State.VultureTickCooldown < vultureTickSeconds)
        {
            return;
        }

        State.VultureTickCooldown -= vultureTickSeconds;
        var x = time.TotalSeconds;
        const float p = 35f;
        const float vulturesFactor = 1f;
        // https://www.desmos.com/calculator/1o7gniviux
        var waveFactor = ((Mathf.Sin((x - p / 4f) * Mathf.PI * 2f / p) + 1f) / 2f) * Mathf.Pow(x / p, 1.5f);
        
        var desiredSpawnCount = Mathf.FloorToInt(waveFactor * vulturesFactor);
        if (desiredSpawnCount == 0)
        {
            return;
        }

        var percentSpawned = Mathf.Clamp01((float)State.Vultures.Count / desiredSpawnCount);

        if (Random.value < (1f - percentSpawned))
        {
            ActivateVulture();
        }
    }

    private static void ActivateVulture()
    {
        var vultureComponent = State.InactiveVultures.Pop();
        vultureComponent.State.IsDead = false;
        vultureComponent.State.SpeedFactor = Random.Range(0.8f, 1.5f);
        vultureComponent.State.Index = State.Vultures.Add(new Vulture
        {
            Action = VultureAction.FlyingLeft,
            LapsMade = 0,
            EnemyComponent = vultureComponent
        });
        vultureComponent.RigidBody.detectCollisions = true;
        vultureComponent.RigidBody.velocity = new Vector2(-VultureVelocity, 0f);
        vultureComponent.transform.position = Vector3.Lerp(_references.MinVultureSpawn.position,
            _references.MaxVultureSpawn.position, Random.value);
        vultureComponent.Animator.Play(VultureAnimations.FlyingLeft);
    }

    private static void UpdateBullets(FrameTime time)
    {
        if (State.ActiveBullets.Count == 0)
        {
            return;
        }

        ref var bullet = ref State.ActiveBullets.Tail();

        while (true)
        {
            var shouldDeactivate = false;
            for (var i = 0; i < bullet.Value.State.CollidedZombies.UsableLength; i++)
            {
                var enemy = bullet.Value.State.CollidedZombies.Data[i];
                if (enemy.State.IsDead)
                {
                    continue;
                }

                State.EnemiesKilled++;
                enemy.State.IsDead = true;
                bullet.Value.State.RemainingHits--;
                KillZombie(enemy);

                if (bullet.Value.State.RemainingHits <= 0)
                {
                    shouldDeactivate = true;
                    break;
                }
            }

            for (var i = 0; i < bullet.Value.State.CollidedVultures.UsableLength; i++)
            {
                var enemy = bullet.Value.State.CollidedVultures.Data[i];
                if (enemy.State.IsDead)
                {
                    continue;
                }

                State.EnemiesKilled++;
                enemy.State.IsDead = true;
                bullet.Value.State.RemainingHits--;
                ref var vulture = ref State.Vultures.GetAt(enemy.State.Index);
                KillVulture(ref vulture.Value);

                if (bullet.Value.State.RemainingHits <= 0)
                {
                    shouldDeactivate = true;
                    break;
                }
            }

            bullet.Value.State.CollidedZombies.Clear();
            bullet.Value.State.CollidedVultures.Clear();

            if (!State.VisibilityBounds.Contains(bullet.Value.transform.position))
            {
                shouldDeactivate = true;
            }

            if (shouldDeactivate)
            {
                DeactivateBullet(ref bullet);
            }

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

    private static void UpdateVultures()
    {
        if (State.Vultures.Count == 0)
        {
            return;
        }

        ref var vulture = ref State.Vultures.Tail();

        while (true)
        {
            switch (vulture.Value.Action)
            {
                case VultureAction.FlyingLeft:
                    if (vulture.Value.EnemyComponent.transform.position.x < State.VultureMinHorizontalPosition)
                    {
                        vulture.Value.Action = VultureAction.FlyingRight;
                        vulture.Value.EnemyComponent.Animator.Play(VultureAnimations.FlyingRight);
                        vulture.Value.EnemyComponent.RigidBody.velocity = new Vector2(VultureVelocity, 0f);
                    }

                    break;
                case VultureAction.FlyingRight:
                    if (vulture.Value.EnemyComponent.transform.position.x > State.VultureMaxHorizontalPosition)
                    {
                        vulture.Value.LapsMade++;
                        if (vulture.Value.LapsMade < 2)
                        {
                            vulture.Value.Action = VultureAction.FlyingLeft;
                        }
                        else
                        {
                            vulture.Value.Action = VultureAction.PreparingDive;
                        }

                        vulture.Value.EnemyComponent.Animator.Play(VultureAnimations.FlyingLeft);
                        vulture.Value.EnemyComponent.RigidBody.velocity = new Vector2(-VultureVelocity, 0f);
                    }

                    break;
                case VultureAction.PreparingDive:
                    if (vulture.Value.EnemyComponent.transform.position.x <= State.VultureDiveHorizontalPosition)
                    {
                        vulture.Value.Action = VultureAction.Diving;
                        vulture.Value.EnemyComponent.Animator.Play(VultureAnimations.Diving);
                        var toPlayer = _references.Player.Center.position -
                                       vulture.Value.EnemyComponent.transform.position;
                        vulture.Value.EnemyComponent.RigidBody.velocity = toPlayer.normalized * VultureVelocity;
                    }

                    break;
                case VultureAction.Diving:
                    break;

                case VultureAction.Dying:
                    var animatorState = vulture.Value.EnemyComponent.Animator.GetCurrentAnimatorStateInfo(0);
                    if ((animatorState.IsName(VultureAnimations.DyingLeft) ||
                         animatorState.IsName(VultureAnimations.DyingRight)) && animatorState.normalizedTime >= 1f)
                    {
                        DeactivateVulture(ref vulture);
                    }

                    break;
            }

            if (!vulture.HasNext)
            {
                break;
            }

            vulture = ref State.Vultures.Next(ref vulture);
        }
    }

    private static void TryFireStraight(Input input, ref Report report)
    {
        if (!input.FireStraight)
        {
            return;
        }

        TryFire(input, ref report, _references.StraightGunNozzle, Vector2.right);
    }

    private static void TryFireDiagonally(Input input, ref Report report)
    {
        if (!input.FireDiagonally)
        {
            return;
        }

        if (TryFire(input, ref report, _references.DiagonalGunNozzle, (Vector2.right + Vector2.up).normalized))
        {
            _references.Player.Animator.ResetTrigger(PlayerShotDiagonallyAnimationTrigger);
            _references.Player.Animator.SetTrigger(PlayerShotDiagonallyAnimationTrigger);
        }
    }

    private static bool TryFire(Input input, ref Report report, Transform origin, Vector2 direction)
    {
        if (State.IsDead)
        {
            return false;
        }

        if (State.AvailableBulletCount <= 0)
        {
            return false;
        }

        State.AvailableBulletCount--;
        report.FiredBullet = true;
        FireBullet(origin, direction);
        return true;
    }

    private static void FireBullet(Transform origin, Vector2 direction)
    {
        const float bulletVelocity = 500f;
        var bullet = State.InactiveBullets.Pop();
        bullet.State.RemainingHits = 1;
        bullet.RigidBody.detectCollisions = true;
        bullet.RigidBody.velocity = direction * bulletVelocity;
        bullet.transform.position = origin.position;
        var upwards = Vector3.Cross(Vector3.forward, direction);
        bullet.transform.localRotation = Quaternion.LookRotation(Vector3.forward, upwards);
        State.ActiveBullets.Add(bullet);
    }

    private static void DeactivateBullet(ref ArrayListNode<BulletComponent> bullet)
    {
        State.ActiveBullets.Remove(ref bullet);
        State.InactiveBullets.Push(bullet.Value);
        bullet.Value.RigidBody.detectCollisions = false;
        bullet.Value.transform.position = new Vector3(-1000f, 0f, 0f);
    }

    private static void ActivateZombie()
    {
        var zombie = State.InactiveZombies.Pop();
        zombie.State.IsDead = false;
        zombie.State.SpeedFactor = Random.Range(0.8f, 1.5f);
        zombie.State.Index = State.ActiveZombies.Add(zombie);
        zombie.RigidBody.detectCollisions = true;
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

    private static void KillZombie(EnemyComponent zombie)
    {
        zombie.Animator.ResetTrigger(ZombieRunAnimationTrigger);
        zombie.Animator.SetTrigger(ZombieDieAnimationTrigger);
        zombie.State.IsDead = true;
        zombie.RigidBody.velocity = new Vector2(-State.PlayerVelocity, 0f);
    }

    private static void KillVulture(ref Vulture vulture)
    {
        vulture.EnemyComponent.Animator.Play(VultureAnimations.DyingLeft);
        vulture.EnemyComponent.State.IsDead = true;
        vulture.EnemyComponent.RigidBody.velocity = new Vector2(50f, 50f);
        vulture.Action = VultureAction.Dying;
    }

    private static void DeactivateZombie(ref ArrayListNode<EnemyComponent> zombie)
    {
        State.ActiveZombies.Remove(ref zombie);
        State.InactiveZombies.Push(zombie.Value);
        zombie.Value.RigidBody.detectCollisions = false;
        zombie.Value.transform.position = new Vector3(-1000f, 0f, 0f);
    }

    private static void DeactivateVulture(ref ArrayListNode<Vulture> vulture)
    {
        State.Vultures.Remove(ref vulture);
        State.InactiveVultures.Push(vulture.Value.EnemyComponent);
        vulture.Value.EnemyComponent.RigidBody.detectCollisions = false;
        vulture.Value.EnemyComponent.transform.position = new Vector3(-1000f, 0f, 0f);
    }

    public struct Report
    {
        public bool FiredBullet;
        public int CollectedBullets;
        public Transform CollectedBulletsSource;
        public int SpawnedZombies;
    }

    public struct Input
    {
        public bool FireStraight;
        public bool FireDiagonally;
    }
}

public struct Vulture
{
    public int LapsMade;
    public VultureAction Action;
    public EnemyComponent EnemyComponent;
}

public enum VultureAction
{
    FlyingLeft,
    FlyingRight,
    PreparingDive,
    Diving,
    Dying
}

public struct GameState
{
    public float PlayerVelocity;
    public bool IsDead;
    public int EnemiesKilled;
    public Rect VisibilityBounds;
    public float ZombieTickCooldown;
    public float VultureTickCooldown;
    public float VultureMinHorizontalPosition;
    public float VultureMaxHorizontalPosition;
    public float VultureDiveHorizontalPosition;
    public int AvailableBulletCount;
    public ArrayList<Vulture> Vultures;
    public ArrayList<BulletComponent> ActiveBullets;
    public Stack<BulletComponent> InactiveBullets;
    public ArrayList<EnemyComponent> ActiveZombies;
    public Stack<EnemyComponent> InactiveZombies;
    public Stack<EnemyComponent> InactiveVultures;
}