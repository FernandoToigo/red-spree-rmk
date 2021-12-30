using UnityEngine;

[ExecuteInEditMode]
public class PlayerComponent : MonoBehaviour
{
    public Transform Center;
    public Animator Animator;
    public ReusableArray<EnemyComponent> CollidedEnemies = new ReusableArray<EnemyComponent>(10);
    
    private void OnTriggerEnter(Collider collider)
    {
        CollidedEnemies.Add(collider.gameObject.GetComponent<EnemyComponent>());
    }
}