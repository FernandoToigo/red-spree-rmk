using System.Collections.Generic;
using UnityEngine;

public static class Game
{
    public const string ZombieTag = "Zombie";
    public const string VultureTag = "Vulture";
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
        State.VultureMinHorizontalPosition =
            cameraPosition.x + _references.PixelPerfectCamera.refResolutionX * -0.5f + 5;
        State.VultureMaxHorizontalPosition =
            cameraPosition.x + _references.PixelPerfectCamera.refResolutionX * 0.5f - 5;
        State.VultureDiveHorizontalPosition = cameraPosition.x + _references.PixelPerfectCamera.refResolutionX * 0.15f;
    }

    private static void InitializeBullets()
    {
        State.AvailableBulletCount = 3;
        State.Bullets = new ArrayList<Bullet>(_references.Bullets.Length);
        State.AvailableBulletComponents = new Stack<BulletComponent>(_references.Bullets.Length);

        foreach (var bullet in _references.Bullets)
        {
            bullet.CollidedZombies = new ReusableArray<EnemyComponent>(10);
            bullet.CollidedVultures = new ReusableArray<EnemyComponent>(10);
            bullet.RigidBody.detectCollisions = false;
            State.AvailableBulletComponents.Push(bullet);
        }
    }

    private static void InitializeZombies()
    {
        State.Zombies = new ArrayList<Zombie>(_references.Zombies.Length);
        State.AvailableZombieComponents = new Stack<EnemyComponent>(_references.Zombies.Length);

        foreach (var zombie in _references.Zombies)
        {
            zombie.RigidBody.detectCollisions = false;
            State.AvailableZombieComponents.Push(zombie);
        }
    }

    private static void InitializeVultures()
    {
        State.Vultures = new ArrayList<Vulture>(_references.Vultures.Length);
        State.AvailableVultureComponents = new Stack<EnemyComponent>(_references.Vultures.Length);

        foreach (var vulture in _references.Vultures)
        {
            vulture.RigidBody.detectCollisions = false;
            State.AvailableVultureComponents.Push(vulture);
        }
    }

    public static Report Update(Input input, FrameTime time)
    {
        var report = new Report();

        TryFireStraight(input, ref report);
        TryFireDiagonally(input, ref report);
        TryCollideWithEnemies(ref report);
        //TrySpawnZombies(time, ref report);
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
            for (var i = 0; i < _references.Player.CollidedZombies.UsableLength; i++)
            {
                ref var enemyNode = ref State.Zombies.GetAt(_references.Player.CollidedZombies.Data[i].Index);
                if (enemyNode.Value.IsDead)
                {
                    var bullets = Random.value <= 0.4f ? 2 : 1;
                    report.CollectedBulletsSource = _references.Player.CollidedZombies.Data[i].transform;
                    report.CollectedBullets += bullets;
                    State.AvailableBulletCount += bullets;
                }
                else
                {
                    KillPlayer();
                    break;
                }
            }

            for (var i = 0; i < _references.Player.CollidedVultures.UsableLength; i++)
            {
                ref var vultureNode = ref State.Zombies.GetAt(_references.Player.CollidedVultures.Data[i].Index);
                if (!vultureNode.Value.IsDead)
                {
                    KillPlayer();
                    break;
                }
            }
        }

        _references.Player.CollidedZombies.Clear();
    }

    private static void KillPlayer()
    {
        State.IsDead = true;
        State.PlayerVelocity = 0f;
        _references.Player.Animator.SetTrigger(PlayerDieAnimationTrigger);
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
        var waveFactor = ((Mathf.Sin((x - p / 4f) * Mathf.PI * 2f / p) + 1f) / 2f) * Mathf.Pow(x / p, 1.6f);
        var percentSpawned = Mathf.Clamp01(State.Zombies.Count / (waveFactor * maxZombies));

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
        var waveFactor = ((Mathf.Sin((x - p / 4f) * Mathf.PI * 2f / p) + 1f) / 2f) * Mathf.Pow(x / p, 1.7f);

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
        var vulture = new Vulture
        {
            Action = VultureAction.FlyingLeft,
            LapsMade = 0,
            IsDead = false,
            SpeedFactor = Random.Range(0.8f, 1.5f),
            Component = State.AvailableVultureComponents.Pop()
        };
        vulture.Component.RigidBody.detectCollisions = true;
        vulture.Component.RigidBody.velocity = new Vector2(-GetVultureSpeed(ref vulture), 0f);
        vulture.Component.transform.position = Vector3.Lerp(_references.MinVultureSpawn.position,
            _references.MaxVultureSpawn.position, Random.value);
        vulture.Component.Animator.Play(VultureAnimations.FlyingLeft);
        vulture.Component.Index = State.Vultures.Add(vulture);
    }

    private static void UpdateBullets(FrameTime time)
    {
        if (State.Bullets.Count == 0)
        {
            return;
        }

        ref var bulletNode = ref State.Bullets.Tail();

        while (true)
        {
            var shouldDeactivate = false;
            for (var i = 0; i < bulletNode.Value.Component.CollidedZombies.UsableLength; i++)
            {
                var index = bulletNode.Value.Component.CollidedZombies.Data[i].Index;
                ref var zombieNode = ref State.Zombies.GetAt(index);
                if (zombieNode.Value.IsDead)
                {
                    continue;
                }

                State.EnemiesKilled++;
                zombieNode.Value.IsDead = true;
                bulletNode.Value.RemainingHits--;
                KillZombie(ref zombieNode);

                if (bulletNode.Value.RemainingHits <= 0)
                {
                    shouldDeactivate = true;
                    break;
                }
            }

            for (var i = 0; i < bulletNode.Value.Component.CollidedVultures.UsableLength; i++)
            {
                var index = bulletNode.Value.Component.CollidedVultures.Data[i].Index;
                ref var vultureNode = ref State.Vultures.GetAt(index);
                if (vultureNode.Value.IsDead)
                {
                    continue;
                }

                State.EnemiesKilled++;
                vultureNode.Value.IsDead = true;
                bulletNode.Value.RemainingHits--;
                KillVulture(ref vultureNode);

                if (bulletNode.Value.RemainingHits <= 0)
                {
                    shouldDeactivate = true;
                    break;
                }
            }

            bulletNode.Value.Component.CollidedZombies.Clear();
            bulletNode.Value.Component.CollidedVultures.Clear();

            if (!State.VisibilityBounds.Contains(bulletNode.Value.Component.transform.position))
            {
                shouldDeactivate = true;
            }

            if (shouldDeactivate)
            {
                DeactivateBullet(ref bulletNode);
            }

            if (!bulletNode.HasNext)
            {
                break;
            }

            bulletNode = ref State.Bullets.Next(ref bulletNode);
        }
    }

    private static void UpdateZombies()
    {
        if (State.Zombies.Count == 0)
        {
            return;
        }

        ref var zombie = ref State.Zombies.Tail();

        while (true)
        {
            if (!State.VisibilityBounds.Contains(zombie.Value.Component.transform.position))
            {
                DeactivateZombie(ref zombie);
            }

            zombie.Value.Component.RigidBody.velocity = GetZombieVelocity(ref zombie.Value);

            if (!zombie.HasNext)
            {
                break;
            }

            zombie = ref State.Zombies.Next(ref zombie);
        }
    }

    private static void UpdateVultures()
    {
        if (State.Vultures.Count == 0)
        {
            return;
        }

        ref var vultureNode = ref State.Vultures.Tail();

        while (true)
        {
            switch (vultureNode.Value.Action)
            {
                case VultureAction.FlyingLeft:
                    if (vultureNode.Value.Component.transform.position.x < State.VultureMinHorizontalPosition)
                    {
                        vultureNode.Value.Action = VultureAction.FlyingRight;
                        vultureNode.Value.Component.Animator.Play(VultureAnimations.FlyingRight);
                        vultureNode.Value.Component.RigidBody.velocity =
                            new Vector2(GetVultureSpeed(ref vultureNode.Value), 0f);
                    }

                    break;
                case VultureAction.FlyingRight:
                    if (vultureNode.Value.Component.transform.position.x > State.VultureMaxHorizontalPosition)
                    {
                        vultureNode.Value.LapsMade++;
                        if (vultureNode.Value.LapsMade < 2)
                        {
                            vultureNode.Value.Action = VultureAction.FlyingLeft;
                        }
                        else
                        {
                            vultureNode.Value.Action = VultureAction.PreparingDive;
                        }

                        vultureNode.Value.Component.Animator.Play(VultureAnimations.FlyingLeft);
                        vultureNode.Value.Component.RigidBody.velocity =
                            new Vector2(-GetVultureSpeed(ref vultureNode.Value), 0f);
                    }

                    break;
                case VultureAction.PreparingDive:
                    if (vultureNode.Value.Component.transform.position.x <= State.VultureDiveHorizontalPosition)
                    {
                        vultureNode.Value.Action = VultureAction.Diving;
                        vultureNode.Value.Component.Animator.Play(VultureAnimations.Diving);
                        var toPlayer = _references.Player.Center.position -
                                       vultureNode.Value.Component.transform.position;
                        vultureNode.Value.Component.RigidBody.velocity =
                            toPlayer.normalized * GetVultureSpeed(ref vultureNode.Value);
                    }

                    break;
                case VultureAction.Diving:
                    break;

                case VultureAction.Dying:
                    var animatorState = vultureNode.Value.Component.Animator.GetCurrentAnimatorStateInfo(0);
                    if ((animatorState.IsName(VultureAnimations.DyingLeft) ||
                         animatorState.IsName(VultureAnimations.DyingRight)) && animatorState.normalizedTime >= 1f)
                    {
                        DeactivateVulture(ref vultureNode);
                    }

                    break;
            }

            if (!vultureNode.HasNext)
            {
                break;
            }

            vultureNode = ref State.Vultures.Next(ref vultureNode);
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
        var bullet = new Bullet
        {
            RemainingHits = State.EnemiesKilled > 500 ? 2 : 1,
            Component = State.AvailableBulletComponents.Pop()
        };
        bullet.Component.Index = State.Bullets.Add(bullet);
        bullet.Component.RigidBody.detectCollisions = true;
        bullet.Component.RigidBody.velocity = direction * bulletVelocity;
        bullet.Component.transform.position = origin.position;
        var upwards = Vector3.Cross(Vector3.forward, direction);
        bullet.Component.transform.localRotation = Quaternion.LookRotation(Vector3.forward, upwards);
    }

    private static void DeactivateBullet(ref ArrayListNode<Bullet> bulletNode)
    {
        State.Bullets.Remove(ref bulletNode);
        State.AvailableBulletComponents.Push(bulletNode.Value.Component);
        bulletNode.Value.Component.RigidBody.velocity = Vector3.zero;
        bulletNode.Value.Component.RigidBody.detectCollisions = false;
        bulletNode.Value.Component.transform.position = new Vector3(-1000f, 0f, 0f);
    }

    private static void ActivateZombie()
    {
        var zombie = new Zombie
        {
            IsDead = false,
            SpeedFactor = Random.Range(0.8f, 1.5f),
            Component = State.AvailableZombieComponents.Pop()
        };
        zombie.Component.Index = State.Zombies.Add(zombie);
        zombie.Component.RigidBody.detectCollisions = true;
        zombie.Component.RigidBody.velocity = GetZombieVelocity(ref zombie);
        zombie.Component.transform.position = _references.ZombieSpawn.position;
        zombie.Component.Animator.SetTrigger(ZombieRunAnimationTrigger);
    }

    private static float GetVultureSpeed(ref Vulture vulture)
    {
        return VultureVelocity * vulture.SpeedFactor;
    }

    private static Vector2 GetZombieVelocity(ref Zombie zombie)
    {
        if (zombie.IsDead)
        {
            return new Vector2(-State.PlayerVelocity, 0f);
        }

        var velocity = -(ZombieVelocity + State.PlayerVelocity) * zombie.SpeedFactor;
        return new Vector2(velocity, 0f);
    }

    private static void KillZombie(ref ArrayListNode<Zombie> zombieNode)
    {
        zombieNode.Value.IsDead = true;
        zombieNode.Value.Component.Animator.ResetTrigger(ZombieRunAnimationTrigger);
        zombieNode.Value.Component.Animator.SetTrigger(ZombieDieAnimationTrigger);
        zombieNode.Value.Component.RigidBody.velocity = new Vector2(-State.PlayerVelocity, 0f);
    }

    private static void KillVulture(ref ArrayListNode<Vulture> vultureNode)
    {
        vultureNode.Value.IsDead = true;
        vultureNode.Value.Component.Animator.Play(VultureAnimations.DyingLeft);
        vultureNode.Value.Component.RigidBody.velocity = new Vector2(50f, 50f);
        vultureNode.Value.Action = VultureAction.Dying;
    }

    private static void DeactivateZombie(ref ArrayListNode<Zombie> zombie)
    {
        State.Zombies.Remove(ref zombie);
        State.AvailableZombieComponents.Push(zombie.Value.Component);
        zombie.Value.Component.RigidBody.detectCollisions = false;
        zombie.Value.Component.transform.position = new Vector3(-1000f, 0f, 0f);
    }

    private static void DeactivateVulture(ref ArrayListNode<Vulture> vulture)
    {
        State.Vultures.Remove(ref vulture);
        State.AvailableVultureComponents.Push(vulture.Value.Component);
        vulture.Value.Component.RigidBody.detectCollisions = false;
        vulture.Value.Component.transform.position = new Vector3(-1000f, 0f, 0f);
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
    public ArrayList<Bullet> Bullets;
    public ArrayList<Zombie> Zombies;
    public Stack<BulletComponent> AvailableBulletComponents;
    public Stack<EnemyComponent> AvailableZombieComponents;
    public Stack<EnemyComponent> AvailableVultureComponents;
}

public struct Bullet
{
    public int RemainingHits;
    public BulletComponent Component;
}

public struct Zombie
{
    public bool IsDead;
    public float SpeedFactor;
    public EnemyComponent Component;
}

public struct Vulture
{
    public bool IsDead;
    public float SpeedFactor;
    public int LapsMade;
    public VultureAction Action;
    public EnemyComponent Component;
}

public enum VultureAction
{
    FlyingLeft,
    FlyingRight,
    PreparingDive,
    Diving,
    Dying
}