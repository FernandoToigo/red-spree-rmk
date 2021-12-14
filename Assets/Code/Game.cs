using System.Collections.Generic;
using UnityEngine;

public static class Game
{
    private static readonly int Die = Animator.StringToHash("Die");
    private static References _references;
    private static State _state;

    public static void Initialize(References references)
    {
        _references = references;
        _state.ActiveBullets = new ArrayList<BulletComponent>(references.Bullets.Length);
        _state.InactiveBullets = new Stack<BulletComponent>(references.Bullets.Length);
        _state.ActiveZombies = new ArrayList<EnemyComponent>(references.Zombies.Length);
        _state.InactiveZombies = new Stack<EnemyComponent>(references.Zombies.Length);
        _state.BulletBounds = new Rect(
            _references.Camera.transform.position.x - _references.Camera.refResolutionX * 0.5f,
            _references.Camera.transform.position.y - _references.Camera.refResolutionY * 0.5f,
            _references.Camera.refResolutionX,
            _references.Camera.refResolutionY);

        InitializeBullets();
        InitializeZombies();
    }

    private static void InitializeBullets()
    {
        foreach (var bullet in _references.Bullets)
        {
            bullet.State.CollidedEnemies = new ReusableArray<EnemyComponent>(10);
            bullet.RigidBody.simulated = false;
            _state.InactiveBullets.Push(bullet);
        }
    }

    private static void InitializeZombies()
    {
        foreach (var zombie in _references.Zombies)
        {
            zombie.RigidBody.simulated = false;
            _state.InactiveZombies.Push(zombie);
        }
    }

    public static void Update(Input input, FrameTime time)
    {
        TryFireStraight(input);
        SpawnZombies(time);
        UpdateBullets();
        UpdateZombies();
    }

    private static void SpawnZombies(FrameTime time)
    {
        const float zombieTickSeconds = 0.25f;
        _state.ZombieTickCooldown += time.DeltaSeconds;

        if (_state.ZombieTickCooldown < zombieTickSeconds)
        {
            return;
        }

        _state.ZombieTickCooldown -= zombieTickSeconds;
        var x = time.TotalSeconds;
        const float p = 15f;
        const float maxZombies = 3f;
        // https://www.desmos.com/calculator/1o7gniviux
        var waveFactor = ((Mathf.Sin((x - p / 4f) * Mathf.PI * 2f / p) + 1f) / 2f) * Mathf.Pow(x / p, 1.5f);
        var percentSpawned = Mathf.Clamp01(_state.ActiveZombies.Count / (waveFactor * maxZombies));

        if (Random.value < (1f - percentSpawned))
        {
            ActivateZombie();
        }
    }

    private static void UpdateBullets()
    {
        if (_state.ActiveBullets.Count == 0)
        {
            return;
        }

        ref var bullet = ref _state.ActiveBullets.Tail();

        while (true)
        {
            var shouldDeactivate = false;
            for (var i = 0; i < bullet.Value.State.CollidedEnemies.UsableLength; i++)
            {
                var enemy = bullet.Value.State.CollidedEnemies.Data[i];
                enemy.State.IsDead = true;
                bullet.Value.State.RemainingHits--;

                if (bullet.Value.State.RemainingHits <= 0)
                {
                    DieEnemy(enemy);
                    shouldDeactivate = true;
                    break;
                }
            }

            if (!_state.BulletBounds.Contains(bullet.Value.transform.position))
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

            bullet = ref _state.ActiveBullets.Next(ref bullet);
        }
    }

    private static void UpdateZombies()
    {
        //DeactivateZombie(enemy.State.Index);
        /*if (_state.ActiveZombies.Count == 0)
        {
            return;
        }

        ref var zombie = ref _state.ActiveZombies.Tail();

        while (true)
        {
            if (zombie.Value.State.HasCollided && zombie.Value.State.CollidedWithBullet.State.IsActive)
            {
                DeactivateZombie(ref zombie);
            }

            if (!zombie.HasNext)
            {
                break;
            }

            zombie = ref _state.ActiveZombies.Next(ref zombie);
        }*/
    }

    private static void TryFireStraight(Input input)
    {
        if (!input.FireStraight)
        {
            return;
        }

        ActivateBullet();
    }

    private static void ActivateBullet()
    {
        var bullet = _state.InactiveBullets.Pop();
        bullet.State.RemainingHits = 1;
        bullet.RigidBody.simulated = true;
        bullet.RigidBody.velocity = new Vector2(500f, 0f);
        bullet.transform.position = _references.GunNozzle.position;
        _state.ActiveBullets.Add(bullet);
    }

    private static void DeactivateBullet(ref ArrayListNode<BulletComponent> bullet)
    {
        _state.ActiveBullets.Remove(ref bullet);
        _state.InactiveBullets.Push(bullet.Value);
        bullet.Value.RigidBody.simulated = false;
        bullet.Value.transform.position = new Vector3(-1000f, 0f, 0f);
    }

    private static void ActivateZombie()
    {
        var zombie = _state.InactiveZombies.Pop();
        zombie.State.SpeedFactor = Random.Range(0.8f, 1.5f);
        zombie.State.Index = _state.ActiveZombies.Add(zombie);
        const float zombieVelocity = 100f;
        zombie.RigidBody.simulated = true;
        zombie.RigidBody.velocity = new Vector2(-zombieVelocity * zombie.State.SpeedFactor, 0f);
        zombie.Collider.enabled = true;
        zombie.transform.position = _references.ZombieSpawn.position;
    }

    private static void DieEnemy(EnemyComponent enemy)
    {
        const float zombieCorpseVelocity = 50f;
        enemy.Animator.SetTrigger(Die);
        enemy.State.IsDead = true;
        enemy.Collider.enabled = false;
        enemy.RigidBody.velocity = new Vector2(-zombieCorpseVelocity, 0f);
    }

    private static void DeactivateZombie(int index)
    {
        DeactivateZombie(ref _state.ActiveZombies.GetAt(index));
    }

    private static void DeactivateZombie(ref ArrayListNode<EnemyComponent> zombie)
    {
        _state.ActiveZombies.Remove(ref zombie);
        _state.InactiveZombies.Push(zombie.Value);
        zombie.Value.RigidBody.simulated = false;
        zombie.Value.transform.position = new Vector3(-1000f, 0f, 0f);
    }

    public struct Input
    {
        public bool FireStraight;
        public bool FireDiagonally;
    }

    private struct State
    {
        public Rect BulletBounds;
        public float ZombieTickCooldown;
        public ArrayList<BulletComponent> ActiveBullets;
        public Stack<BulletComponent> InactiveBullets;
        public ArrayList<EnemyComponent> ActiveZombies;
        public Stack<EnemyComponent> InactiveZombies;
    }
}