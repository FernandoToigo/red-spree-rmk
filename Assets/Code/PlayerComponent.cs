using UnityEngine;

public class PlayerComponent : MonoBehaviour
{
    public Transform Center;
    public Animator Animator;
    public ReusableArray<EnemyComponent> CollidedEnemies = new ReusableArray<EnemyComponent>(10);
    
    private void OnTriggerEnter2D(Collider2D collider)
    {
        CollidedEnemies.Add(collider.gameObject.GetComponent<EnemyComponent>());
    }
}