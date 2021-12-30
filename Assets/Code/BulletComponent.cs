using UnityEngine;

[ExecuteInEditMode]
public class BulletComponent : MonoBehaviour
{
    private const string ZombieTag = "Zombie";
    private const string VultureTag = "Vulture";
    public BulletState State;
    public Rigidbody RigidBody;

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.CompareTag(ZombieTag))
        {
            State.CollidedZombies.Add(collider.gameObject.GetComponent<EnemyComponent>());
        }
        else if (collider.gameObject.CompareTag(VultureTag))
        {
            State.CollidedVultures.Add(collider.gameObject.GetComponent<EnemyComponent>());
        }
    }

    public struct BulletState
    {
        public int RemainingHits;
        public ReusableArray<EnemyComponent> CollidedZombies;
        public ReusableArray<EnemyComponent> CollidedVultures;
    }
}