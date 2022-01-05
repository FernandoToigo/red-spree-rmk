using System;
using UnityEngine;

[ExecuteInEditMode]
public class BulletComponent : MonoBehaviour
{
    private const string ZombieTag = "Zombie";
    private const string VultureTag = "Vulture";
    public Rigidbody RigidBody;
    
    [NonSerialized] public int Index;
    [NonSerialized] public ReusableArray<EnemyComponent> CollidedZombies;
    [NonSerialized] public ReusableArray<EnemyComponent> CollidedVultures;

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.CompareTag(ZombieTag))
        {
            CollidedZombies.Add(collider.gameObject.GetComponent<EnemyComponent>());
        }
        else if (collider.gameObject.CompareTag(VultureTag))
        {
            CollidedVultures.Add(collider.gameObject.GetComponent<EnemyComponent>());
        }
    }
}