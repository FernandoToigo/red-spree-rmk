using System;
using UnityEngine;

public class BulletComponent : MonoBehaviour
{
    public BulletState State;
    public Rigidbody2D RigidBody;

    private void OnTriggerEnter2D(Collider2D collider)
    {
        State.CollidedEnemies.Add(collider.gameObject.GetComponent<EnemyComponent>());
    }

    public struct BulletState
    {
        public int RemainingHits;
        public ReusableArray<EnemyComponent> CollidedEnemies;
    }
}