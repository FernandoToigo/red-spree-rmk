using System;
using UnityEngine;

public class EnemyComponent : MonoBehaviour
{
    [NonSerialized] public int Index;
    public Rigidbody RigidBody;
    public Animator Animator;
}