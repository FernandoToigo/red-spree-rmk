using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class Game
{
    private static References _references;
    private static Definitions _definitions;
    public static GameState State;

    public static void Initialize(References references, Definitions definitions)
    {
        _references = references;
        _definitions = definitions;

        State = new GameState
        {
            TimeFactor = 1f,
            PlayerVelocity = _definitions.PlayerSpeed,
            DiagonalShootDirection = (Vector2.right + Vector2.up).normalized,
            BulletPenetration = 0
        };

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
        State.VultureDiveHorizontalPosition = cameraPosition.x;
    }

    private static void InitializeBullets()
    {
        State.AvailableBulletCount = _definitions.StartingBulletCount;
        State.Bullets = new ArrayList<Bullet>(_references.Bullets.Length);
        State.BulletComponentsPool = new Stack<BulletComponent>(_references.Bullets.Length);

        foreach (var bullet in _references.Bullets)
        {
            bullet.CollidedZombies = new ReusableArray<EnemyComponent>(10);
            bullet.CollidedVultures = new ReusableArray<EnemyComponent>(10);
            bullet.RigidBody.detectCollisions = false;
            State.BulletComponentsPool.Push(bullet);
        }
    }

    private static void InitializeZombies()
    {
        State.Zombies = new ArrayList<Zombie>(_references.Zombies.Length);
        State.ZombieComponentsPool = new Stack<EnemyComponent>(_references.Zombies.Length);

        foreach (var zombie in _references.Zombies)
        {
            zombie.RigidBody.detectCollisions = false;
            State.ZombieComponentsPool.Push(zombie);
        }
    }

    private static void InitializeVultures()
    {
        State.Vultures = new ArrayList<Vulture>(_references.Vultures.Length);
        State.VultureComponentsPool = new Stack<EnemyComponent>(_references.Vultures.Length);

        foreach (var vulture in _references.Vultures)
        {
            vulture.RigidBody.detectCollisions = false;
            State.VultureComponentsPool.Push(vulture);
        }
    }

    public static Report Update(Input input, FrameTime time)
    {
        var report = new Report();

        TryStartGame(input);

        if (!State.HasStarted)
        {
            return report;
        }

        TryToggleAutoFire(input);
        TryAutoFire(ref input);
        TryChangeTimeFactor(input);
        PassTime(ref time);
        TryUpgradeBulletPenetration(ref report);
        TryFireStraight(input, ref report);
        TryFireDiagonally(input, ref report);
        CheckPlayerEnemyCollisions(ref report);
        TrySpawnZombies(time, ref report);
        TrySpawnVultures(time);
        UpdateBullets(ref report);
        UpdateZombies();
        UpdateVultures();
        UpdatePhysics(time);
        CheckAutoFirePendingKills(report);

        return report;
    }

    private static void ResetGame()
    {
        var previousState = State;
        State = new GameState
        {
            TimeFactor = 1f,
            PlayerVelocity = _definitions.PlayerSpeed,
            AvailableBulletCount = _definitions.StartingBulletCount,
            DiagonalShootDirection = previousState.DiagonalShootDirection,
            Bullets = previousState.Bullets,
            Zombies = previousState.Zombies,
            Vultures = previousState.Vultures,
            BulletComponentsPool = previousState.BulletComponentsPool,
            ZombieComponentsPool = previousState.ZombieComponentsPool,
            VultureComponentsPool = previousState.VultureComponentsPool,
            VisibilityBounds = previousState.VisibilityBounds,
            VultureMinHorizontalPosition = previousState.VultureMinHorizontalPosition,
            VultureMaxHorizontalPosition = previousState.VultureMaxHorizontalPosition,
            VultureDiveHorizontalPosition = previousState.VultureDiveHorizontalPosition,
        };

        var bulletsIterator = State.Bullets.Iterate();
        while (bulletsIterator.Next())
        {
            DeactivateBullet(ref bulletsIterator.Current());
        }
        
        var zombiesIterator = State.Zombies.Iterate();
        while (zombiesIterator.Next())
        {
            DeactivateZombie(ref zombiesIterator.Current());
        }
        
        var vulturesIterator = State.Vultures.Iterate();
        while (vulturesIterator.Next())
        {
            DeactivateVulture(ref vulturesIterator.Current());
        }
        
        _references.Player.Animator.Play(PlayerAnimations.Running);
    }

    private static void TryStartGame(Input input)
    {
        if (!input.StartGame)
        {
            return;
        }

        ResetGame();
        State.HasStarted = true;
    }

    private static void TryToggleAutoFire(Input input)
    {
        if (!input.ToggleAutoFire)
        {
            return;
        }

        State.AutoFire.IsActive = !State.AutoFire.IsActive;
    }

    private static void TryAutoFire(ref Input input)
    {
        if (!State.AutoFire.IsActive)
        {
            return;
        }

        TryAutoFireToZombies(ref input);
        TryAutoFireToVultures(ref input);
    }

    private static void TryAutoFireToZombies(ref Input input)
    {
        var zombieIterator = State.Zombies.Iterate();

        var nearZombies = 0;
        while (zombieIterator.Next())
        {
            ref var zombieNode = ref zombieIterator.Current();
            if (zombieNode.Value.IsDead)
            {
                continue;
            }

            var difference = zombieNode.Value.Component.transform.position.x - _references.Player.transform.position.x;
            if (difference < 40f)
            {
                nearZombies++;
            }
        }

        if (nearZombies - State.AutoFire.PendingZombieKills > 0)
        {
            input.FireStraight = true;
            State.AutoFire.PendingZombieKills++;
        }
    }

    private static void TryAutoFireToVultures(ref Input input)
    {
        var vulturesIterator = State.Vultures.Iterate();

        var nearVultures = 0;
        while (vulturesIterator.Next())
        {
            ref var vultureNode = ref vulturesIterator.Current();
            if (vultureNode.Value.IsDead)
            {
                continue;
            }

            var difference = vultureNode.Value.Component.transform.position - _references.Player.transform.position;
            if (difference.x < 40f && difference.y < 40f)
            {
                nearVultures++;
            }
        }

        if (nearVultures - State.AutoFire.PendingVultureKills > 0)
        {
            input.FireDiagonally = true;
            State.AutoFire.PendingVultureKills++;
        }
    }

    private static void CheckAutoFirePendingKills(Report report)
    {
        State.AutoFire.PendingZombieKills = Math.Max(0, State.AutoFire.PendingZombieKills - report.KilledZombies);
        State.AutoFire.PendingVultureKills = Math.Max(0, State.AutoFire.PendingVultureKills - report.KilledVultures);
    }

    private static void TryUpgradeBulletPenetration(ref Report report)
    {
        var bulletPenetration = State.EnemiesKilled / 500;
        if (bulletPenetration <= State.BulletPenetration)
        {
            return;
        }

        State.BulletPenetration = bulletPenetration;
        report.BulletPenetrationUpgraded = true;
    }

    private static void PassTime(ref FrameTime time)
    {
        time.DeltaSeconds *= State.TimeFactor;
        State.SecondsPassed += time.DeltaSeconds;
    }

    private static void TryChangeTimeFactor(Input input)
    {
        if (!input.ChangedTimeFactor.HasValue)
        {
            return;
        }

        State.TimeFactor = input.ChangedTimeFactor.Value;

        _references.Player.Animator.speed = State.TimeFactor;
        for (var i = 0; i < _references.Zombies.Length; i++)
        {
            _references.Zombies[i].Animator.speed = State.TimeFactor;
        }

        for (var i = 0; i < _references.Vultures.Length; i++)
        {
            _references.Vultures[i].Animator.speed = State.TimeFactor;
        }
    }

    private static void UpdatePhysics(FrameTime time)
    {
        Physics.Simulate(time.DeltaSeconds);
    }

    private static void CheckPlayerEnemyCollisions(ref Report report)
    {
        if (!State.IsDead)
        {
            CheckZombieCollisions(ref report);
            CheckVultureCollisions(ref report);
        }

        _references.Player.CollidedZombies.Clear();
        _references.Player.CollidedVultures.Clear();
    }

    private static void CheckZombieCollisions(ref Report report)
    {
        for (var i = 0; i < _references.Player.CollidedZombies.UsableLength; i++)
        {
            ref var enemyNode = ref State.Zombies.GetAt(_references.Player.CollidedZombies.Data[i].Index);
            if (enemyNode.Value.IsDead)
            {
                var bullets = Random.value <= _definitions.DoubleBulletCollectionChance ? 2 : 1;
                report.CollectedBulletsSource = _references.Player.CollidedZombies.Data[i].transform;
                report.CollectedBullets += bullets;
                State.AvailableBulletCount += bullets;
            }
            else
            {
                KillPlayer(ref report);
                break;
            }
        }
    }

    private static void CheckVultureCollisions(ref Report report)
    {
        for (var i = 0; i < _references.Player.CollidedVultures.UsableLength; i++)
        {
            ref var vultureNode = ref State.Vultures.GetAt(_references.Player.CollidedVultures.Data[i].Index);
            if (vultureNode.Value.IsDead)
            {
                continue;
            }

            KillPlayer(ref report);
            break;
        }
    }

    private static void KillPlayer(ref Report report)
    {
        report.Died = true;
        State.IsDead = true;
        State.PlayerVelocity = 0f;
        _references.Player.Animator.Play(PlayerAnimations.Dying);
    }

    private static void TrySpawnZombies(FrameTime time, ref Report report)
    {
        if (State.IsDead)
        {
            return;
        }

        State.ZombieTickCooldown += time.DeltaSeconds;

        if (State.ZombieTickCooldown < _definitions.ZombieSpawnTickSeconds)
        {
            return;
        }

        State.ZombieTickCooldown -= _definitions.ZombieSpawnTickSeconds;
        var x = State.SecondsPassed;
        const float p = 15f;
        const float maxZombies = 1f;
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
        if (State.IsDead)
        {
            return;
        }

        State.VultureTickCooldown += time.DeltaSeconds;

        if (State.VultureTickCooldown < _definitions.VultureSpawnTickSeconds)
        {
            return;
        }

        State.VultureTickCooldown -= _definitions.VultureSpawnTickSeconds;
        var x = State.SecondsPassed;
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
            SpeedFactor = Random.Range(_definitions.VultureMinimumSpeedFactor, _definitions.VultureMaximumSpeedFactor),
            Component = State.VultureComponentsPool.Pop()
        };
        vulture.Component.RigidBody.detectCollisions = true;
        vulture.Component.RigidBody.velocity = new Vector2(-GetVultureSpeed(ref vulture), 0f);
        vulture.Component.transform.position = Vector3.Lerp(
            _references.MinVultureSpawn.position,
            _references.MaxVultureSpawn.position, Random.value);
        vulture.Component.Animator.Play(VultureAnimations.FlyingLeft);
        vulture.Component.Index = State.Vultures.Add(vulture);
    }

    private static void UpdateBullets(ref Report report)
    {
        var bulletIterator = State.Bullets.Iterate();

        while (bulletIterator.Next())
        {
            ref var bulletNode = ref bulletIterator.Current();

            CheckBulletZombieCollisions(ref bulletNode, ref report);
            if (bulletNode.Value.RemainingHits > 0)
            {
                CheckBulletVultureCollisions(ref bulletNode, ref report);
            }

            var isBulletInsideBounds = IsInsideBounds(bulletNode.Value.Component.transform.position);
            if (bulletNode.Value.RemainingHits <= 0 || !isBulletInsideBounds)
            {
                DeactivateBullet(ref bulletNode);
            }
        }
    }

    private static void CheckBulletZombieCollisions(ref ArrayListNode<Bullet> bulletNode, ref Report report)
    {
        for (var i = 0; i < bulletNode.Value.Component.CollidedZombies.UsableLength; i++)
        {
            var index = bulletNode.Value.Component.CollidedZombies.Data[i].Index;
            ref var zombieNode = ref State.Zombies.GetAt(index);
            if (zombieNode.Value.IsDead)
            {
                continue;
            }

            bulletNode.Value.RemainingHits--;
            KillZombie(ref zombieNode);
            report.KilledZombies++;

            if (bulletNode.Value.RemainingHits <= 0)
            {
                break;
            }
        }

        bulletNode.Value.Component.CollidedZombies.Clear();
    }

    private static void CheckBulletVultureCollisions(ref ArrayListNode<Bullet> bulletNode, ref Report report)
    {
        for (var i = 0; i < bulletNode.Value.Component.CollidedVultures.UsableLength; i++)
        {
            var index = bulletNode.Value.Component.CollidedVultures.Data[i].Index;
            ref var vultureNode = ref State.Vultures.GetAt(index);
            if (vultureNode.Value.IsDead)
            {
                continue;
            }

            bulletNode.Value.RemainingHits--;
            KillVulture(ref vultureNode);
            report.KilledVultures++;

            if (bulletNode.Value.RemainingHits <= 0)
            {
                break;
            }
        }

        bulletNode.Value.Component.CollidedVultures.Clear();
    }

    private static void UpdateZombies()
    {
        var zombieIterator = State.Zombies.Iterate();

        while (zombieIterator.Next())
        {
            ref var zombie = ref zombieIterator.Current();

            if (!IsInsideBounds(zombie.Value.Component.transform.position))
            {
                DeactivateZombie(ref zombie);
                continue;
            }

            zombie.Value.Component.RigidBody.velocity = GetZombieVelocity(ref zombie.Value);
        }
    }

    private static void UpdateVultures()
    {
        var vultureIterator = State.Vultures.Iterate();

        while (vultureIterator.Next())
        {
            ref var vultureNode = ref vultureIterator.Current();

            switch (vultureNode.Value.Action)
            {
                case VultureAction.FlyingLeft:
                    UpdateVultureFlyingLeft(ref vultureNode);
                    break;
                case VultureAction.FlyingRight:
                    UpdateVultureFlyingRight(ref vultureNode);
                    break;
                case VultureAction.PreparingDive:
                    UpdateVulturePreparingDive(ref vultureNode);
                    break;
                case VultureAction.Dying:
                    UpdateVultureDying(ref vultureNode);
                    break;
            }
        }
    }

    private static void UpdateVultureFlyingLeft(ref ArrayListNode<Vulture> vultureNode)
    {
        if (vultureNode.Value.Component.transform.position.x >= State.VultureMinHorizontalPosition)
        {
            return;
        }

        vultureNode.Value.Action = VultureAction.FlyingRight;
        vultureNode.Value.Component.Animator.Play(VultureAnimations.FlyingRight);
        vultureNode.Value.Component.RigidBody.velocity = new Vector2(GetVultureSpeed(ref vultureNode.Value), 0f);
    }

    private static void UpdateVultureFlyingRight(ref ArrayListNode<Vulture> vultureNode)
    {
        if (vultureNode.Value.Component.transform.position.x <= State.VultureMaxHorizontalPosition)
        {
            return;
        }

        vultureNode.Value.LapsMade++;

        vultureNode.Value.Action = vultureNode.Value.LapsMade switch
        {
            2 => VultureAction.FlyingLeft,
            _ => VultureAction.PreparingDive
        };

        vultureNode.Value.Component.Animator.Play(VultureAnimations.FlyingLeft);
        vultureNode.Value.Component.RigidBody.velocity = new Vector2(-GetVultureSpeed(ref vultureNode.Value), 0f);
    }

    private static void UpdateVulturePreparingDive(ref ArrayListNode<Vulture> vultureNode)
    {
        if (vultureNode.Value.Component.transform.position.x > State.VultureDiveHorizontalPosition)
        {
            return;
        }

        vultureNode.Value.Action = VultureAction.Diving;
        vultureNode.Value.Component.Animator.Play(VultureAnimations.Diving);
        var differenceToPlayer = _references.Player.Center.position -
                                 vultureNode.Value.Component.transform.position;
        vultureNode.Value.Component.RigidBody.velocity =
            differenceToPlayer.normalized * GetVultureSpeed(ref vultureNode.Value);
    }

    private static void UpdateVultureDying(ref ArrayListNode<Vulture> vultureNode)
    {
        var animatorState = vultureNode.Value.Component.Animator.GetCurrentAnimatorStateInfo(0);
        if (animatorState.IsName(VultureAnimations.Dying) && animatorState.normalizedTime >= 1f)
        {
            DeactivateVulture(ref vultureNode);
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

        if (TryFire(input, ref report, _references.DiagonalGunNozzle, State.DiagonalShootDirection))
        {
            _references.Player.Animator.Play(PlayerAnimations.ShootingUp);
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
        var bullet = new Bullet
        {
            RemainingHits = 1 + State.BulletPenetration,
            Component = State.BulletComponentsPool.Pop()
        };
        bullet.Component.Index = State.Bullets.Add(bullet);
        bullet.Component.RigidBody.detectCollisions = true;
        bullet.Component.RigidBody.velocity = direction * _definitions.BulletSpeed;
        bullet.Component.transform.position = origin.position;
        var upwards = Vector3.Cross(Vector3.forward, direction);
        bullet.Component.transform.localRotation = Quaternion.LookRotation(Vector3.forward, upwards);
    }

    private static void DeactivateBullet(ref ArrayListNode<Bullet> bulletNode)
    {
        State.Bullets.Remove(ref bulletNode);
        State.BulletComponentsPool.Push(bulletNode.Value.Component);
        bulletNode.Value.Component.RigidBody.velocity = Vector3.zero;
        bulletNode.Value.Component.RigidBody.detectCollisions = false;
        bulletNode.Value.Component.transform.position = new Vector3(-1000f, 0f, 0f);
    }

    private static void ActivateZombie()
    {
        var zombie = new Zombie
        {
            IsDead = false,
            SpeedFactor = Random.Range(_definitions.ZombieMinimumSpeedFactor, _definitions.ZombieMaximumSpeedFactor),
            Component = State.ZombieComponentsPool.Pop()
        };
        zombie.Component.Index = State.Zombies.Add(zombie);
        zombie.Component.RigidBody.detectCollisions = true;
        zombie.Component.RigidBody.velocity = GetZombieVelocity(ref zombie);
        zombie.Component.transform.position = _references.ZombieSpawn.position;
        zombie.Component.Animator.Play(ZombieAnimations.Running);
    }

    private static float GetVultureSpeed(ref Vulture vulture)
    {
        return _definitions.VultureSpeed * vulture.SpeedFactor;
    }

    private static Vector2 GetZombieVelocity(ref Zombie zombie)
    {
        if (zombie.IsDead)
        {
            return new Vector2(-State.PlayerVelocity, 0f);
        }

        var velocity = (_definitions.ZombieBaseSpeed * zombie.SpeedFactor + State.PlayerVelocity) * -1f;
        return new Vector2(velocity, 0f);
    }

    private static void KillZombie(ref ArrayListNode<Zombie> zombieNode)
    {
        State.EnemiesKilled++;
        zombieNode.Value.IsDead = true;
        zombieNode.Value.Component.Animator.Play(ZombieAnimations.Dying);
        zombieNode.Value.Component.RigidBody.velocity = new Vector2(State.PlayerVelocity * -1f, 0f);
    }

    private static void KillVulture(ref ArrayListNode<Vulture> vultureNode)
    {
        State.EnemiesKilled++;
        vultureNode.Value.IsDead = true;
        vultureNode.Value.Component.Animator.Play(VultureAnimations.Dying);
        vultureNode.Value.Component.RigidBody.velocity = new Vector2(50f, 50f);
        vultureNode.Value.Action = VultureAction.Dying;
    }

    private static void DeactivateZombie(ref ArrayListNode<Zombie> zombie)
    {
        State.Zombies.Remove(ref zombie);
        State.ZombieComponentsPool.Push(zombie.Value.Component);
        zombie.Value.Component.RigidBody.velocity = new Vector2();
        zombie.Value.Component.RigidBody.detectCollisions = false;
        zombie.Value.Component.transform.position = new Vector3(-1000f, 0f, 0f);
    }

    private static void DeactivateVulture(ref ArrayListNode<Vulture> vulture)
    {
        State.Vultures.Remove(ref vulture);
        State.VultureComponentsPool.Push(vulture.Value.Component);
        vulture.Value.Component.RigidBody.velocity = new Vector2();
        vulture.Value.Component.RigidBody.detectCollisions = false;
        vulture.Value.Component.transform.position = new Vector3(-1000f, 0f, 0f);
    }

    private static bool IsInsideBounds(Vector3 position)
    {
        return State.VisibilityBounds.Contains(position);
    }

    public struct Report
    {
        public bool FiredBullet;
        public int CollectedBullets;
        public Transform CollectedBulletsSource;
        public int SpawnedZombies;
        public int KilledZombies;
        public int KilledVultures;
        public bool BulletPenetrationUpgraded;
        public bool Died;
    }

    public struct Input
    {
        public bool StartGame;
        public bool FireStraight;
        public bool FireDiagonally;
        public float? ChangedTimeFactor;
        public bool ToggleAutoFire;
    }
}

public struct GameState
{
    public bool HasStarted;
    public float TimeFactor;
    public float SecondsPassed;
    public float PlayerVelocity;
    public int AvailableBulletCount;
    public int BulletPenetration;
    public Vector3 DiagonalShootDirection;
    public bool IsDead;
    public int EnemiesKilled;
    public Rect VisibilityBounds;
    public float ZombieTickCooldown;
    public float VultureTickCooldown;
    public float VultureMinHorizontalPosition;
    public float VultureMaxHorizontalPosition;
    public float VultureDiveHorizontalPosition;
    public ArrayList<Vulture> Vultures;
    public ArrayList<Bullet> Bullets;
    public ArrayList<Zombie> Zombies;
    public Stack<BulletComponent> BulletComponentsPool;
    public Stack<EnemyComponent> ZombieComponentsPool;
    public Stack<EnemyComponent> VultureComponentsPool;
    public GameStateAutoFire AutoFire;
}

public struct GameStateAutoFire
{
    public bool IsActive;
    public int PendingZombieKills;
    public int PendingVultureKills;
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