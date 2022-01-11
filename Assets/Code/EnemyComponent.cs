using System;
using UnityEngine;

public class EnemyComponent : MonoBehaviour
{
    public const string ZombieTag = "Zombie";
    public const string VultureTag = "Vulture";
    
    public Rigidbody RigidBody;
    public Animator Animator;
    
    [NonSerialized] public int Index;
}