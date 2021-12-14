using UnityEngine;

public class EnemyComponent : MonoBehaviour
{
    public EnemyState State;
    public Rigidbody2D RigidBody;

    public struct EnemyState
    {
        public int Index;
        public float SpeedFactor;
    }
}