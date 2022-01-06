using System;
using UnityEngine;

[ExecuteInEditMode]
public class PlayerComponent : MonoBehaviour
{
    public Transform Center;
    public Animator Animator;
    
    [NonSerialized] public ReusableArray<EnemyComponent> CollidedZombies = new ReusableArray<EnemyComponent>(10);
    [NonSerialized] public ReusableArray<EnemyComponent> CollidedVultures = new ReusableArray<EnemyComponent>(10);

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.CompareTag(Game.ZombieTag))
        {
            CollidedZombies.Add(collider.gameObject.GetComponent<EnemyComponent>());
        }
        else if (collider.gameObject.CompareTag(Game.VultureTag))
        {
            CollidedVultures.Add(collider.gameObject.GetComponent<EnemyComponent>());
        }
    }
}