using UnityEngine;

[CreateAssetMenu(menuName = "Definitions/Definitions")]
public class Definitions : ScriptableObject
{
    [Header("Player")]
    public float PlayerSpeed = 50f;
    public int StartingBulletCount = 3;
    public float BulletSpeed = 500f;
    [Header("Zombie")]
    public float ZombieBaseSpeed = 50f;
    public float ZombieMinimumSpeedFactor = 0.8f;
    public float ZombieMaximumSpeedFactor = 1.5f;
    public float ZombieSpawnTickSeconds = 0.25f;
    public float DoubleBulletCollectionChance = 0.4f;
    [Header("Vulture")]
    public float VultureSpeed = 100f;
    public float VultureSpawnTickSeconds = 0.25f;
    public float VultureMinimumSpeedFactor = 0.8f;
    public float VultureMaximumSpeedFactor = 1.5f;
}