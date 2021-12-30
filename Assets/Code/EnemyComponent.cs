using UnityEngine;

public class EnemyComponent : MonoBehaviour
{
    public EnemyState State;
    public Rigidbody RigidBody;
    public Animator Animator;

    public struct EnemyState
    {
        public bool IsDead;
        public int Index;
        public float SpeedFactor;
    }
}